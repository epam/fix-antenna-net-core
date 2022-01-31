// Copyright (c) 2021 EPAM Systems
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Common.Utils;

namespace Epam.FixAntenna.NetCore.FixEngine.Storage.Queue
{
	internal class MmfPersistentInMemoryQueue<T> : InMemoryQueue<T> where T : IQueueable
	{
		private const int OneMeg = 1024 * 1024;
		private const byte EmptyByte = 0;
		private const byte DeleteMarkerByte = 64;
		private static readonly MmfPersistentInMemoryQueueUtils Utils = new MmfPersistentInMemoryQueueUtils();
		private static readonly int DefaultBufferSize = 10 * OneMeg;
		private static readonly int DefaultGrowSize = DefaultBufferSize / 5;

		private readonly IQueueableFactory<T> _factory;
		private readonly string _filename;

		private readonly ILog _log;
		private MemoryMappedViewStream _mappedBuffer;
		private readonly ByteBuffer _buffer = new ByteBuffer();

		protected internal MemoryMappedFile AccessFile;

		public MmfPersistentInMemoryQueue(string filename, IQueueableFactory<T> factory)
		{
			_factory = factory;
			_filename = filename;
			_log = LogFactory.GetLog(GetType());
		}

		/// <inheritdoc/>
		public override void Initialize()
		{
			if (AccessFile != null)
			{
				Shutdown();
			}

			base.Initialize();
			var position = 0;
			try
			{
				if (System.IO.File.Exists(_filename))
				{
					position = Restore(_filename);
				}
				else
				{
					new FileHelper(_filename).CreateNewFile();
				}
			}
			catch (Exception e)
			{
				if (_log.IsDebugEnabled)
				{
					_log.Warn("IQueue restore failed. There is a possible loss of previously queued messages. " + e, e);
				}
				else
				{
					_log.Warn("IQueue restore failed. There is a possible loss of previously queued messages. " +
							e.Message);
				}

				try
				{
					new FileHelper(_filename).CopyToTemporaryFile().CreateNewFile();
				}
				catch (IOException e1)
				{
					throw new Exception(e1.Message, e1);
				}
			}

			try
			{
				var fileLength = new FileInfo(_filename).Length;
				if (fileLength > int.MaxValue)
				{
					throw new InvalidOperationException("This queue can work with files no more than " + int.MaxValue +
														" bytes");
				}

				var capacity = Math.Max(DefaultBufferSize, (int)fileLength);
				AccessFile = MemoryMappedFile.CreateFromFile(_filename, FileMode.OpenOrCreate, null, capacity,
					MemoryMappedFileAccess.ReadWrite);
				_mappedBuffer = AccessFile.CreateViewStream();
				_mappedBuffer.Position = position;
			}
			catch (IOException e)
			{
				throw new Exception(e.Message, e);
			}

			_log.Debug("MMFPersistentInMemoryQueue started");
		}

		/// <inheritdoc/>
		public override void Commit()
		{
			lock (this)
			{
				var application = base.IsApplicationCommit;
				base.Commit();
				if (application)
				{
					if (IsShutdown)
					{
						throw new InvalidOperationException("IQueue is shutdown");
					}

					if (IsEmpty && _mappedBuffer.Position > OneMeg)
					{
						// if no messages scheduled and this is delete marker truncate the file and return
						if (_mappedBuffer.Position > DefaultBufferSize)
						{
							ClearAndNormalizeFile();
						}
						else
						{
							ClearBuffer(_mappedBuffer.Position);
						}
					}
					else
					{
						try
						{
							WriteDeleteMarker();
						}
						catch (IOException e)
						{
							if (_log.IsDebugEnabled)
							{
								_log.Warn(e.Message, e);
							}
							else
							{
								_log.Warn(e.Message);
							}
						}
					}
				}
			}
		}

		/// <inheritdoc/>
		public override bool Add(T element)
		{
			lock (this)
			{
				var result = AppendEntryToFile(element);
				return result && base.Add(element);
			}
		}

		/// <inheritdoc/>
		public override void Clear()
		{
			lock (this)
			{
				base.Clear();
				ClearAndNormalizeFile();
			}
		}

		/// <inheritdoc/>
		public override void Shutdown()
		{
			lock (this)
			{
				base.Shutdown();
				CloseBuffer();
				if (AccessFile != null)
				{
					try
					{
						AccessFile.Dispose();
					}
					catch (IOException)
					{
					}
				}

				AccessFile = null;
			}
		}

		/// <summary>
		/// Appends entry to file.
		/// <para>
		/// Methods append entry to file if it is opened.
		/// </para>
		/// </summary>
		/// <returns> false if entry doesn't append to file. </returns>
		public virtual bool AppendEntryToFile(IQueueable entry)
		{
			try
			{
				if (IsShutdown)
				{
					_log.Warn("IQueue is shutdown, skip entry:" + entry.ToString());
					throw new InvalidOperationException("IQueue is shutdown");
				}

				WriteData(entry);
				return true;
			}
			catch (IOException e)
			{
				throw new Exception(e.Message, e);
			}
		}

		private void WriteData(IQueueable entry)
		{
			_buffer.ResetBuffer();
			entry.SerializeTo(_buffer);
			var header = Utils.GetLengthHeader(_buffer.Offset);
			ValidateBuffer(_buffer.Offset + header.Length);
			_mappedBuffer.Write(header, 0, header.Length); // length
			_mappedBuffer.Write(_buffer.GetByteArray(), 0, _buffer.Offset);
		}

		private void WriteDeleteMarker()
		{
			ValidateBuffer(1);
			_mappedBuffer.WriteByte(DeleteMarkerByte);
		}

		private void ValidateBuffer(int increment)
		{
			if (_mappedBuffer.Position + increment > _mappedBuffer.Capacity)
			{
				var newSize = Math.Max(_mappedBuffer.Capacity + DefaultGrowSize, _mappedBuffer.Position + increment);
				if (newSize > int.MaxValue)
				{
					throw new IOException("This queue can work with files no more than " + int.MaxValue + " bytes");
				}

				CloseBuffer();
				_mappedBuffer = AccessFile.CreateViewStream();
				_mappedBuffer.Position = 0;
			}
		}

		/// <param name="fileName"> </param>
		/// <returns> last file position </returns>
		/// <exception cref="IOException"> </exception>
		private int Restore(string fileName)
		{
			base.Clear();
			var position = 0;
			using (var ois =
				new BufferedStream(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
			{
				var lengthLength = ois.ReadByte();
				while (lengthLength != -1)
				{
					// 1 byte record length length [n]
					position++;
					if (lengthLength == DeleteMarkerByte)
					{
						_log.Debug("Committed");
						base.Poll();
						base.Commit();
					}
					else if (lengthLength == EmptyByte)
					{
						// EOF. position -1 because we have read empty byte
						return --position;
					}
					else
					{
						var length = 0;
						// n bytes record length [m]
						for (var i = 0; i < lengthLength; i++)
						{
							length = (length << 8) + ois.ReadByte();
							position++;
						}

						var messageHolder = new byte[length];
						// [length] bytes record itself
						var read = ois.Read(messageHolder, 0, length);
						if (read < length)
						{
							throw new Exception("IQueue file is corrupted");
						}

						var element = _factory.CreateObject();
						element.FromBytes(messageHolder, 0, length);
						if (_log.IsDebugEnabled)
						{
							_log.Debug("Restored " + element.ToString());
						}

						base.Add(element);
						position += length;
					}

					lengthLength = ois.ReadByte();
				}
			}

			return position;
		}

		public virtual void ClearAndNormalizeFile()
		{
			try
			{
				if (_mappedBuffer.Capacity > DefaultBufferSize)
				{
					CloseBuffer();

					_mappedBuffer.SetLength(0);
					_mappedBuffer = AccessFile.CreateViewStream();
					_mappedBuffer.Position = 0;
				}
				else
				{
					ClearBuffer();
				}
			}
			catch (IOException e)
			{
				throw new Exception(e.Message, e);
			}
		}

		private void ClearBuffer()
		{
			ClearBuffer(_mappedBuffer.Capacity);
		}

		private void ClearBuffer(long toPoint)
		{
			_mappedBuffer.Position = 0;
			for (var i = 0; i > toPoint; i++)
			{
				_mappedBuffer.WriteByte(EmptyByte);
			}

			_mappedBuffer.Position = 0;
		}

		private void CloseBuffer()
		{
			if (_mappedBuffer != null)
			{
				try
				{
					_mappedBuffer.Flush();
					_mappedBuffer.Close();
				}
				finally
				{
					_mappedBuffer = null;
				}
			}
		}
	}
}
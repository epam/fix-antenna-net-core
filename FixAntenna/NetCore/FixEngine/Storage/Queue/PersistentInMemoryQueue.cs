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
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Common.Utils;

namespace Epam.FixAntenna.NetCore.FixEngine.Storage.Queue
{
	/// <summary>
	/// Persistent queue file format is the following: <br/>
	/// 1 byte record length length [n]; <br/>
	/// n bytes record length [m] <br/>
	/// m bytes record itself <br/>
	/// DELETE marker is byte == (byte)0;
	/// </summary>
	/// <seealso cref="IQueueable"> </seealso>
	internal class PersistentInMemoryQueue<T> : InMemoryQueue<T> where T : IQueueable
	{
		private const long OneMeg = 1024 * 1024;
		private static readonly PersistentInMemoryQueueUtils Utils = new PersistentInMemoryQueueUtils();

		protected internal static readonly IQueueable DeleteMarker = new QueueableAnonymousInnerClass();

		private readonly IQueueableFactory<T> _factory;
		private readonly string _filename;

		private readonly ILog _log;
		private readonly ByteBuffer _buffer;
		private byte[] _messageHolder;
		private FileStream _queueOutputStream;

		public PersistentInMemoryQueue(string filename, IQueueableFactory<T> factory)
		{
			_factory = factory;
			_filename = filename;
			_buffer = new ByteBuffer();
			_log = LogFactory.GetLog(nameof(PersistentInMemoryQueue<T>));
		}

		/// <inheritdoc/>
		public override void Initialize()
		{
			lock (this)
			{
				var initialized = IsInitialized();
				var restored = false;
				if (initialized)
				{
					Shutdown();
				}

				base.Initialize();
				try
				{
					if (System.IO.File.Exists(_filename))
					{
						if (!initialized)
						{
							Restore(_filename);
							restored = true;
						}
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
						_log.Warn("IQueue restore failed. There is a possible loss of previously queued messages. " + e,
							e);
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
					_queueOutputStream = new FileStream(_filename, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
					if (initialized)
					{
						_queueOutputStream.Seek(0, SeekOrigin.End);
					}

					if (restored)
					{
						if (IsEmpty && _queueOutputStream.Length > 0)
						{
							_queueOutputStream.SetLength(0);
						}
					}
				}
				catch (IOException e)
				{
					throw new Exception(e.Message, e);
				}

				_log.Debug("PersistentInMemoryQueue started. IQueue size " + TotalSize);
			}
		}

		private bool IsInitialized()
		{
			return _queueOutputStream != null;
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
					AppendEntryToFile(DeleteMarker);
				}
			}
		}

		/// <inheritdoc/>
		public override void Clear()
		{
			lock (this)
			{
				base.Clear();
				try
				{
					_queueOutputStream.SetLength(0);
				}
				catch (IOException e)
				{
					throw new Exception(e.Message, e);
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

		/// <summary>
		/// Appends entry to file.
		/// <para>
		/// Methods append entry to file if it is opened.
		/// Method truncates the file only if entry is <see cref="DeleteMarker"/> and size of file gr <c>OneMeg</c>.
		/// </para>
		/// </summary>
		/// <returns> false if entry doesn't append to file. </returns>
		public virtual bool AppendEntryToFile(IQueueable entry)
		{
			try
			{
				if (IsShutdown)
				{
					if (_log.IsWarnEnabled)
					{
						_log.Warn("IQueue is shutdown, skip entry:" + entry.ToString());
					}

					throw new InvalidOperationException("IQueue is shutdown");
				}

				if (entry == DeleteMarker && IsEmpty && _queueOutputStream.Length > OneMeg)
				{
					// if no messages scheduled and this is delete marker truncate the file and return
					_queueOutputStream.SetLength(0);
				}

				WriteDataToChannel(entry);
				return true;
			}
			catch (IOException e)
			{
				throw new Exception(e.Message, e);
			}
		}

		private void WriteDataToChannel(IQueueable entry)
		{
			_buffer.ResetBuffer();
			entry.SerializeTo(_buffer);

			var bytesToWrite = Utils.GetLengthHeader(_buffer.Offset);

			_queueOutputStream.Seek(0, SeekOrigin.End);
			_queueOutputStream.Write(bytesToWrite, 0, bytesToWrite.Length);

			bytesToWrite = _buffer.GetByteArray(0, _buffer.Offset);
			_queueOutputStream.Seek(0, SeekOrigin.End);
			_queueOutputStream.Write(bytesToWrite, 0, bytesToWrite.Length);

			_queueOutputStream.Flush(false);
		}

		private void Restore(string fileName)
		{
			_messageHolder = new byte[1024];

			if (_log.IsDebugEnabled)
			{
				_log.Debug("Restore from file: " + fileName);
			}

			base.Clear();

			using (var ois = new BufferedStream(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
			{
				var lengthLength = ois.ReadByte();
				while (lengthLength != -1)
				{
					// 1 byte record length length [n]
					if (lengthLength == 0)
					{
						if (_log.IsDebugEnabled)
						{
							_log.Debug("Committed");
						}

						if (base.Poll() != null)
						{
							base.Commit();
						}
					}
					else
					{
						var length = 0;
						// n bytes record length [m]
						for (var i = 0; i < lengthLength; i++)
						{
							length = (length << 8) + ois.ReadByte();
						}

						if (_messageHolder.Length < length)
						{
							_messageHolder = new byte[length * 2];
						}

						var read = ois.Read(_messageHolder, 0, length);
						if (read < length)
						{
							throw new Exception("IQueue file is corrupted");
						}

						var element = _factory.CreateObject();
						element.FromBytes(_messageHolder, 0, length);
						if (_log.IsDebugEnabled)
						{
							_log.Debug("Restored " + element.ToString());
						}

						base.Add(element);
					}

					lengthLength = ois.ReadByte();
				}
			}
		}

		/// <inheritdoc/>
		public override void Shutdown()
		{
			lock (this)
			{
				base.Shutdown();
				if (_queueOutputStream != null)
				{
					try
					{
						_queueOutputStream.Dispose();
					}
					catch (IOException)
					{
						_log.Debug("Can't close queue file stream: " + _filename);
					}
				}

				_queueOutputStream = null;
			}
		}

		private class QueueableAnonymousInnerClass : IQueueable
		{
			/// <inheritdoc />
			public void SerializeTo(ByteBuffer buffer)
			{
				buffer.ResetBuffer();
			}

			/// <inheritdoc />
			public void FromBytes(byte[] bytes, int i, int length)
			{
			}
		}
	}
}
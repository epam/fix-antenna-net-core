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
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Storage.File
{
	/// <summary>
	/// Indexed message storage implementation.
	/// </summary>
	/// <seealso cref="FilesystemStorageFactory"> </seealso>
	internal class IndexedMessageStorage : FlatFileMessageStorage
	{
		private const int IndexLength = 12;
		public const string DefaultExt = ".idx";
		private static readonly ILog Log = LogFactory.GetLog(typeof(IndexedMessageStorage));

		private readonly object _indexLock = new object();
		private readonly ByteBuffer _indexBlock = new ByteBuffer(IndexLength);

		private FileStream _indexFile;
		private string _indexFileExtension = DefaultExt;

		public IndexedMessageStorage(Config config) : base(config)
		{
		}

		/// <inheritdoc />
		protected internal override int CalculateFormatLength()
		{
			if (IsTimestampEnabled)
			{
				return base.CalculateFormatLength();
			}

			return 0;
		}

		/// <summary>
		/// Retrieves message from storage.
		/// </summary>
		/// <param name="from">     the start sequence position </param>
		/// <param name="to">       the end sequence position </param>
		/// <param name="listener"> the callback listener </param>
		/// <param name="blocking"> if sets to true the method call not blocking </param>
		/// <exception cref="IOException"></exception>
		/// <exception cref="StorageClosedException"></exception>
		/// <exception cref="InvalidOperationException"></exception>
		protected override void RetrieveMessagesImplementation(long from, long to, IMessageStorageListener listener, bool blocking)
		{
			if (from < 1)
			{
				throw new ArgumentException("From can't be <1");
			}

			if (to < 1)
			{
				throw new ArgumentException("To can't be <1");
			}

			FileStream readFile = null;
			try
			{
				readFile = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

				for (var i = from; i <= to; i++)
				{
					var indexBlock = new ByteBuffer(IndexLength);
					if (IsClosed)
					{
						throw new InvalidOperationException("Storage is closed");
					}

					indexBlock.Position = 0;
					var len = -1;
					lock (_indexLock)
					{
						_indexFile.Position = (i - 1) * IndexLength;
						var tempIndexData = new byte[IndexLength];
						len = _indexFile.Read(tempIndexData, 0, tempIndexData.Length);
						indexBlock.Put(tempIndexData, 0, len);
					}

					if (len <= 0)
					{
						return; //no more data
					}

					var msgPos = GetMessagePosition(indexBlock);
					var msgLen = GetMessageSize(indexBlock);
					var message = ReadFromStorage(readFile, msgPos, msgLen);

					NotifyListener(listener, blocking, message);
				}
			}
			catch (Exception e)
			{
				if (Log.IsWarnEnabled)
				{
					if (Log.IsDebugEnabled)
					{
						Log.Warn("Problem in retrieving messages from indexed message storage: " + e.ToString(), e);
					}
					else
					{
						Log.Warn("Problem in retrieving messages from indexed message storage: " + e.ToString());
					}
				}

				if (IsClosed)
				{
					throw new StorageClosedException("Storage is closed. Cause: " + e.Message);
				}
				else
				{
					throw new IOException(e.Message);
				}
			}
			finally
			{
				readFile?.Close();
			}
		}

		public virtual byte[] ReadFromStorage(FileStream readChannel, long readFrom, int messageLength)
		{
			var message = new byte[messageLength];
			readChannel.Position = readFrom;
			readChannel.Read(message, 0, messageLength);
			return message;
		}

		private int GetMessageSize(ByteBuffer indexBlock)
		{
			var indexB = indexBlock.ToArray();
			return (indexB[8] << 24)
					| ((indexB[9] << 16) & 0x00FF0000)
					| ((indexB[10] << 8) & 0x0000FF00)
					| (indexB[11] & 0x0000000FF);
		}

		private long GetMessagePosition(ByteBuffer indexBlock)
		{
			var indexB = indexBlock.ToArray();
			return ((long)indexB[0] << 56)
					| (((long)indexB[1] << 48) & 0x00FF000000000000L)
					| (((long)indexB[2] << 40) & 0x0000FF0000000000L)
					| (((long)indexB[3] << 32) & 0x000000FF00000000L)
					| (((long)indexB[4] << 24) & 0x00000000FF000000L)
					| (((long)indexB[5] << 16) & 0x0000000000FF0000L)
					| (((long)indexB[6] << 8) & 0x000000000000FF00L)
					| (indexB[7] & 0x00000000000000FFL);
		}

		/// <summary>
		/// Opens the indexed file. </summary>
		/// <exception cref="IOException"> if I/O errors occurred </exception>
		protected override void OpenStorageFile()
		{
			_indexFile = new FileStream(FileName + _indexFileExtension, FileMode.OpenOrCreate, FileAccess.ReadWrite,
				FileShare.ReadWrite);
			base.OpenStorageFile();
		}

		/// <summary>
		/// Close the storage.
		/// </summary>
		/// <exception cref="IOException"> if I/O error occurred </exception>
		public override void Close()
		{
			Log.Debug("Indexed storage closed");
			base.Close();
			if (_indexFile != null)
			{
				lock (_indexLock)
				{
					_indexFile.Close();
				}
			}
		}

		/// <inheritdoc />
		protected internal override long AppendMessageInternal(long ticks, byte[] message, int offset, int length)
		{
			var msgPos = ChannelPosition + FormatLength;
			var size = AppendMessageToFile(ticks, message, offset, length);
			var seqNum = RawFixUtil.GetSequenceNumber(message, offset, length);
			if (seqNum == 1 && RawFixUtil.IsLogon(RawFixUtil.GetMessageType(message)))
			{
				// take care of new session with new sequences
				_indexFile.SetLength(0);
			}

			var indexPos = (seqNum - 1) * IndexLength;

			var msgLen = length;

			_indexBlock.Position = 0;
			_indexBlock.Put((byte)(long)((ulong)msgPos >> 56));
			_indexBlock.Put((byte)(long)((ulong)msgPos >> 48));
			_indexBlock.Put((byte)(long)((ulong)msgPos >> 40));
			_indexBlock.Put((byte)(long)((ulong)msgPos >> 32));
			_indexBlock.Put((byte)(long)((ulong)msgPos >> 24));
			_indexBlock.Put((byte)(long)((ulong)msgPos >> 16));
			_indexBlock.Put((byte)(long)((ulong)msgPos >> 8));
			_indexBlock.Put((byte)msgPos);
			_indexBlock.Put((byte)(int)((uint)msgLen >> 24));
			_indexBlock.Put((byte)(int)((uint)msgLen >> 16));
			_indexBlock.Put((byte)(int)((uint)msgLen >> 8));
			_indexBlock.Put((byte)msgLen);

			var indexB = _indexBlock.GetByteArray();

			lock (_indexLock)
			{
				_indexFile.Position = indexPos;
				_indexFile.Write(indexB, 0, indexB.Length);
			}

			return size;
		}

		public virtual long AppendMessageToFile(long ticks, byte[] message, int offset, int length)
		{
			return base.AppendMessageInternal(ticks, message, offset, length);
		}

		/// <inheritdoc />
		public override void BackupStorageFile(string fullPathToStorageFile, string fullPathToDestinationBackupFile)
		{
			base.BackupStorageFile(fullPathToStorageFile, fullPathToDestinationBackupFile);
			BackupFile(fullPathToStorageFile + _indexFileExtension,
				fullPathToDestinationBackupFile + _indexFileExtension);
		}

		/// <inheritdoc />
		public override void DeleteStorageFile(string fullPathToStorageFile)
		{
			base.DeleteStorageFile(fullPathToStorageFile);
			DeleteFile(fullPathToStorageFile + _indexFileExtension);
		}
	}
}
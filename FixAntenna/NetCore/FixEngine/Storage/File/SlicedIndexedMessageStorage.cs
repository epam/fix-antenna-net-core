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
	internal class SlicedIndexedMessageStorage : SlicedFileMessageStorage
	{
		private const int IndexLength = 16;
		public const string DefaultExt = ".idx";
		private static readonly ILog Log = LogFactory.GetLog(typeof(SlicedIndexedMessageStorage));
		private static readonly byte[] EmptyData = Array.Empty<byte>();

		private readonly object _indexLock = new object();
		private readonly ByteBuffer _indexBlock = new ByteBuffer(IndexLength);

		private FileStream _index;
		private string _indexFileExtension = DefaultExt;
		private long _startMsgPos;

		public SlicedIndexedMessageStorage(Config config) : base(config)
		{
		}

		/// <summary>
		/// Opens the indexed file. </summary>
		/// <exception cref="IOException"> if I/O errors occurred </exception>
		protected override void OpenStorageFile()
		{
			_index = new FileStream(GetIndexFileName(), FileMode.OpenOrCreate, FileAccess.ReadWrite,
				FileShare.ReadWrite);
			base.OpenStorageFile();
		}

		public virtual string GetIndexFileName()
		{
			return FileManager.GetFileNameParent() + _indexFileExtension;
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
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="StorageClosedException"></exception>
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
			var currentChunk = -1;
			try
			{
				for (var i = from; i <= to; i++)
				{
					var indexBlock = new ByteBuffer(IndexLength);
					if (IsClosed)
					{
						throw new InvalidOperationException("Storage is closed");
					}

					var len = -1;
					lock (_indexLock)
					{
						_index.Position = (i - 1) * IndexLength;
						var tempIndexData = new byte[IndexLength];
						len = _index.Read(tempIndexData, 0, tempIndexData.Length);
						indexBlock.Put(tempIndexData, 0, len);
					}

					if (len <= 0)
					{
						return; //no more data
					}

					indexBlock.Position = 0;
					var chunkId = indexBlock.GetIntBe();
					var messagePosition = indexBlock.GetLongBe();
					var messageLength = indexBlock.GetIntBe();
					if (currentChunk != chunkId && chunkId != 0)
					{
						// chunkId==0 it is means that there is gap.
						currentChunk = chunkId;
						if (readFile != null)
						{
							readFile.Close();
							readFile = null;
						}

						readFile = new FileStream(FileManager.GetFileName(currentChunk), FileMode.Open, FileAccess.Read,
							FileShare.ReadWrite);
					}

					var message = ReadFromStorage(readFile, messagePosition, messageLength);

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
			if (messageLength == 0)
			{
				return EmptyData;
			}

			var message = new byte[messageLength];
			readChannel.Position = readFrom;
			readChannel.Read(message, 0, messageLength);
			return message;
		}

		/// <summary>
		/// Close the storage.
		/// </summary>
		/// <exception cref="IOException">if I/O error occurred </exception>
		public override void Close()
		{
			Log.Debug("Indexed storage closed");
			base.Close();
			if (_index != null)
			{
				lock (_indexLock)
				{
					_index.Close();
				}
			}
		}

		/// <seealso cref="AbstractFileMessageStorage.GetNextSequenceNumber"> </seealso>
		protected override long GetNextSequenceNumber()
		{
			if (!IsIndexValid())
			{
				Log.Warn("Index file " + FileName + _indexFileExtension +
						" is corrupt and will be truncated. Sequence won't be restored.");
				_index.SetLength(0);
				return 1;
			}

			return _index.Length / IndexLength + 1;
		}

		private bool IsIndexValid()
		{
			return _index.Length % IndexLength == 0;
		}

		/// <inheritdoc />
		protected internal override long AppendMessageInternal(long timestamp, byte[] message, int offset, int length)
		{
			_startMsgPos = ChannelPosition + FormatLength;
			var size = AppendMessageToFile(timestamp, message, offset, length);
			var seqNum = RawFixUtil.GetSequenceNumber(message, offset, length);
			if (seqNum == 1 && RawFixUtil.IsLogon(RawFixUtil.GetMessageType(message)))
			{
				// take care of new session with new sequences
				_index.SetLength(0);
			}

			var indexPos = (seqNum - 1) * IndexLength;

			var msgLen = length;
			var chunkId = FileManager.GetChunkId();

			_indexBlock.Position = 0;
			_indexBlock.PutIntBe(chunkId);
			_indexBlock.PutLongBe(_startMsgPos);
			_indexBlock.PutIntBe(msgLen);
			_indexBlock.Position = 0;

			lock (_indexLock)
			{
				_index.Position = indexPos;
				var bytesToWrite = _indexBlock.GetByteArray();
				_index.Write(bytesToWrite, 0, bytesToWrite.Length);
				_index.Flush();
			}

			return size;
		}

		public override void NextChunk()
		{
			base.NextChunk();
			_startMsgPos = FormatLength;
		}

		public virtual long AppendMessageToFile(long timestamp, byte[] message, int offset, int length)
		{
			return base.AppendMessageInternal(timestamp, message, offset, length);
		}

		public override void BackupStorageFile(string fullPathToStorageFile, string fullPathToDestinationBackupFile)
		{
			base.BackupStorageFile(fullPathToStorageFile, fullPathToDestinationBackupFile);
			BackupFile(GetIndexFileName(), fullPathToDestinationBackupFile + _indexFileExtension);
		}

		public override void DeleteStorageFile(string fullPathToStorageFile)
		{
			base.DeleteStorageFile(fullPathToStorageFile);
			DeleteFile(GetIndexFileName());
		}
	}
}
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
using System.Threading;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Storage.File
{
	/// <summary>
	/// <para>WARNING: Can persist message with sequence number is not greater than 178,956,969.</para>
	/// </summary>
	internal class MmfIndexedMessageStorage : AbstractFileMessageStorage
	{
		internal const int DefaultStorageGrowSize = 100 * 1024 * 1024;
		internal const int DefaultIndexGrowSize = 20 * 1024 * 1024;
		internal const byte NewLine = (byte)'\n';
		internal const long Delta = 1000;
		internal const int IndexLength = 12;
		internal const int PositionLength = 16;
		internal const string DefaultExt = ".idx";
		public static readonly int MaxSeqnum = (int.MaxValue - PositionLength) / IndexLength;

		private readonly ReaderWriterLockSlim _indexMappedBufferLock =
			new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

		private readonly ILog _log;
		private readonly object _mappedBufferLock = new object();
		private readonly string _indexFileExtension = DefaultExt;

		private MemoryMappedFile _indexFile;
		private FileStream _indexFileStream;
		private MemoryMappedViewAccessor _indexLastPositionBuffer;
		private long _indexMappedSize;
		private MemoryMappedViewAccessor _indexMessagesPositionsBuffer;
		private long _mappedStartPosition;
		private int _recordLength;
		private long _seqNum;
		private MemoryMappedViewStream _storageBuffer;

		private MemoryMappedFile _storageFile;
		protected internal long ChannelLength;
		protected internal long ChannelPosition;
		protected internal byte[] DateFormattedBuffer;
		protected internal long IndexGrowSize;

		protected internal long StorageGrowSize = DefaultStorageGrowSize;

		protected internal long TimestampTicks;
		protected internal bool TimestampsInLogs;

		public MmfIndexedMessageStorage(Config config) : base(config)
		{
			_log = LogFactory.GetLog(GetType());
			TimestampsInLogs = Configuration.GetPropertyAsBoolean(Config.TimestampsInLogs, true);
			StorageGrowSize = Configuration.GetPropertyAsBytesLength(Config.MmfStorageGrowSize);
			if (StorageGrowSize <= 0)
			{
				_log.Warn("Parameter \"" + Config.MmfStorageGrowSize + "\" must be integer and not negative");
				StorageGrowSize = DefaultStorageGrowSize;
			}

			IndexGrowSize = Configuration.GetPropertyAsBytesLength(Config.MmfIndexGrowSize);
			if (IndexGrowSize <= 0)
			{
				_log.Warn("Parameter \"" + Config.MmfIndexGrowSize + "\" must be integer and not negative");
				IndexGrowSize = DefaultIndexGrowSize;
			}
		}

		/// <summary>
		/// Opens the indexed file. Make mapped byte buffers
		/// </summary>
		/// <returns> last send sequence number </returns>
		/// <exception cref="IOException">if I/O errors occurred</exception>
		/// <seealso cref="AbstractFileMessageStorage.Initialize"> </seealso>
		public override long Initialize()
		{
			var fileName = FileName + _indexFileExtension;
			var indexFileInfo = new FileInfo(fileName);
			var loggingDir = indexFileInfo.DirectoryName;
			if (!Directory.Exists(loggingDir))
			{
				_log.Warn("Logging directory " + Path.GetFullPath(loggingDir) + " not exist and it will be created");
				Directory.CreateDirectory(loggingDir);
			}

			// before get seqNum we must initialize index file stream. do not make mapped buffer before init.
			_indexFileStream =
				new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
			_seqNum = base.Initialize();

			var newSize = (_seqNum - 1) * IndexLength + IndexGrowSize;
			if (newSize < indexFileInfo.Length)
			{
				newSize = indexFileInfo.Length;
			}
			RemapIndexMemoryMappedFile(newSize);

			ChannelPosition = GetLastStoragePosition();
			ChannelLength = AccessFile.Length;

			RelocateStorageMemoryMappedFile(Math.Max(StorageGrowSize, AccessFile.Length) + 1);

			return _seqNum;
		}

		/// <inheritdoc />
		protected internal override int CalculateFormatLength()
		{
			return TimestampsInLogs ? base.CalculateFormatLength() : 0;
		}

		/// <inheritdoc />
		public override void AppendMessage(byte[] timestampFormatted, byte[] message, int offset, int length)
		{
			AppendMessageInternal(timestampFormatted, message, offset, length);
		}

		/// <inheritdoc />
		public override void AppendMessage(byte[] timestampFormatted, byte[] message)
		{
			AppendMessageInternal(timestampFormatted, message, 0, message.Length);
		}

		public virtual long AppendMessageInternal(byte[] timestampFormatted, byte[] message, int offset, int length)
		{
			return AppendMessageInternal(0, timestampFormatted, message, offset, length);
		}

		/// <inheritdoc />
		protected internal override long AppendMessageInternal(long ticks, byte[] message, int offset, int length)
		{
			return AppendMessageInternal(ticks, null, message, offset, length);
		}

		private long AppendMessageInternal(long ticks, byte[] timestampFormatted, byte[] message, int offset,
			int length)
		{
			var msgSeqNum = RawFixUtil.GetSequenceNumber(message, offset, length);
			ValidateSeqNum(msgSeqNum);
			var msgPos = ChannelPosition + FormatLength;
			var size = AppendMessageToFile(ticks, timestampFormatted, message, offset, length);

			if (msgSeqNum == 1 && RawFixUtil.IsLogon(RawFixUtil.GetMessageType(message)))
			{
				// take care of new session with new sequences
				ResetIndexFile();
			}

			// save storage last position. Read only at initialisation and dose need lock
			_indexLastPositionBuffer.WriteLongBe(0, ChannelPosition);
			_indexLastPositionBuffer.WriteLongBe(8, msgSeqNum);
			_seqNum = msgSeqNum;

			var writePosition = (msgSeqNum - 1) * IndexLength;
			if (writePosition + IndexLength > _indexMappedSize)
			{
				RemapIndexMemoryMappedFile(Math.Min(writePosition + IndexGrowSize, int.MaxValue));
			}

			try
			{
				_indexMappedBufferLock.EnterWriteLock();
				_indexMessagesPositionsBuffer.WriteLongBe(writePosition, msgPos);
				_indexMessagesPositionsBuffer.WriteIntBe(writePosition + 8, length);
			}
			finally
			{
				_indexMappedBufferLock.ExitWriteLock();
			}

			return size;
		}

		private void ValidateSeqNum(long msgSeqNum)
		{
			if (msgSeqNum > MaxSeqnum)
			{
				throw new IOException("This storage can persist message with sequence number is not greater than " +
									MaxSeqnum + ". But was " + msgSeqNum);
			}
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
			try
			{
				readFile = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				for (var i = from; i <= to; i++)
				{
					if (IsClosed)
					{
						throw new InvalidOperationException("Storage is closed");
					}

					// read index block
					long messagePosition;
					int messageLength;
					try
					{
						_indexMappedBufferLock.EnterReadLock();
						var readPosition = (int)(i - 1) * IndexLength;
						if (_indexMappedSize - IndexLength >= readPosition)
						{
							messagePosition = _indexMessagesPositionsBuffer.ReadLongBe(readPosition);
							messageLength = _indexMessagesPositionsBuffer.ReadIntBe(readPosition + 8);
						}
						else
						{
							//no more data
							throw new IOException("Message with seqNum " + i + " is not available.");
						}
					}
					finally
					{
						_indexMappedBufferLock.ExitReadLock();
					}

					var message = ReadFromStorage(readFile, messagePosition, messageLength);

					NotifyListener(listener, blocking, message);
				}
			}
			catch (IOException e)
			{
				if (_log.IsWarnEnabled)
				{
					if (_log.IsDebugEnabled)
					{
						_log.Warn("Problem in retrieving messages from indexed message storage: " + e.ToString(), e);
					}
					else
					{
						_log.Warn("Problem in retrieving messages from indexed message storage: " + e.ToString());
					}
				}

				if (IsClosed)
				{
					throw new StorageClosedException("Storage is closed. Cause: " + e.Message);
				}
				else
				{
					throw;
				}
			}
			finally
			{
				readFile?.Close();
			}
		}

		public virtual byte[] ReadFromStorage(FileStream readChannel, long readFrom, int messageLength)
		{
			if (readFrom >= _mappedStartPosition)
			{
				lock (_mappedBufferLock)
				{
					if (readFrom >= _mappedStartPosition)
					{
						var originalPosition = _storageBuffer.Position;
						_storageBuffer.Position = readFrom - _mappedStartPosition;
						var message = new byte[messageLength];
						_storageBuffer.Read(message, 0, messageLength);
						_storageBuffer.Position = originalPosition;
						return message;
					}
				}
			}

			var result = new byte[messageLength];
			readChannel.Position = readFrom;
			readChannel.Read(result, 0, messageLength);
			return result;
		}

		private void RemapIndexMemoryMappedFile(long newSize)
		{
			try
			{
				_indexMappedBufferLock.EnterWriteLock();

				CloseIndexMemoryMappedFile();

				_indexMappedSize = Math.Min(newSize, int.MaxValue);
				_indexFile = MemoryMappedFile.CreateFromFile(_indexFileStream, null, PositionLength + _indexMappedSize,
					MemoryMappedFileAccess.ReadWrite, HandleInheritability.Inheritable, true);
				_indexLastPositionBuffer = _indexFile.CreateViewAccessor(0, PositionLength);
				_indexMessagesPositionsBuffer = _indexFile.CreateViewAccessor(PositionLength, _indexMappedSize);
			}
			finally
			{
				_indexMappedBufferLock.ExitWriteLock();
			}
		}

		protected virtual long AppendMessageToFile(long ticks, byte[] timestampFormatted, byte[] message,
			int offset, int length)
		{
			if (TimestampsInLogs)
			{
				if (timestampFormatted != null)
				{
					DateFormattedBuffer = timestampFormatted;
				}
				else
				{
					if (ticks != TimestampTicks)
					{
						TimestampTicks = ticks;
						DateFormattedBuffer = GetPrefixFormat(ticks);
					}
				}

				_recordLength = DateFormattedBuffer.Length + length + 1;
			}
			else
			{
				_recordLength = length + 1;
			}

			if (ChannelLength - (ChannelPosition + _recordLength) < Delta)
			{
				RelocateStorageMemoryMappedFile(Math.Max(StorageGrowSize, _recordLength) + 1);
			}

			lock (_mappedBufferLock)
			{
				if (TimestampsInLogs)
				{
					_storageBuffer.Write(DateFormattedBuffer, 0, DateFormattedBuffer.Length);
				}

				_storageBuffer.Write(message, offset, length);
				_storageBuffer.WriteByte(NewLine);
			}

			ChannelPosition += _recordLength;
			return _recordLength;
		}

		private void RelocateStorageMemoryMappedFile(long growSize)
		{
			lock (_mappedBufferLock)
			{
				CloseStorageMemoryMappedFile();

				var newFileLength = ChannelPosition + growSize;

				_storageFile = MemoryMappedFile.CreateFromFile(AccessFile, null, newFileLength,
					MemoryMappedFileAccess.ReadWrite, HandleInheritability.Inheritable, true);

				_storageBuffer = _storageFile.CreateViewStream(ChannelPosition, growSize);
				_storageBuffer.Position = 0;
				_mappedStartPosition = ChannelPosition;
				ChannelLength = newFileLength;
			}
		}

		/// <inheritdoc />
		protected override long GetNextSequenceNumber()
		{
			if (_indexFileStream.Length >= PositionLength)
			{
				var positionBytes = new byte[PositionLength];
				_indexFileStream.Read(positionBytes, 0, PositionLength);

				var positionBlock = new ByteBuffer(PositionLength);
				positionBlock.Position = 0;
				positionBlock.Put(positionBytes);

				var lastSeqNum = positionBlock.GetLongBe(8);
				if (_indexFileStream.Length >= PositionLength + lastSeqNum * IndexLength)
				{
					return lastSeqNum + 1;
				}
			}
			else if (_indexFileStream.Length == 0)
			{
				return 1;
			}

			_log.Warn("Index file " + FileName + _indexFileExtension +
					" is corrupt and will be truncated. Sequence won't be restored.");
			_indexFileStream.SetLength(PositionLength);

			var indexBlock = new ByteBuffer(8);
			indexBlock.PutLong(0, 0); // no need to convert endianness
			var bytesToWrite = indexBlock.ToArray();
			_indexFileStream.Write(bytesToWrite, 0, bytesToWrite.Length);
			_indexFileStream.Flush();

			return 1;
		}

		private void ResetIndexFile()
		{
			CloseIndexMemoryMappedFile();

			_indexFileStream.SetLength(PositionLength);
			RemapIndexMemoryMappedFile(IndexGrowSize);
			_indexLastPositionBuffer.Write(8, (long)0); // no need to convert endianness
		}

		public virtual long GetLastStoragePosition()
		{
			return _indexLastPositionBuffer.ReadLongBe(0);
		}

		private void CloseStorageMemoryMappedFile()
		{
			_storageBuffer?.Flush();
			_storageBuffer?.Dispose();
			_storageBuffer = null;

			_storageFile?.Dispose();
		}

		private void CloseIndexMemoryMappedFile()
		{
			_indexMessagesPositionsBuffer?.Flush();
			_indexMessagesPositionsBuffer?.Dispose();
			_indexMessagesPositionsBuffer = null;

			_indexLastPositionBuffer?.Flush();
			_indexLastPositionBuffer?.Dispose();
			_indexLastPositionBuffer = null;

			_indexFile?.Dispose();
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

		/// <inheritdoc />
		public override void Close()
		{
			try
			{
				_indexMappedBufferLock.EnterWriteLock();
				_log.Debug("Indexed storage closed");
				CloseStorageMemoryMappedFile();
				CloseIndexMemoryMappedFile();

				_indexFileStream?.Dispose();

				base.Close();
			}
			finally
			{
				_indexMappedBufferLock.ExitWriteLock();
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (Disposed)
			{
				return;
			}

			base.Dispose(disposing);

			if (disposing)
			{
				_indexMappedBufferLock.Dispose();
			}

			Disposed = true;
		}
	}
}
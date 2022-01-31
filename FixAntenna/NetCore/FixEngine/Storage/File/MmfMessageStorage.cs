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
using Epam.FixAntenna.NetCore.Configuration;

namespace Epam.FixAntenna.NetCore.FixEngine.Storage.File
{
	internal class MmfMessageStorage : FlatFileMessageStorage
	{
		private const int DefaultStorageGrowSize = 100 * 1024 * 1024;
		private static readonly ILog Log = LogFactory.GetLog(typeof(MmfMessageStorage));
		private readonly long _mmfStorageGrowSize;
		private MemoryMappedViewStream _fileViewStream;
		private MemoryMappedFile _mappedFile;
		private int _recordLength;

		public MmfMessageStorage(Config config) : base(config)
		{
			_mmfStorageGrowSize = Configuration.GetPropertyAsBytesLength(Config.MmfStorageGrowSize);
			if (_mmfStorageGrowSize <= 0)
			{
				Log.Warn("Parameter \"" + Config.MmfStorageGrowSize + "\" must be integer and not negative");
				_mmfStorageGrowSize = DefaultStorageGrowSize;
			}
		}

		/// <inheritdoc />
		public override long Initialize()
		{
			var seqNum = base.Initialize();

			if (ChannelLength > 0)
			{
				_mappedFile = MemoryMappedFile.CreateFromFile(AccessFile, null, ChannelLength, MemoryMappedFileAccess.ReadWrite,
					HandleInheritability.Inheritable, true);

				_fileViewStream = _mappedFile.CreateViewStream();
			}

			return seqNum;
		}

		/// <inheritdoc />
		protected internal override long AppendMessageInternal(long ticks, byte[] message, int offset, int length)
		{
			if (IsTimestampEnabled)
			{
				if (ticks != TimestampTicks)
				{
					TimestampTicks = ticks;
					DateFormattedBuffer = GetPrefixFormat(ticks);
				}

				_recordLength = DateFormattedBuffer.Length + length + 1;
			}
			else
			{
				_recordLength = length + 1;
			}

			if (ChannelLength - (ChannelPosition + _recordLength) < Delta)
			{
				RelocateBuffer(Math.Max(_mmfStorageGrowSize, _recordLength) + 1);
			}

			if (IsTimestampEnabled)
			{
				_fileViewStream.Write(DateFormattedBuffer, 0, DateFormattedBuffer.Length);
			}

			_fileViewStream.Write(message, offset, length);
			_fileViewStream.Write(NewLine, 0, NewLine.Length);

			ChannelPosition += _recordLength;
			return _recordLength;
		}

		private void RelocateBuffer(long growSize)
		{
			CloseBuffer();

			var newFileLength = ChannelPosition + growSize;
			_mappedFile = MemoryMappedFile.CreateFromFile(AccessFile, null, newFileLength,
				MemoryMappedFileAccess.ReadWrite, HandleInheritability.Inheritable, true);

			_fileViewStream = _mappedFile.CreateViewStream(ChannelPosition, growSize, MemoryMappedFileAccess.ReadWrite);
			_fileViewStream.Position = 0;
			ChannelLength = newFileLength;
		}

		/// <inheritdoc />
		public override void Close()
		{
			CloseBuffer();
			base.Close();
		}

		private void CloseBuffer()
		{
			_fileViewStream?.Dispose();

			if (_mappedFile != null)
			{
				try
				{
					_mappedFile.Dispose();
				}
				finally
				{
					_mappedFile = null;
				}
			}
		}
	}
}
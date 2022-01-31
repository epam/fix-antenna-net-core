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

using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Configuration;

namespace Epam.FixAntenna.NetCore.FixEngine.Storage.File
{
	internal class SlicedFileMessageStorage : FlatFileMessageStorage
	{
		/// <summary>
		/// The default maximum file size is 100MB.
		/// </summary>
		internal const long DefaultMaxFileSize = 100 * 1024 * 1024;

		private static readonly ILog Log = LogFactory.GetLog(typeof(SlicedFileMessageStorage));
		protected internal SlicedFileManager FileManager;
		protected internal long MaxFileSize;

		public SlicedFileMessageStorage(Config config) : base(config)
		{
			MaxFileSize = Configuration.GetPropertyAsBytesLength(Config.MaxStorageSliceSize);
			if (MaxFileSize <= 0)
			{
				Log.Warn("Parameter \"" + Config.MaxStorageSliceSize +
						"\" must be integer and greater than zero.");
				MaxFileSize = DefaultMaxFileSize;
			}
		}

		/// <inheritdoc />
		public override long Initialize()
		{
			FileManager = new SlicedFileManager(FileName);
			FileManager.Initialize();
			FileName = FileManager.GetFileName();
			return base.Initialize();
		}

		public virtual long GetMaxFileSize()
		{
			return MaxFileSize;
		}

		public virtual void SetMaxFileSize(long maxFileSize)
		{
			MaxFileSize = maxFileSize;
		}

		/// <inheritdoc />
		protected internal override long AppendMessageInternal(long timestamp, byte[] message, int offset, int length)
		{
			if (ChannelPosition > MaxFileSize)
			{
				NextChunk();
			}

			return base.AppendMessageInternal(timestamp, message, offset, length);
		}

		public virtual void NextChunk()
		{
			Close();
			FileName = FileManager.GetNextFileName();
			OpenStorageFile();
		}

		/// <inheritdoc />
		public override void BackupStorageFile(string fullPathToStorageFile, string fullPathToDestinationBackupFile)
		{
			var backupFileManager = new SlicedFileManager(fullPathToDestinationBackupFile);
			backupFileManager.Initialize();
			var last = FileManager.GetChunkId();
			for (var i = 1; i <= last; i++)
			{
				BackupFile(FileManager.GetFileName(i), backupFileManager.GetFileName(i));
			}
		}

		/// <inheritdoc />
		public override void DeleteStorageFile(string fullPathToStorageFile)
		{
			var last = FileManager.GetChunkId();
			for (var i = 1; i <= last; i++)
			{
				DeleteFile(FileManager.GetFileName(i));
			}
		}
	}
}
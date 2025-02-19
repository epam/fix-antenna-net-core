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
using System.Linq;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Storage.File;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Storage.File
{
	internal class MmfIndexedMessageStorageBackupTest : AbstractMessageStorageTest
	{
		public static readonly string IndexEnding = MmfIndexedMessageStorage.DefaultExt;

		[Test]
		public virtual void TestBackup()
		{
			UpMessageStorage();

			WriteSomeDataAndBackup();

			ValidateBackupResult();
		}

		public override AbstractFileMessageStorage GetInstanceMessageStorage()
		{
			return new MmfIndexedMessageStorage(ConfigurationAdapter.Configuration);
		}

		protected virtual void WriteSomeDataAndBackup()
		{
			// write some data
			WriteDummyMessageToStorage();
			WriteDummyMessageToStorage();

			var sessionParameters = new SessionParameters();
			sessionParameters.SenderCompId = Sender;
			sessionParameters.TargetCompId = Target;
			sessionParameters.Configuration = ConfigurationAdapter.Configuration;
			MessageStorage.BackupStorage(sessionParameters);
		}

		protected virtual void ValidateBackupResult()
		{
			var helper = new FileHelper(this);
			ClassicAssert.IsTrue(helper.GetBackupStorageFileSize(Sender) > 0);
			ClassicAssert.IsTrue(helper.GetBackupStorageIndexFileSize(Sender) > MmfIndexedMessageStorage.PositionLength);

			ClassicAssert.AreEqual(0, helper.GetStorageFileSize(Sender));
			var indLength = helper.GetStorageIndexFileSize(Sender);
			ClassicAssert.IsTrue((indLength == 0) | (indLength == MmfIndexedMessageStorage.PositionLength));
		}

		[Test]
		public virtual void TestDelete()
		{
			ConfigurationAdapter.Configuration
				.SetProperty(Config.StorageCleanupMode, StorageCleanupMode.Delete.ToString());

			UpMessageStorage();
			WriteSomeDataAndBackup();

			ValidateDeleteResult();
		}

		[Test]
		public virtual void TestBackupAndOpenStorageAgain()
		{
			ConfigurationAdapter.Configuration
				.SetProperty(Config.StorageCleanupMode, StorageCleanupMode.Backup.ToString());
			UpMessageStorage();
			WriteSomeDataAndBackup();
			ValidateBackupResult();
			UpMessageStorage();
			ClassicAssert.AreEqual(1L, GetInitializedSeqId());
		}

		[Test]
		public virtual void TestBackupAndWrite()
		{
			ConfigurationAdapter.Configuration
				.SetProperty(Config.StorageCleanupMode, StorageCleanupMode.Backup.ToString());
			UpMessageStorage();
			WriteSomeDataAndBackup();

			ValidateBackupResult();

			WriteDummyMessageToStorage();

			var helper = new FileHelper(this);
			ClassicAssert.IsTrue(helper.GetStorageFileSize(Sender) > 0);
		}

		public virtual void ValidateDeleteResult()
		{
			var helper = new FileHelper(this);
			ClassicAssert.AreEqual(0, helper.GetStorageFileSize(Sender));
		}

		private class FileHelper
		{
			internal const int OverlapLength = 20;
			internal const int ReadBufferLength = 1024;

			private readonly MmfIndexedMessageStorageBackupTest _outerInstance;

			public FileHelper(MmfIndexedMessageStorageBackupTest outerInstance)
			{
				_outerInstance = outerInstance;
			}

			public virtual long GetStorageFileSize(string sender)
			{
				return GetFileSize(_outerInstance.ConfigurationAdapter.StorageDirectory, sender + "-", ".out");
			}

			public virtual long GetStorageIndexFileSize(string sender)
			{
				return GetFileSize(_outerInstance.ConfigurationAdapter.StorageDirectory, sender + "-",
					IndexEnding);
			}

			public virtual long GetBackupStorageFileSize(string sender)
			{
				return GetFileSize(_outerInstance.ConfigurationAdapter.BackupStorageDirectory, sender + "-",
					".out");
			}

			public virtual long GetBackupStorageIndexFileSize(string sender)
			{
				return GetFileSize(_outerInstance.ConfigurationAdapter.BackupStorageDirectory, sender + "-",
					IndexEnding);
			}

			public virtual long GetFileSize(string dir, string prefix, string ending)
			{
				var dirFile = new DirectoryInfo(dir);
				var files = dirFile.GetFiles()
					.Where(x => x.Name.EndsWith(ending, StringComparison.Ordinal) &&
								x.Name.StartsWith(prefix, StringComparison.Ordinal))
					.Select(x => x.Name)
					.ToList();
				ClassicAssert.AreEqual(1, files.Count);
				return GetLastChannelPosition(dir + Path.DirectorySeparatorChar + files[0]);
			}

			protected virtual long GetLastChannelPosition(string fileName)
			{
				long position = 0;
				var buffer = new byte[ReadBufferLength];
				try
				{
					using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					{
						var bufferEndPosition = stream.Length;
						var attempt = 0;
						var found = false;
						do
						{
							var seekPosition = Math.Max(0, bufferEndPosition - ReadBufferLength);
							stream.Seek(seekPosition, SeekOrigin.Begin);
#if NET48
							var numberOfBytesRead = stream.Read(buffer, 0, buffer.Length);
#else
							var numberOfBytesRead = stream.Read(buffer);
#endif
							var index = numberOfBytesRead - 1;
							while (index > 0)
							{
								if (buffer[index] != 0)
								{
									position = seekPosition + index + 1;
									found = true;
									break;
								}

								index--;
							}
						} while (!found && (bufferEndPosition =
							stream.Length - (ReadBufferLength - OverlapLength) * ++attempt) > 0);

						stream.Seek(0, SeekOrigin.Begin);
						var content = new byte[(int)position];
#if NET48
						stream.Read(content, 0, content.Length);
#else
						stream.Read(content);
#endif
					}
				}
				catch (Exception)
				{
					return position;
				}

				return position;
			}
		}
	}
}
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
	internal class MmfMessageStorageBackupTest : AbstractMessageStorageTest
	{
		[Test]
		public virtual void TestBackup()
		{
			UpMessageStorage();

			WriteSomeDataAndBackup();

			ValidateBackupResult();
		}

		public override AbstractFileMessageStorage GetInstanceMessageStorage()
		{
			return new MmfMessageStorage(ConfigurationAdapter.Configuration);
		}

		public virtual void WriteSomeDataAndBackup()
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

		public virtual void ValidateBackupResult()
		{
			var helper = new FileHelper(this);
			ClassicAssert.AreEqual(0, helper.GetStorageFileSize(Sender));

			ClassicAssert.IsTrue(helper.CountFilesInBackStorage() > 0);
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

		internal class FileHelper
		{
			private readonly MmfMessageStorageBackupTest _outerInstance;

			public FileHelper(MmfMessageStorageBackupTest outerInstance)
			{
				_outerInstance = outerInstance;
			}

			public virtual long GetStorageFileSize(string sender)
			{
				var dir = new DirectoryInfo(_outerInstance.ConfigurationAdapter.StorageDirectory);
				var files = dir.GetFiles()
					.Where(x => x.Name.EndsWith(".out", StringComparison.Ordinal) &&
								x.Name.StartsWith(sender + "-", StringComparison.Ordinal))
					.ToList();
				var file = new FileInfo(Path.Combine(_outerInstance.ConfigurationAdapter.StorageDirectory,
					files[0].Name));
				return file.Length;
			}

			public virtual long CountFilesInBackStorage()
			{
				var directoryName = _outerInstance.ConfigurationAdapter.BackupStorageDirectory;
				var dir = new DirectoryInfo(directoryName);
				var files = dir.GetFiles()
					.Where(x => x.Name.EndsWith(".out", StringComparison.Ordinal) ||
								x.Name.EndsWith(".idx", StringComparison.Ordinal))
					.ToList();

				var file = new FileInfo(Path.Combine(directoryName, files[0].Name));
				return file.Length;
			}
		}
	}
}
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

using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Storage;
using Epam.FixAntenna.NetCore.FixEngine.Storage.File;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Storage.File
{
	internal class IndexedMessageStorageBackupTest : FlatFileMessageStorageBackupTest
	{
		public override AbstractFileMessageStorage GetInstanceMessageStorage()
		{
			return new IndexedMessageStorage(ConfigurationAdapter.Configuration);
		}

		[Test]
		public override void TestBackup()
		{
			ConfigurationAdapter.Configuration
				.SetProperty(Config.StorageCleanupMode, StorageCleanupMode.Backup.ToString());

			UpMessageStorage();
			WriteSomeDataAndBackup();

			ValidateBackupResult();
		}

		[Test]
		public override void TestDelete()
		{
			ConfigurationAdapter.Configuration
				.SetProperty(Config.StorageCleanupMode, StorageCleanupMode.Delete.ToString());

			UpMessageStorage();
			WriteSomeDataAndBackup();

			ValidateDeleteResult();
		}

		[Test]
		public override void TestBackupAndOpenStorageAgain()
		{
			ConfigurationAdapter.Configuration
				.SetProperty(Config.StorageCleanupMode, StorageCleanupMode.Backup.ToString());
			UpMessageStorage();

			WriteSomeDataAndBackup();
			ValidateBackupResult();

			InitializeStorageAgain();

			RetrieveMessageFromStorageExpectedNoneMessages();
		}

		private void InitializeStorageAgain()
		{
			UpMessageStorage();
		}

		private void RetrieveMessageFromStorageExpectedNoneMessages()
		{
			var listener = new MessageStorageListener();
			MessageStorage.RetrieveMessages(1, 10, listener, true);

			Assert.IsTrue(listener.Count == 0);
		}

		private class MessageStorageListener : IMessageStorageListener
		{
			internal int Count { get; private set; }

			public void OnMessage(byte[] message)
			{
				Count++;
			}
		}
	}
}
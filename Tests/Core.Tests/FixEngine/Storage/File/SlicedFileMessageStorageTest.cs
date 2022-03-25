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
using Epam.FixAntenna.NetCore.Helpers;

using NUnit.Framework;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Epam.FixAntenna.NetCore.FixEngine.Storage.File
{
	internal class SlicedFileMessageStorageTest : AbstractMessageStorageTest
	{
		private const string FileEnding = "-.*\\.[0-9]{1,10}\\.[a-zA-Z]{1,4}$";

		public override AbstractFileMessageStorage GetInstanceMessageStorage()
		{
			return new SlicedFileMessageStorage(ConfigurationAdapter.Configuration);
		}

		[Test]
		public virtual void TestWrite1KMsgWith1KbMaxStorage()
		{
			ConfigurationAdapter.Configuration.SetProperty(Config.MaxStorageSliceSize, "1Kb");
			UpMessageStorage();
			var msgNum = 1000;
			var messages = new string[msgNum];
			for (var i = 0; i < msgNum; i++)
			{
				var arrMsg = GetNextMessage().AsByteArray();
				MessageStorage.AppendMessage(arrMsg);
				messages[i] = StringHelper.NewString(arrMsg);
			}

			AssertEqualsMessages(messages);
		}

		[Test]
		public virtual void TestBackup()
		{
			UpMessageStorage();

			WriteSomeDataAndBackup();

			ValidateBackupResult();
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
			Assert.AreEqual(1L, GetInitializedSeqId());
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
			Assert.AreEqual(0, helper.GetStorageFileSize(Sender));

			Assert.IsTrue(helper.CountFilesInBackStorage(Sender) > 0);
		}

		public virtual void ValidateDeleteResult()
		{
			var helper = new FileHelper(this);
			Assert.AreEqual(0, helper.GetStorageFileSize(Sender));

			Assert.IsTrue(helper.CountFilesInBackStorage(Sender) <= 0);
		}

		protected override void AssertEqualsMessages(params string[] expectMessages)
		{
			var dir = new DirectoryInfo(ConfigurationAdapter.StorageDirectory);
			var listFiles = dir.GetFiles().Where(x => ShouldFileBeSelected(x.Name, Sender)).Select(x => x.Name)
				.ToList();

			Assert.IsTrue(listFiles.Count >= 1, "Storage files not found");
			var sortedList = SortFiles(listFiles);
			StreamReader br = null;
			var fileCount = 0;

			try
			{
				br = OpenStream(sortedList[fileCount++]);
				foreach (var expMsg in expectMessages)
				{
					var actualMsg = br.ReadLine();
					if (actualMsg == null && fileCount != sortedList.Count)
					{
						// try open next chunk and read line from it
						br.Close();
						br = OpenStream(sortedList[fileCount++]);
						actualMsg = br.ReadLine();
					}

					Assert.IsNotNull("Not all messages are written to the storage.", actualMsg);
					Assert.AreEqual(expMsg, actualMsg.Substring(24)); // get fix message without timestamp
				}
			}
			finally
			{
				br?.Close();
			}
		}

		public virtual StreamReader OpenStream(string fileName)
		{
			FileStream stream = null;
			StreamReader reader = null;
			try
			{
				stream = new FileStream(Path.Combine(ConfigurationAdapter.StorageDirectory, fileName),
					FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				reader = new StreamReader(stream);
			}
			catch (IOException)
			{
				reader?.Close();
				stream?.Close();
				throw;
			}

			return reader;
		}

		public virtual IList<string> SortFiles(IList<string> list)
		{
			return list.OrderBy(x => x, new Comparator()).ToList();
		}

		private static bool ShouldFileBeSelected(string fileName, string sender)
		{
			return Regex.IsMatch(fileName, "^" + sender + FileEnding);
		}

		private class Comparator : IComparer<string>
		{
			public int Compare(string o1, string o2)
			{
				if (ReferenceEquals(o1, o2))
				{
					return 0;
				}

				return GetNumber(o1.Substring(0, o1.LastIndexOf('.'))) -
						GetNumber(o2.Substring(0, o2.LastIndexOf('.')));
			}

			public int GetNumber(string name)
			{
				var length = 0;
				var lastCharPos = name.Length - 1;
				var c = name[lastCharPos - length];
				while (name.Length >= length && c >= '0' && c <= '9')
				{
					c = name[lastCharPos - ++length];
				}

				return int.Parse(name.Substring(name.Length - length));
			}
		}

		private class FileHelper
		{
			private readonly SlicedFileMessageStorageTest _outerInstance;

			public FileHelper(SlicedFileMessageStorageTest outerInstance)
			{
				_outerInstance = outerInstance;
			}

			public virtual long GetStorageFileSize(string sender)
			{
				var dir = new DirectoryInfo(_outerInstance.ConfigurationAdapter.StorageDirectory);
				var files = dir.GetFiles().Where(x => ShouldFileBeSelected(x.Name, sender)).ToList();
				var file = new FileInfo(Path.Combine(_outerInstance.ConfigurationAdapter.StorageDirectory,
					files[0].Name));
				return file.Length;
			}

			public virtual int CountFilesInBackStorage(string sender)
			{
				var directoryName = _outerInstance.ConfigurationAdapter.BackupStorageDirectory;
				var dir = new DirectoryInfo(directoryName);
				var files = dir.GetFiles().Where(x => ShouldFileBeSelected(x.Name, sender)).ToList();
				return files.Count;
			}
		}
	}
}
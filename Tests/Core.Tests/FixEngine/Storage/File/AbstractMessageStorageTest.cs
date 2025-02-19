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

using System.IO;
using Epam.FixAntenna.TestUtils;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.FixEngine.Storage;
using Epam.FixAntenna.NetCore.FixEngine.Storage.File;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Storage.File
{
	internal abstract class AbstractMessageStorageTest
	{
		private const string SenderDefaultValue = "AFB";
		private const string TargetDefaultValue = "BFB";
		private const int DefaultSequenceNumber = 1;

		private long _seqNum = DefaultSequenceNumber;
		protected internal ConfigurationAdapter ConfigurationAdapter;
		protected internal ILogFileLocator FileLocator;
		protected internal FileNameHelper FileNameHelper;

		protected internal AbstractFileMessageStorage MessageStorage;
		protected internal string Sender = SenderDefaultValue;
		protected internal long SeqId;
		protected internal string Target = TargetDefaultValue;

		[SetUp]
		public virtual void SetUp()
		{
			ConfigurationHelper.StoreGlobalConfig();
			Config.GlobalConfiguration.SetProperty(Config.StorageCleanupMode, "Backup");
			ClearLogs();
			FileNameHelper = new FileNameHelper(Sender, Target);
			FileLocator = new LogFileLocator(this);
			ConfigurationAdapter = new ConfigurationAdapter(Config.GlobalConfiguration);
			new DirectoryInfo(ConfigurationAdapter.BackupStorageDirectory).Create();
		}

		[TearDown]
		public virtual void TearDown()
		{
			CloseStorage();
			ClearLogs();
			ConfigurationHelper.RestoreGlobalConfig();
		}

		public virtual void ClearLogs()
		{
			var logsCleaner = new LogsCleaner();
			logsCleaner.Clean("./logs");
			logsCleaner.Clean("./logs/backup");
		}

		public virtual void UpMessageStorage()
		{
			CloseStorage();
			InitStorage();
		}

		public abstract AbstractFileMessageStorage GetInstanceMessageStorage();

		public virtual void InitStorage()
		{
			MessageStorage = GetInstanceMessageStorage();
			MessageStorage.FileName = FileNameHelper.GetStorageFileName(ConfigurationAdapter.StorageDirectory);
			MessageStorage.SetBackupFileLocator(FileLocator);
			SeqId = MessageStorage.Initialize();
			_seqNum = DefaultSequenceNumber;
		}

		public virtual void CloseStorage()
		{
			MessageStorage?.Close();
		}

		public virtual long GetInitializedSeqId()
		{
			return SeqId;
		}

		public virtual void WriteDummyMessageToStorage()
		{
			var message = GetNextMessage();
			MessageStorage.AppendMessage(message.AsByteArray());
		}

		public virtual FixMessage GetNextMessage()
		{
			var message = RawFixUtil.GetFixMessage("8=FIX.4.2\u000134=1\u000158=Hello World!!!\u0001".AsByteArray());
			message.Set(34, _seqNum++);
			return message;
		}

		protected virtual void ClassicAssertEqualsMessages(params string[] expectMessages)
		{
			var storageFileName = FileNameHelper.GetStorageFileName(ConfigurationAdapter.StorageDirectory);
			using (var inputStream =
				new FileStream(storageFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			using (var br = new StreamReader(inputStream))
			{
				foreach (var expMsg in expectMessages)
				{
					var actualMsg = br.ReadLine();
					ClassicAssert.AreEqual(expMsg,
						actualMsg.Substring(MessageStorage.CalculateFormatLength())); // get fix message without timestamp
				}
			}
		}

		protected virtual void ClassicAssertEqualsMessagesWithTimestamps(string[] expectMessages, string expectedTimestamp)
		{
			var storageFileName = FileNameHelper.GetStorageFileName(ConfigurationAdapter.StorageDirectory);
			using (var inputStream =
				new FileStream(storageFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			using (var br = new StreamReader(inputStream))
			{
				foreach (var expMsg in expectMessages)
				{
					var actualMsgRecord = br.ReadLine();
					var actualMsg = actualMsgRecord.Substring(expectedTimestamp.Length);
					var actualTimestamp = actualMsgRecord.Substring(0, actualMsgRecord.Length - actualMsg.Length);
					ClassicAssert.AreEqual(expMsg, actualMsg);
					ClassicAssert.AreEqual(actualTimestamp, expectedTimestamp);
				}
			}
		}

		private class LogFileLocator : ILogFileLocator
		{
			private readonly AbstractMessageStorageTest _outerInstance;

			public LogFileLocator(AbstractMessageStorageTest outerInstance)
			{
				_outerInstance = outerInstance;
			}

			public string GetFileName(SessionParameters sessionParameters)
			{
				var fileNameHelper = new FileNameHelper(_outerInstance.Sender,
					_outerInstance.Target + DateTimeHelper.CurrentMilliseconds);
				return fileNameHelper.GetStorageFileName(
					_outerInstance.ConfigurationAdapter.BackupStorageDirectory);
			}
		}
	}
}
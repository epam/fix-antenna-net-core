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
using System.Text;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Storage.File;
using Epam.FixAntenna.NetCore.Helpers;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Storage.File
{
	internal class MmfMessageStorageTest : AbstractMessageStorageTest
	{
		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			UpMessageStorage();
		}

		public override AbstractFileMessageStorage GetInstanceMessageStorage()
		{
			return new MmfMessageStorage(ConfigurationAdapter.Configuration);
		}

		[Test]
		public virtual void TestWrite1KMessages()
		{
			ConfigurationAdapter.Configuration
				.SetProperty(Config.MmfStorageGrowSize, Convert.ToString(1000));
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
		public virtual void TestWriteLongMessage()
		{
			ConfigurationAdapter.Configuration
				.SetProperty(Config.MmfStorageGrowSize, Convert.ToString(1000));
			UpMessageStorage();
			var longMsgBuilder = new StringBuilder("34=1\u0001");
			var size = 1024 * 1024 + 10;
			while (longMsgBuilder.Length < size)
			{
				longMsgBuilder.Append("58=Hello World!!!\u0001");
			}

			var longMsg = longMsgBuilder.ToString();
			MessageStorage.AppendMessage(longMsg.AsByteArray());
			var nextMsg = GetNextMessage().ToString();
			MessageStorage.AppendMessage(nextMsg.AsByteArray());
			AssertEqualsMessages(longMsg, nextMsg);
		}

		[Test]
		public virtual void TestWriteMessageWithMicroTimestamp()
		{
			ConfigurationAdapter.Configuration.SetProperty(Config.TimestampsPrecisionInLogs,
				TimestampPrecision.Micro.ToString());
			UpMessageStorage();

			var arrMsg = GetNextMessage().AsByteArray();
			MessageStorage.AppendMessage(arrMsg);

			AssertEqualsMessages(StringHelper.NewString(arrMsg));
		}

		[Test]
		public virtual void TestWriteMessageWithNanoTimestamp()
		{
			ConfigurationAdapter.Configuration.SetProperty(Config.TimestampsPrecisionInLogs,
				TimestampPrecision.Nano.ToString());
			UpMessageStorage();

			var arrMsg = GetNextMessage().AsByteArray();
			MessageStorage.AppendMessage(arrMsg);

			AssertEqualsMessages(StringHelper.NewString(arrMsg));
		}

		[Test]
		public virtual void ItShouldNotIncreaseFilesSizeAfterReopening()
		{
			ConfigurationAdapter.Configuration.SetProperty(Config.MmfStorageGrowSize, "1kb");
			UpMessageStorage();

			MessageStorage.AppendMessage("35=A\u000134=1\u000110=001\u0001".AsByteArray());
			CloseStorage();

			var storageFile = new FileInfo(MessageStorage.FileName);
			var storageFileLengthAfterCreating = storageFile.Length;

			for (var i = 0; i < 10; i++)
			{
				UpMessageStorage();
				CloseStorage();
			}

			Assert.AreEqual(storageFileLengthAfterCreating, storageFile.Length);
		}

		[Test]
		public void ReopenStorageTest()
		{
			void AppendMessage() => MessageStorage.AppendMessage("35=A\u000134=1\u000110=001\u0001".AsByteArray());

			AppendMessage();
			UpMessageStorage(); // close and re-open storage

			Assert.That(AppendMessage, Throws.Nothing, "Storage not initialized correctly", null);
		}
	}
}
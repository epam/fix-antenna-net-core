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

using System.Text;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Storage;
using Epam.FixAntenna.NetCore.FixEngine.Storage.File;
using Epam.FixAntenna.NetCore.Helpers;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Storage.File
{
	internal class SlicedIndexedMessageStorageTest : SlicedFileMessageStorageTest
	{
		public override AbstractFileMessageStorage GetInstanceMessageStorage()
		{
			return new SlicedIndexedMessageStorage(ConfigurationAdapter.Configuration);
		}

		[Test]
		public virtual void TestRetrieveAfterRestart()
		{
			ConfigurationAdapter.Configuration.SetProperty(Config.MaxStorageSliceSize, "1Kb");
			UpMessageStorage();
			var msgNum = 100;
			for (var i = 1; i < msgNum; i++)
			{
				var arrMsg = GetNextMessage().AsByteArray();
				MessageStorage.AppendMessage(arrMsg);
			}

			CloseStorage();
			UpMessageStorage();
			Assert.AreEqual(msgNum, GetInitializedSeqId());
		}

		[Test]
		public virtual void AppendAndReadMessageMicroTimestamp()
		{
			ConfigurationAdapter.Configuration.SetProperty(Config.TimestampsPrecisionInLogs, "micro");
			UpMessageStorage();

			Assert.AreEqual(1L, GetInitializedSeqId());

			var msg = GetNextMessage();
			MessageStorage.AppendMessageInternal(DateTimeHelper.CurrentTicks, msg.AsByteArray(), 0,
				msg.AsByteArray().Length);
			Assert.AreEqual(msg.AsByteArray(), MessageStorage.RetrieveMessage(msg.MsgSeqNumber),
				"Assert " + msg.MsgSeqNumber + " seqNumber");
		}

		[Test]
		public virtual void AppendAndReadMessageNanoTimestamp()
		{
			ConfigurationAdapter.Configuration.SetProperty(Config.TimestampsPrecisionInLogs, "nano");
			UpMessageStorage();

			Assert.AreEqual(1L, GetInitializedSeqId());

			var msg = GetNextMessage();
			MessageStorage.AppendMessageInternal(DateTimeHelper.CurrentTicks, msg.AsByteArray(), 0,
				msg.AsByteArray().Length);
			Assert.AreEqual(msg.AsByteArray(), MessageStorage.RetrieveMessage(msg.MsgSeqNumber),
				"Assert " + msg.MsgSeqNumber + " seqNumber");
		}

		[Test]
		public virtual void RestoreMessagesWithGap()
		{
			UpMessageStorage();
			Assert.AreEqual(1L, GetInitializedSeqId());

			var msg1 = GetNextMessage().AsByteArray();
			GetNextMessage(); // make gap in 1 message
			var msg3 = GetNextMessage().AsByteArray();
			MessageStorage.AppendMessageInternal(DateTimeHelper.CurrentTicks, msg1, 0, msg1.Length);
			MessageStorage.AppendMessageInternal(DateTimeHelper.CurrentTicks, msg3, 0, msg3.Length);
			var restoredMsg = new byte[3][];
			MessageStorage.RetrieveMessages(1, 3, new MessageStorageListener(restoredMsg), true);

			Assert.IsNotNull(restoredMsg[0], "First message");
			Assert.AreEqual(msg1, restoredMsg[0], "Assert 1 seqNumber");
			Assert.IsTrue(restoredMsg[1] == null || restoredMsg[1].Length == 0, "First message");
			Assert.IsNotNull(restoredMsg[2], "First message");
			Assert.AreEqual(msg3, restoredMsg[2], "Assert 3 seqNumber");
		}

		[Test]
		public virtual void TestWrite1KMsgWith1KbMaxStorageAndRetrieve()
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

			var listener = new MessageStorageListener2(messages);
			MessageStorage.RetrieveMessages(1, msgNum + 1, listener, true);
			Assert.AreEqual(msgNum, listener.Counter);
			AssertEqualsMessages(messages);
		}

		[Test]
		public virtual void TestWriteMsgLongerThenMaxSizeSlice()
		{
			ConfigurationAdapter.Configuration.SetProperty(Config.MaxStorageSliceSize, "1Kb");
			var longMsgBuilder = new StringBuilder("34=1\u0001");
			var size = 1024 * 1024 + 10;
			while (longMsgBuilder.Length < size)
			{
				longMsgBuilder.Append("58=Hello World!!!\u0001");
			}

			UpMessageStorage();
			var longMsg = longMsgBuilder.ToString();
			var msgNum = 5;
			var array = new string[msgNum];
			for (var i = 0; i < msgNum; i++)
			{
				array[i] = longMsg;
				MessageStorage.AppendMessage(array[i].AsByteArray());
			}

			AssertEqualsMessages(array);
		}

		private class MessageStorageListener : IMessageStorageListener
		{
			private readonly byte[][] _restoredMessages;

			public MessageStorageListener(byte[][] restoredMessages)
			{
				_restoredMessages = restoredMessages;
			}

			internal int Counter { get; set; }

			public void OnMessage(byte[] message)
			{
				_restoredMessages[Counter++] = message;
			}
		}

		private class MessageStorageListener2 : IMessageStorageListener
		{
			private readonly string[] _messages;

			public MessageStorageListener2(string[] messages)
			{
				_messages = messages;
			}

			internal int Counter { get; private set; }

			public void OnMessage(byte[] message)
			{
				var expected = _messages[Counter++];
				var actual = StringHelper.NewString(message);
				Assert.AreEqual(expected, actual);
			}
		}
	}
}
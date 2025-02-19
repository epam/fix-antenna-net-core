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
using System.Collections.Generic;
using System.Threading.Tasks;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Storage;
using Epam.FixAntenna.NetCore.FixEngine.Storage.File;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Storage.File
{
	internal class IndexedMessageStorageTest : FlatFileMessageStorageTest
	{
		private readonly byte[] _batchTestMessage =
			"35=h\u000134=1\u000110=001\u000135=h\u000134=2\u000110=001\u000135=h\u000134=3\u000110=001\u0001"
				.AsByteArray();

		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			UpMessageStorage();
		}

		public override AbstractFileMessageStorage GetInstanceMessageStorage()
		{
			return new IndexedMessageStorage(ConfigurationAdapter.Configuration);
		}

		[Test]
		public virtual void AppendOneMessage()
		{
			ClassicAssert.AreEqual(1L, GetInitializedSeqId());

			var msg = GetNextMessage();
			MessageStorage.AppendMessageInternal(DateTimeHelper.CurrentTicks, msg.AsByteArray(), 0,
				msg.AsByteArray().Length);
		}

		[Test]
		public virtual void AppendTreeMessages()
		{
			ClassicAssert.AreEqual(1L, GetInitializedSeqId());

			var msg = GetNextMessage();
			MessageStorage.AppendMessageInternal(DateTimeHelper.CurrentTicks, msg.AsByteArray(), 0,
				msg.AsByteArray().Length);

			msg = GetNextMessage();
			MessageStorage.AppendMessageInternal(DateTimeHelper.CurrentTicks, msg.AsByteArray(), 0,
				msg.AsByteArray().Length);

			msg = GetNextMessage();
			MessageStorage.AppendMessageInternal(DateTimeHelper.CurrentTicks, msg.AsByteArray(), 0,
				msg.AsByteArray().Length);

			UpMessageStorage();
			ClassicAssert.AreEqual(4L, GetInitializedSeqId());
		}

		[Test]
		public virtual void AppendBatchMessages()
		{
			ClassicAssert.AreEqual(1L, GetInitializedSeqId());

			MessageStorage.AppendMessageInternal(DateTimeHelper.CurrentTicks, _batchTestMessage, 0,
				_batchTestMessage.Length / 3);
			MessageStorage.AppendMessageInternal(DateTimeHelper.CurrentTicks, _batchTestMessage,
				_batchTestMessage.Length / 3, 2 * _batchTestMessage.Length / 3);
			MessageStorage.AppendMessageInternal(DateTimeHelper.CurrentTicks, _batchTestMessage,
				2 * _batchTestMessage.Length / 3, _batchTestMessage.Length / 3);

			UpMessageStorage();
			ClassicAssert.AreEqual(4L, GetInitializedSeqId());
		}

		[Test]
		public virtual void RestoreMessagesWithGap()
		{
			ClassicAssert.AreEqual(1L, GetInitializedSeqId());

			var msg1 = GetNextMessage().AsByteArray();
			GetNextMessage(); // make gap in 1 message
			var msg3 = GetNextMessage().AsByteArray();
			MessageStorage.AppendMessageInternal(DateTimeHelper.CurrentTicks, msg1, 0, msg1.Length);
			MessageStorage.AppendMessageInternal(DateTimeHelper.CurrentTicks, msg3, 0, msg3.Length);
			var restoredMsg = new byte[3][];
			MessageStorage.RetrieveMessages(1, 3, new MessageStorageListener(restoredMsg), true);

			ClassicAssert.IsNotNull(restoredMsg[0], "First message");
			ClassicAssert.AreEqual(msg1, restoredMsg[0], "ClassicAssert 1 seqNumber");
			ClassicAssert.IsTrue(restoredMsg[1] == null || restoredMsg[1].Length == 0, "First message");
			ClassicAssert.IsNotNull(restoredMsg[2], "First message");
			ClassicAssert.AreEqual(msg3, restoredMsg[2], "ClassicAssert 3 seqNumber");
		}

		[Test]
		public virtual void AppendAndReadMessage()
		{
			ClassicAssert.AreEqual(1L, GetInitializedSeqId());

			var msg = GetNextMessage();
			MessageStorage.AppendMessageInternal(DateTimeHelper.CurrentTicks, msg.AsByteArray(), 0,
				msg.AsByteArray().Length);
			ClassicAssert.AreEqual(msg.AsByteArray(), MessageStorage.RetrieveMessage(msg.MsgSeqNumber),
				"ClassicAssert " + msg.MsgSeqNumber + " seqNumber");
		}

		[Test]
		public virtual void AppendAndReadMessageMicroTimestamp()
		{
			ConfigurationAdapter.Configuration.SetProperty(Config.TimestampsPrecisionInLogs, "micro");
			UpMessageStorage();

			ClassicAssert.AreEqual(1L, GetInitializedSeqId());

			var msg = GetNextMessage();
			MessageStorage.AppendMessageInternal(DateTimeHelper.CurrentTicks, msg.AsByteArray(), 0,
				msg.AsByteArray().Length);
			ClassicAssert.AreEqual(msg.AsByteArray(), MessageStorage.RetrieveMessage(msg.MsgSeqNumber),
				"ClassicAssert " + msg.MsgSeqNumber + " seqNumber");
		}

		[Test]
		public virtual void AppendAndReadMessageNanoTimestamp()
		{
			ConfigurationAdapter.Configuration.SetProperty(Config.TimestampsPrecisionInLogs, "nano");
			UpMessageStorage();

			ClassicAssert.AreEqual(1L, GetInitializedSeqId());

			var msg = GetNextMessage();
			MessageStorage.AppendMessageInternal(DateTimeHelper.CurrentTicks, msg.AsByteArray(), 0,
				msg.AsByteArray().Length);
			ClassicAssert.AreEqual(msg.AsByteArray(), MessageStorage.RetrieveMessage(msg.MsgSeqNumber),
				"ClassicAssert " + msg.MsgSeqNumber + " seqNumber");
		}

		[Test]
		public virtual void AppendAndReadMessagesParallel()
		{
			ClassicAssert.AreEqual(1L, GetInitializedSeqId());
			const int recordNum = 10000;
			const int readNum = 100;
			// seqNum start from 1 and end at recordNum
			var tasks = new List<Task>();
			for (var i = 1; i < recordNum + 1; i++)
			{
				var msg = GetNextMessage().AsByteArray();
				MessageStorage.AppendMessage(msg, 0, msg.Length);
				if (i % (recordNum / readNum) == 0)
				{
					long seqNum = i;
					var task = Task.Run(() =>
					{
						var readSeqNum = Math.Min(seqNum, recordNum);
						var restoredMsg = MessageStorage.RetrieveMessage(readSeqNum);
						var restoredSeqNum = RawFixUtil.GetSequenceNumber(restoredMsg, 0, restoredMsg.Length);
						ClassicAssert.AreEqual(readSeqNum, restoredSeqNum, "ClassicAssert " + readSeqNum + " message");
					});

					tasks.Add(task);
				}
			}

			Task.WaitAll(tasks.ToArray());
		}

		private class MessageStorageListener : IMessageStorageListener
		{
			private readonly byte[][] _restoredMsg;

			internal int Counter;

			public MessageStorageListener(byte[][] restoredMsg)
			{
				_restoredMsg = restoredMsg;
			}

			public void OnMessage(byte[] message)
			{
				_restoredMsg[Counter++] = message;
			}
		}
	}
}
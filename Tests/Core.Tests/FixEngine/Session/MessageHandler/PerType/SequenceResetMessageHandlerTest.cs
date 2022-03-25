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

using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.PerType;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.TestUtils;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.PerType
{
	[TestFixture]
	internal class SequenceResetMessageHandlerTest
	{
		private SequenceResetMessageHandler _sequenceResetMessageHandler;
		private AbstractFixSessionHelper _fixSession;

		[SetUp]
		public virtual void Init()
		{
			_fixSession = new AbstractFixSessionHelper();
			_fixSession.Init();
			_sequenceResetMessageHandler = new SequenceResetMessageHandler();
			_sequenceResetMessageHandler.Session = _fixSession;
		}

		[TearDown]
		public virtual void TearDown()
		{
			_fixSession.Dispose();
			ClearLogs();
			Assert.IsTrue(ClearLogs(), "Can't Clean logs after tests");
		}

		public virtual bool ClearLogs()
		{
			var logsCleaner = new LogsCleaner();
			return logsCleaner.Clean("./logs") && logsCleaner.Clean("./logs/backup");
		}

		/// <summary>
		/// b. Receive Sequence Reset (Gap Fill) message with NewSeqNo > MsgSeqNum and MsgSeqNum = to expected sequence number
		/// ->
		/// Set next expected sequence number = NewSeqNo
		/// </summary>
		[Test]
		public virtual void ShouldResetSequence()
		{
			_fixSession.InSeqNum = 100;
			var sequenceReset = new FixMessage();
			sequenceReset.AddTag(Tags.MsgType, "4");
			sequenceReset.AddTag(Tags.MsgSeqNum, (long)100);
			sequenceReset.AddTag(Tags.NewSeqNo, (long)200);
			_sequenceResetMessageHandler.OnNewMessage(sequenceReset);
			Assert.AreEqual(0, _fixSession.GetMessageQueueSize());
			Assert.AreEqual(200 - 1, _fixSession.InSeqNum); //in real flow it will be incremented by next handler before receiving next message
		}

		/// <summary>
		/// c. Receive Sequence Reset (Gap Fill) message with NewSeqNo > MsgSeqNum and MsgSeqNum &lt; than expected sequence
		/// number and PossDupFlag = "Y"
		/// ->
		/// Ignore message
		/// </summary>
		[Test]
		public virtual void ShouldIgnoreMessagesWithMsgSeqLessThanExpectedAndPossDup()
		{
			_fixSession.InSeqNum = 99;
			var sequenceReset = new FixMessage();
			sequenceReset.AddTag(Tags.MsgType, "4");
			sequenceReset.AddTag(Tags.MsgSeqNum, (long)98);
			sequenceReset.AddTag(Tags.PossDupFlag, "Y");
			sequenceReset.AddTag(Tags.GapFillFlag, "Y");
			sequenceReset.AddTag(Tags.NewSeqNo, (long)200);
			_sequenceResetMessageHandler.OnNewMessage(sequenceReset);
			Assert.AreEqual(0, _fixSession.GetMessageQueueSize());
			Assert.AreEqual(98, _fixSession.InSeqNum); //in real flow it will be incremented by next handler before receiving next message
		}

		/// <summary>
		/// e. Receive Sequence Reset (Gap Fill) message with NewSeqNo &lt;= MsgSeqNum and MsgSeqNum = to expected sequence
		/// number
		/// ->
		/// Send Reject (session-level) message with message "attempt to lower sequence number, invalid value
		/// NewSeqNum=&lt;x>"
		/// </summary>
		[Test]
		public virtual void ShouldRejectAndIncrementSequenceIfGapFillAndNewSeqIsEqualToExpected()
		{
			long seqNum = 100;
			_fixSession.InSeqNum = seqNum;
			var sequenceReset = new FixMessage();
			sequenceReset.AddTag(Tags.MsgType, "4");
			sequenceReset.AddTag(Tags.MsgSeqNum, seqNum);
			sequenceReset.AddTag(Tags.NewSeqNo, seqNum);
			sequenceReset.AddTag(Tags.GapFillFlag, "Y");
			_sequenceResetMessageHandler.OnNewMessage(sequenceReset);
			var messageFromQueue = _fixSession.GetMessageWithTypeFromQueue();
			Assert.AreEqual("3", messageFromQueue.MessageType);
			Assert.AreEqual("Attempt to lower sequence number, invalid value NewSeqNum=100", messageFromQueue.FixMessage.GetTagValueAsString(Tags.Text));
			Assert.AreEqual(100, _fixSession.InSeqNum); //in real flow it will be incremented by next handler before receiving next message
		}

		/// <summary>
		/// c. Receive Sequence Reset (reset) message with NewSeqNo &lt; than expected sequence number
		/// ->
		/// 1) Accept the Sequence Reset (Reset) message without regards to its MsgSeqNum
		/// 2) Send Reject (session-level) message referencing invalid MsgType (>= FIX 4.2: SessionRejectReason = "Value
		/// is incorrect (out of range) for this tag")
		/// 3) Do NOT Increment inbound MsgSeqNum
		/// 4) Generate an "error" condition in test output
		/// 5) Do NOT lower expected sequence number
		/// </summary>
		[Test]
		public virtual void ShouldSendRejectIfNewSeqIsLessThanCurrent()
		{
			_fixSession.InSeqNum = 100;
			var sequenceReset = new FixMessage();
			sequenceReset.AddTag(Tags.MsgType, "4");
			sequenceReset.AddTag(Tags.MsgSeqNum, (long)100);
			sequenceReset.AddTag(Tags.NewSeqNo, (long)50);
			_sequenceResetMessageHandler.OnNewMessage(sequenceReset);
			Assert.AreEqual(1, _fixSession.GetMessageQueueSize());
			var messageFromQueue = _fixSession.GetMessageWithTypeFromQueue();
			Assert.AreEqual("3", messageFromQueue.MessageType);
			Assert.AreEqual("Value 50 is incorrect (out of range) for this tag 36", messageFromQueue.FixMessage.GetTagValueAsString(Tags.Text));
			Assert.AreEqual(100 - 1, _fixSession.InSeqNum); //in real flow it will be incremented by next handler before receiving next message
		}

		/// <summary>
		/// b. Receive Sequence Reset (reset) message with NewSeqNo = to expected sequence number
		/// ->
		/// 1) Accept the Sequence Reset Reset) message without regards to its MsgSeqNum
		/// 2) Generate a "warning" condition in test output.
		/// </summary>
		[Test]
		public virtual void ShouldNotRejectAndChangeSequenceIfNewSeqIsEqualToCurrentExpected()
		{
			_fixSession.InSeqNum = 100;
			var sequenceReset = new FixMessage();
			sequenceReset.AddTag(Tags.MsgType, "4");
			sequenceReset.AddTag(Tags.MsgSeqNum, (long)100);
			sequenceReset.AddTag(Tags.NewSeqNo, (long)100);
			_sequenceResetMessageHandler.OnNewMessage(sequenceReset);
			Assert.AreEqual(0, _fixSession.GetMessageQueueSize());
			Assert.AreEqual(100 - 1, _fixSession.InSeqNum); //in real flow it will be incremented by next handler before receiving next message
		}

		[Test]
		public virtual void ShouldSendRejectIfNewSeqIsLessThanCurrentAndMsgSequenceIsNotEqualToExpected()
		{
			var sequenceReset = new FixMessage();
			sequenceReset.AddTag(Tags.MsgType, "4");
			sequenceReset.AddTag(Tags.MsgSeqNum, 1000);
			sequenceReset.AddTag(Tags.NewSeqNo, (long)50);

			_fixSession.SetAttribute(ExtendedFixSessionAttribute.SequenceWasDecremented.ToString(), 100L);
			_fixSession.InSeqNum = 99;

			_sequenceResetMessageHandler.OnNewMessage(sequenceReset);
			Assert.AreEqual(1, _fixSession.GetMessageQueueSize());
			var messageFromQueue = _fixSession.GetMessageWithTypeFromQueue();
			Assert.AreEqual("3", messageFromQueue.MessageType);
			Assert.AreEqual("Value 50 is incorrect (out of range) for this tag 36", messageFromQueue.FixMessage.GetTagValueAsString(Tags.Text));
		}

	}
}
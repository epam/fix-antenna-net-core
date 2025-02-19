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
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Epam.FixAntenna.TestUtils;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	[TestFixture]
	internal class AbstractFixSessionTest
	{
		private static long _millisInHour = 60 * 60 * 1000;
		private AbstractFixSessionHelper _sessionHelper;

		[SetUp]
		public void Before()
		{
			ClearLogs();
			_sessionHelper = new AbstractFixSessionHelper();
			_sessionHelper.PrepareForConnect();
		}

		[TearDown]
		public virtual void TearDown()
		{
			_sessionHelper.Dispose();
			ClassicAssert.IsTrue(ClearLogs(), "Can't clean logs after tests");
		}

		public virtual bool ClearLogs()
		{
			return new LogsCleaner().Clean("./logs") && new LogsCleaner().Clean("./logs/backup");
		}

		[Test]
		public virtual void TestSendNullMessage()
		{
			ClassicAssert.Throws<ArgumentException>(() =>_sessionHelper.SendMessage((FixMessage) null));
		}

		[Test]
		public virtual void TestSendEmptyMessage()
		{
			ClassicAssert.Throws<ArgumentException>(() => _sessionHelper.SendMessage(new FixMessage()));
		}

		[Test]
		public virtual void TestSendAndGetQueueSizeAsync()
		{
			_sessionHelper.ResetQueue();
			ClassicAssert.AreEqual(1, _sessionHelper.SendMessageAndGetQueueSize(GetAppMessage(), FixSessionSendingType.SendAsync));
			ClassicAssert.AreEqual(2, _sessionHelper.SendMessageAndGetQueueSize(GetAppMessage(), FixSessionSendingType.SendAsync));
		}

		[Test]
		public virtual void TestSendWithChangeAndGetQueueSizeAsync()
		{
			_sessionHelper.ResetQueue();
			ClassicAssert.AreEqual(1, _sessionHelper.SendWithChangesAndGetQueueSize(GetAppMessage(), ChangesType.AddSmhAndSmt, FixSessionSendingType.SendAsync));
			ClassicAssert.AreEqual(2, _sessionHelper.SendWithChangesAndGetQueueSize(GetAppMessage(), ChangesType.AddSmhAndSmt, FixSessionSendingType.SendAsync));

		}

		[Test]
		public virtual void TestSendWithTypeAndGetQueueSizeAsync()
		{
			_sessionHelper.ResetQueue();
			ClassicAssert.AreEqual(1, _sessionHelper.SendMessageAndGetQueueSize("A", GetAppMessage(), FixSessionSendingType.SendAsync));
			ClassicAssert.AreEqual(2, _sessionHelper.SendMessageAndGetQueueSize("A", GetAppMessage(), FixSessionSendingType.SendAsync));
		}

		[Test]
		public virtual void TestSendMessage()
		{
			var content = new FixMessage();
			content.AddTag(35, "A");
			_sessionHelper.SendMessage(content);
		}

		[Test]
		public virtual void TestSendAsIsNullMessage()
		{
			ClassicAssert.Throws<ArgumentException>(() => _sessionHelper.SendAsIs(null));
		}

		[Test]
		public virtual void TestSendAsIsEmptyMessage()
		{
			ClassicAssert.Throws<ArgumentException>(() => _sessionHelper.SendAsIs(new FixMessage()));
		}

		[Test]
		public virtual void TestSendAsIsMessage()
		{
			var content = new FixMessage();
			content.AddTag(35, "A");
			_sessionHelper.SendAsIs(content);
		}

		[Test]
		public virtual void TestSendMessageOutOfTurn()
		{
			ClassicAssert.Throws<ArgumentException>(() => _sessionHelper.SendMessageOutOfTurn(null, null));
		}

		[Test]
		public virtual void TestSendMessageOutOfTurnWithEmptyType()
		{
			ClassicAssert.Throws<ArgumentException>(() => _sessionHelper.SendMessageOutOfTurn("", null));
		}

		[Test]
		public virtual void TestSendMessageOutOfTurnWithType()
		{
			_sessionHelper.SendMessageOutOfTurn("A", null);
		}

		[Test]
		public virtual void TestSendMessageOutOfTurnWithEnptyContent()
		{
			ClassicAssert.Throws<ArgumentException>(() => _sessionHelper.SendMessageOutOfTurn("", new FixMessage()));
		}

		[Test]
		public virtual void TestSendMessageOutOfTurnWithMsgTypeInContent()
		{
			var content = new FixMessage();
			content.AddTag(35, "A");
			_sessionHelper.SendMessageOutOfTurn("", content);
		}

		[Test]
		public virtual void TestResetSequenceNumber()
		{
			_sessionHelper.Parameters.OutgoingLoginMessage.Clear();
			_sessionHelper.SessionState = SessionState.Connected;
			_sessionHelper.ResetQueue();

			_sessionHelper.ResetSequenceNumbers(false);
			var logon = _sessionHelper.GetMessageFromQueue();
			ClassicAssert.IsNotNull(logon);
			var buffer = new ByteBuffer();
			_sessionHelper.MessageFactory.Serialize(null, "", logon, buffer, new SerializationContext());
			logon = RawFixUtil.GetFixMessage(buffer.GetByteArray(), 0, buffer.Offset);
			var resetFlag = logon.GetTag(Tags.ResetSeqNumFlag);
			ClassicAssert.IsNotNull(resetFlag);
			ClassicAssert.AreEqual("Y", resetFlag.StringValue);
		}

		[Test]
		public virtual void TestResetSequenceNumberWithOutgoingResetFlag()
		{
			_sessionHelper.Parameters.OutgoingLoginMessage.AddTag(Tags.ResetSeqNumFlag, "N");
			_sessionHelper.SessionState = SessionState.Connected;

			_sessionHelper.ResetQueue();

			_sessionHelper.ResetSequenceNumbers(false);
			var logon = _sessionHelper.GetMessageFromQueue();
			ClassicAssert.IsNotNull(logon);
			var buffer = new ByteBuffer();
			_sessionHelper.MessageFactory.Serialize(null, "", logon, buffer, new SerializationContext());
			logon = RawFixUtil.GetFixMessage(buffer.GetByteArray(), 0, buffer.Offset);

			var resetFlag = logon.GetTag(Tags.ResetSeqNumFlag);
			ClassicAssert.IsNotNull(resetFlag); // this situation happens because 141
			// tags will be occurred before message send and in this
			// point we should be sure that message does not contains 141 tags
		}

		[Test]
		public virtual void TestSetSequenceNumberConnected()
		{
			_sessionHelper.Parameters.OutgoingLoginMessage.Clear();
			_sessionHelper.SessionState = SessionState.Connected;
			_sessionHelper.ResetQueue();

			var newInSeqNum = 100;
			var newOutSeqNum = 200;
			_sessionHelper.SetSequenceNumbers(newInSeqNum, newOutSeqNum);
			ClassicAssert.AreEqual(newInSeqNum, _sessionHelper.Parameters.IncomingSequenceNumber);
			ClassicAssert.AreEqual(newOutSeqNum, _sessionHelper.Parameters.OutgoingSequenceNumber);
			ClassicAssert.AreEqual(newInSeqNum, _sessionHelper.RuntimeState.InSeqNum);
			ClassicAssert.AreEqual(newOutSeqNum, _sessionHelper.RuntimeState.OutSeqNum);
		}

		[Test]
		public virtual void TestSetSequenceNumberDisconnected()
		{
			_sessionHelper.Parameters.OutgoingLoginMessage.Clear();
			_sessionHelper.SessionState = SessionState.Disconnected;
			_sessionHelper.ResetQueue();

			var newInSeqNum = 100;
			var newOutSeqNum = 200;
			_sessionHelper.SetSequenceNumbers(newInSeqNum, newOutSeqNum);
			ClassicAssert.AreEqual(newInSeqNum, _sessionHelper.Parameters.IncomingSequenceNumber);
			ClassicAssert.AreEqual(newOutSeqNum, _sessionHelper.Parameters.OutgoingSequenceNumber);
			ClassicAssert.AreEqual(0, _sessionHelper.RuntimeState.InSeqNum);
			ClassicAssert.AreEqual(0, _sessionHelper.RuntimeState.OutSeqNum);
		}

		[Test]
		public virtual void TestSetSequenceNumberBeforeConnect()
		{
			_sessionHelper.Parameters.OutgoingLoginMessage.Clear();
			//sessionHelper.SessionState = SessionState.Disconnected;
			_sessionHelper.ResetQueue();

			var newInSeqNum = 100;
			var newOutSeqNum = 200;
			_sessionHelper.SetSequenceNumbers(newInSeqNum, newOutSeqNum);
			ClassicAssert.AreEqual(newInSeqNum, _sessionHelper.Parameters.IncomingSequenceNumber);
			ClassicAssert.AreEqual(newOutSeqNum, _sessionHelper.Parameters.OutgoingSequenceNumber);
			ClassicAssert.AreEqual(newInSeqNum, _sessionHelper.RuntimeState.InSeqNum);
			ClassicAssert.AreEqual(newOutSeqNum, _sessionHelper.RuntimeState.OutSeqNum);
		}

		[Test]
		public virtual void TestSetSequenceNumberAfterDisconnect()
		{
			_sessionHelper.Parameters.OutgoingLoginMessage.Clear();
			_sessionHelper.SessionState = SessionState.Disconnected;
			_sessionHelper.ResetQueue();

			var newInSeqNum = 100;
			var newOutSeqNum = 200;
			_sessionHelper.SetSequenceNumbers(newInSeqNum, newOutSeqNum);
			ClassicAssert.AreEqual(newInSeqNum, _sessionHelper.Parameters.IncomingSequenceNumber);
			ClassicAssert.AreEqual(newOutSeqNum, _sessionHelper.Parameters.OutgoingSequenceNumber);
			ClassicAssert.AreEqual(0, _sessionHelper.RuntimeState.InSeqNum);
			ClassicAssert.AreEqual(0, _sessionHelper.RuntimeState.OutSeqNum);

			_sessionHelper.PrepareForConnect();
			ClassicAssert.AreEqual(0, _sessionHelper.Parameters.IncomingSequenceNumber);
			ClassicAssert.AreEqual(0, _sessionHelper.Parameters.OutgoingSequenceNumber);
			ClassicAssert.AreEqual(newInSeqNum, _sessionHelper.RuntimeState.InSeqNum);
			ClassicAssert.AreEqual(newOutSeqNum, _sessionHelper.RuntimeState.OutSeqNum);
		}

		[Test]
		public virtual void TestSendingLogoutFlag()
		{
			_sessionHelper.AlreadySendingLogout = false;
			ClassicAssert.IsTrue(_sessionHelper.TryStartSendingLogout());

			ClassicAssert.IsFalse(_sessionHelper.TryStartSendingLogout());
		}

		internal string SeqResetTimeFormat = "HH:mm:ss";
		[Test]
		public virtual void CheckIsResetTimeNotMissed()
		{
			var currentTime = DateTimeHelper.CurrentMilliseconds;
			var resetTime = currentTime - 1 * _millisInHour;

			_sessionHelper.SetConfigProperty(Config.ResetSequenceTimeZone, TimeZoneInfo.Local.Id);
			_sessionHelper.SetConfigProperty(Config.ResetSequenceTime, resetTime.ToDateTimeString(SeqResetTimeFormat));
			var resetTimeMissed = _sessionHelper.SequenceManager.IsResetTimeMissed(currentTime);
			ClassicAssert.IsFalse(resetTimeMissed, "Reset time don't miss");
		}

		[Test]
		public virtual void CheckIsResetTimeNotMissedInTime()
		{
			var currentTime = DateTimeHelper.CurrentMilliseconds;

			_sessionHelper.SetConfigProperty(Config.ResetSequenceTimeZone, TimeZoneInfo.Local.Id);
			_sessionHelper.SetConfigProperty(Config.ResetSequenceTime, currentTime.ToDateTimeString(SeqResetTimeFormat));
			var resetTimeMissed = _sessionHelper.SequenceManager.IsResetTimeMissed(currentTime);
			ClassicAssert.IsFalse(resetTimeMissed, "Reset time don't miss");
		}

		[Test]
		public virtual void CheckIsResetTimeIsMissedLess24H()
		{
			var currentTime = DateTime.UtcNow;
			var resetTime = currentTime.AddHours(-1);
			var testTime = currentTime.AddHours(-2);

			_sessionHelper.SetConfigProperty(Config.ResetSequenceTimeZone, TimeZoneInfo.Local.Id);
			_sessionHelper.SetConfigProperty(Config.ResetSequenceTime, resetTime.ToString(SeqResetTimeFormat, CultureInfo.InvariantCulture));

			var resetTimeMissed = _sessionHelper.SequenceManager.IsResetTimeMissed(testTime.TotalMilliseconds());
			ClassicAssert.IsTrue(resetTimeMissed, "Last reset was before required time");
		}

		[Test]
		public virtual void CheckIsResetTimeIsMissedMore24H()
		{
			var currentTime = DateTime.UtcNow;
			var resetTime = currentTime.AddHours(-1);
			var testTime = currentTime.AddHours(-26);

			_sessionHelper.SetConfigProperty(Config.ResetSequenceTimeZone, TimeZoneInfo.Local.Id);
			_sessionHelper.SetConfigProperty(Config.ResetSequenceTime, resetTime.ToString(SeqResetTimeFormat, CultureInfo.InvariantCulture));
			var resetTimeMissed = _sessionHelper.SequenceManager.IsResetTimeMissed(testTime.TotalMilliseconds());
			ClassicAssert.IsTrue(resetTimeMissed, "Last reset was before required time");
		}

		[Test]
		public virtual void CheckThatSessionCanAcceptMessagesAfterShutdown()
		{
			_sessionHelper.ResetQueue();
			_sessionHelper.SetOutOfTurnOnlyMode(false);

			_sessionHelper.SendMessage("B", new FixMessage());
			ClassicAssert.AreEqual(1, _sessionHelper.GetMessageQueueSize(), "1 message will be in queue");
			_sessionHelper.Shutdown(DisconnectReason.GetDefault(), true);
			_sessionHelper.SendMessage("B", new FixMessage());
			ClassicAssert.AreEqual(2, _sessionHelper.GetMessageQueueSize(), "2 message will be in queue");
		}

		[Test]
		public virtual void CheckThatSessionDontAcceptMessagesAfterDispose()
		{
			_sessionHelper.ResetQueue();
			_sessionHelper.SetOutOfTurnOnlyMode(false);

			_sessionHelper.SendMessage("B", new FixMessage());
			ClassicAssert.AreEqual(1, _sessionHelper.GetMessageQueueSize(), "1 message will be in queue");
			_sessionHelper.Dispose();
			ClassicAssert.Throws<InvalidOperationException>(() => _sessionHelper.SendMessage("B", new FixMessage()));
		}

		[Test]
		public virtual void TestDontDisconnectAfterTestRequestWaitingInterval()
		{
			// hbi = 2
			//to allow changing to disconnected and sending Logout
			_sessionHelper.SessionState = SessionState.Connected;
			var heartbeatInterval = _sessionHelper.Parameters.HeartbeatInterval;
			_sessionHelper.ResetQueue();

			//wait till need to send testRequest
			Thread.Sleep((heartbeatInterval + 1) * 1000);
			//send test request
			_sessionHelper.CheckHasSessionSendOrReceivedTestRequest();
			ClassicAssert.AreEqual(1, _sessionHelper.GetMessageQueueSize(), "1 message will be in queue");
			ClassicAssert.AreEqual("1", _sessionHelper.GetMessageWithTypeFromQueue().MessageType, "Should be TestRequest");
			_sessionHelper.ResetQueue();

			//wait more then bhi seconds and send a message every sec
			for (var i = 0; i < heartbeatInterval + 1; i++)
			{
				//emulate sending any message to session and reset time
				_sessionHelper.ResetLastInMessageTimestamp();
				//sleep for a while
				Thread.Sleep(1000);
				_sessionHelper.CheckHasSessionSendOrReceivedTestRequest();
				if (_sessionHelper.GetMessageQueueSize() > 0)
				{
					break;
				}
			}

			//session os alive and there are in messages - no need to send TR or Logout
			ClassicAssert.AreEqual(0, _sessionHelper.GetMessageQueueSize(), "0 message will be in queue");
		}

		[Test]
		public virtual void TestSendTestRequestSecondTimeAfterAnswer()
		{
			//to allow changing to disconnected and sending Logout
			_sessionHelper.SessionState = SessionState.Connected;
			// hbi = 2
			var heartbeatInterval = _sessionHelper.Parameters.HeartbeatInterval;
			_sessionHelper.ResetQueue();

			//wait till need to send testRequest
			Thread.Sleep((heartbeatInterval + 1) * 1000);
			//send test request
			_sessionHelper.CheckHasSessionSendOrReceivedTestRequest();
			ClassicAssert.AreEqual(1, _sessionHelper.GetMessageQueueSize(), "1 message will be in queue");
			ClassicAssert.AreEqual("1", _sessionHelper.GetMessageWithTypeFromQueue().MessageType, "Should be TestRequest");
			var testRequestId = _sessionHelper.GetMessageFromQueue().GetTag(112);
			_sessionHelper.ResetQueue();

			//emulate answer
			_sessionHelper.ResetLastInMessageTimestamp();
			_sessionHelper.SetAttribute(ExtendedFixSessionAttribute.LastReceivedTestReqId.Name, testRequestId);

			//process answer
			_sessionHelper.CheckHasSessionSendOrReceivedTestRequest();

			//pause one more time
			//wait till need to send testRequest
			Thread.Sleep((heartbeatInterval + 1) * 1000);
			//send test request
			_sessionHelper.CheckHasSessionSendOrReceivedTestRequest();
			ClassicAssert.AreEqual(1, _sessionHelper.GetMessageQueueSize(), "1 message will be in queue");
			ClassicAssert.AreEqual("1", _sessionHelper.GetMessageWithTypeFromQueue().MessageType, "Should be TestRequest");
		}

		[Test]
		public virtual void ShouldTerminateSessionWithoutLogoutMessageIfThereIsNoResponseForTestRequest()
		{
			_sessionHelper.SessionState = SessionState.Connected;
			var heartbeatInterval = _sessionHelper.Parameters.HeartbeatInterval;
			_sessionHelper.ResetQueue();

			var lastTimeOfTr = DateTimeHelper.CurrentMilliseconds - ((heartbeatInterval * 3) * 1000);
			_sessionHelper.TestRequestTime.Value = lastTimeOfTr;
			_sessionHelper.SetAttribute(ExtendedFixSessionAttribute.LastSentTestReqId.Name, _sessionHelper.TestRequestTime);
			_sessionHelper.Reader.MessageProcessedTimestamp = lastTimeOfTr;
			_sessionHelper.SentTrNum.SetNumber(1);
			_sessionHelper.SetAttribute(ExtendedFixSessionAttribute.SentTestReqNumberId.Name, _sessionHelper.SentTrNum);
			_sessionHelper.CheckHasSessionSendOrReceivedTestRequest(); //disconnect

			ClassicAssert.AreEqual(0, _sessionHelper.GetMessageQueueSize(), "Test request should not be sent");
			CheckingUtils.CheckWithinTimeout(
				() => _sessionHelper.SessionState == SessionState.WaitingForForcedLogoff ||
							_sessionHelper.SessionState == SessionState.DisconnectedAbnormally ||
							_sessionHelper.SessionState == SessionState.Dead, TimeSpan.FromSeconds(1));
		}

		[Test]
		public virtual void TestSendTestRequestSecondTimeAfterReinit()
		{
			//to allow changing to disconnected and sending Logout
			_sessionHelper.SessionState = SessionState.Connected;
			// hbi = 2
			var heartbeatInterval = _sessionHelper.Parameters.HeartbeatInterval;
			_sessionHelper.ResetQueue();

			//wait till need to send testRequest
			Thread.Sleep((heartbeatInterval + 1) * 1000);
			//send test request
			_sessionHelper.CheckHasSessionSendOrReceivedTestRequest();
			ClassicAssert.AreEqual(1, _sessionHelper.GetMessageQueueSize(), "1 message will be in queue");
			ClassicAssert.AreEqual("1", _sessionHelper.GetMessageWithTypeFromQueue().MessageType, "Should be TestRequest");
			_sessionHelper.Shutdown(DisconnectReason.GetDefault(), true);
			_sessionHelper.PrepareForConnect();
			//to allow changing to disconnected and sending Logout
			_sessionHelper.SessionState = SessionState.Connected;
			_sessionHelper.ResetQueue();

			//pause one more time
			//wait till need to send testRequest
			Thread.Sleep((heartbeatInterval + 1) * 1000);
			//send test request
			_sessionHelper.CheckHasSessionSendOrReceivedTestRequest();
			ClassicAssert.AreEqual(1, _sessionHelper.GetMessageQueueSize(), "1 message will be in queue");
			ClassicAssert.AreEqual("1", _sessionHelper.GetMessageWithTypeFromQueue().MessageType, "Should be TestRequest");
		}

		private FixMessage GetAppMessage()
		{
			var msg = new FixMessage();
			msg.AddTag(35, 'D');
			return msg;
		}

		private FixMessage GetSessionMessage()
		{
			var msg = new FixMessage();
			msg.AddTag(35, 'A');
			return msg;
		}

		[Test]
		public virtual void TestDefaultQueueClean()
		{
			IList<FixMessage> rejected = new List<FixMessage>();
			_sessionHelper.RejectMessageListener = new RejectMessageListenerImpl(rejected);
			_sessionHelper.ResetQueue();
			ClassicAssert.AreEqual(1, _sessionHelper.SendMessageAndGetQueueSize(GetSessionMessage(), FixSessionSendingType.SendAsync));
			ClassicAssert.AreEqual(2, _sessionHelper.SendMessageAndGetQueueSize(GetAppMessage(), FixSessionSendingType.SendAsync));
			ClassicAssert.IsFalse(_sessionHelper.SendMessageOutOfTurn("A", new FixMessage()));
			ClassicAssert.IsFalse(_sessionHelper.SendMessageOutOfTurn("W", new FixMessage()));
			ClassicAssert.AreEqual(4, _sessionHelper.MessageQueue.TotalSize);

			//by default Configuration.EnableMessageRejecting is disabled and clear queue do nothing
			_sessionHelper.ClearQueue();
			ClassicAssert.AreEqual(2, _sessionHelper.MessageQueue.TotalSize);
			ClassicAssert.AreEqual(0, rejected.Count);
		}

		[Test]
		public virtual void TestQueueCleanWithEnabledRejecting()
		{
			var @params = new SessionParameters();
			@params.Configuration.SetProperty(Config.EnableMessageRejecting, "true");

			//re-init session with new params
			_sessionHelper.Dispose();
			_sessionHelper = new AbstractFixSessionHelper(@params);
			_sessionHelper.PrepareForConnect();

			IList<FixMessage> rejected = new List<FixMessage>();
			_sessionHelper.RejectMessageListener = new RejectMessageListenerImpl(rejected);
			_sessionHelper.ResetQueue();
			ClassicAssert.AreEqual(1, _sessionHelper.SendMessageAndGetQueueSize(GetSessionMessage(), FixSessionSendingType.SendAsync));
			ClassicAssert.AreEqual(2, _sessionHelper.SendMessageAndGetQueueSize(GetAppMessage(), FixSessionSendingType.SendAsync));
			ClassicAssert.IsFalse(_sessionHelper.SendMessageOutOfTurn("A", new FixMessage()));
			ClassicAssert.IsFalse(_sessionHelper.SendMessageOutOfTurn("W", new FixMessage()));
			ClassicAssert.AreEqual(4, _sessionHelper.MessageQueue.TotalSize);

			//by default Configuration.EnableMessageRejecting is disabled and clear queue do nothing
			_sessionHelper.ClearQueue();
			ClassicAssert.AreEqual(0, _sessionHelper.MessageQueue.TotalSize);
			ClassicAssert.AreEqual(2, rejected.Count);
		}

		private class RejectMessageListenerImpl : IRejectMessageListener
		{
			private readonly IList<FixMessage> _rejected;
			public RejectMessageListenerImpl(IList<FixMessage> rejected)
			{
				_rejected = rejected;
			}
			public void OnRejectMessage(FixMessage message)
			{
				_rejected.Add(message);
			}
		}
	}
}
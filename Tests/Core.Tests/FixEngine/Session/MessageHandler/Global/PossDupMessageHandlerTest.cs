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
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global
{
	[TestFixture]
	internal class PossDupMessageHandlerTest : AbstractDataLengthCheckHandlerTst
	{
		private PossDupMessageHandler _messageHandler;
		private TestFixSession _session;
		private FixMessage _hbMessage;

		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			_hbMessage = RawFixUtil.GetFixMessage(
				"8=FIX4.4\x000135=0\x00019=1\x000134=1\x000152=20110126-15:35:47.086\x000143=Y\x0001122=20110105-10:46:44.442\x000110=\x0001"
					.AsByteArray());
			_session = new TestFixSession();
			_messageHandler = new PossDupMessageHandler();
			_messageHandler.NextHandler = this;
			_messageHandler.Session = _session;
			_session.RuntimeState.InSeqNum = 100;
		}

		[Test]
		public virtual void TestProcessPosDupMessageWithHiSeqNum()
		{
			_hbMessage.Set(34, "101");
			_messageHandler.OnNewMessage(_hbMessage);

			ClassicAssert.AreEqual(_session.RuntimeState.InSeqNum, 100);
			ClassicAssert.AreEqual(0, _session.Messages.Count);
			ClassicAssertThatMessagePassedToNextHandlerIs(_hbMessage);
		}

		[Test]
		public virtual void TestProcessPosDupMessageWithLoSeqNum()
		{
			_hbMessage.Set(34, "99");
			_messageHandler.OnNewMessage(_hbMessage);

			ClassicAssert.AreEqual(_session.RuntimeState.InSeqNum, 100);
			ClassicAssert.AreEqual(0, _session.Messages.Count);
			ClassicAssertThatMessagePassedToNextHandlerIs(_hbMessage);
		}

		[Test]
		public virtual void TestProcessPosDupMessageWithExpectedSeqNum()
		{
			_hbMessage.Set(34, "100");
			_messageHandler.OnNewMessage(_hbMessage);

			ClassicAssert.AreEqual(_session.RuntimeState.InSeqNum, 100);
			ClassicAssert.AreEqual(0, _session.Messages.Count);
			ClassicAssertThatMessagePassedToNextHandlerIs(_hbMessage);
		}

		[Test]
		public virtual void TestProcessMessageWithExpectedSeqNum()
		{
			_hbMessage.Set(34, "100");
			_hbMessage.RemoveTag(43);
			_messageHandler.OnNewMessage(_hbMessage);

			ClassicAssert.AreEqual(_session.RuntimeState.InSeqNum, 100);
			ClassicAssert.AreEqual(0, _session.Messages.Count);
			ClassicAssertThatMessagePassedToNextHandlerIs(_hbMessage);
		}

		[Test]
		public virtual void TestOnTooBigOrigSendingTimeSendReject()
		{
			_hbMessage.Set(122, "20110126-15:35:47.087");

			var ex = ClassicAssert.Throws<MessageValidationException>(() =>
			{
				_messageHandler.OnNewMessage(_hbMessage);
			});

			ClassicAssert.IsFalse(ex.IsCritical());
			ClassicAssertNoMessagePassedForNext();
			ClassicAssert.AreEqual(1, _session.Messages.Count);
			var fixMessage = _session.Messages[0];
			ClassicAssert.AreEqual(3, fixMessage.GetTagAsInt(35));
			ClassicAssert.AreEqual(122, fixMessage.GetTagAsInt(371));
			ClassicAssert.AreEqual("OriginalSendingTime is after SendingTime", fixMessage.GetTagValueAsString(58));
		}

		[Test]
		public virtual void TestOnEqualsOrigSendingTimeNoReject()
		{
			_hbMessage.Set(122, _hbMessage.GetTagValueAsString(52));
			_messageHandler.OnNewMessage(_hbMessage);

			ClassicAssert.AreEqual(0, _session.Messages.Count);
			ClassicAssertThatMessagePassedToNextHandlerIs(_hbMessage);
		}

		[Test]
		public virtual void TestOnAbsentOrigSendingTimeSendReject()
		{
			_hbMessage.RemoveTag(122);
			var ex = ClassicAssert.Throws<MessageValidationException>(() =>
			{
				_messageHandler.OnNewMessage(_hbMessage);
			});

			ClassicAssert.IsFalse(ex.IsCritical());
			ClassicAssertNoMessagePassedForNext();
			ClassicAssert.AreEqual(1, _session.Messages.Count);
			var fixMessage = _session.Messages[0];
			ClassicAssert.AreEqual(3, fixMessage.GetTagAsInt(35));
			ClassicAssert.AreEqual(122, fixMessage.GetTagAsInt(371));
			ClassicAssert.AreEqual("OriginalSendingTime is missing for PossDup message",
				fixMessage.GetTagValueAsString(58));
		}

		[Test]
		public virtual void TestOnEmptyOrigSendingTimeSendReject()
		{
			_hbMessage.Set(122, "");
			var ex = ClassicAssert.Throws<MessageValidationException>(() =>
			{
				_messageHandler.OnNewMessage(_hbMessage);
			});

			ClassicAssert.IsFalse(ex.IsCritical());
			ClassicAssertNoMessagePassedForNext();
			ClassicAssert.AreEqual(1, _session.Messages.Count);
			var fixMessage = _session.Messages[0];
			ClassicAssert.AreEqual(3, fixMessage.GetTagAsInt(35));
			ClassicAssert.AreEqual(122, fixMessage.GetTagAsInt(371));
			ClassicAssert.AreEqual("invalid OriginalSendingTime", fixMessage.GetTagValueAsString(58));
		}

		[Test]
		public virtual void TestDisableOrigSendingTimeValidation()
		{
			//init handler with updated session
			_session.Parameters.Configuration.SetProperty(Config.OrigSendingTimeChecking, "false");
			_messageHandler.Session = _session;

			_hbMessage.Set(122, "20110126-15:35:47.087");
			_messageHandler.OnNewMessage(_hbMessage);

			ClassicAssert.AreEqual(0, _session.Messages.Count);
			ClassicAssertThatMessagePassedToNextHandlerIs(_hbMessage);
		}

		[Test]
		public virtual void TestAbsentOrigSendingTimeWithDisabledValidation()
		{
			//init handler with updated session
			_session.Parameters.Configuration.SetProperty(Config.OrigSendingTimeChecking, "false");
			_messageHandler.Session = _session;

			_hbMessage.RemoveTag(122);
			_messageHandler.OnNewMessage(_hbMessage);

			ClassicAssert.AreEqual(0, _session.Messages.Count);
			ClassicAssertThatMessagePassedToNextHandlerIs(_hbMessage);
		}
	}

}
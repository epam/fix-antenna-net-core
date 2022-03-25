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
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global
{
	[TestFixture]
	internal class OutOfSequenceMessageHandlerTest
	{
		private OutOfSequenceMessageHandler _handler;
		private NextHandler _nextHandler;

		private FixMessage _message;
		public const string MsgTypeBytes = "2";
		private TestFixSession _testFixSession;

		[SetUp]
		public virtual void SetUp()
		{
			_testFixSession = new TestFixSession();

			_handler = new OutOfSequenceMessageHandler();
			_nextHandler = new NextHandler();
			_handler.NextHandler = _nextHandler;
			_handler.Session = _testFixSession;

			_message = new FixMessage();
			_message.AddTag(8, "FIX.4.4");
			_message.AddTag(35, "A");
			_message.AddTag(34, "7");
			_message.AddTag(10, "20");
		}

		[TearDown]
		public virtual void TearDown()
		{
			_testFixSession.Dispose();
		}

		[Test]
		public virtual void ToHighMessageSequenceProduceRr()
		{
			_message.Set(35, "0");
			_handler.OnNewMessage(_message);

			var resendSeqMessage = GetMessageWithType(MsgTypeBytes);
			Assert.IsNotNull(resendSeqMessage);
			Assert.AreEqual(MsgTypeBytes, resendSeqMessage.GetTagValueAsString(35));
			AssertNoOutMessage();
		}

		[Test]
		public virtual void ToHighMessageSequenceProduceRrForLogon()
		{
			_handler.OnNewMessage(_message);

			var resendSeqMessage = GetMessageWithType(MsgTypeBytes);
			Assert.IsNotNull(resendSeqMessage);
			Assert.AreEqual(MsgTypeBytes, resendSeqMessage.GetTagValueAsString(35));
			AssertOutMessageEquals(_message);
		}

		[Test]
		public virtual void ToLowMessageSequenceDisconnect()
		{
			Assert.Throws<SequenceToLowException>(() =>
			{
				_message.Set(34, 1);
				_testFixSession.RuntimeState.InSeqNum = 20;
				_testFixSession.SessionState = SessionState.Connected;
				_handler.OnNewMessage(_message);
			});

			Assert.AreEqual("Incoming seq number 1 is less then expected 20", _testFixSession.DisconnectReason);
			Assert.AreEqual(SessionState.WaitingForForcedDisconnect, _testFixSession.SessionState);
		}

		[Test]
		public virtual void PosDupSmartDeliveryTest()
		{
			_testFixSession.Dispose();
			var testFixSessionWithSmartDelivery = new TestFixSession();
			testFixSessionWithSmartDelivery.Parameters.Configuration.SetProperty(Config.PossDupSmartDelivery, "true");
			testFixSessionWithSmartDelivery.RuntimeState.InSeqNum = 20;
			_handler.Session = testFixSessionWithSmartDelivery;

			_message.Set(34, 1);
			_message.AddTag(43, "Y");
			_handler.OnNewMessage(_message);

			AssertOutMessageEquals(_message);
		}

		private FixMessage GetMessageWithType(string msgType)
		{
			foreach (var item in _testFixSession.Messages)
			{
				if (!msgType.Equals(item.GetTagValueAsString(35)))
				{
					continue;
				}
				return item;
			}
			return null;
		}

		private void AssertNoOutMessage()
		{
			Assert.IsNull(_nextHandler.GetMessage());
		}

		private void AssertOutMessageEquals(FixMessage message)
		{
			var processed = _nextHandler.GetMessage();
			Assert.IsNotNull(processed);
			Assert.AreEqual(message.ToString(), processed.ToString());
		}

		private class NextHandler : AbstractGlobalMessageHandler
		{
			internal FixMessage Message;

			public virtual FixMessage GetMessage()
			{
				return Message;
			}

			public override void OnNewMessage(FixMessage message)
			{
				Message = message;
			}
		}
	}

}
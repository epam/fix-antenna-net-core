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
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global
{
	[TestFixture]
	internal class RrSequenceRangeResponseHandlerTest
	{
		private RrSequenceRangeResponseHandler _rrHandler;
		private NextHandler _nextHandler;
		private FixMessage _message;
		private TestFixSession _testFixSession;

		[SetUp]
		public virtual void SetUp()
		{
			_nextHandler = new NextHandler();
			_testFixSession = new TestFixSession();

			_rrHandler = new RrSequenceRangeResponseHandler();
			_rrHandler.NextHandler = _nextHandler;
			_rrHandler.Session = _testFixSession;

			_message = new FixMessage();
			_message.AddTag(8, "FIX.4.4");
			_message.AddTag(35, "D");
			_message.AddTag(34, "1");
			_message.AddTag(10, "020");
		}

		[Test]
		public virtual void TestMsgWithoutPossDupInRangeRr()
		{
			Assert.Throws<SequenceToLowException>(() => { TestInRangeRr(_message); });
		}

		[Test]
		public virtual void TestMsgWithPossDupInRangeRr()
		{
			AddPossDupFlag(_message);
			TestInRangeRr(_message);
		}

		[Test]
		public virtual void TestLogonMsgWithoutPossDupInRangeRr()
		{
			_message.Set(Tags.MsgType, "A".AsByteArray());
			TestInRangeRr(_message);
		}

		[Test]
		public virtual void TestLogoutMsgWithoutPossDupInRangeRrSessionWaitForLogoff()
		{
			_testFixSession.SessionState = SessionState.WaitingForLogoff;
			_message.Set(Tags.MsgType, "5".AsByteArray());
			TestInRangeRr(_message);
		}

		[Test]
		public virtual void TestLogoutMsgWithoutPossDupInRangeRrSessionWaitForForcedLogoff()
		{
			_testFixSession.SessionState = SessionState.WaitingForForcedLogoff;
			_message.Set(Tags.MsgType, "5".AsByteArray());
			TestInRangeRr(_message);
		}

		[Test]
		public virtual void TestEndRangeOfRrSequence()
		{
			AddPossDupFlag(_message);
			TestRangeRr(_message, 10, 15, 15);
			Assert.IsFalse(_testFixSession.SequenceManager.IsRRangeExists());
		}

		private FixMessage GetMessageWithType(string type)
		{
			var tagValue = new TagValue();
			foreach (var item in _testFixSession.Messages)
			{
				item.LoadTagValue(35, tagValue);
				if (tagValue.Length != type.Length)
				{
					continue;
				}
				if (!type.Equals(tagValue.StringValue))
				{
					continue;
				}
				return item;
			}
			return null;
		}


		private void TestInRangeRr(FixMessage message)
		{
			TestRangeRr(message, 10, 15, 12);
			Assert.IsTrue(_testFixSession.SequenceManager.IsRRangeExists());
		}

		private void AddPossDupFlag(FixMessage message)
		{
			message.Set(Tags.PossDupFlag, "Y".AsByteArray());
		}

		private void TestRangeRr(FixMessage message, int start, int end, int current)
		{
			_testFixSession.SetAttribute(ExtendedFixSessionAttribute.StartOfRrRange, start);
			_testFixSession.SetAttribute(ExtendedFixSessionAttribute.EndOfRrRange, end);
			message.Set(Tags.MsgSeqNum, current);
			_rrHandler.OnNewMessage(message);
			AssertMsgNextHandlerEquals(message);
		}

		private void AssertMsgNextHandlerEquals(FixMessage message)
		{
			var processed = _nextHandler.Message;
			Assert.IsNotNull(processed);
			Assert.AreEqual(message.ToString(), processed.ToString());
		}

		private class NextHandler : AbstractGlobalMessageHandler
		{
			public FixMessage Message { get; private set; }

			public override void OnNewMessage(FixMessage message)
			{
				Message = message;
			}
		}
	}
}
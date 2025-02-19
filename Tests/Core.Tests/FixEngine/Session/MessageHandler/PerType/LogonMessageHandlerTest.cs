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
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.PerType;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation.Error;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.PerType
{
	[TestFixture]
	internal class LogonMessageHandlerTest
	{
		private LogonMessageHandler _logonMessageHandler;
		private TestFixSession _testFixSession;

		[SetUp]
		public virtual void SetUp()
		{
			_testFixSession = new TestFixSession();
			_logonMessageHandler = new LogonMessageHandler();
			_logonMessageHandler.Session = _testFixSession;
		}

		[Test]
		public virtual void TestInvalidSeqNumReset()
		{
			var fieldList = new FixMessage();
			fieldList.AddTag(Tags.BeginString, "FIX.4.4");
			fieldList.AddTag(Tags.ResetSeqNumFlag, 'Y');
			fieldList.AddTag(Tags.HeartBtInt, 30L);
			fieldList.AddTag(Tags.MsgSeqNum, 100L);

			var ex = ClassicAssert.Throws<MessageValidationException>(() =>
			{
				_logonMessageHandler.OnNewMessage(fieldList);
			});

			ClassicAssert.IsTrue(ex.IsCritical());
			ClassicAssert.AreEqual(Tags.MsgSeqNum, ex.GetProblemField().TagId);
			ClassicAssert.AreEqual(FixErrorCode.ValueIncorrectOutOfRangeForTag, ex.GetFixErrorCode());

			ClassicAssert.IsTrue(_testFixSession.Messages.Count > 0);
			var responseMessage = _testFixSession.Messages[0];
			ClassicAssert.AreEqual("3", responseMessage.GetTagValueAsString(Tags.MsgType));
			ClassicAssert.AreEqual("MsgSeqNum must be equal to 1 while resetting the sequence number", responseMessage.GetTagValueAsString(58));
		}

		[Test]
		public virtual void TestSeqNumReset()
		{
			var fixMessage = new FixMessage();
			fixMessage.AddTag(Tags.BeginString, "FIX.4.4");
			fixMessage.AddTag(Tags.ResetSeqNumFlag, 'Y');
			fixMessage.AddTag(Tags.HeartBtInt, (long)30);
			fixMessage.AddTag(Tags.MsgSeqNum, (long)1);

			_logonMessageHandler.OnNewMessage(fixMessage);

			ClassicAssert.IsFalse(_testFixSession.Messages.Count > 0);
		}

		[Test]
		public virtual void TestInvalidHeartBtInt()
		{
			var fixMessage = new FixMessage();
			fixMessage.AddTag(Tags.BeginString, "FIX.4.4");
			fixMessage.AddTag(Tags.ResetSeqNumFlag, "Y");
			fixMessage.AddTag(Tags.HeartBtInt, -60);
			fixMessage.AddTag(Tags.MsgSeqNum, (long)1);

			var ex = ClassicAssert.Throws<MessageValidationException>(() =>
			{
				_logonMessageHandler.OnNewMessage(fixMessage);
			});

			ClassicAssert.IsTrue(ex.IsCritical());
			ClassicAssert.AreEqual(Tags.HeartBtInt, ex.GetProblemField().TagId);
			ClassicAssert.AreEqual(FixErrorCode.ValueIncorrectOutOfRangeForTag, ex.GetFixErrorCode());

			ClassicAssert.IsTrue(_testFixSession.Messages.Count > 0);
			var responseMessage = _testFixSession.Messages[0];
			ClassicAssert.AreEqual("3", responseMessage.GetTagValueAsString(Tags.MsgType));
			ClassicAssert.AreEqual("Negative heartbeat interval", responseMessage.GetTagValueAsString(58));
		}

		[Test]
		public virtual void TestInvalidHeartBtIntNotNumber()
		{
			var ex = ClassicAssert.Throws<MessageValidationException>(() =>
			{
				var fixMessage = new FixMessage();
				fixMessage.AddTag(Tags.BeginString, "FIX.4.4");
				fixMessage.AddTag(Tags.ResetSeqNumFlag, "Y");
				fixMessage.AddTag(Tags.HeartBtInt, "a");
				fixMessage.AddTag(Tags.MsgSeqNum, (long)1);

				_logonMessageHandler.OnNewMessage(fixMessage);
			});

			ClassicAssert.IsTrue(ex.IsCritical());
			ClassicAssert.AreEqual(Tags.HeartBtInt, ex.GetProblemField().TagId);
			ClassicAssert.AreEqual(FixErrorCode.IncorrectDataFormatForValue, ex.GetFixErrorCode());

			ClassicAssert.IsTrue(_testFixSession.Messages.Count > 0);
			var responseMessage = _testFixSession.Messages[0];
			ClassicAssert.AreEqual("3", responseMessage.GetTagValueAsString(Tags.MsgType));
			ClassicAssert.AreEqual("Incorrect or undefined heartbeat interval", responseMessage.GetTagValueAsString(58));
		}

		[Test]
		public virtual void TestInvalidHeartBtValue()
		{
			var ex = ClassicAssert.Throws<MessageValidationException>(() =>
			{
				var fixMessage = new FixMessage();
				fixMessage.AddTag(Tags.BeginString, "FIX.4.4");
				fixMessage.AddTag(Tags.ResetSeqNumFlag, "Y");
				//session has value 30 but set 1
				fixMessage.AddTag(Tags.HeartBtInt, "1");
				fixMessage.AddTag(Tags.MsgSeqNum, (long)1);

				_logonMessageHandler.OnNewMessage(fixMessage);
			});

			ClassicAssert.IsTrue(ex.IsCritical());
			ClassicAssert.AreEqual(Tags.HeartBtInt, ex.GetProblemField().TagId);
			ClassicAssert.AreEqual(FixErrorCode.ValueIncorrectOutOfRangeForTag, ex.GetFixErrorCode());

			ClassicAssert.IsTrue(_testFixSession.Messages.Count > 0);
			var responseMessage = _testFixSession.Messages[0];
			ClassicAssert.AreEqual("3", responseMessage.GetTagValueAsString(Tags.MsgType));
			ClassicAssert.AreEqual("Logon HeartBtInt(108) does not match value configured for session",
				responseMessage.GetTagValueAsString(58));
		}

		[Test]
		public virtual void TestAbsentHeartBtValue()
		{
			var ex = ClassicAssert.Throws<MessageValidationException>(() =>
			{
				var fixMessage = new FixMessage();
				fixMessage.AddTag(Tags.BeginString, "FIX.4.4");
				fixMessage.AddTag(Tags.ResetSeqNumFlag, "Y");
				//session has value 30 but set 1
				//fixMessage.AddTag(Tags.HeartBtInt, "1");
				fixMessage.AddTag(Tags.MsgSeqNum, (long)1);

				_logonMessageHandler.OnNewMessage(fixMessage);
			});

			ClassicAssert.IsTrue(ex.IsCritical());
			ClassicAssert.AreEqual(Tags.HeartBtInt, ex.GetProblemField().TagId);
			ClassicAssert.AreEqual(FixErrorCode.RequiredTagMissing, ex.GetFixErrorCode());

			ClassicAssert.IsTrue(_testFixSession.Messages.Count > 0);
			var responseMessage = _testFixSession.Messages[0];
			ClassicAssert.AreEqual("3", responseMessage.GetTagValueAsString(Tags.MsgType));
			ClassicAssert.AreEqual("Incorrect or undefined heartbeat interval", responseMessage.GetTagValueAsString(58));
		}
	}

}
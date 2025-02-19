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
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation.Error;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global
{
	[TestFixture]
	internal class SenderTargetIdConsistencyHandlerTest : AbstractDataLengthCheckHandlerTst
	{
		private SenderTargetIdConsistencyHandler _senderTargetHandler;
		private TestFixSession _testFixSession;

		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			_senderTargetHandler = new SenderTargetIdConsistencyHandler();
			_testFixSession = new TestFixSession();
			_senderTargetHandler.Session = _testFixSession;
			_senderTargetHandler.NextHandler = this;
		}

		[Test]
		public virtual void InvalidTargetProduceException()
		{
			var ex = ClassicAssert.Throws<MessageValidationException>(() =>
			{
				var message = new FixMessage();
				message.AddTag(8, "FIX.4.4");
				message.AddTag(35, "A");
				message.AddTag(Tags.TargetCompID, "invalid" + _testFixSession.Parameters.SenderCompId);
				message.AddTag(Tags.SenderCompID, _testFixSession.Parameters.TargetCompId);

				_senderTargetHandler.OnNewMessage(message);
			});

			ClassicAssert.IsTrue(ex.IsCritical());
			ClassicAssert.AreEqual(Tags.TargetCompID, ex.GetProblemField().TagId);
			ClassicAssert.AreEqual(FixErrorCode.CompidProblem, ex.GetFixErrorCode());

			ClassicAssertNoMessagePassedForNext();
			ClassicAssert.IsTrue(_testFixSession.Messages.Count > 0);
			var responseMessage = _testFixSession.Messages[0];
			ClassicAssert.AreEqual("3", responseMessage.GetTagValueAsString(35));
			ClassicAssert.AreEqual("Invalid SenderCompID or TargetCompID",
				responseMessage.GetTagValueAsString(Tags.Text));
			ClassicAssert.AreEqual(Tags.TargetCompID, responseMessage.GetTagValueAsLong(Tags.RefTagID));

			ClassicAssert.AreEqual("Invalid SenderCompID or TargetCompID", _testFixSession.DisconnectReason);
			ClassicAssert.AreEqual(SessionState.WaitingForLogoff, _testFixSession.SessionState);
			ClassicAssert.AreEqual(DisconnectReason.InvalidMessage, _testFixSession.LastDisconnectReason);
		}

		[Test]
		public virtual void InvalidSenderProduceException()
		{
			var ex = ClassicAssert.Throws<MessageValidationException>(() =>
			{
				var message = new FixMessage();
				message.AddTag(8, "FIX.4.4");
				message.AddTag(35, "A");
				message.AddTag(Tags.TargetCompID, _testFixSession.Parameters.SenderCompId);
				message.AddTag(Tags.SenderCompID, "invalid" + _testFixSession.Parameters.TargetCompId);

				_senderTargetHandler.OnNewMessage(message);
			});

			ClassicAssert.IsTrue(ex.IsCritical());
			ClassicAssert.AreEqual(Tags.SenderCompID, ex.GetProblemField().TagId);
			ClassicAssert.AreEqual(FixErrorCode.CompidProblem, ex.GetFixErrorCode());

			ClassicAssertNoMessagePassedForNext();
			ClassicAssert.IsTrue(_testFixSession.Messages.Count > 0);
			var responseMessage = _testFixSession.Messages[0];
			ClassicAssert.AreEqual("3", responseMessage.GetTagValueAsString(35));
			ClassicAssert.AreEqual("Invalid SenderCompID or TargetCompID",
				responseMessage.GetTagValueAsString(Tags.Text));
			ClassicAssert.AreEqual(Tags.SenderCompID, responseMessage.GetTagValueAsLong(Tags.RefTagID));

			ClassicAssert.AreEqual("Invalid SenderCompID or TargetCompID", _testFixSession.DisconnectReason);
			ClassicAssert.AreEqual(SessionState.WaitingForLogoff, _testFixSession.SessionState);
			ClassicAssert.AreEqual(DisconnectReason.InvalidMessage, _testFixSession.LastDisconnectReason);
		}

		[Test]
		public virtual void AbsentTargetProduceException()
		{
			var ex = ClassicAssert.Throws<MessageValidationException>(() =>
			{
				var message = new FixMessage();
				message.AddTag(8, "FIX.4.4");
				message.AddTag(35, "A");
				message.AddTag(Tags.SenderCompID, _testFixSession.Parameters.TargetCompId);

				_testFixSession.SessionState = SessionState.Dead;
				_senderTargetHandler.OnNewMessage(message);
			});

			ClassicAssert.IsFalse(ex.IsCritical());
			ClassicAssert.AreEqual(Tags.TargetCompID, ex.GetProblemField().TagId);
			ClassicAssert.AreEqual(FixErrorCode.RequiredTagMissing, ex.GetFixErrorCode());

			ClassicAssertNoMessagePassedForNext();
			ClassicAssert.IsTrue(_testFixSession.Messages.Count > 0);
			var responseMessage = _testFixSession.Messages[0];
			ClassicAssert.AreEqual("3", responseMessage.GetTagValueAsString(35));
			ClassicAssert.AreEqual(responseMessage.GetTagValueAsString(Tags.Text), "Missed SenderCompID or TargetCompID");
			ClassicAssert.AreEqual(Tags.TargetCompID, responseMessage.GetTagValueAsLong(Tags.RefTagID));

			//session state not changed
			ClassicAssert.IsNull(_testFixSession.DisconnectReason);
			ClassicAssert.AreEqual(SessionState.Dead, _testFixSession.SessionState);
		}

		[Test]
		public virtual void AbsentSenderProduceException()
		{
			var ex = ClassicAssert.Throws<MessageValidationException>(() =>
			{
				var message = new FixMessage();
				message.AddTag(8, "FIX.4.4");
				message.AddTag(35, "A");
				message.AddTag(Tags.TargetCompID, _testFixSession.Parameters.SenderCompId);

				_testFixSession.SessionState = SessionState.Dead;
				_senderTargetHandler.OnNewMessage(message);
			});

			ClassicAssert.IsFalse(ex.IsCritical());
			ClassicAssert.AreEqual(Tags.SenderCompID, ex.GetProblemField().TagId);
			ClassicAssert.AreEqual(FixErrorCode.RequiredTagMissing, ex.GetFixErrorCode());

			ClassicAssertNoMessagePassedForNext();
			ClassicAssert.IsTrue(_testFixSession.Messages.Count > 0);
			var responseMessage = _testFixSession.Messages[0];
			ClassicAssert.AreEqual("3", responseMessage.GetTagValueAsString(35));
			ClassicAssert.AreEqual("Missed SenderCompID or TargetCompID",
				responseMessage.GetTagValueAsString(Tags.Text));
			ClassicAssert.AreEqual(Tags.SenderCompID, responseMessage.GetTagValueAsLong(Tags.RefTagID));

			//session state not changed
			ClassicAssert.IsNull(_testFixSession.DisconnectReason);
			ClassicAssert.AreEqual(SessionState.Dead, _testFixSession.SessionState);
		}
	}
}
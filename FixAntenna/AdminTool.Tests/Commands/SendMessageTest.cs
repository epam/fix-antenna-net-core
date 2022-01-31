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

using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework;

namespace Epam.FixAntenna.AdminTool.Tests.Commands
{
	internal class SendMessageTest : AdminToolHelper
	{
		private FixAntenna.Fixicc.Message.SendMessage _sendMessage;

		[SetUp]
		public void Setup()
		{
			base.Before();
			_sendMessage = new FixAntenna.Fixicc.Message.SendMessage();
			RequestID = GetNextRequest();
			_sendMessage.RequestID = RequestID;

			FixSession = FindAdminSession();
		}

		[Test]
		public void TestSendMessage()
		{
			var extendedFIXSession = (AdminFixSession) GetSession(TestSessionID);

			_sendMessage.SenderCompID = extendedFIXSession.Parameters.SenderCompId;
			_sendMessage.TargetCompID = extendedFIXSession.Parameters.TargetCompId;
			var message = new FixMessage();
			message.AddTag(35, "AS");
			message.AddTag(112, "1233455");
			message.AddTag(58, "ok");
			message.AddTag(10, "001");
			var expectedMsg = message.ToUnmaskedString();
			_sendMessage.Message = message.AsByteArray();

			var response = GetReponse(_sendMessage);
			Assert.AreEqual(RequestID, response.RequestID);

			Assert.AreEqual(expectedMsg, extendedFIXSession.Message.ToUnmaskedString());
		}

		[Test]
		public void TestSendMessageSessionWithQualifier()
		{
			var extendedFIXSession = (AdminFixSession) GetSession(TestSessionIdQualifier);

			_sendMessage.SenderCompID = extendedFIXSession.Parameters.SenderCompId;
			_sendMessage.TargetCompID = extendedFIXSession.Parameters.TargetCompId;
			_sendMessage.SessionQualifier = extendedFIXSession.Parameters.SessionQualifier;
			var message = new FixMessage();
			message.AddTag(35, "AS");
			message.AddTag(112, "1233455");
			message.AddTag(58, "ok");
			message.AddTag(10, "001");
			var expectedMsg = message.ToString();
			_sendMessage.Message = message.AsByteArray();

			var response = GetReponse(_sendMessage);
			Assert.AreEqual(RequestID, response.RequestID);

			Assert.AreEqual(expectedMsg, extendedFIXSession.Message.ToUnmaskedString());
		}

		[Test]
		public void TestSendMessageWithInvalidCharacter()
		{
			var extendedFIXSession = (AdminFixSession) FixSessionManager.Instance.SessionListCopy[1];

			_sendMessage.SenderCompID = extendedFIXSession.Parameters.SenderCompId;
			_sendMessage.TargetCompID = extendedFIXSession.Parameters.TargetCompId;

			var message = new FixMessage();
			message.AddTag(35, "AS");
			message.AddTag(112, "1233455");
			message.AddTag(58, "#");
			message.AddTag(10, "001");
			_sendMessage.Message = message.ToString().AsByteArray();

			var response = GetReponse(_sendMessage);
			Assert.AreEqual(RequestID, response.RequestID);

			Assert.AreEqual(message.ToString(), extendedFIXSession.Message.ToUnmaskedString());
		}

		[Test]
		public void TestSendValidMessage()
		{
			// message got from bug - 15166.
			var message = "8=FIX.4.4\u00019=93\u000135=B\u000149=SNDR44\u000156=TRGT44\u000134=2\u000152=20100331-14:06:55.647\u0001148=Testing special symbols\u000133=1\u000158=#\u000110=185\u0001";
			var extendedFIXSession = (AdminFixSession) FixSessionManager.Instance.SessionListCopy[1];

			_sendMessage.SenderCompID = extendedFIXSession.Parameters.SenderCompId;
			_sendMessage.TargetCompID = extendedFIXSession.Parameters.TargetCompId;

			_sendMessage.Message = message.AsByteArray();

			var response = GetReponse(_sendMessage);
			Assert.AreEqual(RequestID, response.RequestID);

			Assert.AreEqual(message, extendedFIXSession.Message.ToUnmaskedString());
		}

		[Test]
		public void TestSendMessageWithInvalidCharacter1()
		{
			var message = "8=FIX.4.4&#01;9=056&#01;35=8&#01;49=THEM&#01;56=US&#01;37=1&#01;17=1&#01;34=530&#01;11=00492-0476&#01;150=0&#01;39=0&#01;10=163&#01;";

			var extendedFIXSession = (AdminFixSession) FixSessionManager.Instance.SessionListCopy[1];

			_sendMessage.SenderCompID = extendedFIXSession.Parameters.SenderCompId;
			_sendMessage.TargetCompID = extendedFIXSession.Parameters.TargetCompId;

			var fieldListMessage = RawFixUtil.GetFixMessage(message.ReplaceAll(AdminConstants.SendMessageDelimiter, "\u0001").AsByteArray());
			_sendMessage.Message = XmlHelper.ProtectSpecialCharacters(fieldListMessage.ToString()).AsByteArray();

			var response = GetReponse(_sendMessage);
			Assert.AreEqual(RequestID, response.RequestID);

			Assert.AreEqual(fieldListMessage.ToString(), extendedFIXSession.Message.ToUnmaskedString());
		}

		[Test]
		public void TestInvalidSender()
		{
			var extendedFIXSession = (AdminFixSession) FixSessionManager.Instance.SessionListCopy[1];

			_sendMessage.TargetCompID = extendedFIXSession.Parameters.TargetCompId;

			var response = GetReponse(_sendMessage);
			Assert.AreEqual(RequestID, response.RequestID);
			Assert.AreEqual(ResultCode.OperationInvalidArgument.Code, response.ResultCode);
			Assert.That(response.Description, Does.Contain("SenderCompID is required"));
		}

		[Test]
		public void TestInvalidTarget()
		{
			var extendedFIXSession = (AdminFixSession) FixSessionManager.Instance.SessionListCopy[1];

			_sendMessage.SenderCompID = extendedFIXSession.Parameters.SenderCompId;

			_sendMessage.Message = "".AsByteArray();

			var response = GetReponse(_sendMessage);
			Assert.AreEqual(RequestID, response.RequestID);
			Assert.AreEqual(ResultCode.OperationInvalidArgument.Code, response.ResultCode);
			Assert.That(response.Description, Does.Contain("TargetCompID is required"));
		}

		[Test]
		public void TestInvalidMessage()
		{
			var extendedFIXSession = (AdminFixSession) FixSessionManager.Instance.SessionListCopy[1];

			_sendMessage.TargetCompID = extendedFIXSession.Parameters.TargetCompId;
			_sendMessage.SenderCompID = extendedFIXSession.Parameters.SenderCompId;

			_sendMessage.Message = "".AsByteArray();

			var response = GetReponse(_sendMessage);
			Assert.AreEqual(RequestID, response.RequestID);
			Assert.AreEqual(ResultCode.OperationInvalidArgument.Code, response.ResultCode);
			Assert.That(response.Description, Does.Contain("Message is required"));
		}
	}
}
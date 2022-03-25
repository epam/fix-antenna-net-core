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

using Epam.FixAntenna.Fixicc.Message;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using NUnit.Framework;

namespace Epam.FixAntenna.AdminTool.Tests.Commands
{
	internal class TestRequestTest : AdminToolHelper
	{
		private TestRequest _testRequest;
		private AdminFixSession _extendedFIXSession;

		[SetUp]
		public override void Before()
		{
			base.Before();
			FixSession = FindAdminSession();

			_testRequest = new TestRequest();

			RequestID = GetNextRequest();
			_testRequest.RequestID = RequestID;

			_extendedFIXSession = (AdminFixSession) FixSessionManager.Instance.SessionListCopy[1];
		}

		[Test]
		public void TestTestRequest()
		{
			var session = (AdminFixSession)GetSession(TestSessionID);
			_testRequest.SenderCompID = session.Parameters.SenderCompId;
			_testRequest.TargetCompID = session.Parameters.TargetCompId;
			_testRequest.TestReqID = "2";

			var response = GetReponse(_testRequest);
			Assert.AreEqual(RequestID, response.RequestID);
			Assert.AreEqual(ResultCode.OperationSuccess.Code, response.ResultCode);

			Assert.IsNotNull(session.Message);

			Assert.AreEqual("1", session.Message.GetTagValueAsString(35));
			Assert.AreEqual("2", session.Message.GetTagValueAsString(112));
		}

		[Test]
		public void TestTestRequestSessionWithQualifier()
		{
			var session = (AdminFixSession)GetSession(TestSessionIdQualifier);
			_testRequest.SenderCompID = session.Parameters.SenderCompId;
			_testRequest.TargetCompID = session.Parameters.TargetCompId;
			_testRequest.SessionQualifier = session.Parameters.SessionQualifier;
			_testRequest.TestReqID = "2";

			var response = GetReponse(_testRequest);
			Assert.AreEqual(RequestID, response.RequestID);
			Assert.AreEqual(ResultCode.OperationSuccess.Code, response.ResultCode);

			Assert.IsNotNull(session.Message);

			Assert.AreEqual("1", session.Message.GetTagValueAsString(35));
			Assert.AreEqual("2", session.Message.GetTagValueAsString(112));
		}

		[Test]
		public void TestInvalidTestRequest()
		{
			_testRequest.SenderCompID = _extendedFIXSession.Parameters.SenderCompId;
			_testRequest.TargetCompID = _extendedFIXSession.Parameters.TargetCompId;

			var response = GetReponse(_testRequest);
			Assert.AreEqual(RequestID, response.RequestID);
			Assert.AreEqual(ResultCode.OperationInvalidArgument.Code, response.ResultCode, response.Description);
		}

		[Test]
		public void TestInvalidSender()
		{
			_testRequest.TargetCompID = _extendedFIXSession.Parameters.TargetCompId;
			_testRequest.TestReqID = "11";

			var response = GetReponse(_testRequest);
			Assert.AreEqual(RequestID, response.RequestID);
			Assert.AreEqual(ResultCode.OperationInvalidArgument.Code, response.ResultCode, response.Description);
			Assert.That(response.Description, Does.Contain("SenderCompID is required"));
		}

		[Test]
		public void TestInvalidTarget()
		{
			_testRequest.SenderCompID = _extendedFIXSession.Parameters.SenderCompId;
			_testRequest.TestReqID = "22";

			var response = GetReponse(_testRequest);
			Assert.AreEqual(RequestID, response.RequestID);
			Assert.AreEqual(ResultCode.OperationInvalidArgument.Code, response.ResultCode, response.Description);
			Assert.That(response.Description, Does.Contain("TargetCompID is required"));
		}
	}
}
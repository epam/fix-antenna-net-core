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
using NUnit.Framework;

namespace Epam.FixAntenna.AdminTool.Tests.Commands
{
	internal class StopSessionTest : AdminToolHelper
	{
		private StopSession _stopSession;

		[SetUp]
		public void Setup()
		{
			base.Before();
			_stopSession = new StopSession();
			_stopSession.LogoutReason = "";
			RequestID = GetNextRequest();
			_stopSession.RequestID = RequestID;

			FixSession = FindAdminSession();
		}

		[Test]
		public void TestStopSession()
		{
			var session = (AdminFixSession)GetSession(TestSessionID);
			_stopSession.SenderCompID = session.Parameters.SenderCompId;
			_stopSession.TargetCompID = session.Parameters.TargetCompId;

			var response = GetReponse(_stopSession);
			Assert.AreEqual(RequestID, response.RequestID);
			Assert.AreEqual(ResultCode.OperationSuccess.Code, response.ResultCode);
			// is it correct to dispose session on stop command?
			Assert.IsTrue(session.IsDisposed());
		}

		[Test]
		public void TestStopSessionWithQualifier()
		{
			var session = (AdminFixSession)GetSession(TestSessionIdQualifier);
			_stopSession.SenderCompID = session.Parameters.SenderCompId;
			_stopSession.TargetCompID = session.Parameters.TargetCompId;
			_stopSession.SessionQualifier = session.Parameters.SessionQualifier;

			var response = GetReponse(_stopSession);
			Assert.AreEqual(RequestID, response.RequestID);
			Assert.AreEqual(ResultCode.OperationSuccess.Code, response.ResultCode);
			// is it correct to dispose session on stop command?
			Assert.IsTrue(session.IsDisposed());
		}


		[Test]
		public void TestInvalidSender()
		{
			_stopSession.TargetCompID = FixSession.Parameters.TargetCompId;

			var response = GetReponse(_stopSession);
			Assert.AreEqual(RequestID, response.RequestID);
			Assert.AreEqual(ResultCode.OperationInvalidArgument.Code, response.ResultCode);
			Assert.That(response.Description, Does.Contain("SenderCompID is required"));
		}

		[Test]
		public void TestInvalidTarget()
		{
			_stopSession.SenderCompID = FixSession.Parameters.SenderCompId;

			var response = GetReponse(_stopSession);
			Assert.AreEqual(RequestID, response.RequestID);
			Assert.AreEqual(ResultCode.OperationInvalidArgument.Code, response.ResultCode);
			Assert.That(response.Description, Does.Contain("TargetCompID is required"));
		}
	}
}
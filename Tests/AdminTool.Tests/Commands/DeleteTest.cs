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
using Epam.FixAntenna.NetCore.FixEngine;
using NUnit.Framework;

namespace Epam.FixAntenna.AdminTool.Tests.Commands
{
	internal class DeleteTest : AdminToolHelper
	{
		private Delete _delete;

		[SetUp]
		public void Setup()
		{
			Before();
			RequestID = GetNextRequest();
			_delete = new Delete { LogoutReason = string.Empty, RequestID = RequestID };
			FixSession = FindAdminSession();
		}

		[Test]
		public void TestDelete()
		{
			_delete.SenderCompID = TestSessionID.Sender;
			_delete.TargetCompID = TestSessionID.Target;

			IFixSession session = GetSession(TestSessionID);
			Assert.IsNotNull(session);

			var response = GetReponse(_delete);
			Assert.AreEqual(RequestID, response.RequestID);
			Assert.IsTrue(((AdminFixSession) session).IsDisposed());
			Assert.AreEqual(ResultCode.OperationSuccess.Code, response.ResultCode);
		}

		[Test]
		public void TestDeleteWithQualifier()
		{
			_delete.SenderCompID = TestSessionIdQualifier.Sender;
			_delete.TargetCompID = TestSessionIdQualifier.Target;
			_delete.SessionQualifier = TestSessionIdQualifier.Qualifier;

			IFixSession session = GetSession(TestSessionIdQualifier);
			Assert.IsNotNull(session);

			var response = GetReponse(_delete);
			Assert.AreEqual(RequestID, response.RequestID);
			Assert.IsTrue(((AdminFixSession)session).IsDisposed());
			Assert.AreEqual(ResultCode.OperationSuccess.Code, response.ResultCode);
		}

		[Test]
		public void TestInvalidSender()
		{
			_delete.TargetCompID = TestSessionID.Target;

			var response = GetReponse(_delete);
			Assert.AreEqual(RequestID, response.RequestID);
			Assert.AreEqual(ResultCode.OperationInvalidArgument.Code, response.ResultCode);
			Assert.That(response.Description, Does.Contain("SenderCompID is required"));
		}

		[Test]
		public void TestInvalidTarget()
		{
			_delete.SenderCompID = FixSession.Parameters.SenderCompId;

			var response = GetReponse(_delete);
			Assert.AreEqual(RequestID, response.RequestID);
			Assert.AreEqual(ResultCode.OperationInvalidArgument.Code, response.ResultCode);
			Assert.That(response.Description, Does.Contain("TargetCompID is required"));
		}
	}
}
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
using Epam.FixAntenna.NetCore.FixEngine.Session;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.AdminTool.Tests.Commands
{
	internal class SessionStatusTest : AdminToolHelper
	{
		private SessionStatus _sessionParams;

		[SetUp]
		public override void Before()
		{
			base.Before();

			FixSession = FindAdminSession();
			_sessionParams = new SessionStatus();

			RequestID = GetNextRequest();
			_sessionParams.RequestID = RequestID;
		}

		[Test]
		public void TestSessionStatus()
		{
			var session = GetSession(TestSessionID);
			_sessionParams.SenderCompID = TestSessionID.Sender;
			_sessionParams.TargetCompID = TestSessionID.Target;

			var response = GetReponse(_sessionParams);
			// check response
			ClassicAssert.AreEqual(ResultCode.OperationSuccess.Code, response.ResultCode);
			ClassicAssert.AreEqual(RequestID, response.RequestID);
			ClassicAssert.IsNotNull(response.SessionStatusData);

			var sessionStatusData = response.SessionStatusData;

			// sender and taret
			ClassicAssert.AreEqual(TestSessionID.Sender, sessionStatusData.SenderCompID);
			ClassicAssert.AreEqual(TestSessionID.Target, sessionStatusData.TargetCompID);

			// session type
			ClassicAssert.AreEqual((long?) session.RuntimeState.InSeqNum, sessionStatusData.InSeqNum);
			ClassicAssert.AreEqual((long?)(session.RuntimeState.OutSeqNum - 1), sessionStatusData.OutSeqNum);
			ClassicAssert.AreEqual(StatusGroup.ESTABLISHED, sessionStatusData.StatusGroup);
			ClassicAssert.AreEqual(session.SessionState.ToString(), sessionStatusData.Status);
			if (session is InitiatorFixSession)
			{
				ClassicAssert.AreEqual(BackupState.PRIMARY, sessionStatusData.BackupState);
			}
		}

		[Test]
		public void TestSessionStatusSessionWithQualifier()
		{
			var session = GetSession(TestSessionIdQualifier);
			_sessionParams.SenderCompID = TestSessionIdQualifier.Sender;
			_sessionParams.TargetCompID = TestSessionIdQualifier.Target;

			var response = GetReponse(_sessionParams);

			// check response
			ClassicAssert.AreEqual(ResultCode.OperationSuccess.Code, response.ResultCode);
			ClassicAssert.AreEqual(RequestID, response.RequestID);
			ClassicAssert.IsNotNull(response.SessionStatusData);

			var sessionStatusData = response.SessionStatusData;
			// sender and taret

			ClassicAssert.AreEqual(TestSessionIdQualifier.Sender, sessionStatusData.SenderCompID);
			ClassicAssert.AreEqual(TestSessionIdQualifier.Target, sessionStatusData.TargetCompID);

			// session type
			ClassicAssert.AreEqual((long?) session.RuntimeState.InSeqNum, sessionStatusData.InSeqNum);
			ClassicAssert.AreEqual((long?)(session.RuntimeState.OutSeqNum - 1), sessionStatusData.OutSeqNum);
			ClassicAssert.AreEqual(StatusGroup.ESTABLISHED, sessionStatusData.StatusGroup);
			ClassicAssert.AreEqual(session.SessionState.ToString(), sessionStatusData.Status);
			if (session is InitiatorFixSession)
			{
				ClassicAssert.AreEqual(BackupState.PRIMARY, sessionStatusData.BackupState);
			}
		}
	}
}
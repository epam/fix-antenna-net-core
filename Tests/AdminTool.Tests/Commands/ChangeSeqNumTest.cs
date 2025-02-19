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
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.AdminTool.Tests.Commands
{
	internal class ChangeSeqNumTest : AdminToolHelper
	{
		private ChangeSeqNum _changeSeqNum;

		[SetUp]
		public override void Before()
		{
			base.Before();
			FixSession = FindAdminSession();

			_changeSeqNum = new ChangeSeqNum();

			_changeSeqNum.SenderCompID = TestSessionID.Sender;
			_changeSeqNum.TargetCompID = TestSessionID.Target;

			RequestID = GetNextRequest();
			_changeSeqNum.RequestID = RequestID;
		}

		[Test]
		public void TestChangeSeqNum()
		{
			var session = GetSession(TestSessionID);
			_changeSeqNum.OutSeqNum = 10L;
			_changeSeqNum.InSeqNum = 10L;

			var response = GetReponse(_changeSeqNum);

			ClassicAssert.AreEqual(10, session.RuntimeState.OutSeqNum, "OutgoingSequenceNumber is not changed");
			ClassicAssert.AreEqual(10, session.RuntimeState.InSeqNum, "IncomingSequenceNumber is not changed");

			ClassicAssert.AreEqual(RequestID, response.RequestID);
			ClassicAssert.AreEqual(ResultCode.OperationSuccess.Code, response.ResultCode);
		}

		[Test]
		public void TestChangeSeqNumSessionWithQualifier()
		{
			var sessionWithQ = GetSession(TestSessionIdQualifier);
			var session = GetSession(TestSessionID);
			var inSeqNumSessionWithoutQ = session.RuntimeState.InSeqNum;
			var outSeqNumSessionWithoutQ = session.RuntimeState.OutSeqNum;

			_changeSeqNum.SenderCompID = TestSessionIdQualifier.Sender;
			_changeSeqNum.TargetCompID = TestSessionIdQualifier.Target;
			_changeSeqNum.SessionQualifier = TestSessionIdQualifier.Qualifier;
			_changeSeqNum.OutSeqNum = 10L;
			_changeSeqNum.InSeqNum = 10L;

			var response = GetReponse(_changeSeqNum);

			ClassicAssert.AreEqual(10, sessionWithQ.RuntimeState.OutSeqNum, "OutgoingSequenceNumber is not changed");
			ClassicAssert.AreEqual(10, sessionWithQ.RuntimeState.InSeqNum, "IncomingSequenceNumber is not changed");

			ClassicAssert.AreEqual(outSeqNumSessionWithoutQ, session.RuntimeState.OutSeqNum, "Command ChangeSeqNum was affect to other session");
			ClassicAssert.AreEqual(inSeqNumSessionWithoutQ, session.RuntimeState.InSeqNum, "Command ChangeSeqNum was affect to other session");

			ClassicAssert.AreEqual(RequestID, response.RequestID);
			ClassicAssert.AreEqual(ResultCode.OperationSuccess.Code, response.ResultCode);
		}

		[Test]
		public void TestChangeSeqNumEmptyCompIds()
		{
			_changeSeqNum.SenderCompID = null;
			_changeSeqNum.TargetCompID = null;
			_changeSeqNum.InSeqNum = 1L;
			_changeSeqNum.OutSeqNum = 1L;
			var response = GetReponse(_changeSeqNum);
			ClassicAssert.AreEqual(RequestID, response.RequestID);
			ClassicAssert.AreEqual(ResultCode.OperationInvalidArgument.Code, response.ResultCode);
			ClassicAssert.That(response.Description, Does.Contain("SenderCompID is required"));
		}

		[Test]
		public void TestChangeSeqNumUnknownSession()
		{
			_changeSeqNum.SenderCompID = "SUnknown";
			_changeSeqNum.TargetCompID = "TUnknown";
			_changeSeqNum.InSeqNum = 1L;
			_changeSeqNum.OutSeqNum = 1L;
			var response = GetReponse(_changeSeqNum);
			ClassicAssert.AreEqual(RequestID, response.RequestID);
			ClassicAssert.AreEqual(ResultCode.ResultUnknownSession.Code, response.ResultCode);
			ClassicAssert.That(response.Description, Does.Contain("Failed to execute `ChangeSeqNum` command: Unknown session: SUnknown-TUnknown."));
		}

		[Test]
		public void TestChangeSeqNumWithEmptyParameter()
		{
			var response = GetReponse(_changeSeqNum);
			ClassicAssert.AreEqual(RequestID, response.RequestID);
			ClassicAssert.AreEqual(ResultCode.OperationSuccess.Code, response.ResultCode);
			ClassicAssert.That(response.Description, Does.Contain("completed."));
		}

		[Test]
		public void TestChangeSeqNumInvalidNegativeArgument()
		{
			_changeSeqNum.InSeqNum = -100L;
			_changeSeqNum.OutSeqNum = -1000L;

			var response = GetReponse(_changeSeqNum);
			ClassicAssert.AreEqual(RequestID, response.RequestID);
			ClassicAssert.AreEqual(ResultCode.OperationInvalidArgument.Code, response.ResultCode);
		}
	}
}
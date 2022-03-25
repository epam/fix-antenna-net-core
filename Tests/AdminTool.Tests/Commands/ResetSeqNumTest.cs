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

using NUnit.Framework;

namespace Epam.FixAntenna.AdminTool.Tests.Commands
{
	internal class ResetSeqNumTest : AdminToolHelper
	{
		private FixAntenna.Fixicc.Message.ResetSeqNum _changeSeqNum;

		[SetUp]
		public void Setup()
		{
			base.Before();
			_changeSeqNum = new FixAntenna.Fixicc.Message.ResetSeqNum();
			RequestID = GetNextRequest();
			_changeSeqNum.RequestID = RequestID;

			FixSession = FindAdminSession();
		}

		[Test]
		public void TestResetSeqNum()
		{
			_changeSeqNum.SenderCompID = TestSessionID.Sender;
			_changeSeqNum.TargetCompID = TestSessionID.Target;

			var response = GetReponse(_changeSeqNum);
			Assert.AreEqual(RequestID, response.RequestID);
			Assert.AreEqual(ResultCode.OperationSuccess.Code, response.ResultCode);
		}

		[Test]
		public void TestResetSeqNumSessionWithQualifier()
		{
			_changeSeqNum.SenderCompID = TestSessionIdQualifier.Sender;
			_changeSeqNum.TargetCompID = TestSessionIdQualifier.Target;
			_changeSeqNum.SessionQualifier = TestSessionIdQualifier.Qualifier;

			var response = GetReponse(_changeSeqNum);
			Assert.AreEqual(RequestID, response.RequestID);
			Assert.AreEqual(ResultCode.OperationSuccess.Code, response.ResultCode);
		}

		[Test]
		public void TestResetSeqNumFailed()
		{
			var response = GetReponse(_changeSeqNum);
			Assert.AreEqual(RequestID, response.RequestID);
			Assert.AreEqual(ResultCode.OperationInvalidArgument.Code, response.ResultCode);
		}
	}
}
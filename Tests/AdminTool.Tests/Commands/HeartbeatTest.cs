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
	internal class HeartbeatTest : AdminToolHelper
	{
		private Heartbeat _heartbeat;

		[SetUp]
		public void Setup()
		{
			base.Before();
			_heartbeat = new Heartbeat();
			RequestID = GetNextRequest();
			_heartbeat.RequestID = RequestID;

			FixSession = FindAdminSession();
		}

		[Test]
		public void TestHeartbeat()
		{
			var extendedFIXSession = (AdminFixSession) GetSession(TestSessionID);

			_heartbeat.SenderCompID = TestSessionID.Sender;
			_heartbeat.TargetCompID = TestSessionID.Target;

			var response = GetReponse(_heartbeat);
			ClassicAssert.AreEqual(RequestID, response.RequestID);
			ClassicAssert.AreEqual(ResultCode.OperationSuccess.Code, response.ResultCode);

			ClassicAssert.IsNotNull(extendedFIXSession.Message);

			ClassicAssert.AreEqual(extendedFIXSession.Message.GetTagValueAsString(35), "0");
		}

		[Test]
		public void TestHeartbeatSessionWithQalifier()
		{
			var extendedFIXSession = (AdminFixSession) GetSession(TestSessionIdQualifier);

			_heartbeat.SenderCompID = TestSessionIdQualifier.Sender;
			_heartbeat.TargetCompID = TestSessionIdQualifier.Target;
			_heartbeat.SessionQualifier = TestSessionIdQualifier.Qualifier;

			var response = GetReponse(_heartbeat);
			ClassicAssert.AreEqual(RequestID, response.RequestID);
			ClassicAssert.AreEqual(ResultCode.OperationSuccess.Code, response.ResultCode);

			ClassicAssert.IsNotNull(extendedFIXSession.Message);

			ClassicAssert.AreEqual(extendedFIXSession.Message.GetTagValueAsString(35), "0");
		}
	}
}
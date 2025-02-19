﻿// Copyright (c) 2021 EPAM Systems
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
	internal class SessionStatTest : AdminToolHelper
	{
		private SessionStat _sessionStat;

		[SetUp]
		public override void Before()
		{
			base.Before();

			_sessionStat = new SessionStat();
			RequestID = GetNextRequest();
			_sessionStat.RequestID = RequestID;

			FixSession = FindAdminSession();
		}

		[Test]
		public void TestSessionStat()
		{
			_sessionStat.SenderCompID = TestSessionID.Sender;
			_sessionStat.TargetCompID = TestSessionID.Target;

			var response = GetReponse(_sessionStat);

			var sessionStatData = response.SessionStatData;
			ClassicAssert.AreEqual(TestSessionID.Sender, sessionStatData.SenderCompID);
			ClassicAssert.AreEqual(TestSessionID.Target, sessionStatData.TargetCompID);
			ClassicAssert.IsNotNull(sessionStatData.Established);
			ClassicAssert.IsNotNull(sessionStatData.NumOfProcessedMessages);
			ClassicAssert.IsTrue(sessionStatData.ReceivedBytes > 0);
			ClassicAssert.IsTrue(sessionStatData.SentBytes > 0);
		}

		[Test]
		public void TestSessionStatSessionWithQualifier()
		{
			_sessionStat.SenderCompID = TestSessionIdQualifier.Sender;
			_sessionStat.TargetCompID = TestSessionIdQualifier.Target;
			_sessionStat.SessionQualifier = TestSessionIdQualifier.Qualifier;

			var response = GetReponse(_sessionStat);

			var sessionStatData = response.SessionStatData;
			ClassicAssert.AreEqual(TestSessionIdQualifier.Sender, sessionStatData.SenderCompID);
			ClassicAssert.AreEqual(TestSessionIdQualifier.Target, sessionStatData.TargetCompID);
			ClassicAssert.AreEqual(TestSessionIdQualifier.Qualifier, sessionStatData.SessionQualifier);
			ClassicAssert.IsNotNull(sessionStatData.Established);
			ClassicAssert.IsNotNull(sessionStatData.NumOfProcessedMessages);
			ClassicAssert.IsTrue(sessionStatData.ReceivedBytes > 0);
			ClassicAssert.IsTrue(sessionStatData.SentBytes > 0);
		}
	}
}
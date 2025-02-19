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
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.AdminTool.Tests.Commands
{
	internal class SessionListTest : AdminToolHelper
	{
		private SessionsList _sessionsList;

		[SetUp]
		public void Setup()
		{
			base.Before();
			_sessionsList = new SessionsList();
			RequestID = GetNextRequest();
			_sessionsList.RequestID = RequestID;

			FixSession = FindAdminSession();
		}

		[Test]
		public void TestSessionsList()
		{
			_sessionsList.SubscriptionRequestType = SubscriptionRequestType.Item1;
			var response = GetReponse(_sessionsList);

			var sessionsListData = response.SessionsListData;
			ClassicAssert.AreEqual(sessionsListData.Session.Count, FixSessionManager.Instance.SessionListCopy.Count);
			SessionsListDataSession senderSession1 = null;
			SessionsListDataSession senderSession2 = null;
			foreach (var session in sessionsListData.Session)
			{
				ClassicAssert.AreEqual(Action.NEW, session.Action);
				ClassicAssert.AreEqual(StatusGroup.ESTABLISHED, session.StatusGroup);
				ClassicAssert.IsNotNull(session.Timestamp);
				ClassicAssert.IsNotNull(session.Status);

				if (TestSessionID.Sender.Equals(session.SenderCompID)
						&& TestSessionID.Target.Equals(session.TargetCompID)
						&& TestSessionID.Qualifier == session.SessionQualifier)
				{ // qualifier must be null
					senderSession1 = session;
				}
				if (TestSessionIdQualifier.Sender.Equals(session.SenderCompID)
						&& TestSessionIdQualifier.Target.Equals(session.TargetCompID)
						&& TestSessionIdQualifier.Qualifier.Equals(session.SessionQualifier))
				{
					senderSession2 = session;
				}
			}

			ClassicAssert.IsNotNull(senderSession1, "Can't find sender session in received response.");
			ClassicAssert.AreEqual(GetSession(TestSessionID).SessionState.ToString(), senderSession1.Status);

			ClassicAssert.IsNotNull(senderSession2, "Can't find sender session in received response.");
			ClassicAssert.AreEqual(GetSession(TestSessionIdQualifier).SessionState.ToString(), senderSession2.Status);
		}
	}
}
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
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.AdminTool.Tests.Commands
{
	/// <summary>
	/// Test class for SessionSnapshot command
	/// </summary>
	internal class SessionSnapshotTest : AdminToolHelper
	{
		private SessionsSnapshot _sessionsSnapshot;

		[SetUp]
		public void Setup()
		{
			base.Before();
			_sessionsSnapshot = new SessionsSnapshot();
			RequestID = GetNextRequest();
			_sessionsSnapshot.RequestID = RequestID;

			FixSession = FindAdminSession();
		}

		[Test]
		public void TestSessionsSnapshotStatus()
		{
			_sessionsSnapshot.View = View.STATUS;

			var response = GetReponse(_sessionsSnapshot);
			ClassicAssert.AreEqual(RequestID, response.RequestID);
			ClassicAssert.IsNotNull(response.SessionsSnapshotData);

			var receivedStatData = response.SessionsSnapshotData;
			ClassicAssert.IsNotNull(receivedStatData.Session);
			ClassicAssert.AreEqual(FixSessionManager.Instance.SessionsCount, receivedStatData.Session.Count);
			foreach (var session in receivedStatData.Session)
			{
				var statusData = session.StatusData;
				ClassicAssert.IsNotNull(statusData);
				ClassicAssert.IsTrue(statusData.InSeqNum > 0);
				ClassicAssert.IsTrue(statusData.OutSeqNum > 0);
				ClassicAssert.IsNotNull(statusData.BackupState);
				ClassicAssert.AreEqual(BackupState.PRIMARY, statusData.BackupState);
				ClassicAssert.AreEqual(SessionState.Connected.ToString(), statusData.Status);
			}
		}

		[Test]
		public void TestSessionsSnapshotStatusParams()
		{
			_sessionsSnapshot.View = View.STATUS_PARAMS;
			var response = GetReponse(_sessionsSnapshot);
			ClassicAssert.AreEqual(RequestID, response.RequestID);
			ClassicAssert.IsNotNull(response.SessionsSnapshotData);

			var receivedStatData = response.SessionsSnapshotData;
			ClassicAssert.IsNotNull(receivedStatData.Session);
			ClassicAssert.AreEqual(FixSessionManager.Instance.SessionsCount, receivedStatData.Session.Count);
			foreach (var session in receivedStatData.Session)
			{
				var paramsData = session.ParamsData;
				ClassicAssert.IsNotNull(paramsData);
				ClassicAssert.AreEqual(FIXVersion.FIX44.ToString(), paramsData.Version);
				ClassicAssert.IsNotNull(paramsData.ExtraSessionParams);
				var sessionParams = paramsData.ExtraSessionParams;
				ClassicAssert.IsNotNull(paramsData.Role);
				if (paramsData.Role == SessionRole.INITIATOR)
				{
					ClassicAssert.IsNotNull(paramsData.RemoteHost);
					ClassicAssert.IsNotNull(paramsData.RemotePort);
					ClassicAssert.IsNull(paramsData.Backup);
					ClassicAssert.IsNotNull(sessionParams.HBI);
					ClassicAssert.AreEqual(new int?(30), sessionParams.HBI);
				}
				ClassicAssert.IsNotNull(sessionParams.InSeqNum);
				ClassicAssert.IsTrue(sessionParams.InSeqNum > 0);
				ClassicAssert.IsNotNull(sessionParams.OutSeqNum);
				ClassicAssert.IsTrue(sessionParams.OutSeqNum > 0);
	//            ClassicAssertNotNull(sessionParams.getPassword());
	//            ClassicAssertEquals(StorageType.TRANSIENT, sessionParams.getStorageType());
			}
		}

		[Test]
		public void TestSessionsSnapshotStatParams()
		{
			_sessionsSnapshot.View = View.STATUS_PARAMS_STAT;

			var response = GetReponse(_sessionsSnapshot);
			ClassicAssert.AreEqual(RequestID, response.RequestID);

			var receivedStatData = response.SessionsSnapshotData;
			ClassicAssert.IsNotNull(receivedStatData.Session);
			ClassicAssert.AreEqual(FixSessionManager.Instance.SessionsCount, receivedStatData.Session.Count);
			foreach (var session in receivedStatData.Session)
			{
				var statData = session.StatData;
				ClassicAssert.IsNotNull(statData);
				ClassicAssert.IsNotNull(statData.LastReceivedMessage);
				ClassicAssert.IsNotNull(statData.LastSentMessage);
				ClassicAssert.IsNotNull(statData.Established);
			}
		}

		[Test]
		public void TestSessionsSnapshotStatusWithStatParams()
		{
			_sessionsSnapshot.View = View.STATUS;

			var sessionView = new SessionsSnapshotSessionView();
			sessionView.View = View.STATUS_PARAMS_STAT;
			sessionView.SenderCompID = FixSession.Parameters.SenderCompId;
			sessionView.TargetCompID = FixSession.Parameters.TargetCompId;
			_sessionsSnapshot.SessionView.Add(sessionView);

			var response = GetReponse(_sessionsSnapshot);
			ClassicAssert.AreEqual(RequestID, response.RequestID);

			var receivedStatData = response.SessionsSnapshotData;
			ClassicAssert.IsNotNull(receivedStatData.Session);
			ClassicAssert.AreEqual(FixSessionManager.Instance.SessionsCount, receivedStatData.Session.Count);
			StatData statData = null;
			foreach (var session in receivedStatData.Session)
			{
				ClassicAssert.IsNotNull(session.StatusData);
				if (session.SenderCompID.Equals(FixSession.Parameters.SenderCompId)
						&& session.TargetCompID.Equals(FixSession.Parameters.TargetCompId))
				{
					statData = session.StatData;
					ClassicAssert.IsNull(session.ParamsData);
				}
			}
			ClassicAssert.IsNotNull(statData);
		}

		[Test]
		public void TestSessionsSnapshotStatusWithStatParamsSessionWithQualifier()
		{
			_sessionsSnapshot.View = View.STATUS;

			var sessionView = new SessionsSnapshotSessionView();
			sessionView.View = View.STATUS_PARAMS_STAT;
			sessionView.SenderCompID = TestSessionIdQualifier.Sender;
			sessionView.TargetCompID = TestSessionIdQualifier.Target;
			sessionView.SessionQualifier = TestSessionIdQualifier.Qualifier;
			_sessionsSnapshot.SessionView.Add(sessionView);

			var response = GetReponse(_sessionsSnapshot);
			ClassicAssert.AreEqual(RequestID, response.RequestID);

			var receivedStatData = response.SessionsSnapshotData;
			ClassicAssert.IsNotNull(receivedStatData.Session);
			ClassicAssert.AreEqual(FixSessionManager.Instance.SessionsCount, receivedStatData.Session.Count);
			StatData statData = null;
			foreach (var session in receivedStatData.Session)
			{
				ClassicAssert.IsNotNull(session.StatusData);
				if (session.SenderCompID.Equals(TestSessionIdQualifier.Sender)
						&& session.TargetCompID.Equals(TestSessionIdQualifier.Target)
						&& IsEquals(session.SessionQualifier, TestSessionIdQualifier.Qualifier))
				{
					statData = session.StatData;
					ClassicAssert.IsNull(session.ParamsData);
				}
			}
			ClassicAssert.IsNotNull(statData);
		}

		[Test]
		public void TestSessionsSnapshotStatusParamsWithStatData()
		{
			_sessionsSnapshot.View = View.STATUS_PARAMS;

			var sessionView = new SessionsSnapshotSessionView();
			sessionView.View = View.STATUS_PARAMS_STAT;
			sessionView.SenderCompID = FixSession.Parameters.SenderCompId;
			sessionView.TargetCompID = FixSession.Parameters.TargetCompId;
			_sessionsSnapshot.SessionView.Add(sessionView);

			var response = GetReponse(_sessionsSnapshot);
			ClassicAssert.AreEqual(RequestID, response.RequestID);

			var receivedStatData = response.SessionsSnapshotData;
			ClassicAssert.IsNotNull(receivedStatData.Session);
			ClassicAssert.AreEqual(FixSessionManager.Instance.SessionsCount, receivedStatData.Session.Count);
			StatData statData = null;
			foreach (var session in receivedStatData.Session)
			{
				ClassicAssert.IsNotNull(session.ParamsData);
				if (session.SenderCompID.Equals(FixSession.Parameters.SenderCompId)
						&& session.TargetCompID.Equals(FixSession.Parameters.TargetCompId))
				{
					statData = session.StatData;
					ClassicAssert.IsNull(session.StatusData);
				}
			}
			ClassicAssert.IsNotNull(statData);
		}

		[Test]
		public void TestSessionsSnapshotStatusParamsWithStatus()
		{
			_sessionsSnapshot.View = View.STATUS_PARAMS;

			var sessionView = new SessionsSnapshotSessionView();
			sessionView.View = View.STATUS;
			sessionView.SenderCompID = FixSession.Parameters.SenderCompId;
			sessionView.TargetCompID = FixSession.Parameters.TargetCompId;
			_sessionsSnapshot.SessionView.Add(sessionView);

			var response = GetReponse(_sessionsSnapshot);
			ClassicAssert.AreEqual(RequestID, response.RequestID);

			var receivedStatData = response.SessionsSnapshotData;
			ClassicAssert.IsNotNull(receivedStatData.Session);
			ClassicAssert.AreEqual(FixSessionManager.Instance.SessionsCount, receivedStatData.Session.Count);
			StatusData statusData = null;
			foreach (var session in receivedStatData.Session)
			{
				ClassicAssert.IsNotNull(session.ParamsData);
				if (session.SenderCompID.Equals(FixSession.Parameters.SenderCompId)
						&& session.TargetCompID.Equals(FixSession.Parameters.TargetCompId))
				{
					statusData = session.StatusData;
					ClassicAssert.IsNull(session.StatData);
				}
			}
			ClassicAssert.IsNotNull(statusData);
		}

		[Test]
		public void TestSessionsSnapshotStatDataWithStatusData()
		{
			_sessionsSnapshot.View = View.STATUS_PARAMS_STAT;

			var sessionView = new SessionsSnapshotSessionView();
			sessionView.View = View.STATUS;
			sessionView.SenderCompID = FixSession.Parameters.SenderCompId;
			sessionView.TargetCompID = FixSession.Parameters.TargetCompId;
			_sessionsSnapshot.SessionView.Add(sessionView);

			var response = GetReponse(_sessionsSnapshot);
			ClassicAssert.AreEqual(RequestID, response.RequestID);

			var receivedStatData = response.SessionsSnapshotData;
			ClassicAssert.IsNotNull(receivedStatData.Session);
			ClassicAssert.AreEqual(FixSessionManager.Instance.SessionsCount, receivedStatData.Session.Count);
			StatusData statusData = null;
			foreach (var session in receivedStatData.Session)
			{
				ClassicAssert.IsNotNull(session.StatData);
				if (session.SenderCompID.Equals(FixSession.Parameters.SenderCompId)
						&& session.TargetCompID.Equals(FixSession.Parameters.TargetCompId))
				{
					statusData = session.StatusData;
					ClassicAssert.IsNull(session.ParamsData);
				}
			}
			ClassicAssert.IsNotNull(statusData);
		}

		private bool IsEquals(string s1, string s2)
		{
			return (string.IsNullOrWhiteSpace(s1) || string.IsNullOrWhiteSpace(s2)) ? string.IsNullOrWhiteSpace(s1) && string.IsNullOrWhiteSpace(s2) : s1.Equals(s2);
		}
	}
}
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

using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.AdminTool.Tests.Commands
{
	internal class GeneralSessionsStatTest : AdminToolHelper
	{
		private bool InstanceFieldsInitialized = false;

		public GeneralSessionsStatTest()
		{
			if (!InstanceFieldsInitialized)
			{
				InitializeInstanceFields();
				InstanceFieldsInitialized = true;
			}
		}

		private void InitializeInstanceFields()
		{
			_log = LogFactory.GetLog(GetType());
		}

		private ILog _log;

		private FixAntenna.Fixicc.Message.GeneralSessionsStat _sessionsStat;

		[SetUp]
		public void Setup()
		{
			base.Before();
			_sessionsStat = new FixAntenna.Fixicc.Message.GeneralSessionsStat();
			RequestID = GetNextRequest();
			_sessionsStat.RequestID = RequestID;

			FixSession = FindAdminSession();
		}

		[Test]
		public void TestGeneralSessionsStat()
		{
			var list = FixSessionManager.Instance.SessionListCopy;
			foreach (var s in list)
			{
				_log.Debug("Session " + s.Parameters.SessionId + " (" + s.GetType().FullName + ") state: " + s.SessionState);
			}

			var response = GetReponse(_sessionsStat);
			ClassicAssert.AreEqual(RequestID, response.RequestID);
			ClassicAssert.AreEqual(ResultCode.OperationSuccess.Code, response.ResultCode);

			var sessionsStatData = response.GeneralSessionsStatData;
			ClassicAssert.IsNotNull(sessionsStatData);
			ClassicAssert.IsTrue(sessionsStatData.ActiveSessions != 0);
			ClassicAssert.IsTrue(sessionsStatData.AwaitingSessions == 0);
			ClassicAssert.IsNotNull(sessionsStatData.LastSessionCreation);
			ClassicAssert.IsTrue(sessionsStatData.MaxSessionLifetime != 0);
			ClassicAssert.IsTrue(sessionsStatData.MinSessionLifetime != 0);
			ClassicAssert.IsTrue(sessionsStatData.NumOfProcessedMessages != 0);
			ClassicAssert.AreEqual(0, sessionsStatData.ReconnectingSessions);
			ClassicAssert.AreEqual(0, sessionsStatData.TerminatedAbnormalSessions);
			ClassicAssert.AreEqual(0, sessionsStatData.TerminatedNormalSessions);
		}
	}
}
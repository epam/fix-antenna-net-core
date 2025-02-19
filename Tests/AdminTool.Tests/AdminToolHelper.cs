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

using Epam.FixAntenna.AdminTool.Tests.Commands.Util;
using Epam.FixAntenna.AdminTool.Tests.Util;
using Epam.FixAntenna.Fixicc.Message;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.AdminTool.Tests
{
	internal class AdminToolHelper
	{
		protected internal static IFixSession FixSession;

		protected internal static AdminFixSession AdminSession;
		// config sessions register is static because FixSessionManager is static and init only once
		protected internal static ConfiguredSessionRegisterImpl ConfiguredSessionRegister = new ConfiguredSessionRegisterImpl();
		protected internal static SessionId TestSessionID = new SessionId("test1", "sender");
		protected internal static SessionId TestSessionIdQualifier = new SessionId("test1", "sender", "idT");

		protected internal long? RequestID = 1L;

		protected internal AdminTool AdminTool = new AdminTool();

		static AdminToolHelper()
		{
			// we can't configure FixSessionManager
			// do it with static calls
			FixSessionManager.DisposeAllSession();
			ConstructSessions();
		}

		public virtual long? GetNextRequest()
		{
			return RequestID++;
		}

		[SetUp]
		public virtual void Before()
		{
			LogAppender.Clear();
			AdminSession = FindAdminSession();
			ConfiguredSessionRegister.DeleteAll();
			AdminTool.AdminSession = AdminSession;
			AdminTool.SetSessionRegister(ConfiguredSessionRegister);
			foreach (var session in FixSessionManager.Instance.SessionListCopy)
			{
				ConfiguredSessionRegister.RegisterSession(session.Parameters);
			}
		}

		[TearDown]
		public virtual void After()
		{
			LogAppender.AssertIfErrorExist();
			ConfiguredSessionRegister.DeleteAll();
			FixSessionManager.DisposeAllSession();
		}

		/// <summary>
		/// Gets admin session, admin session always first.
		/// </summary>
		protected AdminFixSession FindAdminSession()
		{
			return (AdminFixSession) FixSessionManager.Instance.SessionListCopy[0];
		}

		public virtual Response GetReponse(Request requestCommand)
		{
			AdminTool.Process(CommandUtilHelper.BuildFixMessage(requestCommand));
			var response = AdminSession.Message;
			ClassicAssert.IsNotNull(response, "no response");
			return CommandUtilHelper.GetXmlData(response);
		}

		private static void ConstructSessions()
		{
			var session = new AdminFixSession(DateTimeHelper.CurrentMilliseconds - 100L);
			var parameters = new SessionParameters();
			parameters.FixVersion = FixVersion.Fix44;
			parameters.Host = "localhost";
			parameters.HeartbeatInterval = 30;
			parameters.Port = 3000;
			parameters.SenderCompId = "sender";
			parameters.TargetCompId = "admin";
			parameters.UserName = "username";
			parameters.Password = "password";
			session.Parameters = parameters;

			var runtimeState = new FixSessionRuntimeState();
			runtimeState.OutSeqNum = 7;
			runtimeState.InSeqNum = 8;
			session.RuntimeState = runtimeState;

			FixSessionManager.Instance.RegisterFixSession(session);

			//session = new AdminFIXSession(System.currentTimeMillis() - 5000);
			parameters = new SessionParameters();
			parameters.FixVersion = FixVersion.Fix44;
			parameters.Host = "localhost13";
			parameters.HeartbeatInterval = 30;
			parameters.Port = 3001;
			parameters.SenderCompId = "admin";
			parameters.TargetCompId = "sender";
			parameters.Configuration.SetProperty("storageFactory", "");

			runtimeState = new FixSessionRuntimeState();
			runtimeState.OutSeqNum = 32;
			runtimeState.InSeqNum = 14;

			session = new AdminFixSession(DateTimeHelper.CurrentMilliseconds - 15000L);
			session.Parameters = parameters;
			session.RuntimeState = runtimeState;
			FixSessionManager.Instance.RegisterFixSession(session);

			session = new AdminFixSession(DateTimeHelper.CurrentMilliseconds - 2000L);
			parameters = new SessionParameters();
			parameters.FixVersion = FixVersion.Fix44;
			parameters.Host = "localhost13";
			parameters.HeartbeatInterval = 30;
			parameters.Port = 3001;
			parameters.SenderCompId = TestSessionID.Sender;
			parameters.TargetCompId = TestSessionID.Target;

			runtimeState = new FixSessionRuntimeState();
			runtimeState.OutSeqNum = 3;
			runtimeState.InSeqNum = 34;

			session.Parameters = parameters;
			session.RuntimeState = runtimeState;
			FixSessionManager.Instance.RegisterFixSession(session);

			session = new AdminFixSession(DateTimeHelper.CurrentMilliseconds - 4000L);

			parameters = new SessionParameters();
			parameters.FixVersion = FixVersion.Fix44;
			parameters.Host = "localhost15";
			parameters.HeartbeatInterval = 30;
			parameters.Port = 3001;
			parameters.SenderCompId = TestSessionIdQualifier.Sender;
			parameters.TargetCompId = TestSessionIdQualifier.Target;
			parameters.Configuration.SetProperty(Config.LogonMessageSessionQualifierTag, 9021);
			parameters.SessionQualifier = TestSessionIdQualifier.Qualifier;


			runtimeState = new FixSessionRuntimeState();
			runtimeState.OutSeqNum = 3;
			runtimeState.InSeqNum = 34;

			session.Parameters = parameters;
			session.RuntimeState = runtimeState;
			FixSessionManager.Instance.RegisterFixSession(session);
		}

		protected internal static IExtendedFixSession GetSession(SessionId id)
		{
			return FixSessionManager.Instance.Locate(id);
		}
	}
}
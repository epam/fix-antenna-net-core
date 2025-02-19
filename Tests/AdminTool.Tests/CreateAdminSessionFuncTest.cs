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

using System;
using System.IO;
using System.Threading;
using Epam.FixAntenna.AdminTool.Tests.Util;
using Epam.FixAntenna.Fixicc.Message;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.NetCore.Helpers;
using NUnit.Framework; 
using NUnit.Framework.Legacy;
using Version = Epam.FixAntenna.NetCore.Common.Utils.Version;

namespace Epam.FixAntenna.AdminTool.Tests
{
	internal class CreateAdminSessionFuncTest : IFixServerListener
	{
		private const int TEST_SLEEP = 2000;
		public const int ServerPort = 12345;
		private FixServer _fixServer;
		private IExtendedFixSession _fixSession;
		private SessionParameters _sessionParams;
		private IFixSession _createdSession;

		private const string Localhost = "127.0.0.1";

		[SetUp]
		public void SetUp()
		{
			_fixServer = new FixServer();
			_fixServer.SetPort(ServerPort);
			_fixServer.SetListener(this);
			_fixServer.Start();

			_sessionParams = new SessionParameters();
			_sessionParams.Port = ServerPort;
			_sessionParams.Host = Localhost;
			_sessionParams.TargetCompId = "admin";
			_sessionParams.SenderCompId = "admin3";
			LogAppender.Clear();
		}

		[TearDown]
		public void TearDown()
		{
			_fixServer.Stop();
			FixSessionManager.DisposeAllSession();
			LogAppender.Clear();
		}

		[Test]
		public void TestCreateAdminSession()
		{
			_sessionParams.OutgoingLoginMessage.AddTag(553, "admin");
			_sessionParams.OutgoingLoginMessage.AddTag(554, "admin");

			_fixSession = (IExtendedFixSession) _sessionParams.CreateNewFixSession();
			_fixSession.Connect();

			Thread.Sleep(TEST_SLEEP);

			// this admin session
			ClassicAssert.IsNull(_createdSession);
			ClassicAssert.AreEqual(SessionState.Connected, _fixSession.SessionState);
		}

		[Test]
		public void TestEnableTimeZoneAndVersionTag()
		{
			_sessionParams.OutgoingLoginMessage.AddTag(553, "admin");
			_sessionParams.OutgoingLoginMessage.AddTag(554, "admin");
			_fixSession = (IExtendedFixSession) _sessionParams.CreateNewFixSession();
			_fixSession.Connect();

			Thread.Sleep(TEST_SLEEP);

			// this admin session
			ClassicAssert.IsNull(_createdSession);
			ClassicAssert.AreEqual(SessionState.Connected, _fixSession.SessionState);

			var inLogon = _fixSession.Parameters.IncomingLoginMessage;
			ClassicAssert.AreEqual(Version.GetProductVersion(typeof(IMessage)), inLogon.GetTagValueAsString(AdminConstants.AdminProtocolVersionTag));
			ClassicAssert.AreEqual(AdminTool.GetFormattedTimeZone(), inLogon.GetTagValueAsString(AdminConstants.TimezoneTag));
		}

		[Test]
		public void TestCreateAdminSessionWithInvalidPassAndUsr()
		{
			_sessionParams.OutgoingLoginMessage.AddTag(553, "admin1");
			_sessionParams.OutgoingLoginMessage.AddTag(554, "admin1");

			_fixSession = (IExtendedFixSession) _sessionParams.CreateNewFixSession();
			_fixSession.Connect();

			Thread.Sleep(TEST_SLEEP);

			// this admin session
			ClassicAssert.IsNull(_createdSession);
			ClassicAssert.AreEqual(SessionState.DisconnectedAbnormally, _fixSession.SessionState);
			ThereIsWarn("Username/password for session admin-admin3 is different from expected.");
		}

		[Test]
		public void TestCreateAdminSessionWithInvalidUsr()
		{
			_sessionParams.OutgoingLoginMessage.AddTag(553, "admin");
			_sessionParams.OutgoingLoginMessage.AddTag(554, "admin1");

			_fixSession = (IExtendedFixSession) _sessionParams.CreateNewFixSession();
			_fixSession.Connect();

			Thread.Sleep(TEST_SLEEP);

			// this admin session
			ClassicAssert.IsNull(_createdSession);
			ClassicAssert.AreEqual(SessionState.DisconnectedAbnormally, _fixSession.SessionState);
			ThereIsWarn("Username/password for session admin-admin3 is different from expected.");
		}

		[Test]
		public void TestCreateAdminSessionWithInvalidHost()
		{
			_sessionParams.TargetCompId = "admin2";
			_sessionParams.OutgoingLoginMessage.AddTag(553, "admin2");
			_sessionParams.OutgoingLoginMessage.AddTag(554, "admin2");

			_fixSession = (IExtendedFixSession) _sessionParams.CreateNewFixSession();
			_fixSession.Connect();

			Thread.Sleep(TEST_SLEEP);

			// this admin session
			ClassicAssert.IsNull(_createdSession);
			ClassicAssert.AreEqual(SessionState.DisconnectedAbnormally, _fixSession.SessionState);
			ThereIsWarn($"Connection from {Localhost} not allowed for session admin2-admin3.");
		}

		private void ThereIsWarn(string expectedWarning)
		{
			var warnings = LogAppender.GetWarnings();
			var warnMessages = warnings.Split(Environment.NewLine, true);
			foreach (var warnMessage in warnMessages)
			{
				if (warnMessage.Equals(expectedWarning, StringComparison.Ordinal))
				{
					return;
				}
			}
			ClassicAssert.AreEqual(expectedWarning, warnings, "There is no expected warning");
		}

		public void NewFixSession(IFixSession session)
		{
			try
			{
				_createdSession = session;
				session.Connect();
			}
			catch (IOException e)
			{
				throw new Exception(e.Message, e);
			}
		}
	}
}
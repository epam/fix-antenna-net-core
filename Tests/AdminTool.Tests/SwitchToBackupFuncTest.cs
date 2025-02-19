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
using Epam.FixAntenna.AdminTool.Tests.Smoke.Util;
using Epam.FixAntenna.Fixicc.Message;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.TestUtils.Hooks;
using NUnit.Framework; 
using NUnit.Framework.Legacy;
using ForceSeqNumReset = Epam.FixAntenna.NetCore.FixEngine.ForceSeqNumReset;

namespace Epam.FixAntenna.AdminTool.Tests
{
	internal class SwitchToBackupFuncTest
	{
		private bool InstanceFieldsInitialized = false;

		public SwitchToBackupFuncTest()
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

		public const int ServerPort = 12345;
		public const int BackupServerPort1 = 15678;
		public const int BackupServerPort2 = 15679;

		private ILog _log;
		private FixServer _fixServer;
		private FixServer _fixServerBackup1;
		private FixServer _fixServerBackup2;

		private IFixSession _primarySession;
		private IFixSession _backupSession;
		private EventHook _backup1ConnectedHook;
		private EventHook _backup2ConnectedHook;

		[SetUp]
		public void SetUp()
		{
			_fixServer = new FixServer();
			_fixServer.SetPort(ServerPort);
			_fixServer.SetListener(new PrimarySessionListener(this));
			_fixServer.Start();

			_backup1ConnectedHook = new EventHook("Backup 1 connected event", 10000);
			_fixServerBackup1 = new FixServer();
			_fixServerBackup1.SetPort(BackupServerPort1);
			_fixServerBackup1.SetListener(new BackUpSessionListener(this, _backup1ConnectedHook));
			_fixServerBackup1.Start();

			_backup2ConnectedHook = new EventHook("Backup 2 connected event", 10000);
			_fixServerBackup2 = new FixServer();
			_fixServerBackup2.SetPort(BackupServerPort2);
			_fixServerBackup2.SetListener(new BackUpSessionListener(this, _backup2ConnectedHook));
			_fixServerBackup2.Start();

			_log.Info("---------------------------== START ==-----------------------------");
		}

		private SessionParameters GetAdminSessionParams()
		{
			var adminSessionParams = new SessionParameters();
			adminSessionParams.Port = ServerPort;
			adminSessionParams.Host = "localhost";
			adminSessionParams.TargetCompId = "admin";
			adminSessionParams.SenderCompId = "admin3";
			adminSessionParams.OutgoingLoginMessage.AddTag(553, "admin");
			adminSessionParams.OutgoingLoginMessage.AddTag(554, "admin");
			return adminSessionParams;
		}

		private SessionParameters GetSingleBackupSessionParams()
		{
			var testedSessionParams = new SessionParameters();
			testedSessionParams.ForceSeqNumReset = ForceSeqNumReset.Always;
			testedSessionParams.Port = ServerPort;
			testedSessionParams.Host = "localhost";
			testedSessionParams.TargetCompId = "TRGT";
			testedSessionParams.SenderCompId = "SNDR";
			testedSessionParams.AddDestination("localhost", BackupServerPort1);
			testedSessionParams.Configuration.SetProperty(Config.AutoreconnectDelayInMs, "200");
			return testedSessionParams;
		}


		[TearDown]
		public void TearDown()
		{
			_log.Info("---------------------------== END ==-----------------------------");

			_fixServer.Stop();
			_fixServerBackup1.Stop();
			_fixServerBackup2.Stop();
			FixSessionManager.DisposeAllSession();
		}

		[Test]
		public void TestSwitchToBackUp()
		{
			var testingSessionParams = GetSingleBackupSessionParams();
			var testableSession = new TestableSession(testingSessionParams);
			testableSession.Init();
			testableSession.Connect();
			testableSession.ClassicAssertConnected();
			ClassicAssert.IsNotNull(_primarySession, "Not initialized primary connection");

			var adminSession = new TestableSession(GetAdminSessionParams());
			adminSession.Init();
			adminSession.Connect();
			adminSession.ClassicAssertConnected();

			var toBackup = new ToBackup();
			toBackup.RequestID = 2L;
			toBackup.SenderCompID = testingSessionParams.SenderCompId;
			toBackup.TargetCompID = testingSessionParams.TargetCompId;

			SmokeUtil.SendRequest(toBackup, adminSession.Session);

			ClassicAssert.IsTrue
			(
				_backup1ConnectedHook.IsEventRaised(),
				$"Backup connection not established({_backup1ConnectedHook.TimeToWait}ms)"
			);

			ClassicAssert.IsNotNull(_backupSession);
			ClassicAssert.AreEqual(SessionState.Dead, _primarySession.SessionState);
		}

		private SessionParameters GetMultipleBackupSessionParams()
		{
			var testedSessionParams = new SessionParameters();
			testedSessionParams.ForceSeqNumReset = ForceSeqNumReset.Always;
			testedSessionParams.Port = ServerPort;
			testedSessionParams.Host = "localhost";
			testedSessionParams.TargetCompId = "TRGT";
			testedSessionParams.SenderCompId = "SNDR";
			testedSessionParams.AddDestination("localhost", BackupServerPort1);
			testedSessionParams.AddDestination("localhost", BackupServerPort2);
			testedSessionParams.Configuration.SetProperty(Config.AutoreconnectDelayInMs, "200");
			return testedSessionParams;
		}

		[Test]
		public void TestSwitchToMultipleBackUp()
		{
			var testingSessionParams = GetMultipleBackupSessionParams();
			var testableSession = new TestableSession(testingSessionParams);
			testableSession.Init();
			testableSession.Connect();
			testableSession.ClassicAssertConnected();
			ClassicAssert.IsNotNull(_primarySession, "Not initialized primary connection");

			var adminSession = new TestableSession(GetAdminSessionParams());
			adminSession.Init();
			adminSession.Connect();
			adminSession.ClassicAssertConnected();

			var toBackup = new ToBackup();
			toBackup.RequestID = 2L;
			toBackup.SenderCompID = testingSessionParams.SenderCompId;
			toBackup.TargetCompID = testingSessionParams.TargetCompId;

			SmokeUtil.SendRequest(toBackup, adminSession.Session);

			ClassicAssert.IsTrue
			(
				_backup1ConnectedHook.IsEventRaised(),
				$"Backup connection not established({_backup1ConnectedHook.TimeToWait}ms)"
			);

			ClassicAssert.IsNotNull(_backupSession);
			ClassicAssert.AreEqual(SessionState.Dead, _primarySession.SessionState);

			toBackup.RequestID = 3L;
			toBackup.SenderCompID = testingSessionParams.SenderCompId;
			toBackup.TargetCompID = testingSessionParams.TargetCompId;

			SmokeUtil.SendRequest(toBackup, adminSession.Session);

			ClassicAssert.IsTrue
			(
				_backup2ConnectedHook.IsEventRaised(),
				$"Backup connection not established({_backup2ConnectedHook.TimeToWait}ms)"
			);


			//cycling switch
			_backup1ConnectedHook.ResetEvent();
			toBackup.RequestID = 4L;
			toBackup.SenderCompID = testingSessionParams.SenderCompId;
			toBackup.TargetCompID = testingSessionParams.TargetCompId;

			SmokeUtil.SendRequest(toBackup, adminSession.Session);

			ClassicAssert.IsTrue
			(
				_backup1ConnectedHook.IsEventRaised(),
				$"Backup connection not established({_backup1ConnectedHook.TimeToWait}ms)"
			);
		}

		private class PrimarySessionListener : IFixServerListener
		{
			private readonly SwitchToBackupFuncTest _server;

			public PrimarySessionListener(SwitchToBackupFuncTest server)
			{
				_server = server;
			}

			public void NewFixSession(IFixSession session)
			{
				try
				{
					_server._primarySession = session;
					var listener = new DisposableFIXSessionListener(_server._primarySession);
					session.SetFixSessionListener(listener);
					session.Connect();
				}
				catch (IOException e)
				{
					throw new Exception(e.Message, e);
				}
			}
		}

		private class BackUpSessionListener : IFixServerListener
		{
			private readonly SwitchToBackupFuncTest _server;
			private readonly EventHook _hook;
			public BackUpSessionListener(SwitchToBackupFuncTest server, EventHook hook)
			{
				_server = server;
				_hook = hook;
			}

			public void NewFixSession(IFixSession session)
			{
				try
				{
					_server._backupSession = session;
					var listener = new DisposableFIXSessionListener(_server._backupSession);
					session.SetFixSessionListener(listener);
					session.Connect();
					_hook.RaiseEvent();
				}
				catch (IOException e)
				{
					throw new Exception(e.Message, e);
				}
			}
		}

		private class DisposableFIXSessionListener : IFixSessionListener
		{
			private readonly IFixSession _session;

			public DisposableFIXSessionListener(IFixSession session)
			{
				_session = session;
			}

			public void OnSessionStateChange(SessionState sessionState)
			{
				if (SessionState.IsDisconnected(sessionState))
				{
					_session.Dispose();
				}
			}

			public void OnNewMessage(FixMessage message)
			{
			}
		}

		private class TestableSession
		{
			internal SessionParameters Parameters;
			internal IExtendedFixSession Session;
			internal EventHook ConnectedEventHook;
			internal EventHook DisconnectedEventHook;

			public TestableSession(SessionParameters parameters)
			{
				Parameters = parameters;
				var sessionId = $"{parameters.SenderCompId}-{parameters.TargetCompId}";
				ConnectedEventHook = new EventHook($"{sessionId} connect event", 10000);
				DisconnectedEventHook = new EventHook($"{sessionId} disconnect event", 10000);
			}

			public virtual void Init()
			{
				Session = (IExtendedFixSession) Parameters.CreateInitiatorSession();
				Session.SetFixSessionListener(new FixSessionListenerAnonymousInnerClass(this));
			}

			private class FixSessionListenerAnonymousInnerClass : IFixSessionListener
			{
				private readonly TestableSession _testableSession;

				public FixSessionListenerAnonymousInnerClass(TestableSession session)
				{
					_testableSession = session;
				}

				public void OnSessionStateChange(SessionState sessionState)
				{
					if (SessionState.IsConnected(sessionState))
					{
						_testableSession.ConnectedEventHook.RaiseEvent();
					}
					else if (SessionState.IsDisconnected(sessionState))
					{
						_testableSession.DisconnectedEventHook.RaiseEvent();
					}
				}

				public void OnNewMessage(FixMessage message)
				{
				}
			}

			public virtual void Connect()
			{
				ResetEvents();
				Session.Connect();
			}

			public virtual void ResetEvents()
			{
				ConnectedEventHook.ResetEvent();
				DisconnectedEventHook.ResetEvent();
			}

			public virtual void ClassicAssertDisconnected()
			{
				ClassicAssert.IsTrue
				(
					DisconnectedEventHook.IsEventRaised(),
					$"Could not disconnect({DisconnectedEventHook.TimeToWait}ms)"
				);
			}

			public virtual void ClassicAssertConnected()
			{
				ClassicAssert.IsTrue
				(
					ConnectedEventHook.IsEventRaised(),
					$"Could not connect({ConnectedEventHook.TimeToWait}ms)"
				);
			}
		}
	}
}
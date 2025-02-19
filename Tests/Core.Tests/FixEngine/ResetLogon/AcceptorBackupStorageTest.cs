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
using Epam.FixAntenna.Core.Tests;
using Epam.FixAntenna.TestUtils;
using Epam.FixAntenna.TestUtils.Hooks;
using Epam.FixAntenna.NetCore.FixEngine.ResetLogon.Util;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.ResetLogon
{
	[TestFixture]
	internal class AcceptorBackupStorageTest
	{
		private const int Port = 1778;
		private const string InitiatorCompId = "initiator";
		private const string AcceptorCompId = "acceptor";

		private InitiatorSessionEmulator _sessionHelper;
		private IExtendedFixSession _session;
		private FixServer _server;

		[SetUp]
		public void SetUp()
		{
			ConfigurationHelper.StoreGlobalConfig();
			ClearLogs();

			var globalConfiguration = Config.GlobalConfiguration;
			globalConfiguration.SetProperty(Config.IntraDaySeqnumReset, "false");
			globalConfiguration.SetProperty(Config.StorageCleanupMode, StorageCleanupMode.Backup.ToString());

			var acceptorSessionConnectedEvent = new EventHook("Acceptor start", 5000);
			_server = new FixServer();
			_server.SetListener(new FixServerListener(this, acceptorSessionConnectedEvent));
			_server.SetPort(Port);

			var sessionParameters = new SessionParameters();
			sessionParameters.Host = "localhost";
			sessionParameters.Port = Port;
			sessionParameters.SenderCompId = InitiatorCompId;
			sessionParameters.TargetCompId = AcceptorCompId;
			sessionParameters.IncomingSequenceNumber = 1;
			sessionParameters.OutgoingSequenceNumber = 1;
			_sessionHelper = new InitiatorSessionEmulator(sessionParameters);

			_server.Start();
			_sessionHelper.Open();
			_sessionHelper.SendLogon();
			ClassicAssert.IsTrue(acceptorSessionConnectedEvent.IsEventRaised(), "Acceptor wasn't started");
			_sessionHelper.ReceiveLogon();
		}

		[TearDown]
		public virtual void TearDown()
		{
			_server.Stop();
			_sessionHelper.Close();
			FixSessionManager.DisposeAllSession();

			ConfigurationHelper.RestoreGlobalConfig();
			ClearLogs();
		}

		private void ClearLogs()
		{
			var logsCleaner = new LogsCleaner();
			logsCleaner.Clean("./logs");
			logsCleaner.Clean("./logs/backup");
		}

		[Test]
		public virtual void TestBackupStorageWhenResetLogonReceived()
		{
			CheckBackupStorageSize(_session.Parameters, 0);

			_sessionHelper.SendNewsMessage();
			ResetSeqNumsByInitiator();
			_sessionHelper.SendNewsMessage();

			CheckBackupStorageSize(_session.Parameters, 3);
		}

		private void CheckBackupStorageSize(SessionParameters sessionParameters, int expectedSize)
		{
			var configurationAdapter = new ConfigurationAdapter(sessionParameters.Configuration);
			ClassicAssert.IsTrue(CountMatchedFiles(configurationAdapter.BackupStorageDirectory, GetLogFileName(sessionParameters)) >= expectedSize);
		}

		private string GetLogFileName(SessionParameters sessionParameters)
		{
			return sessionParameters.SenderCompId + "-" + sessionParameters.TargetCompId;
		}

		private int CountMatchedFiles(string dir, string fileMaskPrefix)
		{
			string[] matches = { ".*\\.in$", ".*\\.out$", ".*\\.idx$" };
			return FileHelper.CountMatchedFiles(dir, fileMaskPrefix, matches);
		}

		private void ResetSeqNumsByInitiator()
		{
			_sessionHelper.SendResetLogon();
			_sessionHelper.ReceiveResetLogon();
		}

		private class FixServerListener : IFixServerListener
		{
			private readonly AcceptorBackupStorageTest _outerInstance;
			private readonly EventHook _acceptorSessionConnectedEvent;

			public FixServerListener(AcceptorBackupStorageTest outerInstance, EventHook acceptorSessionConnectedEvent)
			{
				_outerInstance = outerInstance;
				_acceptorSessionConnectedEvent = acceptorSessionConnectedEvent;
			}

			public void NewFixSession(IFixSession acceptorSession)
			{
				_outerInstance._session = (IExtendedFixSession)acceptorSession;
				_outerInstance._session.SetFixSessionListener(new FixSessionAdapter());
				try
				{
					_outerInstance._session.Connect();
					_acceptorSessionConnectedEvent.RaiseEvent();
				}
				catch (IOException e)
				{
					Console.WriteLine(e.ToString());
					Console.Write(e.StackTrace);
				}
			}
		}

		private class FixSessionAdapter : IFixSessionListener
		{
			public void OnSessionStateChange(SessionState sessionState)
			{
			}

			public void OnNewMessage(FixMessage message)
			{
			}
		}
	}
}
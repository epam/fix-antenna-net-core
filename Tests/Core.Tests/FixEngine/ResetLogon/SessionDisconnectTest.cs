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
using Epam.FixAntenna.TestUtils;
using Epam.FixAntenna.TestUtils.Hooks;
using Epam.FixAntenna.NetCore.FixEngine.ResetLogon.Util;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.ResetLogon
{
	[TestFixture]
	internal class SessionDisconnectTest
	{
		private const int Port = 1778;
		private const string InitiatorCompId = "initiator";
		private const string AcceptorCompId = "acceptor";

		private InitiatorSessionEmulator _initiatorHelper;
		private IExtendedFixSession _session;
		private FixServer _server;
		private EventHook _acceptorSessionConnectedEvent;
		private EventHook _acceptorSessionDisconnectedEvent;

		[SetUp]
		public void SetUp()
		{
			ClearLogs();

			_acceptorSessionConnectedEvent = new EventHook("Acceptor start", 5000);
			_acceptorSessionDisconnectedEvent = new EventHook("Acceptor stop", 5000);

			var globalConfiguration = Config.GlobalConfiguration;
			globalConfiguration.SetProperty(Config.IntraDaySeqnumReset, "false");

			_server = new FixServer();
			_server.SetListener(new FixServerListenerAnonymousInnerClass(this));
			_server.SetPort(Port);

			var sessionParameters = new SessionParameters();
			sessionParameters.Host = "localhost";
			sessionParameters.Port = Port;
			sessionParameters.SenderCompId = InitiatorCompId;
			sessionParameters.TargetCompId = AcceptorCompId;
			sessionParameters.IncomingSequenceNumber = 1;
			sessionParameters.OutgoingSequenceNumber = 1;
			_initiatorHelper = new InitiatorSessionEmulator(sessionParameters);
		}

		private class FixServerListenerAnonymousInnerClass : IFixServerListener
		{
			private readonly SessionDisconnectTest _outerInstance;

			public FixServerListenerAnonymousInnerClass(SessionDisconnectTest outerInstance)
			{
				_outerInstance = outerInstance;
			}

			public void NewFixSession(IFixSession acceptorSession)
			{
				_outerInstance._session = (IExtendedFixSession)acceptorSession;
				_outerInstance._session.SetFixSessionListener(new FixSessionAdapter(_outerInstance));
				try
				{
					_outerInstance._session.Connect();
				}
				catch (IOException e)
				{
					Console.WriteLine(e.ToString());
					Console.Write(e.StackTrace);
				}
			}
		}

		[TearDown]
		public virtual void TearDown()
		{
			_server.Stop();
			_initiatorHelper.Close();
			FixSessionManager.DisposeAllSession();

			ClearLogs();
		}

		private void ClearLogs()
		{
			new LogsCleaner().Clean("./logs");
			new LogsCleaner().Clean("./logs/backup");
		}

		[Test, Timeout(35000)]
		public virtual void TestBackupStorageWhenResetLogonReceived()
		{
			_server.Start();
			_initiatorHelper.Open();
			_initiatorHelper.SendLogon();
			ClassicAssert.IsTrue(_acceptorSessionConnectedEvent.IsEventRaised(), "Acceptor wasn't started");
			_initiatorHelper.ReceiveLogon();

			_initiatorHelper.SendNewsMessage();
			_initiatorHelper.Close();
			Thread.Sleep(1000);
			_initiatorHelper.ForcedResetSequences();


			_acceptorSessionConnectedEvent.ResetEvent();
			_initiatorHelper.Open();
			_initiatorHelper.SendLogon();

			ClassicAssert.Throws<IOException>(() =>
			{
				while (true)
				{
					Thread.Sleep(100);
					_initiatorHelper.SendNewsMessage();
				}
			});
		}

		[Test, Timeout(15000)]
		public virtual void TestAnswerOnLogout()
		{
			_server.Start();
			_initiatorHelper.Open();
			_initiatorHelper.SendLogon();

			ClassicAssert.IsTrue(_acceptorSessionConnectedEvent.IsEventRaised(), "Acceptor wasn't started");
			_initiatorHelper.ReceiveLogon();

			_initiatorHelper.SendNewsMessage();

			_acceptorSessionDisconnectedEvent.ResetEvent();
			_initiatorHelper.SendLogout("close");
			ClassicAssert.IsTrue(_acceptorSessionDisconnectedEvent.IsEventRaised(), "Acceptor wasn't stopped");
		}

		private class FixSessionAdapter : IFixSessionListener
		{
			private readonly SessionDisconnectTest _outerInstance;

			public FixSessionAdapter(SessionDisconnectTest outerInstance)
			{
				_outerInstance = outerInstance;
			}

			public virtual void OnSessionStateChange(SessionState sessionState)
			{
				if (SessionState.Connected == sessionState)
				{
					_outerInstance._acceptorSessionConnectedEvent.RaiseEvent();
				}
				else if (SessionState.IsDisconnected(sessionState))
				{
					_outerInstance._acceptorSessionDisconnectedEvent.RaiseEvent();
				}
			}

			public virtual void OnNewMessage(FixMessage message)
			{
			}
		}
	}
}
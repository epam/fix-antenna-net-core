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

using System;
using System.Threading;
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.TestUtils;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.FixEngine.Session.Common;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.User;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.User
{
	[TestFixture]
	internal class UserMessageHandlerPropertiesTest
	{
		private const int Port = 12345;

		private FixServer _server;

		private IFixSession _sessionA;
		private IFixSession _sessionB;

		private CountdownEvent _connected;
		private CountdownEvent _disconnected;
		private CountdownEvent _received1;
		private CountdownEvent _received2;

		private FixMessage _msg1;
		private FixMessage _msg2;

		[SetUp]
		public virtual void SetUp()
		{
			ClearLogs();
			InitServer();
			InitClients();
		}

		[TearDown]
		public virtual void TearDown()
		{
			_server.Stop();
			FixSessionManager.DisposeAllSession();
		}

		public virtual bool ClearLogs()
		{
			var logsCleaner = new LogsCleaner();
			return logsCleaner.Clean("./logs") && logsCleaner.Clean("./logs/backup");
		}

		private void InitServer()
		{
			var config = Config.GlobalConfiguration;
			config.SetProperty(AbstractFixSessionFactory.UserMessagehandlerGlobal + "0", typeof(DeliverToCompIdMessageHandler).FullName);

			_server = new FixServer();
			_server.SetPort(Port);

			_server.SetListener(new FixServerListenerAnonymousInnerClass(this));

		}

		internal class FixServerListenerAnonymousInnerClass : IFixServerListener
		{
			private readonly UserMessageHandlerPropertiesTest _outerInstance;

			public FixServerListenerAnonymousInnerClass(UserMessageHandlerPropertiesTest outerInstance)
			{
				_outerInstance = outerInstance;
			}

			public void NewFixSession(IFixSession session)
			{
				try
				{
					session.SetFixSessionListener(new FixSessionListenerAnonymousInnerClass(this));

					session.Connect();
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.ToString());
					Console.Write(ex.StackTrace);
				}
			}

			internal class FixSessionListenerAnonymousInnerClass : IFixSessionListener
			{
				private readonly FixServerListenerAnonymousInnerClass _outerInstance;

				public FixSessionListenerAnonymousInnerClass(FixServerListenerAnonymousInnerClass outerInstance)
				{
					_outerInstance = outerInstance;
				}

				public void OnSessionStateChange(SessionState sessionState)
				{
				}

				public void OnNewMessage(FixMessage message)
				{
					_outerInstance._outerInstance._msg1 = (FixMessage)message.Clone();
					_outerInstance._outerInstance._received1.Signal();
				}
			}
		}

		private void InitClients()
		{
			_connected = new CountdownEvent(2);
			_disconnected = new CountdownEvent(2);

			_sessionA = GetClientSession("A", "server");
			_sessionB = GetClientSession("B", "server");
		}

		private IFixSession GetClientSession(string senderId, string targetId)
		{
			var @params = new SessionParameters();

			@params.FixVersion = FixVersion.Fix44;
			@params.Host = "localhost";
			@params.Port = Port;
			@params.SenderCompId = senderId;
			@params.TargetCompId = targetId;

			var session = @params.CreateInitiatorSession();
			session.SetFixSessionListener(new FixSessionListenerAnonymousInnerClass2(this));

			return session;
		}

		internal class FixSessionListenerAnonymousInnerClass2 : IFixSessionListener
		{
			private readonly UserMessageHandlerPropertiesTest _outerInstance;

			public FixSessionListenerAnonymousInnerClass2(UserMessageHandlerPropertiesTest outerInstance)
			{
				_outerInstance = outerInstance;
			}

			public void OnSessionStateChange(SessionState sessionState)
			{
				if (sessionState.Equals(SessionState.Connected))
				{
					_outerInstance._connected.Signal();
				}
				if (sessionState.Equals(SessionState.Disconnected))
				{
					_outerInstance._disconnected.Signal();
				}
			}

			public void OnNewMessage(FixMessage message)
			{
				_outerInstance._msg2 = (FixMessage)message.Clone();
				_outerInstance._received2.Signal();
			}
		}


		[Test]
		public virtual void CheckUserMessageHandlerAsProperties()
		{
			try
			{
				_server.Start();

				_sessionA.Connect();
				_sessionB.Connect();

				_connected.Wait(TimeSpan.FromSeconds(5));
				ClassicAssert.AreEqual(_sessionA.SessionState, SessionState.Connected);
				ClassicAssert.AreEqual(_sessionB.SessionState, SessionState.Connected);

				_received1 = new CountdownEvent(1);
				_received2 = new CountdownEvent(1);

				var message = new FixMessage();
				_sessionA.SendMessage("B", message);

				_received1.Wait(TimeSpan.FromSeconds(5));
				_received2.Wait(TimeSpan.FromSeconds(5));
				ClassicAssert.IsNotNull(_msg1);
				ClassicAssert.IsNull(_msg2);

				_received1 = new CountdownEvent(1);
				_received2 = new CountdownEvent(1);

				message = new FixMessage();
				message.AddTag(Tags.DeliverToCompID, "B");
				_sessionA.SendMessage("B", message);

				_received1.Wait(TimeSpan.FromSeconds(5));
				_received2.Wait(TimeSpan.FromSeconds(5));
				ClassicAssert.IsNotNull(_msg1);
				ClassicAssert.IsNotNull(_msg2);
				ClassicAssert.AreEqual(_msg1.GetTagValueAsString(Tags.OnBehalfOfCompID), "A");
				ClassicAssert.AreEqual(_msg2.GetTagValueAsString(Tags.OnBehalfOfCompID), "A");

				_sessionA.Disconnect("disconnect");
				_sessionB.Disconnect("disconnect");

				_disconnected.Wait(TimeSpan.FromSeconds(5));
				ClassicAssert.AreEqual(_sessionA.SessionState, SessionState.Disconnected);
				ClassicAssert.AreEqual(_sessionB.SessionState, SessionState.Disconnected);

			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
				Console.Write(ex.StackTrace);
			}
		}
	}
}
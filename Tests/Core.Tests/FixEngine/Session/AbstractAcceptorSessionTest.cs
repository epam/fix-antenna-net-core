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
using Epam.FixAntenna.TestUtils;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	[TestFixture]
	internal abstract class AbstractAcceptorSessionTest
	{
		protected internal const string Localhost = "localhost";
		protected internal const int Port = 12345;

		protected internal FixServer Server;

		[SetUp]
		public virtual void SetUp()
		{
			ClassicAssert.IsTrue(ClearLogs(), "Can't clean logs before tests");
			Server = CreateSever(Port);
			ClassicAssert.IsTrue(Server.Start(), "Can't start server in SetUp().");
		}

		[TearDown]
		public virtual void TierDown()
		{
			Server?.Stop();
			FixSessionManager.DisposeAllSession();
			FixSessionManager.Instance.RemoveAllSessions();
			ClassicAssert.IsTrue(ClearLogs(), "Can't clean logs after tests");
		}

		public virtual bool ClearLogs()
		{
			return new LogsCleaner().Clean("./logs") && new LogsCleaner().Clean("./logs/backup");
		}

		public virtual FixServer CreateSever(int port)
		{
			var server = new FixServer();
			server.SetPort(port);
			server.SetListener(new AbstractAcceptorSessionListener());
			return server;
		}

		internal class AbstractAcceptorSessionListener : IFixServerListener
		{
			public void NewFixSession(IFixSession session)
			{
				try
				{
					session.SetFixSessionListener(new AbstractFixSessionListener());
					session.Connect();
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
					//throw;
				}
			}
		}

		internal class AbstractFixSessionListener : IFixSessionListener
		{
			public void OnNewMessage(FixMessage message)
			{
			}

			public void OnSessionStateChange(SessionState sessionState)
			{
			}
		}
	}
}
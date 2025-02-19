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

using System.IO;
using Epam.FixAntenna.TestUtils;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	[TestFixture]
	internal class FixSessionWithoutListenerTest
	{
		private const int Port = 12345;
		private FixServer _server;

		[SetUp]
		public void SetUp()
		{
			ClassicAssert.IsTrue(ClearLogs(), "Can't clean logs before tests");
			ConfigurationHelper.StoreGlobalConfig();
			Config.GlobalConfiguration.SetProperty(Config.SendRejectIfApplicationIsNotAvailable, "true");
			_server = new FixServer();
			_server.SetPort(Port);
			_server.SetListener(new FixServerListener());
			_server.Start();
		}

		private class FixServerListener : IFixServerListener
		{
			public void NewFixSession(IFixSession session)
			{
				try
				{
					session.Connect();
				}
				catch (IOException)
				{
				}
			}
		}

		[TearDown]
		public virtual void TearDown()
		{
			try
			{
				FixSessionManager.DisposeAllSession();
				_server.Stop();
				ClassicAssert.IsTrue(ClearLogs(), "Can't clean logs after tests");
			}
			finally
			{
				ConfigurationHelper.RestoreGlobalConfig();
			}
		}

		public virtual bool ClearLogs()
		{
			var logsCleaner = new LogsCleaner();
			return logsCleaner.Clean("./logs") && logsCleaner.Clean("./logs/backup");
		}

		[Test]
		public virtual void TestRejectMessages()
		{
			var gen = new MessageGenerator("s", "t");

			var helper = new SimplestFixSessionHelper("localhost", Port);
			helper.Connect();
			helper.PrepareToReceiveMessages(2);
			helper.SendMessage(gen.GetLogonMessage().AsByteArray());
			helper.SendMessage(gen.GetNewsMessage(2).AsByteArray());
			helper.WaitForMessages(30000);
			helper.Disconnect();

			var messages = helper.GetMessages();
			ClassicAssert.AreEqual("A", messages[0].GetTagValueAsString(35), "First message not Logon");
			ClassicAssert.AreEqual("3", messages[1].GetTagValueAsString(35), "Second message should be Reject");
		}
	}
}
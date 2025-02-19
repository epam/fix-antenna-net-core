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
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Storage;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using NLog;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	[TestFixture]
	internal class AcceptorWithNoQueuePumper
	{
		private readonly ILogger _logger = LogManager.GetLogger(typeof(AcceptorWithNoQueuePumper).FullName);
		public const int Port = 11192;

		[Test, Ignore("Intermittently fails on CI")]
		public virtual void ThereShouldBeDelayBeforeSendingApplicationMessages()
		{
			var msgTypeForTest = "D";
			var configuration = Config.GlobalConfiguration;
			configuration.SetProperty(Config.PreferredSendingMode, "SyncNoqueue");
			configuration.SetProperty(Config.InMemoryQueue, "true");
			configuration.SetProperty(Config.StorageFactory, typeof(InMemoryStorageFactory).FullName);

			// testing parameter with default value. The test should ClassicAssert.Fail if it is 0
			configuration.SetProperty(Config.MaxDelayToSendAfterLogon, "500");

			var fixServer = new FixServer(configuration);
			fixServer.SetPort(Port);
			fixServer.SetListener(new DummyFixServerListener(msgTypeForTest, _logger));
			fixServer.Start();

			var parameters = new SessionParameters();
			parameters.SenderCompId = "target";
			parameters.TargetCompId = "sender";
			parameters.Port = Port;
			parameters.Host = "localhost";
			var initiatorSession = StandardFixSessionFactory.GetFactory(parameters).CreateInitiatorSession(parameters);
			var cls = new FixSessionListenerAnonymousInnerClass(msgTypeForTest);
			initiatorSession.SetFixSessionListener(cls);
			initiatorSession.Connect();
			CheckingUtils.CheckWithinTimeout(() => cls.MessageReceivedFromServer, TimeSpan.FromMilliseconds(8000));
			initiatorSession.Dispose();
			fixServer.Stop();
		}

		private class DummyFixServerListener : IFixServerListener
		{
			private readonly string _msgType;
			private readonly ILogger _logger;

			public DummyFixServerListener(string msgType, ILogger logger)
			{
				_msgType = msgType;
				_logger = logger;
			}
			public void NewFixSession(IFixSession session)
			{
				try
				{
					session.SetSequenceNumbers(1, 10);
					session.Connect();
					session.SendWithChanges(RawFixUtil.GetFixMessage("35=" + _msgType + "\u0001"), ChangesType.AddSmhAndSmt);
				}
				catch (IOException e)
				{
					_logger.Error(e.Message, e);
				}
			}
		}

		private class FixSessionListenerAnonymousInnerClass : IFixSessionListener
		{
			private string _msgTypeForTest;
			public volatile bool MessageReceivedFromServer = false;

			public FixSessionListenerAnonymousInnerClass(string msgTypeForTest)
			{
				_msgTypeForTest = msgTypeForTest;
			}

			public void OnSessionStateChange(SessionState sessionState)
			{
			}

			public void OnNewMessage(FixMessage message)
			{
				MessageReceivedFromServer = _msgTypeForTest.Equals(StringHelper.NewString(message.MsgType));
			}
		}
	}
}
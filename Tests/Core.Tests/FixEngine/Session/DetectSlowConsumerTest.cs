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
using System.Threading;
using Epam.FixAntenna.TestUtils;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.NetCore.FixEngine.Session.Common;
using Epam.FixAntenna.NetCore.FixEngine.Session.Impl;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler;
using Epam.FixAntenna.NetCore.FixEngine.Transport;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	[TestFixture]
	internal class DetectSlowConsumerTest
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(DetectSlowConsumerTest));

		private const int FixPort = 2000;
		private const int SlowConsumerWriteDelayThreshold = 10;

		private IFixSession _slowSenderInitiator;
		private FixServer _acceptorServer;

		[SetUp]
		public void SetUp()
		{
			ClassicAssert.IsTrue(ClearLogs(), "Can't clean logs before tests");
		}

		[TearDown]
		public void TearDown()
		{
			StopCounterparties();
			FixSessionManager.DisposeAllSession();
			ClassicAssert.IsTrue(ClearLogs(), "Can't clean logs after tests");
		}

		private bool ClearLogs()
		{
			var logsCleaner = new LogsCleaner();
			return logsCleaner.Clean("logs") && logsCleaner.Clean("logs/backup");
		}

		[Test]
		public void TestSyncSessions()
		{
			DetectSlowConsumer(SendingMode.Sync);
		}

		[Test]
		public void TestAsyncSessions()
		{
			DetectSlowConsumer(SendingMode.Async);
		}

		private void DetectSlowConsumer(SendingMode sendingMode)
		{
			using (var slowConsumerDetectionLatch = new CountdownEvent(1))
			{
				InitCounterparties(sendingMode, slowConsumerDetectionLatch);
				StartCounterparties();

				var msgNum = 5;
				for (var i = 0; i < msgNum; i++)
				{
					var list = GetMessage(i);
					_slowSenderInitiator.SendMessage("B", list);
				}

				ClassicAssert.IsTrue(slowConsumerDetectionLatch.Wait(SlowConsumerWriteDelayThreshold * msgNum * 5));
			}
		}


		private void InitCounterparties(SendingMode sendingMode, CountdownEvent slowConsumerDetectionLatch)
		{
			try
			{
				var sessionFactory = new SlowSenderSessionFactory();
				var initiatorParams = GetInitiatorSessionParameters("localhost", FixPort, "initiator", "acceptor", sendingMode);
				_slowSenderInitiator = sessionFactory.CreateInitiatorSession(initiatorParams);
				_slowSenderInitiator.SetFixSessionListener(new DummySessionListener());
				_slowSenderInitiator.SlowConsumerListener = new SlowConsumerListener(slowConsumerDetectionLatch);

				_acceptorServer = GetFixServer(FixPort);
			}
			catch (Exception ex)
			{
				Log.Error(ex);
			}
		}

		private class SlowConsumerListener : IFixSessionSlowConsumerListener
		{
			private readonly CountdownEvent _event;
			public SlowConsumerListener(CountdownEvent ev)
			{
				_event = ev;
			}
			public void OnSlowConsumerDetected(SlowConsumerReason reason, long expected, long current)
			{
				_event.Signal();
			}
		}

		private class DummySessionListener : IFixSessionListener
		{
			public void OnSessionStateChange(SessionState sessionState)
			{
			}

			public void OnNewMessage(FixMessage message)
			{
			}
		}

		private SessionParameters GetInitiatorSessionParameters(string host, int port, string senderCompId, string targetCompId, SendingMode sendingMode)
		{
			var @params = new SessionParameters();

			@params.FixVersion = FixVersion.Fix44;
			@params.Host = host;
			@params.HeartbeatInterval = 60;
			@params.Port = port;
			@params.SenderCompId = senderCompId;
			@params.TargetCompId = targetCompId;
			@params.ForceSeqNumReset = ForceSeqNumReset.Always;

			@params.Configuration.SetProperty(Config.SlowConsumerDetectionEnabled, "true");
			@params.Configuration.SetProperty(Config.SlowConsumerWriteDelayThreshold, "" + SlowConsumerWriteDelayThreshold);
			@params.Configuration.SetProperty(Config.PreferredSendingMode, sendingMode.ToString());

			return @params;
		}


		private FixServer GetFixServer(int port)
		{
			var fixServer = new FixServer();
			fixServer.SetPort(port);
			fixServer.SetListener(new DummyFixServerListener(Log));
			return fixServer;
		}

		private class DummyFixServerListener : IFixServerListener
		{
			private readonly ILog _log;

			public DummyFixServerListener(ILog log)
			{
				_log = log;
			}
			public void NewFixSession(IFixSession session)
			{
				try
				{
					session.SetFixSessionListener(new DummySessionListener());
					session.Connect();
				}
				catch (Exception ex)
				{
					_log.Error("Can't connect initiator", ex);
				}
			}
		}

		private void StartCounterparties()
		{
			_acceptorServer.Start();
			_slowSenderInitiator.Connect();
			CheckingUtils.CheckWithinTimeout(() => _slowSenderInitiator.SessionState == SessionState.Connected,
				TimeSpan.FromMilliseconds(1000));
		}

		private void StopCounterparties()
		{
			var fixSessions = FixSessionManager.Instance.SessionListCopy;
			foreach (var fixSession in fixSessions)
			{
				fixSession.Dispose();
			}
			_slowSenderInitiator.Dispose();
			_acceptorServer.Stop();

			for (var i = 0; i < 10; i++)
			{
				var stoppedAll = true;
				foreach (var fixSession in FixSessionManager.Instance.SessionListCopy)
				{
					stoppedAll = stoppedAll && fixSession.SessionState == SessionState.Dead;
				}

				if (stoppedAll)
				{
					break;
				}
				Thread.Sleep(100);
			}
		}


		private FixMessage GetMessage(int number)
		{
			var list = new FixMessage();

			list.Set(58, "message" + number);

			return list;
		}

		private class SlowSenderSessionFactory : AbstractFixSessionFactory
		{
			public override IFixMessageFactory MessageFactory
			{
				get { return new Fix44MessageFactory(); }
			}

			public override IExtendedFixSession GetInitiatorSession(SessionParameters details, HandlerChain chain)
			{
				return new SlowInitiatorFixSession(MessageFactory, details, chain);
			}

			private class SlowInitiatorFixSession : InitiatorFixSession
			{
				public SlowInitiatorFixSession(IFixMessageFactory getMessageFactory, SessionParameters details, HandlerChain chain) : base(getMessageFactory, details, chain)
				{
				}

				protected override IFixTransport GetTransport(string host, int port, SessionParameters sessionParameters)
				{
					return new SlowInitiatorFixTransport(host, port, sessionParameters);
				}

				private class SlowInitiatorFixTransport : InitiatorFixTransport
				{
					public SlowInitiatorFixTransport(string host, int port, SessionParameters sessionParameters)
						: base(host, port, sessionParameters) {}

					public override void Write(byte[] message)
					{
						Delay();
						base.Write(message);
					}

					public override int Write(byte[] message, int offset, int length)
					{
						Delay();
						return base.Write(message, offset, length);
					}

					public override int Write(ByteBuffer message, int offset, int length)
					{
						Delay();
						return base.Write(message, offset, length);
					}

					private static void Delay()
					{
						try
						{
							Thread.Sleep(SlowConsumerWriteDelayThreshold * 5);
						}
						catch (ThreadInterruptedException e)
						{
							Console.WriteLine(e.ToString());
							Console.Write(e.StackTrace);
						}
					}
				}
			}
		}
	}
}
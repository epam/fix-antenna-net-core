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
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Acceptor;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
    [TestFixture]
	internal class ThrottleCheckingTest
	{
		private const int MessagesNumber = 1000;

		private IFixSession _initiatorSession;
		private IFixSession _acceptorSession;

		private FixServer _acceptorServer;
		private AcceptorFixServerListener _acceptorServerListener;

		private CountdownEvent _counter;

		private bool _checkThrottle;

		[SetUp]
		public void SetUp()
		{
			Assert.IsTrue(ClearLogs(),"Can't clean logs before tests");
			_counter = new CountdownEvent(MessagesNumber);
		}

		[TearDown]
		public virtual void TearDown()
		{
			StopCounterparties();
			FixSessionManager.DisposeAllSession();
			Assert.IsTrue(ClearLogs(), "Can't clean logs after tests");
		}

		protected virtual bool ClearLogs()
		{
			var logsCleaner = new LogsCleaner();
			return logsCleaner.Clean("logs") && logsCleaner.Clean("logs/backup");
		}

		private void InitCounterparties()
		{
			try
			{
				_initiatorSession = GetInitiatorSessionParameters("localhost", 2000, "initiator", "acceptor").CreateNewFixSession();
				_initiatorSession.SetFixSessionListener(new SessionListener(this, _initiatorSession));

				_acceptorServerListener = new AcceptorFixServerListener(this);
				_acceptorServer = GetFixServer(2000, _acceptorServerListener, "initiator", "acceptor");
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
				Console.Write(ex.StackTrace);
			}
		}

		private void StartCounterparties()
		{
			_acceptorServer.Start();
			_initiatorSession.Connect();
		}

		private void StopCounterparties()
		{
			_initiatorSession.Disconnect("disconnect");
			_acceptorServer.Stop();
		}

		private FixServer GetFixServer(int port, IFixServerListener fixServerListener, string senderCompId, string targetCompId)
		{
			var config = (Config) Config.GlobalConfiguration.Clone();
			config.SetProperty(Config.ServerAcceptorStrategy, typeof(AllowNonRegisteredAcceptorStrategyHandler).FullName);

			config.SetProperty(Config.ThrottleCheckingEnabled, _checkThrottle ? "true" : "false");
			config.SetProperty(Config.ThrottleCheckingPeriod, "100");
			config.SetProperty("throttleChecking.B.threshold", "100");

			var fixServer = new FixServer(config);

			fixServer.SetPort(port);
			fixServer.SetListener(fixServerListener);

			var registerAcceptorParams = new SessionParameters();
			registerAcceptorParams.Port = 2000;
			registerAcceptorParams.FixVersion = FixVersion.Fix44;
			registerAcceptorParams.SenderCompId = targetCompId;
			registerAcceptorParams.TargetCompId = senderCompId;
			registerAcceptorParams.Configuration = config;

			fixServer.RegisterAcceptorSession(registerAcceptorParams);

			return fixServer;
		}

		private SessionParameters GetInitiatorSessionParameters(string host, int port, string senderCompId, string targetCompId)
		{
			var parameters = new SessionParameters();
			parameters.FixVersion = FixVersion.Fix44;
			parameters.Host = host;
			parameters.HeartbeatInterval = 10;
			parameters.Port = port;
			parameters.SenderCompId = senderCompId;
			parameters.TargetCompId = targetCompId;
			parameters.UserName = "user";
			parameters.Password = "pass";
			parameters.ForceSeqNumReset = ForceSeqNumReset.Always;
			return parameters;
		}

		[Test]
		public virtual void CheckNoThrottle()
		{
			_checkThrottle = false;

			InitCounterparties();
			StartCounterparties();

			Thread.Sleep(1000);

			for (var i = 0; i < MessagesNumber; i++)
			{
				var list = GetMessage(i);
				_initiatorSession.SendMessage("B", list);
			}

			_counter.Wait();

			Assert.IsTrue(!_checkThrottle && _initiatorSession.SessionState == SessionState.Connected);
		}

		[Test]
		public virtual void CheckThrottle()
		{
			_checkThrottle = true;

			InitCounterparties();
			StartCounterparties();

			Thread.Sleep(1000);

			for (var i = 0; i < MessagesNumber; i++)
			{
				var list = GetMessage(i);
				if (_initiatorSession.SessionState == SessionState.Connected)
				{
					_initiatorSession.SendMessage("B", list);
				}
			}

			_counter.Wait(90000);

			CheckingUtils.CheckWithinTimeout(() => _initiatorSession.SessionState.Equals(SessionState.Disconnected), TimeSpan.FromMilliseconds(2000));

			Assert.AreEqual(DisconnectReason.ClosedByCounterparty, ((AbstractFixSession)_initiatorSession).LastDisconnectReason);
			Assert.AreEqual(DisconnectReason.Throttling, ((AbstractFixSession)_acceptorSession).LastDisconnectReason);
		}


		private FixMessage GetMessage(int number)
		{
			var list = new FixMessage();

			list.Set(58, "message" + number);

			return list;
		}

		internal class AcceptorFixServerListener : IFixServerListener
		{
			private readonly ThrottleCheckingTest _outerInstance;

			public AcceptorFixServerListener(ThrottleCheckingTest outerInstance)
			{
				_outerInstance = outerInstance;
			}

			public virtual void NewFixSession(IFixSession session)
			{
				try
				{
					_outerInstance._acceptorSession = session;
					_outerInstance._acceptorSession.SetFixSessionListener(new SessionListener(_outerInstance, session));
					_outerInstance._acceptorSession.Connect();
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.ToString());
					Console.Write(ex.StackTrace);
				}
			}
		}

		private class SessionListener : IFixSessionListener
		{
			private readonly ThrottleCheckingTest _outerInstance;
			internal readonly IFixSession Session;

			public SessionListener(ThrottleCheckingTest outerInstance, IFixSession session)
			{
				_outerInstance = outerInstance;
				Session = session;
			}

			public virtual void OnSessionStateChange(SessionState sessionState)
			{
				if (sessionState == SessionState.Disconnected)
				{
					if (Session is InitiatorFixSession && _outerInstance._counter.CurrentCount < MessagesNumber)
					{
						while (_outerInstance._counter.CurrentCount > 0)
						{
							_outerInstance._counter.Signal();
						}
					}
				}
			}

			public virtual void OnNewMessage(FixMessage message)
			{
				if (Session is AcceptorFixSession)
				{
					ProcessAcceptorMessage(message);
				}
				else
				{
					ProcessInitiatorMessage(message);
				}
			}

			public virtual void ProcessAcceptorMessage(FixMessage message)
			{
				try
				{
					_outerInstance._counter.Signal();
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.ToString());
					Console.Write(ex.StackTrace);
				}
			}

			public virtual void ProcessInitiatorMessage(FixMessage message)
			{
				try
				{
					Thread.Sleep(100);
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.ToString());
					Console.Write(ex.StackTrace);
				}
			}
		}
	}
}
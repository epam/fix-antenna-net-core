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
using System.Collections.Generic;
using Epam.FixAntenna.TestUtils;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	[TestFixture]
	internal class FixSessionSequenceMethodsTest
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(FixSessionSequenceMethodsTest));
		private Config _config;
		private IFixSession _initiatorSession;

		private IFixSession _acceptorSession;
		private SessionListener _acceptorListener;

		private FixServer _acceptorServer;
		private AcceptorFixServerListener _acceptorServerListener;

		[SetUp]
		public void SetUp()
		{
			Assert.IsTrue(ClearLogs(), "Can't clean logs before tests");
			_config = (Config)Config.GlobalConfiguration.Clone();
		}

		[TearDown]
		public virtual void TearDown()
		{
			StopCounterparties();
			FixSessionManager.DisposeAllSession();
			Assert.IsTrue(ClearLogs(), "Can't clean logs after tests");
		}

		[Test]
		public virtual void TestSetSeqNumBeforeConnect()
		{
			_initiatorSession = CreateInitiatorSession();
			_initiatorSession.InSeqNum = 100;
			_initiatorSession.OutSeqNum = 200;
			AssertSeqNums(_initiatorSession, 100, 200);

			_acceptorSession = CreateAcceptorSession();
			_acceptorSession.InSeqNum = 200;
			_acceptorSession.OutSeqNum = 100;
			AssertSeqNums(_acceptorSession, 200, 100);

			_acceptorServer = GetFixServer();
			_acceptorServer.Start();

			_initiatorSession.Connect();
			CheckingUtils.CheckWithinTimeout(
				() => _initiatorSession.SessionState.Equals(SessionState.Connected) &&
					_acceptorSession.SessionState.Equals(SessionState.Connected),
				TimeSpan.FromMilliseconds(1000));

			AssertSeqNums(_initiatorSession, 101, 201);
			AssertSeqNums(_acceptorSession, 201, 101);
		}

		[Test]
		public virtual void TestSetOutSeqNumForConnected()
		{
			_initiatorSession = CreateInitiatorSession();
			_acceptorSession = CreateAcceptorSession();
			_acceptorServer = GetFixServer();
			_acceptorServer.Start();

			_initiatorSession.Connect();
			CheckingUtils.CheckWithinTimeout(
				() => _initiatorSession.SessionState.Equals(SessionState.Connected) &&
					_acceptorSession.SessionState.Equals(SessionState.Connected),
				TimeSpan.FromMilliseconds(1000));

			_initiatorSession.OutSeqNum = 100;
			Assert.AreEqual(100, _initiatorSession.OutSeqNum);

			_acceptorSession.InSeqNum = 100;
			Assert.AreEqual(100, _acceptorSession.InSeqNum);

			_initiatorSession.SendMessage("B", GetNewMessage(1));
			CheckingUtils.CheckWithinTimeout(() => _initiatorSession.OutSeqNum == 101, TimeSpan.FromSeconds(1));

			var acceptorMsg = _acceptorListener.GetNextMessage(1000);
			CheckingUtils.CheckWithinTimeout(() => acceptorMsg.MsgSeqNumber == 100, TimeSpan.FromSeconds(1));
			CheckingUtils.CheckWithinTimeout(() => _acceptorSession.InSeqNum == 101, TimeSpan.FromSeconds(1));
		}

		[Test]
		public virtual void TestSetSeqNumForDisconnected()
		{
			_initiatorSession = CreateInitiatorSession();
			_acceptorSession = CreateAcceptorSession();
			_acceptorServer = GetFixServer();
			_acceptorServer.Start();

			_initiatorSession.Connect();
			CheckingUtils.CheckWithinTimeout(
				() => _initiatorSession.SessionState.Equals(SessionState.Connected) &&
					_acceptorSession.SessionState.Equals(SessionState.Connected),
				TimeSpan.FromMilliseconds(1000));

			_initiatorSession.Disconnect("TEST");
			CheckingUtils.CheckWithinTimeout(
				() => _initiatorSession.SessionState.Equals(SessionState.Disconnected) &&
					_acceptorSession.SessionState.Equals(SessionState.Disconnected),
				TimeSpan.FromMilliseconds(1000));

			// >>  35=A | 34=1
			// <<  35=A | 34=1
			// >> 35=5 | 34=2
			// << 35=5 | 34=2
			AssertSeqNums(_initiatorSession, 3, 3);

			_initiatorSession.InSeqNum = 100;
			_initiatorSession.OutSeqNum = 200;
			AssertSeqNums(_initiatorSession, 100, 200);

			_acceptorSession.InSeqNum = 200;
			_acceptorSession.OutSeqNum = 100;
			AssertSeqNums(_acceptorSession, 200, 100);

			_initiatorSession.Connect();
			CheckingUtils.CheckWithinTimeout(
				() => _initiatorSession.SessionState.Equals(SessionState.Connected) &&
					_acceptorSession.SessionState.Equals(SessionState.Connected),
				TimeSpan.FromMilliseconds(1000));
			AssertSeqNums(_initiatorSession, 101, 201);
			AssertSeqNums(_acceptorSession, 201, 101);
		}

		[Test]
		public virtual void TestSetSeqNumForDisposed()
		{
			_initiatorSession = CreateInitiatorSession();
			_acceptorSession = CreateAcceptorSession();
			_acceptorServer = GetFixServer();
			_acceptorServer.Start();

			_initiatorSession.Connect();
			CheckingUtils.CheckWithinTimeout(
				() => _initiatorSession.SessionState.Equals(SessionState.Connected) &&
					_acceptorSession.SessionState.Equals(SessionState.Connected),
				TimeSpan.FromMilliseconds(1000));

			_initiatorSession.Disconnect("TEST");
			CheckingUtils.CheckWithinTimeout(
				() => _initiatorSession.SessionState.Equals(SessionState.Disconnected) &&
					_acceptorSession.SessionState.Equals(SessionState.Disconnected),
				TimeSpan.FromMilliseconds(1000));

			_initiatorSession.Dispose();
			Assert.AreEqual(SessionState.Dead, _initiatorSession.SessionState);

			_initiatorSession.InSeqNum = 100;
			_initiatorSession.OutSeqNum = 200;
			AssertSeqNums(_initiatorSession, 100, 200);

			_acceptorSession.InSeqNum = 200;
			_acceptorSession.OutSeqNum = 100;
			AssertSeqNums(_acceptorSession, 200, 100);

			_initiatorSession.Connect();
			CheckingUtils.CheckWithinTimeout(
				() => _initiatorSession.SessionState.Equals(SessionState.Connected) &&
					_acceptorSession.SessionState.Equals(SessionState.Connected),
				TimeSpan.FromMilliseconds(1000));
			AssertSeqNums(_initiatorSession, 101, 201);
			AssertSeqNums(_acceptorSession, 201, 101);
		}

		[Test]
		public virtual void TestSetSeqNumAfterRecreate()
		{
			//init some sequence
			_initiatorSession = CreateInitiatorSession();
			_acceptorSession = CreateAcceptorSession();
			_acceptorServer = GetFixServer();
			_acceptorServer.Start();

			_initiatorSession.Connect();
			CheckingUtils.CheckWithinTimeout(
				() => _initiatorSession.SessionState.Equals(SessionState.Connected) &&
					_acceptorSession.SessionState.Equals(SessionState.Connected),
				TimeSpan.FromMilliseconds(1000));

			_initiatorSession.Disconnect("TEST");
			CheckingUtils.CheckWithinTimeout(
				() => _initiatorSession.SessionState.Equals(SessionState.Disconnected) &&
					_acceptorSession.SessionState.Equals(SessionState.Disconnected),
				TimeSpan.FromMilliseconds(1000));

			// >>  35=A | 34=1
			// <<  35=A | 34=1
			// >> 35=5 | 34=2
			// << 35=5 | 34=2
			AssertSeqNums(_initiatorSession, 3, 3);
			AssertSeqNums(_acceptorSession, 3, 3);
			_initiatorSession.Dispose();
			_acceptorSession.Dispose();
			CheckingUtils.CheckWithinTimeout(
				() => _initiatorSession.SessionState.Equals(SessionState.Dead) &&
					_acceptorSession.SessionState.Equals(SessionState.Dead),
				TimeSpan.FromMilliseconds(1000));

			//recreate session
			_initiatorSession = CreateInitiatorSession();
			_acceptorSession = CreateAcceptorSession();
			AssertSeqNums(_initiatorSession, 3, 3);
			AssertSeqNums(_acceptorSession, 3, 3);

			//change sequences
			_initiatorSession.InSeqNum = 100;
			_initiatorSession.OutSeqNum = 200;
			AssertSeqNums(_initiatorSession, 100, 200);

			_acceptorSession = CreateAcceptorSession();
			_acceptorSession.InSeqNum = 200;
			_acceptorSession.OutSeqNum = 100;
			AssertSeqNums(_acceptorSession, 200, 100);

			//connect and check
			_initiatorSession.Connect();
			CheckingUtils.CheckWithinTimeout(
				() => _initiatorSession.SessionState.Equals(SessionState.Connected) &&
					_acceptorSession.SessionState.Equals(SessionState.Connected),
				TimeSpan.FromMilliseconds(1000));
			AssertSeqNums(_initiatorSession, 101, 201);
			AssertSeqNums(_acceptorSession, 201, 101);
		}

		[Test]
		public virtual void TestNegativeSequenceToSet()
		{
			_initiatorSession = CreateInitiatorSession();

			//set before connect
			_initiatorSession.InSeqNum = -1;
			_initiatorSession.OutSeqNum = -1;
			AssertSeqNums(_initiatorSession, 1, 1);

			_acceptorSession = CreateAcceptorSession();
			_acceptorSession.InSeqNum = -1;
			_acceptorSession.OutSeqNum = -1;
			AssertSeqNums(_acceptorSession, 1, 1);

			_acceptorServer = GetFixServer();
			_acceptorServer.Start();

			_initiatorSession.Connect();
			CheckingUtils.CheckWithinTimeout(
				() => _initiatorSession.SessionState.Equals(SessionState.Connected) &&
					_acceptorSession.SessionState.Equals(SessionState.Connected),
				TimeSpan.FromMilliseconds(1000));
			AssertSeqNums(_initiatorSession, 2, 2);
			AssertSeqNums(_acceptorSession, 2, 2);

			//set for connected
			_initiatorSession.InSeqNum = -1;
			_initiatorSession.OutSeqNum = -1;
			AssertSeqNums(_initiatorSession, 2, 2);

			_acceptorSession.InSeqNum = -1;
			_acceptorSession.OutSeqNum = -1;
			AssertSeqNums(_acceptorSession, 2, 2);

			//disconnect and check sequences
			_initiatorSession.Disconnect("TEST");
			CheckingUtils.CheckWithinTimeout(
				() => _initiatorSession.SessionState.Equals(SessionState.Disconnected) &&
					_acceptorSession.SessionState.Equals(SessionState.Disconnected),
				TimeSpan.FromMilliseconds(1000));

			// >>  35=A | 34=1
			// <<  35=A | 34=1
			// >> 35=5 | 34=2
			// << 35=5 | 34=2
			//set for connected
			_initiatorSession.InSeqNum = -1;
			_initiatorSession.OutSeqNum = -1;
			AssertSeqNums(_initiatorSession, 3, 3);

			_acceptorSession.InSeqNum = -1;
			_acceptorSession.OutSeqNum = -1;
			AssertSeqNums(_acceptorSession, 3, 3);
		}

		[Test]
		public virtual void TestZeroSequenceToSet()
		{
			_initiatorSession = CreateInitiatorSession();

			//set before connect
			_initiatorSession.InSeqNum = 0;
			_initiatorSession.OutSeqNum = 0;
			AssertSeqNums(_initiatorSession, 1, 1);

			_acceptorSession = CreateAcceptorSession();
			_acceptorSession.InSeqNum = 0;
			_acceptorSession.OutSeqNum = 0;
			AssertSeqNums(_acceptorSession, 1, 1);

			_acceptorServer = GetFixServer();
			_acceptorServer.Start();

			_initiatorSession.Connect();
			CheckingUtils.CheckWithinTimeout(
				() => _initiatorSession.SessionState.Equals(SessionState.Connected) &&
					_acceptorSession.SessionState.Equals(SessionState.Connected),
				TimeSpan.FromMilliseconds(1000));

			//set for connected
			// >>  35=A | 34=1
			// <<  35=A | 34=1
			_initiatorSession.InSeqNum = 0;
			_initiatorSession.OutSeqNum = 0;
			AssertSeqNums(_initiatorSession, 2, 2);

			_acceptorSession.InSeqNum = 0;
			_acceptorSession.OutSeqNum = 0;
			AssertSeqNums(_acceptorSession, 2, 2);

			//disconnect and check sequences
			_initiatorSession.Disconnect("TEST");
			CheckingUtils.CheckWithinTimeout(
				() => _initiatorSession.SessionState.Equals(SessionState.Disconnected) &&
					_acceptorSession.SessionState.Equals(SessionState.Disconnected),
				TimeSpan.FromMilliseconds(1000));

			// >> 35=5 | 34=2
			// << 35=5 | 34=2
			//set for connected
			_initiatorSession.InSeqNum = 0;
			_initiatorSession.OutSeqNum = 0;
			AssertSeqNums(_initiatorSession, 3, 3);

			_acceptorSession.InSeqNum = 0;
			_acceptorSession.OutSeqNum = 0;
			AssertSeqNums(_acceptorSession, 3, 3);
		}


		[Test]
		[Property("JIRA", "https://jira.epam.com/jira/browse/BBP-23597")]
		public virtual void TestResetAndSequenceToSet()
		{
			_initiatorSession = CreateInitiatorSession();

			//PART 1: BEFORE CONNECTED
			_initiatorSession.InSeqNum = 10;
			_initiatorSession.OutSeqNum = 20;
			AssertSeqNums(_initiatorSession, 10, 20);

			_acceptorSession = CreateAcceptorSession();
			_acceptorSession.InSeqNum = 20;
			_acceptorSession.OutSeqNum = 10;
			AssertSeqNums(_acceptorSession, 20, 10);

			//do reset
			_initiatorSession.ResetSequenceNumbers();
			AssertSeqNums(_initiatorSession, 1, 1);

			_acceptorSession.ResetSequenceNumbers();
			AssertSeqNums(_acceptorSession, 1, 1);

			_acceptorServer = GetFixServer();
			_acceptorServer.Start();

			//PART 2: CONNECTED
			_initiatorSession.Connect();
			CheckingUtils.CheckWithinTimeout(
				() => _initiatorSession.SessionState.Equals(SessionState.Connected) &&
					_acceptorSession.SessionState.Equals(SessionState.Connected),
				TimeSpan.FromMilliseconds(5000));

			//set for connected
			// >>  35=A | 34=1
			// <<  35=A | 34=1
			AssertSeqNums(_initiatorSession, 2, 2);
			AssertSeqNums(_acceptorSession, 2, 2);

			_initiatorSession.InSeqNum = 10;
			_initiatorSession.OutSeqNum = 20;
			AssertSeqNums(_initiatorSession, 10, 20);

			_acceptorSession.InSeqNum = 20;
			_acceptorSession.OutSeqNum = 10;
			AssertSeqNums(_acceptorSession, 20, 10);

			//do reset
			_initiatorSession.ResetSequenceNumbers();
			// >>  35=A | 34=1
			// <<  35=A | 34=1
			CheckingUtils.CheckWithinTimeout(
				() => _initiatorSession.InSeqNum == 2 && _initiatorSession.OutSeqNum == 2,
				TimeSpan.FromMilliseconds(5000));
			AssertSeqNums(_acceptorSession, 2, 2);

			//PART 2: DISCONNECTED
			_initiatorSession.Disconnect("TEST");
			CheckingUtils.CheckWithinTimeout(
				() => _initiatorSession.SessionState.Equals(SessionState.Disconnected) &&
					_acceptorSession.SessionState.Equals(SessionState.Disconnected),
				TimeSpan.FromMilliseconds(5000));

			AssertSeqNums(_initiatorSession, 3, 3);
			AssertSeqNums(_acceptorSession, 3, 3);

			_initiatorSession.InSeqNum = 10;
			_initiatorSession.OutSeqNum = 20;
			AssertSeqNums(_initiatorSession, 10, 20);

			_acceptorSession = CreateAcceptorSession();
			_acceptorSession.OutSeqNum = 10;
			_acceptorSession.InSeqNum = 20;
			AssertSeqNums(_acceptorSession, 20, 10);

			//do reset
			_initiatorSession.ResetSequenceNumbers();
			AssertSeqNums(_initiatorSession, 1, 1);

			// check acceptor sequence reset after logon (reset on initiator side only)
			AssertSeqNums(_acceptorSession, 20, 10);
			//acceptorSession.ResetSequenceNumbers();
			//assertSeqNums(acceptorSession, 1, 1);

			_initiatorSession.Connect();
			CheckingUtils.CheckWithinTimeout(
				() => _initiatorSession.SessionState.Equals(SessionState.Connected) &&
					_acceptorSession.SessionState.Equals(SessionState.Connected),
				TimeSpan.FromMilliseconds(5000));

			AssertSeqNums(_initiatorSession, 2, 2);
			AssertSeqNums(_acceptorSession, 2, 2);
		}

		public virtual bool ClearLogs()
		{
			var logsCleaner = new LogsCleaner();
			return logsCleaner.Clean("logs") && logsCleaner.Clean("logs/backup");
		}

		private FixServer GetFixServer()
		{
			_acceptorServerListener = new AcceptorFixServerListener(this);
			return GetFixServer(2000, _acceptorServerListener);
		}

		private IFixSession CreateInitiatorSession()
		{
			return GetInitiatorSessionParameters("localhost", 2000, "initiator", "acceptor").CreateInitiatorSession();
		}

		private IFixSession CreateAcceptorSession()
		{
			var acceptorSession = GetAcceptorSessionParameters("initiator", "acceptor").CreateAcceptorSession();
			_acceptorListener = new SessionListener(acceptorSession);
			return acceptorSession;
		}


		private FixServer GetFixServer(int port, IFixServerListener fixServerListener)
		{
			var fixServer = new FixServer(_config);
			fixServer.SetPort(port);
			fixServer.SetListener(fixServerListener);
			return fixServer;
		}

		private SessionParameters GetInitiatorSessionParameters(string host, int port, string senderCompId, string targetCompId)
		{
			var @params = new SessionParameters
			{
				FixVersion = FixVersion.Fix44,
				Host = host,
				HeartbeatInterval = 30,
				Port = port,
				SenderCompId = senderCompId,
				TargetCompId = targetCompId
			};
			return @params;
		}

		private SessionParameters GetAcceptorSessionParameters(string senderCompId, string targetCompId)
		{
			var acceptorParams = new SessionParameters
			{
				FixVersion = FixVersion.Fix44,
				SenderCompId = targetCompId,
				TargetCompId = senderCompId,
				Configuration = _config
			};
			return acceptorParams;
		}


		private void StopCounterparties()
		{
			_initiatorSession.Disconnect("disconnect");

			_acceptorServer.Stop();
		}

		private FixMessage GetNewMessage(int number)
		{
			var list = new FixMessage();
			list.Set(58, "message" + number);
			return list;
		}

		public virtual void AssertSeqNums(IFixSession session, long expectedInSeqNum, long expectedOutSeqNum)
		{
			CheckingUtils.CheckWithinTimeout(() => session.InSeqNum == expectedInSeqNum,
				TimeSpan.FromMilliseconds(1000));

			CheckingUtils.CheckWithinTimeout(() => session.OutSeqNum == expectedOutSeqNum,
				TimeSpan.FromMilliseconds(1000));
		}

		private class AcceptorFixServerListener : IFixServerListener
		{
			private readonly FixSessionSequenceMethodsTest _outerInstance;

			public AcceptorFixServerListener(FixSessionSequenceMethodsTest outerInstance)
			{
				_outerInstance = outerInstance;
			}


			public virtual void NewFixSession(IFixSession session)
			{
				try
				{
					_outerInstance._acceptorSession = session;
					_outerInstance._acceptorListener = new SessionListener(_outerInstance._acceptorSession);
					_outerInstance._acceptorSession.SetFixSessionListener(_outerInstance._acceptorListener);
					_outerInstance._acceptorSession.Connect();
				}
				catch (Exception ex)
				{
					Log.Info(ex.Message, ex);
				}
			}
		}

		private class SessionListener : IFixSessionListener
		{
			internal readonly IFixSession Session;
			internal readonly IList<FixMessage> Messages = new List<FixMessage>();

			public SessionListener(IFixSession session)
			{
				Session = session;
			}

			public virtual void OnSessionStateChange(SessionState sessionState)
			{
				Log.Debug("Session " + Session.Parameters.SessionId + " changes state to : " + sessionState);
			}

			public virtual void OnNewMessage(FixMessage message)
			{
				Log.Debug("Incoming message for " + Session.Parameters.SessionId + ": " + message.ToPrintableString());

				Messages.Add((FixMessage)message.Clone());
			}

			public virtual FixMessage GetNextMessage()
			{
				if (Messages.Count > 0)
				{
					var msg = Messages[0];
					Messages.Remove(msg);
					return msg;
				}

				return null;
			}

			public virtual FixMessage GetNextMessage(long timeout)
			{
				CheckingUtils.CheckWithinTimeout(() => Messages.Count > 0, TimeSpan.FromMilliseconds(timeout));
				var msg = Messages[0];
				Messages.Remove(msg);
				return msg;
			}
		}
	}
}
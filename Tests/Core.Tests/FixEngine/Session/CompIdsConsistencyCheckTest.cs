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
using System.Collections.Concurrent;
using System.Threading;
using Epam.FixAntenna.TestUtils;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	[TestFixture]
	internal class CompIdsConsistencyCheckTest
	{
		private const int WaitMs = 2000;

		private const string Acceptor1 = "acceptor1";
		private const string Initiator11 = "initiator11";

		private IFixSession _initiatorSession11;

		private FixServer _acceptorServer1;

		private ConcurrentQueue<FixMessage> _acceptorQueue;

		private enum SendType
		{
			SendAsIs,
			SendMessageWithoutType,
			SendMessageWithType,
			SendMessageWithChanges
		}

		private static readonly string DateFormat = "yyyyMMdd-HH:mm:ss.SSS";

		[SetUp]
		public void SetUp()
		{
			ConfigurationHelper.StoreGlobalConfig();
			_acceptorQueue = new ConcurrentQueue<FixMessage>();
			ClassicAssert.IsTrue(ClearLogs(), "Can't clean logs before tests");

			Config.GlobalConfiguration.SetProperty(Config.SenderTargetIdConsistencyCheck, "false");
			InitCounterparties();
			StartCounterparties();
		}

		[TearDown]
		public virtual void TearDown()
		{
			try
			{
				StopCounterparties();
				FixSessionManager.DisposeAllSession();
				ClassicAssert.IsTrue(ClearLogs(), "Can't clean logs after tests");
			}
			finally
			{
				ConfigurationHelper.RestoreGlobalConfig();
			}
		}

		public virtual bool ClearLogs()
		{
			return (new LogsCleaner()).Clean("logs") && (new LogsCleaner()).Clean("logs/backup");
		}

		private void InitCounterparties()
		{
			try
			{
				_initiatorSession11 = GetInitiatorSessionParameters("localhost", 2000, Initiator11, Acceptor1).CreateNewFixSession();

				_initiatorSession11.SetFixSessionListener(new SessionListener(this, _initiatorSession11));

				_acceptorServer1 = GetFixServer(2000, new AcceptorFixServerListener(this));
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
				Console.Write(ex.StackTrace);
			}
		}

		private FixServer GetFixServer(int port, IFixServerListener fixServerListener)
		{
			var config = (Config) Config.GlobalConfiguration.Clone();
			config.SetProperty(Config.SenderTargetIdConsistencyCheck, "false");

			var fixServer = new FixServer(config);

			fixServer.SetPort(port);
			fixServer.SetListener(fixServerListener);

			return fixServer;
		}

		private SessionParameters GetInitiatorSessionParameters(string host, int port, string senderCompId, string targetCompId)
		{
			var @params = new SessionParameters();

			@params.FixVersion = FixVersion.Fix44;
			@params.Host = host;
			@params.HeartbeatInterval = 300;
			@params.Port = port;
			@params.SenderCompId = senderCompId;
			@params.TargetCompId = targetCompId;
			@params.UserName = "user";
			@params.Password = "pass";

			@params.Configuration.SetProperty(Config.SenderTargetIdConsistencyCheck, "false");

			return @params;
		}


		[Test]
		public virtual void TestFullMessages()
		{

			var initiator11Sender = "initiator11_" + DateTimeHelper.CurrentMilliseconds.ToString();
			var initiator12Sender = "initiator11_" + DateTimeHelper.CurrentMilliseconds.ToString();
			var acceptor1Target = "acceptor1_" + DateTimeHelper.CurrentMilliseconds.ToString();
			var messageType = "B";

			var messageN = GetFullMessage();
			var message1 = (FixMessage)messageN.Clone();
			message1.Set(49, initiator11Sender);
			message1.Set(56, acceptor1Target);

			var message2 = (FixMessage)messageN.Clone();
			message2.Set(49, initiator12Sender);
			message2.Set(56, acceptor1Target);

			SendMessage(SendType.SendMessageWithoutType, _initiatorSession11, (FixMessage)message1.Clone(), messageType, null);
			ClassicAssertAcceptorMessage(Initiator11, Acceptor1, messageType);

			SendMessage(SendType.SendMessageWithChanges, _initiatorSession11, (FixMessage)message1.Clone(), messageType, ChangesType.UpdateSmhAndSmt);
			ClassicAssertAcceptorMessage(Initiator11, Acceptor1, messageType);

			SendMessage(SendType.SendMessageWithChanges, _initiatorSession11, (FixMessage)message1.Clone(), messageType, ChangesType.UpdateSmhAndSmtExceptCompids);
			ClassicAssertAcceptorMessage(initiator11Sender, acceptor1Target, messageType);
		}

		private void ClassicAssertAcceptorMessage(string initiator11Sender, string acceptor1Target, string messageType)
		{
			FixMessage aMessage = null;
			CheckingUtils.CheckWithinTimeout(() => _acceptorQueue.TryDequeue(out aMessage), TimeSpan.FromMilliseconds(WaitMs));
			ClassicAssert.IsNotNull(aMessage);
			ClassicAssert.AreEqual(initiator11Sender, aMessage.GetTagValueAsString(49));
			ClassicAssert.AreEqual(acceptor1Target, aMessage.GetTagValueAsString(56));
			ClassicAssert.AreEqual(messageType, aMessage.GetTagValueAsString(35));
		}

		private FixMessage GetFullMessage()
		{
			var list = new FixMessage();

			list.Set(8, "FIX.4.4");
			list.Set(9, 0);
			list.Set(35, "B");
			list.Set(34, 1);
			list.Set(49, "initiator");
			list.Set(56, "acceptor");
			list.Set(52, "20170221-11:40:02.201");
			list.Set(58, "fullMessage");
			list.Set(10, "000");

			return list;
		}

		private void SendMessage(SendType sendType, IFixSession session, FixMessage message, string type, ChangesType? changes)
		{

			var text = message.GetTagValueAsString(58);
			text = !string.ReferenceEquals(text, null) ? (text.Trim() + "|" + sendType.ToString()) : sendType.ToString();

			if ((sendType == SendType.SendMessageWithChanges) && (changes != null))
			{
				text += "(" + changes.ToString() + ")";
			}
			message.Set(58, text);

			switch (sendType)
			{
				case CompIdsConsistencyCheckTest.SendType.SendAsIs:
				{
					while (((AbstractFixSession) session).QueuedMessagesCount > 0)
					{
						Thread.Yield();
					}

					message.RemoveTag(10);
					message.Set(34, ((AbstractFixSession) session).RuntimeState.OutSeqNum);
					message.Set(52, DateTime.UtcNow.ToString(DateFormat));
					if (!message.IsMessageBufferContinuous)
					{
						message.Set(9, message.RawLength - 14);
					}
					var checkSum = RawFixUtil.GetChecksum(message.AsByteArray());
					message.Set(10, checkSum < 10 ? "00" + checkSum : checkSum < 100 ? "0" + checkSum : "" + checkSum);

					session.SendAsIs(message);
					((AbstractFixSession)session).SequenceManager.IncrementOutSeqNum();
				}
				break;
				case CompIdsConsistencyCheckTest.SendType.SendMessageWithoutType:
				{
					session.SendMessage(message);
				}
				break;
				case CompIdsConsistencyCheckTest.SendType.SendMessageWithType:
				{
					session.SendMessage(type, message);
				}
				break;
				case CompIdsConsistencyCheckTest.SendType.SendMessageWithChanges:
				{
					session.SendWithChanges(message, changes);
				}
				break;
			}
		}

		private void StartCounterparties()
		{
			this._acceptorServer1.Start();

			this._initiatorSession11.Connect();
			CheckingUtils.CheckWithinTimeout(() => _initiatorSession11.SessionState == SessionState.Connected,
				TimeSpan.FromMilliseconds(WaitMs));
		}

		private void StopCounterparties()
		{
			this._initiatorSession11.Disconnect("disconnect11");

			CheckingUtils.CheckWithinTimeout(() => _initiatorSession11.SessionState == SessionState.Disconnected,
				TimeSpan.FromMilliseconds(WaitMs));

			this._acceptorServer1.Stop();
		}


		internal class AcceptorFixServerListener : IFixServerListener
		{
			private readonly CompIdsConsistencyCheckTest _outerInstance;

			public AcceptorFixServerListener(CompIdsConsistencyCheckTest outerInstance)
			{
				this._outerInstance = outerInstance;
			}

			public virtual void NewFixSession(IFixSession session)
			{
				try
				{
					session.SetFixSessionListener(new SessionListener(_outerInstance, session));
					session.Connect();
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.ToString());
					Console.Write(ex.StackTrace);
				}
			}
		}

		internal class SessionListener : IFixSessionListener
		{
			private readonly CompIdsConsistencyCheckTest _outerInstance;


			internal readonly IFixSession Session;

			public SessionListener(CompIdsConsistencyCheckTest outerInstance, IFixSession session)
			{
				this._outerInstance = outerInstance;
				this.Session = session;
			}

			public virtual void OnSessionStateChange(SessionState sessionState)
			{
			}

			public virtual void OnNewMessage(FixMessage message)
			{
				if (Session is AcceptorFixSession)
				{
					ProcessServerMessage(message);
				}
			}

			public virtual void ProcessServerMessage(FixMessage message)
			{

				var senderCompId = Session.Parameters.SenderCompId; //might not be same as
				switch (senderCompId)
				{
					case Acceptor1:
					{
						_outerInstance._acceptorQueue.Enqueue((FixMessage)message.Clone());
					}
					break;
				}
			}
		}
	}
}
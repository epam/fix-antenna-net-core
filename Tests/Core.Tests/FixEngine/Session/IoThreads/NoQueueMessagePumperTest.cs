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
using Epam.FixAntenna.NetCore.FixEngine.Session.Impl;
using Epam.FixAntenna.NetCore.FixEngine.Session.IoThreads;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.FixEngine.Storage.Queue;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.IOThreads
{
	[TestFixture]
	internal class NoQueueMessagePumperTest
	{
		private NoQueueMessagePumper _messagePumper;
		private TestFixTransport _transport;
		private FixMessage _message;
		private TestFixSession _extendedFixSession;
		private TestMessageStorage _messageStorage;
		private IQueue<FixMessageWithType> _queue;

		[SetUp]
		public void SetUp()
		{
			_message = RawFixUtil.GetFixMessage("8=FIX.4.29=15835=D49=BLP56=SCHB34=99350=3073797=Y52=20100715-07:17:27.59111=900010081=1003000321=255=TESTA54=138=400040=259=044=3047=I60=20000809-18:20:3210=095".AsByteArray());
			var sessionParameters = new SessionParameters();
			sessionParameters.TargetCompId = "SCHB";
			sessionParameters.SenderCompId = "BLP";
			sessionParameters.OutgoingSequenceNumber = 993;
			sessionParameters.IncomingSequenceNumber = 993;
			_transport = new TestFixTransport();
			var messageFactory = new StandardMessageFactory();
			messageFactory.SetSessionParameters(sessionParameters);
			messageFactory.SetRuntimeState(new FixSessionRuntimeState());
			_extendedFixSession = new TestFixSession();
			_extendedFixSession.SessionState = SessionState.Connected;
			_messageStorage = new TestMessageStorage();
			_queue = new InMemoryQueue<FixMessageWithType>();
			_queue.Initialize();
			_messagePumper = new NoQueueMessagePumper(_extendedFixSession, _queue, _messageStorage, messageFactory, _transport, _extendedFixSession.SequenceManager);
			_messagePumper.Init();
		}

		[TearDown]
		public virtual void TearDown()
		{
			_extendedFixSession.Dispose();
			_messagePumper.Shutdown();
			_queue.Dispose();
		}

		[Test]
		public virtual void WriteInButch()
		{
			// test session has AlreadySendingLogout = true while not inited
			_extendedFixSession.AlreadySendingLogout.AtomicExchange(false);

			_messagePumper.Start();
			for (var i = 0; i < 5; i++)
			{
				_messagePumper.Send("D", _message);
			}

			Assert.AreEqual(5, _transport.GetMessages().Count);
		}

		[Test, Timeout(5000)]
		public virtual void ShouldNotBePossibleToSendApplicationMessagesBeforeStart()
		{
			var doSend = true;
			long sentNumber = 0;

			void DoSend(object state)
			{
				while (doSend)
				{
					try
					{
						Interlocked.Increment(ref sentNumber);
						_messagePumper.Send("D", _message);
						Thread.Sleep(10);
					}
					catch (Exception)
					{
						// ignored
					}
				}
			}

			try
			{
				//start continued sending of app messages
				ThreadPool.QueueUserWorkItem(DoSend);

				while (Interlocked.Read(ref sentNumber) < 10)
				{
					//wait for 10 sending tries
					Thread.Sleep(10);
				}

				//check that there are no sent app messages
				Assert.AreEqual(0, _transport.GetMessages().Count);

				//send Logon out of turn (like during connecting)
				var logon = "8=FIX.4.2#9=0#35=A#34=1#49=ME#56=YOU#52=20090320-11:00:35.027#98=0#108=5#10=000#".Replace('#', '\u0001');
				var fixFields = RawFixUtil.GetFixMessage(logon.AsByteArray());
				_messagePumper.SendOutOfTurn("", fixFields);

				//check that still there are no sent app messages
				Assert.AreEqual(0, _transport.GetMessages().Count);

				//start pumper (start session)
				_messagePumper.Start();
				while (_transport.GetMessagesCount() == 0)
				{
					//wait transport get messages
					Thread.Sleep(10);
				}

				//check that first message is Logon
				Assert.AreEqual("A", StringHelper.NewString(RawFixUtil.GetMessageType(_transport.GetMessages()[0])),
					"First message not a Logon: " + StringHelper.NewString(_transport.GetMessages()[0]));
			}
			finally
			{
				doSend = false;
			}
		}

		[Test, Timeout(3000), Ignore("Temporary (debugging)")]
		public virtual void SenderIsLockedDuringSessionStart()
		{
			var sender = new Thread(() =>
			{
				try
				{
					_messagePumper.Send("D", _message);
				}
				catch (Exception)
				{
				}
			});
			sender.Start();

			//FIX session send Logon out of turn on start
			//emulate this behaviour and send Logon
			var logon = "8=FIX.4.2#9=0#35=A#34=1#49=ME#56=YOU#52=20090320-11:00:35.027#98=0#108=5#10=000#".Replace('#', '\u0001');
			var fixFields = RawFixUtil.GetFixMessage(logon.AsByteArray());
			_messagePumper.SendOutOfTurn("", fixFields);

			//sender should be blocked and no messages are sent yet
			while (sender.ThreadState != ThreadState.WaitSleepJoin)
			{
				//wait until another thread runs and locks
			}

			Assert.AreEqual(0, _transport.GetMessages().Count);

			//send other system level message (RR) out of turn
			var anotherSysMessage = "8=FIX.4.2#9=0#35=2#34=11#49=ME#56=YOU#52=20031027-14:29:11#7=1#16=100#10=000#".Replace('#', '\u0001');
			fixFields = RawFixUtil.GetFixMessage(anotherSysMessage.AsByteArray());
			_messagePumper.SendOutOfTurn("", fixFields);

			//sender should be still blocked and no messages are sent yet
			while (sender.ThreadState != ThreadState.WaitSleepJoin)
			{
				//wait until another thread runs and locks
			}

			Assert.AreEqual(0, _transport.GetMessages().Count);

			_messagePumper.Start();
			while (sender.IsAlive)
			{
				//wait until another thread finishes
			}

			while (_transport.GetMessages().Count == 0)
			{
				//wait transport get messages
			}

			//check that first message is Logon
			Assert.AreEqual("A", StringHelper.NewString(RawFixUtil.GetMessageType(_transport.GetMessages()[0])),
				"First message not a Logon: " + StringHelper.NewString(_transport.GetMessages()[0]));
		}

		[Test]
		public virtual void ShouldThrowExceptionWhileSendingWhenShutdown()
		{
			_messagePumper.Start();
			_messagePumper.Shutdown();
			try
			{
				_messagePumper.Send("D", _message);
			}
			catch (InvalidOperationException)
			{
				return;
			}
			Assert.Fail();
		}

		[Test]
		public virtual void SessionMessagesShouldBePutInQueueAndSendWhenPumperIsStarted()
		{
			for (var i = 0; i < 5; i++)
			{
				_messagePumper.SendOutOfTurn("0", _message);
			}
			Assert.AreEqual(0, _transport.GetMessages().Count);
			_messagePumper.Start();
			Assert.AreEqual(5, _transport.GetMessages().Count);
		}
	}
}
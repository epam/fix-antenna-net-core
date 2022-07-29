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
using System.Threading;

using Epam.FixAntenna.NetCore.FixEngine.Session.Impl;
using Epam.FixAntenna.NetCore.FixEngine.Session.IoThreads;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.FixEngine.Storage.Queue;
using Epam.FixAntenna.NetCore.FixEngine.Transport;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;

using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.IOThreads
{
	[TestFixture]
	internal class MessagePumperTest
	{
		private SyncMessagePumper _messagePumper;
		protected internal IQueue<FixMessageWithType> Queue;
		private StandardMessageFactory _messageFactory;
		private TestFixTransport _transport;
		private SessionParameters _sessionParameters;
		private FixMessage _message;
		private TestMessageStorage _messageStorage;

		internal TestFixSession ExtendedFixSession;

		[SetUp]
		public void Before()
		{
			_message = RawFixUtil.GetFixMessage("8=FIX.4.29=15835=D49=BLP56=SCHB34=99350=3073797=Y52=20100715-07:17:27.59111=900010081=1003000321=255=TESTA54=138=400040=259=044=3047=I60=20000809-18:20:3210=095".AsByteArray());
			_sessionParameters = new SessionParameters();
			_sessionParameters.TargetCompId = "SCHB";
			_sessionParameters.SenderCompId = "BLP";
			_sessionParameters.OutgoingSequenceNumber = 993;
			_sessionParameters.IncomingSequenceNumber = 993;
			_transport = new TestFixTransport();
			_messageFactory = new StandardMessageFactory();
			_messageFactory.SetSessionParameters(_sessionParameters);
			_messageFactory.SetRuntimeState(new FixSessionRuntimeState());
			
			ExtendedFixSession = new TestFixSession();
			ExtendedFixSession.SessionState = SessionState.Connected;
			_messageStorage = new TestMessageStorage();

			Queue = new InMemoryQueue<FixMessageWithType>();
			Queue.Initialize();
			_messagePumper = new SyncMessagePumper(ExtendedFixSession, Queue, _messageStorage, _messageFactory, _transport, ExtendedFixSession.SequenceManager);
			_messagePumper.Init();
		}

		[TearDown]
		public virtual void TearDown()
		{
			ExtendedFixSession.Dispose();
			_messageStorage.Dispose();
			_messagePumper.Shutdown();
		}

		[Test]
		public virtual void WriteInButch()
		{
			Queue.Add(FixMessageWithTypePoolFactory.GetFixMessageWithTypeFromPool(_message, ""));
			Queue.Add(FixMessageWithTypePoolFactory.GetFixMessageWithTypeFromPool(_message, ""));
			Queue.Add(FixMessageWithTypePoolFactory.GetFixMessageWithTypeFromPool(_message, ""));
			Queue.Add(FixMessageWithTypePoolFactory.GetFixMessageWithTypeFromPool(_message, ""));
			Queue.Add(FixMessageWithTypePoolFactory.GetFixMessageWithTypeFromPool(_message, ""));

			var msgCount = _messagePumper.FillBuffer(5);
			_messagePumper.SendMessages(msgCount);
			Assert.AreEqual(1, _transport.GetChunks().Count);
			var fixMessageChopper = new FixMessageChopper(new MemoryStream(_transport.GetMessages()[0]), 10000, 1000000);

			Assert.IsTrue(_transport.GetMessages().Count == 5, "Expected: 5, but was: " + _transport.GetMessages().Count);
		}

		[Test]
		public virtual void SendMessageDuringOutOfTurnBlock()
		{
			_messagePumper.Start();
			_messagePumper.Send("", _message, FixSessionSendingType.SendSync);
			Queue.OutOfTurnOnlyMode = true;
			_messagePumper.Send("", _message, FixSessionSendingType.SendSync);
			Thread.Sleep(1000);
			var actualMsgCount = _transport.GetMessages().Count;
			Assert.IsTrue(actualMsgCount == 1, "Sending of application messages was blocked, Expected: 1, but was: " + actualMsgCount);

			_messagePumper.SendOutOfTurn("", _message);
			Thread.Sleep(1000);
			actualMsgCount = _transport.GetMessages().Count;
			Assert.IsTrue(actualMsgCount == 2, "Sent message with priority, Expected: 1, but was: " + actualMsgCount);

			Queue.OutOfTurnOnlyMode = false;
			Thread.Sleep(1000);
			actualMsgCount = _transport.GetMessages().Count;
			Assert.IsTrue(actualMsgCount == 3, "Sending is free, Expected: 4, but was: " + actualMsgCount);
		}
	}
}
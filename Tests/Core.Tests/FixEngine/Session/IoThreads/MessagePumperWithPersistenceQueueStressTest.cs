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

using Epam.FixAntenna.NetCore.FixEngine.Session.Impl;
using Epam.FixAntenna.NetCore.FixEngine.Session.IoThreads;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.FixEngine.Storage.Queue;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;

using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.IOThreads
{
	[TestFixture]
	internal class MessagePumperWithPersistenceQueueStressTest
	{
		private const string StOutq = "S-T.outq";
		private SyncMessagePumper _messagePumper;
		protected internal IQueue<FixMessageWithType> Queue;
		private StandardMessageFactory _messageFactory;
		private TestFixTransport _transport;
		private SessionParameters _sessionParameters;
		private FixMessage _message;
		private TestMessageStorage _messageStorage;

		[SetUp]
		public void Before()
		{
			DeleteQueueFile();

			_message = RawFixUtil.GetFixMessage("8=FIX.4.29=15835=D49=BLP56=SCHB34=99350=3073797=Y52=20100715-07:17:27.59111=900010081=1003000321=255=TESTA54=138=400040=259=044=3047=I60=20000809-18:20:3210=095".AsByteArray());
			_sessionParameters = new SessionParameters();
			_sessionParameters.TargetCompId = "SNDR";
			_sessionParameters.SenderCompId = "TRGT";
			_sessionParameters.OutgoingSequenceNumber = 1;
			_sessionParameters.IncomingSequenceNumber = 1;
			_sessionParameters.HeartbeatInterval = 2;
			_transport = new TestFixTransport(_message.DeepClone(false, false));
			_messageFactory = new StandardMessageFactory();
			_messageFactory.SetSessionParameters(_sessionParameters);
			_messageFactory.SetRuntimeState(new FixSessionRuntimeState());
			_messageStorage = new TestMessageStorage();
			
			var extendedFixSession = new TestFixSession();

			Queue = new PersistentInMemoryQueue<FixMessageWithType>(StOutq, new FixMessageWithTypeFactory());
			Queue.Initialize();
			_messagePumper = new SyncMessagePumper(extendedFixSession, Queue, _messageStorage, _messageFactory, _transport, extendedFixSession.SequenceManager);
			_messagePumper.Init();
		}

		private void DeleteQueueFile()
		{
			try
			{
				File.Delete(StOutq);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		[TearDown]
		public virtual void TearDown()
		{
			Queue.Clear();
			Queue.Shutdown();
			_messagePumper.Shutdown();
			DeleteQueueFile();
		}

		[Test]
		public virtual void TestWriteTooManyMassages()
		{
			var numMessages = 1000;
			_messagePumper.Start();

			for (var i = 0; i < numMessages; i++)
			{
				_messagePumper.Send("B", CreateSampleMessage(Convert.ToString(i + 1)));
			}

			Thread.Sleep(5000);
			ClassicAssert.AreEqual(_transport.GetMessages().Count, _messageStorage.GetMessages().Count, "Expected " + numMessages);
		}

		[Test]
		public virtual void TestWriteBeforePumperStated()
		{
			var numMessages = 1000;

			for (var i = 0; i < numMessages / 2; i++)
			{
				_messagePumper.Send("B", CreateSampleMessage(Convert.ToString(i + 1)));
			}

			_messagePumper.Start();

			for (var i = numMessages / 2; i < numMessages; i++)
			{
				_messagePumper.Send("B", CreateSampleMessage(Convert.ToString(i + 1)));
			}

			Thread.Sleep(5000);
			ClassicAssert.AreEqual(_transport.GetMessages().Count, _messageStorage.GetMessages().Count, "Expected " + numMessages);
		}

		[Test]
		public virtual void TestWriteWithQueueOverControl()
		{
			var numMessages = 1000;
			Queue.OutOfTurnOnlyMode = false;
			for (var i = 0; i < numMessages / 2; i++)
			{
				_messagePumper.Send("B", CreateSampleMessage(Convert.ToString(i + 1)));
			}

			_messagePumper.Start();

			for (var i = numMessages / 2; i < numMessages; i++)
			{
				Queue.OutOfTurnOnlyMode = true;
				_messagePumper.SendOutOfTurn("B", CreateSampleMessage(Convert.ToString(i + 1)));

				Queue.OutOfTurnOnlyMode = false;
				Queue.OutOfTurnOnlyMode = false;
				Queue.OutOfTurnOnlyMode = true;
				Queue.OutOfTurnOnlyMode = true;

				_messagePumper.Send("B", CreateSampleMessage(Convert.ToString(i + 1)));
				Queue.OutOfTurnOnlyMode = false;
			}

			Thread.Sleep(2000);

			_messagePumper.Shutdown();
	//        queue.SetOutOfTurnOnlyMode(false);

			ClassicAssert.AreEqual(0, Queue.Size, "Expected " + 0);
			ClassicAssert.AreEqual(numMessages + numMessages / 2, _messageStorage.GetMessages().Count, "Expected " + numMessages);

			foreach (var el in _messageStorage.GetMessages())
			{
				var e = (byte[]) el;
				ClassicAssert.AreEqual(StringHelper.NewString(RawFixUtil.GetMessageType(e)), "B");
			}
		}

		public virtual FixMessage CreateSampleMessage(string mark)
		{
			// create FIX 4.2 News
			var messageContent = new FixMessage();

			messageContent.AddTag(148, "Hello there:" + mark); // Add Subject
			messageContent.AddTag(33, 1); // Add Repeating group
			messageContent.AddTag(58, "line1");

			return messageContent;
		}
	}
}
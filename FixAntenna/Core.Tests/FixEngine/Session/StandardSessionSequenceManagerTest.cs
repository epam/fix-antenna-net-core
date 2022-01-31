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

using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Session.Impl;
using Epam.FixAntenna.NetCore.FixEngine.Session.IoThreads;
using Epam.FixAntenna.NetCore.FixEngine.Session.IoThreads.Bean;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.FixEngine.Storage;
using Epam.FixAntenna.NetCore.FixEngine.Storage.Queue;
using Epam.FixAntenna.NetCore.FixEngine.Transport;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.TestUtils;

using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	[TestFixture]
	internal class StandardSessionSequenceManagerTest
	{
		private InitiatorFixSessionStub _sessionHelper;
		private ISessionSequenceManager _sequenceManager;

		[SetUp]
		public void SetUp()
		{
			ClearLogs();
			ConfigurationHelper.StoreGlobalConfig();
			_sessionHelper = new InitiatorFixSessionStub(3, 5);
			_sessionHelper.Init();
			_sequenceManager = new StandardSessionSequenceManager(_sessionHelper);
		}

		[TearDown]
		public virtual void TearDown()
		{
			_sessionHelper.Shutdown(DisconnectReason.GetDefault(), true);
			_sessionHelper.Dispose();
			ConfigurationHelper.RestoreGlobalConfig();
			Assert.IsTrue(ClearLogs(), "Can't clean logs after tests");
		}

		public virtual bool ClearLogs()
		{
			var logsCleaner = new LogsCleaner();
			return logsCleaner.Clean("./logs") && logsCleaner.Clean("./logs/backup");
		}

		[Test]
		public virtual void TestInitSessionSequencesFromStorage()
		{
			_sequenceManager.InitSeqNums(3, 5);
			var runtimeState = _sessionHelper.RuntimeState;
			Assert.That(runtimeState.InSeqNum, Is.EqualTo(3L));
			Assert.That(runtimeState.OutSeqNum, Is.EqualTo(5L));
			Assert.IsFalse(runtimeState.OutgoingLogon.IsTagExists(141));
		}

		[Test]
		public virtual void TestIgnoreSecondInit()
		{
			_sequenceManager.InitSeqNums(3, 5);
			_sequenceManager.InitSeqNums(22, 55);
			var runtimeState = _sessionHelper.RuntimeState;
			Assert.That(runtimeState.InSeqNum, Is.EqualTo(3L));
			Assert.That(runtimeState.OutSeqNum, Is.EqualTo(5L));
			Assert.IsFalse(runtimeState.OutgoingLogon.IsTagExists(141));
		}

		[Test]
		public virtual void TestInitFromParameters()
		{
			_sessionHelper.Parameters.IncomingSequenceNumber = 12;
			_sessionHelper.Parameters.OutgoingSequenceNumber = 21;

			_sequenceManager.InitSeqNums(3, 5);

			var runtimeState = _sessionHelper.RuntimeState;
			Assert.That(runtimeState.InSeqNum, Is.EqualTo(12L));
			Assert.That(runtimeState.OutSeqNum, Is.EqualTo(21L));
			Assert.IsFalse(runtimeState.OutgoingLogon.IsTagExists(141));
		}

		[Test]
		public virtual void TestInitInSequenceFromParameters()
		{
			_sessionHelper.Parameters.IncomingSequenceNumber = 12;

			_sequenceManager.InitSeqNums(3, 5);

			var runtimeState = _sessionHelper.RuntimeState;
			Assert.That(runtimeState.InSeqNum, Is.EqualTo(12L));
			Assert.That(runtimeState.OutSeqNum, Is.EqualTo(5L));
			Assert.IsFalse(runtimeState.OutgoingLogon.IsTagExists(141));
		}

		[Test]
		public virtual void TestInitOutSequenceFromParameters()
		{
			_sessionHelper.Parameters.OutgoingSequenceNumber = 12;

			_sequenceManager.InitSeqNums(3, 5);

			var runtimeState = _sessionHelper.RuntimeState;
			Assert.That(runtimeState.InSeqNum, Is.EqualTo(3L));
			Assert.That(runtimeState.OutSeqNum, Is.EqualTo(12L));
			Assert.IsFalse(runtimeState.OutgoingLogon.IsTagExists(141));
		}

		[Test]
		public virtual void TestInitWithForceSeqNumReset()
		{
			var runtimeState = _sessionHelper.RuntimeState;
			runtimeState.InSeqNum = 111;
			runtimeState.OutSeqNum = 222;

			var configuration = _sessionHelper.Parameters.Configuration;
			configuration.SetProperty(Config.ForceSeqNumReset, ForceSeqNumReset.Always.ToString());
			_sessionHelper.Parameters.IncomingSequenceNumber = 2;
			_sessionHelper.Parameters.OutgoingSequenceNumber = 3;
			_sequenceManager.InitSeqNums(4, 5);

			Assert.That(runtimeState.InSeqNum, Is.EqualTo(1L));
			Assert.That(runtimeState.OutSeqNum, Is.EqualTo(1L));
			Assert.IsTrue(runtimeState.OutgoingLogon.IsTagExists(141));

			runtimeState.InSeqNum = 111;
			runtimeState.OutSeqNum = 222;
			_sequenceManager.InitSeqNums(4, 5);

			Assert.That(runtimeState.InSeqNum, Is.EqualTo(1L));
			Assert.That(runtimeState.OutSeqNum, Is.EqualTo(1L));
			Assert.IsTrue(runtimeState.OutgoingLogon.IsTagExists(141));
		}

		[Test]
		public virtual void TestInitWithOneTimeReset()
		{
			var runtimeState = _sessionHelper.RuntimeState;
			runtimeState.InSeqNum = 111;
			runtimeState.OutSeqNum = 222;

			var configuration = _sessionHelper.Parameters.Configuration;
			configuration.SetProperty(Config.ForceSeqNumReset, ForceSeqNumReset.OneTime.ToString());
			_sessionHelper.Parameters.IncomingSequenceNumber = 2;
			_sessionHelper.Parameters.OutgoingSequenceNumber = 3;
			_sequenceManager.InitSeqNums(4, 5);


			Assert.That(runtimeState.InSeqNum, Is.EqualTo(1L));
			Assert.That(runtimeState.OutSeqNum, Is.EqualTo(1L));
			Assert.IsTrue(runtimeState.OutgoingLogon.IsTagExists(141));


			runtimeState.InSeqNum = 111;
			runtimeState.OutSeqNum = 222;
			_sequenceManager.InitSeqNums(4, 5);

			Assert.That(runtimeState.InSeqNum, Is.EqualTo(111L));
			Assert.That(runtimeState.OutSeqNum, Is.EqualTo(222L));
			Assert.IsFalse(runtimeState.OutgoingLogon.IsTagExists(141));
		}

		[Test]
		public virtual void TestInitWithNewerReset()
		{
			var runtimeState = _sessionHelper.RuntimeState;
			runtimeState.InSeqNum = 111;
			runtimeState.OutSeqNum = 222;

			var configuration = _sessionHelper.Parameters.Configuration;
			configuration.SetProperty(Config.ForceSeqNumReset, ForceSeqNumReset.Never.ToString());
			_sequenceManager.InitSeqNums(4, 5);

			Assert.That(runtimeState.InSeqNum, Is.EqualTo(111L));
			Assert.That(runtimeState.OutSeqNum, Is.EqualTo(222L));
			Assert.IsFalse(runtimeState.OutgoingLogon.IsTagExists(141));
		}

		[Test]
		public virtual void TestResetFlagInOutLogon()
		{
			var runtimeState = _sessionHelper.RuntimeState;
			runtimeState.InSeqNum = 111;
			runtimeState.OutSeqNum = 222;

			runtimeState.OutgoingLogon.AddTag(141, "Y");
			_sequenceManager.InitSeqNums(4, 5);

			Assert.That(runtimeState.InSeqNum, Is.EqualTo(1L));
			Assert.That(runtimeState.OutSeqNum, Is.EqualTo(1L));
			Assert.IsTrue(runtimeState.OutgoingLogon.IsTagExists(141));
		}

		[Test]
		public virtual void TestIntradaySeqNumReset()
		{
			var runtimeState = _sessionHelper.RuntimeState;
			runtimeState.InSeqNum = 111;
			runtimeState.OutSeqNum = 222;

			var configuration = _sessionHelper.Parameters.Configuration;
			configuration.SetProperty(Config.IntraDaySeqnumReset, "true");

			_sequenceManager.InitSeqNums(4, 5);

			Assert.That(runtimeState.InSeqNum, Is.EqualTo(1L));
			Assert.That(runtimeState.OutSeqNum, Is.EqualTo(1L));
			Assert.IsFalse(runtimeState.OutgoingLogon.IsTagExists(141));
		}

		[Test]
		public virtual void TestResetByTimestamp()
		{
			var runtimeState = _sessionHelper.RuntimeState;
			runtimeState.InSeqNum = 111;
			runtimeState.OutSeqNum = 222;

			var df = "HH:mm:ss";
			var cal = DateTime.UtcNow;

			var configuration = _sessionHelper.Parameters.Configuration;
			configuration.SetProperty(Config.PerformResetSeqNumTime, "true");
			configuration.SetProperty(Config.ResetSequenceTimeZone, "UTC");
			configuration.SetProperty(Config.ResetSequenceTime, cal.ToString(df));
			cal = cal.AddHours(-25);
			var ms = cal.TotalMilliseconds();
			_sessionHelper.Parameters.LastSeqNumResetTimestamp = ms;

			_sequenceManager.InitSeqNums(4, 5);

			Assert.That(runtimeState.InSeqNum, Is.EqualTo(1L));
			Assert.That(runtimeState.OutSeqNum, Is.EqualTo(1L));
			Assert.IsTrue(runtimeState.OutgoingLogon.IsTagExists(141));
		}

		private class InitiatorFixSessionStub : InitiatorFixSession
		{
			internal readonly long InitInSeqNum;
			internal readonly long InitOutSeqNum;

			public InitiatorFixSessionStub(long initInSeqNum, long initOutSeqNum)
				: base(new Fix44MessageFactory(), GetInitSessionParameters(),
#pragma warning disable CA2000 // Dispose objects before losing scope: disposed in AbstractFIXSession
					new HandlerChain())
#pragma warning restore CA2000 // Dispose objects before losing scope
			{
				InitInSeqNum = initInSeqNum;
				InitOutSeqNum = initOutSeqNum;
			}

			public override void Shutdown(DisconnectReason reason, bool blocking)
			{
				try
				{
					IncomingStorage.Dispose();
					OutgoingStorage.Dispose();
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.ToString());
					Console.Write(ex.StackTrace);
				}

				base.Shutdown(reason, blocking);
			}

			private static SessionParameters GetInitSessionParameters()
			{
				var sessionParams = new SessionParameters();
				sessionParams.FixVersion = FixVersion.Fix44;
				sessionParams.TargetCompId = "T";
				sessionParams.SenderCompId = "S";
				sessionParams.HeartbeatInterval = 2;
				sessionParams.IncomingSequenceNumber = SessionParameters.DefaultSequenceNum;
				sessionParams.OutgoingSequenceNumber = SessionParameters.DefaultSequenceNum;
				sessionParams.Host = "host";
				return sessionParams;
			}


			public override IMessageReader BuildMessageReader(IMessageStorage incomingMessageStorage,
				HandlerChain listener, IFixTransport transport)
			{
				return new MessageReader(this);
			}

			private class MessageReader : IMessageReader
			{
				private readonly InitiatorFixSessionStub _outerInstance;

				public MessageReader(InitiatorFixSessionStub outerInstance)
				{
					_outerInstance = outerInstance;
				}

				public long Init(ConfigurationAdapter configurationAdapter)
				{
					return _outerInstance.InitInSeqNum;
				}

				public void Shutdown()
				{
				}

				public long MessageProcessedTimestamp { get; set; }

				public bool IsStatisticEnabled => false;

				public MessageStatistic MessageStatistic => null;

				public IMessageStorage IncomingMessageStorage => null;

				public bool GracefulShutdown { get; set; }

				public void Start()
				{
				}

				public void Join()
				{
				}

				public void Run()
				{
				}

				public Thread WorkerThread { get; }
			}

			public override IMessagePumper BuildMessagePumper(ConfigurationAdapter configuration, IQueue<FixMessageWithType> queue,
				IMessageStorage outgoingMessageStorage, IFixMessageFactory messageFactory, IFixTransport transport, 
				ISessionSequenceManager sequenceManager)
			{
				return new MessagePumper(this);
			}

			private class MessagePumper : IMessagePumper
			{
				private readonly InitiatorFixSessionStub _outerInstance;

				public MessagePumper(InitiatorFixSessionStub outerInstance)
				{
					_outerInstance = outerInstance;
				}

				public long Init()
				{
					return _outerInstance.InitOutSeqNum;
				}

				public bool GracefulShutdown { get; set; }

				public void RejectQueueMessages()
				{
				}

				public void RejectFirstQueueMessage()
				{
				}

				public bool IsStatisticEnabled => false;

				public MessageStatistic Statistic
				{
					get { return null; }
				}

				public long MessageProcessedTimestamp
				{
					get { return 0; }
				}

				public bool SendOutOfTurn(string msgType, FixMessage content)
				{
					return false;
				}

				public void Start()
				{
				}

				public void Shutdown()
				{
				}

				public void Join()
				{
				}

				public int Send(string type, FixMessage content, FixSessionSendingType optionMask)
				{
					return 0;
				}

				public int Send(FixMessage content, ChangesType? allowedChangesType)
				{
					return 0;
				}

				public int Send(FixMessage content, ChangesType? allowedChangesType, FixSessionSendingType optionMask)
				{
					return 0;
				}

				public int Send(string s, FixMessage message)
				{
					return 0;
				}

				public void SendMessages(int messageCount)
				{

				}

				public Thread WorkerThread { get; }
			}
		}
	}
}
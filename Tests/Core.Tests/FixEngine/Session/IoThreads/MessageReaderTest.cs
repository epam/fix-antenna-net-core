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

using Epam.FixAntenna.NetCore.FixEngine.Session.IoThreads;
using Epam.FixAntenna.NetCore.FixEngine.Session.IOThreads.Helper;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.FixEngine.Storage;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;

using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.IOThreads
{
	[TestFixture]
	internal class MessageReaderTest
	{
		private static readonly FixMessage Message = RawFixUtil.GetFixMessage("8=FIX.4.4\u00019=74\u000135=e\u000149=TESTI\u000156=TESTA\u000134=1\u000152=20030204-09:25:43\u0001324=0001\u000155=TESTB\u0001263=0\u000110=071\u0001".AsByteArray());

		private IMessageReader _messageReader;
		private IMessageStorage _messageStorage;
		private CompositeMessageHandlerHelper _handlerListener;
		private FixTransportHelper _fixTransport;
		private TestFixSession _session;
		private FixMessage _message;

		[SetUp]
		public void Before()
		{
			_session = new TestFixSession();
			_fixTransport = new FixTransportHelper();
			_handlerListener = new CompositeMessageHandlerHelper(_session);
			_messageStorage = new InMemoryStorageFactory(null).CreateIncomingMessageStorage(_session.Parameters);
			try
			{
				var sessionParametersInstance = _session.Parameters;
				_messageReader = new MessageReader(_session, _messageStorage, _handlerListener, _fixTransport);
				var conf = new ConfigurationAdapter(sessionParametersInstance.Configuration);
				_messageReader.Init(conf);
			}
			catch (IOException)
			{
			}

			_messageReader.GracefulShutdown = true;
			_messageReader.Start();
		}

		[TearDown]
		public virtual void After()
		{
			try
			{
				_fixTransport.Close();
			}
			catch (IOException)
			{
			}
			_handlerListener.Destroy();
			_messageReader.Shutdown();
		}

		/// <summary>
		/// Test checked all chain of call handlers.
		/// </summary>
		[Test]
		public virtual void SendOneMessage()
		{
			ExchangeWithMessages();
			ClassicAssert.IsNotNull(_message);
		}

		private void ExchangeWithMessages()
		{
			_fixTransport.SetMessage(Message);
			_message = _handlerListener.GetMessage();
		}

		/// <summary>
		/// Test checked incremental handler.
		/// </summary>
		[Test]
		public virtual void CheckIncrementHandler()
		{
			var seqNumBefore = _session.RuntimeState.InSeqNum;
			ExchangeWithMessages();
			Thread.Sleep(500);
			var seqNumAfter = _session.RuntimeState.InSeqNum;
			ClassicAssert.IsTrue(seqNumBefore != seqNumAfter, "sequence should not be equals: " + seqNumBefore + ", " + seqNumAfter);
		}
	}
}
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
using System.Threading.Tasks;

using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Session.Impl;
using Epam.FixAntenna.NetCore.FixEngine.Session.IoThreads;
using Epam.FixAntenna.NetCore.FixEngine.Session.IOThreads;
using Epam.FixAntenna.NetCore.FixEngine.Session.IOThreads.Helper;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.FixEngine.Storage;
using Epam.FixAntenna.NetCore.FixEngine.Storage.Queue;
using Epam.FixAntenna.NetCore.FixEngine.Transport;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	internal class AbstractFixSessionHelper : AbstractFixSession
	{
		public AbstractFixSessionHelper() : this("host")
		{
		}

		public AbstractFixSessionHelper(string host) : this(GetInitSessionParameters(host))
		{
		}

		public AbstractFixSessionHelper(SessionParameters @params) : base(new Fix44MessageFactory(), @params,
#pragma warning disable CA2000 // Dispose objects before losing scope: disposed in AbstractFIXSession
			new HandlerChain())
#pragma warning restore CA2000 // Dispose objects before losing scope
		{
			Transport = new FixTransportHelper();

			//SessionParameters sessionParametersInstance = getSessionParametersInstance();
			//runtimeState.initInSequenceWithStorageNumber(reader.Init(configuration));
		}

		public override IMessageReader BuildMessageReader(IMessageStorage incomingMessageStorage, HandlerChain listener, IFixTransport transport)
		{
#pragma warning disable CA2000 // Dispose objects before losing scope: disposed in MessageReader
			var messageStorage = new TestMessageStorage();
#pragma warning restore CA2000 // Dispose objects before losing scope

			return new MessageReader(this, messageStorage, new CompositeMessageHandlerListenerAnonymousInnerClass(this), transport);
		}

		internal class CompositeMessageHandlerListenerAnonymousInnerClass : ICompositeMessageHandlerListener
		{
			private readonly AbstractFixSessionHelper _outerInstance;

			public CompositeMessageHandlerListenerAnonymousInnerClass(AbstractFixSessionHelper outerInstance)
			{
				_outerInstance = outerInstance;
			}

			public void OnMessage(MsgBuf messageBuf)
			{
			}
		}

		public override IMessagePumper BuildMessagePumper(ConfigurationAdapter configuration, IQueue<FixMessageWithType> queue, IMessageStorage outgoingMessageStorage, IFixMessageFactory messageFactory, IFixTransport transport, ISessionSequenceManager sequenceManager)
		{
#pragma warning disable CA2000 // Dispose objects before losing scope: disposed in SyncMessagePumper
			var messageStorage = new TestMessageStorage();
#pragma warning restore CA2000 // Dispose objects before losing scope

			return new SyncMessagePumper(this, queue, messageStorage, new StandardMessageFactory(), transport, sequenceManager);
		}

		protected internal static SessionParameters GetInitSessionParameters(string host)
		{
			var sessionParams = new SessionParameters();
			sessionParams.FixVersion = FixVersion.Fix44;
			sessionParams.TargetCompId = "T";
			sessionParams.SenderCompId = "S";
			sessionParams.HeartbeatInterval = 2;
			sessionParams.Configuration.SetProperty(Config.HbtReasonableTransmissionTime, 200);
			sessionParams.Host = host;
			return sessionParams;
		}

		public override void Connect()
		{
		}

		public override Task ConnectAsync()
		{
			return Task.CompletedTask;
		}

		public override void Reject(string reason)
		{
		}

		public virtual FixMessage GetMessageFromQueue()
		{
			var poll = Queue.Poll();
			return poll.FixMessage;
		}

		public virtual FixMessageWithType GetMessageWithTypeFromQueue()
		{
			return Queue.Poll();
		}

		public virtual int GetMessageQueueSize()
		{
			return Queue.Size;
		}

		public virtual void ResetQueue()
		{
			Queue.Clear();
		}

		public virtual void SetConfigProperty(string propertyName, string value)
		{
			Parameters.Configuration.SetProperty(propertyName, value);
		}

		public virtual void SetOutOfTurnOnlyMode(bool b)
		{
			base.SetOutOfTurnMode(b);
		}

		public virtual void ResetLastInMessageTimestamp()
		{
			Reader.MessageProcessedTimestamp = DateTimeHelper.CurrentMilliseconds;
		}

		public override void Shutdown(DisconnectReason reason, bool blocking)
		{
			base.Shutdown(reason, blocking);
			try
			{
				IncomingStorage.Dispose();
				OutgoingStorage.Dispose();
			}
			catch (IOException e)
			{
				Log.Debug(e);
			}
		}
	}
}
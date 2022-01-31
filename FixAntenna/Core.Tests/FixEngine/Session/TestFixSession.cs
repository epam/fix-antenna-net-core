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

using System.Collections.Generic;

using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.FixEngine.Session.Impl;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler;
using Epam.FixAntenna.NetCore.FixEngine.Storage;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation;
using Epam.FixAntenna.NetCore.Validation.Error;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	internal class TestFixSession : InitiatorFixSession
	{
		private SessionState _sessionState = SessionState.Disconnected;
		private IStorageFactory _storageFactory = new InMemoryStorageFactory(null);
		private IMessageValidator _messageValidator;
		private StandardSessionFactory _factory = new StandardSessionFactory(typeof(Fix44MessageFactory));
		private IFixMessageFactory _messageFactory;

		public TestFixSession() : base(null, new SessionParameters(),
#pragma warning disable CA2000 // Dispose objects before losing scope: disposed in AbstractFIXSession
			new HandlerChain())
#pragma warning restore CA2000 // Dispose objects before losing scope
		{
			_messageValidator = new ValidMessageValidator();

			Parameters.FixVersion = FixVersion.Fix44;
			Parameters.TargetCompId = "T";
			Parameters.SenderCompId = "S";
			Parameters.HeartbeatInterval = 30;
			Parameters.IncomingSequenceNumber = 1;
			Parameters.OutgoingSequenceNumber = 1;

			_messageFactory = _factory.MessageFactory;
			_messageFactory.SetRuntimeState(RuntimeState);
			_messageFactory.SetSessionParameters(Parameters);
		}

		public virtual void RestoreSessionParameters()
		{
			//To change body of implemented methods use File | Settings | File Templates.
		}

		public override void SaveSessionParameters()
		{
			//To change body of implemented methods use File | Settings | File Templates.
		}

		public IList<FixMessage> Messages { get; } = new List<FixMessage>();

		public override IRejectMessageListener RejectMessageListener
		{
			get => null;
			set { }
		}

		public override SessionState SessionState
		{
			get => _sessionState;
			set => _sessionState = value;
		}

		public override void ResetSequenceNumbers()
		{
			Parameters.IncomingSequenceNumber = 1;
			Parameters.OutgoingSequenceNumber = 1;
		}

		public override void Connect()
		{
			_sessionState = SessionState.Connected;
		}

		public override void Disconnect(DisconnectReason type, string reason, bool isGracefull, bool isForced, bool continueReading)
		{
			LastDisconnectReason = type;
			DisconnectReason = reason;
			SetPreLogoffSessionState(isForced, continueReading);
		}

		public string DisconnectReason { get; private set; }

		public override bool SendMessage(string type, FixMessage content)
		{
			var message = new FixMessage();
			if (!content.IsTagExists(Tags.MsgType))
			{
				message.AddTag(Tags.MsgType, type);
			}

			message.AddAll(content);
			return SendMessage(message);
		}

		public override bool SendMessage(FixMessage message)
		{

			message.SetPaddedLongTag(Tags.MsgSeqNum, RuntimeState.OutSeqNum + 1, MessageFactory.MinSeqNumFieldsLength, IndexedStorage.MissingTagHandling.AddIfNotExists);

			Parameters.IncomingSequenceNumber++;
			Parameters.OutgoingSequenceNumber++;

			Messages.Add(message);
			return false;
		}

		public override bool SendAsIs(FixMessage message)
		{
			return false;
		}

		public override bool SendAsIs(FixMessage message, FixSessionSendingType options)
		{
			return false;
		}

		public override bool SendWithChanges(FixMessage content, ChangesType? allowedChangesType)
		{
			//To change body of implemented methods use File | Settings | File Templates.
			return false;
		}

		public override bool SendWithChanges(FixMessage content, ChangesType allowedChangesType, FixSessionSendingType options)
		{
			return false;
		}

		public override void Dispose()
		{
			_sessionState = SessionState.Dead;
			base.Dispose();
		}

		public override IMessageValidator MessageValidator => _messageValidator;

		public void SetMessageValidator(IMessageValidator messageValidator)
		{
			_messageValidator = messageValidator;
		}

		public override void SetFixSessionListener(IFixSessionListener listener)
		{
		}

		public override IExtendedFixSessionListener ExtendedFixSessionListener => new DummyListenerImpl();

		private class DummyListenerImpl : IExtendedFixSessionListener
		{
			public void OnMessageReceived(MsgBuf msgBuf)
			{
			}

			public void OnMessageSent(byte[] bytes, int offset, int length)
			{
			}

			public void OnSessionStateChange(SessionState sessionState)
			{
			}

			public void OnNewMessage(FixMessage message)
			{
			}
		}

		public override void Shutdown(DisconnectReason reason, bool blocking)
		{
			_sessionState = SessionState.Dead;
		}

		public override IMessageStorage IncomingStorage => _storageFactory.CreateIncomingMessageStorage(Parameters);

		public override IMessageStorage OutgoingStorage => _storageFactory.CreateOutgoingMessageStorage(Parameters);

		public override IFixMessageFactory MessageFactory => _messageFactory;

		public override long LastInMessageTimestamp => 0;

		public override long IsEstablished => 0;

		public override long BytesSent => 0;

		public override long BytesRead => 0;

		public override long NoOfInMessages => 0;

		public override long NoOfOutMessages => 0;

		public override long LastOutMessageTimestamp => DateTimeHelper.CurrentMilliseconds;

		public override bool SendMessageOutOfTurn(string type, FixMessage list)
		{
			return SendMessage(type, list);
		}

		public override IErrorHandler ErrorHandler
		{
			get => new LoggingErrorHandler();
			set { }
		}

		public override void MarkShutdownAsGraceful()
		{
		}

		public override void ClearQueue()
		{
		}

		internal class ValidMessageValidator : IMessageValidator
		{
			private readonly IValidationResult _validResult = new ValidationResultImpl();

			private class ValidationResultImpl : IValidationResult
			{
				public bool IsMessageValid => true;

				public FixErrorContainer Errors => null;

				public void Reset()
				{
				}
			}

			public IValidationResult Validate(FixMessage message)
			{
				return _validResult;
			}

			public void Validate(FixMessage message, IValidationResult result)
			{
				//result.
			}

			public IValidationResult ValidateContent(string msgType, FixMessage content)
			{
				return _validResult;
			}

			public bool ContentValidation { get; set; }
		}
	}
}
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
using System.Threading.Tasks;

using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.FixEngine.Storage;
using Epam.FixAntenna.NetCore.FixEngine.Storage.Queue;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation;

namespace Epam.FixAntenna.AdminTool.Tests
{
	/// <summary>
	/// The admin test session.
	/// </summary>
	internal sealed class AdminFixSession : IExtendedFixSession
	{
		private readonly long _established = DateTimeHelper.CurrentMilliseconds;
		private bool _isDisposed = false;

		public AdminFixSession()
		{
		}

		public void RestoreSessionParameters()
		{
		}

		public void SaveSessionParameters()
		{
		}

		public bool SendAsIs(FixMessage message)
		{
			return false;
		}

		public bool SendAsIs(FixMessage message, FixSessionSendingType optionMask)
		{
			return false;
		}

		public bool SendWithChanges(FixMessage content, ChangesType? allowedChangesType)
		{
			return false;
		}

		public bool SendWithChanges(FixMessage content, ChangesType allowedChangesType, FixSessionSendingType optionMask)
		{
			return false;
		}

		public IRejectMessageListener RejectMessageListener
		{
			get => null;
			set { }
		}

		public void SetOutOfTurnMode(bool mode)
		{
		}

		public AdminFixSession(long established) : this()
		{
			_established = established;
		}

		public FixMessage Message { get; private set; }

		public FixSessionRuntimeState RuntimeState { set; get; } = new FixSessionRuntimeState();

		public IExtendedFixSessionListener ExtendedFixSessionListener => null;

		public SessionState SessionState
		{
			get => SessionState.Connected;
			set { }
		}

		/// <inheritdoc/>
		public FixMessage PrepareMessage(FixMessage message, MessageStructure mesStructure)
		{
			var pmu = new PreparedMessageUtil(new SessionParameters());
			return pmu.PrepareMessage(message, mesStructure);
		}


		/// <inheritdoc/>
		public FixMessage PrepareMessage(FixMessage message, string type, MessageStructure mesStructure)
		{
			var pmu = new PreparedMessageUtil(new SessionParameters());
			return pmu.PrepareMessage(message, type, mesStructure);
		}

		/// <inheritdoc/>
		public FixMessage PrepareMessage(string msgType, MessageStructure userStructure)
		{
			var pmu = new PreparedMessageUtil(new SessionParameters());
			return pmu.PrepareMessage(msgType, userStructure);
		}

		/// <inheritdoc cref="IFixSession.PrepareMessageFromString(byte[], MessageStructure)"/>
		public FixMessage PrepareMessageFromString(byte[] message, MessageStructure structure)
		{
			var pmu = new PreparedMessageUtil(new SessionParameters());
			return pmu.PrepareMessageFromString(message, structure);
		}

		/// <inheritdoc cref="IFixSession.PrepareMessageFromString(byte[], String, MessageStructure)"/>
		public FixMessage PrepareMessageFromString(byte[] message, string type, MessageStructure structure)
		{
			var pmu = new PreparedMessageUtil(new SessionParameters());
			return pmu.PrepareMessageFromString(message, type, structure);
		}

		public void Shutdown(DisconnectReason reason, bool blocking)
		{
		}

		public SessionParameters Parameters { get; internal set; } = new SessionParameters();

		public IMessageStorage IncomingStorage => null;

		public void ResetSequenceNumbers()
		{
		}

		public void ResetSequenceNumbers(bool b)
		{
			Parameters.IncomingSequenceNumber = 1;
			Parameters.OutgoingSequenceNumber = 1;
		}

		public void SetSequenceNumbers(long inSeqNum, long outSeqNum)
		{
		}

		public IMessageStorage OutgoingStorage => null;

		public IQueue<FixMessageWithType> MessageQueue => null;

		public IFixMessageFactory MessageFactory => null;

		public long LastInMessageTimestamp => DateTimeHelper.CurrentMilliseconds;

		public long IsEstablished => _established;

		public bool IsStatisticEnabled => true;

		public long BytesSent => 10;

		public long BytesRead => 10;

		public long NoOfInMessages => 7;

		public void ResetSequenceNumbers(int outgoingSequenceNumber, int incomingSequenceNumber)
		{
			Parameters.IncomingSequenceNumber = outgoingSequenceNumber;
			Parameters.OutgoingSequenceNumber = incomingSequenceNumber;
		}

		public long NoOfOutMessages => 2;

		public long LastOutMessageTimestamp => _established;

		public void SetAttribute(string key, object obj)
		{
		}

		public void SetAttribute(ExtendedFixSessionAttribute key, object obj)
		{
		}

		public object GetAttribute(ExtendedFixSessionAttribute key)
		{
			return null;
		}

		public object GetAttribute(string key)
		{
			return null;
		}

		public bool GetAttributeAsBool(ExtendedFixSessionAttribute key)
		{
			return false;
		}

		public long GetAttributeAsLong(ExtendedFixSessionAttribute key)
		{
			return 0;
		}

		public void SetAttribute(ExtendedFixSessionAttribute key, bool v)
		{
		}

		public void SetAttribute(ExtendedFixSessionAttribute key, long v)
		{
		}


		public void RemoveAttribute(string key)
		{
		}

		public void SubscribeForAttributeChanges(ExtendedFixSessionAttribute attr,
			IExtendedFixSessionAttributeListener listener)
		{
		}

		public void Init()
		{
		}

		public bool SendMessageOutOfTurn(string type, FixMessage list)
		{
			return SendMessage(type, list);
		}

		public IErrorHandler ErrorHandler
		{
			get => null;
			set { }
		}

		public void MarkShutdownAsGraceful()
		{
		}

		public void ClearQueue()
		{
		}

		public DisconnectReason LastDisconnectReason => null;

		public IFixSessionOutOfSyncListener FixSessionOutOfSyncListener { get; set; } = null;

		public void Connect()
		{
		}

		public Task ConnectAsync()
		{
			return Task.CompletedTask;
		}

		public void Reject(string reason)
		{
		}

		public void Disconnect(string reason)
		{
		}

		public Task DisconnectAsync(string reason)
		{
			return Task.CompletedTask;
		}

		public void Disconnect(DisconnectReason reasonType, string reasonDescription)
		{
		}

		public void ForcedDisconnect(DisconnectReason reasonType, string reason, bool continueReading)
		{
		}

		public bool SendMessage(string type, FixMessage content)
		{
			return SendMessage(type, content, 0);
		}

		public bool SendMessage(string type, FixMessage content, FixSessionSendingType optionsMask)
		{
			Message = content;
			Message.AddTag(35, type);
			return false;
		}

		public bool SendMessage(FixMessage message, FixSessionSendingType optionsMask)
		{
			Message = message;
			return false;
		}

		public int SendMessageAndGetQueueSize(string type, FixMessage content, FixSessionSendingType optionMask)
		{
			Message = content;
			Message.AddTag(35, type);
			return 1; // not 0 - we do not sent this message sync
		}

		public int SendWithChangesAndGetQueueSize(FixMessage content, ChangesType allowedChangesType, FixSessionSendingType options)
		{
			Message = content;
			return 1; // not 0 - we do not sent this message sync
		}

		public int SendMessageAndGetQueueSize(FixMessage message, FixSessionSendingType optionMask)
		{
			Message = message;
			return 1; // not 0 - we do not sent this message sync
		}

		public int QueuedMessagesCount
		{
			get
			{
				return 0; //To change body of implemented methods use File | Settings | File Templates.
			}
		}

		public bool SendMessage(FixMessage message)
		{
			Message = message;
			return false;
		}

		public void Dispose()
		{
			_isDisposed = true;
		}

		public bool IsDisposed()
		{
			return _isDisposed;
		}

		public IMessageValidator MessageValidator => null;

		public void SetFixSessionListener(IFixSessionListener listener)
		{
		}

		public void AddInSessionLevelMessageListener(IFixMessageListener listener)
		{
		}

		public void AddOutSessionLevelMessageListener(ITypedFixMessageListener listener)
		{
		}

		public void AddUserGlobalMessageHandler(AbstractUserGlobalMessageHandler userMessageHandler)
		{
		}

		public IFixSessionSlowConsumerListener SlowConsumerListener
		{
			set { }
		}

		public void LockSending()
		{

		}

		public void UnlockSending()
		{

		}

		public long InSeqNum
		{
			get => 0;
			set { }
		}

		public long OutSeqNum
		{
			get => 0;
			set { }
		}

		public object GetAndRemoveAttribute(string key)
		{
			throw new NotImplementedException();
		}

		public bool TryStartSendingLogout()
		{
			return true;
		}

		public int OutgoingQueueSize => 0;

		public IList<IEnqueuedFixMessage> GetOutgoingQueueMessages()
		{
			return new List<IEnqueuedFixMessage>();
		}

		public byte[] RetrieveSentMessage(long seqNumber)
		{
			return null;
		}

		public byte[] RetrieveReceivedMessage(long seqNumber)
		{
			return null;
		}
	}
}
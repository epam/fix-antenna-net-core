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

using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.FixEngine.Storage;
using Epam.FixAntenna.NetCore.FixEngine.Storage.Queue;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	/// <summary>
	/// Extended fix session.
	/// </summary>
	internal interface IExtendedFixSession : IFixSession
	{
		/// <summary>
		/// Gets extended fix session listener.
		/// </summary>
		IExtendedFixSessionListener ExtendedFixSessionListener { get; }

		/// <summary>
		/// Shutdown the session with specified reason.
		/// </summary>
		/// <param name="reason">   disconnect reason </param>
		/// <param name="blocking"> If the parameter is true, the next call of method is blocking. </param>
		void Shutdown(DisconnectReason reason, bool blocking);

		/// <summary>
		/// Returns true if the fix session has marked as started sending first or reply logout message
		/// Returns false if the fix session was already marked before this call
		/// </summary>
		/// <returns></returns>
		bool TryStartSendingLogout();

		void Disconnect(DisconnectReason reasonType, string reasonDescription);

		/// <summary>
		/// Gracefully disconnects current session but wait for answer forcedLogoffTimeout.
		///
		/// Logoff with specified reason will be sent to your counterparty. Session will wait the answer for some period,
		/// defined by forcedLogoffTimeout configuration property.
		/// <i>Note: This method doesn't guarantee immediate shutdown,
		/// since FIX protocol require us to wait for counterparty logoff reply.
		/// </i>
		/// <i>Note: Use this method if you need to close session in exceptional cases and when there is a chance that
		/// counterparty newer answer.
		/// </i>
		/// </summary>
		/// <param name="reason">          the reason </param>
		/// <param name="continueReading"> if false then reading of incoming messages will be stopped. This can be used for prevent
		///                        reading of messages with broken sequencing. </param>
		void ForcedDisconnect(DisconnectReason reasonType, string reason, bool continueReading);

		/// <summary>
		/// Gets session parameter instance.
		/// </summary>
		FixSessionRuntimeState RuntimeState { get; }

		/// <summary>
		/// Gets incoming message storage.
		/// </summary>
		/// <value> message storage </value>
		IMessageStorage IncomingStorage { get; }

		/// <summary>
		/// Gets outgoing message storage.
		/// </summary>
		/// <value> message storage </value>
		IMessageStorage OutgoingStorage { get; }

		/// <summary>
		/// Gets internal message queue.
		/// </summary>
		/// <value> message storage </value>
		IQueue<FixMessageWithType> MessageQueue { get; }

		/// <summary>
		/// Save session parameters to file.
		/// </summary>
		/// <exception cref="IOException"> if I/O error occurred </exception>
		void SaveSessionParameters();

		/// <summary>
		/// Gets message factory.
		/// </summary>
		/// <value> message factory </value>
		IFixMessageFactory MessageFactory { get; }

		/// <summary>
		/// Gets last received message timestamp in milliseconds.
		/// </summary>
		/// <value> timestamp </value>
		long LastInMessageTimestamp { get; }

		/// <summary>
		/// Gets established session timestamp in milliseconds.
		/// </summary>
		/// <value> timestamp </value>
		long IsEstablished { get; }

		/// <summary>
		/// Shows statistics on or off.
		/// </summary>
		/// <returns> true is statistic is enabled </returns>
		/// <seealso cref="IExtendedFixSession.BytesSent"></seealso>
		/// <seealso cref="IExtendedFixSession.BytesRead"></seealso>
		/// <seealso cref="IExtendedFixSession.NoOfInMessages"></seealso>
		/// <seealso cref="IExtendedFixSession.NoOfOutMessages"></seealso>
		bool IsStatisticEnabled { get; }

		/// <summary>
		/// Gets send bytes.
		/// </summary>
		/// <value> number of send bytes or -1 if statistic is disabled </value>
		long BytesSent { get; }

		/// <summary>
		/// Gets read bytes.
		/// </summary>
		/// <value> number of read bytes or -1 if statistic is disabled </value>
		long BytesRead { get; }

		/// <summary>
		/// Gets number of received message.
		/// </summary>
		/// <value> number of received message or -1 if statistic is disabled </value>
		long NoOfInMessages { get; }

		/// <summary>
		/// Gets number of sent message.
		/// </summary>
		/// <value> number of sent message or -1 if statistic is disabled </value>
		long NoOfOutMessages { get; }

		/// <summary>
		/// Gets time when the message is sent
		/// </summary>
		/// <value> time when the message is sent or -1 if statistic is disabled </value>
		long LastOutMessageTimestamp { get; }

		/// <summary>
		/// Sets session attribute.
		/// </summary>
		/// <param name="key">    the attribute key </param>
		/// <param name="value"> the attribute value </param>
		void SetAttribute(string key, object value);

		void SetAttribute(ExtendedFixSessionAttribute key, object value);

		/// <summary>
		/// Gets session attribute value.
		/// </summary>
		/// <param name="key"> the attribute key </param>
		/// <returns> attribute value </returns>
		object GetAttribute(string key);

		object GetAndRemoveAttribute(string key);

		object GetAttribute(ExtendedFixSessionAttribute key);

		long GetAttributeAsLong(ExtendedFixSessionAttribute attr);

		void SetAttribute(ExtendedFixSessionAttribute attr, long value);

		void SetAttribute(ExtendedFixSessionAttribute attr, bool value);

		bool GetAttributeAsBool(ExtendedFixSessionAttribute attr);

		/// <summary>
		/// Removes session attribute.
		/// </summary>
		/// <param name="key"> the attribute key </param>
		void RemoveAttribute(string key);

		void SubscribeForAttributeChanges(ExtendedFixSessionAttribute attr, IExtendedFixSessionAttributeListener listener);

		/// <summary>
		/// Sends message out of turn.
		/// </summary>
		/// <param name="msgType"> the message type </param>
		/// <param name="message"> the message </param>
		/// <returns> true if message was send immediately, false - message was queued for later sending </returns>
		bool SendMessageOutOfTurn(string msgType, FixMessage message);

		/// <summary>
		/// Sets OutOf turn mode.
		/// </summary>
		/// <param name="mode"> the mode </param>
		void SetOutOfTurnMode(bool mode);

		/// <summary>
		/// Do the same that <seealso cref="IFixSession.SendMessage(string, FixMessage, FixSessionSendingType)"/> and return queue size.
		/// </summary>
		/// <returns> queue size. 0 if message was send synchronously </returns>
		/// <seealso cref="IFixSession.SendMessage(String, FixMessage, FixSessionSendingType)"></seealso>
		int SendMessageAndGetQueueSize(string type, FixMessage content, FixSessionSendingType optionMask);

		/// <summary>
		/// Do the same that <seealso cref="IFixSession.SendWithChanges(FixMessage, ChangesType, FixSessionSendingType)"/> and return queue size.
		/// </summary>
		/// <returns> queue size. 0 if message was send synchronously </returns>
		/// <seealso cref="IFixSession.SendWithChanges(FixMessage, ChangesType, FixSessionSendingType)"></seealso>
		int SendWithChangesAndGetQueueSize(FixMessage content, ChangesType allowedChangesType, FixSessionSendingType options);

		/// <summary>
		/// Do the same that <seealso cref="IFixSession.SendMessage(FixMessage, FixSessionSendingType)"/> and return queue size.
		/// </summary>
		/// <returns> queue size. 0 if message was send synchronously </returns>
		/// <seealso cref="IFixSession.SendMessage(FixMessage, FixSessionSendingType)"> </seealso>
		int SendMessageAndGetQueueSize(FixMessage message, FixSessionSendingType optionMask);

		int QueuedMessagesCount { get; }

		/// <summary>
		/// Marks the session shutdown as gracefully.
		/// </summary>
		void MarkShutdownAsGraceful();

		/// <summary>
		/// Clears output message queue.
		/// Method also reject all messages from the queue.
		/// To use this feature, the user should set the <c>RejectMessageListener</c> listener.
		/// </summary>
		// TODO: <returns> true if session was able to reject all messages and clean queue (enableMessageRejecting option is enabled)
		void ClearQueue();

		DisconnectReason LastDisconnectReason { get; }

		IFixSessionOutOfSyncListener FixSessionOutOfSyncListener { set; get; }

		void AddUserGlobalMessageHandler(AbstractUserGlobalMessageHandler userMessageHandler);

		void LockSending();

		void UnlockSending();
	}
}
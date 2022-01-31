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
using System.IO;
using System.Threading.Tasks;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation;

namespace Epam.FixAntenna.NetCore.FixEngine
{
	/// <summary>
	/// As far as .Net Standard 2.0 does not allow define constants in interfaces
	/// new Enum was introduced.
	/// </summary>
	[Flags]
	public enum FixSessionSendingType
	{
		/// <summary>
		/// Enqueue the message before sending as opposed to sending immediately from calling thread.
		/// </summary>
		SendAsync = 1,
		/// <summary>
		/// Send the message synchronously.
		/// </summary>
		SendSync = 2,
		/// <summary>
		/// Default SendingType value.
		/// </summary>
		DefaultSendingOption = SendSync
	}

	/// <summary>
	/// This interface is the main interface user works with.
	/// It represents IFixSession either acceptor or initiator.
	/// And capable to send/receive messages.
	/// </summary>
	public interface IFixSession
	{
		/// <summary>
		/// Gets or sets the session state.
		/// </summary>
		SessionState SessionState { get; set; }

		/// <summary>
		/// Builds <see cref="FixMessage"/> object from <see cref="FixMessage"/> object.
		/// </summary>
		/// <param name="message">   <see cref="FixMessage"/> object </param>
		/// <param name="structure"> message structure </param>
		/// <returns> <see cref="FixMessage"/> object </returns>
		/// <exception cref="PreparedMessageException"> </exception>
		FixMessage PrepareMessage(FixMessage message, MessageStructure structure);

		/// <summary>
		/// Builds <see cref="FixMessage"/> object from <see cref="FixMessage"/> object.
		/// </summary>
		/// <param name="message">   <see cref="FixMessage"/> object </param>
		/// <param name="structure"> message structure </param>
		/// <param name="type">      type of the message </param>
		/// <returns> <see cref="FixMessage"/> object </returns>
		/// <exception cref="PreparedMessageException"> </exception>
		FixMessage PrepareMessage(FixMessage message, string type, MessageStructure structure);


		/// <summary>
		/// Builds <see cref="FixMessage"/> object with specified type, message structure and prefilled header information
		/// </summary>
		/// <param name="msgType">       message type </param>
		/// <param name="userStructure"> message structure </param>
		/// <returns> <see cref="FixMessage"/> object </returns>
		FixMessage PrepareMessage(string msgType, MessageStructure userStructure);

		/// <summary>
		/// Builds <see cref="FixMessage"/> object from String object.
		/// </summary>
		/// <param name="message">   message string </param>
		/// <param name="structure"> message structure object </param>
		/// <returns> <see cref="FixMessage"/> object </returns>
		/// <exception cref="PreparedMessageException"> </exception>
		FixMessage PrepareMessageFromString(byte[] message, MessageStructure structure);

		/// <summary>
		/// Builds <see cref="FixMessage"/> object from String object.
		/// </summary>
		/// <param name="message">   message string </param>
		/// <param name="structure"> message structure object </param>
		/// <param name="type">      message type </param>
		/// <returns> <see cref="FixMessage"/> object </returns>
		/// <exception cref="PreparedMessageException"> </exception>
		FixMessage PrepareMessageFromString(byte[] message, string type, MessageStructure structure);

		/// <summary>
		/// Allows user to reset sequences numbers by sending logon with 141=Y.
		///
		/// Notice: Only supported for FIX versions 4.1 and above.
		/// Notice: The logon message will be send only when session is in connected state.
		/// </summary>
		void ResetSequenceNumbers();

		/// <summary>
		/// Allows user to reset sequences numbers by sending logon with 141=Y.
		///
		/// Notice: Only supported for FIX versions 4.1 and above.
		///
		/// Notice: The logon message will be send only when session is in connected state.
		/// </summary>
		/// <param name="checkGapFillBefore"> - the flag indicates if needed to check the seq num.
		///                           <p/>
		///                           If true the TR will be sent before introday logon otherwise only intraday logon will be sent. </param>
		void ResetSequenceNumbers(bool checkGapFillBefore);

		/// <summary>
		/// Allows user to change sequences numbers for disconnected sessions.
		/// </summary>
		/// <param name="inSeqNum"> new incoming sequence number. If new value is &lt; 0, then this parameter will be ignored. </param>
		/// <param name="outSeqNum"> new outgoing sequence number. If new value is &lt; 0, then this parameter will be ignored. </param>
		/// <exception cref="IOException"> </exception>
		void SetSequenceNumbers(long inSeqNum, long outSeqNum);

		/// <summary>
		/// Gets session parameter instance.
		/// </summary>
		SessionParameters Parameters { get; }

		/// <summary>
		/// Initialize FIX session. This allows to put messages to session. These messages will be send after connect.
		/// </summary>
		/// <exception cref="IOException"> I/O exception if error occurred </exception>
		void Init();


		/// <summary>
		/// Connects to remote counterparty,
		/// if initiator or accepts incoming connection if acceptor.
		/// </summary>
		/// <exception cref="IOException"> I/O exception if error occurred </exception>
		void Connect();

		/// <summary>
		/// Connects to remote counterparty,
		/// if initiator or accepts incoming connection if acceptor.
		/// Async version
		/// </summary>
		/// <exception cref="IOException"> I/O exception if error occurred </exception>
		Task ConnectAsync();

		/// <summary>
		/// Reject incoming connection for acceptor.
		///
		/// Not applicable for initiator.
		/// </summary>
		/// <exception cref="IOException"> I/O exception if error occurred </exception>
		void Reject(string reason);

		/// <summary>
		/// Gracefully disconnects current session.
		///
		/// Logoff with specified reason will be sent to your counterparty.
		///
		/// Note: This method doesn't guarantee immediate shutdown, since FIX protocol require us to wait for counterparty logoff reply.
		/// </summary>
		/// <param name="reason"> the reason </param>
		void Disconnect(string reason);

		/// <summary>
		/// Gracefully disconnects current session. Async version.
		///
		/// Logoff with specified reason will be sent to your counterparty.
		///
		/// Note: This method doesn't guarantee immediate shutdown, since FIX protocol require us to wait for counterparty logoff reply.
		/// </summary>
		/// <param name="reason"> the reason </param>
		Task DisconnectAsync(string reason);

		/// <summary>
		/// Convenient method to send out FIX message based on the message type and message content.
		///
		/// Depending on implementation and configuration may send message
		/// immediately or put it in the outgoing queue either persistent or stateless.
		///
		/// If the <c>msgType</c> is null, Engine sends the message as is;
		///
		/// If the <c>msgType</c> is "", Engine updates body length, sequence number, sending time and checksum fields;
		///
		/// Otherwise Engine wraps the <c>content</c>.
		/// <p/>
		/// If session is disposed no more messages should be send by session.
		/// </summary>
		/// <param name="msgType"> the message type (Tag 35 content) </param>
		/// <param name="content"> the message content </param>
		/// <returns> true if message was send immediately, false - message was queued for later sending </returns>
		/// <exception cref="InvalidOperationException"> if session is disposed </exception>
		/// <exception cref="ArgumentException"> </exception>
		bool SendMessage(string msgType, FixMessage content);

		/// <summary>
		/// Convenient method to send out FIX message based on the message type and message content.
		///
		/// Depending on implementation and configuration may send message
		/// immediately or put it in the outgoing queue either persistent or stateless.
		///
		/// If the <c>msgType</c> is null, Engine sends the message as is;
		///
		/// If the <c>msgType</c> is "", Engine updates body length, sequence number, sending time and checksum fields;
		///
		/// Otherwise Engine wraps the <c>content</c>.
		/// <p/>
		/// If session is disposed no more messages should be send by session.
		/// </summary>
		/// <param name="msgType"> the message type (Tag 35 content) </param>
		/// <param name="content"> the message content </param>
		/// <param name="optionMask"> <seealso cref="FixSessionSendingType.SendSync"/> or <seealso cref="FixSessionSendingType.SendAsync"/> </param>
		/// <returns> true if message was send immediately, false - message was queued for later sending </returns>
		/// <exception cref="InvalidOperationException"> if session is disposed </exception>
		/// <exception cref="ArgumentException"> </exception>
		bool SendMessage(string msgType, FixMessage content, FixSessionSendingType optionMask);

		/// <summary>
		/// Convenient method to send out user built FIX message.
		/// Depending on implementation and configuration may send message
		/// immediately or put it in the outgoing queue either persistent or stateless.
		///
		/// The Engine updates the footer and header in the <c>message</c>.
		/// <p/>
		/// If session is disposed no more messages should be send by session.
		/// </summary>
		/// <param name="message"> the message </param>
		/// <returns> true if message was send immediately, false - message was queued for later sending </returns>
		/// <exception cref="InvalidOperationException"> if session is disposed </exception>
		/// <exception cref="ArgumentException"> </exception>
		bool SendMessage(FixMessage message);

		/// <summary>
		/// Convenient method to send out user built FIX message.
		/// Depending on implementation and configuration may send message
		/// immediately or put it in the outgoing queue either persistent or stateless.
		///
		/// The Engine updates the footer and header in the <c>message</c>.
		/// <p/>
		/// If session is disposed no more messages should be send by session.
		/// </summary>
		/// <param name="message"> the message </param>
		/// <param name="optionMask"> <seealso cref="FixSessionSendingType.SendSync"/> or <seealso cref="FixSessionSendingType.SendAsync"/> </param>
		/// <returns> true if message was send immediately, false - message was queued for later sending </returns>
		/// <exception cref="InvalidOperationException"> if session is disposed </exception>
		/// <exception cref="ArgumentException"> </exception>
		bool SendMessage(FixMessage message, FixSessionSendingType optionMask);

		/// <summary>
		/// Convenient method to send out FIX message based on the message type and message content.
		///
		/// Depending on implementation and configuration may send message
		/// immediately or put it in the outgoing queue either persistent or stateless.
		///
		/// The Engine sends the message as is.
		/// <p/>
		/// If session is disposed no more messages should be send by session.
		/// </summary>
		/// <param name="message"> the message content </param>
		/// <returns> true if message was send immediately, false - message was queued for later sending </returns>
		/// <exception cref="InvalidOperationException"> if session is disposed </exception>
		/// <exception cref="ArgumentException"> </exception>
		bool SendAsIs(FixMessage message);

		/// <summary>
		/// Convenient method to send out FIX message based on the message type and message content.
		///
		/// Depending on implementation and configuration may send message
		/// immediately or put it in the outgoing queue either persistent or stateless.
		///
		/// The Engine sends the message as is.
		/// <p/>
		/// If session is disposed no more messages should be send by session.
		/// </summary>
		/// <param name="message"> the message content </param>
		/// <param name="optionMask"> <seealso cref="FixSessionSendingType.SendSync"/> or <seealso cref="FixSessionSendingType.SendAsync"/> </param>
		/// <returns> true if message was send immediately, false - message was queued for later sending </returns>
		/// <exception cref="InvalidOperationException"> if session is disposed </exception>
		/// <exception cref="ArgumentException"> </exception>
		bool SendAsIs(FixMessage message, FixSessionSendingType optionMask);

		/// <summary>
		/// Convenient method to send out FIX message based on the message type and message content.
		///
		/// Depending on implementation and configuration may send message
		/// immediately or put it in the outgoing queue either persistent or stateless.
		///
		/// The Engine sends the message as is.
		/// <p/>
		/// If session is disposed no more messages should be send by session.
		/// </summary>
		/// <param name="content">            the message content </param>
		/// <param name="allowedChangesType"> the parameter takes the next values: </param>
		/// <returns> true if message was send immediately, false - message was queued for later sending </returns>
		/// <exception cref="InvalidOperationException"> if session is disposed </exception>
		/// <exception cref="ArgumentException"> </exception>
		/// <seealso cref="ChangesType"> </seealso>
		bool SendWithChanges(FixMessage content, ChangesType? allowedChangesType);

		/// <summary>
		/// Convenient method to send out FIX message based on the message type and message content.
		///
		/// Depending on implementation and configuration may send message
		/// immediately or put it in the outgoing queue either persistent or stateless.
		///
		/// The Engine sends the message as is.
		/// <p/>
		/// If session is disposed no more messages should be send by session.
		/// </summary>
		/// <param name="content">            the message content </param>
		/// <param name="allowedChangesType"> the parameter takes the next values: </param>
		/// <param name="optionMask"> <seealso cref="FixSessionSendingType.SendSync"/> or <seealso cref="FixSessionSendingType.SendAsync"/> </param>
		/// <returns> true if message was send immediately, false - message was queued for later sending </returns>
		/// <exception cref="InvalidOperationException"> if session is disposed </exception>
		/// <exception cref="ArgumentException"> </exception>
		/// <seealso cref="ChangesType"> </seealso>
		bool SendWithChanges(FixMessage content, ChangesType allowedChangesType, FixSessionSendingType optionMask);

		/// <summary>
		/// Disposes current session, removes its reference from GlobalSessionManager
		/// and frees all allocated resources. Normally should be called
		/// after disconnect().
		///
		/// However it is possible to call this method for active session without
		/// previous disconnect() that will abnormally terminate fix session by closing fix connection
		/// (without logon exchange). This is useful to terminate stuck sessions
		/// if standard FIX logoff procedure doesn't work.
		/// </summary>
		void Dispose();

		/// <summary>
		/// Returns message validator.
		/// </summary>
		/// <value> message validator for current session </value>
		IMessageValidator MessageValidator { get; }

		/// <summary>
		/// Sets IFIXSession listener to monitor session
		/// status and receive incoming messages.
		/// </summary>
		/// <param name="listener"> the user specified listener </param>
		void SetFixSessionListener(IFixSessionListener listener);


		/// <summary>
		/// Add listeners to receive session level incoming messages.
		/// FIX session level message type:
		/// <ul>
		/// <li> 'A' - Logon </li>
		/// <li> '0' - Heartbeat </li>
		/// <li> '1' - Test Request </li>
		/// <li> '2' - Resend Request </li>
		/// <li> '3' - Reject </li>
		/// <li> '4' - Sequence Reset </li>
		/// <li> '5' - Logout </li>
		/// </ul>
		/// </summary>
		/// <param name="listener"> the user specified listener </param>
		void AddInSessionLevelMessageListener(IFixMessageListener listener);

		void AddOutSessionLevelMessageListener(ITypedFixMessageListener listener);

		/// <summary>
		/// Sets error handler.
		/// </summary>
		/// <seealso cref="IErrorHandler"> </seealso>
		IErrorHandler ErrorHandler { get; set; }

		/// <summary>
		/// Sets reject listener.
		/// </summary>
		/// <seealso cref="IRejectMessageListener"> </seealso>
		IRejectMessageListener RejectMessageListener { get; set; }

		/// <summary>
		/// Sets slow consumer message listener.
		/// </summary>
		/// <value> slow consumer listener </value>
		/// <seealso cref="IFixSessionSlowConsumerListener"> </seealso>
		IFixSessionSlowConsumerListener SlowConsumerListener { set; }

		long InSeqNum { get; set; }
		long OutSeqNum { get; set; }

		/// <summary>
		/// Returns outgoing message queue size
		/// </summary>
		/// <value></value>
		int OutgoingQueueSize { get; }

		/// <summary>
		/// Returns copies of outgoing messages from the queue
		/// </summary>
		/// <returns></returns>
		IList<IEnqueuedFixMessage> GetOutgoingQueueMessages();


		/// <summary>
		/// Retrieves sent message from the storage.
		/// </summary>
		/// <param name="seqNumber">the sequence number of message</param>
		/// <returns>the retrieved message</returns>
		/// <exception cref="System.IO.IOException">if error occurred.</exception>
		byte[] RetrieveSentMessage(long seqNumber);

		/// <summary>
		/// Retrieves received message from the storage.
		/// </summary>
		/// <param name="seqNumber">the sequence number of message</param>
		/// <returns>the retrieved message</returns>
		/// <exception cref="System.IO.IOException">if error occurred.</exception>
		byte[] RetrieveReceivedMessage(long seqNumber);
	}
}
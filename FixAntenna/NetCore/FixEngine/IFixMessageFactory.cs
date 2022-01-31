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

using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Message.Format;

namespace Epam.FixAntenna.NetCore.FixEngine
{
	/// <summary>
	/// FixMessageFactory interface defines object capable to create session level messages.
	/// User could extend existing standard MessageFactories or even write his own custom message factory
	/// </summary>
	internal interface IFixMessageFactory
	{
		/// <summary>
		/// Creates reject message.
		/// </summary>
		/// <param name="rejectMessage"> the rejected message </param>
		/// <param name="refTagId">      the reference tag </param>
		/// <param name="rejectReason">  the reject reason </param>
		/// <param name="rejectText">    the reject text </param>
		FixMessage GetRejectForMessageTag(FixMessage rejectMessage, int refTagId, int rejectReason,
			string rejectText);

		/// <summary>
		/// Setter for session parameters used to construct message headers.
		/// </summary>
		/// <param name="sessionParameters"> the session parameters </param>
		void SetSessionParameters(SessionParameters sessionParameters);

		void SetRuntimeState(FixSessionRuntimeState runtimeState);

		/// <summary>
		/// Build message based on a type and content.
		/// </summary>
		/// <param name="msgType"> the message type </param>
		/// <param name="content"> the message content </param>
		/// <returns> bytes of message </returns>
		void Serialize(MsgBuf buf, string msgType, FixMessage content, ByteBuffer buffer, SerializationContext context);

		/// <summary>
		/// Build message based on a type and content.
		/// </summary>
		/// <param name="content">     the message content </param>
		/// <param name="changesType"> the change type </param>
		/// <returns> bytes of message </returns>
		void Serialize(FixMessage content, ChangesType? changesType, ByteBuffer buffer, SerializationContext context);

		/// <summary>
		/// Get for current fix version the maximum sequence number.
		/// </summary>
		/// <returns> sequence number </returns>
		long GetEndSequenceNumber();

		/// <summary>
		/// Gets current sending time.
		/// </summary>
		/// <returns> bytes of sending time </returns>
		byte[] GetCurrentSendingTime();

		/// <summary>
		/// Returns appropriate SendingTime implementation
		/// </summary>
		/// <value> SendingTime implementation </value>
		ISendingTime SendingTime { get; }

		/// <summary>
		/// Completes the message.
		/// </summary>
		/// <param name="msgType"> the message type </param>
		/// <param name="content"> the content of message </param>
		/// <returns> message </returns>
		FixMessage CompleteMessage(string msgType, FixMessage content);

		/// <summary>
		/// Minimal length for the SeqNum fields.
		/// The SeqNum fields are:
		///		BeginSeqNo(7), EndSeqNo(16), MsgSeqNum(34), NewSeqNo(36), RefSeqNum(45),
		///		LastMsgSeqNumProcessed(369), HopRefID(630), NextExpectedMsgSeqNum(789)
		/// </summary>
		/// <remarks>
		/// As far as HopRefID field doesn't processed directly by FA .NET Core,
		/// it is up to user to format this field with leading zeroes if required.
		/// The same for any SeqNum field processed by the user's logic:
		/// it is up to user to keep required padding using <see cref="FixMessage.SetPaddedLongTag"/>
		/// or <see cref="ByteBuffer.AddLikeString"/>.
		/// </remarks>
		int MinSeqNumFieldsLength { get; }
	}
}
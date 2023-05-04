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

using System.Text;

using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Message.Format;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.Common
{
	/// <summary>
	/// The abstract fix message factory implementation.
	/// This class provides the base functionality to work with message.
	/// All sub classes should implement <c>AsByteArray</c> method to provide ability
	/// to wrap fix content (adds or updates sequence, body length and checksum fields).
	/// </summary>
	internal abstract class AbstractFixMessageFactory : IFixMessageFactory
	{
		protected const int SeparatorLength = 1;
		protected const char Separator = '\u0001';
		protected const char Equal = '=';

		/// <summary>
		/// Minimum length of the SeqNum type fields.
		/// </summary>
		public int MinSeqNumFieldsLength { get; protected set; } = 1;

		public AbstractFixMessageFactory()
		{
		}

		/// <summary>
		/// Creates reject message.
		/// </summary>
		/// <param name="rejectMessage"> the rejected message </param>
		/// <param name="refTagId">   the reference tag </param>
		/// <param name="rejectReason">  the reject reason </param>
		/// <param name="rejectText">    the reject text </param>
		public virtual FixMessage GetRejectForMessageTag(FixMessage rejectMessage, int refTagId, int rejectReason, string rejectText)
		{
			var message = new FixMessage();

			if (rejectMessage != null)
			{
				message.SetPaddedLongTag(Tags.RefSeqNum, rejectMessage.MsgSeqNumber, MinSeqNumFieldsLength);
			}

			if (refTagId > 0)
			{
				message.AddTag(Tags.RefTagID, refTagId);
			}
			if (rejectMessage?.MsgType != null)
			{
				message.AddTag(Tags.RefMsgType, rejectMessage.MsgType);
			}
			if (rejectReason >= 0)
			{
				message.AddTag(Tags.SessionRejectReason, rejectReason);
			}
			if (!string.IsNullOrWhiteSpace(rejectText))
			{
				message.AddTag(Tags.Text, rejectText);
			}
			return message;
		}

		public virtual void SafeAdd(StringBuilder sb, int tag, string value)
		{
			if (!string.IsNullOrEmpty(value))
			{
				sb.Append(tag).Append(Equal).Append(value).Append(Separator);
			}
		}

		/// <inheritdoc />
		public virtual long GetEndSequenceNumber()
		{
			return 0;
		}

		public virtual void SafeAdd(FixMessage message, int tag, string value)
		{
			if (!string.IsNullOrEmpty(value))
			{
				message.AddTag(tag, value);
			}
		}

		/// <inheritdoc />
		public abstract void SetSessionParameters(SessionParameters sessionParameters);

		/// <inheritdoc />
		public abstract void SetRuntimeState(FixSessionRuntimeState runtimeState);

		/// <inheritdoc />
		public abstract void Serialize(MsgBuf buf, string msgType, FixMessage content, ByteBuffer buffer, SerializationContext context);

		/// <inheritdoc />
		public abstract void Serialize(FixMessage content, ChangesType? changesType, ByteBuffer buffer, SerializationContext context);

		/// <inheritdoc />
		public abstract byte[] GetCurrentSendingTime();

		/// <inheritdoc />
		public abstract ISendingTime SendingTime { get; }

		/// <inheritdoc />
		public abstract FixMessage CompleteMessage(string msgType, FixMessage content);
	}
}
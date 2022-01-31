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
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.PerType.Util
{
	internal class RrMessageCarrierSender
	{
		private readonly TagValue _sendTime = new TagValue();

		protected internal ILog Log;
		private static readonly byte[] YesValue = new byte[]{ (byte)'Y' };

		private IExtendedFixSession _session;

		public RrMessageCarrierSender(IExtendedFixSession session)
		{
			Log = LogFactory.GetLog(GetType());
			_session = session;
		}

		public virtual void SendSequenceReset(byte[] sendingTime)
		{
			var content = new FixMessage();
			var seqNumLength = _session.MessageFactory.MinSeqNumFieldsLength;

			content.AddTag(Tags.PossDupFlag, YesValue);
			content.AddTag(Tags.OrigSendingTime, sendingTime);
			content.AddTag(Tags.GapFillFlag, YesValue);
			content.SetPaddedLongTag(Tags.NewSeqNo, _session.RuntimeState.OutSeqNum + 1, seqNumLength);
			_session.SendMessageOutOfTurn("4", content);
		}

		public virtual void SendRetrievedMessage(byte[] message)
		{
			var list = RawFixUtil.GetFixMessage(message);
			_session.SendMessageOutOfTurn(null, PrepareForResendRequest(list));
		}

		public virtual FixMessage PrepareForResendRequest(FixMessage content)
		{
			var deltaLength = AddOrUpdateOriginSendingTimeField(content) + AddPusDupField(content);
			UpdateBodyLengthField(content, deltaLength);
			UpdateCheckSumField(content);
			return content;
		}

		private int AddPusDupField(FixMessage content)
		{
			if (!content.IsTagExists(Tags.PossDupFlag))
			{
				content.AddTagAtIndex(6, Tags.PossDupFlag, true);
				return FixTypes.FormatIntLength(Tags.PossDupFlag) + 3; // SOH + '=' + 'Y'
			}
			else
			{
				content.SetAtIndex(content.GetTagIndex(Tags.PossDupFlag), true);
				return 0;
			}
		}

		private void UpdateBodyLengthField(FixMessage content, int addLength)
		{
			if (addLength > 0)
			{ // update length if needed
				try
				{
					long oldLength = content.GetTagAsInt(Tags.BodyLength);
					content.Set(Tags.BodyLength, oldLength + addLength);
				}
				catch (FieldNotFoundException e)
				{
					throw new InvalidOperationException("Tag " + Tags.BodyLength + " not found. " + e.Message);
				}
			}
		}

		private void UpdateCheckSumField(FixMessage content)
		{
			content.Set(Tags.CheckSum, FixTypes.FormatCheckSum(content.CalculateChecksum()));
		}

		private int AddOrUpdateOriginSendingTimeField(FixMessage content)
		{
			var deltaLength = 0;
			if (!content.IsTagExists(Tags.OrigSendingTime))
			{
				try
				{
					content.LoadTagValue(Tags.SendingTime, _sendTime);
				}
				catch (FieldNotFoundException e)
				{
					throw new InvalidOperationException("SendingTime (tagId=" + Tags.SendingTime + ") not found. " + e.Message);
				}
				content.AddTagAtIndex(6, Tags.OrigSendingTime, _sendTime.Buffer, _sendTime.Offset, _sendTime.Length);

				deltaLength += 2 + _sendTime.Length; // SOH + '=' + value length
				// + tag length
				var tag = Tags.OrigSendingTime;
				deltaLength += 1;
				while ((tag /= 10) > 0)
				{
					deltaLength++;
				}
			}
			else
			{
				// bug 15990 - send 112 tag with original time
				// deltaLength += sendingTime.GetValue().Length - originalTime.GetValue().Length;
				// originalTime.SetValue(sendingTime.GetValue());
				// deltaLength += 1 + originalTime.getSize();
			}
			try
			{
				var oldValueLength = content.GetTagLength(Tags.SendingTime);
				content.UpdateValue(Tags.SendingTime, _session.MessageFactory.GetCurrentSendingTime(), IndexedStorage.MissingTagHandling.DontAddIfNotExists);
				deltaLength += content.GetTagLength(Tags.SendingTime) - oldValueLength;
			}
			catch (FieldNotFoundException e)
			{
				throw new InvalidOperationException("SendingTime (tagId=" + Tags.SendingTime + ") not found. " + e.Message);
			}
			return deltaLength;
		}

		/// <summary>
		/// Sent gap fill message.
		/// </summary>
		/// <param name="newSeqNo"> the new value of NewSeqNo tag </param>
		public virtual long SendGapFill(long lastSentSeqNum, long newSeqNo, byte[] lastSendingTimeValue)
		{
			var content = new FixMessage();
			var msgFactory = _session.MessageFactory;

			content.AddTag(Tags.PossDupFlag, YesValue);
			content.AddTag(Tags.OrigSendingTime, lastSendingTimeValue);
			content.AddTag(Tags.GapFillFlag, YesValue);
			content.SetPaddedLongTag(Tags.NewSeqNo, newSeqNo, msgFactory.MinSeqNumFieldsLength);

			var message = msgFactory.CompleteMessage(MsgType.SequenceReset, content);
			message.SetPaddedLongTag(Tags.MsgSeqNum, lastSentSeqNum - 1, msgFactory.MinSeqNumFieldsLength, IndexedStorage.MissingTagHandling.AddIfNotExists); // todo: -1 moved to upper level

			message.Set(Tags.BodyLength, message.CalculateBodyLength());
			message.Set(Tags.CheckSum, FixTypes.FormatCheckSum(message.CalculateChecksum()));
			_session.SendMessageOutOfTurn(null, message);

			_session.FixSessionOutOfSyncListener.OnGapFillSent(message);

			return (newSeqNo + 1); // calculate the last sent seq num
		}
	}
}
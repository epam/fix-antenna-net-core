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

using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.Impl
{
	internal class LeaveIdsSerializationStrategy : AbstractSerializationStrategy
	{
		protected internal int SenderCompIdLength;
		protected internal int TargetCompIdLength;
		protected internal int SenderLocationIdLength;
		protected internal int TargetLocationIdLength;
		protected internal int SenderSubIdLength;
		protected internal int TargetSubIdLength;


		public override void Init(SessionParameters sessionParameters)
		{
			base.Init(sessionParameters);
			if (SenderCompId != null)
			{
				SenderCompIdLength = 3 + SenderCompId.Length + SohLength;
			}
			if (TargetCompId != null)
			{
				TargetCompIdLength = 3 + TargetCompId.Length + SohLength;
			}
			if (SenderSubId != null)
			{
				SenderSubIdLength = 3 + SenderSubId.Length + SohLength;
			}
			if (TargetSubId != null)
			{
				TargetSubIdLength = 3 + TargetSubId.Length + SohLength;
			}
			if (SenderLocationId != null)
			{
				SenderLocationIdLength = 4 + SenderLocationId.Length + SohLength;
			}
			if (TargetLocationId != null)
			{
				TargetLocationIdLength = 4 + TargetLocationId.Length + SohLength;
			}
		}

		public override void Serialize(FixMessage content, ByteBuffer buffer, SerializationContext context, FixSessionRuntimeState runtimeState)
		{

			content.MarkUnserializableTag(Tags.BeginString);
			content.MarkUnserializableTag(Tags.BodyLength);
			content.MarkUnserializableTag(Tags.MsgType);
			content.MarkUnserializableTag(Tags.MsgSeqNum);
			content.MarkUnserializableTag(Tags.SendingTime);
			content.MarkUnserializableTag(Tags.ApplVerID);
			content.MarkUnserializableTag(Tags.CstmApplVerID);
			content.MarkUnserializableTag(Tags.CheckSum);

			var length = content.RawLength;
			var msgTypeIndex = content.GetTagIndex(Tags.MsgType);
			var msgTypeValueLength = content.GetTagValueLengthAtIndex(msgTypeIndex);
			length += MsgTypeTagValue.Length + msgTypeValueLength + 1;

			var updateSender = SenderCompIdLength != 0 && !content.IsTagExists(Tags.SenderCompID);
			if (updateSender)
			{
				length -= GetTagLength(content, Tags.SenderCompID, 3);
				length += SenderCompIdLength;

				length -= GetTagLength(content, Tags.SenderSubID, 3);
				length += SenderSubIdLength;

				length -= GetTagLength(content, Tags.SenderLocationID, 4);
				length += SenderLocationIdLength;
			}

			var updateTarget = TargetCompIdLength != 0 && !content.IsTagExists(Tags.TargetCompID);
			if (updateTarget)
			{
				length -= GetTagLength(content, Tags.TargetCompID, 3);
				length += TargetCompIdLength;

				length -= GetTagLength(content, Tags.TargetSubID, 3);
				length += TargetSubIdLength;

				length -= GetTagLength(content, Tags.TargetLocationID, 4);
				length += TargetLocationIdLength;
			}

			var isLogon = RawFixUtil.IsLogon(content);
			byte[] loginHeader = null;
			if (isLogon)
			{
				loginHeader = GetLoginHeader(runtimeState);
				length += loginHeader.Length;
			}

			// out seq num
			var outgoingSequenceNumber = runtimeState.OutSeqNum;
			length += FixTypes.GetSeqNumLength(outgoingSequenceNumber, MinSeqNumLength); // out sequence
			length += SohLength + SeqNumTagValue.Length;

			long incomingSequenceNumber = 0;
			if (IncludeLastProcessed)
			{
				incomingSequenceNumber = runtimeState.InSeqNum - 1;
				length += FixTypes.GetSeqNumLength(incomingSequenceNumber, MinSeqNumLength); // in sequence
				length += SohLength + LastProcessedTagValue.Length;
			}


			length += SohLength + SendingTimeTagValue.Length + context.FormatLength; // sending time

			var msgLength = length;
			length += CheckSumFieldLength;
			length += BeginStringBodyLengthHeader.Length;
			length += FixTypes.FormatIntLength(msgLength) + SohLength; // 9 (MsgLength) value

			if (!buffer.IsAvailable(length))
			{
				buffer.IncreaseBuffer(length);
			}

			// msg length
			var offset = buffer.Offset;
			buffer.Add(BeginStringBodyLengthHeader); // msg version + part of msg length
			buffer.AddLikeString(msgLength);
			buffer.Add(Soh);

			// msg type
			buffer.Add(MsgTypeTagValue); // 35=
			content.GetTagValueAtIndex(msgTypeIndex, buffer);

			// SeqNum
			buffer.Add(Soh);
			buffer.Add(SeqNumTagValue);
			buffer.AddLikeString(outgoingSequenceNumber, MinSeqNumLength);
			buffer.Add(Soh);

			AddHeaderTag(content, buffer, Tags.SenderCompID, SenderCompId, updateSender);
			AddHeaderTag(content, buffer, Tags.TargetCompID, TargetCompId, updateTarget);
			AddHeaderTag(content, buffer, Tags.SenderSubID, SenderSubId, updateSender);
			AddHeaderTag(content, buffer, Tags.TargetSubID, TargetSubId, updateTarget);
			AddHeaderTag(content, buffer, Tags.SenderLocationID, SenderLocationId, updateSender);
			AddHeaderTag(content, buffer, Tags.TargetLocationID, TargetLocationId, updateTarget);

			// last processed SeqNum
			if (IncludeLastProcessed)
			{
				buffer.Add(LastProcessedTagValue).AddLikeString(incomingSequenceNumber, MinSeqNumLength).Add(Soh);
			}

			// sending time
			var sendingTime = context.CurrentDateValue;
			buffer.Add(SendingTimeTagValue).Add(sendingTime).Add(Soh);

			if (isLogon)
			{
				buffer.Add(loginHeader);
			}

			buffer.Offset = content.ToByteArrayAndReturnNextPosition(buffer.GetByteArray(), buffer.Offset, StandardMessageFactory.ExcludedFields);

			// checksum
			if (!buffer.IsAvailable(CheckSumFieldLength))
			{
				buffer.IncreaseBuffer(CheckSumFieldLength);
			}
			WriteChecksumField(buffer.GetByteArray(), offset, buffer.Offset - offset);
			buffer.Offset = buffer.Offset + CheckSumFieldLength;

			content.ClearUnserializableTags();
		}

		private int GetTagLength(FixMessage content, int tagId, int tagIdLength)
		{
			var tagIndex = content.GetTagIndex(tagId);
			if (tagIndex != FixMessage.NotFound)
			{
				return content.GetTagValueLengthAtIndex(tagIndex) + tagIdLength + SohLength;
			}
			return 0;
		}

		private void AddHeaderTag(FixMessage content, ByteBuffer buffer, int tagId, byte[] defaultValue, bool update)
		{
			var tagIndex = content.GetTagIndex(tagId);
			if (update && defaultValue != null)
			{
				//set new value from properties
				buffer.AddLikeString(tagId).Add(Equal).Add(defaultValue).Add(Soh);
			}
			else
			{
				//use old value from source message
				if (tagIndex != FixMessage.NotFound)
				{
					buffer.AddLikeString(tagId).Add(Equal);
					content.GetTagValueAtIndex(tagIndex, buffer);
					buffer.Add(Soh);
				}
			}

			if (tagIndex != FixMessage.NotFound)
			{
				content.MarkUnserializableTag(tagId);
			}
		}
	}
}
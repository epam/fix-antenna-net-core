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
using System.Reflection;
using System.Text;
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Session.Common;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Message.Format;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.Impl
{
	/// <summary>
	/// The standard message factory implementation.
	/// This class implements <c>AsByteArray</c> method for wrapping message.
	/// </summary>
	internal class StandardMessageFactory : AbstractFixMessageFactory
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(StandardMessageFactory));
		public static readonly int[] ExcludedFields = new int[]{ 35 };
		public static readonly int[] DeletedHeaderTrailerTags = new int[]
		{
			Tags.BeginString,
			Tags.SenderCompID,
			Tags.TargetCompID,
			Tags.SenderLocationID,
			Tags.TargetLocationID,
			Tags.SenderSubID,
			Tags.TargetSubID,
			Tags.MsgSeqNum,
			Tags.SendingTime,
			Tags.LastMsgSeqNumProcessed,
			Tags.BodyLength,
			Tags.CheckSum
		};

		private static readonly byte[] SendingTimeTagValue = "52=".AsByteArray();
		private static readonly byte[] SeqNumTagValue = "34=".AsByteArray();
		private static readonly byte[] LastProcessedTagValue = "369=".AsByteArray();
		private static readonly byte[] MsgTypeTagValue = "35=".AsByteArray();

		private const int CheckSumFieldLength = 7;
		private const int SohLength = 1;

		public static readonly byte[] ChecksumStub = "000".AsByteArray();

		private const byte SeparatorByte = 0x01;

		protected internal ISendingTime SendingTimeObj;
		private byte[] _senderTargetHeader;
		private byte[] _beginStringBodyLengthHeader;

		protected internal SessionParameters SessionParameters;
		protected internal FixSessionRuntimeState RuntimeState;
		protected internal bool IncludeLastProcessed;
		private byte[] _messageVersion;
		private byte[] _senderCompId;
		private byte[] _targetCompId;
		private byte[] _senderLocationId;
		private byte[] _targetLocationId;
		private byte[] _senderSubId;
		private byte[] _targetSubId;
		protected internal int HeartbeatInterval;

		internal LeaveIdsSerializationStrategy LeaveIdsSerializationStrategy = new LeaveIdsSerializationStrategy();
		private ILogonCustomizationStrategy _logonCustomizationStrategy;

		/// <summary>
		/// Creates the <c>StandardMessageFactory</c>.
		/// </summary>
		public StandardMessageFactory()
		{
		}

		/// <inheritdoc />
		public override void SetSessionParameters(SessionParameters sessionParameters)
		{
			SessionParameters = sessionParameters;
			if (sessionParameters.FixVersion.MessageVersion != null)
			{
				_messageVersion = sessionParameters.FixVersion.MessageVersion.AsByteArray();
			}
			if (sessionParameters.SenderCompId != null)
			{
				_senderCompId = sessionParameters.SenderCompId.AsByteArray();
			}
			if (sessionParameters.TargetCompId != null)
			{
				_targetCompId = sessionParameters.TargetCompId.AsByteArray();
			}
			_senderLocationId = sessionParameters.SenderLocationId?.AsByteArray();
			_targetLocationId = sessionParameters.TargetLocationId?.AsByteArray();
			_senderSubId = sessionParameters.SenderSubId?.AsByteArray();
			_targetSubId = sessionParameters.TargetSubId?.AsByteArray();

			_senderTargetHeader = GetSenderTargetHeader();
			_beginStringBodyLengthHeader = GetBeginStringFieldWithBodyLengthTag();
			IncludeLastProcessed = sessionParameters.IsNeedToIncludeLastProcessed();
			HeartbeatInterval = sessionParameters.HeartbeatInterval;

			LeaveIdsSerializationStrategy.Init(sessionParameters);
			var configuration = sessionParameters.Configuration;
			SendingTimeObj = GetSendingTime(configuration);

			MinSeqNumFieldsLength = configuration.GetPropertyAsInt(Config.SeqNumLength, 1, 10, true, "Wrong value in parameter SeqNumLength. The padding is disabled.");

			var logonCustomizationStrategyClass = configuration.GetProperty(Config.LogonCustomizationStrategy);
			if (!Config.LogonCustomizationStrategyUndefined.Equals(logonCustomizationStrategyClass))
			{
				try
				{
					_logonCustomizationStrategy =
						(ILogonCustomizationStrategy)Activator.CreateInstance(Type.GetType(logonCustomizationStrategyClass));
					_logonCustomizationStrategy.SetSessionParameters(sessionParameters);
				}
				catch (Exception e) when (e is NotSupportedException || e is TargetInvocationException || e is MissingMethodException || e is MethodAccessException || e is TypeLoadException)
				{
					Log.Error("Can not load logon customization strategy class '" + logonCustomizationStrategyClass + "'. Cause: " + e.Message + ".", e);
				}
				catch (Exception e)
				{
					Log.Error("Error '" + e.Message + "' has occurred while initializing logon customization strategy.", e);
				}
			}
		}

		public virtual ISendingTime GetSendingTime(Config config)
		{
			ISendingTime st;
			var precision = (new ConfigurationAdapter(config)).TimestampsPrecisionInTags;
			switch (precision)
			{
				case TimestampPrecision.Milli:
					st = new SendingTimeMilli();
					break;
				case TimestampPrecision.Micro:
					st = new SendingTimeMicro();
					break;
				case TimestampPrecision.Nano:
					st = new SendingTimeNano();
					break;
				case TimestampPrecision.Second:
				default:
					st = new SendingTimeSecond();
					break;
			}
			return st;
		}

		public override ISendingTime SendingTime
		{
			get { return SendingTimeObj; }
		}

		public override void SetRuntimeState(FixSessionRuntimeState runtimeState)
		{
			RuntimeState = runtimeState;
		}

		/// <summary>
		/// Wraps the sending message.
		/// </summary>
		/// <param name="msgType">   the message type, if is null - the message send as is,
		///                  and if is '' - method updates body length, sequence number, sending time and checksum fields,
		///                  otherwise method wraps the <c>fixFields</c>.
		///                  '' is also used for sending logon message. </param>
		/// <param name="fixFields"> the list of fix fields </param>
		/// <returns> bytes of sending message </returns>
		public override void Serialize(MsgBuf buf, string msgType, FixMessage fixFields, ByteBuffer buffer, SerializationContext context)
		{
			if (msgType == null)
			{
				// send message as is
				var length = fixFields.RawLength;
				if (!buffer.IsAvailable(length))
				{
					buffer.IncreaseBuffer(length);
				}
				buffer.Offset = fixFields.ToByteArrayAndReturnNextPosition(buffer.GetByteArray(), buffer.Offset);
			}
			else if (fixFields != null && fixFields.IsPreparedMessage)
			{
				SerializePreparedMessage(buf, fixFields, buffer, context);
			}
			else if (msgType.Length == 0)
			{
				if (!fixFields.HasTagValue(Tags.MsgType))
				{
					throw new ArgumentException("Message type(35 tag) cannot be null");
				}
				// update body length, sequence number, sending time, checksum
				SerializeWithUpdatedHeaderAndTrailer(buf, fixFields, buffer, context);
			}
			else
			{
				// regular flow, wrap user content
				SerializeWithAddedHeaderAndTrailer(msgType, fixFields, buffer, context);
			}
		}

		/// <summary>
		/// Wraps the sending message.
		/// </summary>
		/// <param name="message"> preparedMessage </param>
		/// <param name="buffer">  ByteBuffer for write </param>
		/// <param name="context"> Serialization context</param>
		private void SerializePreparedMessage(MsgBuf buf, FixMessage message, ByteBuffer buffer, SerializationContext context)
		{
			// send message and insert seqNum, time, checksum, bodyLength
			//SEQ_NUM (proper value of SeqNum loaded in PreparedMessageUtil)
			message.UpdateValue(Tags.MsgSeqNum, RuntimeState.OutSeqNum, IndexedStorage.MissingTagHandling.DontAddIfNotExists);

			if (IncludeLastProcessed)
			{ // add 369 tag if needed
				//FIXME_NOW: add method for this value
				var lastSeq = RuntimeState.InSeqNum - 1;
				message.SetPaddedLongTag(Tags.LastMsgSeqNumProcessed, lastSeq, MinSeqNumFieldsLength, IndexedStorage.MissingTagHandling.AddIfNotExists);
			}

			//TIME
			message.UpdateValue(Tags.SendingTime, context.CurrentDateValue, IndexedStorage.MissingTagHandling.DontAddIfNotExists);

			if (buf != null && message.IsMessageBufferContinuous)
			{
				//the length should be already defined for this case
				//TODO: check msg length for this case
				message.GetMessageBuffer(buf);
				UpdateCheckSum(buf.Buffer, buf.Offset, buf.Length);
				return;
			}

			//LENGTH
			message.UpdateValue(Tags.BodyLength, message.CalculateBodyLength(), IndexedStorage.MissingTagHandling.DontAddIfNotExists);

			var length = message.RawLength;
			if (!buffer.IsAvailable(length))
			{
				buffer.IncreaseBuffer(length);
			}
			var offset = buffer.Offset;
			buffer.Offset = message.ToByteArrayAndReturnNextPosition(buffer.GetByteArray(), buffer.Offset);

			//CHECKSUM
			UpdateCheckSum(buffer.GetByteArray(), offset, buffer.Offset - offset);
		}

		public virtual void Serialize(string msgType, FixMessage fixFields, ByteBuffer buffer, SerializationContext context)
		{
			Serialize(null, msgType, fixFields, buffer, context);
		}


		/// <summary>
		/// Wraps the sending message.
		/// </summary>
		/// <param name="content">     the list of fix fields </param>
		/// <param name="changesType"> the changesType </param>
		/// <returns> bytes of sending message </returns>
		public override void Serialize(FixMessage content, ChangesType? changesType, ByteBuffer buffer, SerializationContext context)
		{
			if (changesType == null)
			{
				// send message as is
				var length = content.RawLength;
				if (length > (buffer.Length - buffer.Offset))
				{
					buffer.IncreaseBuffer(length);
				}
				buffer.Offset = content.ToByteArrayAndReturnNextPosition(buffer.GetByteArray(), buffer.Offset);
				return;
			}
			if (!content.HasTagValue(Tags.MsgType))
			{
				throw new ArgumentException("Message type(35 tag) cannot be null");
			}
			switch (changesType)
			{
				case ChangesType.DeleteAndAddSmhAndSmt:
					DeleteHeaderAndTrailer(content);
					goto case ChangesType.AddSmhAndSmt;
				case ChangesType.AddSmhAndSmt:
					SerializeWithAddedHeaderAndTrailerWithoutMessageTypeField(content, buffer, context);
					break;
				case ChangesType.UpdateSmhAndSmtDonotUpdateSndr:
					SerializeWithUpdatedHeaderAndTrailerWithoutSenderIfExist(content, buffer, context);
					break;
				case ChangesType.UpdateSmhAndSmtExceptCompids:
					LeaveIdsSerializationStrategy.Serialize(content, buffer, context, RuntimeState);
					break;
				case ChangesType.UpdateSmhAndSmt:
				default:
					SerializeWithUpdatedHeaderAndTrailer(content, buffer, context);
					break;
			}

			content.ClearUnserializableTags();
		}

		private void DeleteHeaderAndTrailer(FixMessage content)
		{
			foreach (var tag in DeletedHeaderTrailerTags)
			{
				content.MarkUnserializableTag(tag);
			}
		}

		private void SerializeWithAddedHeaderAndTrailerWithoutMessageTypeField(FixMessage fixMessage, ByteBuffer buffer, SerializationContext context)
		{
			var length = _senderTargetHeader.Length;
			byte[] loginHeader = null;
			var msgType = fixMessage.MsgType;
			var isLogon = RawFixUtil.IsLogon(msgType);
			if (isLogon)
			{
				if (_logonCustomizationStrategy != null)
				{
					Log.Warn("logonCustomizationStrategy has been set but changesType provided does not support it (only update is supported).");
				}
				loginHeader = GetLoginHeader();
				length += loginHeader.Length;
			}
			length += fixMessage.RawLength;
			var outSeqNum = RuntimeState.OutSeqNum;
			var outSeqNumLen = FixTypes.GetSeqNumLength(outSeqNum, MinSeqNumFieldsLength);
			length += SeqNumTagValue.Length + SohLength + outSeqNumLen; // out sequence

			var inSeqNum = RuntimeState.InSeqNum - 1;
			if (IncludeLastProcessed)
			{
				//FIXME_NOW: add method for this value
				var inSeqNumLen = FixTypes.GetSeqNumLength(inSeqNum, MinSeqNumFieldsLength);
				length += LastProcessedTagValue.Length + SohLength + inSeqNumLen; // in sequence
			}

			length += SohLength + SendingTimeTagValue.Length + context.FormatLength; // sending time
			var msgLength = length;
			length += CheckSumFieldLength + FixTypes.FormatIntLength(msgLength) + SeparatorLength + _beginStringBodyLengthHeader.Length;

			if (!buffer.IsAvailable(length))
			{
				buffer.IncreaseBuffer(length);
			}
			var offset = buffer.Offset;
			buffer.Add(_beginStringBodyLengthHeader); // msg version + part of msg length
			buffer.AddLikeString((long) msgLength);
			buffer.Add(SeparatorByte);
			buffer.Add(MsgTypeTagValue); // 35=
			buffer.Add(msgType); // msg type
			buffer.Add(SeparatorByte);
			buffer.Add(SeqNumTagValue);
			buffer.AddLikeString(outSeqNum, MinSeqNumFieldsLength);
			buffer.Add(SeparatorByte);
			buffer.Add(_senderTargetHeader);
			if (IncludeLastProcessed)
			{
				buffer.Add(LastProcessedTagValue);
				buffer.AddLikeString(inSeqNum, MinSeqNumFieldsLength);
				buffer.Add(SeparatorByte);
			}
			var sendingTime = context.CurrentDateValue;
			buffer.Add(SendingTimeTagValue);
			buffer.Add(sendingTime);
			buffer.Add(SeparatorByte);

			if (isLogon)
			{
				buffer.Add(loginHeader);
			}

			buffer.Offset = fixMessage.ToByteArrayAndReturnNextPosition(buffer.GetByteArray(), buffer.Offset, ExcludedFields);
			if (!buffer.IsAvailable(CheckSumFieldLength))
			{
				buffer.IncreaseBuffer(CheckSumFieldLength);
			}
			WriteChecksumField(buffer.GetByteArray(), offset, buffer.Offset - offset);
			buffer.Offset = buffer.Offset + CheckSumFieldLength;
		}

		private void SerializeWithAddedHeaderAndTrailer(string type, FixMessage fixMessage, ByteBuffer buffer, SerializationContext context)
		{
			var isLogon = RawFixUtil.IsLogon(type);
			if (isLogon)
			{
				throw new ArgumentException("This method cannot be used for sending logon. " + "Call serialize method with message msgType = '' for proper adding logon tags.");
			}

			var length = _senderTargetHeader.Length;
			length += 4 + type.Length; // msg length
			if (fixMessage != null)
			{
				length += fixMessage.RawLength;
			}

			var outSeqNum = RuntimeState.OutSeqNum;
			var outSeqNumLen = FixTypes.GetSeqNumLength(outSeqNum, MinSeqNumFieldsLength);
			length += SeqNumTagValue.Length + SohLength + outSeqNumLen; // out sequence
			var inSeqNum = RuntimeState.InSeqNum - 1;
			if (IncludeLastProcessed)
			{
				var inSeqNumLen = FixTypes.GetSeqNumLength(inSeqNum, MinSeqNumFieldsLength);
				length += LastProcessedTagValue.Length + SohLength + inSeqNumLen; // in sequence
			}

			length += 1 + SendingTimeTagValue.Length + context.FormatLength; // sending time
			var msgLength = length;
			length += CheckSumFieldLength + FixTypes.FormatIntLength(msgLength) + SeparatorLength + _beginStringBodyLengthHeader.Length;

			var offset = buffer.Offset;
			buffer.Add(_beginStringBodyLengthHeader); // msg version + part of msg length
			buffer.AddLikeString((long) msgLength);
			buffer.Add(SeparatorByte);
			buffer.Add(MsgTypeTagValue); // 35=
			buffer.Add(type); // msg type
			buffer.Add(SeparatorByte);
			buffer.Add(SeqNumTagValue);
			buffer.AddLikeString(outSeqNum, MinSeqNumFieldsLength);
			buffer.Add(SeparatorByte);
			buffer.Add(_senderTargetHeader);
			if (IncludeLastProcessed)
			{
				buffer.Add(LastProcessedTagValue);
				buffer.AddLikeString(inSeqNum, MinSeqNumFieldsLength);
				buffer.Add(SeparatorByte);
			}

			var sendingTime = context.CurrentDateValue;
			buffer.Add(SendingTimeTagValue);
			buffer.Add(sendingTime);
			buffer.Add(SeparatorByte);

			if (fixMessage != null)
			{
				if (!buffer.IsAvailable(length))
				{
					buffer.IncreaseBuffer(length);
				}
				buffer.Offset = fixMessage.ToByteArrayAndReturnNextPosition(buffer.GetByteArray(), buffer.Offset);
			}

			// checksum
			if (!buffer.IsAvailable(CheckSumFieldLength))
			{
				buffer.IncreaseBuffer(CheckSumFieldLength);
			}
			WriteChecksumField(buffer.GetByteArray(), offset, buffer.Offset - offset);
			buffer.Offset = buffer.Offset + CheckSumFieldLength;
		}

		private void WriteChecksumField(byte[] rawMessage, int offset, int length)
		{
			var checksum = RawFixUtil.GetChecksum(rawMessage, offset, length);
			var position = offset + length;
			rawMessage[position++] = (byte)'1';
			rawMessage[position++] = (byte)'0';
			rawMessage[position++] = (byte)'=';

			rawMessage[position++] = (byte)(checksum / 100 + (byte) '0');
			rawMessage[position++] = (byte)(((checksum / 10) % 10) + (byte) '0');
			rawMessage[position++] = (byte)((checksum % 10) + (byte) '0');
			rawMessage[position] = SeparatorByte;
		}

		private void SerializeWithUpdatedHeaderAndTrailer(FixMessage fixMessage, ByteBuffer buffer, SerializationContext context)
		{
			SerializeWithUpdatedHeaderAndTrailer(null, fixMessage, buffer, context);
		}

		private void SerializeWithUpdatedHeaderAndTrailer(MsgBuf buf, FixMessage fixMessage, ByteBuffer buffer, SerializationContext context)
		{
			// update header fields
			if (!fixMessage.IsPreparedMessage)
			{
				SafeSetValue(fixMessage, Tags.BeginString, _messageVersion);
				SafeSetValue(fixMessage, Tags.SenderCompID, _senderCompId);
				SafeSetValue(fixMessage, Tags.TargetCompID, _targetCompId);
				SafeSetValue(fixMessage, Tags.SenderLocationID, _senderLocationId);
				SafeSetValue(fixMessage, Tags.TargetLocationID, _targetLocationId);
				SafeSetValue(fixMessage, Tags.SenderSubID, _senderSubId);
				SafeSetValue(fixMessage, Tags.TargetSubID, _targetSubId);
			}

			// update critical fields
			fixMessage.SetPaddedLongTag(Tags.MsgSeqNum, RuntimeState.OutSeqNum, MinSeqNumFieldsLength,
				IndexedStorage.MissingTagHandling.DontAddIfNotExists);

			// fixed bug 14837 Tag 369 should be include in message automatically if includeLastProcessed=true
			if (IncludeLastProcessed)
			{ // add 369 tag if needed
				//FIXME_NOW
				var lastSeq = RuntimeState.InSeqNum - 1;
				SafeSetPaddedValueAfter(fixMessage, Tags.MsgSeqNum, Tags.LastMsgSeqNumProcessed, lastSeq, MinSeqNumFieldsLength);
			}

			// update critical fields
			SafeSetValue(fixMessage, Tags.SendingTime, context.CurrentDateValue);

			if (RawFixUtil.IsLogon(fixMessage))
			{
				CompleteLogin(fixMessage);
			}

			if (buf != null && fixMessage.IsMessageBufferContinuous)
			{
				//TODO: check msg length for this case
				fixMessage.GetMessageBuffer(buf);
				UpdateCheckSum(buf.Buffer, buf.Offset, buf.Length);
				return;
			}

			if (fixMessage.GetTagNumAtIndex(fixMessage.Length - 1) == Tags.CheckSum)
			{
				SafeSetValue(fixMessage, Tags.BodyLength, fixMessage.CalculateBodyLength());
				if (fixMessage.GetTagLength(Tags.CheckSum) != 3)
				{
					//Fix checksum value
					SafeSetValue(fixMessage, Tags.CheckSum, ChecksumStub);
				}
				var length = fixMessage.RawLength;
				if (!buffer.IsAvailable(length))
				{
					buffer.IncreaseBuffer(length);
				}
				var offset = buffer.Offset;
				buffer.Offset = fixMessage.ToByteArrayAndReturnNextPosition(buffer.GetByteArray(), buffer.Offset);
				UpdateCheckSum(buffer.GetByteArray(), offset, buffer.Offset - offset);
			}
			else
			{
				SafeSetValue(fixMessage, Tags.BodyLength, fixMessage.CalculateBodyLength());
				var length = fixMessage.RawLength;
				if (!buffer.IsAvailable(length))
				{
					buffer.IncreaseBuffer(length);
				}
				buffer.Offset = fixMessage.ToByteArrayAndReturnNextPosition(buffer.GetByteArray(), buffer.Offset);
			}
		}

		private void SerializeWithUpdatedHeaderAndTrailerWithoutSenderIfExist(FixMessage fixMessage, ByteBuffer buffer, SerializationContext context)
		{
			// update header fields
			SafeSetValue(fixMessage, Tags.BeginString, _messageVersion);
			SafeAddOnlyIfDoesNotExistValue(fixMessage, Tags.SenderCompID, _senderCompId);
			SafeSetValue(fixMessage, Tags.TargetCompID, _targetCompId);
			SafeSetValue(fixMessage, Tags.SenderLocationID, _senderLocationId);
			SafeSetValue(fixMessage, Tags.TargetLocationID, _targetLocationId);
			SafeSetValue(fixMessage, Tags.SenderSubID, _senderSubId);
			SafeSetValue(fixMessage, Tags.TargetSubID, _targetSubId);

			// update critical fields
			SafeSetValue(fixMessage, Tags.MsgSeqNum, RuntimeState.OutSeqNum);

			// fixed bug 14837 Tag 369 should be include in message automatically if includeLastProcessed=true
			if (IncludeLastProcessed)
			{ // add 369 tag if needed
				//FIXME_NOW
				var lastSeq = RuntimeState.InSeqNum - 1;
				SafeSetPaddedValueAfter(fixMessage, Tags.MsgSeqNum, Tags.LastMsgSeqNumProcessed, lastSeq, MinSeqNumFieldsLength);
			}

			// update critical fields
			SafeSetValue(fixMessage, Tags.SendingTime, context.CurrentDateValue);

			var field = fixMessage[fixMessage.Length - 1];
			if (field.TagId == Tags.CheckSum)
			{
				if (field.Length != 3)
				{
					field.Value = ChecksumStub;
				}
				SafeSetValue(fixMessage, Tags.BodyLength, fixMessage.CalculateBodyLength());
				var length = fixMessage.RawLength;
				if (!buffer.IsAvailable(length))
				{
					buffer.IncreaseBuffer(length);
				}
				var offset = buffer.Offset;
				buffer.Offset = fixMessage.ToByteArrayAndReturnNextPosition(buffer.GetByteArray(), buffer.Offset);
				UpdateCheckSum(buffer.GetByteArray(), offset, buffer.Offset - offset);
			}
			else
			{
				SafeSetValue(fixMessage, Tags.BodyLength, fixMessage.CalculateBodyLength());
				var length = fixMessage.RawLength;
				if (!buffer.IsAvailable(length))
				{
					buffer.IncreaseBuffer(length);
				}
				buffer.Offset = fixMessage.ToByteArrayAndReturnNextPosition(buffer.GetByteArray(), buffer.Offset);
			}
		}

		private void UpdateCheckSum(byte[] byteArray, int startOffset, int length)
		{
			var checksum = RawFixUtil.GetChecksum(byteArray, startOffset, length - CheckSumFieldLength);
			var position = startOffset + length - 4;
			byteArray[position++] = (byte)(checksum / 100 + (byte) '0');
			byteArray[position++] = (byte)(((checksum / 10) % 10) + (byte) '0');
			byteArray[position] = (byte)((checksum % 10) + (byte) '0');
		}

		internal static void SafeSetValue(FixMessage fixMessage, int tag, string value)
		{
			SafeSetValue(fixMessage, tag, value.AsByteArray());
		}

		internal static void SafeSetValue(FixMessage fixMessage, int tag, byte[] value)
		{
			if (value != null)
			{
				fixMessage.UpdateValue(tag, value, IndexedStorage.MissingTagHandling.DontAddIfNotExists);
			}
		}

		internal static void SafeSetValue(FixMessage fixMessage, int tag, long value)
		{
			fixMessage.UpdateValue(tag, value, IndexedStorage.MissingTagHandling.DontAddIfNotExists);
		}

		internal static void SafeAddOnlyIfDoesNotExistValue(FixMessage fixMessage, int tag, byte[] value)
		{
			if (value != null)
			{
				var field = fixMessage.GetTag(tag);
				if (field == null)
				{
					SafeSetValueAfter(fixMessage, 9, Tags.SenderCompID, value);
				}
				else
				{
					// skip value
				}
			}
		}

		/// <summary>
		/// If tag exist update fix field, otherwise added fix field after afterTag,
		/// if afterTag field exists.
		/// </summary>
		/// <param name="fixMessagest"> the fix message </param>
		/// <param name="afterTag">     the tag, if afterTag exists, the <c>tag</c> insert after it </param>
		/// <param name="tag">          the to insert tag </param>
		/// <param name="value">        the value if insert tag </param>
		internal static void SafeSetValueAfter(FixMessage fixMessage, int afterTag, int tag, byte[] value)
		{
			if (fixMessage.UpdateValue(tag, value, IndexedStorage.MissingTagHandling.DontAddIfNotExists) == IndexedStorage.NotFound)
			{
				var after = fixMessage.GetTagIndex(afterTag);
				if (after != -1)
				{
					fixMessage.AddTagAtIndex(after + 1, tag, value);
				}
			}
		}

		private static void SafeSetPaddedValueAfter(FixMessage fixMessage, int afterTag, int tag, long value, int padding)
		{
			if (fixMessage.SetPaddedLongTag(tag, value, padding, IndexedStorage.MissingTagHandling.DontAddIfNotExists) !=
					IndexedStorage.NotFound)
				return;

			var after = fixMessage.GetTagIndex(afterTag);
			if (after == -1)
				return;

			fixMessage.ReserveTagAtIndex(after + 1, tag, true);
			fixMessage.UpdateValueAtIndex(after + 1, value);
		}

		public virtual byte[] GetLoginHeader()
		{
			var sb = new StringBuilder();
			sb.Append(Tags.EncryptMethod).Append(Equal).Append(0).Append(Separator); // NONE encryption
			sb.Append(Tags.HeartBtInt).Append(Equal).Append(HeartbeatInterval).Append(Separator);
			sb.Append(GetOutLogonFields().ToString());
			return sb.ToString().AsByteArray();
		}

		public virtual FixMessage GetOutLogonFields()
		{
			return RuntimeState.OutgoingLogon;
		}

		public virtual void CompleteLogin(FixMessage content)
		{
			content.AddTag(Tags.EncryptMethod, (long)0);
			content.AddTag(Tags.HeartBtInt, HeartbeatInterval);
			content.AddAll(GetOutLogonFields());
			if (_logonCustomizationStrategy != null)
			{
				_logonCustomizationStrategy.CompleteLogon(content);
			}
			//in case if CheckSum tag was added earlier
			if (content.IsTagExists(Tags.CheckSum))
			{
				content.RemoveTag(Tags.CheckSum);
			}
			//CheckSum tag must be at the end of the message
			content.AddTag(Tags.CheckSum, ChecksumStub);
		}

		/// <summary>
		/// Gets header with sender and target fields.
		/// </summary>
		private byte[] GetSenderTargetHeader()
		{
			// no need to optimize this, because header is only constructed once per factory create
			var sb = new StringBuilder();
			sb.Append(Tags.SenderCompID).Append(Equal).Append(SessionParameters.SenderCompId).Append(Separator);
			sb.Append(Tags.TargetCompID).Append(Equal).Append(SessionParameters.TargetCompId).Append(Separator);
			SafeAdd(sb, Tags.SenderSubID, SessionParameters.SenderSubId);
			SafeAdd(sb, Tags.TargetSubID, SessionParameters.TargetSubId);
			SafeAdd(sb, Tags.SenderLocationID, SessionParameters.SenderLocationId);
			SafeAdd(sb, Tags.TargetLocationID, SessionParameters.TargetLocationId);
			sb.Append(SessionParameters.UserDefinedFields.ToUnmaskedString());
			return sb.ToString().AsByteArray();
		}

		/// <summary>
		/// Gets header
		/// </summary>
		private byte[] GetBeginStringFieldWithBodyLengthTag()
		{
			var sb = new StringBuilder();
			sb.Append("8=").Append(SessionParameters.FixVersion.MessageVersion).Append(Separator);
			sb.Append("9=");
			return sb.ToString().AsByteArray();
		}

		/// <summary>
		/// Completes the message <c>content</c> with necessarily fields.
		/// </summary>
		/// <param name="msgType"> the message type field </param>
		/// <param name="content"> the content of message </param>
		/// <returns> the completed message </returns>
		public override FixMessage CompleteMessage(string msgType, FixMessage content)
		{
			var message = new FixMessage();
			message.AddTag(Tags.BeginString, _messageVersion);
			message.AddTag(Tags.BodyLength, (long)0);
			message.AddTag(Tags.MsgType, msgType);
			message.SetPaddedLongTag(Tags.MsgSeqNum, 1L, MinSeqNumFieldsLength);
			message.AddTag(Tags.SenderCompID, _senderCompId);
			message.AddTag(Tags.TargetCompID, _targetCompId);
			SafeAdd(message, Tags.SenderSubID, SessionParameters.SenderSubId);
			SafeAdd(message, Tags.TargetSubID, SessionParameters.TargetSubId);
			SafeAdd(message, Tags.SenderLocationID, SessionParameters.SenderLocationId);
			SafeAdd(message, Tags.TargetLocationID, SessionParameters.TargetLocationId);
			message.AddAll(SessionParameters.UserDefinedFields);
			if (IncludeLastProcessed)
			{ // add 369 tag if needed
				var lastPrcSeqNum = RuntimeState.InSeqNum - 1;
				message.SetPaddedLongTag(Tags.LastMsgSeqNumProcessed, lastPrcSeqNum, MinSeqNumFieldsLength);
			}
			message.AddTag(Tags.SendingTime, GetCurrentSendingTime());
			message.AddAll(content);
			message.AddTag(Tags.CheckSum, (long)1); // 15470 "Resend Request -- Sequence reset" mechanism is broken
			return message;
		}

		/// <inheritdoc />
		public override byte[] GetCurrentSendingTime()
		{
			return SendingTimeObj.CurrentDateValue;
		}
	}
}

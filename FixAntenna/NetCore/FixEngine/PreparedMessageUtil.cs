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
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Message.Format;

namespace Epam.FixAntenna.NetCore.FixEngine
{
	public class PreparedMessageUtil
	{
		public const int BodylengthFieldLength = 3;
		public const int BodylengthFieldDefaultMax = 999;
		public readonly int SeqnumFieldLength = 5;
		private const int CheckSumFieldLength = 3;
		public static readonly byte[] EmptyBytes = new byte[0];
		protected internal static readonly ILog Log = LogFactory.GetLog(typeof(PreparedMessageUtil));

		private static int _timeStampLength;
		private readonly bool _includeLastProcessed;

		private readonly SessionParameters _sessionParameters;

		/// <summary>
		/// Default constructor with <see cref="SessionParameters"/> object. This parameters will be used for building new PreparedMessage
		/// objects.
		/// </summary>
		/// <param name="sessionParameters"> SessionParameters object </param>
		public PreparedMessageUtil(SessionParameters sessionParameters)
		{
			_sessionParameters = sessionParameters;
			_includeLastProcessed = sessionParameters.IsNeedToIncludeLastProcessed();
			var sendingTimeFormatter =
				FixDateFormatterFactory.GetSendingTimeFormatter(sessionParameters.FixVersion);
			_timeStampLength = sendingTimeFormatter.GetFormattedStringLength(DateTimeOffset.Now);
			var seqNumLen = _sessionParameters.Configuration.GetPropertyAsInt(Config.SeqNumLength, 1, 10, true, "Wrong value in parameter SeqNumLength. The padding is disabled.");

			// don't know why by it was 5 as default
			if (seqNumLen > 5) SeqnumFieldLength = seqNumLen;
		}

		/// <summary>
		/// Builds FixMessage object from exist template. Instance is received from pool.
		/// </summary>
		/// <param name="message"> </param>
		/// <param name="userStructure">
		/// @return </param>
		/// <exception cref="PreparedMessageException">
		///  </exception>
		public virtual FixMessage PrepareMessage(FixMessage message, MessageStructure userStructure)
		{
			return PrepareMessage(message, userStructure, true);
		}

		/// <summary>
		/// Builds FixMessage object from exist template.
		/// </summary>
		/// <param name="message"> </param>
		/// <param name="userStructure">
		/// @return </param>
		/// <exception cref="PreparedMessageException">
		///  </exception>
		public virtual FixMessage PrepareMessage(FixMessage message, MessageStructure userStructure, bool fromPool)
		{
			var msgType = new TagValue();
			message.LoadTagValue(Tags.MsgType, msgType);
			var res = new byte[msgType.Length];
			Array.Copy(msgType.Buffer, msgType.Offset, res, 0, msgType.Length);
			return PrepareMessage(message, res, userStructure);
		}

		/// <summary>
		/// Builds FixMessage object with specified type, message structure and prefilled header information
		/// </summary>
		/// <param name="msgTypeStr">    message type </param>
		/// <param name="userStructure"> message structure </param>
		public virtual FixMessage PrepareMessage(string msgTypeStr, MessageStructure userStructure)
		{
			return PrepareMessage(msgTypeStr, userStructure, true);
		}

		/// <summary>
		/// Builds FixMessage object with specified type, message structure and prefilled header information
		/// </summary>
		/// <param name="msgTypeStr">    message type </param>
		/// <param name="userStructure"> message structure </param>
		public virtual FixMessage PrepareMessage(string msgTypeStr, MessageStructure userStructure, bool fromPool)
		{
			var msgType = msgTypeStr.AsByteArray();
			return PrepareMessage(msgType, userStructure, fromPool);
		}

		/// <summary>
		/// Builds FixMessage object with specified type, message structure and prefilled header information
		/// </summary>
		/// <param name="msgType">       message type </param>
		/// <param name="userStructure"> message structure </param>
		public virtual FixMessage PrepareMessage(byte[] msgType, MessageStructure userStructure)
		{
			return PrepareMessage(msgType, userStructure, true);
		}

		/// <summary>
		/// Builds FixMessage object with specified type, message structure and prefilled header information
		/// </summary>
		/// <param name="msgType">       message type </param>
		/// <param name="userStructure"> message structure </param>
		public virtual FixMessage PrepareMessage(byte[] msgType, MessageStructure userStructure, bool fromPool)
		{
			//Create complete structure
			var ms = PrepareFullMessageStructure(null, msgType, userStructure);

			var length = CalculateLength(ms);
			var list = FixMessageFactory.NewInstance(fromPool, true);
			list.IsPreparedMessage = true;

			var msgBuffer = new byte[length];
			msgBuffer.Fill((byte)' ');
			list.SetBuffer(msgBuffer, 0, length);

			FillMessage(list, msgBuffer, ms);

			FillHeaderAndTrailer(msgType, list, ms);

			return list;
		}

		/// <summary>
		/// Builds <see cref="FixMessage"/> object from exist template with specified message structure and prefilled
		/// header and trailer.
		/// </summary>
		/// <param name="template"> </param>
		/// <param name="msgType"> </param>
		/// <param name="userStructure"> </param>
		/// <exception cref="PreparedMessageException"> </exception>
		public virtual FixMessage PrepareMessage(FixMessage template, string msgType,
			MessageStructure userStructure)
		{
			return PrepareMessage(template, msgType, userStructure, true);
		}

		/// <summary>
		/// Builds <see cref="FixMessage"/> object from exist template with specified message structure and prefilled
		/// header and trailer.
		/// </summary>
		/// <param name="template"> </param>
		/// <param name="msgType"> </param>
		/// <param name="userStructure"> </param>
		/// <exception cref="PreparedMessageException"> </exception>
		public virtual FixMessage PrepareMessage(FixMessage template, string msgType,
			MessageStructure userStructure, bool fromPool)
		{
			return PrepareMessage(template, msgType.AsByteArray(), userStructure, fromPool);
		}

		public virtual FixMessage PrepareMessage(FixMessage template, byte[] msgType,
			MessageStructure userStructure)
		{
			return PrepareMessage(template, msgType, userStructure, true);
		}

		/// <summary>
		/// Builds <see cref="FixMessage"/> object from exist template with specified message type, message structure
		/// and prefilled header and trailer.
		/// </summary>
		/// <param name="template">      FixMessage object </param>
		/// <param name="userStructure"> template structure </param>
		/// <param name="msgType">       type of the template </param>
		/// <exception cref="PreparedMessageException"> exception </exception>
		public virtual FixMessage PrepareMessage(FixMessage template, byte[] msgType,
			MessageStructure userStructure, bool fromPool)
		{
			var ms = PrepareFullMessageStructure(template, msgType, userStructure);

			foreach (var field in template)
			{
				if (!ms.ContainsTagId(field.TagId))
				{
					throw new PreparedMessageException("MessageStructure doesn't contain all necessary fields " +
														"uniquetempvar.");
				}
			}

			var headerTagSet = PrepareHeaderTagSet(msgType);

			var length = CalculateLength(ms);
			var list = FixMessageFactory.NewInstance(fromPool, true);
			list.IsPreparedMessage = true;
			var msgBuffer = new byte[length];
			msgBuffer.Fill((byte)' ');
			list.SetBuffer(msgBuffer, 0, length);

			var offset = 0;
			var size = ms.Size;
			var templateValue = new TagValue();
			var templateCopy = template.DeepClone(true, false);
			for (var i = 0; i < size; i++)
			{
				var tag = ms.GetTagId(i);
				var tagLength = ms.GetLength(i);
				var type = ms.GetType(i);

				var templateValuePresent = false;
				if (templateCopy.IsTagExists(tag))
				{
					templateValue = templateCopy.GetTag(tag);
					templateCopy.RemoveTag(tag);
					templateValuePresent = true;
				}

				if (MessageStructure.VariableLength != tagLength)
				{
					offset += FixTypes.FormatInt(tag, msgBuffer, offset);
					msgBuffer[offset++] = (byte)'=';
					list.AddPrepared(tag, offset, tagLength);
					offset += tagLength;
					msgBuffer[offset++] = (byte)'\u0001';
					if (!headerTagSet.Contains(tag) && templateValuePresent)
					{
						if (type == ValueType.Double)
						{
							try
							{
								var v = FixTypes.ParseFloat(templateValue.Buffer, templateValue.Offset,
									templateValue.Length);
								//TODO: fix precision
								list.SetAtIndex(i, v, tagLength);
							}
							catch (Exception)
							{
								Log.Warn("Can't init tag " + tag + " for prepared message. Template value '" +
										StringHelper.NewString(templateValue.Buffer, templateValue.Offset,
											templateValue.Length) + "' isn't a double");
							}
						}
						else if (type == ValueType.Long)
						{
							try
							{
								var v = FixTypes.ParseInt(templateValue.Buffer, templateValue.Offset,
									templateValue.Length);
								list.SetAtIndex(i, v);
							}
							catch (Exception)
							{
								Log.Warn("Can't init tag " + tag + " for prepared message. Template value '" +
										StringHelper.NewString(templateValue.Buffer, templateValue.Offset,
											templateValue.Length) + "' isn't a number");
							}
						}
						else
						{
							list.SetAtIndex(i, templateValue);
						}
					}
				}
				else
				{
					if (templateValuePresent)
					{
						list.AddTag(tag, templateValue.Buffer, templateValue.Offset, templateValue.Length);
					}
					else
					{
						list.AddTag(tag, EmptyBytes);
					}
				}
			}

			FillHeaderAndTrailer(msgType, list, ms);
			return list;
		}

		public virtual MessageStructure PrepareFullMessageStructure(FixMessage template, byte[] msgType,
			MessageStructure userStructure)
		{
			var ms = new MessageStructure();
			AddHeaderStructure(msgType, ms);

			if (template != null)
			{
				BuildStructureFromFixMessage(ms, template, userStructure);
				UpdateBodyLengthStructure(ms, template);
			}
			else
			{
				ms.Merge(userStructure);
			}

			AddTrailerStructure(ms);
			return ms;
		}

		/// <summary>
		/// Builds <see cref="FixMessage"/> object from String object.
		/// </summary>
		/// <param name="message"> </param>
		/// <param name="structure"> </param>
		/// <exception cref="PreparedMessageException"> </exception>
		public virtual FixMessage PrepareMessageFromString(byte[] message, MessageStructure structure)
		{
			return PrepareMessageFromString(message, structure, true);
		}

		/// <summary>
		/// Builds <see cref="FixMessage"/> object from String object.
		/// </summary>
		/// <param name="message"> </param>
		/// <param name="structure"> </param>
		/// <exception cref="PreparedMessageException"> </exception>
		public virtual FixMessage PrepareMessageFromString(byte[] message, MessageStructure structure, bool fromPool)
		{
			return PrepareMessage(RawFixUtil.GetFixMessage(message), structure, fromPool);
		}

		/// <summary>
		/// Builds <see cref="FixMessage"/> object from String object.
		/// </summary>
		/// <param name="message">   message string </param>
		/// <param name="structure"> message structure object </param>
		/// <param name="type">      message type </param>
		/// <exception cref="PreparedMessageException"> exception </exception>
		public virtual FixMessage PrepareMessageFromString(byte[] message, string type, MessageStructure structure)
		{
			return PrepareMessageFromString(message, type, structure, true);
		}

		/// <summary>
		/// Builds <see cref="FixMessage"/> object from String object.
		/// </summary>
		/// <param name="message">   message string </param>
		/// <param name="structure"> message structure object </param>
		/// <param name="type">      message type </param>
		/// <exception cref="PreparedMessageException"> exception </exception>
		public virtual FixMessage PrepareMessageFromString(byte[] message, string type, MessageStructure structure,
			bool fromPool)
		{
			return PrepareMessageFromString(message, type.AsByteArray(), structure, fromPool);
		}

		/// <summary>
		/// Builds <see cref="FixMessage"/> object from String object.
		/// </summary>
		/// <param name="message"> </param>
		/// <param name="type"> </param>
		/// <param name="structure"> </param>
		/// <exception cref="PreparedMessageException"> </exception>
		public virtual FixMessage PrepareMessageFromString(byte[] message, byte[] type, MessageStructure structure)
		{
			return PrepareMessageFromString(message, type, structure, true);
		}

		/// <summary>
		/// Builds <see cref="FixMessage"/> object from String object.
		/// </summary>
		/// <param name="message"> </param>
		/// <param name="type"> </param>
		/// <param name="structure"> </param>
		/// <exception cref="PreparedMessageException"> </exception>
		public virtual FixMessage PrepareMessageFromString(byte[] message, byte[] type, MessageStructure structure,
			bool fromPool)
		{
			return PrepareMessage(RawFixUtil.GetFixMessage(message), type, structure);
		}

		private int CalculateLength(MessageStructure ms)
		{
			var msgLength = 0;
			var size = ms.Size;
			for (var i = 0; i < size; i++)
			{
				var tagLength = ms.GetLength(i);
				if (MessageStructure.VariableLength != tagLength)
				{
					var tagId = ms.GetTagId(i);
					msgLength += FixTypes.FormatIntLength(tagId) + tagLength + 2;
				}
			}

			return msgLength;
		}

		private void FillMessage(FixMessage list, byte[] msgBuffer, MessageStructure ms)
		{
			var offset = 0;
			var size = ms.Size;
			for (var i = 0; i < size; i++)
			{
				var tagLength = ms.GetLength(i);
				var tag = ms.GetTagId(i);
				if (MessageStructure.VariableLength != tagLength)
				{
					offset += FixTypes.FormatInt(tag, msgBuffer, offset);
					msgBuffer[offset++] = (byte)'=';
					list.AddPrepared(tag, offset, tagLength);
					offset += tagLength;
					msgBuffer[offset++] = (byte)'\u0001';
				}
				else
				{
					list.AddTag(tag, EmptyBytes);
				}
			}
		}

		/// <exception cref="PreparedMessageException"> </exception>
		private MessageStructure BuildStructureFromFixMessage(MessageStructure dest, FixMessage message,
			MessageStructure structure)
		{
			IList<int> userTagIds = new List<int>(structure.TagIds);
			IList<int> userLengths = new List<int>(structure.Lengths);
			IList<ValueType> userTypes = new List<ValueType>(structure.Types);

			var markers = new int[dest.Size];
			markers.Fill(0);

			foreach (var tag in message)
			{
				var tagId = tag.TagId;

				var destPos = -1;
				do
				{
					destPos = dest.IndexOf(tagId);
				} while (destPos >= 0 && destPos < markers.Length && markers[destPos] > 0);

				if (destPos >= 0 && destPos < markers.Length)
				{
					//tag found in original set - need update
					markers[destPos] = 1;
					if (userTagIds.Contains(tagId))
					{
						var index = userTagIds.IndexOf(tagId);
						var length = userLengths[index];
						var type = userTypes[index];

						dest.SetLengthAtIndex(destPos, length);
						dest.SetTypeAtIndex(destPos, type);

						userTagIds.RemoveAt(index);
						userLengths.RemoveAt(index);
						userTypes.RemoveAt(index);
					}
				}
				else
				{
					//tag not found or it was just added - need adding
					if (userTagIds.Contains(tagId))
					{
						var index = userTagIds.IndexOf(tagId);
						var length = userLengths[index];
						var type = userTypes[index];
						dest.Reserve(tagId, length, type);
						userTagIds.RemoveAt(index);
						userLengths.RemoveAt(index);
						userTypes.RemoveAt(index);
					}
					else
					{
						dest.Reserve(tagId, tag.Length);
					}
				}
			}

			if (userTagIds.Count > 0)
			{
				throw new PreparedMessageException("There are reserved fields which are absent in sample message: " +
													string.Join(", ", userTagIds));
			}

			return dest;
		}

		private void FillHeaderAndTrailer(byte[] msgType, FixMessage msg, MessageStructure ms)
		{
			msg.Set(Tags.BeginString, _sessionParameters.FixVersion.MessageVersion);
			msg.Set(Tags.MsgType, msgType);
			msg.Set(Tags.SenderCompID, _sessionParameters.SenderCompId);
			msg.Set(Tags.TargetCompID, _sessionParameters.TargetCompId);

			SafeSetValue(msg, Tags.SenderSubID, _sessionParameters.SenderSubId);
			SafeSetValue(msg, Tags.TargetSubID, _sessionParameters.TargetSubId);
			SafeSetValue(msg, Tags.SenderLocationID, _sessionParameters.SenderLocationId);
			SafeSetValue(msg, Tags.TargetLocationID, _sessionParameters.TargetLocationId);

			foreach (var field in _sessionParameters.UserDefinedFields)
			{
				msg.Set(field.TagId, field.Buffer, field.Offset, field.Length);
			}

			if (RawFixUtil.IsLogon(msgType))
			{
				msg.Set(Tags.EncryptMethod, "0");
				msg.Set(Tags.HeartBtInt, Convert.ToString(_sessionParameters.HeartbeatInterval));
				foreach (var field in _sessionParameters.OutgoingLoginMessage)
				{
					msg.Set(field.TagId, field.Buffer, field.Offset, field.Length);
				}
			}

			msg.Set(Tags.BodyLength, msg.CalculateBodyLength());
		}

		private void SafeSetValue(FixMessage msg, int tagId, string value)
		{
			if (!ReferenceEquals(value, null))
			{
				msg.Set(tagId, value);
			}
		}

		private void AddHeaderStructure(byte[] msgType, MessageStructure ms)
		{
			var senderCompId = _sessionParameters.SenderCompId;
			if (ReferenceEquals(senderCompId, null))
			{
				throw new ArgumentException("SenderCompId can't be null");
			}

			var targetCompId = _sessionParameters.TargetCompId;
			if (ReferenceEquals(targetCompId, null))
			{
				throw new ArgumentException("TargetCompId can't be null");
			}

			var insetOffset = 0;
			insetOffset = ReserveIfAbsent(ms, insetOffset, Tags.BeginString,
				_sessionParameters.FixVersion.MessageVersion.Length, ValueType.String);
			insetOffset = ReserveIfAbsent(ms, insetOffset, Tags.BodyLength, BodylengthFieldLength, ValueType.Long);
			insetOffset = ReserveIfAbsent(ms, insetOffset, Tags.MsgType, msgType.Length, ValueType.String);
			insetOffset = ReserveIfAbsent(ms, insetOffset, Tags.MsgSeqNum, SeqnumFieldLength, ValueType.Long);
			insetOffset = ReserveIfAbsent(ms, insetOffset, Tags.SenderCompID, senderCompId.Length);
			insetOffset = ReserveIfAbsent(ms, insetOffset, Tags.TargetCompID, targetCompId.Length);

			//optional params
			insetOffset = SafeReserve(ms, insetOffset, Tags.SenderSubID, _sessionParameters.SenderSubId);
			insetOffset = SafeReserve(ms, insetOffset, Tags.TargetSubID, _sessionParameters.TargetSubId);
			insetOffset = SafeReserve(ms, insetOffset, Tags.SenderLocationID, _sessionParameters.SenderLocationId);
			insetOffset = SafeReserve(ms, insetOffset, Tags.TargetLocationID, _sessionParameters.TargetLocationId);
			foreach (var field in _sessionParameters.UserDefinedFields)
			{
				insetOffset = ReserveIfAbsent(ms, insetOffset, field.TagId, field.Length);
			}

			insetOffset = ReserveIfAbsent(ms, insetOffset, Tags.SendingTime, _timeStampLength, ValueType.Date);
			if (_includeLastProcessed)
			{
				// add 369 tag if needed
				var currentLength = GetBytesLength(_sessionParameters.IncomingSequenceNumber - 1);
				var seqNumFieldLength = Math.Max(currentLength, SeqnumFieldLength);
				insetOffset = ReserveIfAbsent(ms, insetOffset, Tags.LastMsgSeqNumProcessed, seqNumFieldLength,
					ValueType.Long);
			}

			if (RawFixUtil.IsLogon(msgType))
			{
				insetOffset = ReserveIfAbsent(ms, insetOffset, Tags.EncryptMethod, 1);
				insetOffset = ReserveIfAbsent(ms, insetOffset, Tags.HeartBtInt,
					GetBytesLength(_sessionParameters.HeartbeatInterval), ValueType.Long);
				foreach (var field in _sessionParameters.OutgoingLoginMessage)
				{
					insetOffset = ReserveIfAbsent(ms, insetOffset, field.TagId, field.Length);
				}
			}
		}

		private ISet<int> PrepareHeaderTagSet(byte[] msgType)
		{
			ISet<int> res = new HashSet<int>();
			res.Add(Tags.BeginString);
			res.Add(Tags.BodyLength);
			res.Add(Tags.MsgType);
			res.Add(Tags.MsgSeqNum);
			res.Add(Tags.SenderCompID);
			res.Add(Tags.TargetCompID);
			res.Add(Tags.SendingTime);
			if (!ReferenceEquals(_sessionParameters.SenderSubId, null))
			{
				res.Add(Tags.SenderSubID);
			}

			if (!ReferenceEquals(_sessionParameters.TargetSubId, null))
			{
				res.Add(Tags.TargetSubID);
			}

			if (!ReferenceEquals(_sessionParameters.SenderLocationId, null))
			{
				res.Add(Tags.SenderLocationID);
			}

			if (!ReferenceEquals(_sessionParameters.TargetLocationId, null))
			{
				res.Add(Tags.TargetLocationID);
			}

			var message = _sessionParameters.UserDefinedFields;
			var size = message.Count;
			for (var i = 0; i < size; i++)
			{
				res.Add(message.GetTagIdAtIndex(i));
			}

			if (_includeLastProcessed)
			{
				res.Add(Tags.LastMsgSeqNumProcessed);
			}

			if (RawFixUtil.IsLogon(msgType))
			{
				res.Add(Tags.EncryptMethod);
				res.Add(Tags.HeartBtInt);

				var outgoingLoginFixMessage = _sessionParameters.OutgoingLoginMessage;
				var logonFieldsSize = outgoingLoginFixMessage.Count;
				for (var i = 0; i < logonFieldsSize; i++)
				{
					res.Add(outgoingLoginFixMessage.GetTagIdAtIndex(i));
				}
			}

			return res;
		}

		private MessageStructure AddTrailerStructure(MessageStructure ms)
		{
			ReserveIfAbsent(ms, ms.Size, Tags.CheckSum, CheckSumFieldLength, ValueType.Long);
			return ms;
		}

		private MessageStructure UpdateBodyLengthStructure(MessageStructure ms, FixMessage template)
		{
			var msgBl = template.CalculateBodyLength();
			if (msgBl > BodylengthFieldDefaultMax)
			{
				var length = 1;
				var tempValue = msgBl;
				while ((tempValue /= 10) > 0)
				{
					length++;
				}

				ReserveIfAbsent(ms, ms.IndexOfTag(Tags.BodyLength), Tags.BodyLength, length, ValueType.Long);
			}

			return ms;
		}

		private int SafeReserve(MessageStructure msg, int pos, int tagId, string param)
		{
			if (!ReferenceEquals(param, null))
			{
				return ReserveIfAbsent(msg, pos, tagId, param.Length);
			}

			return pos;
		}

		private int ReserveIfAbsent(MessageStructure ms, int pos, int tagId, int length)
		{
			return ReserveIfAbsent(ms, pos, tagId, length, ValueType.ByteArray);
		}

		private int ReserveIfAbsent(MessageStructure ms, int pos, int tagId, int length, ValueType type)
		{
			if (!ms.ContainsTagId(tagId))
			{
				ms.Reserve(pos, tagId, length, type);
				return ++pos;
			}

				ms.SetLength(tagId, length);
				ms.SetType(tagId, type);
				return ms.IndexOfTag(tagId);
			}

		/// <summary>
		/// returns size of serialized long in bytes
		/// </summary>
		/// <param name="num"> number </param>
		/// <returns> number of bytes </returns>
		public static int GetBytesLength(long num)
		{
			var size = 1;
			if (num < 0)
			{
				size++;
				num = -num;
			}

			while ((num /= 10) > 0)
			{
				size++;
			}

			return size;
		}

		/// <seealso cref="FixTypes.FormatUInt(long)"> </seealso>
		internal static byte[] FormatUInt(long val)
		{
			return FixTypes.FormatUInt(val);
		}

		internal static byte[] FormatInt(long val)
		{
			return FixTypes.FormatInt(val);
		}

		/// <seealso cref="FixTypes.ParseInt(byte[], int, int)"> </seealso>
		internal static int ParseInt(byte[] buffer, int offset, int length)
		{
			return (int)FixTypes.ParseInt(buffer, offset, length);
		}
	}
}
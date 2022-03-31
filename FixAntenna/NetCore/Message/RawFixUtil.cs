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
using System.Linq;
using System.Text;
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Common.Pool;
using Epam.FixAntenna.NetCore.Common.Pool.Provider;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message.Rg;
using Epam.FixAntenna.NetCore.Message.Rg.Exceptions;

namespace Epam.FixAntenna.NetCore.Message
{
	using TIntHashSet = HashSet<int>;

	public sealed class RawFixUtil
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(RawFixUtil));
		internal static readonly IRawTags DefaultRawTags = new DefaultRawTags();
		private static readonly byte[] EmptyValue = Array.Empty<byte>();

		private static readonly byte[] LogonBytes = "A".AsByteArray();

		internal static IPool<TagValue> FieldPool;
		internal static IPool<FixMessage> FieldListPool;

		static RawFixUtil()
		{
			{
				FieldPool = PoolFactory.GetConcurrentBucketsPool(10, 200, 2000,
					new TagValueProvider());

				FieldListPool = PoolFactory.GetConcurrentBucketsPool(10, 200, 2000,
					new FixMessageProvider());
			}
		}

		private RawFixUtil()
		{
		}

		/// <summary>
		/// Calculates checksum in array of bytes.
		/// </summary>
		/// <param name="bytes"> array of bytes </param>
		public static int GetChecksum(byte[] bytes)
		{
			return GetChecksum(bytes, 0, bytes.Length);
		}

		/// <summary>
		/// Calculates checksum in array of bytes.
		/// </summary>
		/// <param name="bytes">  the array of bytes </param>
		/// <param name="offset"> the offset in array of  bytes </param>
		/// <param name="length"> the length of bytes </param>
		public static int GetChecksum(byte[] bytes, int offset, int length)
		{
			var checksum = 0;
			if (offset + length > bytes.Length)
			{
				throw new ArgumentException(
					"Argument error. Byte array length is less then offset + length. Bytearray length: " +
					bytes.Length + " Offset: " + offset + " Length: " + length);
			}

			for (var i = offset; i < offset + length; i++)
			{
				checksum += (char)bytes[i];
			}

			return checksum % 256;
		}

		/// <summary>
		/// Calculates checksum in matrix of bytes.
		/// </summary>
		/// <param name="bytes">  the array of bytes </param>
		/// <param name="offset"> the offset in array of  bytes </param>
		/// <param name="length"> the length of bytes </param>
		internal static int GetChecksum(byte[][] bytes, int offset, int length)
		{
			var checksum = 0;
			var wholeLength = 0;
			foreach (var item in bytes)
			{
				wholeLength += item.Length;
			}

			var off = 0;
			if (offset + length > wholeLength)
			{
				throw new ArgumentException(
					"Argument error. Byte array length is less then offset + length. Byte matrix length: " +
					wholeLength + " Offset: " + offset + " Length: " + length);
			}

			foreach (var item in bytes)
			{
				foreach (var nested in item)
				{
					off++;
					if (off > offset && off <= offset + length)
					{
						checksum += (char)nested;
					}
				}
			}

			return checksum % 256;
		}

		/// <summary>
		/// Gets the value from buffer as bytes.
		/// </summary>
		/// <param name="buffer">          the buffer of bytes </param>
		/// <param name="offset">          the offset in buffer </param>
		/// <param name="length">          the buffer length </param>
		/// <param name="tag">             the tag id </param>
		/// <param name="searchFromStart"> search from start or end of the given buffer </param>
		public static byte[] GetRawValue(byte[] buffer, int offset, int length, int tag, bool searchFromStart = true)
		{
			int seqStart;
			if (searchFromStart)
			{
				seqStart = GetStartValueFromStartBuffer(buffer, offset, length, tag);
			}
			else
			{
				seqStart = GetStartValueFromEndBuffer(buffer, offset, length, tag);
			}

			// if tag is not exist, return null
			if (seqStart == -1)
			{
				return null;
			}

			var j = 0;
			// count the length of tag value
			while (seqStart + j < buffer.Length && buffer[seqStart + j] != 1)
			{
				j++;
			}

			var result = new byte[j];
			Array.Copy(buffer, seqStart, result, 0, j);
			return result;
		}

		/// <summary>
		/// Gets the value from buffer as bytes.
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		/// <param name="offset"> the offset in buffer </param>
		/// <param name="length"> the buffer length </param>
		/// <param name="tag">    the tag id </param>
		internal static IList<byte[]> GetAllRawValues(byte[] buffer, int offset, int length, int tag)
		{
			IList<byte[]> result = new List<byte[]>();

			var relativePosition = 0;
			int seqStart;
			while (relativePosition < length &&
					(seqStart = GetStartValueFromStartBuffer(buffer, offset + relativePosition,
						length - relativePosition, tag)) != -1)
			{
				var j = 0;
				// count the length of tag value
				while (seqStart + j < buffer.Length && buffer[seqStart + j] != 1)
				{
					j++;
				}

				var tagValue = new byte[j];
				Array.Copy(buffer, seqStart, tagValue, 0, j);
				result.Add(tagValue);
				relativePosition = seqStart + j - offset;
			}

			return result;
		}

		internal static int GetStartValueFromEndBuffer(byte[] buffer, int offset, int length, int tagId)
		{
			var tagIdLength = FixTypes.FormatIntLength(tagId);
			var startIndex = -1;

			for (var i = length - tagIdLength; i >= offset; i--)
			{
				if (buffer[i] == 1)
				{
					// is SOH
					if (i + tagIdLength + 1 >= buffer.Length || buffer[i + tagIdLength + 1] != (byte)'=')
					{
						goto outerContinue; // next iteration
					}

					var tempTagId = tagId;
					var pos = i + tagIdLength + 1;
					do
					{
						if (buffer[--pos] != tempTagId % 10 + '0')
						{
							goto outerContinue; // next iteration
						}
					} while ((tempTagId /= 10) > 0);

					startIndex = i + tagIdLength + 2;
					break;
				}

				outerContinue: ;
			}

			return startIndex;
		}

		internal static int GetStartValueFromStartBuffer(byte[] buffer, int offset, int length, int tagId)
		{
			var tagIdLength = FixTypes.FormatIntLength(tagId);
			var lookUpToPosition = offset + length - tagIdLength;
			var startIndex = -1;

			for (var i = offset; i < lookUpToPosition; i++)
			{
				if (i == offset || buffer[i - 1] == 1)
				{
					// is SOH
					if (buffer[i + tagIdLength] != (byte)'=')
					{
						goto outerContinue; // next iteration
					}

					var tempTagId = tagId;
					var pos = i + tagIdLength;
					do
					{
						if (buffer[--pos] != tempTagId % 10 + '0')
						{
							goto outerContinue; // next iteration
						}
					} while ((tempTagId /= 10) > 0);

					startIndex = i + tagIdLength + 1;
					break;
				}

				outerContinue: ;
			}

			return startIndex;
		}

		/// <summary>
		/// Gets the value from buffer as long.
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		/// <param name="offset"> the offset in buffer </param>
		/// <param name="length"> the buffer length </param>
		/// <param name="tag">    the tag id </param>
		/// <exception cref="ArgumentException"> if tag not exists </exception>
		internal static long GetLongValue(byte[] buffer, int offset, int length, int tag)
		{
			return GetLongValue(buffer, offset, length, tag, true);
		}

		/// <summary>
		/// Gets the value from buffer as long.
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		/// <param name="offset"> the offset in buffer </param>
		/// <param name="length"> the buffer length </param>
		/// <param name="tag">    the tag id </param>
		/// <param name="searchFromStart">    search from the start or end buffer </param>
		/// <exception cref="ArgumentException"> if tag not exists </exception>
		internal static long GetLongValue(byte[] buffer, int offset, int length, int tag, bool searchFromStart)
		{
			int startIndexValue;
			if (searchFromStart)
			{
				startIndexValue = GetStartValueFromStartBuffer(buffer, offset, length, tag);
			}
			else
			{
				startIndexValue = GetStartValueFromEndBuffer(buffer, offset, length, tag);
			}

			if (startIndexValue == -1)
			{
				throw new ArgumentException("Tag " + tag + " is missing or has invalid value");
			}

			var lengthValue = 0;
			// count the length of tag value
			while (startIndexValue + lengthValue < buffer.Length && buffer[startIndexValue + lengthValue] != 1)
			{
				lengthValue++;
			}

			return FixTypes.ParseInt(buffer, startIndexValue, lengthValue);
		}

		/// <summary>
		/// Gets sequence number.
		/// </summary>
		/// <param name="message"> the buffer of bytes </param>
		/// <exception cref="ArgumentException"> if tag not exists </exception>
		internal static long GetSequenceNumber(byte[] message)
		{
			return GetLongValue(message, 0, message.Length, Tags.MsgSeqNum);
		}

		/// <summary>
		/// Gets sequence number.
		/// </summary>
		/// <param name="message"> the buffer of bytes </param>
		/// <param name="offset">  the offset in buffer </param>
		/// <param name="length">  the buffer length </param>
		/// <exception cref="ArgumentException"> if tag not exists </exception>
		public static long GetSequenceNumber(byte[] message, int offset, int length)
		{
			return GetLongValue(message, offset, length, Tags.MsgSeqNum);
		}

		public static FixMessage GetFixMessage(byte[] buffer, int messageOffset, int messageLength)
		{
			return GetFixMessage(buffer, messageOffset, messageLength, DefaultRawTags);
		}

		public static FixMessage GetFixMessage(MsgBuf buf)
		{
			return GetFixMessage(buf.Buffer, buf.Offset, buf.Length, DefaultRawTags);
		}

		internal static FixMessage GetFixMessage(MsgBuf buf, IRawTags rawTags)
		{
			return GetFixMessage(buf.Buffer, buf.Offset, buf.Length, rawTags, false, false);
		}

		internal static FixMessage GetFixMessage(FixMessage target, MsgBuf buf, IRawTags rawTags,
			bool clearTheMessageBeforeUse)
		{
			return GetFixMessage(target, buf.Buffer, buf.Offset, buf.Length, rawTags, clearTheMessageBeforeUse);
		}

		/// <summary>
		/// Creates index by dictionary for repeating group.
		/// After calling this method, its possible to use Repeating Group API
		/// Version and type of message will be got from message
		/// Validation is turn off </summary>
		/// <param name="msg"> message for indexing </param>
		/// <returns> the passed message </returns>
		public static FixMessage IndexRepeatingGroup(FixMessage msg)
		{
			var version = msg.GetFixVersion();
			var msgType = GetMsgType(msg);
			return IndexRepeatingGroup(msg, version, msgType, false);
		}

		public static string GetMsgType(IndexedStorage msg)
		{
			var index = msg.GetTagIndex(Tags.MsgType);
			if (index == IndexedStorage.NotFound)
			{
				return null;
			}

			return Encoding.UTF8.GetString(msg.GetTagValueAsBytesAtIndex(index));
		}

		/// <summary>
		/// Creates index by dictionary for repeating group.
		/// After calling this method, its possible to use Repeating Group API
		/// Version and type of message will be got from message </summary>
		/// <param name="msg"> message for indexing </param>
		/// <param name="validation"> turn on/off validation </param>
		/// <returns> the passed message </returns>
		public static FixMessage IndexRepeatingGroup(FixMessage msg, bool validation)
		{
			return (FixMessage)IndexRepeatingGroup((IndexedStorage)msg, validation);
		}

		public static IndexedStorage IndexRepeatingGroup(IndexedStorage msg, bool validation)
		{
			var version = msg.GetFixVersion();
			if (version == null)
			{
				throw new ArgumentException("There is no info about FIX version in message. Please use method " +
											"indexRepeatingGroup(FixMessage, FixVersion, MessageType");
			}

			var msgType = GetMsgType(msg);
			if (msgType == null)
			{
				throw new ArgumentException("There is no info about message type in message. Please use method " +
											"indexRepeatingGroup(FixMessage, FixVersion, MessageType");
			}

			return IndexRepeatingGroup(msg, version, msgType, validation);
		}

		/// <summary>
		/// Creates index by dictionary for repeating group.
		/// After calling this method, its possible to use Repeating Group API
		/// Validation is turn off </summary>
		/// <param name="msg"> message for indexing </param>
		/// <param name="version"> version for passed message. Used for choose dictionary for indexing message </param>
		/// <param name="msgType"> type of passed message. Used for indexing message. </param>
		/// <returns> the passed message </returns>
		public static FixMessage IndexRepeatingGroup(FixMessage msg, FixVersion version, string msgType)
		{
			return IndexRepeatingGroup(msg, FixVersionContainer.GetFixVersionContainer(version), msgType);
		}

		public static FixMessage IndexRepeatingGroup(FixMessage msg, FixVersionContainer version, string msgType)
		{
			return IndexRepeatingGroup(msg, version, msgType, false);
		}

		/// <summary>
		/// Creates index by dictionary for repeating group.
		/// After calling this method, its possible to use Repeating Group API </summary>
		/// <param name="msg"> message for indexing </param>
		/// <param name="version"> version for passed message. Used for choose dictionary for indexing message </param>
		/// <param name="msgType"> type of passed message. Used for indexing message. </param>
		/// <param name="validation"> turn on/off validation </param>
		/// <returns> the passed message </returns>
		public static FixMessage IndexRepeatingGroup(FixMessage msg, FixVersion version, string msgType,
			bool validation)
		{
			return IndexRepeatingGroup(msg, FixVersionContainer.GetFixVersionContainer(version), msgType, validation);
		}

		public static FixMessage IndexRepeatingGroup(FixMessage msg, FixVersionContainer version, string msgType,
			bool validation)
		{
			return (FixMessage)IndexRepeatingGroup((IndexedStorage)msg, version, msgType, validation);
		}

		public static FixMessage IndexRepeatingGroup(FixMessage msg, FixVersion version, bool validation)
		{
			return IndexRepeatingGroup(msg, FixVersionContainer.GetFixVersionContainer(version), validation);
		}

		public static FixMessage IndexRepeatingGroup(FixMessage msg, FixVersionContainer version, bool validation)
		{
			var msgType = GetMsgType(msg);
			if (version == null || msgType == null)
			{
				throw new ArgumentException(
					"There is no info about message type in message. Please use method indexRepeatingGroup(FixMessage, FixVersion, MessageType");
			}

			return (FixMessage)IndexRepeatingGroup((IndexedStorage)msg, version, msgType, validation);
		}

		public static IndexedStorage IndexRepeatingGroup(IndexedStorage msg, FixVersion version, string msgType,
			bool validation)
		{
			return IndexRepeatingGroup(msg, FixVersionContainer.GetFixVersionContainer(version), msgType, validation);
		}

		public static IndexedStorage IndexRepeatingGroup(IndexedStorage msg, FixVersionContainer version,
			string msgType, bool validation)
		{
			msg.InitRepeatingGroupStorage(version, msgType, validation);
			if (validation)
			{
				return IndexRepeatingGroupWithValidation(msg, version, msgType);
			}

			return IndexRepeatingGroupWithoutValidation(msg, version, msgType);
		}

		private static IndexedStorage IndexRepeatingGroupWithValidation(IndexedStorage msg, FixVersionContainer version,
			string msgType)
		{
			var dict = DictionaryHolder.GetDictionary(version);
			var messageWithGroupDict = dict.GetMessageDict(msgType);
			if (messageWithGroupDict != null)
			{
				var leadingTags = messageWithGroupDict.GetOuterLeadingTags();
				var nestedTags = messageWithGroupDict.GetNestedLeadingTagsArray();
				var allGroupTags = messageWithGroupDict.GetAllGroupTags();
				leadingTags.UnionWith(nestedTags);
				for (var i = 0; i < msg.Count; i++)
				{
					var tag = msg.GetTagIdAtIndex(i);
					if (leadingTags.Contains(tag) && msg.GetTagValueAsLongAtIndex(i) > 0)
					{
						i = ParseGroup(msg, messageWithGroupDict, i, tag, leadingTags, 0, version, msgType, true) - 1;
					}
					else if (allGroupTags.Contains(tag))
					{
						throw new UnexpectedGroupTagException(tag, version, msgType);
					}
				}
			}

			return msg;
		}

		private static IndexedStorage IndexRepeatingGroupWithoutValidation(IndexedStorage msg,
			FixVersionContainer version, string msgType)
		{
			var dict = DictionaryHolder.GetDictionary(version);
			var messageWithGroupDict = dict.GetMessageDict(msgType);

			if (messageWithGroupDict != null)
			{
				var outerLeadingTags = messageWithGroupDict.GetOuterLeadingTagsArray();
				var nestedTags = messageWithGroupDict.GetNestedLeadingTags();
				foreach (var leadingTag in outerLeadingTags)
				{
					if (msg.IsTagExists(leadingTag) && msg.GetTagValueAsLongAtIndex(msg.GetTagIndex(leadingTag)) > 0)
					{
						var parseStartIndex = msg.GetTagIndex(leadingTag);
						ParseGroup(msg, messageWithGroupDict, parseStartIndex, leadingTag, nestedTags, 0, version,
							msgType, false);
					}
				}
			}

			return msg;
		}

		private static int ParseGroup(IndexedStorage msg, MessageWithGroupDict messageWithGroupDict, int i, int tag,
			TIntHashSet leadingTags, int level, FixVersionContainer version, string msgType, bool validation)
		{
			var delimiter = messageWithGroupDict.GetDelimiterTag(tag);
			msg.StartCreateRg(tag, delimiter, i);
			var groupTags = messageWithGroupDict.GetGroupTags(tag);
			var allGroupTags = messageWithGroupDict.GetAllGroupTags();
			i++;
			var tagForAdd = msg.GetTagIdAtIndex(i);
			if (tagForAdd != delimiter)
			{
				throw new InvalidDelimiterTagException(delimiter, tagForAdd, version, msgType);
			}

			while (i < msg.Count && groupTags.Contains(tagForAdd))
			{
				if (leadingTags.Contains(tagForAdd) && msg.GetTagValueAsLongAtIndex(i) > 0)
				{
					i = ParseGroup(msg, messageWithGroupDict, i, tagForAdd, leadingTags, ++level, version, msgType,
						validation);
					if (i < msg.Count)
					{
						tagForAdd = msg.GetTagIdAtIndex(i);
					}
					else
					{
						break;
					}
				}
				else
				{
					msg.AddTagToRg(tagForAdd, i, tag);
					i++;
					if (i < msg.Count)
					{
						tagForAdd = msg.GetTagIdAtIndex(i);
					}
					else
					{
						break;
					}
				}
			}

			if (validation && level == 0)
			{
				if (allGroupTags.Contains(tagForAdd))
				{
					throw new UnexpectedGroupTagException(tagForAdd, version, msgType);
				}
			}

			msg.StopCreateRg();
			return i;
		}

		public static FixMessage GetFixMessage(string message)
		{
			return GetFixMessage(message.AsByteArray());
		}

		/// <summary>
		/// Parses fix message from array of bytes.
		/// </summary>
		/// <param name="message"> the buffer of bytes </param>
		/// <exception cref="GarbledMessageException"> if message is garbled </exception>
		public static FixMessage GetFixMessage(byte[] message)
		{
			return GetFixMessage(message, 0, message.Length, DefaultRawTags, false, false);
		}

		internal static FixMessage GetFixMessage(byte[] message, bool usePool, bool isUserOwned)
		{
			return GetFixMessage(message, 0, message.Length, DefaultRawTags, usePool, isUserOwned);
		}

		/// <summary>
		/// Parses fix message from array of bytes.
		/// </summary>
		/// <param name="message"> the buffer of bytes </param>
		/// <exception cref="GarbledMessageException"> if message is garbled </exception>
		internal static FixMessage GetFixMessageUntilTagsExists(byte[] message)
		{
			return GetFixMessageUntilTagsExists(message, 0, message.Length, DefaultRawTags);
		}

		/// <summary>
		/// Parses fix message from array of bytes.
		/// </summary>
		/// <param name="message"> the buffer of bytes </param>
		/// <param name="rawTags"> the raw tags </param>
		/// <exception cref="GarbledMessageException"> if message is garbled </exception>
		internal static FixMessage GetFixMessage(byte[] message, int[] rawTags)
		{
			return GetFixMessage(message, new CustomRawTags(rawTags));
		}

		internal static FixMessage GetFixMessage(byte[] message, IRawTags rawTags)
		{
			return GetFixMessage(message, 0, message.Length, rawTags);
		}

		/// <summary>
		/// Parses fix message from array of bytes.
		/// </summary>
		/// <param name="message"> the buffer of bytes </param>
		/// <param name="rawTags"> the raw tags </param>
		/// <exception cref="GarbledMessageException"> if message is garbled </exception>
		internal static FixMessage GetFixMessage(byte[] message, int messageOffset, int messageLength, int[] rawTags)
		{
			return GetFixMessage(message, messageOffset, messageLength, new CustomRawTags(rawTags));
		}

		internal static FixMessage GetFixMessage(byte[] message, int messageOffset, int messageLength,
			IRawTags rawTags)
		{
			return GetFixMessage(message, messageOffset, messageLength, rawTags, false, false);
		}

		/// <summary>
		/// Parses fix message from array of bytes.
		/// </summary>
		/// <param name="message"> the buffer of bytes </param>
		/// <param name="rawTags"> the raw tags </param>
		/// <returns> instance of parsed message </returns>
		/// <exception cref="GarbledMessageException"> if message is garbled </exception>
		internal static FixMessage GetFixMessage(byte[] message, int messageOffset, int messageLength,
			IRawTags rawTags, bool allocateFromPool, bool isUserOwned)
		{
			FixMessage list;
			if (allocateFromPool)
			{
				list = GetFixMessageFromPool(isUserOwned);
			}
			else
			{
				list = new FixMessage(isUserOwned);
			}

			return GetFixMessage(list, message, messageOffset, messageLength, rawTags, false);
		}

		internal static FixMessage GetFixMessage(FixMessage list, byte[] message, int messageOffset,
			int messageLength, IRawTags rawTags)
		{
			return GetFixMessage(list, message, messageOffset, messageLength, rawTags, true);
		}

		internal static FixMessage GetFixMessage(FixMessage list, byte[] message, int messageOffset,
			int messageLength, IRawTags rawTags, bool clearTheMessageBeforeUse)
		{
			if (clearTheMessageBeforeUse)
			{
				((AbstractFixMessage)list).Clear();
			}

			list.SetBuffer(message, messageOffset, messageLength);

			var valueStartIndex = 0;
			var tag = 0;
			var isTagParsing = true;
			var messageEndOffset = messageOffset + messageLength;
			for (var index = messageOffset; index < messageEndOffset; index++)
			{
				var b = message[index];
				if (isTagParsing)
				{
					if (b >= (byte)'0' && b <= (byte)'9')
					{
						tag = tag * 10 + (b - '0');
					}
					else if (b == (byte)'=')
					{
						valueStartIndex = index + 1;
						if (rawTags.IsWithinRawTags(tag))
						{
							index += GetRawTagLengthFromPreviousField(list);
						}

						isTagParsing = false;
					}
					else
					{
						throw new GarbledMessageException("Invalid tag number");
					}
				}
				else
				{
					if (b == (byte)'\u0001')
					{
						list.Add(tag, valueStartIndex, index - valueStartIndex);

						tag = 0;
						isTagParsing = true;
					}
				}
			}

			if (!isTagParsing || tag != 0)
			{
				throw new GarbledMessageException("No SOH symbol at the end of message");
			}

			return list;
		}

		/// <summary>
		/// Parses fix message from array of bytes.
		/// </summary>
		/// <param name="message"> the buffer of bytes </param>
		/// <param name="rawTags"> the raw tags </param>
		/// <returns> instance of parsed message </returns>
		/// <exception cref="GarbledMessageException"> if message is garbled </exception>
		internal static FixMessage GetFixMessageUntilTagsExists(byte[] message, int messageOffset, int messageLength,
			IRawTags rawTags)
		{
			var list = new FixMessage();
			var valueStartIndex = 0;
			var tag = 0;
			var isTagParsing = true;
			var messageEndOffset = messageOffset + messageLength;
			for (var index = messageOffset; index < messageEndOffset; index++)
			{
				var b = message[index];
				if (isTagParsing)
				{
					if (b >= (byte)'0' && b <= (byte)'9')
					{
						tag = tag * 10 + (b - '0');
					}
					else if (b == (byte)'=')
					{
						valueStartIndex = index + 1;
						if (rawTags.IsWithinRawTags(tag))
						{
							index += GetRawTagLengthFromPreviousField(list);
						}

						isTagParsing = false;
					}
					else
					{
						break;
					}
				}
				else
				{
					if (b == (byte)'\x0001')
					{
						list.AddTag(tag, CopyValue(message, valueStartIndex, index - valueStartIndex));
						tag = 0;
						isTagParsing = true;
					}
				}
			}

			if (!isTagParsing || tag != 0)
			{
				throw new GarbledMessageException("No SOH symbol at the end of message");
			}

			return list;
		}

		internal static TagValue GetFieldFromPool()
		{
			try
			{
				return FieldPool.Object;
			}
			catch (Exception e)
			{
				if (Log.IsDebugEnabled)
				{
					Log.Warn("Can't get new object from pool: " + e.Message, e);
				}
				else
				{
					Log.Warn("Can't get new object from pool: " + e.Message);
				}
			}

			return null;
		}

		internal static FixMessage GetFixMessageFromPool(bool isUserOwned)
		{
			try
			{
				var o = FieldListPool.Object;
				o.IsUserOwned = isUserOwned;
				o.IsFree = false;
				return o;
			}
			catch (Exception e)
			{
				if (Log.IsDebugEnabled)
				{
					Log.Warn("Can't get new object from pool: " + e.Message, e);
				}
				else
				{
					Log.Warn("Can't get new array object pool: " + e.Message);
				}
			}

			return null;
		}

		internal static void ReturnObj(TagValue field)
		{
			FieldPool.ReturnObject(field);
		}

		internal static void ReturnObj(FixMessage fixMessage)
		{
			if (fixMessage.IsOriginatingFromPool)
			{
				((AbstractFixMessage)fixMessage).Clear();
				fixMessage.IsFree = true;
				FieldListPool.ReturnObject(fixMessage);
			}
		}

		internal static int FieldObjectsCreated => FieldPool.ObjectsCreated;

		internal static int FieldListObjectsCreated => FieldListPool.ObjectsCreated;

		internal static byte[] CopyValueUsePool(byte[] message, int valueStartIndex, int length)
		{
			var value = ByteArrayPool.GetByteArrayFromPool(length);
			Array.Copy(message, valueStartIndex, value, 0, length);
			return value;
		}

		internal static byte[] CopyValue(byte[] message, int valueStartIndex, int length)
		{
			var value = new byte[length];
			Array.Copy(message, valueStartIndex, value, 0, length);
			return value;
		}

		internal static int GetRawTagLengthFromPreviousField(FixMessage list)
		{
			try
			{
				return (int)list.GetTagValueAsLongAtIndex(list.Length - 1);
			}
			catch (Exception)
			{
				throw new GarbledMessageException("Invalid or missing raw tag length");
			}
		}

		/// <summary>
		/// Gets the message type.
		/// If type is unknown return empty array.
		/// </summary>
		/// <param name="bytes"> the message </param>
		internal static byte[] GetMessageType(byte[] bytes)
		{
			var tagValue = GetRawValue(bytes, 0, bytes.Length, Tags.MsgType, true);
			return tagValue ?? EmptyValue;
		}

		/// <summary>
		/// Gets the message type.
		/// If type is unknown return empty array.
		/// </summary>
		/// <param name="bytes"> the message </param>
		/// <param name="offset">the offset in buffer </param>
		/// <param name="length">the buffer length </param>
		internal static byte[] GetMessageType(byte[] bytes, int offset, int length)
		{
			var tagValue = GetRawValue(bytes, offset, length, Tags.MsgType, true);
			return tagValue ?? EmptyValue;
		}

		/// <summary>
		/// Checks the message session level type.
		/// </summary>
		/// <param name="message"> the fix message </param>
		/// <returns> true if message is level session message </returns>
		internal static bool IsSessionLevelMessage(byte[] message, int offset, int length)
		{
			var bytes = GetMessageType(message, offset, length);
			return bytes != null && IsSessionLevelType(bytes);
		}

		/// <summary>
		/// Checks the message session level type.
		/// </summary>
		/// <param name="message"> the fix message </param>
		/// <returns> true if message is level session message </returns>
		internal static bool IsSessionLevelMessage(byte[] message)
		{
			var bytes = GetMessageType(message);
			return bytes != null && IsSessionLevelType(bytes);
		}

		internal static bool IsSessionLevelMessage(FixMessage message)
		{
			try
			{
				var length = message.GetTagLength(35);
				var msgType = message.GetTagValueAsByte(35, 0);
				return IsSessionLevelType(msgType, length);
			}
			catch (FieldNotFoundException)
			{
			}

			return false;
		}

		/// <summary>
		/// Checks the message session level type.
		/// </summary>
		/// <param name="msgType"> the message type </param>
		/// <returns> true if message is level session message </returns>
		internal static bool IsSessionLevelType(string msgType)
		{
			if (msgType.Length == 1)
			{
				var ch = msgType[0];
				if (ch == 'A' || ch >= '0' && ch <= '5')
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Checks the message session level type.
		/// </summary>
		/// <param name="msgType"> the fix message </param>
		/// <returns> true if message is level session message </returns>
		internal static bool IsSessionLevelType(byte[] msgType)
		{
			if (msgType.Length == 1)
			{
				var b = msgType[0];
				if (b == (byte)'A' || b >= (byte)'0' && b <= (byte)'5')
				{
					return true;
				}
			}

			return false;
		}

		internal static bool IsSessionLevelType(byte msgType, int length)
		{
			if (length == 1)
			{
				if (msgType == (byte)'A' || msgType >= (byte)'0' && msgType <= (byte)'5')
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Checks if message type is Logon.
		/// </summary>
		/// <param name="msgType"> the message type, parameter must be not null </param>
		/// <returns> true if is </returns>
		internal static bool IsLogon(string msgType)
		{
			return msgType.Length == 1 && msgType[0] == 'A';
		}

		/// <summary>
		/// Checks if message type is Logon.
		/// </summary>
		/// <param name="msgType"> the message type, parameter must be not null </param>
		/// <returns> true if is </returns>
		internal static bool IsLogon(byte[] msgType)
		{
			return msgType.Length == 1 && msgType[0] == LogonBytes[0];
		}

		/// <summary>
		/// Checks if message type is Logon.
		/// </summary>
		/// <param name="fixMessage"> fix field list, parameter must be not null </param>
		/// <returns> true if is </returns>
		internal static bool IsLogon(FixMessage fixMessage)
		{
			return fixMessage.IsTagValueEqual(Tags.MsgType, LogonBytes);
		}

		/// <summary>
		/// Creates RawTags from array of int.
		/// </summary>
		/// <param name="rawTags"> </param>
		/// <returns> RawTags </returns>
		internal static IRawTags CreateRawTags(int[] rawTags)
		{
			var customRawTags = new CustomRawTags(rawTags);
			if (customRawTags.GetRawTags().SequenceEqual(Message.DefaultRawTags.DefaultRawTagsSorted))
			{
				return DefaultRawTags;
			}

			return customRawTags;
		}

		/// <summary>
		/// Creates RawTags from string. The raw tags should be separate by ',' , ' ' or '.'.
		/// </summary>
		/// <param name="rawTags"> </param>
		/// <returns> RawTags </returns>
		internal static IRawTags CreateRawTags(string rawTags)
		{
			if (string.IsNullOrEmpty(rawTags))
			{
				// if empty value
				return DefaultRawTags;
			}

			var tokens = rawTags.Split(new[] { ',', ' ', '.' }, StringSplitOptions.RemoveEmptyEntries);
			var rawTagsArray = new int[tokens.Length];

			for (var i = 0; i < tokens.Length; i++)
			{
				try
				{
					rawTagsArray[i] = int.Parse(tokens[i]);
				}
				catch (Exception)
				{
					rawTagsArray[i] = 0;
				}
			}

			return CreateRawTags(rawTagsArray);
		}

		private class TagValueProvider : AbstractPoolableProvider<TagValue>
		{
			public override TagValue Create()
			{
				return new TagValue(true);
			}

			public override void Activate(TagValue t)
			{
			}
		}

		private class FixMessageProvider : AbstractPoolableProvider<FixMessage>
		{
			public override FixMessage Create()
			{
				var list = new FixMessage
				{
					IsOriginatingFromPool = true
				};
				return list;
			}

			public override void Activate(FixMessage t)
			{
			}
		}

		internal interface IRawTags
		{
			/// <summary>
			/// Checks if tag exist in array of tags.
			/// </summary>
			/// <param name="tag"> the tag id </param>
			/// <returns> true if exists </returns>
			bool IsWithinRawTags(int tag);
		}
	}
}
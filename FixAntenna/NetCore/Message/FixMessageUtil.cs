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

using System.Collections.Generic;
using Epam.FixAntenna.Constants.Fixt11;

namespace Epam.FixAntenna.NetCore.Message
{
	/// <summary>
	/// FixMessage helper class.
	/// </summary>
	internal class FixMessageUtil
	{
		private FixMessageUtil()
		{
		}

		/// <summary>
		/// Checks the message type.
		/// </summary>
		/// <param name="message"> the message </param>
		/// <param name="msgType"> the message type </param>
		public static bool IsMessageType(FixMessage message, byte[] msgType)
		{
			if (msgType.Length == 1)
			{
				return IsValueEquals(message, Tags.MsgType, msgType[0]);
			}

			return IsTagValueEquals(message, Tags.MsgType, msgType);
		}

		/// <summary>
		/// Checks is tag exist and value equals.
		/// </summary>
		/// <param name="message"> the message </param>
		/// <param name="tag">     the tag id </param>
		/// <param name="value">   the tag value </param>
		public static bool IsTagValueEquals(FixMessage message, int tag, byte[] value)
		{
			var tagIndex = message.GetTagIndex(tag);
			if (tagIndex == IndexedStorage.NotFound)
			{
				return false;
			}

			var length = message.GetTagValueLengthAtIndex(tagIndex);
			if (length != value.Length)
			{
				return false;
			}

			var offset = message.GetTagValueOffsetAtIndex(tagIndex);
			var buff = message.GetStorage(tagIndex).GetByteArray(tagIndex);
			for (var i = 0; i < length; i++)
			{
				if (value[i] != buff[offset + i])
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Checks is tag exist and value equals.
		/// </summary>
		/// <param name="message"> the message </param>
		/// <param name="tag">     the tag id </param>
		/// <param name="value">   the tag value </param>
		public static bool IsTagValueEquals(FixMessage message, int tag, string value)
		{
			var tagIndex = message.GetTagIndex(tag);
			if (tagIndex == IndexedStorage.NotFound)
			{
				return false;
			}

			var length = message.GetTagValueLengthAtIndex(tagIndex);
			if (length != value.Length)
			{
				return false;
			}

			var offset = message.GetTagValueOffsetAtIndex(tagIndex);
			var buff = message.GetStorage(tagIndex).GetByteArray(tagIndex);
			for (var i = 0; i < length; i++)
			{
				if (value[i] != (char)buff[offset + i])
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Compares two message, the order of tag is ignored.
		/// </summary>
		/// <param name="message1"> the message </param>
		/// <param name="message2"> the message </param>
		public static bool IsEqualIgnoreOrder(FixMessage message1, FixMessage message2)
		{
			if (message1.Length != message2.Length)
			{
				return false;
			}

			var iterator1 = message1.GetTagValueIterator();
			IEnumerator<TagValue> iterator2;
			bool found;
			TagValue value1;
			while (iterator1.MoveNext())
			{
				value1 = iterator1.Current;
				if (value1.TagId != 9 && value1.TagId != 10)
				{
					found = false;
					iterator2 = message2.GetTagValueIterator();
					while (iterator2.MoveNext())
					{
						if (value1.Equals(iterator2.Current))
						{
							found = true;
							break;
						}
					}

					if (!found)
					{
						return false;
					}
				}
			}

			return true;
		}

		/// <summary>
		/// Compare Field's raw value to passed String constant.
		/// </summary>
		/// <param name="field"> the field </param>
		/// <param name="value"> the String constant </param>
		public static bool IsTagValueEquals(TagValue field, string value)
		{
			if (field == null || field.Length != value.Length)
			{
				return false;
			}

			for (var i = 0; i < field.Length; i++)
			{
				if ((char)field.Buffer[field.Offset + i] != value[i])
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Returns true if message has the 43 tag.
		/// </summary>
		/// <param name="message"> the message </param>
		public static bool IsPosDup(FixMessage message)
		{
			return IsValueEquals(message, Tags.PossDupFlag, (byte)'Y');
		}

		/// <summary>
		/// Returns true if message has the 123 tag and value is equals 'Y'.
		/// </summary>
		/// <param name="message"> the message </param>
		public static bool IsGapFill(FixMessage message)
		{
			return IsValueEquals(message, Tags.GapFillFlag, (byte)'Y');
		}

		/// <summary>
		/// Checks if message type is login.
		/// </summary>
		/// <param name="message"> FIX message </param>
		public static bool IsLogon(FixMessage message)
		{
			return IsValueEquals(message, Tags.MsgType, (byte)'A');
		}

		/// <summary>
		/// Checks if message type is logout.
		/// </summary>
		/// <param name="message"> FIX message </param>
		public static bool IsLogout(FixMessage message)
		{
			return IsValueEquals(message, Tags.MsgType, (byte)'5');
		}

		/// <summary>
		/// Checks if message type is logout.
		/// </summary>
		/// <param name="message"> FIX message </param>
		public static bool IsSeqReset(FixMessage message)
		{
			return IsValueEquals(message, Tags.MsgType, (byte)'4');
		}

		/// <summary>
		/// Methods checks if message has 4 or A type.
		/// </summary>
		/// <param name="message"> the message </param>
		/// <returns> true if it is </returns>
		public static bool IsIgnorableMsg(FixMessage message)
		{
			if (message.GetTagLength(Tags.MsgType) == 1)
			{
				var type = message.GetTagValueAsByte(Tags.MsgType, 0);
				if (type == (byte)'4' && !IsGapFill(message))
				{
					return true;
				}

				if (type == (byte)'2' && IsPosDup(message))
				{
					return true;
				}

				if (type == (byte)'A')
				{
					var seqNumResetTagIndex = message.GetTagIndex(Tags.ResetSeqNumFlag);
					if (seqNumResetTagIndex != IndexedStorage.NotFound)
					{
						var seqNumResetVal = message.GetTagValueAsStringAtIndex(seqNumResetTagIndex);
						if ("Y".Equals(seqNumResetVal) && message.MsgSeqNumber == 1)
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Returns true if message is logon and has 34=1 and 141=Y
		/// </summary>
		/// <param name="message"> the message </param>
		public static bool IsResetLogon(FixMessage message)
		{
			if (IsLogon(message) && IsValueEquals(message, Tags.ResetSeqNumFlag, (byte)'Y') &&
				message.MsgSeqNumber == 1)
			{
				return true;
			}

			return false;
		}

		private static bool IsValueEquals(FixMessage message, int tagId, byte value)
		{
			var tagIndex = message.GetTagIndex(tagId);
			return tagIndex != IndexedStorage.NotFound && message.GetTagValueLengthAtIndex(tagIndex) == 1 &&
					message.GetTagValueAsByteAtIndex(tagIndex, 0) == value;
		}
	}
}
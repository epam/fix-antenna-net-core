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
using System.Text;
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.FixEngine.Storage.Queue;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.Util
{
	/// <summary>
	/// IQueueable element consists of FixMessage and message type.
	/// </summary>
	/// <seealso cref="FixMessageWithTypeFactory"> </seealso>
	internal class FixMessageWithType : IEnqueuedFixMessage, IQueueable
	{
		private bool _originatingFromPool;

		protected internal const byte SeparatorChar = (byte)'\u0001';

		// make it a config option ?
		internal bool AlwaysCloneOnSend = true;

		/// <summary>
		/// Gets list of fields.
		/// </summary>
		public virtual FixMessage FixMessage {get; set; }

		/// <summary>
		/// Gets change type
		/// </summary>
		public ChangesType? ChangesType { get; set; }

		/// <summary>
		/// Creates <c>FixMessageWithType</c>.
		/// </summary>
		public FixMessageWithType()
		{
		}

		public string MessageType { get; set; }

		public virtual FixMessage CloneOnSend(FixMessage list)
		{
			if (list != null)
			{
				if (AlwaysCloneOnSend || list.NeedCloneOnSend)
				{
					list = list.DeepClone(true, false);
				}
			}

			return list;
		}

		public virtual void Init(FixMessage content, string type)
		{
			//TODO: check if type is empty
			ChangesType = null;
			FixMessage = CloneOnSend(content);
			MessageType = type;
		}

		public virtual void Init(FixMessage content, ChangesType? changesType)
		{
			MessageType = null;
			FixMessage = CloneOnSend(content);
			ChangesType = changesType;
		}

		/// <summary>
		/// Serialize instance to byte array.
		/// </summary>
		/// <param name="buffer"> byte buffer </param>
		public virtual void SerializeTo(ByteBuffer buffer)
		{
			SerializeFixMessage(buffer);
		}

		private void SerializeFixMessage(ByteBuffer buffer)
		{
			var length = 0; // length length
			if (FixMessage != null)
			{
				length += FixMessage.RawLength; // content
			}
			var typeArray = EncodeType();
			length += typeArray.Length;
			length += 1; // list flags
			length += 1; // separator;
			if (ChangesType != null)
			{
				length += 2; // changes type + separator
			}

			byte flags = 0;
			flags += (byte)(FixMessage.IsMessageIncomplete ? 2 : 0);
			flags += (byte)(FixMessage.IsPreparedMessage ? 1 : 0);
			buffer.Add(flags);

			buffer.Add(typeArray, 0, typeArray.Length);

			buffer.Add(SeparatorChar);

			if (ChangesType != null)
			{
				buffer.Add(Code(ChangesType.Value));
				buffer.Add(SeparatorChar);
			}

			if (FixMessage != null)
			{
				if (!buffer.IsAvailable(length))
				{
					buffer.IncreaseBuffer(length);
				}

				buffer.Offset = FixMessage.ToByteArrayAndReturnNextPosition(buffer.GetByteArray(), buffer.Offset);
			}
		}

		private byte[] EncodeType()
		{
			if (MessageType == null)
			{
				return Array.Empty<byte>();
			}

			if (MessageType.Length == 0)
			{
				return new byte[] { 0 };
			}

			return MessageType.AsByteArray();
		}

		private byte Code(ChangesType changesType)
		{
			return (byte)((int)changesType + 10);
		}

		private ChangesType Decode(byte val)
		{
			return (ChangesType)Enum.GetValues(typeof(ChangesType)).GetValue(val - 10);
		}

		/// <summary>
		/// DeSerialize instance from byte array.
		/// </summary>
		/// <param name="bytes">  the array of bytes </param>
		/// <param name="offset"> the offset in buffer </param>
		/// <param name="length"> the number of array elements to be read </param>
		public virtual void FromBytes(byte[] bytes, int offset, int length)
		{
			ReadFixMessage(bytes, offset, length);
		}

		private void ReadFixMessage(byte[] bytes, int offset, int length)
		{
			var i = offset;
			var typeBuff = new StringBuilder();

			var flags = bytes[i++];

			for (; bytes[i] != SeparatorChar && i - offset < length; i++)
			{
				typeBuff.Append((char)bytes[i]);
			}

			MessageType = DecodeType(typeBuff);
			if (length < i + 2)
			{
				return;
			}

			if (bytes[i + 2] == 1)
			{
				//new format
				var changesTypeBytes = bytes[i + 1];
				ChangesType = Decode(changesTypeBytes);
				i += 2;
			}

			if (length - i <= 0)
			{
				return;
			}

			i++; //separator
			var msgLen = length - (i - offset);
			var msg = new byte[msgLen];
			Array.Copy(bytes, i, msg, 0, msgLen);
			FixMessage = RawFixUtil.GetFixMessage(msg, true, false);

			FixMessage.IsMessageIncomplete = (flags & 2) > 0;
			FixMessage.IsPreparedMessage = (flags & 1) > 0;
		}

		private string DecodeType(StringBuilder type)
		{
			if (type.Length == 0)
			{
				return null;
			}

			if (type.Length == 1 && type[0] == 0)
			{
				return "";
			}

			return type.ToString(); // restore type
		}

		public override string ToString()
		{
			return " message type='" + MessageType + '\'' + " content: '" + FixMessage + "'";
		}

		public virtual void SetOriginatingFromPool()
		{
			_originatingFromPool = true;
		}

		public virtual bool IsOriginatingFromPool()
		{
			return _originatingFromPool;
		}

		public virtual void Clear()
		{
			FixMessage = null;
			MessageType = null;
			ChangesType = null;
		}

		public virtual void ReleaseInstance()
		{
			FixMessageWithTypePoolFactory.ReturnObj(this);
		}

		/// <summary>
		/// Check message type.
		/// </summary>
		/// <returns> true if message has application level type </returns>
		public virtual bool IsApplicationLevelMessage()
		{
			if (!string.IsNullOrEmpty(MessageType))
			{
				return !RawFixUtil.IsSessionLevelType(MessageType);
			}

			var msg = FixMessage;
			return msg != null && !msg.IsEmpty && !RawFixUtil.IsSessionLevelMessage(msg);
		}

		public virtual FixMessage PrepareMsgForReject()
		{
			var message = FixMessage;
			if (string.IsNullOrEmpty(MessageType))
			{
				return message;
			}

			var tagIndex = message.GetTagIndex(Tags.MsgType);
			if (tagIndex != IndexedStorage.NotFound)
			{
				message.SetAtIndex(tagIndex, MessageType);
			}
			else
			{
				message.AddTagAtIndex(0, Tags.MsgType, MessageType);
			}

			return message;
		}
	}
}
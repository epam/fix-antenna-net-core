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
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message.Format;

namespace Epam.FixAntenna.NetCore.Message
{
	public sealed class TagValue //TODO: rename to Tag? Field?
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(TagValue));
		private readonly bool _readOnly;

		public byte[] Buffer { get; private set; }
		public int Length { get; private set; }
		public int Offset { get; private set; }
		public int TagId { get; set; } //TODO: rename to Tag? Id?
		public bool IsFromPool { get; private set; }

		/// <summary>
		/// Use with caution. Each GetValue do Array.Copy.
		/// </summary>
		public byte[] Value
		{
			get
			{
				if (Length == 0)
					return Array.Empty<byte>(); // null??

				if (Length == Buffer.Length)
					return Buffer;

				var val = new byte[Length];
				Array.Copy(Buffer, Offset, val, 0, Length);
				return val;
			}
			set
			{
				CheckReadOnly();
				Buffer = value;
				Length = value.Length;
				Offset = 0;
			}
		}

		/// <summary>
		/// Gets field size as length of array segment plus length of tag value plus 1 ('=' char).
		/// </summary>
		public int FullSize => GetTagBytesLength() + Length + 1;

		public string StringValue => StringHelper.NewString(Buffer, Offset, Length);

		public long LongValue => FixTypes.ParseInt(Buffer, Offset, Length);

		public double DoubleValue => DoubleFormatter.ParseDouble(Buffer, Offset, Length);

		public TagValue(int tagId, byte[] value, bool readOnly = false) : this(tagId, value, 0, value.Length, readOnly)
		{
		}

		private TagValue(TagValue toClone)
		{
			TagId = toClone.TagId;
			Buffer = toClone.Buffer;
			Offset = toClone.Offset;
			Length = toClone.Length;
		}

		public TagValue(int tagId, double value, int precision) : this(tagId)
		{
			var formatLength = DoubleFormatter.GetFormatLength(value, precision);
			var b = new byte[formatLength];
			DoubleFormatter.Format(value, precision, b, 0);
			Value = b;
		}

		public TagValue(int tagId, long value) : this(tagId, FixTypes.FormatInt(value))
		{
		}

		public TagValue(int tagId, string text) : this(tagId, text.AsByteArray())
		{
		}

		public TagValue(int tagId) : this()
		{
			TagId = tagId;
		}

		public TagValue(bool isFromPool = false)
		{
			IsFromPool = isFromPool;
		}

		public TagValue(int id, byte[] buffer, int offset, int length, bool readOnly = false)
		{
			_readOnly = readOnly;

			TagId = id;
			Buffer = buffer;
			Length = length;
			Offset = offset;
		}

		internal void Reload(int tagId, byte[] data, bool isFromPool = false)
		{
			TagId = tagId;
			Value = data;

			IsFromPool = isFromPool;
		}

		internal void Reload(int tagId, byte[] buffer, int offset, int length, bool isFromPool = false)
		{
			TagId = tagId;
			Buffer = buffer;
			Offset = offset;
			Length = length;

			IsFromPool = isFromPool;
		}

		public bool Equals(TagValue value)
		{
			if (value == null)
			{
				return false;
			}

			if (this == value)
			{
				return true;
			}

			if (TagId != value.TagId)
			{
				return false;
			}

			if (Length != value.Length)
			{
				return false;
			}

			if (Length == value.Length && Offset == value.Offset && Buffer == value.Buffer)
			{
				return true;
			}

			for (var i = 0; i < Length; i++)
			{
				if (Buffer[Offset + i] != value.Buffer[value.Offset + i])
				{
					return false;
				}
			}

			return true;
		}

		public override int GetHashCode()
		{
			var result = GetHashCode(Buffer, Offset, Length);
			result = 31 * result + TagId;
			return result;
		}

		private static int GetHashCode(byte[] a, int offset, int length)
		{
			if (a == null)
			{
				return 1;
			}

			var result = 1;
			var end = offset + length;
			for (var i = offset; i < end; i++)
			{
				result = 31 * result + a[i];
			}

			return result;
		}

		public override bool Equals(object obj)
		{
			return obj is TagValue value && Equals(value);
		}

		public long CalculateChecksum()
		{
			long sum = 0;
			var tag = TagId;
			do
			{
				sum += tag % 10 + '0';
			} while ((tag /= 10) > 0);

			sum += '=';

			var end = Offset + Length;
			for (var i = Offset; i < end; i++)
			{
				var b = Buffer[i];
				sum += (char)b;
			}

			return sum;
		}

		public override string ToString()
		{
			return TagId + "=" + (Buffer == null ? "" : StringHelper.NewString(Buffer, Offset, Length));
		}

		public byte[] ToByteArray()
		{
			var result = new byte[GetSize()];
			ToByteArrayAndReturnNextPosition(result, 0);
			return result;
		}

		private int GetSize()
		{
			return GetTagBytesLength() + Length + 1;
		}

		private int GetTagBytesLength()
		{
			var length = 1;
			var tag = TagId;
			while ((tag /= 10) > 0)
			{
				length++;
			}

			return length;
		}

		private int ToByteArrayAndReturnNextPosition(byte[] buffer, int position)
		{
			var tagBytesLength = GetTagBytesLength();
			var equalsPosition = position += tagBytesLength;
			var tag = TagId;
			do
			{
				buffer[--position] = (byte)(tag % 10 + '0');
			} while ((tag /= 10) > 0);

			buffer[equalsPosition] = (byte)'=';
			Array.Copy(Buffer, Offset, buffer, equalsPosition + 1, Length);
			return equalsPosition + Length + 1;
		}

		private void CheckReadOnly()
		{
			if (_readOnly)
			{
				Log.Warn(
					"Error: TagValue that is obtained from message object is read-only, use FixMessage API to alter value");
				throw new Exception(
					"Error: TagValue that is obtained from message object is read-only, use FixMessage API to alter value");
			}
		}

		public TagValue Clone()
		{
			return new TagValue(this);
		}
	}
}
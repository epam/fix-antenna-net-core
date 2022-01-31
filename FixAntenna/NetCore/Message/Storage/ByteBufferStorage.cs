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
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Message.Format;

namespace Epam.FixAntenna.NetCore.Message.Storage
{
	internal abstract class ByteBufferStorage : MessageStorage
	{
		//TODO: check this methods, especially offset
		public override void SetValue(int tagIndex, string value, int offset, int length)
		{
			var stg = GetByteBuffer(tagIndex);
			stg.Offset = Math.Max(stg.Offset, offset + length);
			var dst = stg.GetByteArray();
			for (var j = 0; j < length; j++)
			{
				dst[offset + j] = (byte)value[j];
			}
		}

		public override void SetValue(int tagIndex, long value, int length)
		{
			var stg = GetByteBuffer(tagIndex);
			var offset = stg.Offset;
			stg.Offset = offset + length;
			var byteArray = stg.GetByteArray();
			FixTypes.FormatInt(value, byteArray, offset);
		}

		public override void SetPaddedValue(int tagIndex, long value, int length)
		{
			var stg = GetByteBuffer(tagIndex);
			var offset = stg.Offset;
			stg.Offset = offset + length;
			var byteArray = stg.GetByteArray();
			FixTypes.FormatIntWithPadding(value, length, byteArray.AsSpan(offset));
		}

		public override int SetValue(int tagIndex, double value, int precision, int length)
		{
			var stg = GetByteBuffer(tagIndex);
			var offset = stg.Offset;
			stg.Offset = offset + length;
			var byteArray = stg.GetByteArray();
			var realLength = DoubleFormatter.Format(value, precision, byteArray, offset);
			return realLength;
		}

		public override void SetValue(int tagIndex, string value, int length)
		{
			var stg = GetByteBuffer(tagIndex);
			var offset = stg.Offset;
			stg.Offset = offset + length;
			var byteArray = stg.GetByteArray();
			for (var j = 0; j < length; j++)
			{
				byteArray[offset + j] = (byte)value[j];
			}
		}

		public override void SetCalendarValue(int index, IFixDateFormatter fixDateFormatter, DateTimeOffset value,
			int length)
		{
			var stg = GetByteBuffer(index);
			var offset = stg.Offset;
			stg.Offset = offset + length;
			var byteArray = stg.GetByteArray();
			fixDateFormatter.Format(value, byteArray, offset);
		}

		public override void SetTimeValue(int index, DateTime value, TimestampPrecision precision, int length)
		{
			var stg = GetByteBuffer(index);
			var offset = stg.Offset;
			stg.Offset = offset + length;
			var byteArray = stg.GetByteArray();
			HighPrecisionDateTimeFormatters.formatTimeOnly(byteArray, value, precision, offset);
		}

		public override void SetTimeValue(int index, DateTimeOffset value, TimestampPrecision precision, int length)
		{
			var stg = GetByteBuffer(index);
			var offset = stg.Offset;
			stg.Offset = offset + length;
			var byteArray = stg.GetByteArray();
			HighPrecisionDateTimeFormatters.formatTZTimeOnly(byteArray, value, precision, offset);
		}

		public override void SetDateTimeValue(int index, DateTime value, TimestampPrecision precision, int length)
		{
			var stg = GetByteBuffer(index);
			var offset = stg.Offset;
			stg.Offset = offset + length;
			var byteArray = stg.GetByteArray();
			HighPrecisionDateTimeFormatters.FormatTimestamp(byteArray, value, precision, offset);
		}

		public override void SetDateTimeValue(int index, DateTimeOffset value, TimestampPrecision precision, int length)
		{
			var stg = GetByteBuffer(index);
			var offset = stg.Offset;
			stg.Offset = offset + length;
			var byteArray = stg.GetByteArray();
			HighPrecisionDateTimeFormatters.FormatTzTimestamp(byteArray, value, precision, offset);
		}

		protected internal abstract ByteBuffer GetByteBuffer(int tagIndex);
	}
}
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
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Message.Format;

namespace Epam.FixAntenna.NetCore.Message
{
	internal abstract class MessageStorage
	{
		public abstract void ClearAll();

		public abstract void Add(int tagIndex, byte[] value, int offset, int length);

		public abstract void SetValue(int tagIndex, string value, int offset, int length);

		public abstract void SetValue(int tagIndex, long value, int length);

		public abstract void SetPaddedValue(int tagIndex, long value, int length);

		public abstract int SetValue(int tagIndex, double value, int precision, int length);

		public abstract void SetValue(int tagIndex, string value, int length);

		public abstract void SetCalendarValue(int index, IFixDateFormatter fixDateFormatter, DateTimeOffset value,
			int length);

		public abstract void SetTimeValue(int index, DateTime value, TimestampPrecision precision, int length);

		public abstract void SetTimeValue(int index, DateTimeOffset value, TimestampPrecision precision, int length);

		public abstract void SetDateTimeValue(int index, DateTime value, TimestampPrecision precision, int length);

		public abstract void SetDateTimeValue(int index, DateTimeOffset value, TimestampPrecision precision,
			int length);

		public abstract byte[] GetByteArray(int index);

		public abstract bool IsEmpty { get; }

		public virtual int GetAsByteArray(int index, IFieldIndexData fieldsIndex, byte[] dest, int destOffset)
		{
			var fieldData = GetByteArray(index);
			var fieldDataOffset = fieldsIndex.GetOffset(index);
			var fieldDataLength = fieldsIndex.GetLength(index);
			Array.Copy(fieldData, fieldDataOffset, dest, destOffset, fieldDataLength);
			return fieldDataLength;
		}

		public virtual byte GetAsByte(int index, IFieldIndexData fieldsIndex, int offset)
		{
			var fieldData = GetByteArray(index);
			if (offset > fieldsIndex.GetLength(index))
			{
				throw new ArgumentException("offset argument is greater than actual value length");
			}

			return fieldData[fieldsIndex.GetOffset(index) + offset];
		}

		public virtual long GetAsLong(int index, IFieldIndexData fieldsIndex)
		{
			var fieldData = GetByteArray(index);
			var fieldDataLength = fieldsIndex.GetLength(index);
			var fieldDataOffset = fieldsIndex.GetOffset(index);
			return FixTypes.ParseInt(fieldData, fieldDataOffset, fieldDataLength);
		}

		public virtual double GetAsDouble(int index, IFieldIndexData fieldsIndex)
		{
			var fieldData = GetByteArray(index);
			var fieldDataLength = fieldsIndex.GetLength(index);
			var fieldDataOffset = fieldsIndex.GetOffset(index);
			return FixTypes.ParseFloat(fieldData, fieldDataOffset, fieldDataLength);
		}

		public virtual bool GetAsBoolean(int index, IFieldIndexData fieldsIndex)
		{
			var fieldData = GetByteArray(index);
			var fieldDataLength = fieldsIndex.GetLength(index);
			var fieldDataOffset = fieldsIndex.GetOffset(index);
			return FixTypes.ParseBoolean(fieldData, fieldDataOffset, fieldDataLength);
		}

		public virtual string GetAsString(int index, IFieldIndexData fieldsIndex)
		{
			var fieldData = GetByteArray(index);
			var fieldDataLength = fieldsIndex.GetLength(index);
			var fieldDataOffset = fieldsIndex.GetOffset(index);
			return Encoding.UTF8.GetString(fieldData, fieldDataOffset, fieldDataLength);
		}

		public virtual void GetAsStringBuffer(int index, IFieldIndexData fieldsIndex, StringBuilder dest)
		{
			var fieldData = GetByteArray(index);
			var fieldDataLength = fieldsIndex.GetLength(index);
			var fieldDataOffset = fieldsIndex.GetOffset(index);

			dest.Length = fieldDataLength;

			for (var j = 0; j < fieldDataLength; j++)
			{
				dest[j] = (char)fieldData[fieldDataOffset + j];
			}
		}

		public virtual void GetAsReusableString(int index, IFieldIndexData fieldsIndex, ReusableString dest)
		{
			var fieldData = GetByteArray(index);
			var fieldDataLength = fieldsIndex.GetLength(index);
			var fieldDataOffset = fieldsIndex.GetOffset(index);

			dest.SetLength(fieldDataLength);

			for (var j = 0; j < fieldDataLength; j++)
			{
				dest.SetCharAt(j, (char)fieldData[fieldDataOffset + j]);
			}
		}

		public virtual void UpdateValue(int index, IFieldIndexData fieldsIndex, ReadOnlySpan<byte> value)
		{
			var fieldData = GetByteArray(index);
			var fieldDataOffset = fieldsIndex.GetOffset(index);
			var fieldDataLength = fieldsIndex.GetLength(index);

			value.CopyTo(fieldData.AsSpan(fieldDataOffset));

			if (fieldDataLength <= value.Length)
				return;

			for (var i = value.Length; i < fieldDataLength; i++)
			{
				fieldData[fieldDataOffset + i] = (byte)' ';
			}
		}

		public virtual void UpdateValue(int index, IFieldIndexData fieldsIndex, long value)
		{
			var fieldData = GetByteArray(index);
			var fieldDataOffset = fieldsIndex.GetOffset(index);
			var oldLength = fieldsIndex.GetLength(index);

			FixTypes.FormatIntWithPadding(value, oldLength, fieldData.AsSpan(fieldDataOffset));
		}

		public virtual void UpdateValue(int index, IFieldIndexData fieldsIndex, double value, int precision)
		{
			var fieldData = GetByteArray(index);
			var fieldDataOffset = fieldsIndex.GetOffset(index);
			var oldLength = fieldsIndex.GetLength(index);

			FixTypes.FormatDoubleWithPadding(value, precision, oldLength, fieldData, fieldDataOffset);
		}

		public virtual void UpdateCalendarValue(int index, IFieldIndexData fieldsIndex,
			IFixDateFormatter fixDateFormatter, DateTimeOffset value)
		{
			var fieldData = GetByteArray(index);
			var fieldDataOffset = fieldsIndex.GetOffset(index);

			fixDateFormatter.Format(value, fieldData, fieldDataOffset);
		}

		public virtual void UpdateTimeValue(int index, IFieldIndexData fieldsIndex, DateTime value,
			TimestampPrecision precision)
		{
			var fieldData = GetByteArray(index);
			var fieldDataOffset = fieldsIndex.GetOffset(index);

			HighPrecisionDateTimeFormatters.formatTimeOnly(fieldData, value, precision, fieldDataOffset);
		}

		public virtual void UpdateTimeValue(int index, IFieldIndexData fieldsIndex, DateTimeOffset value,
			TimestampPrecision precision)
		{
			var fieldData = GetByteArray(index);
			var fieldDataOffset = fieldsIndex.GetOffset(index);

			HighPrecisionDateTimeFormatters.formatTZTimeOnly(fieldData, value, precision, fieldDataOffset);
		}

		public virtual void UpdateDateTimeValue(int index, IFieldIndexData fieldsIndex, DateTime value,
			TimestampPrecision precision)
		{
			var fieldData = GetByteArray(index);
			var fieldDataOffset = fieldsIndex.GetOffset(index);

			HighPrecisionDateTimeFormatters.FormatTimestamp(fieldData, value, precision, fieldDataOffset);
		}

		public virtual void UpdateDateTimeValue(int index, IFieldIndexData fieldsIndex, DateTimeOffset value,
			TimestampPrecision precision)
		{
			var fieldData = GetByteArray(index);
			var fieldDataOffset = fieldsIndex.GetOffset(index);

			HighPrecisionDateTimeFormatters.FormatTzTimestamp(fieldData, value, precision, fieldDataOffset);
		}
	}
}
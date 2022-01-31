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
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message.Format;

namespace Epam.FixAntenna.NetCore.Message
{
	public abstract class ExtendedIndexedStorage : IndexedStorage
	{
		protected internal ExtendedIndexedStorage(int initialSize) : base(initialSize)
		{
		}

		//------------- GET tag methods ----------------//
		public byte[] GetTagValueAsBytes(int tag)
		{
			var index = GetTagIndex(tag);
			if (index != FieldIndex.Notfound)
			{
				var res = new byte[GetTagValueLength(tag)];
				GetTagValueAsBytesAtIndex(index, res, 0);
				return res;
			}

			//throw new FieldNotFoundException("Field (tagId=" + tagId + ") not found");
			return null;
		}

		/// <param name="tag"> </param>
		/// <param name="dest"> </param>
		/// <param name="offset"> </param>
		/// <returns> value length </returns>
		/// <exception cref="FieldNotFoundException"> </exception>
		public int GetTagValueAsBytes(int tag, byte[] dest, int offset)
		{
			var index = GetTagIndex(tag);
			if (index != FieldIndex.Notfound)
			{
				return GetStorage(index).GetAsByteArray(index, FieldIndexData, dest, offset);
			}

			throw new FieldNotFoundException("Field (tagId=" + tag + ") not found");
		}

		public byte[] GetTagValueAsBytes(int tagId, int occurrence)
		{
			var index = GetTagIndex(tagId, occurrence);
			if (index != FieldIndex.Notfound)
			{
				var res = new byte[GetTagValueLength(tagId, occurrence)];
				GetTagValueAsBytesAtIndex(index, res, 0);
				return res;
			}

			//throw new FieldNotFoundException("Field (tagId=" + tagId + ") not found");
			return null;
		}

		public int GetTagValueAsBytes(int tagId, int occurrence, byte[] dest, int offset)
		{
			var index = GetTagIndex(tagId, occurrence);
			if (index != FieldIndex.Notfound)
			{
				return GetStorage(index).GetAsByteArray(index, FieldIndexData, dest, offset);
			}

			throw new FieldNotFoundException("Field (tagId=" + tagId + ") not found");
		}

		public byte GetTagValueAsByte(int tag)
		{
			return GetTagValueAsByte(tag, 0);
		}

		public byte GetTagValueAsByte(int tag, int offset)
		{
			var index = GetTagIndex(tag);
			if (index != FieldIndex.Notfound)
			{
				return GetStorage(index).GetAsByte(index, FieldIndexData, offset);
			}

			throw new FieldNotFoundException("Field (tagId=" + tag + ") not found");
		}

		public byte GetTagValueAsByte(int tagId, int offset, int occurrence)
		{
			var index = GetTagIndex(tagId, occurrence);
			if (index != FieldIndex.Notfound)
			{
				return GetStorage(index).GetAsByte(index, FieldIndexData, offset);
			}

			throw new FieldNotFoundException("Field (tagId=" + tagId + ") not found");
		}

		public byte GetTagValueAsByteAtIndex(int index)
		{
			return GetTagValueAsByteAtIndex(index, 0);
		}

		public bool GetTagValueAsBool(int tag)
		{
			var index = GetTagIndex(tag);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tagId=" + tag + ") not found");
			}

			return GetTagValueAsBoolAtIndex(index);
		}

		public bool GetTagValueAsBool(int tagId, int occurrence)
		{
			var index = GetTagIndex(tagId, occurrence);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tagId=" + tagId + ") not found");
			}

			return GetTagValueAsBoolAtIndex(index);
		}

		public double GetTagValueAsDouble(int tag)
		{
			var index = GetTagIndex(tag);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tagId=" + tag + ") not found");
			}

			return GetTagValueAsDoubleAtIndex(index);
		}

		public double GetTagValueAsDouble(int tagId, int occurrence)
		{
			var index = GetTagIndex(tagId, occurrence);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tagId=" + tagId + ") not found");
			}

			return GetTagValueAsDoubleAtIndex(index);
		}

		public decimal GetTagValueAsDecimal(int tag)
		{
			var index = GetTagIndex(tag);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tagId=" + tag + ") not found");
			}

			return GetTagValueAsDecimalAtIndex(index);
		}

		public decimal GetTagValueAsDecimal(int tag, int occurrence)
		{
			var index = GetTagIndex(tag, occurrence);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tagId=" + tag + ") not found");
			}

			return GetTagValueAsDecimalAtIndex(index);
		}

		public long GetTagValueAsLong(int tag)
		{
			var index = GetTagIndex(tag);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tagId=" + tag + ") not found");
			}

			return GetTagValueAsLongAtIndex(index);
		}

		public long GetTagValueAsLong(int tagId, int occurrence)
		{
			var index = GetTagIndex(tagId, occurrence);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tagId=" + tagId + ") not found");
			}

			return GetTagValueAsLongAtIndex(index);
		}

		public void GetTagValueAsStringBuff(int tag, StringBuilder str)
		{
			var index = GetTagIndex(tag);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tagId=" + tag + ") not found");
			}

			GetTagValueAsStringBuffAtIndex(index, str);
		}

		public void GetTagValueAsStringBuff(int tagId, StringBuilder str, int occurrence)
		{
			var index = GetTagIndex(tagId, occurrence);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tagId=" + tagId + ") not found");
			}

			GetTagValueAsStringBuffAtIndex(index, str);
		}

		/// <summary>
		/// Sets the value in provided ReusableString. The method can be used for obtaining string values without creating a new object (to avoid garbage). </summary>
		/// <param name="tagId"> number of tag for which the value will be obtained. </param>
		/// <exception cref="FieldNotFoundException"> if there is no value for specified tag. </exception>
		internal void GetTagValueAsReusableString(ReusableString reusableString, int tagId)
		{
			var index = GetTagIndex(tagId);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tagId=" + tagId + ") not found");
			}

			GetTagValueAsReusableStringAtIndex(reusableString, index);
		}

		/// <summary>
		/// Sets the value in provided ReusableString. The method can be used for obtaining string values without creating a new object (to avoid garbage). </summary>
		/// <param name="tagId"> number of tag for which the value will be obtained. </param>
		/// <param name="occurrence"> value occurrence of specified tag. Numeration starts with 1. </param>
		/// <exception cref="FieldNotFoundException"> if there is no value for specified tag. </exception>
		internal void GetTagValueAsReusableString(ReusableString reusableString, int tagId, int occurrence)
		{
			var index = GetTagIndex(tagId, occurrence);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tagId=" + tagId + ") not found");
			}

			GetTagValueAsReusableStringAtIndex(reusableString, index);
		}

		public string GetTagValueAsString(int tagId, int occurrence)
		{
			var index = GetTagIndex(tagId, occurrence);
			if (index == FieldIndex.Notfound)
			{
				//TBD! getTagValueAsString(int tagId) return null!
				//throw new FieldNotFoundException("Field (tagId=" + tagId + ") not found");
				return null;
			}

			return GetTagValueAsStringAtIndex(index);
		}

		/// <summary>
		/// Gets string value.
		/// </summary>
		/// <param name="tagId"> the tag id </param>
		/// <returns> string value if tag exist, otherwise null </returns>
		public string GetTagValueAsString(int tagId)
		{
			var index = GetTagIndex(tagId);
			if (index == FieldIndex.Notfound)
			{
				//TBD! getTagValueAsString(int tagId) return null!
				//throw new FieldNotFoundException("Field (tagId=" + tagId + ") not found");
				return null;
			}

			return GetTagValueAsStringAtIndex(index);
		}

		// Parse the value of date (YYYYMMDD) into given Calendar.
		public virtual DateTime getTagValueAsDateOnly(int tag)
		{
			var index = GetTagIndex(tag);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tagId=" + tag + ") not found");
			}

			return getTagValueAsDateOnlyAtIndex(index);
		}

		// Parse the value of date (YYYYMMDD) into given Calendar.
		public virtual DateTime getTagValueAsDateOnly(int tagId, int occurrence)
		{
			var index = GetTagIndex(tagId, occurrence);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tagId=" + tagId + ") not found");
			}

			return getTagValueAsDateOnlyAtIndex(index);
		}

		// Parse the value of date (YYYYMMDD) into given Calendar.
		public virtual DateTime getTagValueAsDateOnlyAtIndex(int index)
		{
			var storage = GetStorage(index);
			var buffer = storage.GetByteArray(index);
			var offset = GetTagValueOffsetAtIndex(index);
			var length = GetTagValueLengthAtIndex(index);
			return FixTypes.ParseDate(buffer, offset, length);
		}

		/// <summary>
		/// Parse the value of time (HH:MM:SS or HH:MM:SS.sss)
		/// </summary>
		/// <param name="tag"></param>
		/// <returns>Time with UTC kind</returns>
		public virtual DateTime getTagValueAsTimeOnly(int tag)
		{
			var index = GetTagIndex(tag);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tagId=" + tag + ") not found");
			}

			return getTagValueAsTimeOnlyAtIndex(index);
		}

		/// <summary>
		/// Parse the value of time (HH:MM:SS or HH:MM:SS[.sss])
		/// </summary>
		/// <param name="tagId"></param>
		/// <param name="occurrence"></param>
		/// <returns>Time with UTC kind</returns>
		public virtual DateTime getTagValueAsTimeOnly(int tagId, int occurrence)
		{
			var index = GetTagIndex(tagId, occurrence);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tagId=" + tagId + ") not found");
			}

			return getTagValueAsTimeOnlyAtIndex(index);
		}

		/// <summary>
		/// Parse the value of time (HH:MM:SS or HH:MM:SS[.sss])
		/// </summary>
		/// <param name="index"></param>
		/// <returns>Time with UTC kind</returns>
		public virtual DateTime getTagValueAsTimeOnlyAtIndex(int index)
		{
			var storage = GetStorage(index);
			var buffer = storage.GetByteArray(index);
			var offset = GetTagValueOffsetAtIndex(index);
			var length = GetTagValueLengthAtIndex(index);
			return FixTypes.parseTimeOnly(buffer, offset, length);
		}

		/// <summary>
		/// Parse the value of timestamp (YYYYMMDD-HH:MM:SS or YYYYMMDD-HH:MM:SS.sss)
		/// </summary>
		/// <param name="tag"></param>
		/// <returns>Time with UTC kind</returns>
		public virtual DateTime GetTagValueAsTimestamp(int tag)
		{
			var index = GetTagIndex(tag);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tagId=" + tag + ") not found");
			}

			return GetTagValueAsTimestampAtIndex(index);
		}

		/// <summary>
		/// Parse the value of timestamp (YYYYMMDD-HH:MM:SS or YYYYMMDD-HH:MM:SS.sss)
		/// </summary>
		/// <param name="tagId"></param>
		/// <param name="occurrence"></param>
		/// <returns>Time with UTC kind</returns>
		public virtual DateTime GetTagValueAsTimestamp(int tagId, int occurrence)
		{
			var index = GetTagIndex(tagId, occurrence);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tagId=" + tagId + ") not found");
			}

			return GetTagValueAsTimestampAtIndex(index);
		}

		/// <summary>
		/// Parse the value of timestamp (YYYYMMDD-HH:MM:SS or YYYYMMDD-HH:MM:SS.sss)
		/// </summary>
		/// <param name="index"></param>
		/// <returns>Time with UTC kind</returns>
		public virtual DateTime GetTagValueAsTimestampAtIndex(int index)
		{
			var storage = GetStorage(index);
			var buffer = storage.GetByteArray(index);
			var offset = GetTagValueOffsetAtIndex(index);
			var length = GetTagValueLengthAtIndex(index);
			return FixTypes.ParseTimestamp(buffer, offset, length);
		}

		// Parse the value of month-year (YYYYMM, YYYYMMDD or YYYYMMWW).
		public virtual DateTime GetTagValueAsMonthYear(int tag)
		{
			var index = GetTagIndex(tag);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tagId=" + tag + ") not found");
			}

			return GetTagValueAsMonthYearAtIndex(index);
		}

		// Parse the value of month-year (YYYYMM, YYYYMMDD or YYYYMMWW).
		public virtual DateTime GetTagValueAsMonthYear(int tagId, int occurrence)
		{
			var index = GetTagIndex(tagId, occurrence);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tagId=" + tagId + ") not found");
			}

			return GetTagValueAsMonthYearAtIndex(index);
		}

		// Parse the value of month-year (YYYYMM, YYYYMMDD or YYYYMMWW).
		public virtual DateTime GetTagValueAsMonthYearAtIndex(int index)
		{
			var storage = GetStorage(index);
			var buffer = storage.GetByteArray(index);
			var offset = GetTagValueOffsetAtIndex(index);
			var length = GetTagValueLengthAtIndex(index);
			return FixTypes.ParseMonthYear44(buffer, offset, length);
		}

		/// <summary>
		/// Parse the value of TZTimeOnly (HH:MM[:SS][.sss][Z | [ + | - hh[:mm]]])
		/// </summary>
		/// <param name="tag"></param>
		/// <returns>Time with offset</returns>
		public virtual DateTimeOffset getTagValueAsTZTimeOnly(int tag)
		{
			var index = GetTagIndex(tag);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tagId=" + tag + ") not found");
			}

			return getTagValueAsTZTimeOnlyAtIndex(index);
		}

		/// <summary>
		/// Parse the value of TZTimeOnly (HH:MM[:SS][.sss][Z | [ + | - hh[:mm]]])
		/// </summary>
		/// <param name="tagId"></param>
		/// <param name="occurrence"></param>
		/// <returns>Time with offset</returns>
		public virtual DateTimeOffset getTagValueAsTZTimeOnly(int tagId, int occurrence)
		{
			var index = GetTagIndex(tagId, occurrence);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tagId=" + tagId + ") not found");
			}

			return getTagValueAsTZTimeOnlyAtIndex(index);
		}

		/// <summary>
		/// Parse the value of TZTimeOnly (HH:MM[:SS][.sss][Z | [ + | - hh[:mm]]])
		/// </summary>
		/// <param name="index"></param>
		/// <returns>Time with offset</returns>
		public virtual DateTimeOffset getTagValueAsTZTimeOnlyAtIndex(int index)
		{
			var storage = GetStorage(index);
			var buffer = storage.GetByteArray(index);
			var offset = GetTagValueOffsetAtIndex(index);
			var length = GetTagValueLengthAtIndex(index);
			return FixTypes.parseTZTimeOnly(buffer, offset, length);
		}

		/// <summary>
		/// Parse the value of TZTimestamp (YYYYMMDD-HH:MM:SS[.sss][Z | [ + | - hh[:mm]]])
		/// </summary>
		/// <param name="tag"></param>
		/// <returns>Time with offset</returns>
		public virtual DateTimeOffset GetTagValueAsTzTimestamp(int tag)
		{
			var index = GetTagIndex(tag);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tagId=" + tag + ") not found");
			}

			return GetTagValueAsTzTimestampAtIndex(index);
		}

		/// <summary>
		/// Parse the value of TZTimestamp (YYYYMMDD-HH:MM:SS[.sss][Z | [ + | - hh[:mm]]])
		/// </summary>
		/// <param name="tagId"></param>
		/// <param name="occurrence"></param>
		/// <returns>Time with offset</returns>
		public virtual DateTimeOffset GetTagValueAsTzTimestamp(int tagId, int occurrence)
		{
			var index = GetTagIndex(tagId, occurrence);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tagId=" + tagId + ") not found");
			}

			return GetTagValueAsTzTimestampAtIndex(index);
		}

		/// <summary>
		/// Parse the value of TZTimestamp (YYYYMMDD-HH:MM:SS[.sss][Z | [ + | - hh[:mm]]])
		/// </summary>
		/// <param name="index"></param>
		/// <returns>Time with offset</returns>
		public virtual DateTimeOffset GetTagValueAsTzTimestampAtIndex(int index)
		{
			var storage = GetStorage(index);
			var buffer = storage.GetByteArray(index);
			var offset = GetTagValueOffsetAtIndex(index);
			var length = GetTagValueLengthAtIndex(index);
			return FixTypes.ParseTzTimestamp(buffer, offset, length);
		}

		public virtual bool IsTagValueEqual(int tagId, byte[] value)
		{
			var index = GetTagIndex(tagId);
			if (index == FieldIndex.Notfound)
			{
				return false;
			}

			var byteArray = GetStorage(index).GetByteArray(index);
			var offset = GetTagValueOffsetAtIndex(index);
			var length = GetTagValueLengthAtIndex(index);
			return EqualsWithOffset(byteArray, offset, length, value);
		}

		public virtual bool HasTagValue(int tagId)
		{
			var index = GetTagIndex(tagId);
			if (index == FieldIndex.Notfound)
			{
				return false;
			}

			var byteArray = GetStorage(index).GetByteArray(index);
			var length = GetTagValueLengthAtIndex(index);
			return byteArray != null && byteArray.Length != 0 && length > 0;
		}

		//----------------- ADD -----------------

		public int AddTag(int tagId, byte[] value)
		{
			return AddTag(tagId, value, 0, value.Length);
		}

		public int AddTagAtIndex(int index, int tagId, byte[] value)
		{
			return AddTagAtIndex(index, tagId, value, 0, value.Length);
		}

		public int AddTag(int tagId, bool value)
		{
			var formattedValue = FixTypes.FormatBoolean(value);
			return AddTag(tagId, formattedValue);
		}

		public int AddTagAtIndex(int index, int tagId, bool value)
		{
			var formattedValue = FixTypes.FormatBoolean(value);
			return AddTagAtIndex(index, tagId, formattedValue);
		}

		public int AddTag(TagValue tagValue)
		{
			return AddTag(tagValue.TagId, tagValue.Buffer, tagValue.Offset, tagValue.Length);
		}

		public int AddTagAtIndex(int index, TagValue tagValue)
		{
			return AddTagAtIndex(index, tagValue.TagId, tagValue.Buffer, tagValue.Offset, tagValue.Length);
		}

		//----------------- SET -----------------

		public void Set(int tagId, byte value)
		{
			//TBD! implement in right way!
			var bytes = new[] { value };
			Set(tagId, bytes);
		}

		public void Set(int tagId, int occurrence, byte value)
		{
			//TBD! implement in right way!
			var bytes = new[] { value };
			Set(tagId, occurrence, bytes);
		}

		public void SetAtIndex(int index, byte value)
		{
			//TBD! implement in right way!
			var bytes = new[] { value };
			SetAtIndex(index, bytes);
		}

		public void Set(int tagId, char value)
		{
			//TBD! implement in right way!
			Set(tagId, (byte)value);
		}

		public void Set(int tagId, int occurrence, char value)
		{
			//TBD! implement in right way!
			Set(tagId, occurrence, (byte)value);
		}

		public void SetAtIndex(int index, char value)
		{
			//TBD! implement in right way!
			SetAtIndex(index, (byte)value);
		}

		public void Set(int tagId, byte[] value)
		{
			UpdateValue(tagId, value, MissingTagHandling.AddIfNotExists);
		}

		public void Set(int tagId, int occurrence, byte[] value)
		{
			UpdateValue(tagId, occurrence, value, MissingTagHandling.AddIfNotExists);
		}

		public void SetAtIndex(int index, byte[] value)
		{
			UpdateValueAtIndex(index, value);
		}

		public void Set(int tagId, byte[] value, int offset, int length)
		{
			UpdateValue(tagId, value, offset, length, MissingTagHandling.AddIfNotExists);
		}

		public void Set(int tagId, int occurrence, byte[] value, int offset, int length)
		{
			UpdateValue(tagId, occurrence, value, offset, length, MissingTagHandling.AddIfNotExists);
		}

		public void SetAtIndex(int index, byte[] value, int offset, int length)
		{
			UpdateValueAtIndex(index, value, offset, length);
		}

		public void Set(int tagId, long value)
		{
			UpdateValue(tagId, value, MissingTagHandling.AddIfNotExists);
		}

		public void Set(int tagId, int occurrence, long value)
		{
			UpdateValue(tagId, occurrence, value, MissingTagHandling.AddIfNotExists);
		}

		public void SetAtIndex(int index, long value)
		{
			UpdateValueAtIndex(index, value);
		}

		public void Set(int tagId, int value)
		{
			UpdateValue(tagId, value, MissingTagHandling.AddIfNotExists);
		}

		public void Set(int tagId, int occurrence, int value)
		{
			UpdateValue(tagId, occurrence, (long)value, MissingTagHandling.AddIfNotExists);
		}

		public void SetAtIndex(int index, int value)
		{
			UpdateValueAtIndex(index, value);
		}

		public void Set(int tagId, double value, int precision)
		{
			UpdateValue(tagId, value, precision, MissingTagHandling.AddIfNotExists);
		}

		public void Set(int tagId, int occurrence, double value, int precision)
		{
			UpdateValue(tagId, occurrence, value, precision, MissingTagHandling.AddIfNotExists);
		}

		public void SetAtIndex(int index, double value, int precision)
		{
			UpdateValueAtIndex(index, value, precision);
		}

		public void Set(int tagId, StringBuilder value)
		{
			UpdateValue(tagId, value.ToString().AsByteArray(), MissingTagHandling.AddIfNotExists);
		}

		public void Set(int tagId, string value)
		{
			UpdateValue(tagId, 0, value, MissingTagHandling.AddIfNotExists);
		}

		public void Set(int tagId, int occurrence, string value)
		{
			UpdateValue(tagId, occurrence, value, MissingTagHandling.AddIfNotExists);
		}

		public void Set(int tagId, bool value)
		{
			UpdateValue(tagId, value, MissingTagHandling.AddIfNotExists);
		}

		public void Set(int tagId, int occurrence, bool value)
		{
			UpdateValue(tagId, occurrence, value, MissingTagHandling.AddIfNotExists);
		}

		public void SetAtIndex(int index, bool value)
		{
			UpdateValueAtIndex(index, value);
		}

		public void Set(TagValue value)
		{
			UpdateValue(value, MissingTagHandling.AddIfNotExists);
		}

		public void Set(int occurrence, TagValue value)
		{
			UpdateValue(value, occurrence, MissingTagHandling.AddIfNotExists);
		}

		public void SetAtIndex(int tagIndex, TagValue value)
		{
			UpdateValueAtIndex(tagIndex, value);
		}

		public void SetAtIndex(int index, string value)
		{
			UpdateValueAtIndex(index, value);
		}

		public void SetCalendarValue(int tagId, DateTimeOffset value, FixDateFormatterFactory.FixDateType type)
		{
			UpdateCalendarValue(tagId, value, type, MissingTagHandling.AddIfNotExists);
		}

		public void SetCalendarValue(int tagId, int occurrence, DateTimeOffset value,
			FixDateFormatterFactory.FixDateType type)
		{
			UpdateCalendarValue(tagId, occurrence, value, type, MissingTagHandling.AddIfNotExists);
		}

		public void SetCalendarValueAtIndex(int index, DateTimeOffset value, FixDateFormatterFactory.FixDateType type)
		{
			UpdateCalendarValueAtIndex(index, value, type);
		}

		//----------------- GET -----------------

		/// <summary>
		/// Gets message type.
		/// </summary>
		/// <value> message type or null if tag 35 not exists. </value>
		public byte[] MsgType
		{
			get
			{
				try
				{
					return GetTagValueAsBytes(Tags.MsgType);
				}
				catch (Exception)
				{
					return null;
				}
			}
		}

		/// <summary>
		/// Gets message fix version.
		/// </summary>
		/// <value> FIXVersion if 9 tg exists </value>
		/// <exception cref="ArgumentException"> if version is invalid. </exception>
		public FixVersion MsgVersion => FixVersion.GetInstanceByMessageVersion(GetTagValueAsString(Tags.BeginString));

		/// <summary>
		/// Gets message sequence number.
		/// </summary>
		/// <value> sequence number if field exist and -1 if doesn't. </value>
		public long MsgSeqNumber
		{
			get
			{
				try
				{
					return GetTagValueAsLong(Tags.MsgSeqNum);
				}
				catch (FieldNotFoundException)
				{
					return -1;
				}
			}
		}

		private static bool EqualsWithOffset(byte[] a, int aOffset, int aLength, byte[] a2)
		{
			if (a == a2)
			{
				return true;
			}

			if (a == null || a2 == null)
			{
				return false;
			}

			if (a2.Length != aLength)
			{
				return false;
			}

			for (var i = 0; i < a2.Length; i++)
			{
				if (a[i + aOffset] != a2[i])
				{
					return false;
				}
			}

			return true;
		}
	}
}
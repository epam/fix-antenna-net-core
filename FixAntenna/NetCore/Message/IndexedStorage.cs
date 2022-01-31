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
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message.Format;
using Epam.FixAntenna.NetCore.Message.Rg;
using Epam.FixAntenna.NetCore.Message.SpecialTags;
using Epam.FixAntenna.NetCore.Message.Storage;

namespace Epam.FixAntenna.NetCore.Message
{
	public class IndexedStorage
	{
		public enum MissingTagHandling
		{
			AddIfNotExists,
			AlwaysAdd,
			DontAddIfNotExists
		}

		public const int NotFound = FieldIndex.Notfound;

		//Must be power of two
		internal const int EnlargeTablesByRatio = 2;
		protected internal const char FieldSeparator = '\u0001'; //TODO: get rid
		private const byte SOH = 0x01;

		/// <summary>
		/// internal independent storage. Used for new values or for making instance standalone
		/// </summary>
		/// <seealso cref="_origBuffer"></seealso>
		private readonly ArenaMessageStorage _arenaStorage;

		private readonly FieldIndex _fieldsIndex;

		/// <summary>
		/// link to the original buffer which is external for messages.
		/// </summary>
		/// <seealso cref="_arenaStorage"> </seealso>
		private readonly ByteArrayMessageStorage _origBuffer;

		private readonly PerFieldMessageStorage _perFieldStorage;

		private readonly HashSet<int> _unserializableTags = new HashSet<int>();

		/// <summary>
		/// Indicate that yet not allocated new space for tag and all values in original place
		/// </summary>
		private bool _continuousBuffer = true;

		private FixVersionContainer _fixVersion;

		private RepeatingGroupStorage _repeatingGroupStorage;

		public IndexedStorage(int initialSize)
		{
			_fieldsIndex = new FieldIndex();
			_perFieldStorage = new PerFieldMessageStorage(initialSize);
			_arenaStorage = new ArenaMessageStorage();
			_origBuffer = new ByteArrayMessageStorage();
			_unserializableTags = new HashSet<int>(Enumerable.Range(0, 32).ToList());
			_unserializableTags.Clear();
		}

		//------------- ADD tag methods ----------------//
		public int AddTagAtIndex(int addAtIndex, int tagId, byte value)
		{
			//TBD! implement in right way!
			var bytes = new[] { value };
			return AddTagAtIndex(addAtIndex, tagId, bytes, 0, 1);
		}

		public int AddTag(int tagId, char value)
		{
			//TBD! implement in right way!
			return AddTag(tagId, new []{(byte)value}, 0, 1);
		}

		public int AddTagAtIndex(int addAtIndex, int tagId, char value)
		{
			//TBD! implement in right way!
			return AddTagAtIndex(addAtIndex, tagId, (byte)value);
		}

		public int AddTag(int tag, byte[] value, int offset, int length)
		{
			return UpdateValue(tag, value, offset, length, MissingTagHandling.AlwaysAdd);
		}

		internal virtual RepeatingGroupStorage GetRepeatingGroupStorage()
		{
			return _repeatingGroupStorage;
		}

		public int AddTagAtIndex(int addAtIndex, int tagId, byte[] value, int offset, int length)
		{
			var fieldCount = ReserveTagAtIndex(addAtIndex, tagId);
			UpdateValueAtIndex(addAtIndex, value, offset, length);
			return fieldCount;
		}

		public int AddTag(int tag, long value)
		{
			return UpdateValue(tag, value, MissingTagHandling.AlwaysAdd);
		}

		public int AddTagAtIndex(int index, int tagId, long value)
		{
			return AddTagAtIndex(index, tagId, value, true);
		}

		public int AddTagAtIndex(int index, int tagId, long value, bool shiftRg)
		{
			var fieldCount = ReserveTagAtIndex(index, tagId, shiftRg);
			UpdateValueAtIndex(index, value);
			return fieldCount;
		}

		protected internal int AddTagAtIndexForRg(int index, int tagId, long value, int rgId)
		{
			var fieldCount = ReserveTagAtIndexForRg(index, tagId, rgId);
			UpdateValueAtIndex(index, value);
			return fieldCount;
		}

		public int AddTag(int tag, double value, int precision)
		{
			return UpdateValue(tag, value, precision, MissingTagHandling.AlwaysAdd);
		}

		public int AddTagAtIndex(int index, int tagId, double value, int precision)
		{
			var fieldCount = ReserveTagAtIndex(index, tagId);
			UpdateValueAtIndex(index, value, precision);
			return fieldCount;
		}

		public int AddTag(int tag, string value)
		{
			return UpdateValue(tag, value.AsByteArray(), MissingTagHandling.AlwaysAdd);
		}

		public int AddTagAtIndex(int index, int tagId, string value)
		{
			var fieldCount = ReserveTagAtIndex(index, tagId);
			UpdateValueAtIndex(index, value.AsByteArray());
			return fieldCount;
		}

		public int AddCalendarTag(int tag, DateTimeOffset value, FixDateFormatterFactory.FixDateType type)
		{
			return UpdateCalendarValue(tag, value, type, MissingTagHandling.AlwaysAdd);
		}

		public int AddCalendarTagAtIndex(int index, int tagId, DateTimeOffset value,
			FixDateFormatterFactory.FixDateType type)
		{
			var fieldCount = ReserveTagAtIndex(index, tagId);
			UpdateCalendarValueAtIndex(index, value, type);
			return fieldCount;
		}

		public int AddTimeTag(int tag, DateTime value, TimestampPrecision precision)
		{
			if (precision == TimestampPrecision.Minute)
			{
				throw new ArgumentException("Invalid value of the desired precision: " + precision);
			}

			return UpdateTimeValue(tag, value, precision, MissingTagHandling.AlwaysAdd);
		}

		public int AddTimeTagAtIndex(int index, int tagId, DateTime value, TimestampPrecision precision)
		{
			var fieldCount = ReserveTagAtIndex(index, tagId);
			if (precision == TimestampPrecision.Minute)
			{
				throw new ArgumentException("Invalid value of the desired precision: " + precision);
			}

			UpdateTimeValueAtIndex(index, value, precision);
			return fieldCount;
		}

		public int AddTimeTag(int tag, DateTimeOffset value, TimestampPrecision precision)
		{
			return UpdateTimeValue(tag, value, precision, MissingTagHandling.AlwaysAdd);
		}

		public int AddTimeTagAtIndex(int index, int tagId, DateTimeOffset value, TimestampPrecision precision)
		{
			var fieldCount = ReserveTagAtIndex(index, tagId);
			UpdateTimeValueAtIndex(index, value, precision);
			return fieldCount;
		}

		public int AddDateTimeTag(int tag, DateTime value, TimestampPrecision precision)
		{
			if (precision == TimestampPrecision.Minute)
			{
				throw new ArgumentException("Invalid value of the desired precision: " + precision);
			}

			return UpdateDateTimeValue(tag, value, precision, MissingTagHandling.AlwaysAdd);
		}

		public int AddDateTimeTagAtIndex(int index, int tagId, DateTime value, TimestampPrecision precision)
		{
			var fieldCount = ReserveTagAtIndex(index, tagId);
			if (precision == TimestampPrecision.Minute)
			{
				throw new ArgumentException("Invalid value of the desired precision: " + precision);
			}

			UpdateDateTimeValueAtIndex(index, value, precision);
			return fieldCount;
		}

		public int AddDateTimeTag(int tag, DateTimeOffset value, TimestampPrecision precision)
		{
			return UpdateDateTimeValue(tag, value, precision, MissingTagHandling.AlwaysAdd);
		}

		public int AddDateTimeTagAtIndex(int index, int tagId, DateTimeOffset value, TimestampPrecision precision)
		{
			var fieldCount = ReserveTagAtIndex(index, tagId);
			UpdateDateTimeValueAtIndex(index, value, precision);
			return fieldCount;
		}

		/// <summary>
		/// Add or update tag with padded long value.
		/// </summary>
		/// <param name="tag">Tag ID to process.</param>
		/// <param name="value">Tag value.</param>
		/// <param name="padding">Tag value padding.</param>
		/// <param name="addIfNotExists"><see cref="MissingTagHandling"/> - defines what to do if the tag is not present in the message.</param>
		/// <returns>Returns index of the processed tag, or -1 if tag not found and <paramref name="addIdNotExist"/> is <see cref="MissingTagHandling.DontAddIfNotExists"/></returns>
		public int SetPaddedLongTag(int tag, long value, int padding, MissingTagHandling addIfNotExists = MissingTagHandling.AlwaysAdd)
		{
			var index = FindOrPrepareToAdd(tag, 0, addIfNotExists);

			if (index == FieldIndex.Notfound)
				return index;

			_fieldsIndex.CheckTagExistsAtIndex(index);

			var oldLen = _fieldsIndex.GetLength(index);
			var newLength = FixTypes.FormatIntLengthWithPadding(value, padding);

			if (CanCopyInPlaceNumber(index, oldLen, newLength))
			{
				var length = newLength > oldLen ? newLength : oldLen;
				_fieldsIndex.UpdateLength(index, length);
				GetStorage(index).UpdateValue(index, _fieldsIndex, value);
			}
			else
			{
				_fieldsIndex.UpdateLength(index, newLength);
				var stg = NewStorageForEntry(index, newLength);
				stg.SetPaddedValue(index, value, newLength);
			}

			return index;
		}

		protected internal virtual int ReserveTagAtIndexForRg(int addAtIndex, int tagId, int rgId)
		{
			EnsureCapacityAndEnlarge();
			_fieldsIndex.AddAtIndex(addAtIndex, tagId, 0);

			var fieldCount = _fieldsIndex.Count;

			_perFieldStorage.Shift(addAtIndex, 1, fieldCount);

			return fieldCount;
		}

		public virtual int ReserveTagAtIndex(int addAtIndex, int tagId)
		{
			return ReserveTagAtIndex(addAtIndex, tagId, true);
		}

		public virtual int ReserveTagAtIndex(int addAtIndex, int tagId, bool shiftRg)
		{
			EnsureCapacityAndEnlarge();
			_fieldsIndex.AddAtIndex(addAtIndex, tagId, 0);

			var fieldCount = _fieldsIndex.Count;

			_perFieldStorage.Shift(addAtIndex, 1, fieldCount);

			if (shiftRg && _repeatingGroupStorage != null && !_repeatingGroupStorage.IsInvalidated)
			{
				_repeatingGroupStorage.Shift(addAtIndex, 1, -1, -1, true);
			}

			return fieldCount;
		}

		//------------- GET tag methods ----------------//
		#region LoadTagValue methods
		public void LoadTagValue(int tagId, TagValue destination)
		{
			var index = GetTagIndex(tagId);

			if (index != FieldIndex.Notfound)
			{
				ReloadTagValue(index, destination);
			}
			else
			{
				throw new FieldNotFoundException("Field (tagId=" + tagId + ") not found");
			}
		}

		public void LoadTagValue(int tagId, TagValue destination, int occurrence)
		{
			var index = GetTagIndex(tagId, occurrence);

			if (index != FieldIndex.Notfound)
			{
				ReloadTagValue(index, destination);
			}
			else
			{
				throw new FieldNotFoundException("Field (tagId=" + tagId + ") not found");
			}
		}

		public void LoadTagValueByIndex(int index, TagValue destination)
		{
			_fieldsIndex.CheckTagExistsAtIndex(index);
			ReloadTagValue(index, destination);
		}
		#endregion

		public byte[] GetTagValueAsBytesAtIndex(int index)
		{
			var res = new byte[GetTagValueLengthAtIndex(index)];
			GetTagValueAsBytesAtIndex(index, res, 0);
			return res;
		}

		public int GetTagValueAsBytesAtIndex(int index, byte[] dest, int offset)
		{
			_fieldsIndex.CheckTagExistsAtIndex(index);
			return GetStorage(index).GetAsByteArray(index, FieldIndexData, dest, offset);
		}

		public byte GetTagValueAsByteAtIndex(int index, int offset)
		{
			_fieldsIndex.CheckTagExistsAtIndex(index);
			return GetStorage(index).GetAsByte(index, FieldIndexData, offset);
		}

		public bool GetTagValueAsBoolAtIndex(int index)
		{
			_fieldsIndex.CheckTagExistsAtIndex(index);
			return GetStorage(index).GetAsBoolean(index, FieldIndexData);
		}

		public double GetTagValueAsDoubleAtIndex(int index)
		{
			_fieldsIndex.CheckTagExistsAtIndex(index);
			return GetStorage(index).GetAsDouble(index, FieldIndexData);
		}

		public decimal GetTagValueAsDecimalAtIndex(int index)
		{
			_fieldsIndex.CheckTagExistsAtIndex(index);
			var decimalInString = GetStorage(index).GetAsString(index, FieldIndexData);
			return decimal.Parse(decimalInString, FixTypes.UsFormatSymbols);
		}

		public long GetTagValueAsLongAtIndex(int index)
		{
			_fieldsIndex.CheckTagExistsAtIndex(index);
			return GetStorage(index).GetAsLong(index, FieldIndexData);
		}

		public void GetTagValueAsStringBuffAtIndex(int index, StringBuilder str)
		{
			_fieldsIndex.CheckTagExistsAtIndex(index);
			GetStorage(index).GetAsStringBuffer(index, FieldIndexData, str);
		}

		internal void GetTagValueAsReusableStringAtIndex(ReusableString reusableString, int index)
		{
			_fieldsIndex.CheckTagExistsAtIndex(index);
			GetStorage(index).GetAsReusableString(index, FieldIndexData, reusableString);
		}

		public virtual string GetTagValueAsStringAtIndex(int index)
		{
			_fieldsIndex.CheckTagExistsAtIndex(index);
			return GetStorage(index).GetAsString(index, FieldIndexData);
		}

		internal void GetTagValueAtIndex(int index, ByteBuffer destination)
		{
			_fieldsIndex.CheckTagExistsAtIndex(index);
			AddTagValueToBuffer(index, destination);
		}

		private void AddTagValueToBuffer(int index, ByteBuffer destination)
		{
			var array = GetStorage(index).GetByteArray(index);
			var offset = _fieldsIndex.GetOffset(index);
			var length = _fieldsIndex.GetLength(index);
			destination.Add(array, offset, length);
		}

		//------------- Update tag methods ----------------//

		public int UpdateValue(int tag, byte[] value, int offset, int length, MissingTagHandling addIfNotExists)
		{
			return UpdateValue(tag, 0, value, offset, length, addIfNotExists);
		}

		public int UpdateValue(int tag, int occurrence, byte[] value, int offset, int length,
			MissingTagHandling addIfNotExists)
		{
			var index = FindOrPrepareToAdd(tag, occurrence, addIfNotExists);

			if (index != FieldIndex.Notfound)
			{
				UpdateValueAtIndex(index, value, offset, length);
			}

			return index;
		}

		public virtual void UpdateValueAtIndex(int tagIndex, byte[] value, int offset, int length)
		{
			_fieldsIndex.CheckTagExistsAtIndex(tagIndex);

			var oldLen = _fieldsIndex.GetLength(tagIndex);

			if (CanCopyInPlace(tagIndex, oldLen, length))
			{
				GetStorage(tagIndex).UpdateValue(tagIndex, _fieldsIndex, value.AsSpan(offset, length));
			}
			else
			{
				_fieldsIndex.UpdateLength(tagIndex, length);
				var stg = NewStorageForEntry(tagIndex, length);
				stg.Add(tagIndex, value, offset, length);
			}
		}

		public int UpdateValue(int tag, byte[] value, MissingTagHandling addIfNotExists)
		{
			return UpdateValue(tag, value, 0, value.Length, addIfNotExists);
		}

		public int UpdateValue(int tagId, int occurrence, byte[] value, MissingTagHandling addIfNotExists)
		{
			return UpdateValue(tagId, occurrence, value, 0, value.Length, addIfNotExists);
		}

		public virtual void UpdateValueAtIndex(int index, byte[] value)
		{
			UpdateValueAtIndex(index, value, 0, value.Length);
		}

		public int UpdateValue(int tag, long value, MissingTagHandling addIfNotExists)
		{
			return UpdateValue(tag, 0, value, addIfNotExists);
		}

		public int UpdateValue(int tag, int occurrence, long value, MissingTagHandling addIfNotExists)
		{
			var index = FindOrPrepareToAdd(tag, occurrence, addIfNotExists);

			if (index != FieldIndex.Notfound)
			{
				UpdateValueAtIndex(index, value);
			}

			return index;
		}

		public virtual void UpdatePaddedValueAtIndex(int index, long value, int padding)
		{
			_fieldsIndex.CheckTagExistsAtIndex(index);

			var oldLen = _fieldsIndex.GetLength(index);
			var newLength = FixTypes.FormatIntLengthWithPadding(value, padding);

			if (CanCopyInPlaceNumber(index, oldLen, newLength))
			{
				var length = newLength > oldLen ? newLength : oldLen;
				_fieldsIndex.UpdateLength(index, length);
				GetStorage(index).UpdateValue(index, _fieldsIndex, value);
			}
			else
			{
				_fieldsIndex.UpdateLength(index, newLength);
				var stg = NewStorageForEntry(index, newLength);
				stg.SetPaddedValue(index, value, newLength);
			}
		}

		public virtual void UpdateValueAtIndex(int index, long value)
		{
			_fieldsIndex.CheckTagExistsAtIndex(index);

			var oldLen = _fieldsIndex.GetLength(index);
			var newLength = FixTypes.FormatIntLength(value);

			if (CanCopyInPlaceNumber(index, oldLen, newLength))
			{
				var length = newLength > oldLen ? newLength : oldLen;
				_fieldsIndex.UpdateLength(index, length);
				GetStorage(index).UpdateValue(index, _fieldsIndex, value);
			}
			else
			{
				_fieldsIndex.UpdateLength(index, newLength);
				var stg = NewStorageForEntry(index, newLength);
				stg.SetValue(index, value, newLength);
			}
		}

		public int UpdateValue(int tag, double value, int precision, MissingTagHandling addIfNotExists)
		{
			return UpdateValue(tag, 0, value, precision, addIfNotExists);
		}

		public int UpdateValue(int tag, int occurrence, double value, int precision,
			MissingTagHandling addIfNotExists)
		{
			var index = FindOrPrepareToAdd(tag, occurrence, addIfNotExists);

			if (index != FieldIndex.Notfound)
			{
				UpdateValueAtIndex(index, value, precision);
			}

			return index;
		}

		public virtual void UpdateValueAtIndex(int index, double value, int precision)
		{
			_fieldsIndex.CheckTagExistsAtIndex(index);

			var oldLen = _fieldsIndex.GetLength(index);
			//TODO: fix getFormatLength to return the right lenth for rounded with truncation doubles (0.099999 -> 0.1) and fix the rest code
			var newLength = DoubleFormatter.GetFormatLength(value, precision);
			if (CanCopyInPlaceNumber(index, oldLen, newLength))
			{
				var length = newLength > oldLen ? newLength : oldLen;
				_fieldsIndex.UpdateLength(index, length);
				GetStorage(index).UpdateValue(index, _fieldsIndex, value, precision);
			}
			else
			{
				var stg = NewStorageForEntry(index, newLength);
				var realLength = stg.SetValue(index, value, precision, newLength);
				_fieldsIndex.UpdateLength(index, realLength);
			}
		}

		public int UpdateValue(int tag, string strBuffer, MissingTagHandling addIfNotExists)
		{
			return UpdateValue(tag, 0, strBuffer, addIfNotExists);
		}

		public int UpdateValue(int tag, int occurrence, string strBuffer, MissingTagHandling addIfNotExists)
		{
			var index = FindOrPrepareToAdd(tag, occurrence, addIfNotExists);

			if (index != FieldIndex.Notfound)
			{
				UpdateValueAtIndex(index, strBuffer.AsByteArray());
			}

			return index;
		}

		public virtual void UpdateValueAtIndex(int tagIndex, string str)
		{
			_fieldsIndex.CheckTagExistsAtIndex(tagIndex);

			var length = str.Length;
			var oldLen = _fieldsIndex.GetLength(tagIndex);

			MessageStorage stg = null;
			if (CanCopyInPlace(tagIndex, oldLen, length))
			{
				length = oldLen;
				stg = GetStorage(tagIndex);
			}
			else if (CanCopyInPlaceWithLengthReducing(tagIndex, oldLen, length))
			{
				_fieldsIndex.UpdateLength(tagIndex, length);
				stg = GetStorage(tagIndex);
			}
			else
			{
				_fieldsIndex.UpdateLength(tagIndex, length);
				stg = NewStorageForEntry(tagIndex, length);
			}

			var dstOffset = _fieldsIndex.GetOffset(tagIndex);

			stg.SetValue(tagIndex, str, dstOffset, length);
		}

		public int UpdateValue(int tag, bool value, MissingTagHandling addIfNotExists)
		{
			return UpdateValue(tag, 0, value, addIfNotExists);
		}

		public int UpdateValue(int tag, int occurrence, bool value, MissingTagHandling addIfNotExists)
		{
			var index = FindOrPrepareToAdd(tag, occurrence, addIfNotExists);

			if (index != FieldIndex.Notfound)
			{
				UpdateValueAtIndex(index, value);
			}

			return index;
		}

		public virtual void UpdateValueAtIndex(int tagIndex, bool value)
		{
			var formattedValue = FixTypes.FormatBoolean(value);

			UpdateValueAtIndex(tagIndex, formattedValue);
		}

		public int UpdateValue(TagValue value, MissingTagHandling addIfNotExists)
		{
			return UpdateValue(value, 0, addIfNotExists);
		}

		public int UpdateValue(TagValue value, int occurrence, MissingTagHandling addIfNotExists)
		{
			var index = FindOrPrepareToAdd(value.TagId, occurrence, addIfNotExists);

			if (index != FieldIndex.Notfound)
			{
				UpdateValueAtIndex(index, value);
			}

			return index;
		}

		public virtual void UpdateValueAtIndex(int tagIndex, TagValue value)
		{
			UpdateValueAtIndex(tagIndex, value.Buffer, value.Offset, value.Length);
		}

		public int UpdateCalendarValue(int tag, DateTimeOffset value, FixDateFormatterFactory.FixDateType type,
			MissingTagHandling addIfNotExists)
		{
			return UpdateCalendarValue(tag, 0, value, type, addIfNotExists);
		}

		public int UpdateCalendarValue(int tag, int occurrence, DateTimeOffset value,
			FixDateFormatterFactory.FixDateType type, MissingTagHandling addIfNotExists)
		{
			var index = FindOrPrepareToAdd(tag, occurrence, addIfNotExists);

			if (index != FieldIndex.Notfound)
			{
				UpdateCalendarValueAtIndex(index, value, type);
			}

			return index;
		}

		public virtual void UpdateCalendarValueAtIndex(int index, DateTimeOffset value,
			FixDateFormatterFactory.FixDateType type)
		{
			_fieldsIndex.CheckTagExistsAtIndex(index);
			var fixDateFormatter = FixDateFormatterFactory.GetFixDateFormatter(type);

			var oldLen = _fieldsIndex.GetLength(index);
			var newLength = fixDateFormatter.GetFormattedStringLength(value);

			if (CanCopyInPlace(index, oldLen, newLength))
			{
				var length = newLength > oldLen ? newLength : oldLen;
				_fieldsIndex.UpdateLength(index, length);
				GetStorage(index).UpdateCalendarValue(index, _fieldsIndex, fixDateFormatter, value);
			}
			else
			{
				_fieldsIndex.UpdateLength(index, newLength);
				var stg = NewStorageForEntry(index, newLength);
				stg.SetCalendarValue(index, fixDateFormatter, value, newLength);
			}
		}

		public int UpdateTimeValue(int tag, DateTime value, TimestampPrecision precision,
			MissingTagHandling addIfNotExists)
		{
			return UpdateTimeValue(tag, 0, value, precision, addIfNotExists);
		}

		public int UpdateTimeValue(int tag, int occurrence, DateTime value, TimestampPrecision precision,
			MissingTagHandling addIfNotExists)
		{
			var index = FindOrPrepareToAdd(tag, occurrence, addIfNotExists);

			if (index != FieldIndex.Notfound)
			{
				UpdateTimeValueAtIndex(index, value, precision);
			}

			return index;
		}

		public virtual void UpdateTimeValueAtIndex(int index, DateTime value, TimestampPrecision precision)
		{
			_fieldsIndex.CheckTagExistsAtIndex(index);

			var oldLen = _fieldsIndex.GetLength(index);
			var newLength = GetHpTimestampBufferLength(false, precision, 0);

			if (CanCopyInPlace(index, oldLen, newLength))
			{
				var length = newLength > oldLen ? newLength : oldLen;
				_fieldsIndex.UpdateLength(index, length);
				GetStorage(index).UpdateTimeValue(index, _fieldsIndex, value, precision);
			}
			else
			{
				_fieldsIndex.UpdateLength(index, newLength);
				var stg = NewStorageForEntry(index, newLength);
				stg.SetTimeValue(index, value, precision, newLength);
			}
		}

		public int UpdateTimeValue(int tag, DateTimeOffset value, TimestampPrecision precision,
			MissingTagHandling addIfNotExists)
		{
			return UpdateTimeValue(tag, 0, value, precision, addIfNotExists);
		}

		public int UpdateTimeValue(int tag, int occurrence, DateTimeOffset value, TimestampPrecision precision,
			MissingTagHandling addIfNotExists)
		{
			var index = FindOrPrepareToAdd(tag, occurrence, addIfNotExists);

			if (index != FieldIndex.Notfound)
			{
				UpdateTimeValueAtIndex(index, value, precision);
			}

			return index;
		}

		public virtual void UpdateTimeValueAtIndex(int index, DateTimeOffset value, TimestampPrecision precision)
		{
			_fieldsIndex.CheckTagExistsAtIndex(index);

			var oldLen = _fieldsIndex.GetLength(index);
			var newLength = GetHpTimestampBufferLength(false, precision, GetTzLength(value.Offset));

			if (CanCopyInPlace(index, oldLen, newLength))
			{
				var length = newLength > oldLen ? newLength : oldLen;
				_fieldsIndex.UpdateLength(index, length);
				GetStorage(index).UpdateTimeValue(index, _fieldsIndex, value, precision);
			}
			else
			{
				_fieldsIndex.UpdateLength(index, newLength);
				var stg = NewStorageForEntry(index, newLength);
				stg.SetTimeValue(index, value, precision, newLength);
			}
		}

		public int UpdateDateTimeValue(int tag, DateTime value, TimestampPrecision precision,
			MissingTagHandling addIfNotExists)
		{
			return UpdateDateTimeValue(tag, 0, value, precision, addIfNotExists);
		}

		public int UpdateDateTimeValue(int tag, int occurrence, DateTime value, TimestampPrecision precision,
			MissingTagHandling addIfNotExists)
		{
			var index = FindOrPrepareToAdd(tag, occurrence, addIfNotExists);

			if (index != FieldIndex.Notfound)
			{
				UpdateDateTimeValueAtIndex(index, value, precision);
			}

			return index;
		}

		public virtual void UpdateDateTimeValueAtIndex(int index, DateTime value, TimestampPrecision precision)
		{
			_fieldsIndex.CheckTagExistsAtIndex(index);

			var oldLen = _fieldsIndex.GetLength(index);
			var newLength = GetHpTimestampBufferLength(true, precision, 0);

			if (CanCopyInPlace(index, oldLen, newLength))
			{
				var length = newLength > oldLen ? newLength : oldLen;
				_fieldsIndex.UpdateLength(index, length);
				GetStorage(index).UpdateDateTimeValue(index, _fieldsIndex, value, precision);
			}
			else
			{
				_fieldsIndex.UpdateLength(index, newLength);
				var stg = NewStorageForEntry(index, newLength);
				stg.SetDateTimeValue(index, value, precision, newLength);
			}
		}

		public int UpdateDateTimeValue(int tag, DateTimeOffset value, TimestampPrecision precision,
			MissingTagHandling addIfNotExists)
		{
			return UpdateDateTimeValue(tag, 0, value, precision, addIfNotExists);
		}

		public int UpdateDateTimeValue(int tag, int occurrence, DateTimeOffset value, TimestampPrecision precision,
			MissingTagHandling addIfNotExists)
		{
			var index = FindOrPrepareToAdd(tag, occurrence, addIfNotExists);

			if (index != FieldIndex.Notfound)
			{
				UpdateDateTimeValueAtIndex(index, value, precision);
			}

			return index;
		}

		public virtual void UpdateDateTimeValueAtIndex(int index, DateTimeOffset value, TimestampPrecision precision)
		{
			_fieldsIndex.CheckTagExistsAtIndex(index);

			var oldLen = _fieldsIndex.GetLength(index);
			var newLength = GetHpTimestampBufferLength(true, precision, GetTzLength(value.Offset));

			if (CanCopyInPlace(index, oldLen, newLength))
			{
				var length = newLength > oldLen ? newLength : oldLen;
				_fieldsIndex.UpdateLength(index, length);
				GetStorage(index).UpdateDateTimeValue(index, _fieldsIndex, value, precision);
			}
			else
			{
				_fieldsIndex.UpdateLength(index, newLength);
				var stg = NewStorageForEntry(index, newLength);
				stg.SetDateTimeValue(index, value, precision, newLength);
			}
		}

		private int GetTzLength(TimeSpan zoneOffset)
		{
			var offsetMinutes = zoneOffset.GetTotalMinutes();
			if (offsetMinutes == 0)
			{
				return 1;
			}

			if (offsetMinutes % 60 == 0)
			{
				return 3;
			}

			return 6;
		}

		private int GetHpTimestampBufferLength(bool date, TimestampPrecision precision, int tz)
		{
			// add bytes for time zone
			var bufLength = tz;
			// add bytes for time
			switch (precision)
			{
				case TimestampPrecision.Minute:
				{
					bufLength += 5;
					break;
				}
				case TimestampPrecision.Second:
				{
					bufLength += 8;
					break;
				}
				case TimestampPrecision.Milli:
				{
					bufLength += 12;
					break;
				}
				case TimestampPrecision.Micro:
				{
					bufLength += 15;
					break;
				}
				case TimestampPrecision.Nano:
				{
					bufLength += 18;
					break;
				}
			}

			// add bytes for date
			if (date)
			{
				bufLength += 9;
			}

			return bufLength;
		}

		//------------- FIND tag methods ----------------//

		public virtual bool IsTagExists(int tag)
		{
			return _fieldsIndex.FindIndexEntryInHashTbl(tag) != FieldIndex.Notfound;
		}

		public virtual bool IsTagExists(int tag, int occurrence)
		{
			var tagIndex = GetTagIndex(tag, occurrence);
			return tagIndex != NotFound;
		}

		public virtual int GetTagIndex(int tag)
		{
			return _fieldsIndex.GetTagIndex(tag);
		}

		public virtual int GetTagIndex(int tag, int occurrence)
		{
			return _fieldsIndex.GetTagOccurrenceIndex(tag, occurrence);
		}

		public int GetTagIndexStartingFrom(int tag, int fromIndex)
		{
			return GetTagIndexBetween(tag, fromIndex, Count);
		}

		public int GetTagIndexBetween(int tag, int startIndex, int endIndex)
		{
			return _fieldsIndex.GetTagIndex(tag, startIndex, endIndex);
		}

		public virtual int GetTagIdAtIndex(int index)
		{
			_fieldsIndex.CheckTagExistsAtIndex(index);

			return _fieldsIndex.GetTag(index);
		}

		public virtual int GetTagValueLengthAtIndex(int index)
		{
			_fieldsIndex.CheckTagExistsAtIndex(index);

			return _fieldsIndex.GetLength(index);
		}

		protected internal virtual int GetTagValueOffsetAtIndex(int index)
		{
			_fieldsIndex.CheckTagExistsAtIndex(index);

			return _fieldsIndex.GetOffset(index);
		}

		public virtual int GetTagValueLength(int tagId)
		{
			var index = GetTagIndex(tagId);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tag=" + tagId + ") not found");
			}

			return _fieldsIndex.GetLength(index);
		}

		public virtual int GetTagValueLength(int tagId, int occurrence)
		{
			var index = GetTagIndex(tagId, occurrence);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tag=" + tagId + ") not found");
			}

			return _fieldsIndex.GetLength(index);
		}

		//------------- Message info methods ----------------//

		public virtual int GetNumOfGroup()
		{
			return 0;
		}

		public virtual int Count => _fieldsIndex.Count;

		public int RawLength
		{
			get
			{
				var length = 0;

				//TBD! optimize
				var msgSize = _fieldsIndex.Count;
				for (var i = 0; i < msgSize; i++)
				{
					var tag = _fieldsIndex.GetTag(i);
					if (IsUnserializableTag(tag))
					{
						continue;
					}

					length += GetTagBytesLength(tag) + _fieldsIndex.GetLength(i) + 1 + 1; // + '=' + 'SOH'
				}

				return length;
			}
		}

		internal virtual IFieldIndexData FieldIndexData => _fieldsIndex;

		protected internal virtual bool IsAllTagsInOneBuffer => _continuousBuffer && _fieldsIndex.CheckTagsStorageType(FieldIndex.FlagOrigbufStorage | FieldIndex.FlagArenaStorage);

		//------------- Message manipulation methods ----------------//

		protected internal virtual void FillSubStorage(int fromIndex, int toIndex, IndexedStorage subStorage)
		{
			var size = _fieldsIndex.Count;
			if (fromIndex < 0 || fromIndex > size || toIndex < 0 || toIndex > size)
			{
				throw new IndexOutOfRangeException("Invalid bounds for new list ([" + fromIndex + ":" + toIndex +
													"]). The current index size is " + size);
			}

			subStorage.SetOriginalBuffer(_origBuffer.Buffer, _origBuffer.Offset,
				_origBuffer.Length);
			//TODO: may be we can use isAllTagsInOneBuffer
			for (var index = fromIndex; index <= toIndex; index++)
			{
				var buffer = GetStorage(index).GetByteArray(index);
				var valueOffset = _fieldsIndex.GetOffset(index);
				var valueLength = _fieldsIndex.GetLength(index);
				var tagId = _fieldsIndex.GetTag(index);

				subStorage.UpdateValue(tagId, buffer, valueOffset, valueLength, MissingTagHandling.AlwaysAdd);
			}
		}

		public virtual void Clear()
		{
			_fieldsIndex.Clear();
			_origBuffer.ClearAll();
			_arenaStorage.ClearAll();
			_perFieldStorage.ClearAll();
			_continuousBuffer = true;
			InvalidateRepeatingGroupIndex();
		}

		/// <summary>
		/// Removes a fix field with specified tag from collection.
		/// The methods removes the first occurrence of the specified tag.
		/// </summary>
		/// <param name="tag"> the fix tag. </param>
		/// <returns> <c>true</c> if the element was removed. </returns>
		public virtual bool RemoveTag(int tag)
		{
			var index = GetTagIndex(tag);
			if (index == FieldIndex.Notfound)
			{
				return false;
			}

			return RemoveTagAtIndex(index);
		}

		public virtual bool RemoveTag(int tag, int occurrence)
		{
			var index = GetTagIndex(tag, occurrence);
			if (index == FieldIndex.Notfound)
			{
				return false;
			}

			return RemoveTagAtIndex(index);
		}

		public virtual bool RemoveTagAtIndex(int tagIndex)
		{
			return RemoveTagAtIndex(tagIndex, true);
		}

		public virtual bool RemoveTagAtIndex(int tagIndex, bool shiftRg)
		{
			_fieldsIndex.CheckTagExistsAtIndex(tagIndex);
			_fieldsIndex.RemoveFromHashtbl(tagIndex);
			RemoveElementFromIndex(tagIndex, shiftRg);
			return true;
		}

		protected internal virtual void DeepCopy(IndexedStorage source)
		{
			_origBuffer.Copy(source._origBuffer);
			_arenaStorage.Copy(source._arenaStorage);
			_perFieldStorage.Copy(source._perFieldStorage);
			_fieldsIndex.DeepCopyFrom(source._fieldsIndex);
			if (source._repeatingGroupStorage != null && !source._repeatingGroupStorage.IsInvalidated)
			{
				_repeatingGroupStorage = source._repeatingGroupStorage.Copy(this);
			}

			_continuousBuffer = source._continuousBuffer;
			_fixVersion = source._fixVersion;
		}

		protected internal virtual void EnsureCapacityAndEnlarge()
		{
			EnsureCapacityAndEnlarge(EnlargeTablesByRatio);
		}

		protected internal virtual bool EnsureCapacityAndEnlarge(int ratio)
		{
			var needToEnlarge = _fieldsIndex.IsNeedToEnlarge();
			if (needToEnlarge)
			{
				_fieldsIndex.EnlargeIndex(ratio);
				_fieldsIndex.EnlargeHastable(ratio);
				_perFieldStorage.Enlarge(ratio);

				OnEnlarge(ratio, _fieldsIndex.GetIndexCapacity());
			}

			return needToEnlarge;
		}

		protected internal virtual void OnEnlarge(int ratio, int newSize)
		{
		}

		protected internal virtual int GetIndexCapacity()
		{
			return _fieldsIndex.GetIndexCapacity();
		}

		public int ToByteArrayAndReturnNextPosition(byte[] dst, int offset, int[] excludedFields)
		{
			var size = Count;
			for (var i = 0; i < size; i++)
			{
				var tagId = _fieldsIndex.GetTag(i);

				if (IsExcludeTag(tagId, excludedFields))
				{
					continue;
				}

				if (IsUnserializableTag(tagId))
				{
					continue;
				}

				// maskedTags is null because all three usages not require masking
				offset = TagToByteArrayAndReturnNextPosition(dst, offset, i, null);
				dst[offset++] = SOH;
			}

			return offset;
		}

		/*protected*/ internal virtual int GenericMessageToByteArrayAndReturnNextPosition(byte[] dst, int offset, IMaskedTags maskedTags)
		{
			//TBD! use iterator
			var size = Count;
			for (var i = 0; i < size; i++)
			{
				offset = TagToByteArrayAndReturnNextPosition(dst, offset, i, maskedTags);
				dst[offset++] = SOH;
			}

			return offset;
		}

		/*protected*/ internal virtual int PreparedToByteArrayAndReturnNextPosition(byte[] dst, int offset, IMaskedTags maskedTags)
		{
			//TBD! add processing of special case: whole message original buffer was copied to arena - no needs to serialize by field-by-field
			//init with offset of the first tag (hope it will be in original buffer)
			var startIndex = 0;

			var size = Count;
			while (startIndex < size && !_fieldsIndex.IsOriginalMessageStorage(startIndex))
			{
				offset = TagToByteArrayAndReturnNextPosition(dst, offset, startIndex, maskedTags);
				dst[offset++] = SOH;
				startIndex++;
			}

			if (startIndex == size)
			{
				//in previous loop was processed all fields
				return offset;
			}

			var stripBroken = false;
			var tagId = _fieldsIndex.GetTag(startIndex);
			var stripStart = _fieldsIndex.GetOffset(startIndex) - 1 - GetTagBytesLength(tagId);

			for (var i = startIndex; i < size; i++)
			{
				if (_fieldsIndex.IsOriginalMessageStorage(i))
				{
					// TBD! throw an exception, write to log ?
					//System.out.println("Prepared message continuity broken at tag: " + tagId);
					//int stretchLen = 0;
					if (stripBroken)
					{
						tagId = _fieldsIndex.GetTag(i);
						stripBroken = false;
						stripStart = _fieldsIndex.GetOffset(i) - 1 - GetTagBytesLength(tagId);
					}
				}
				else
				{
					if (!stripBroken)
					{
						stripBroken = true;

						var lastOrigTagOffset = _fieldsIndex.GetOffset(i - 1);
						var lastOrigTagLength = _fieldsIndex.GetLength(i - 1);
						var stripLen = lastOrigTagOffset + lastOrigTagLength + 1 - stripStart;
						Array.Copy(GetOrigBuffer().Buffer, stripStart, dst, offset, stripLen);
						offset += stripLen;
					}

					// current tag is out of orig buffer and should be processed separately
					offset = TagToByteArrayAndReturnNextPosition(dst, offset, i, maskedTags);
					dst[offset++] = SOH;
				}
			}

			if (!stripBroken)
			{
				var lastTagId = size - 1;
				var lastTagOffset = _fieldsIndex.GetOffset(lastTagId);
				var lastTagLength = _fieldsIndex.GetLength(lastTagId);
				var stretchLen = lastTagOffset + lastTagLength + 1 - stripStart;
				var byteArray = GetOrigBuffer().Buffer;
				Array.Copy(byteArray, stripStart, dst, offset, stretchLen);
				offset += stretchLen;
			}

			return offset;
		}

		internal void GetMessageBuffer(MsgBuf buf)
		{
			var stg = GetOrigBuffer();
			buf.Buffer = stg.Buffer;
			buf.Offset = stg.Offset;
			buf.Length = stg.Length;
		}

		protected internal virtual void SetOriginalBuffer(byte[] buf, int offset, int length)
		{
			_origBuffer.SetBuffer(buf, offset, length);
		}

		internal virtual void ShiftBuffer(byte[] buf, int offset, int length)
		{
			var shift = offset - _origBuffer.Offset;

			_fieldsIndex.ShiftBuffer(shift);
			_origBuffer.SetBuffer(buf, offset, length);
		}

		protected internal virtual void TransferDataToArena()
		{
			if (_origBuffer.IsActive)
			{
				var newOrigBufStartOffset = _arenaStorage.GetOffset();
				var origByteArray = _origBuffer.Buffer;
				var startOffset = _origBuffer.Offset;

				_arenaStorage.Add(origByteArray, startOffset, _origBuffer.Length);
				_origBuffer.ClearAll();

				_fieldsIndex.ShiftBufferAndChangeStorage(newOrigBufStartOffset - startOffset,
					FieldIndex.FlagArenaStorage);
			}
		}

		protected internal virtual int MapTagInOrigStorage(int tag, int offset, int length)
		{
			EnsureCapacityAndEnlarge();
			return _fieldsIndex.Add(tag, offset, length, FieldIndex.FlagOrigbufStorage);
		}

		protected internal virtual int MapPreparedTagInOrigStorage(int tag, int offset, int length)
		{
			EnsureCapacityAndEnlarge();
			return _fieldsIndex.Add(tag, offset, length,
				FieldIndex.FlagOrigbufStorage | FieldIndex.FlagPreparedTag);
		}

		internal virtual void InitRepeatingGroupStorage(FixVersionContainer version, string msgType, bool validation)
		{
			if (_repeatingGroupStorage == null)
			{
				_repeatingGroupStorage = new RepeatingGroupStorage(this, version, msgType, validation);
			}
			else if (_repeatingGroupStorage.IsInvalidated)
			{
				_repeatingGroupStorage.Init(version, msgType, validation);
			}
			else
			{
				_repeatingGroupStorage.ClearRepeatingGroupStorage();
				_repeatingGroupStorage.Init(version, msgType, validation);
			}
		}

		internal virtual void StartCreateRg(int leadingTag, int delimTag, int leadingTagIndex)
		{
			var size = (int)GetTagValueAsLongAtIndex(leadingTagIndex);
			_repeatingGroupStorage.StartCreateRg(leadingTag, leadingTagIndex, size, delimTag);
		}

		internal virtual void StopCreateRg()
		{
			_repeatingGroupStorage.StopCreateRg();
		}

		internal virtual void AddTagToRg(int tag, int tagIndex, int counterTag)
		{
			_repeatingGroupStorage.AddTag(tag, tagIndex, counterTag);
		}

		/// <summary>
		/// Fills passed repeating group instance by data from storage. If group doesn't exist, throws exception. </summary>
		/// <param name="leadingTag"> leading tag for repeating group </param>
		/// <param name="group"> repeating group object for fill </param>
		public virtual void GetRepeatingGroup(int leadingTag, RepeatingGroup group)
		{
			if (_repeatingGroupStorage == null || _repeatingGroupStorage.IsInvalidated)
			{
				InitRepeatingGroupStorage(false);
			}

			_repeatingGroupStorage.GetRepeatingGroup(leadingTag, group);
		}

		/// <summary>
		/// Returns repeating group from storage by leading tag. If group doesn't exist, returns null. </summary>
		/// <param name="leadingTag"> leading tag for repeating group </param>
		/// <returns> instance of RepeatingGroup from RepeatingGroupPool. There is no need to call release for this object. </returns>
		public virtual RepeatingGroup GetRepeatingGroup(int leadingTag)
		{
			if (_repeatingGroupStorage == null || _repeatingGroupStorage.IsInvalidated)
			{
				InitRepeatingGroupStorage(false);
			}

			return _repeatingGroupStorage.GetRepeatingGroup(leadingTag);
		}

		/// <summary>
		/// Returns repeating group from storage by leading tag. If group doesn't exist, add group at end of message. </summary>
		/// <param name="leadingTag"> leading tag for repeating group </param>
		/// <returns> instance of RepeatingGroup from RepeatingGroupPool. There is no need to call release for this object. </returns>
		public virtual RepeatingGroup GetOrAddRepeatingGroup(int leadingTag)
		{
			if (IsRepeatingGroupExists(leadingTag))
			{
				return GetRepeatingGroup(leadingTag);
			}

			return AddRepeatingGroup(leadingTag);
		}

		/// <summary>
		/// Fills passed repeating group instance by data from storage. If group doesn't exist, add group at end of of message </summary>
		/// <param name="leadingTag"> leading tag for repeating group </param>
		/// <param name="group"> repeating group object for fill </param>
		public virtual void GetOrAddRepeatingGroup(int leadingTag, RepeatingGroup group)
		{
			if (IsRepeatingGroupExists(leadingTag))
			{
				GetRepeatingGroup(leadingTag, group);
			}
			else
			{
				AddRepeatingGroup(leadingTag, group);
			}
		}

		/// <summary>
		/// Returns repeating group from storage by leading tag. If group doesn't exist, add group at passed index in message. </summary>
		/// <param name="leadingTag"> leading tag for repeating group </param>
		/// <returns> instance of RepeatingGroup from RepeatingGroupPool. There is no need to call release for this object. </returns>
		public virtual RepeatingGroup GetOrAddRepeatingGroupAtIndex(int leadingTag, int index)
		{
			if (IsRepeatingGroupExists(leadingTag))
			{
				return GetRepeatingGroup(leadingTag);
			}

			return AddRepeatingGroupAtIndex(index, leadingTag);
		}

		/// <summary>
		/// Returns repeating group from storage by leading tag. If group doesn't exist, add group at passed index in message. </summary>
		/// <param name="leadingTag"> leading tag for repeating group </param>
		/// <returns> instance of RepeatingGroup from RepeatingGroupPool. There is no need to call release for this object. </returns>
		public virtual void GetOrAddRepeatingGroupAtIndex(int leadingTag, int index, RepeatingGroup group)
		{
			if (IsRepeatingGroupExists(leadingTag))
			{
				GetRepeatingGroup(leadingTag, group);
			}
			else
			{
				AddRepeatingGroupAtIndex(index, leadingTag, group);
			}
		}

		/// <summary>
		/// Returns repeating group, founded by index of leading tag </summary>
		/// <param name="index"> index of repeating group's leading tag at message </param>
		/// <returns> instance of RepeatingGroup from RepeatingGroupPool. There is no need to call release for this object. </returns>
		public virtual RepeatingGroup GetRepeatingGroupAtIndex(int index)
		{
			if (_repeatingGroupStorage == null || _repeatingGroupStorage.IsInvalidated)
			{
				InitRepeatingGroupStorage(false);
			}

			var leadingTag = GetTagIdAtIndex(index);
			return GetRepeatingGroup(leadingTag);
		}

		/// <summary>
		/// Fills passed repeating group instance by data, founded by index of leading tag </summary>
		/// <param name="index"> leading tag for repeating group </param>
		/// <param name="group"> repeating group object for fill </param>
		public virtual void GetRepeatingGroupAtIndex(int index, RepeatingGroup group)
		{
			var leadingTag = GetTagIdAtIndex(index);
			GetRepeatingGroup(leadingTag, group);
		}

		/// <summary>
		/// Checks is message contains not nested group with passed leading tag.
		/// Note that empty groups, that doesn't appear in the message, also considered existing </summary>
		/// <param name="leadingTag"> leading tag for check </param>
		/// <returns> true if repeating group exists. </returns>
		public virtual bool IsRepeatingGroupExists(int leadingTag)
		{
			if (_repeatingGroupStorage == null || _repeatingGroupStorage.IsInvalidated)
			{
				InitRepeatingGroupStorage(false);
			}

			return _repeatingGroupStorage.IsRepeatingGroupExists(leadingTag);
		}

		/// <summary>
		/// Removes repeating group with specified leading tag </summary>
		/// <param name="leadingTag"> leading tag of group </param>
		/// <returns> true if there is group with specified leading tag </returns>
		public virtual bool RemoveRepeatingGroup(int leadingTag)
		{
			if (IsRepeatingGroupExists(leadingTag))
			{
				GetRepeatingGroup(leadingTag).Remove();
				return true;
			}

			return false;
		}

		/// <summary>
		/// Removes repeating group with leading tag at specified index </summary>
		/// <param name="index"> of repeating group's leading tag </param>
		/// <returns> true if there is group with specified leading tag </returns>
		public virtual bool RemoveRepeatingGroupAtIndex(int index)
		{
			return RemoveRepeatingGroup(GetTagIdAtIndex(index));
		}

		private void InitRepeatingGroupStorage(bool validation)
		{
			if (_repeatingGroupStorage == null || _repeatingGroupStorage.IsInvalidated)
			{
				RawFixUtil.IndexRepeatingGroup(this, validation);
			}
		}

		/// <summary>
		/// Returns all inner arrays, RepeatingGroup and Entry back to pool.
		/// It returns only those RepeatingGroup and Entry, that have been got implicitly from addRepeatingGroup/getRepeatingGroup.
		/// If you got RepeatingGroup or Entry explicit from RepeatingGroupPool, you should take care of call release.
		/// Also this method implicitly calls in <see cref="FixMessage.ReleaseInstance"/>.
		/// </summary>
		public virtual void InvalidateRepeatingGroupIndex()
		{
			if (_repeatingGroupStorage != null)
			{
				_repeatingGroupStorage.ClearRepeatingGroupStorage();
			}
		}

		/// <summary>
		/// Adds group without validation at the end of message. Trailer is ignored. </summary>
		/// <param name="leadingTag"> leading tag for new group </param>
		/// <param name="group"> repeating group object for further work with group </param>
		public virtual void AddRepeatingGroup(int leadingTag, RepeatingGroup group)
		{
			AddRepeatingGroupAtIndex(Count, leadingTag, group);
		}

		/// <summary>
		/// Adds group without validation at the end of message. Trailer is ignored.
		/// Repeating </summary>
		/// <param name="leadingTag"> leading tag for new group </param>
		/// <returns> instance of RepeatingGroup from RepeatingGroupPool. There is no need to call release for this object. </returns>
		public virtual RepeatingGroup AddRepeatingGroup(int leadingTag)
		{
			return AddRepeatingGroupAtIndex(Count, leadingTag);
		}

		/// <summary>
		/// Adds group at the end of message. Trailer is ignored. </summary>
		/// <param name="leadingTag"> leading tag for new group </param>
		/// <param name="validation"> turn on/off validation </param>
		/// <returns> instance of RepeatingGroup from RepeatingGroupPool. There is no need to call release for this object. </returns>
		public virtual RepeatingGroup AddRepeatingGroup(int leadingTag, bool validation)
		{
			return AddRepeatingGroupAtIndex(Count, leadingTag, validation);
		}

		/// <summary>
		/// Adds group without validation at the end of message. Trailer is ignored. </summary>
		/// <param name="leadingTag"> leading tag for new group </param>
		/// <param name="group"> repeating group object for further work with group </param>
		public virtual void AddRepeatingGroup(int leadingTag, bool validation, RepeatingGroup group)
		{
			AddRepeatingGroupAtIndex(Count, leadingTag, validation, group);
		}

		/// <summary>
		/// Adds group at specific place at message </summary>
		/// <param name="index"> index in FIX message. Leading tag will be inserted at this index and all other group tags will follow. </param>
		/// <param name="leadingTag"> leading tag for new group </param>
		/// <param name="validation"> turn on/off validation </param>
		/// <param name="group"> repeating group object for further work with group </param>
		public virtual void AddRepeatingGroupAtIndex(int index, int leadingTag, bool validation, RepeatingGroup group)
		{
			if (_repeatingGroupStorage == null || _repeatingGroupStorage.IsInvalidated)
			{
				InitRepeatingGroupStorage(validation);
			}

			if (validation)
			{
				_repeatingGroupStorage.ValidateLeadingTag(leadingTag);
			}

			_repeatingGroupStorage.ValidateGroupDuplicate(leadingTag);
			_repeatingGroupStorage.AddRepeatingGroup(index, leadingTag, validation, group);
		}

		/// <summary>
		/// Adds group at specific place at message </summary>
		/// <param name="index"> index in FIX message. Leading tag will be inserted at this index and all other group tags will follow. </param>
		/// <param name="leadingTag"> leading tag for new group </param>
		/// <param name="validation"> turn on/off validation </param>
		/// <returns> instance of RepeatingGroup from RepeatingGroupPool. There is no need to call release for this object. </returns>
		public virtual RepeatingGroup AddRepeatingGroupAtIndex(int index, int leadingTag, bool validation)
		{
			if (_repeatingGroupStorage == null || _repeatingGroupStorage.IsInvalidated)
			{
				InitRepeatingGroupStorage(validation);
			}

			var group = RepeatingGroupPool.RepeatingGroup;
			AddRepeatingGroupAtIndex(index, leadingTag, validation, group);
			return group;
		}

		/// <summary>
		/// Adds group at specific place at message </summary>
		/// <param name="index"> index in FIX message. Leading tag will be inserted at this index and all other group tags will follow. </param>
		/// <param name="leadingTag"> leading tag for new group </param>
		/// <returns> instance of RepeatingGroup from RepeatingGroupPool. There is no need to call release for this object. </returns>
		public virtual RepeatingGroup AddRepeatingGroupAtIndex(int index, int leadingTag)
		{
			var group = RepeatingGroupPool.RepeatingGroup;
			AddRepeatingGroupAtIndex(index, leadingTag, false, group);
			return group;
		}

		/// <summary>
		/// Adds group at specific place at message </summary>
		/// <param name="index"> index in FIX message. Leading tag will be inserted at this index and all other group tags will follow. </param>
		/// <param name="leadingTag"> leading tag for new group </param>
		/// <param name="group"> repeating group object for further work with group </param>
		public virtual void AddRepeatingGroupAtIndex(int index, int leadingTag, RepeatingGroup group)
		{
			AddRepeatingGroupAtIndex(index, leadingTag, false, group);
		}

		/// <summary>
		/// Copy repeating group at end of message </summary>
		/// <param name="source"> repeating group for copy </param>
		/// <returns> copied repeating group </returns>
		public virtual RepeatingGroup CopyRepeatingGroup(RepeatingGroup source)
		{
			return CopyRepeatingGroup(source, Count);
		}

		/// <summary>
		/// Copy repeating group at specified index of message </summary>
		/// <param name="source"> repeating group for copy </param>
		/// <param name="index"> index at which the source entry is to be copied </param>
		/// <returns> copied repeating group </returns>
		public virtual RepeatingGroup CopyRepeatingGroup(RepeatingGroup source, int index)
		{
			var dest = AddRepeatingGroupAtIndex(index, source.LeadingTag);
			for (var i = 0; i < source.Count; i++)
			{
				dest.CopyEntry(source.GetEntry(i));
			}

			return dest;
		}

		/// <summary>
		/// Copy repeating group at end of message </summary>
		/// <param name="source"> repeating group for copy </param>
		/// <param name="dest"> entry for hold copied repeating group </param>
		public virtual void CopyRepeatingGroup(RepeatingGroup source, RepeatingGroup dest)
		{
			CopyRepeatingGroup(source, dest, Count);
		}

		/// <summary>
		/// Copy repeating group at specified index of message </summary>
		/// <param name="source"> repeating group for copy </param>
		/// <param name="dest"> entry for hold copied repeating group </param>
		/// <param name="index"> index at which the source entry is to be copied </param>
		public virtual void CopyRepeatingGroup(RepeatingGroup source, RepeatingGroup dest, int index)
		{
			AddRepeatingGroupAtIndex(index, source.LeadingTag, source.Validation, dest);
			for (var i = 0; i < source.Count; i++)
			{
				dest.CopyEntry(source.GetEntry(i));
			}
		}

		public override int GetHashCode()
		{
			return _fieldsIndex.GetHashCode();
		}

		/// <param name="tag"> </param>
		/// <param name="addIfNotExists"> </param>
		/// <returns> index of tag </returns>
		protected internal virtual int FindOrPrepareToAdd(int tag, MissingTagHandling addIfNotExists)
		{
			return FindOrPrepareToAdd(tag, 1, addIfNotExists);
		}

		protected internal virtual int FindOrPrepareToAdd(int tag, int occurrence, MissingTagHandling addIfNotExists)
		{
			var index = FieldIndex.Notfound;

			if (addIfNotExists != MissingTagHandling.AlwaysAdd)
			{
				index = GetTagIndex(tag, occurrence);
			}

			if (addIfNotExists == MissingTagHandling.AlwaysAdd ||
				index == FieldIndex.Notfound && addIfNotExists == MissingTagHandling.AddIfNotExists)
			{
				EnsureCapacityAndEnlarge();

				index = _fieldsIndex.Add(tag, 0, -1, 0);
			}

			return index;
		}

		private MessageStorage NewStorageForEntry(int tagIndex, int length)
		{
			_continuousBuffer = false;
			if (!_arenaStorage.Overflow())
			{
				//put in arena storage
				_fieldsIndex.UpdateStorageData(tagIndex, FieldIndex.FlagArenaStorage, _arenaStorage.GetOffset(),
					length);
				return _arenaStorage;
			}

			//put in perFieldStorage
			_fieldsIndex.UpdateStorageData(tagIndex, FieldIndex.FlagPerfieldStorage, 0, length);
			_perFieldStorage.Init(tagIndex);
			return _perFieldStorage;
		}

		protected internal virtual bool CanCopyInPlaceNumber(int index, int oldLen, int length)
		{
			if (_fieldsIndex.GetStorageType(index) == 0)
			{
				//storage not defined
				return false;
			}

			if (_fieldsIndex.IsOriginalMessageStorage(index))
			{
				if (oldLen >= length)
				{
					return true;
				}

				// debug
				// TBD! throw an exception, write to log ?
				//System.out.println("Prepared message continuity will break due to tag assignment: tag " + fieldsIndex.getTag(index) + ", expected value length " + oldLen + ", assigned length " + length);
				//log.warn("Prepared message continuity will break due to tag assignment: tag " + fieldsIndex.getTag(index) + ", expected value length " + oldLen + ", assigned length " + length);
				return false;
			}

			if (oldLen >= length)
			{
				return true;
			}

			var maxAvail = _fieldsIndex.GetMaxAvailableInPlace(index);
			return maxAvail >= length;
		}

		protected internal virtual bool CanCopyInPlace(int index, int oldLen, int length)
		{
			if (_fieldsIndex.GetStorageType(index) == 0)
			{
				//storage not defined
				return false;
			}

			if (_fieldsIndex.IsOriginalMessageStorage(index))
			{
				if (_fieldsIndex.IsPreparedOriginalStorage(index))
				{
					if (oldLen >= length)
					{
						return true;
					}

					return false;
				}

				if (oldLen == length)
				{
					return true;
				}

				// debug
				// TBD! throw an exception, write to log ?
				//System.out.println("Prepared message continuity will break due to tag assignment: tag " + fieldsIndex.getTag(index) + ", expected value length " + oldLen + ", assigned length " + length);
				//log.warn("Prepared message continuity will break due to tag assignment: tag " + fieldsIndex.getTag(index) + ", expected value length " + oldLen + ", assigned length " + length);
				return false;
			}

			return false;
		}

		protected internal virtual bool CanCopyInPlaceWithLengthReducing(int index, int oldLen, int length)
		{
			var storageType = _fieldsIndex.GetStorageType(index);
			if (storageType != FieldIndex.FlagArenaStorage && storageType != FieldIndex.FlagPerfieldStorage)
			{
				//this is possible only for ByteBuffer based storages
				return false;
			}

			return oldLen >= length;
		}

		/*protected*/ internal virtual MessageStorage GetStorage(int index)
		{
			var storageType = GetStorageType(index);
			switch (storageType)
			{
				case FieldIndex.FlagOrigbufStorage:
					return _origBuffer.IsActive ? (MessageStorage)_origBuffer : _arenaStorage;
				case FieldIndex.FlagArenaStorage:
					return _arenaStorage;
				case FieldIndex.FlagPerfieldStorage:
					return _perFieldStorage;
			}

			throw new Exception("internal error: field storage not set");
		}

		protected internal virtual int GetStorageType(int index)
		{
			_fieldsIndex.CheckTagExistsAtIndex(index);
			return _fieldsIndex.GetStorageType(index);
		}

		private void ReloadTagValue(int index, TagValue tagValue)
		{
			var buffer = GetStorage(index).GetByteArray(index);
			var offset = _fieldsIndex.GetOffset(index);
			var length = _fieldsIndex.GetLength(index);
			var tagId = _fieldsIndex.GetTag(index);
			tagValue.Reload(tagId, buffer, offset, length);
		}

		private void RemoveElementFromIndex(int index, bool shiftRg)
		{
			InvalidatePerFieldStorage(index);
			var fieldCount = _fieldsIndex.Count;
			_fieldsIndex.RemoveElementFromIndex(index);
			if (index < fieldCount - 1)
			{
				_perFieldStorage.ShiftBack(index, 1, fieldCount);
			}

			if (shiftRg && _repeatingGroupStorage != null && !_repeatingGroupStorage.IsInvalidated)
			{
				_repeatingGroupStorage.Shift(index, -1, -1, -1, true);
			}
		}

		private void InvalidatePerFieldStorage(int tagIndex)
		{
			_perFieldStorage.Clear(tagIndex);
		}

		private IContinuousMessageStorage GetOrigBuffer()
		{
			return _origBuffer.IsActive ? (IContinuousMessageStorage)_origBuffer : _arenaStorage;
		}

		private int TagToByteArrayAndReturnNextPosition(byte[] dst, int offset, int tagIndex, IMaskedTags maskedTags)
		{
			var tag = _fieldsIndex.GetTag(tagIndex);
			var maskTag = maskedTags != null && maskedTags.IsTagListed(tag);
			var tagBytesLength = GetTagBytesLength(tag);
			var equalsPosition = offset += tagBytesLength;
			do
			{
				dst[--offset] = (byte)(tag % 10 + '0');
			} while ((tag /= 10) > 0);

			dst[equalsPosition] = (byte)'=';
			var len = _fieldsIndex.GetLength(tagIndex);
			if (len != 0)
			{
				if (maskTag)
				{
					dst.Fill((byte)'*', equalsPosition + 1, len);
				}
				else
				{
					//copy only if there is value
					Array.Copy(GetStorage(tagIndex).GetByteArray(tagIndex), _fieldsIndex.GetOffset(tagIndex), dst, equalsPosition + 1, len);
				}
			}

			return equalsPosition + len + 1;
		}

		private int GetTagBytesLength(int tag)
		{
			var length = 1;
			while ((tag /= 10) > 0)
			{
				length++;
			}

			return length;
		}

		private bool IsExcludeTag(int tag, int[] tags)
		{
			if (tags == null)
			{
				return false;
			}

			foreach (var excludeTag in tags)
			{
				if (excludeTag == tag)
				{
					return true;
				}
			}

			return false;
		}

		internal virtual ByteArrayMessageStorage GetOriginalStorage()
		{
			return _origBuffer;
		}

		internal virtual ArenaMessageStorage GetArenaStorage()
		{
			return _arenaStorage;
		}

		internal virtual PerFieldMessageStorage GetPerFieldStorage()
		{
			return _perFieldStorage;
		}

		public virtual FixVersionContainer GetFixVersion()
		{
			if (_fixVersion != null)
			{
				return _fixVersion;
			}

			var baseVersionIndex = GetTagIndex(Tags.BeginString);
			if (baseVersionIndex != NotFound)
			{
				var baseFixVersion =
					FixVersion.GetInstanceByMessageVersion(GetTagValueAsStringAtIndex(baseVersionIndex));
				if (baseFixVersion == FixVersion.Fixt11)
				{
					var appVersionIndex = GetTagIndex(Tags.ApplVerID);
					if (appVersionIndex != NotFound)
					{
						baseFixVersion =
							FixVersion.GetInstanceByFixtVersion((int)GetTagValueAsLongAtIndex(appVersionIndex));
					}
				}

				return FixVersionContainer.GetFixVersionContainer(baseFixVersion);
			}

			return null;
		}

		internal virtual void SetFixVersion(FixVersionContainer version)
		{
			_fixVersion = version;
		}

		public void MarkUnserializableTag(in int tag)
		{
			_unserializableTags.Add(tag);
		}

		public void ClearUnserializableTags()
		{
			_unserializableTags.Clear();
		}

		private bool IsUnserializableTag(in int tag)
		{
			return _unserializableTags.Contains(tag);
		}
	}
}
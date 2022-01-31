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

namespace Epam.FixAntenna.NetCore.Message
{
	public class HpExtendedIndexedStorage : ExtendedIndexedStorage
	{
		protected internal HpExtendedIndexedStorage(int initialSize) : base(initialSize)
		{
		}

		/// <summary>
		/// Parse the value of time (HH:MM:SS or HH:MM:SS[.sss][sss][sss])
		/// </summary>
		/// <param name="tag"></param>
		/// <returns>Time with UTC kind</returns>
		public override DateTime getTagValueAsTimeOnly(int tag)
		{
			var index = GetTagIndex(tag);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tagId=" + tag + ") not found");
			}

			return getTagValueAsTimeOnlyAtIndex(index);
		}

		/// <summary>
		/// Parse the value of time (HH:MM:SS or HH:MM:SS[.sss][sss][sss])
		/// </summary>
		/// <param name="tagId"></param>
		/// <param name="occurrence"></param>
		/// <returns>Time with UTC kind</returns>
		public override DateTime getTagValueAsTimeOnly(int tagId, int occurrence)
		{
			var index = GetTagIndex(tagId, occurrence);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tagId=" + tagId + ") not found");
			}

			return getTagValueAsTimeOnlyAtIndex(index);
		}

		/// <summary>
		/// Parse the value of time (HH:MM:SS or HH:MM:SS[.sss][sss][sss])
		/// </summary>
		/// <param name="index"></param>
		/// <returns>Time with UTC kind</returns>
		public override DateTime getTagValueAsTimeOnlyAtIndex(int index)
		{
			var storage = GetStorage(index);
			var buffer = storage.GetByteArray(index);
			var offset = GetTagValueOffsetAtIndex(index);
			var length = GetTagValueLengthAtIndex(index);
			return HighPrecisionDateTimeParsers.parseTimeOnly(buffer, offset, length);
		}

		/// <summary>
		/// Parse the value of TZTimeOnly (HH:MM[:SS][.sss][sss][sss][Z | [ + | - hh[:mm]]])
		/// </summary>
		/// <param name="tag"></param>
		/// <returns>Time with offset</returns>
		public override DateTimeOffset getTagValueAsTZTimeOnly(int tag)
		{
			var index = GetTagIndex(tag);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tagId=" + tag + ") not found");
			}

			return getTagValueAsTZTimeOnlyAtIndex(index);
		}

		/// <summary>
		/// Parse the value of TZTimeOnly (HH:MM[:SS][.sss][sss][sss][Z | [ + | - hh[:mm]]])
		/// </summary>
		/// <param name="tagId"></param>
		/// <param name="occurrence"></param>
		/// <returns>Time with offset</returns>
		public override DateTimeOffset getTagValueAsTZTimeOnly(int tagId, int occurrence)
		{
			var index = GetTagIndex(tagId, occurrence);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tagId=" + tagId + ") not found");
			}

			return getTagValueAsTZTimeOnlyAtIndex(index);
		}

		/// <summary>
		/// Parse the value of TZTimeOnly (HH:MM[:SS][.sss][sss][sss][Z | [ + | - hh[:mm]]])
		/// </summary>
		/// <param name="index"></param>
		/// <returns>Time with offset</returns>
		public override DateTimeOffset getTagValueAsTZTimeOnlyAtIndex(int index)
		{
			var storage = GetStorage(index);
			var buffer = storage.GetByteArray(index);
			var offset = GetTagValueOffsetAtIndex(index);
			var length = GetTagValueLengthAtIndex(index);
			return HighPrecisionDateTimeParsers.parseTZTimeOnly(buffer, offset, length);
		}

		/// <summary>
		/// Parse the value of timestamp (YYYYMMDD-HH:MM:SS.sss[sss][sss])
		/// </summary>
		/// <param name="tag"></param>
		/// <returns>Time with UTC kind</returns>
		public override DateTime GetTagValueAsTimestamp(int tag)
		{
			var index = GetTagIndex(tag);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tagId=" + tag + ") not found");
			}

			return GetTagValueAsTimestampAtIndex(index);
		}

		/// <summary>
		/// Parse the value of timestamp (YYYYMMDD-HH:MM:SS.sss[sss][sss])
		/// </summary>
		/// <param name="tagId"></param>
		/// <param name="occurrence"></param>
		/// <returns>Time with UTC kind</returns>
		public override DateTime GetTagValueAsTimestamp(int tagId, int occurrence)
		{
			var index = GetTagIndex(tagId, occurrence);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tagId=" + tagId + ") not found");
			}

			return GetTagValueAsTimestampAtIndex(index);
		}

		/// <summary>
		/// Parse the value of timestamp (YYYYMMDD-HH:MM:SS.sss[sss][sss])
		/// </summary>
		/// <param name="index"></param>
		/// <returns>Time with UTC kind</returns>
		public override DateTime GetTagValueAsTimestampAtIndex(int index)
		{
			var storage = GetStorage(index);
			var buffer = storage.GetByteArray(index);
			var offset = GetTagValueOffsetAtIndex(index);
			var length = GetTagValueLengthAtIndex(index);
			return HighPrecisionDateTimeParsers.ParseTimestamp(buffer, offset, length);
		}

		/// <summary>
		/// Parse the value of TZTimestamp (YYYYMMDD-HH:MM:SS[.sss][sss][sss][Z | [ + | - hh[:mm]]])
		/// </summary>
		/// <param name="tag"></param>
		/// <returns>Time with offset</returns>
		public override DateTimeOffset GetTagValueAsTzTimestamp(int tag)
		{
			var index = GetTagIndex(tag);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tagId=" + tag + ") not found");
			}

			return GetTagValueAsTzTimestampAtIndex(index);
		}

		/// <summary>
		/// Parse the value of TZTimestamp (YYYYMMDD-HH:MM:SS[.sss][sss][sss][Z | [ + | - hh[:mm]]])
		/// </summary>
		/// <param name="tagId"></param>
		/// <param name="occurrence"></param>
		/// <returns>Time with offset</returns>
		public override DateTimeOffset GetTagValueAsTzTimestamp(int tagId, int occurrence)
		{
			var index = GetTagIndex(tagId, occurrence);
			if (index == FieldIndex.Notfound)
			{
				throw new FieldNotFoundException("Field (tagId=" + tagId + ") not found");
			}

			return GetTagValueAsTzTimestampAtIndex(index);
		}

		/// <summary>
		/// Parse the value of TZTimestamp (YYYYMMDD-HH:MM:SS[.sss][sss][sss][Z | [ + | - hh[:mm]]])
		/// </summary>
		/// <param name="index"></param>
		/// <returns>Time with offset</returns>
		public override DateTimeOffset GetTagValueAsTzTimestampAtIndex(int index)
		{
			var storage = GetStorage(index);
			var buffer = storage.GetByteArray(index);
			var offset = GetTagValueOffsetAtIndex(index);
			var length = GetTagValueLengthAtIndex(index);
			return HighPrecisionDateTimeParsers.ParseTzTimestamp(buffer, offset, length);
		}

		public void SetTimeValue(int tagId, DateTime value, TimestampPrecision precision)
		{
			if (precision == TimestampPrecision.Minute)
			{
				throw new ArgumentException("Invalid value of the desired precision: " + precision);
			}

			UpdateTimeValue(tagId, value, precision, MissingTagHandling.AddIfNotExists);
		}

		public void SetTimeValue(int tagId, int occurrence, DateTime value, TimestampPrecision precision)
		{
			if (precision == TimestampPrecision.Minute)
			{
				throw new ArgumentException("Invalid value of the desired precision: " + precision);
			}

			UpdateTimeValue(tagId, occurrence, value, precision, MissingTagHandling.AddIfNotExists);
		}

		public void SetTimeValueAtIndex(int index, DateTime value, TimestampPrecision precision)
		{
			if (precision == TimestampPrecision.Minute)
			{
				throw new ArgumentException("Invalid value of the desired precision: " + precision);
			}

			UpdateTimeValueAtIndex(index, value, precision);
		}

		public void SetTimeValue(int tagId, DateTimeOffset value, TimestampPrecision precision)
		{
			UpdateTimeValue(tagId, value, precision, MissingTagHandling.AddIfNotExists);
		}

		public void SetTimeValue(int tagId, int occurrence, DateTimeOffset value, TimestampPrecision precision)
		{
			UpdateTimeValue(tagId, occurrence, value, precision, MissingTagHandling.AddIfNotExists);
		}

		public void SetTimeValueAtIndex(int index, DateTimeOffset value, TimestampPrecision precision)
		{
			UpdateTimeValueAtIndex(index, value, precision);
		}

		public void SetDateTimeValue(int tagId, DateTime value, TimestampPrecision precision)
		{
			if (precision == TimestampPrecision.Minute)
			{
				throw new ArgumentException("Invalid value of the desired precision: " + precision);
			}

			UpdateDateTimeValue(tagId, value, precision, MissingTagHandling.AddIfNotExists);
		}

		public void SetDateTimeValue(int tagId, int occurrence, DateTime value, TimestampPrecision precision)
		{
			if (precision == TimestampPrecision.Minute)
			{
				throw new ArgumentException("Invalid value of the desired precision: " + precision);
			}

			UpdateDateTimeValue(tagId, occurrence, value, precision, MissingTagHandling.AddIfNotExists);
		}

		public void SetDateTimeValueAtIndex(int index, DateTime value, TimestampPrecision precision)
		{
			if (precision == TimestampPrecision.Minute)
			{
				throw new ArgumentException("Invalid value of the desired precision: " + precision);
			}

			UpdateDateTimeValueAtIndex(index, value, precision);
		}

		public void SetDateTimeValue(int tagId, DateTimeOffset value, TimestampPrecision precision)
		{
			UpdateDateTimeValue(tagId, value, precision, MissingTagHandling.AddIfNotExists);
		}

		public void SetDateTimeValue(int tagId, int occurrence, DateTimeOffset value, TimestampPrecision precision)
		{
			UpdateDateTimeValue(tagId, occurrence, value, precision, MissingTagHandling.AddIfNotExists);
		}

		public void SetDateTimeValueAtIndex(int index, DateTimeOffset value, TimestampPrecision precision)
		{
			UpdateDateTimeValueAtIndex(index, value, precision);
		}
	}
}
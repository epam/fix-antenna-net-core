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

namespace Epam.FixAntenna.NetCore.Message.Format
{
	/// <summary>
	/// HH:MM[Z | [ + | - hh[:mm]]]
	/// </summary>
	internal class TzTimeFormatter : IFixDateFormatter
	{
		public virtual int GetFormattedStringLength(DateTimeOffset calendar)
		{
			var offsetType = GetOffsetType(calendar.Offset);
			switch (offsetType)
			{
				case TzOffsetType.Zero:
					return 6;
				case TzOffsetType.Hour:
					return 8;
				case TzOffsetType.Minutes:
					return 11;
				default:
					return -1;
			}
		}

		public virtual int Format(DateTimeOffset calendar, byte[] buff, int offset)
		{
			var formatOffset = FormatTime(calendar, buff, offset);
			CalendarFormatUtil.FormatTimeZone(calendar, buff, formatOffset);
			return formatOffset;
		}

		private protected virtual TzOffsetType GetOffsetType(TimeSpan tz)
		{
			var tzOffset = tz.TotalMilliseconds / (1000 * 60);

			if (tzOffset == 0)
			{
				return TzOffsetType.Zero;
			}

			if (tzOffset % 60 == 0)
			{
				return TzOffsetType.Hour;
			}

			return TzOffsetType.Minutes;
		}

		protected internal virtual int FormatTime(DateTimeOffset calendar, byte[] buff, int offset)
		{
			var formatOffset = CalendarFormatUtil.FormatHours(calendar.Hour, buff, offset);
			buff[formatOffset++] = (byte)':';
			formatOffset = CalendarFormatUtil.FormatMinutes(calendar.Minute, buff, formatOffset);
			return formatOffset;
		}

		internal enum TzOffsetType
		{
			Zero,
			Hour,
			Minutes
		}
	}
}
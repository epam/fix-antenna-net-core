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
	internal class TzTimestampMillisFormatter : TzTimeMillisFormatter
	{
		public override int GetFormattedStringLength(DateTimeOffset calendar)
		{
			return base.GetFormattedStringLength(calendar) + 9;
		}

		public override int Format(DateTimeOffset calendar, byte[] buff, int offset)
		{
			var formatOffset = FormatDate(calendar, buff, offset);
			buff[formatOffset++] = (byte)'-';
			formatOffset = FormatTime(calendar, buff, formatOffset);
			formatOffset = CalendarFormatUtil.FormatTimeZone(calendar, buff, formatOffset);
			return formatOffset;
		}

		private int FormatDate(DateTimeOffset calendar, byte[] buff, int offset)
		{
			var formatOffset = CalendarFormatUtil.FormatYear(calendar.Year, buff, offset);
			formatOffset = CalendarFormatUtil.FormatMonth(calendar.Month, buff, formatOffset);
			formatOffset = CalendarFormatUtil.FormatDate(calendar.Day, buff, formatOffset);
			return formatOffset;
		}
	}
}
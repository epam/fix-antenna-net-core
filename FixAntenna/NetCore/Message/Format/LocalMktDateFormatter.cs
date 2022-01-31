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
using Epam.FixAntenna.NetCore.Common;

namespace Epam.FixAntenna.NetCore.Message.Format
{
	/// <summary>
	/// Date of Local Market (vs. UTC) in YYYYMMDD format. Valid values: YYYY = 0000-9999, MM = 01-12, DD = 01-31.
	/// </summary>
	internal class LocalMktDateFormatter : IFixDateFormatter
	{
		public virtual int GetFormattedStringLength(DateTimeOffset calendar)
		{
			return 8;
		}

		public virtual int Format(DateTimeOffset calendar, byte[] buff, int offset)
		{
			if (calendar.Offset == DateTimeHelper.LocalZoneOffset)
			{
				return FormatLocalCalendar(calendar, buff, offset);
			}

			var localCalendar = calendar.ToLocalTime();
			return FormatLocalCalendar(localCalendar, buff, offset);
		}

		protected internal virtual int FormatLocalCalendar(DateTimeOffset utcCal, byte[] buff, int offset)
		{
			var formatOffset = CalendarFormatUtil.FormatYear(utcCal.Year, buff, offset);
			formatOffset = CalendarFormatUtil.FormatMonth(utcCal.Month, buff, formatOffset);
			formatOffset = CalendarFormatUtil.FormatDate(utcCal.Day, buff, formatOffset);
			return formatOffset;
		}
	}
}
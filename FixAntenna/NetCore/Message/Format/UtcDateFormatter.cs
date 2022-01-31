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
	/// Formatter for date with pattern YYYYMMDD
	/// </summary>
	internal class UtcDateFormatter : MonthYearFormatter
	{
		public override int GetFormattedStringLength(DateTimeOffset calendar)
		{
			return 8;
		}

		protected internal override int FormatUtcCalendar(DateTimeOffset utcCal, byte[] buff, int offset)
		{
			var formatOffset = base.FormatUtcCalendar(utcCal, buff, offset);
			formatOffset = CalendarFormatUtil.FormatDate(utcCal.Day, buff, formatOffset);
			return formatOffset;
		}
	}
}
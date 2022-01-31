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
	/// HH:MM:SS[Z | [ + | - hh[:mm]]]
	/// </summary>
	internal class TzTimeSecondsFormatter : TzTimeFormatter
	{
		public override int GetFormattedStringLength(DateTimeOffset calendar)
		{
			var offsetType = GetOffsetType(calendar.Offset);
			switch (offsetType)
			{
				case TzOffsetType.Zero:
					return 9;
				case TzOffsetType.Hour:
					return 11;
				case TzOffsetType.Minutes:
					return 14;
				default:
					return -1;
			}
		}

		protected internal override int FormatTime(DateTimeOffset calendar, byte[] buff, int offset)
		{
			var formatOffset = base.FormatTime(calendar, buff, offset);
			buff[formatOffset++] = (byte)':';
			formatOffset = CalendarFormatUtil.FormatSeconds(calendar.Second, buff, formatOffset);
			return formatOffset;
		}
	}
}
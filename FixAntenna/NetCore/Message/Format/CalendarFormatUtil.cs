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
	internal class CalendarFormatUtil
	{
		public static int FormatYear(int year, byte[] buff, int offset)
		{
			buff[offset] = (byte)(year / 1000 % 10 + '0');
			buff[offset + 1] = (byte)(year / 100 % 10 + '0');
			buff[offset + 2] = (byte)(year / 10 % 10 + '0');
			buff[offset + 3] = (byte)(year % 10 + '0');
			return offset + 4;
		}

		public static int FormatMonth(int month, byte[] buff, int offset)
		{
			var val = month;
			buff[offset] = (byte)(val / 10 % 10 + '0');
			buff[offset + 1] = (byte)(val % 10 + '0');
			return offset + 2;
		}

		public static int FormatDate(int day, byte[] buff, int offset)
		{
			buff[offset] = (byte)(day / 10 % 10 + '0');
			buff[offset + 1] = (byte)(day % 10 + '0');
			return offset + 2;
		}

		public static int FormatWeek(int weekOfMonth, byte[] buff, int offset)
		{
			buff[offset] = (byte)'w';
			buff[offset + 1] = (byte)(weekOfMonth + '0');
			return offset + 2;
		}

		public static int FormatHours(int hour, byte[] buff, int offset)
		{
			buff[offset] = (byte)(hour / 10 % 10 + '0');
			buff[offset + 1] = (byte)(hour % 10 + '0');
			return offset + 2;
		}

		public static int FormatMinutes(int minute, byte[] buff, int offset)
		{
			buff[offset] = (byte)(minute / 10 % 10 + '0');
			buff[offset + 1] = (byte)(minute % 10 + '0');
			return offset + 2;
		}

		public static int FormatSeconds(int second, byte[] buff, int offset)
		{
			buff[offset] = (byte)(second / 10 % 10 + '0');
			buff[offset + 1] = (byte)(second % 10 + '0');
			return offset + 2;
		}

		public static int FormatMillis(int millisecond, byte[] buff, int offset)
		{
			buff[offset] = (byte)(millisecond / 100 % 10 + '0');
			buff[offset + 1] = (byte)(millisecond / 10 % 10 + '0');
			buff[offset + 2] = (byte)(millisecond % 10 + '0');
			return offset + 3;
		}

		public static int FormatTimeZone(DateTimeOffset cal, byte[] buff, int offset)
		{
			var tzOffset = cal.Offset.GetTotalMilliseconds() / (1000 * 60);
			if (tzOffset == 0)
			{
				buff[offset] = (byte)'Z';
				return offset + 1;
			}

			if (tzOffset < 0)
			{
				buff[offset] = (byte)'-';
				tzOffset = -tzOffset;
			}
			else
			{
				buff[offset] = (byte)'+';
			}

			if (tzOffset % 60 == 0)
			{
				var val = tzOffset / 60;
				buff[offset + 1] = (byte)(val / 10 % 10 + '0');
				buff[offset + 2] = (byte)(val % 10 + '0');
				return offset + 3;
			}
			else
			{
				var val = tzOffset / 60;
				buff[offset + 1] = (byte)(val / 10 % 10 + '0');
				buff[offset + 2] = (byte)(val % 10 + '0');
				buff[offset + 3] = (byte)':';
				val = tzOffset % 60;
				buff[offset + 4] = (byte)(val / 10 % 10 + '0');
				buff[offset + 5] = (byte)(val % 10 + '0');
				return offset + 6;
			}
		}
	}
}
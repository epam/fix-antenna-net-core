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

namespace Epam.FixAntenna.NetCore.Common
{
	internal class DateTimeBuilder
	{
		private int? _day = 1;

		private int _hour;

		private int _millisecond;
		private int _minute;
		private int? _month = 1;
		private int _nanosecond;
		private int _second;
		private int? _year = 1;

		public DateTimeBuilder()
		{
		}

		public DateTimeBuilder(int year, int month, int day)
		{
			_year = year;
			_month = month;
			_day = day;
		}

		public DateTimeBuilder(int year, int month, int day, int hour, int minute, int second)
		{
			_year = year;
			_month = month;
			_day = day;

			_hour = hour;
			_minute = minute;
			_second = second;
		}

		public DateTimeBuilder(DateTime source)
		{
			_year = source.Year;
			_month = source.Month;
			_day = source.Day;
			_hour = source.Hour;
			_minute = source.Minute;
			_second = source.Second;
			_millisecond = source.Millisecond;
			_nanosecond = source.GetNanosecondsOfSecond();
		}

		public DateTimeBuilder SetYear(int year)
		{
			_year = year;
			return this;
		}

		public DateTimeBuilder SetMonth(int month)
		{
			_month = month;
			return this;
		}

		public DateTimeBuilder SetDay(int day)
		{
			_day = day;
			return this;
		}

		public DateTimeBuilder SetHour(int hour)
		{
			_hour = hour;
			return this;
		}

		public DateTimeBuilder SetMinute(int minute)
		{
			_minute = minute;
			return this;
		}

		public DateTimeBuilder SetSecond(int second)
		{
			_second = second;
			return this;
		}

		public DateTimeBuilder SetMillisecond(int millisecond)
		{
			_millisecond = millisecond;
			return this;
		}

		public DateTimeBuilder SetNanosecond(int nanosecond)
		{
			_nanosecond = nanosecond;
			return this;
		}

		public DateTime Build(DateTimeKind kind)
		{
			var wasLeapSecond = false;
			if (_second == 60)
			{
				_second = 59;
				wasLeapSecond = true;
			}

			if (_year == null )
			{
				_year = 1;
			}

			if (_month == null)
			{
				_month = 1;
			}

			if (_day == null)
			{
				_day = 1;
			}

			var date = new DateTime(_year.Value, _month.Value, _day.Value, _hour, _minute, _second, _millisecond, kind);
			date = date.AddTicks(CountTicks(_nanosecond));
			if (wasLeapSecond)
			{
				date = date.AddSeconds(1);
			}

			return date;
		}

		public DateTimeOffset Build(TimeSpan offset)
		{
			var wasLeapSecond = false;
			if (_second == 60)
			{
				_second = 59;
				wasLeapSecond = true;
			}

			if (_year == null)
			{
				_year = 1;
			}

			if (_month == null)
			{
				_month = 1;
			}

			if (_day == null)
			{
				_day = 1;
			}

			var date = new DateTimeOffset(_year.Value, _month.Value, _day.Value, _hour, _minute, _second, _millisecond, offset);
			date = date.AddTicks(CountTicks(_nanosecond));
			if (wasLeapSecond)
			{
				date = date.AddSeconds(1);
			}

			return date;
		}

		private int CountTicks(int nanoseconds)
		{
			return (int)Math.Round(nanoseconds / (double)DateTimeHelper.NanosecondsPerTick);
		}
	}
}
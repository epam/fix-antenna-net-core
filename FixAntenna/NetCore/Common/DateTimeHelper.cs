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
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Helpers;

namespace Epam.FixAntenna.NetCore.Common
{
	public static class DateTimeHelper
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(DateTimeHelper));

		public const string GeneralFormat = "yyyy-MM-ddTHH:mm:ss";
		public const string MinutesFormat = "yyyy-MM-ddTHH:mm";
		public const string MillisecondsFormat = "yyyy-MM-ddTHH:mm:ss.fff";
		public const string MicrosecondsFormat = "yyyy-MM-ddTHH:mm:ss.ffffff";
		public const string NanosecondsFormat = "yyyy-MM-ddTHH:mm:ss.fffffff00";
		private const string TimeZoneZeroSuffix = "Z";
		private const string TimeZoneHoursSuffix = "%K";
		private const string TimeZoneHoursAndMinutesSuffix = "%K";

		public const int TicksPerMicrosecond = 10;
		public const int NanosecondsPerTick = 100;

		public static readonly TimeSpan UtcOffset = TimeSpan.Zero;
		public static readonly TimeSpan LocalZoneOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);
		private static readonly Calendar GregorianCalendar = new GregorianCalendar();

		private static readonly double SwPerTick = (double)TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

		private static DateTime _baseNow = DateTime.UtcNow;
		private static double _baseTimestamp = Stopwatch.GetTimestamp();
		private static readonly long SetBaseInterval = TimeSpan.FromSeconds(10).Ticks; // sync to system time each ten seconds 
		private static readonly object Lock = new object();

		/// <summary>
		/// Gets number of ticks representing current UTC time. One tick is 100 ns, equal to DateTime.Tick unit.
		/// </summary>
		public static long CurrentTicks
		{
			get
			{
				var endTime = Stopwatch.GetTimestamp();
				var delta = (endTime - _baseTimestamp) * SwPerTick;

				if (delta < SetBaseInterval)
				{
					return _baseNow.Ticks + (long)delta;
				}

				lock (Lock)
				{
					_baseTimestamp = Stopwatch.GetTimestamp();
					_baseNow = DateTime.UtcNow;
				}
				
				return _baseNow.Ticks;
			}
		}

		/// <summary>
		/// Gets number of seconds representing current UTC time.
		/// </summary>
		public static long CurrentSeconds => CurrentTicks / TimeSpan.TicksPerSecond;

		/// <summary>
		/// Gets number of milliseconds representing current UTC time.
		/// </summary>
		public static long CurrentMilliseconds => CurrentTicks / TimeSpan.TicksPerMillisecond;

		/// <summary>
		/// Gets number of microseconds representing current UTC time.
		/// </summary>
		public static long CurrentMicroseconds => CurrentTicks * 1000 / TimeSpan.TicksPerMillisecond;

		/// <summary>
		/// Gets number of nanoseconds representing current UTC time.
		/// </summary>
		public static long CurrentNanoseconds => CurrentTicks * 1000 * 1000 / TimeSpan.TicksPerMillisecond;

		/// <summary>
		/// Get week of month according to the Gregorian calendar
		/// </summary>
		/// <param name="time"></param>
		/// <returns></returns>
		public static int GetWeekOfMonth(this DateTime time)
		{
			var initialDayOfMonth = new DateTime(time.Year, time.Month, 1);
			return time.GetGregorianWeekOfYear() - initialDayOfMonth.GetGregorianWeekOfYear() + 1;
		}

		private static int GetGregorianWeekOfYear(this DateTime time)
		{
			return GregorianCalendar.GetWeekOfYear(time, CalendarWeekRule.FirstDay, DayOfWeek.Sunday);
		}

		public static long GetTotalMinutes(this TimeSpan self)
		{
			return (long)self.TotalMinutes;
		}

		public static long GetTotalMilliseconds(this TimeSpan self)
		{
			return (long)self.TotalMilliseconds;
		}

		public static int GetMicroseconds(this DateTime self)
		{
			return (int)(self.Ticks / (TimeSpan.TicksPerMillisecond / 1000));
		}

		/// <summary>
		/// Get all nanoseconds of last second, including milliseconds and microseconds.
		/// </summary>
		/// <param name="self"></param>
		/// <returns>Return value could be be between 0 and 999999999</returns>
		public static int GetNanosecondsOfSecond(this DateTime self)
		{
			return (int)(self.Ticks % TimeSpan.TicksPerSecond)
					* NanosecondsPerTick;
		}

		/// <summary>
		/// Get all nanoseconds of last second, including milliseconds and microseconds.
		/// </summary>
		/// <param name="self"></param>
		/// <returns>Return value could be be between 0 and 999999999</returns>
		public static int GetNanosecondsOfSecond(this DateTimeOffset self)
		{
			return (int)(self.Ticks % TimeSpan.TicksPerSecond)
					* NanosecondsPerTick;
		}

		/// <summary>
		/// Get nanoseconds of last millisecond, including microseconds.
		/// </summary>
		/// <param name="self"></param>
		/// <returns>Return value could be be between 0 and 999999</returns>
		public static int GetNanosecondsOfMillisecond(this DateTimeOffset self)
		{
			return (int)(self.Ticks % TimeSpan.TicksPerSecond % TimeSpan.TicksPerMillisecond)
					* NanosecondsPerTick;
		}

		/// <summary>
		/// Get nanoseconds of last millisecond, including microseconds.
		/// </summary>
		/// <returns>Return value could be be between 0 and 999999</returns>
		public static DateTime GetDate(int year, int month, int weekOfMonth, DayOfWeek dayOfWeek)
		{
			var days = Enumerable.Range(1, DateTime.DaysInMonth(year, month))
				.Select(x => new DateTime(year, month, x))
				.Where(x => x.DayOfWeek == dayOfWeek)
				.ToList();

			return days.ElementAt(weekOfMonth - 1);
		}

		public static TimeSpan ParseZoneOffset(byte[] buffer, int offset, int count)
		{
			if (count == 0)
			{
				return LocalZoneOffset;
			}

			if (count == 1 && buffer[offset] == (byte)'Z')
			{
				return TimeSpan.Zero;
			}

			if (count == 3)
			{
				return DateTimeOffset.ParseExact(StringHelper.NewString(buffer, offset, 3), "zz",
					CultureInfo.InvariantCulture).Offset;
			}

			if (count == 6)
			{
				return DateTimeOffset.ParseExact(StringHelper.NewString(buffer, offset, 6), "zzz",
					CultureInfo.InvariantCulture).Offset;
			}

			throw new ArgumentException("Invalid time zone value");
		}

		public static string ToUniversalString(this DateTime self, TimestampPrecision precision)
		{
			var offset = self - self.ToUniversalTime();
			return self.ToString(GetFormat(offset, precision));
		}

		public static string ToUniversalString(this DateTimeOffset self, TimestampPrecision precision)
		{
			return self.ToString(GetFormat(self.Offset, precision));
		}

		public static string ToTzUniversalString(this DateTime self, TimestampPrecision precision)
		{
			var offset = self - self.ToUniversalTime();
			return self.ToString(GetFormat(offset, precision, true));
		}

		public static string ToTzUniversalString(this DateTimeOffset self, TimestampPrecision precision)
		{
			return self.ToString(GetFormat(self.Offset, precision, true));
		}

		private static string GetFormat(TimeSpan offset, TimestampPrecision precision = TimestampPrecision.Nano,
			bool withTimeZone = false)
		{
			string format;

			switch (precision)
			{
				case TimestampPrecision.Minute:
					format = MinutesFormat;
					break;
				case TimestampPrecision.Milli:
					format = MillisecondsFormat;
					break;
				case TimestampPrecision.Micro:
					format = MicrosecondsFormat;
					break;
				case TimestampPrecision.Nano:
					format = NanosecondsFormat;
					break;
				default:
					format = GeneralFormat;
					break;
			}

			if (!withTimeZone)
			{
				return format;
			}

			if (offset == TimeSpan.Zero)
			{
				return format + TimeZoneZeroSuffix;
			}

			if (offset.Minutes != 0)
			{
				return format + TimeZoneHoursAndMinutesSuffix;
			}

			return format + TimeZoneHoursSuffix;
		}

		/// <summary>
		/// Converts amount of milliseconds <c>timestamp</c> to string representation using <c>format</c> string.
		/// </summary>
		/// <param name="timestamp">Milliseconds.</param>
		/// <param name="format">Format string.</param>
		/// <returns>Returns string value of DateTime converted from number of milliseconds.</returns>
		public static string ToDateTimeString(this long timestamp, string format)
		{
			return FromMilliseconds(timestamp).ToString(format, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Gets total number of milliseconds from given DateTime value.
		/// </summary>
		/// <param name="self"></param>
		/// <returns></returns>
		public static long TotalMilliseconds(this DateTime self)
		{
			return self.Ticks / TimeSpan.TicksPerMillisecond;
		}

		/// <summary>
		/// Gets total number of milliseconds from given DateTimeOffset value.
		/// </summary>
		/// <param name="self"></param>
		/// <returns></returns>
		public static long TotalMilliseconds(this DateTimeOffset self)
		{
			return self.Ticks / TimeSpan.TicksPerMillisecond;
		}

		/// <summary>
		/// Creates DateTime <see cref="DateTime"/> from amount of milliseconds (UTC).
		/// </summary>
		/// <param name="milliseconds">Milliseconds passed fro</param>
		/// <returns></returns>
		public static DateTime FromMilliseconds(long milliseconds)
		{
			return new DateTime(milliseconds * TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);
		}

		/// <summary>
		/// Parse input <paramref name="timeZoneId">string</paramref> to <see cref="TimeSpan"/> offset from UTC.
		/// Can use system time zone Id or try to parse strings like GMT+03:30.
		/// </summary>
		/// <param name="timeZoneId">Time zone Id.</param>
		/// <param name="offset">Out parameter with parsed offset.</param>
		/// <returns>Returns <see cref="TimeSpan"/> that represents time offset from UTC for given time zone Id.</returns>
		public static bool TryParseTimeZoneOffset(string timeZoneId, out TimeSpan offset)
		{
			var isParsed = TryParseTimeZone(timeZoneId, out var result);
			offset = result.BaseUtcOffset;
			return isParsed;
		}

		internal static bool TryParseTimeZone(string timeZoneId, out TimeZoneInfo timeZoneInfo)
		{
			try
			{
				timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
				return true;
			}
			catch (TimeZoneNotFoundException)
			{
				if (TryParseGmtPattern(timeZoneId, out var offset))
				{
					var customTimeZoneId = $"UTC{(offset < TimeSpan.Zero ? '-' : '+')}{offset:hh\\:mm}";
					timeZoneInfo = TimeZoneInfo.CreateCustomTimeZone(customTimeZoneId, offset, "", "");
					return true;
				}
			}
			catch (Exception)
			{
				Log.Debug($"Cannot parse time zone: {timeZoneId}");
			}

			timeZoneInfo = TimeZoneInfo.Utc;
			return false;
		}

		private static bool TryParseGmtPattern(string pattern, out TimeSpan offset)
		{
			// trying to find and parse GMT pattern: GMT+05:30, GMT-3 or similar
			var m = Regex.Match(pattern, @"(?:GMT|UTC) ?([+|-]\d{1,2}(:?\d{2})?)?");
			if (m.Success)
			{
				// group 1 can be like "+2:30","-03" or empty
				// empty Group 1 means UTC or GMT
				if (m.Groups[1].Length == 0)
				{
					offset = UtcOffset;
					return true;
				}

				// group 2 can be like ":30" or empty
				var matched = m.Groups[2].Length == 0 ? m.Groups[1].Value + ":00" : m.Groups[1].Value;

				if (TimeSpan.TryParse(matched.TrimStart('+'), out offset))
				{
					return true;
				}
			}

			Log.Debug($"Cannot parse time zone: {pattern}");

			offset = UtcOffset;
			return false;
		}
	}
}

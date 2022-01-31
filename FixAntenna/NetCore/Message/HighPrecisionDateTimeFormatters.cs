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
using Epam.FixAntenna.NetCore.Common.Utils;

namespace Epam.FixAntenna.NetCore.Message
{
	/// <summary>
	/// Helper class for FIX date and time formats.
	///
	/// Provides ability for formatting different types of date/time values
	/// to the buffer of bytes. It is also possible to work with high precision
	/// values: micro- and nanoseconds.
	///
	/// </summary>
	internal class HighPrecisionDateTimeFormatters
	{
		/// <summary>
		/// Formats the value of <c>time</c> to the <c>UTCTimeOnly</c> format.
		/// <p/>
		/// The format for <c>UTCTimeOnly</c> is HH:MM:SS[.sss][sss][sss].
		/// </summary>
		/// <param name="buffer">      the buffer of bytes to keep the <c>UTCTimeOnly</c> format </param>
		/// <param name="time">        the given time value </param>
		/// <param name="precision">   the desired time precision
		/// </param>
		/// <exception cref="ArgumentException"> </exception>
		public static void formatTimeOnly(byte[] buffer, DateTime time, TimestampPrecision precision)
		{
			formatTimeOnly(buffer, time, precision, 0);
		}

		/// <summary>
		/// Formats the value of <c>time</c> to the <c>UTCTimeOnly</c> format.
		/// <p/>
		/// The format for <c>UTCTimeOnly</c> is HH:MM:SS[.sss][sss][sss].
		/// </summary>
		/// <param name="buffer">      the buffer of bytes to keep the <c>UTCTimeOnly</c> format </param>
		/// <param name="time">        the given time value </param>
		/// <param name="precision">   the desired time precision </param>
		/// <param name="offset">      the offset
		/// </param>
		/// <exception cref="ArgumentException"> </exception>
		public static void formatTimeOnly(byte[] buffer, DateTime time, TimestampPrecision precision, int offset)
		{
			offset = FormatTimeWithoutFractions(time.Hour, time.Minute, time.Second, buffer, offset);

			switch (precision)
			{
				case TimestampPrecision.Second:
					break;
				case TimestampPrecision.Milli:
				case TimestampPrecision.Micro:
				case TimestampPrecision.Nano:
					buffer[offset++] = (byte)'.';
					offset = FormatSecondFractions(time.GetNanosecondsOfSecond(), buffer, offset, precision);
					break;
				default:
					throw new ArgumentException(
						"Only 'Second', 'Milli', 'Micro' and 'Nano' values of timestamp precision are available");
			}
		}

		/// <summary>
		/// Formats the value of <c>time</c> to the <c>TZTimeOnly</c> format.
		/// <p/>
		/// The format for <c>TZTimeOnly</c> is HH:MM[:SS][.sss][sss][sss][Z | [ + | - hh[:mm]]].
		/// </summary>
		/// <param name="buffer">      the buffer of bytes to keep the <c>TZTimeOnly</c> format </param>
		/// <param name="time">        the given time value </param>
		/// <param name="precision">   the desired time precision </param>
		public static void formatTZTimeOnly(byte[] buffer, DateTimeOffset time, TimestampPrecision precision)
		{
			formatTZTimeOnly(buffer, time, precision, 0);
		}

		/// <summary>
		/// Formats the value of <c>time</c> to the <c>TZTimeOnly</c> format.
		/// <p/>
		/// The format for <c>TZTimeOnly</c> is HH:MM[:SS][.sss][sss][sss][Z | [ + | - hh[:mm]]].
		/// </summary>
		/// <param name="buffer">      the buffer of bytes to keep the <c>TZTimeOnly</c> format </param>
		/// <param name="time">        the given time value </param>
		/// <param name="precision">   the desired time precision </param>
		/// <param name="offset">      the offset </param>
		public static void formatTZTimeOnly(byte[] buffer, DateTimeOffset time, TimestampPrecision precision,
			int offset)
		{
			offset = FormatHour(time.Hour, buffer, offset);
			buffer[offset++] = (byte)':';
			offset = FormatMinute(time.Minute, buffer, offset);

			switch (precision)
			{
				case TimestampPrecision.Minute:
					break;
				case TimestampPrecision.Second:
					buffer[offset++] = (byte)':';
					offset = FormatSecond(time.Second, buffer, offset);
					break;
				case TimestampPrecision.Milli:
				case TimestampPrecision.Micro:
				case TimestampPrecision.Nano:
					buffer[offset++] = (byte)':';
					offset = FormatSecond(time.Second, buffer, offset);
					buffer[offset++] = (byte)'.';
					offset = FormatSecondFractions(time.GetNanosecondsOfSecond(), buffer, offset, precision);
					break;
			}

			offset = FormatOffset(time.Offset, buffer, offset);
		}

		/// <summary>
		/// Formats the value of <c>dateTime</c> to <c>Timestamp</c> format.
		/// <p/>
		/// The format of <c>Timestamp</c> is YYYYMMDD-HH:MM:SS[.sss][sss][sss].
		/// </summary>
		/// <param name="buffer">      the buffer of bytes to keep the <c>Timestamp</c> format </param>
		/// <param name="dateTime">    the given date time value </param>
		/// <param name="precision">   the desired timestamp precision
		/// </param>
		/// <exception cref="ArgumentException"> </exception>
		public static void FormatTimestamp(byte[] buffer, DateTime dateTime, TimestampPrecision precision)
		{
			FormatTimestamp(buffer, dateTime, precision, 0);
		}

		/// <summary>
		/// Formats the value of <c>dateTime</c> to <c>Timestamp</c> format.
		/// <p/>
		/// The format of <c>Timestamp</c> is YYYYMMDD-HH:MM:SS[.sss][sss][sss].
		/// </summary>
		/// <param name="buffer">      the buffer of bytes to keep the <c>Timestamp</c> format </param>
		/// <param name="dateTime">    the given date time value </param>
		/// <param name="precision">   the desired timestamp precision </param>
		/// <param name="offset">      the offset
		/// </param>
		/// <exception cref="ArgumentException"> </exception>
		public static void FormatTimestamp(byte[] buffer, DateTime dateTime, TimestampPrecision precision, int offset)
		{
			offset = FormatDate(dateTime.Year, dateTime.Month, dateTime.Day, buffer, offset);
			buffer[offset++] = (byte)'-';
			offset = FormatTimeWithoutFractions(dateTime.Hour, dateTime.Minute, dateTime.Second, buffer, offset);

			switch (precision)
			{
				case TimestampPrecision.Second:
					break;
				case TimestampPrecision.Milli:
				case TimestampPrecision.Micro:
				case TimestampPrecision.Nano:
					buffer[offset++] = (byte)'.';
					offset = FormatSecondFractions(dateTime.GetNanosecondsOfSecond(), buffer, offset, precision);
					break;
				default:
					throw new ArgumentException(
						"Only 'Second', 'Milli', 'Micro' and 'Nano' values of timestamp precision are available");
			}
		}

		/// <summary>
		/// Formats the value of <c>dateTime</c> to the <c>TZTimestamp</c>} format.
		/// <p/>
		/// The format for <c>TZTimestamp</c> is YYYYMMDD-HH:MM[:SS][.sss][sss][sss][Z | [ + | - hh[:mm]]].
		/// </summary>
		/// <param name="buffer">      the buffer of bytes to keep the <c>TZTimestamp</c> format </param>
		/// <param name="dateTime">    the given date time value </param>
		/// <param name="precision">   the desired timestamp precision </param>
		public static void FormatTzTimestamp(byte[] buffer, DateTimeOffset dateTime, TimestampPrecision precision)
		{
			FormatTzTimestamp(buffer, dateTime, precision, 0);
		}

		/// <summary>
		/// Formats the value of <c>dateTime</c> to the <c>TZTimestamp</c> format.
		/// <p/>
		/// The format for <c>TZTimestamp</c> is YYYYMMDD-HH:MM[:SS][.sss][sss][sss][Z | [ + | - hh[:mm]]].
		/// </summary>
		/// <param name="buffer">      the buffer of bytes to keep the <c>TZTimestamp</c> format </param>
		/// <param name="dateTime">    the given date time value </param>
		/// <param name="precision">   the desired timestamp precision </param>
		/// <param name="offset">      the offset  </param>
		public static void FormatTzTimestamp(byte[] buffer, DateTimeOffset dateTime, TimestampPrecision precision,
			int offset)
		{
			offset = FormatDate(dateTime.Year, dateTime.Month, dateTime.Day, buffer, offset);
			buffer[offset++] = (byte)'-';
			offset = FormatHour(dateTime.Hour, buffer, offset);
			buffer[offset++] = (byte)':';
			offset = FormatMinute(dateTime.Minute, buffer, offset);

			switch (precision)
			{
				case TimestampPrecision.Minute:
					break;
				case TimestampPrecision.Second:
					buffer[offset++] = (byte)':';
					offset = FormatSecond(dateTime.Second, buffer, offset);
					break;
				case TimestampPrecision.Milli:
				case TimestampPrecision.Micro:
				case TimestampPrecision.Nano:
					buffer[offset++] = (byte)':';
					offset = FormatSecond(dateTime.Second, buffer, offset);
					buffer[offset++] = (byte)'.';
					offset = FormatSecondFractions(dateTime.GetNanosecondsOfSecond(), buffer, offset, precision);
					break;
			}

			offset = FormatOffset(dateTime.Offset, buffer, offset);
		}

		/// <summary>
		/// Formats the value of <c>dateTime</c> to Storage Timestamp format.
		/// <p/>
		/// The format of Storage Timestamp is "YYYYMMDD HH:MM:SS.sss[sss][sss] - ".
		/// </summary>
		/// <param name="buffer"> the buffer of bytes to keep the Storage Timestamp format </param>
		/// <param name="offset"> the offset </param>
		/// <param name="dateTime"> the given date time value </param>
		/// <param name="precision"> the desired timestamp precision
		/// </param>
		/// <exception cref="ArgumentException"> </exception>
		public static void FormatStorageTimestamp(byte[] buffer, int offset, DateTimeOffset dateTime,
			TimestampPrecision precision)
		{
			var localOffset = offset;
			localOffset = FormatDate(dateTime.Year, dateTime.Month, dateTime.Day, buffer, localOffset);
			buffer[localOffset++] = (byte)' ';
			localOffset =
				FormatTimeWithoutFractions(dateTime.Hour, dateTime.Minute, dateTime.Second, buffer, localOffset);

			buffer[localOffset++] = (byte)'.';
			localOffset = FormatSecondFractions(dateTime.GetNanosecondsOfSecond(), buffer, localOffset, precision);

			buffer[localOffset++] = (byte)' ';
			buffer[localOffset++] = (byte)'-';
			buffer[localOffset++] = (byte)' ';
		}

		private static int FormatYear(int year, byte[] buffer, int offset)
		{
			buffer[offset] = (byte)(year / 1000 % 10 + '0');
			buffer[offset + 1] = (byte)(year / 100 % 10 + '0');
			buffer[offset + 2] = (byte)(year / 10 % 10 + '0');
			buffer[offset + 3] = (byte)(year % 10 + '0');
			return offset + 4;
		}

		private static int FormatMonth(int month, byte[] buffer, int offset)
		{
			buffer[offset] = (byte)(month / 10 % 10 + '0');
			buffer[offset + 1] = (byte)(month % 10 + '0');
			return offset + 2;
		}

		private static int FormatDay(int day, byte[] buffer, int offset)
		{
			buffer[offset] = (byte)(day / 10 % 10 + '0');
			buffer[offset + 1] = (byte)(day % 10 + '0');
			return offset + 2;
		}

		private static int FormatDate(int year, int month, int day, byte[] buffer, int offset)
		{
			offset = FormatYear(year, buffer, offset);
			offset = FormatMonth(month, buffer, offset);
			offset = FormatDay(day, buffer, offset);
			return offset;
		}

		private static int FormatHour(int hour, byte[] buffer, int offset)
		{
			buffer[offset] = (byte)(hour / 10 % 10 + '0');
			buffer[offset + 1] = (byte)(hour % 10 + '0');
			return offset + 2;
		}

		private static int FormatMinute(int minute, byte[] buffer, int offset)
		{
			buffer[offset] = (byte)(minute / 10 % 10 + '0');
			buffer[offset + 1] = (byte)(minute % 10 + '0');
			return offset + 2;
		}

		private static int FormatSecond(int second, byte[] buffer, int offset)
		{
			buffer[offset] = (byte)(second / 10 % 10 + '0');
			buffer[offset + 1] = (byte)(second % 10 + '0');
			return offset + 2;
		}

		private static int FormatTimeWithoutFractions(int hour, int minute, int second, byte[] buffer, int offset)
		{
			offset = FormatHour(hour, buffer, offset);
			buffer[offset++] = (byte)':';
			offset = FormatMinute(minute, buffer, offset);
			buffer[offset++] = (byte)':';
			offset = FormatSecond(second, buffer, offset);
			return offset;
		}

		private static int FormatSecondFractions(int nano, byte[] buffer, int offset, TimestampPrecision precision)
		{
			var milli = nano / 1000000;
			var micro = nano / 1000;

			buffer[offset] = (byte)(milli / 100 + '0');
			buffer[offset + 1] = (byte)(milli / 10 % 10 + '0');
			buffer[offset + 2] = (byte)(milli % 10 + '0');

			switch (precision)
			{
				case TimestampPrecision.Milli:
					return offset + 3;
				case TimestampPrecision.Micro:
					micro %= 1000;
					buffer[offset + 3] = (byte)(micro / 100 + '0');
					buffer[offset + 4] = (byte)(micro / 10 % 10 + '0');
					buffer[offset + 5] = (byte)(micro % 10 + '0');

					return offset + 6;
				case TimestampPrecision.Nano:
					micro %= 1000;
					buffer[offset + 3] = (byte)(micro / 100 + '0');
					buffer[offset + 4] = (byte)(micro / 10 % 10 + '0');
					buffer[offset + 5] = (byte)(micro % 10 + '0');

					nano %= 1000;
					buffer[offset + 6] = (byte)(nano / 100 + '0');
					buffer[offset + 7] = (byte)(nano / 10 % 10 + '0');
					buffer[offset + 8] = (byte)(nano % 10 + '0');

					return offset + 9;
				default:
					throw new ArgumentException(
						"Only 'Milli', 'Micro' and 'Nano' values of second fractions precision are available");
			}
		}

		private static int FormatOffset(TimeSpan zoneOffset, byte[] buffer, int offset)
		{
			var offsetMinutes = zoneOffset.GetTotalMinutes();
			if (offsetMinutes == 0)
			{
				buffer[offset] = (byte)'Z';
				return offset + 1;
			}

			if (offsetMinutes < 0)
			{
				buffer[offset] = (byte)'-';
				offsetMinutes = -offsetMinutes;
			}
			else
			{
				buffer[offset] = (byte)'+';
			}

			if (offsetMinutes % 60 == 0)
			{
				var val = offsetMinutes / 60;
				buffer[offset + 1] = (byte)(val / 10 % 10 + '0');
				buffer[offset + 2] = (byte)(val % 10 + '0');
				return offset + 3;
			}
			else
			{
				var val = offsetMinutes / 60;
				buffer[offset + 1] = (byte)(val / 10 % 10 + '0');
				buffer[offset + 2] = (byte)(val % 10 + '0');
				buffer[offset + 3] = (byte)':';
				val = offsetMinutes % 60;
				buffer[offset + 4] = (byte)(val / 10 % 10 + '0');
				buffer[offset + 5] = (byte)(val % 10 + '0');
				return offset + 6;
			}
		}
	}
}
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
using Epam.FixAntenna.NetCore.Helpers;

namespace Epam.FixAntenna.NetCore.Message
{
	/// <summary>
	/// Helper class for FIX date and time formats.
	/// <para>
	/// Provides ability for parsing different types of date/time values
	/// from the buffer of bytes. It is also possible to work with high precision
	/// values: micro- and nanoseconds.
	///
	/// </para>
	/// </summary>
	internal class HighPrecisionDateTimeParsers
	{
		/// <summary>
		/// Parses the <c>TimeOnly</c> value from <c>buffer</c>.
		/// The format for <c>TimeOnly</c> is HH:MM:SS[.sss][sss][sss]
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		/// <returns> the local time structured from the given buffer
		/// <p/>
		/// In contrast to <c>Calendar</c> supports nanoseconds precision. </returns>
		/// <exception cref="ArgumentException"> </exception>
		public static DateTimeOffset parseTimeOnly(byte[] buffer)
		{
			return parseTimeOnly(buffer, 0, buffer.Length);
		}

		/// <summary>
		/// Parses the <c>TimeOnly</c> value from <c>buffer</c>.
		/// The format for <c>TimeOnly</c> is HH:MM:SS[.sss][sss][sss]
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		/// <param name="offset"> the initial offset </param>
		/// <param name="count">  the length </param>
		/// <returns> the local time structured from the given buffer
		/// <p/>
		/// In contrast to <c>Calendar</c> supports nanoseconds precision. </returns>
		/// <exception cref="ArgumentException"> </exception>
		public static DateTime parseTimeOnly(byte[] buffer, int offset, int count)
		{
			if (buffer == null || buffer[offset + 2] != (byte)':' || buffer[offset + 5] != (byte)':')
			{
				throw new ArgumentException();
			}

			var hour = FixTypes.ParseNumberPart(buffer, offset, offset + 2);
			if (hour < 0 || hour > 23)
			{
				throw new ArgumentException();
			}

			var minute = FixTypes.ParseNumberPart(buffer, offset + 3, offset + 5);
			if (minute < 0 || minute > 59)
			{
				throw new ArgumentException();
			}

			var second = FixTypes.ParseNumberPart(buffer, offset + 6, offset + 8);
			if (second < 0 || second > 60)
			{
				throw new ArgumentException();
			}

			var nanosecond = 0;
			if (count >= 10)
			{
				if (buffer[offset + 8] != (byte)'.')
				{
					throw new ArgumentException();
				}

				nanosecond = ParseNano(buffer, offset, 9, count);
			}

			if (second == 60)
			{
				var time = new DateTimeBuilder().SetHour(hour).SetMinute(minute).SetSecond(59).SetNanosecond(nanosecond)
					.Build(DateTimeKind.Utc);
				return time.AddSeconds(1);
			}

			return new DateTimeBuilder().SetHour(hour).SetMinute(minute).SetSecond(second).SetNanosecond(nanosecond)
				.Build(DateTimeKind.Utc);
		}

		/// <summary>
		/// Parses the <c>TZTimeOnly</c> value from <c>buffer</c>.
		/// The format for <c>TZTimeOnly</c> is HH:MM[:SS][.sss][sss][sss][Z | [ + | - hh[:mm]]]
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		/// <returns> the offset time structured from the given buffer
		/// <p/>
		/// In contrast to <c>Calendar</c> supports nanoseconds precision. </returns>
		/// <exception cref="ArgumentException"> </exception>
		public static DateTimeOffset parseTZTimeOnly(byte[] buffer)
		{
			return parseTZTimeOnly(buffer, 0, buffer.Length);
		}

		/// <summary>
		/// Parses the <c>TZTimeOnly</c> value from <c>buffer</c>.
		/// The format for <c>TZTimeOnly</c> is HH:MM[:SS][.sss][sss][sss][Z | [ + | - hh[:mm]]]
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		/// <param name="offset"> the initial offset </param>
		/// <param name="count">  the length </param>
		/// <returns> the offset time structured from the given buffer
		/// <p/>
		/// In contrast to <c>Calendar</c> supports nanoseconds precision. </returns>
		/// <exception cref="ArgumentException"> </exception>
		public static DateTimeOffset parseTZTimeOnly(byte[] buffer, int offset, int count)
		{
			var countWithoutTz = GetTimeCountWithoutTimeZone(buffer, offset, count);
			var hour = FixTypes.ParseNumberPart(buffer, offset, offset + 2);
			if (hour < 0 || hour > 23)
			{
				throw new ArgumentException();
			}

			var minute = FixTypes.ParseNumberPart(buffer, offset + 3, offset + 5);
			if (minute < 0 || minute > 59)
			{
				throw new ArgumentException();
			}

			var second = 0;
			var nanosecond = 0;
			if (count > 5 && buffer[offset + 5] == (byte)':')
			{
				second = FixTypes.ParseNumberPart(buffer, offset + 6, offset + 8);
				if (second < 0 || second > 60)
				{
					throw new ArgumentException();
				}

				if (count > 8 && buffer[offset + 8] == (byte)'.')
				{
					nanosecond = ParseNano(buffer, offset, 9, countWithoutTz);
				}
			}

			var zoneOffset = DateTimeHelper.ParseZoneOffset(buffer, offset + countWithoutTz, count - countWithoutTz);
			if (second == 60)
			{
				var time = new DateTimeBuilder().SetHour(hour).SetMinute(minute).SetSecond(59).SetNanosecond(nanosecond)
					.Build(zoneOffset);
				return time.AddSeconds(1);
			}

			return new DateTimeBuilder().SetHour(hour).SetMinute(minute).SetSecond(second).SetNanosecond(nanosecond)
				.Build(zoneOffset);
		}

		/// <summary>
		/// Parses the <c>Timestamp</c> value from <c>buffer</c> to <c>dateTime</c>.
		/// The format for <c>Timestamp</c> is YYYYMMDD-HH:MM:SS[.sss][sss][sss].
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		/// <returns> the local date time structured from the given buffer
		/// <p/>
		/// In contrast to <c>Calendar</c> supports nanoseconds precision. </returns>
		/// <exception cref="ArgumentException"> </exception>
		public static DateTime ParseTimestamp(byte[] buffer)
		{
			return ParseTimestamp(buffer, 0, buffer.Length);
		}

		/// <summary>
		/// Parses the <c>Timestamp</c> value from <c>buffer</c> to <c>dateTime</c>.
		/// The format for <c>Timestamp</c> is YYYYMMDD-HH:MM:SS[.sss][sss][sss].
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		/// <param name="offset"> the initial offset </param>
		/// <param name="count">  the length </param>
		/// <returns> the local date time structured from the given buffer
		/// <p/>
		/// In contrast to <c>Calendar</c> supports nanoseconds precision. </returns>
		/// <exception cref="ArgumentException"> </exception>
		public static DateTime ParseTimestamp(byte[] buffer, int offset, int count)
		{
			if (buffer == null || buffer[offset + 8] != (byte)'-' || buffer[offset + 11] != (byte)':' ||
				buffer[offset + 14] != (byte)':')
			{
				throw new ArgumentException("Invalid timestamp value: " +
											StringHelper.NewString(buffer, offset, count));
			}

			var year = FixTypes.ParseYearPart(buffer, offset);
			var month = FixTypes.ParseNumberPart(buffer, offset + 4, offset + 6);
			if (month < 1 || month > 12)
			{
				throw new ArgumentException("Incorrect month value");
			}

			var date = FixTypes.ParseDatePart(buffer, offset, year, month);
			if (buffer[offset + 8] != (byte)'-')
			{
				throw new ArgumentException("Incorrect date time delimiter");
			}

			var hour = FixTypes.ParseNumberPart(buffer, offset + 9, offset + 11);
			if (hour < 0 || hour > 23)
			{
				throw new ArgumentException("Incorrect hour value");
			}

			var minute = FixTypes.ParseNumberPart(buffer, offset + 12, offset + 14);
			if (minute < 0 || minute > 59)
			{
				throw new ArgumentException("Incorrect minute value");
			}

			var second = FixTypes.ParseNumberPart(buffer, offset + 15, offset + 17);
			if (second < 0 || second > 60)
			{
				throw new ArgumentException("Incorrect second value");
			}

			var nanosecond = 0;
			if (count >= 19)
			{
				if (buffer[offset + 17] != (byte)'.')
				{
					throw new ArgumentException();
				}

				nanosecond = ParseNano(buffer, offset, 18, count);
			}

			if (second == 60)
			{
				var dateTime = new DateTimeBuilder(year, month, date, hour, minute, 59).SetNanosecond(nanosecond)
					.Build(DateTimeKind.Utc);
				return dateTime.AddSeconds(1);
			}

			return new DateTimeBuilder(year, month, date, hour, minute, second).SetNanosecond(nanosecond)
				.Build(DateTimeKind.Utc);
		}

		/// <summary>
		/// Parses the <c>TZTimestamp</c> value from <c>buffer</c>.
		/// The format for <c>TZTimestamp</c> is YYYYMMDD-HH:MM[:SS][.sss][sss][sss][Z | [ + | - hh[:mm]]]
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		/// <returns> the offset time structured from the given buffer
		/// <p/>
		/// In contrast to <c>Calendar</c> supports nanoseconds precision. </returns>
		/// <exception cref="ArgumentException"> </exception>
		public static DateTimeOffset ParseTzTimestamp(byte[] buffer)
		{
			return ParseTzTimestamp(buffer, 0, buffer.Length);
		}

		/// <summary>
		/// Parses the <c>TZTimestamp</c> value from <c>buffer</c>.
		/// The format for <c>TZTimestamp</c> is YYYYMMDD-HH:MM[:SS][.sss][sss][sss][Z | [ + | - hh[:mm]]]
		/// </summary>
		/// <param name="buffer"> the buffer of bytes </param>
		/// <param name="offset"> the initial offset </param>
		/// <param name="count">  the length </param>
		/// <returns> the offset time structured from the given buffer
		/// <p/>
		/// In contrast to <c>Calendar</c> supports nanoseconds precision. </returns>
		/// <exception cref="ArgumentException"> </exception>
		public static DateTimeOffset ParseTzTimestamp(byte[] buffer, int offset, int count)
		{
			var year = FixTypes.ParseYearPart(buffer, offset);
			var month = FixTypes.ParseNumberPart(buffer, offset + 4, offset + 6);
			if (month < 1 || month > 12)
			{
				throw new ArgumentException("Incorrect month value");
			}

			var date = FixTypes.ParseDatePart(buffer, offset, year, month);

			if (buffer[offset + 8] != (byte)'-')
			{
				throw new ArgumentException("Incorrect date time delimiter");
			}

			var countWithoutTz = 9 + GetTimeCountWithoutTimeZone(buffer, offset + 9, count - 9);
			var hour = FixTypes.ParseNumberPart(buffer, offset + 9, offset + 11);
			if (hour < 0 || hour > 23)
			{
				throw new ArgumentException("Incorrect hour value");
			}

			var minute = FixTypes.ParseNumberPart(buffer, offset + 12, offset + 14);
			if (minute < 0 || minute > 59)
			{
				throw new ArgumentException("Incorrect minute value");
			}

			var second = 0;
			var nanosecond = 0;
			if (count > 14 && buffer[offset + 14] == (byte)':')
			{
				second = FixTypes.ParseNumberPart(buffer, offset + 15, offset + 17);
				if (second < 0 || second > 60)
				{
					throw new ArgumentException("Incorrect second value");
				}

				if (count > 17 && buffer[offset + 17] == (byte)'.')
				{
					nanosecond = ParseNano(buffer, offset, 18, countWithoutTz);
				}
			}

			var zoneOffset = DateTimeHelper.ParseZoneOffset(buffer, offset + countWithoutTz, count - countWithoutTz);
			if (second == 60)
			{
				var time = new DateTimeBuilder(year, month, date, hour, minute, 59).SetNanosecond(nanosecond)
					.Build(zoneOffset);
				return time.AddSeconds(1);
			}

			return new DateTimeBuilder(year, month, date, hour, minute, second).SetNanosecond(nanosecond)
				.Build(zoneOffset);
		}

		private static int GetTimeCountWithoutTimeZone(byte[] buffer, int offset, int count)
		{
			if (buffer[offset + count - 1] == (byte)'Z')
			{
				// zoneID = "Z"
				return count - 1;
			}

			if (count > 6 && (buffer[offset + count - 6] == (byte)'+' || buffer[offset + count - 6] == (byte)'-'))
			{
				// zoneID = "+ | - hh:mm"
				return count - 6;
			}

			// zoneID = "+ | - hh"
			if (buffer[offset + count - 3] == (byte)'+' || buffer[offset + count - 3] == (byte)'-')
			{
				return count - 3;
			}

			// there is no time zone
			return count;
		}

		private static int ParseNano(byte[] buffer, int offset, int offsetNano, int count)
		{
			var summaryOffset = offset + offsetNano;
			if (count <= offsetNano + 3)
			{
				var order = offsetNano + 3 - count;
				var milli = FixTypes.ParseNumberPart(buffer, summaryOffset, summaryOffset + 3 - order);
				while (order > 0)
				{
					milli *= 10;
					order--;
				}

				if (milli < 0 || milli > 999)
				{
					throw new ArgumentException("Incorrect milli value");
				}

				return milli * 1000000;
			}

			if (count <= offsetNano + 6)
			{
				var order = offsetNano + 6 - count;
				var micro = FixTypes.ParseNumberPart(buffer, summaryOffset, summaryOffset + 6 - order);
				while (order > 0)
				{
					micro *= 10;
					order--;
				}

				if (micro < 0 || micro > 999999)
				{
					throw new ArgumentException("Incorrect micro value");
				}

				return micro * 1000;
			}

			if (count <= offsetNano + 9)
			{
				var order = offsetNano + 9 - count;
				var nanosecond = FixTypes.ParseNumberPart(buffer, summaryOffset, summaryOffset + 9 - order);
				while (order > 0)
				{
					nanosecond *= 10;
					order--;
				}

				if (nanosecond < 0 || nanosecond > 999999999)
				{
					throw new ArgumentException("Incorrect nano value");
				}

				return nanosecond;
			}

			throw new ArgumentException("Invalid buffer length");
		}
	}
}
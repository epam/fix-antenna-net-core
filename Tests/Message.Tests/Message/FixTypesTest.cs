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

using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;

using NUnit.Framework; 
using NUnit.Framework.Legacy;

using System;

namespace Epam.FixAntenna.Message.Tests
{
	[TestFixture]
	internal class FixTypesTest
	{
		private const string DefaultTzTimestampFormat1 = "yyyyMMdd-HH:mm:ss.fff";
		private const string DefaultTzTimestampFormat2 = "yyyyMMdd-HH:mm:ss";
		private const string DefaultPrefixFormat = "yyyyMMdd HH:mm:ss.fff - ";

		private const string DateFormat = DefaultTzTimestampFormat1;
		private const string DateFormatGmt530 = DefaultTzTimestampFormat1;
		private const string DateFormatGmt5 = DefaultTzTimestampFormat2;
		private const string DateFormatWithoutMills = DefaultTzTimestampFormat2;
		private const string DateFormatWithoutMills5 = DefaultTzTimestampFormat2;
		private const string DateFormatWithoutMills530 = DefaultTzTimestampFormat2;
		private const string PrefixStorageFormat = DefaultPrefixFormat;

		private static readonly TimeZoneInfo TimeZone5 = TimeZoneInfo.CreateCustomTimeZone(
			"GMT+05", TimeSpan.FromHours(5), "GMT+05", "GMT+05");

		private static readonly TimeZoneInfo TimeZone530 = TimeZoneInfo.CreateCustomTimeZone(
			"GMT+0530", TimeSpan.FromHours(5.5), "GMT+0530", "GMT+0530");

		private void TestTzTime(string[] times, TimeSpan[] zones, bool secs, bool mills, bool micrs, bool nans)
		{
			for (var i = 0; i < times.Length; i++)
			{
				var time = times[i];
				var zone = zones[i];
				var res = FixTypes.parseTZTimeOnly(time.AsByteArray());
				var roundedTime = StringHelper.NewString(FixTypes.formatTZTimeOnly(res));

				ClassicAssert.AreEqual(7, res.Hour);
				ClassicAssert.AreEqual(39, res.Minute);
				if (secs)
				{
					ClassicAssert.AreEqual(32, res.Second);
				}

				if (mills)
				{
					ClassicAssert.AreEqual(1, res.Millisecond);
				}

				var timeZone = res.Offset;
				ClassicAssert.AreEqual(zone, timeZone);

				if (nans)
				{
					time = time.Substring(0, 12) + time.Substring(18);
				}
				else
				{
					time = micrs ? time.Substring(0, 12) + time.Substring(15) : time;
				}

				ClassicAssert.AreEqual(time, roundedTime);
			}
		}

		[Test]
		public virtual void FormatInt()
		{
			ClassicAssert.That(StringHelper.NewString(FixTypes.FormatInt(0)), Is.EqualTo("0"));
			ClassicAssert.That(StringHelper.NewString(FixTypes.FormatInt(10)), Is.EqualTo("10"));
			ClassicAssert.That(StringHelper.NewString(FixTypes.FormatInt(-999999999)), Is.EqualTo("-999999999"));
		}

		[Test]
		public virtual void ParseAndFormatFloat()
		{
			var value = "1.1";
			var parsedValue = FixTypes.ParseFloat(value.AsByteArray());
			var resultValue = StringHelper.NewString(FixTypes.FormatDouble(parsedValue));
			ClassicAssert.That(resultValue, Is.EqualTo(value));
		}

		[Test]
		public virtual void ParseAndFormatMonthYear44()
		{
			var value = "200101w1";
			var expected = "20010101";
			var calendar = FixTypes.ParseMonthYear44(value.AsByteArray());
			var resultValue = StringHelper.NewString(FixTypes.FormatMonthYear44(calendar));
			ClassicAssert.That(resultValue, Is.EqualTo(expected));
		}

		[Test]
		[TestCase("13:39:20", TimestampPrecision.Second, "13:39:20")]
		[TestCase("13:39:20.123", TimestampPrecision.Second, "13:39:20")]
		[TestCase("13:39:20.123456", TimestampPrecision.Second, "13:39:20")]
		[TestCase("13:39:20.123456789", TimestampPrecision.Second, "13:39:20")]
		[TestCase("13:39:20", TimestampPrecision.Milli, "13:39:20.000")]
		[TestCase("13:39:20.123", TimestampPrecision.Milli, "13:39:20.123")]
		[TestCase("13:39:20.123456", TimestampPrecision.Milli, "13:39:20.123")]
		[TestCase("13:39:20.123456789", TimestampPrecision.Milli, "13:39:20.123")]
		public virtual void TestParseUTCTimeOnly(string receivedValue, TimestampPrecision precision, string expectedValue)
		{
			var cal = FixTypes.parseTimeOnly(receivedValue.AsByteArray());
			var actualResult = StringHelper.NewString(FixTypes.formatUTCTimeOnly(cal, precision));
			ClassicAssert.AreEqual(expectedValue, actualResult);
		}

		[Test]
		[TestCase(TimestampPrecision.Minute)]
		[TestCase(TimestampPrecision.Micro)]
		[TestCase(TimestampPrecision.Nano)]
		public virtual void TestFormatUTCTimeOnlyWrongPrecision(TimestampPrecision precision)
		{
			ClassicAssert.Throws<ArgumentException>(() =>
			{
				FixTypes.formatUTCTimeOnly(new DateTime(), precision);
			});
		}

		[Test]
		[TestCase("20100218-13:39:20", TimestampPrecision.Second, "20100218-13:39:20")]
		[TestCase("20100218-13:39:20.123", TimestampPrecision.Second, "20100218-13:39:20")]
		[TestCase("20100218-13:39:20.123456", TimestampPrecision.Second, "20100218-13:39:20")]
		[TestCase("20100218-13:39:20.123456789", TimestampPrecision.Second, "20100218-13:39:20")]
		[TestCase("20100218-13:39:20", TimestampPrecision.Milli, "20100218-13:39:20.000")]
		[TestCase("20100218-13:39:20.123", TimestampPrecision.Milli, "20100218-13:39:20.123")]
		[TestCase("20100218-13:39:20.123456", TimestampPrecision.Milli, "20100218-13:39:20.123")]
		[TestCase("20100218-13:39:20.123456789", TimestampPrecision.Milli, "20100218-13:39:20.123")]
		public virtual void TestParseUtcTimestamp(string receivedValue, TimestampPrecision precision, string expectedValue)
		{
			var cal = FixTypes.ParseTimestamp(receivedValue.AsByteArray());
			var actualResult = StringHelper.NewString(FixTypes.FormatUtcTimestamp(cal, precision));
			ClassicAssert.AreEqual(expectedValue, actualResult);
		}

		[Test]
		[TestCase(TimestampPrecision.Minute)]
		[TestCase(TimestampPrecision.Micro)]
		[TestCase(TimestampPrecision.Nano)]
		public virtual void TesFormatUtcTimestampWrongPrecision(TimestampPrecision precision)
		{
			ClassicAssert.Throws<ArgumentException>(() =>
			{
				FixTypes.FormatUtcTimestamp(new DateTime(), precision);
			});
		}

		[Test]
		public virtual void testTZTimeOnly()
		{
			string[] times = { "07:39Z", "07:39-05", "07:39+05", "07:39+05:30" };
			TimeSpan[] zones =
				{ TimeSpan.FromHours(0), TimeSpan.FromHours(-5), TimeSpan.FromHours(5), TimeSpan.FromHours(5.5) };

			TestTzTime(times, zones, false, false, false, false);
		}

		[Test]
		public virtual void testTZTimeOnlyWithMicros()
		{
			string[] times =
				{ "07:39:32.001234Z", "07:39:32.001234-05", "07:39:32.001234+05", "07:39:32.001234+05:30" };
			TimeSpan[] zones =
				{ TimeSpan.FromHours(0), TimeSpan.FromHours(-5), TimeSpan.FromHours(5), TimeSpan.FromHours(5.5) };

			TestTzTime(times, zones, true, true, true, false);
		}

		[Test]
		public virtual void testTZTimeOnlyWithMills()
		{
			string[] times = { "07:39:32.001Z", "07:39:32.001-05", "07:39:32.001+05", "07:39:32.001+05:30" };
			TimeSpan[] zones =
				{ TimeSpan.FromHours(0), TimeSpan.FromHours(-5), TimeSpan.FromHours(5), TimeSpan.FromHours(5.5) };

			TestTzTime(times, zones, true, true, false, false);
		}

		[Test]
		public virtual void testTZTimeOnlyWithNanos()
		{
			string[] times =
				{ "07:39:32.001234567Z", "07:39:32.001234567-05", "07:39:32.001234567+05", "07:39:32.001234567+05:30" };
			TimeSpan[] zones =
				{ TimeSpan.FromHours(0), TimeSpan.FromHours(-5), TimeSpan.FromHours(5), TimeSpan.FromHours(5.5) };

			TestTzTime(times, zones, true, true, true, true);
		}

		[Test]
		public virtual void testTZTimeOnlyWithSeconds()
		{
			//  Example: 07:39:45Z is 07:39 UTC
			//  Example: 02:39:12-05 is five hours behind UTC, thus Eastern Time
			//  Example: 15:39:31+08 is eight hours ahead of UTC, Hong Kong/Singapore time
			//  Example: 13:09:09+05:30 is 5.5 hours ahead of UTC, India time

			string[] times = { "07:39:32Z", "07:39:32-05", "07:39:32+05", "07:39:32+05:30" };
			TimeSpan[] zones =
				{ TimeSpan.FromHours(0), TimeSpan.FromHours(-5), TimeSpan.FromHours(5), TimeSpan.FromHours(5.5) };

			TestTzTime(times, zones, false, false, false, false);
		}

		[Test]
		public virtual void testParseTZTimeOnlyWithoutTZ()
		{
			TestParseTzTimeWithoutTz("07:39", false, false);
			TestParseTzTimeWithoutTz("07:39:32", false, false);
			TestParseTzTimeWithoutTz("07:39:32.001", false, false);
			TestParseTzTimeWithoutTz("07:39:32.001234", false, false);
			TestParseTzTimeWithoutTz("07:39:32.001234567", false, false);
		}

		private void TestParseTzTimeWithoutTz(string time, bool secs, bool mills)
		{
			var res = FixTypes.parseTZTimeOnly(time.AsByteArray());

			ClassicAssert.AreEqual(7, res.Hour);
			ClassicAssert.AreEqual(39, res.Minute);
			if (secs)
			{
				ClassicAssert.AreEqual(32, res.Second);
			}
			if (mills)
			{
				ClassicAssert.AreEqual(1, res.Millisecond);
			}

			var timeZoneOffset = res.Offset;
			ClassicAssert.AreEqual(DateTimeOffset.Now.Offset, timeZoneOffset);
		}

		[Test]
		public virtual void TestTzTimestamp()
		{
			//  Example: 20060901-07:39Z is 07:39 UTC on 1st of September 2006
			//  Example: 20060901-02:39-05 is five hours behind UTC, thus Eastern Time on 1st of September 2006
			//  Example: 20060901-15:39+08 is eight hours ahead of UTC, Hong Kong/Singapore time on 1st of September 2006
			//  Example: 20060901-13:09+05:30 is 5.5 hours ahead of UTC, India time on 1st of September 2006

			var times = new[] { "20060901-07:39Z", "20060901-07:39+05", "20060901-07:39+05:30" };
			var zones = new[] { TimeSpan.Zero, TimeSpan.FromHours(5), TimeSpan.FromMinutes(330) };

			TestTzTimestamp(times, zones, TimestampPrecision.Minute, false, false, false, false);
		}

		[Test]
		public virtual void TestTzTimestampWithSeconds()
		{
			var times = new[] { "20060901-07:39:32Z", "20060901-07:39:32-05", "20060901-07:39:32+05", "20060901-07:39:32+05:30" };
			var zones = new[] { TimeSpan.Zero, TimeSpan.FromHours(-5), TimeSpan.FromHours(5), TimeSpan.FromMinutes(330) };

			TestTzTimestamp(times, zones, TimestampPrecision.Second, true, false, false, false);
		}

		[Test]
		public virtual void TestTzTimestampWithMills()
		{
			var times = new[] { "20060901-07:39:32.123Z", "20060901-07:39:32.123-05", "20060901-07:39:32.123+05", "20060901-07:39:32.123+05:30" };
			var zones = new[] { TimeSpan.Zero, TimeSpan.FromHours(-5), TimeSpan.FromHours(5), TimeSpan.FromMinutes(330) };

			TestTzTimestamp(times, zones, TimestampPrecision.Milli, true, true, false, false);
		}

		[Test]
		public virtual void TestTzTimestampWithMicros()
		{
			var times = new[] { "20060901-07:39:32.123456Z", "20060901-07:39:32.123456-05", "20060901-07:39:32.123456+05", "20060901-07:39:32.123456+05:30" };
			var zones = new[] { TimeSpan.Zero, TimeSpan.FromHours(-5), TimeSpan.FromHours(5), TimeSpan.FromMinutes(330) };

			TestTzTimestamp(times, zones, TimestampPrecision.Milli, true, true, true, false);
		}

		[Test]
		public virtual void TestTzTimestampWithNanos()
		{
			var times = new[] { "20060901-07:39:32.123456789Z", "20060901-07:39:32.123456789-05", "20060901-07:39:32.123456789+05", "20060901-07:39:32.123456789+05:30" };
			var zones = new[] { TimeSpan.Zero, TimeSpan.FromHours(-5), TimeSpan.FromHours(5), TimeSpan.FromMinutes(330) };

			TestTzTimestamp(times, zones, TimestampPrecision.Milli, true, true, true, true);
		}

		private void TestTzTimestamp(string[] valuesToBeTested, TimeSpan[] zones, TimestampPrecision precision,
			bool seconds, bool mills, bool micrs, bool nans)
		{
			for (var i = 0; i < valuesToBeTested.Length; i++)
			{
				var time = valuesToBeTested[i];
				var zone = zones[i];
				var calendar = FixTypes.ParseTzTimestamp(time.AsByteArray());
				var roundedTime = StringHelper.NewString(FixTypes.FormatTzTimestamp(calendar, precision));

				ClassicAssert.AreEqual(2006, calendar.Year);
				ClassicAssert.AreEqual(9, calendar.Month);

				ClassicAssert.AreEqual(7, calendar.Hour);
				ClassicAssert.AreEqual(39, calendar.Minute);
				if (seconds)
				{
					ClassicAssert.AreEqual(32, calendar.Second);
				}

				if (mills)
				{
					ClassicAssert.AreEqual(123, calendar.Millisecond);
				}

				var timeZoneOffset = calendar.Offset;
				ClassicAssert.AreEqual(zone, timeZoneOffset);

				if (nans)
				{
					time = time.Substring(0, 21) + time.Substring(27);
				}
				else
				{
					time = micrs ? time.Substring(0, 21) + time.Substring(24) : time;
				}

				ClassicAssert.AreEqual(time, roundedTime);
			}
		}

		[Test]
		[TestCase(TimestampPrecision.Micro)]
		[TestCase(TimestampPrecision.Nano)]
		public void TestFormatTzTimestampWrongPrecision(TimestampPrecision precision)
		{
			ClassicAssert.Throws<ArgumentException>(() =>
			{
				StringHelper.NewString(FixTypes.FormatTzTimestamp(new DateTimeOffset(), precision));
			});
		}

		[Test]
		public virtual void TestParseTzTimestampWithoutTz()
		{
			TestParseTzTimestampWithoutTz("20110901-07:39", false, false);
			TestParseTzTimestampWithoutTz("20110901-07:39:32", false, false);
			TestParseTzTimestampWithoutTz("20110901-07:39:32.001", false, false);
			TestParseTzTimestampWithoutTz("20110901-07:39:32.001234", false, false);
			TestParseTzTimestampWithoutTz("20110901-07:39:32.001234567", false, false);
		}

		[Test]
		public void TestParseTzProblem()
		{
			var res = FixTypes.ParseTzTimestamp("20110901-00:00".AsByteArray());
		}

		private void TestParseTzTimestampWithoutTz(string time, bool secs, bool mills)
		{
			var res = FixTypes.ParseTzTimestamp(time.AsByteArray());

			ClassicAssert.AreEqual(2011, res.Year);
			ClassicAssert.AreEqual(9, res.Month);
			ClassicAssert.AreEqual(1, res.Day);
			ClassicAssert.AreEqual(7, res.Hour);
			ClassicAssert.AreEqual(39, res.Minute);
			if (secs)
			{
				ClassicAssert.AreEqual(32, res.Second);
			}
			if (mills)
			{
				ClassicAssert.AreEqual(1, res.Millisecond);
			}

			var timeZoneOffset = res.Offset;
			ClassicAssert.AreEqual(DateTimeOffset.Now.Offset, timeZoneOffset);
		}

		[Test]
		public virtual void TestFormatTzTimestamp()
		{
			var calendar = DateTimeOffset.Now;
			var result = StringHelper.NewString(FixTypes.FormatTzTimestamp(calendar, TimestampPrecision.Milli));
			var parsedCalender = FixTypes.ParseTzTimestamp(result.AsByteArray());

			CompareCalendars(calendar, parsedCalender);

			calendar = DateTimeOffset.Now;
			calendar = calendar.AddSeconds(-calendar.Second);
			calendar = calendar.AddMilliseconds(-calendar.Millisecond);
			result = StringHelper.NewString(FixTypes.FormatTzTimestamp(calendar, TimestampPrecision.Milli));
			parsedCalender = FixTypes.ParseTzTimestamp(result.AsByteArray());

			CompareCalendars(calendar, parsedCalender);
		}

		private void CompareCalendars(DateTimeOffset calendar, DateTimeOffset parsedCalender)
		{
			ClassicAssert.AreEqual(calendar.Year, parsedCalender.Year);
			ClassicAssert.AreEqual(calendar.Month, parsedCalender.Month);
			ClassicAssert.AreEqual(calendar.Day, parsedCalender.Day);
			ClassicAssert.AreEqual(calendar.Hour, parsedCalender.Hour);
			ClassicAssert.AreEqual(calendar.Minute, parsedCalender.Minute);
			ClassicAssert.AreEqual(calendar.Second, parsedCalender.Second);
			ClassicAssert.AreEqual(calendar.Millisecond, parsedCalender.Millisecond);
		}


		[Test]
		public virtual void TestFormatTzTimestampWithMilliSec()
		{
			var cal = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromMinutes(330));
			var ourResult = StringHelper.NewString(FixTypes.FormatTzTimestamp(cal, TimestampPrecision.Milli));
			var actualResult = cal.ToString(DateFormatGmt530);

			ClassicAssert.AreEqual(actualResult + GetTimeZone(cal), ourResult, "Must be the same");
		}

		[Test]
		public virtual void TestFormatTzTimestampWithoutMilli()
		{
			var cal = DateTimeOffset.UtcNow;
			cal = cal.AddMilliseconds(-cal.Millisecond);
			var ourResult = StringHelper.NewString(FixTypes.FormatTzTimestamp(cal, TimestampPrecision.Second));
			var actualResult = cal.ToString(DateFormatWithoutMills);

			ClassicAssert.AreEqual(actualResult + GetTimeZone(cal), ourResult, "Must be the same");
		}

		[Test]
		public virtual void testFormatTZTimestampWithMilliSecTZ5_30()
		{
			var cal = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromMinutes(330));
			var actual = StringHelper.NewString(FixTypes.FormatTzTimestamp(cal, TimestampPrecision.Milli));
			var expected = cal.ToString(DateFormatGmt530) + GetTimeZone(cal);

			ClassicAssert.AreEqual(expected, actual, "Must be the same");
		}

		[Test]
		public virtual void TestFormatTzTimestampWithMilliSecTz5()
		{
			var cal = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromMinutes(-30));
			cal = cal.AddMilliseconds(-cal.Millisecond);
			var actual = StringHelper.NewString(FixTypes.FormatTzTimestamp(cal, TimestampPrecision.Second));
			var expected = cal.ToString(DateFormatGmt5) + GetTimeZone(cal);

			ClassicAssert.AreEqual(expected, actual, "Must be the same");
		}
		[Test]
		public virtual void TestFormatTzTimestampWithoutMilli5()
		{
			var cal = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromMinutes(300));
			cal = cal.AddMilliseconds(-cal.Millisecond);
			var actual = StringHelper.NewString(FixTypes.FormatTzTimestamp(cal, TimestampPrecision.Second));
			var expected = cal.ToString(DateFormatWithoutMills5) + GetTimeZone(cal);

			ClassicAssert.AreEqual(expected, actual, "Must be the same");
		}

		[Test]
		public virtual void testFormatTZTimestampWithoutMilli5_30()
		{
			var cal = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromMinutes(330));
			cal = cal.AddMilliseconds(-cal.Millisecond);
			var actual = StringHelper.NewString(FixTypes.FormatTzTimestamp(cal, TimestampPrecision.Second));

			var expected = cal.ToString(DateFormatWithoutMills530) + GetTimeZone(cal);

			ClassicAssert.AreEqual(expected, actual, "Must be the same");
		}

		// YYYYMM
		// YYYYMMDD
		// YYYYMMWW
		// YYYY = 0000-9999
		// MM = 01-12, DD = 01-31
		// WW = w1, w2, w3, w4, w5

		[Test]
		public virtual void TestMonthYear44Parser()
		{
			var value = "200101";
			var cal = FixTypes.ParseMonthYear44(value.AsByteArray());
			ClassicAssert.AreEqual(2001, cal.Year);
			ClassicAssert.AreEqual(1, cal.Month);

			value = "201003";
			cal = FixTypes.ParseMonthYear44(value.AsByteArray());
			ClassicAssert.AreEqual(2010, cal.Year);
			ClassicAssert.AreEqual(3, cal.Month);

			value = "20100312";
			cal = FixTypes.ParseMonthYear44(value.AsByteArray());
			ClassicAssert.AreEqual(2010, cal.Year);
			ClassicAssert.AreEqual(3, cal.Month);
			ClassicAssert.AreEqual(12, cal.Day);

			value = "201003w5";
			cal = FixTypes.ParseMonthYear44(value.AsByteArray());
			ClassicAssert.AreEqual(2010, cal.Year);
			ClassicAssert.AreEqual(3, cal.Month);
			ClassicAssert.AreEqual(5, cal.GetWeekOfMonth());

			value = "201003w4";
			cal = FixTypes.ParseMonthYear44(value.AsByteArray());
			ClassicAssert.AreEqual(2010, cal.Year);
			ClassicAssert.AreEqual(3, cal.Month);
			ClassicAssert.AreEqual(4, cal.GetWeekOfMonth());

			value = "201003w3";
			cal = FixTypes.ParseMonthYear44(value.AsByteArray());
			ClassicAssert.AreEqual(2010, cal.Year);
			ClassicAssert.AreEqual(3, cal.Month);
			ClassicAssert.AreEqual(3, cal.GetWeekOfMonth());

			value = "201003w2";
			cal = FixTypes.ParseMonthYear44(value.AsByteArray());
			ClassicAssert.AreEqual(2010, cal.Year);
			ClassicAssert.AreEqual(3, cal.Month);
			ClassicAssert.AreEqual(2, cal.GetWeekOfMonth());

			value = "201003w1";
			cal = FixTypes.ParseMonthYear44(value.AsByteArray());
			ClassicAssert.AreEqual(2010, cal.Year);
			ClassicAssert.AreEqual(3, cal.Month);
			ClassicAssert.AreEqual(1, cal.GetWeekOfMonth());
		}

		[Test]
		public virtual void TestValidTzTimestamp()
		{
			var values = new string[] { "20060901-07:39Z", "20060901-07:39:30Z", "20060901-07:39:30.301Z", "20060901-07:39:30.301301Z", "20060901-07:39:30.301301301Z", "20060901-07:39+02", "20060901-07:39:30+02", "20060901-07:39:54.101+02", "20060901-07:39:54.101101-02", "20060901-07:39:54.101101101-02", "20060901-07:39+02:31", "20060901-07:39:33+02:31", "20060901-07:39:54.101+02:17", "20060901-07:39:54.101101+02:17", "20060901-07:39:54.101101101-02:17" };

			foreach (var value in values)
			{
				ClassicAssert.IsFalse(FixTypes.IsInvalidTzTimestamp(value.AsByteArray()));
			}
		}

		[Test]
		public virtual void TestInvalidateTzTimestamp()
		{
			var values = new string[] { "20060901-13:09:05:30", "20060901-07:39+02+31", "20060901:07:39+02+31", "20060901+07:39+02+31", "20060901-07+39-02:31", "20060901-07:39.999+02-31", "20060901-07:39.123.456", "20060901-07:39:02.111111111-1" };

			foreach (var value in values)
			{
				ClassicAssert.IsTrue(FixTypes.IsInvalidTzTimestamp(value.AsByteArray()));
			}
		}

		[Test]
		public virtual void TestCheckSum()
		{
			ClassicAssert.AreEqual("000", StringHelper.NewString(FixTypes.FormatCheckSum(0)));
			ClassicAssert.AreEqual("001", StringHelper.NewString(FixTypes.FormatCheckSum(1)));
			ClassicAssert.AreEqual("002", StringHelper.NewString(FixTypes.FormatCheckSum(2)));
			ClassicAssert.AreEqual("010", StringHelper.NewString(FixTypes.FormatCheckSum(10)));
			ClassicAssert.AreEqual("100", StringHelper.NewString(FixTypes.FormatCheckSum(100)));
			ClassicAssert.AreEqual("256", StringHelper.NewString(FixTypes.FormatCheckSum(256)));
		}

		/// <summary>
		/// code from FixTypes class
		/// </summary>
		private static string GetTimeZone(DateTimeOffset cal)
		{
			var offset = cal.Offset;
			if (offset.Minutes != 0)
			{
				return (offset.Ticks >= 0 ? "+" : "-") + offset.ToString("hh\\:mm");
			}
			else if (offset.Hours != 0)
			{
				return (offset.Ticks >= 0 ? "+" : "-") + offset.ToString("hh");
			}
			else
			{
				return "Z";
			}
		}

		[Test]
		public virtual void TestFormatTimestampFromCalendarAndAdditionalFractions()
		{
			var calendar = DateTime.UtcNow;

			var dateTimeResult = calendar.ToString(DateFormat);
			var fixTypesResult = FixTypes.FormatTimestamp(new byte[21], 0, calendar, 0, TimestampPrecision.Milli);
			ClassicAssert.AreEqual(dateTimeResult, StringHelper.NewString(fixTypesResult));

			dateTimeResult = calendar.ToString(DateFormat) + "001";
			fixTypesResult = FixTypes.FormatTimestamp(new byte[24], 0, calendar, 1, TimestampPrecision.Micro);
			ClassicAssert.AreEqual(dateTimeResult, StringHelper.NewString(fixTypesResult));

			dateTimeResult = calendar.ToString(DateFormat) + "000001";
			fixTypesResult = FixTypes.FormatTimestamp(new byte[27], 0, calendar, 1, TimestampPrecision.Nano);
			ClassicAssert.AreEqual(dateTimeResult, StringHelper.NewString(fixTypesResult));
		}

		[Test]
		public virtual void TestFormatStorageTimestampFromCalendar()
		{
			var calendar = DateTime.UtcNow;

			var dateTimeResult = calendar.ToString(PrefixStorageFormat);
			var fixTypesResult = FixTypes.FormatStorageTimestamp(new byte[24], 0, calendar, 0, TimestampPrecision.Milli);
			ClassicAssert.AreEqual(dateTimeResult, StringHelper.NewString(fixTypesResult));

			dateTimeResult = calendar.ToString(PrefixStorageFormat).Replace(" - ", "999 - ");
			fixTypesResult = FixTypes.FormatStorageTimestamp(new byte[27], 0, calendar, 999, TimestampPrecision.Micro);
			ClassicAssert.AreEqual(dateTimeResult, StringHelper.NewString(fixTypesResult));

			dateTimeResult = calendar.ToString(PrefixStorageFormat).Replace(" - ", "999999 - "); ;
			fixTypesResult = FixTypes.FormatStorageTimestamp(new byte[30], 0, calendar, 999999, TimestampPrecision.Nano);
			ClassicAssert.AreEqual(dateTimeResult, StringHelper.NewString(fixTypesResult));
		}

		[Test]
		public virtual void TestShortTimeShortUtcZone()
		{
			var pattern = "07:39Z";
			IsValid(pattern);

			pattern = "00:00Z";
			IsValid(pattern);
		}

		[Test]
		public virtual void TestShortTimeShortTz()
		{
			var pattern = "02:39-05";
			IsValid(pattern);

			pattern = "00:00-01";
			IsValid(pattern);
		}

		[Test]
		public virtual void TestTimeUtcZone()
		{
			var pattern = "12:45:30Z";
			IsValid(pattern);

			pattern = "00:00:00Z";
			IsValid(pattern);
		}

		[Test]
		public virtual void TestShortTimeFullTz()
		{
			var pattern = "12:34+10:35";
			IsValid(pattern);

			pattern = "12:34-10:35";
			IsValid(pattern);

			pattern = "00:00-10:00";
			IsValid(pattern);
		}

		[Test]
		public virtual void TestShortTimeFullTzInvalid()
		{
			var pattern = "12:34:10:35";
			IsInvalid(pattern);

			pattern = "12:34-14:35";
			IsInvalid(pattern);

			pattern = "00:00-00:00";
			IsInvalid(pattern);
		}

		[Test]
		public virtual void TestTimeShortTz()
		{
			var pattern = "12:34:30+10";
			IsValid(pattern);

			pattern = "12:34:30+10";
			IsValid(pattern);
		}

		[Test]
		public virtual void TestTimeShortTzInvalid()
		{
			var pattern = "12:34:30+13";
			IsInvalid(pattern);

			pattern = "12:34:30-17";
			IsInvalid(pattern);

			pattern = "12:34:61-10";
			IsInvalid(pattern);
		}

		[Test]
		public virtual void TestFullTimeUtctz()
		{
			var pattern = "12:53:30.999Z";
			IsValid(pattern);

			pattern = "12:53:30.000Z";
			IsValid(pattern);

			pattern = "00:00:00.000Z";
			IsValid(pattern);
		}

		[Test]
		public virtual void TestFullTimeUtctzInvalid()
		{
			var pattern = "12:53:30:00Z";
			IsInvalid(pattern);

			pattern = "12:53:30:0Z";
			IsInvalid(pattern);
		}

		[Test]
		public virtual void TestMicrosTimeUtctz()
		{
			var pattern = "12:30:30.999999Z";
			IsValid(pattern);

			pattern = "12:30:12.123456Z";
			IsValid(pattern);

			pattern = "00:00:00.000000Z";
			IsValid(pattern);
		}

		[Test]
		public virtual void TestMicrosTimeUtctzInvalid()
		{
			var pattern = "12:30:30.9999Z";
			IsInvalid(pattern);

			pattern = "12:30:61.000000Z";
			IsInvalid(pattern);

			pattern = "00:00:00.00000Z";
			IsInvalid(pattern);
		}

		[Test]
		public virtual void TestNanosTimeUtctz()
		{
			var pattern = "10:30:00.999999999Z";
			IsValid(pattern);

			pattern = "07:32:56.123456789Z";
			IsValid(pattern);

			pattern = "00:00:00.000000000Z";
			IsValid(pattern);
		}

		[Test]
		public virtual void TestNanosTimeUtctzInvalid()
		{
			var pattern = "10:30:10.1234567Z";
			IsInvalid(pattern);

			pattern = "10:30:61.123456789Z";
			IsInvalid(pattern);

			pattern = "00:00:00.00000000Z";
			IsInvalid(pattern);
		}

		[Test]
		public virtual void TestTimeFullTz()
		{
			var pattern = "12:45:45+11:52";
			IsValid(pattern);

			pattern = "12:45:00-11:52";
			IsValid(pattern);

			pattern = "00:00:00-01:00";
			IsValid(pattern);
		}

		[Test]
		public virtual void TestTimeFullTzInvalid()
		{
			var pattern = "12:45:45+15:52";
			IsInvalid(pattern);

			pattern = "12:45:45-15:52";
			IsInvalid(pattern);

			pattern = "12:45:45:15:52";
			IsInvalid(pattern);
		}

		[Test]
		public virtual void TestFullTimeShortTz()
		{
			var pattern = "12:34:40.001+01";
			IsValid(pattern);

			pattern = "12:04:40.099+01";
			IsValid(pattern);

			pattern = "12:34:00.990-11";
			IsValid(pattern);
		}

		[Test]
		public virtual void TestFullTimeShortTzInvalid()
		{
			var pattern = "12:34:00.001+19";
			IsInvalid(pattern);

			pattern = "12:04:70.099+01";
			IsInvalid(pattern);

			pattern = "12:34:00.990/11";
			IsInvalid(pattern);

			pattern = "12:34:00.99011";
			IsInvalid(pattern);

			pattern = "00:00:00.000-00";
			IsInvalid(pattern);
		}

		[Test]
		public virtual void TestMicrosTimeShortTz()
		{
			var pattern = "01:02:03.001001+01";
			IsValid(pattern);

			pattern = "08:34:07.999999+07";
			IsValid(pattern);

			pattern = "12:30:00.999990-11";
			IsValid(pattern);
		}

		[Test]
		public virtual void TestMicrosTimeShortTzInvalid()
		{
			var pattern = "01:02:03.001001+19";
			IsInvalid(pattern);

			pattern = "12:34:56.990990/11";
			IsInvalid(pattern);

			pattern = "12:34:56.1234";
			IsInvalid(pattern);

			pattern = "12:34:56.12345";
			IsInvalid(pattern);

			pattern = "00:00:00.000000-00";
			IsInvalid(pattern);
		}

		[Test]
		public virtual void TestNanosTimeShortTz()
		{
			var pattern = "12:34:56.001001001+01";
			IsValid(pattern);

			pattern = "05:04:40.000000001+07";
			IsValid(pattern);

			pattern = "10:30:00.999999999-11";
			IsValid(pattern);
		}

		[Test]
		public virtual void TestNanosTimeShortTzInvalid()
		{
			var pattern = "12:12:12.001001001+19";
			IsInvalid(pattern);

			pattern = "12:12:99.123456789+01";
			IsInvalid(pattern);

			pattern = "12:34:00.123123123/11";
			IsInvalid(pattern);

			pattern = "12:34:56.1234567";
			IsInvalid(pattern);

			pattern = "12:34:56.12345678";
			IsInvalid(pattern);

			pattern = "00:00:00.000000000-00";
			IsInvalid(pattern);
		}

		[Test]
		public virtual void TestFullTimeFullTz()
		{
			var pattern = "12:34:40.001+01:55";
			IsValid(pattern);

			pattern = "12:04:40.099+01:01";
			IsValid(pattern);

			pattern = "12:34:00.990-11:00";
			IsValid(pattern);
		}

		[Test]
		public virtual void TestFullTimeFullTzInvalid()
		{
			var pattern = "12:34:40.001+01:60";
			IsInvalid(pattern);

			pattern = "12:04:40.099/01:01";
			IsInvalid(pattern);

			pattern = "12:34:00.990-11:030";
			IsInvalid(pattern);

			pattern = "";
			IsInvalid(pattern);
		}

		[Test]
		public virtual void TestMicrosTimeFullTz()
		{
			var pattern = "12:59:59.999999+01:59";
			IsValid(pattern);

			pattern = "01:01:01.000001+01:01";
			IsValid(pattern);

			pattern = "12:30:00.990990-11:30";
			IsValid(pattern);
		}

		[Test]
		public virtual void TestFullMicrosFullTzInvalid()
		{
			var pattern = "12:34:56.123456+01:60";
			IsInvalid(pattern);

			pattern = "12:34:56.099099/01:01";
			IsInvalid(pattern);

			pattern = "12:34:56.123456-11:030";
			IsInvalid(pattern);

			pattern = "12:30:12.1111-11:59";
			IsInvalid(pattern);

			pattern = "12:30:12.11111-11:59";
			IsInvalid(pattern);
		}

		[Test]
		public virtual void TestNanosTimeFullTz()
		{
			var pattern = "01:01:01.000000001+01:55";
			IsValid(pattern);

			pattern = "11:12:13.999999999+01:01";
			IsValid(pattern);

			pattern = "12:13:14.123456789-11:30";
			IsValid(pattern);
		}

		[Test]
		public virtual void TestNanosTimeFullTzInvalid()
		{
			var pattern = "01:01:01.000000001+01:60";
			IsInvalid(pattern);

			pattern = "11:12:13.099099099/01:01";
			IsInvalid(pattern);

			pattern = "12:13:14.990990990-11:030";
			IsInvalid(pattern);

			pattern = "12:12:12.1234567-11:00";
			IsInvalid(pattern);

			pattern = "12:12:12.12345678+03:30";
			IsInvalid(pattern);
		}

		// YYYYMM
		// YYYYMMDD
		// YYYYMMWW
		// YYYY = 0000-9999
		// MM = 01-12, DD = 01-31
		// WW = w1, w2, w3, w4, w5

		[Test]
		public virtual void TestMonthYear44()
		{
			var value = "200101";
			ClassicAssert.IsTrue(!FixTypes.IsInvalidMonthYear44(value.AsByteArray()));

			value = "201003";
			ClassicAssert.IsTrue(!FixTypes.IsInvalidMonthYear44(value.AsByteArray()));

			value = "20100312";
			ClassicAssert.IsTrue(!FixTypes.IsInvalidMonthYear44(value.AsByteArray()));

			value = "201003w2";
			ClassicAssert.IsTrue(!FixTypes.IsInvalidMonthYear44(value.AsByteArray()));

			value = "200101w1";
			ClassicAssert.IsTrue(!FixTypes.IsInvalidMonthYear44(value.AsByteArray()));
		}

		[Test]
		public virtual void TestShortTime()
		{
			var value = "00:00:00";
			var calendar = new DateTimeOffset();
			calendar = FixTypes.ParseShortTime(value.AsByteArray());

			ClassicAssert.AreEqual(0, calendar.Hour);
			ClassicAssert.AreEqual(0, calendar.Minute);
			ClassicAssert.AreEqual(0, calendar.Second);

			value = "20:50:11";
			calendar = new DateTimeOffset();
			calendar = FixTypes.ParseShortTime(value.AsByteArray());

			ClassicAssert.AreEqual(20, calendar.Hour);
			ClassicAssert.AreEqual(50, calendar.Minute);
			ClassicAssert.AreEqual(11, calendar.Second);
		}

		private void IsValid(string pattern)
		{
			ClassicAssert.IsFalse(FixTypes.isInvalidTZTimeOnly(pattern.AsByteArray()));
		}

		private void IsInvalid(string pattern)
		{
			ClassicAssert.IsTrue(FixTypes.isInvalidTZTimeOnly(pattern.AsByteArray()));
		}

		[Test]
		public virtual void TestName()
		{
			var calendar = new DateTime(2013, 1, 12, 13, 30, 38, 45);

			var result = new byte[15];
			FixTypes.FormatBackupStorageTimestamp(result, calendar, 0, TimestampPrecision.Milli);
			ClassicAssert.AreEqual("130112133038045", StringHelper.NewString(result));

			result = new byte[18];
			FixTypes.FormatBackupStorageTimestamp(result, calendar, 12, TimestampPrecision.Micro);
			ClassicAssert.AreEqual("130112133038045012", StringHelper.NewString(result));

			result = new byte[21];
			FixTypes.FormatBackupStorageTimestamp(result, calendar, 1, TimestampPrecision.Nano);
			ClassicAssert.AreEqual("130112133038045000001", StringHelper.NewString(result));
		}

		[Test]
		public virtual void TestLeapSecondInTimestamp()
		{
			ClassicAssert.IsFalse(FixTypes.IsInvalidTimestamp("20060901-23:59:60.101".AsByteArray()));
			ClassicAssert.IsFalse(FixTypes.IsInvalidTimestamp("20060901-23:59:60.101202".AsByteArray()));
			ClassicAssert.IsFalse(FixTypes.IsInvalidTimestamp("20060901-23:59:60.101202303".AsByteArray()));
		}

		[Test]
		public virtual void TestLeapSecondInTzTimestamp()
		{
			ClassicAssert.IsFalse(FixTypes.IsInvalidTzTimestamp("20060901-23:59:60Z".AsByteArray()));
			ClassicAssert.IsFalse(FixTypes.IsInvalidTzTimestamp("20060901-23:59:60.101Z".AsByteArray()));
			ClassicAssert.IsFalse(FixTypes.IsInvalidTzTimestamp("20060901-23:59:60.101202Z".AsByteArray()));
			ClassicAssert.IsFalse(FixTypes.IsInvalidTzTimestamp("20060901-23:59:60.101202303Z".AsByteArray()));
			ClassicAssert.IsFalse(FixTypes.IsInvalidTzTimestamp("20060901-23:59:60+02".AsByteArray()));
			ClassicAssert.IsFalse(FixTypes.IsInvalidTzTimestamp("20060901-23:59:60+02:31".AsByteArray()));
			ClassicAssert.IsFalse(FixTypes.IsInvalidTzTimestamp("20060901-23:59:60.101+02".AsByteArray()));
			ClassicAssert.IsFalse(FixTypes.IsInvalidTzTimestamp("20060901-23:59:60.101+02:31".AsByteArray()));
			ClassicAssert.IsFalse(FixTypes.IsInvalidTzTimestamp("20060901-23:59:60.101202+02".AsByteArray()));
			ClassicAssert.IsFalse(FixTypes.IsInvalidTzTimestamp("20060901-23:59:60.101202+02:31".AsByteArray()));
			ClassicAssert.IsFalse(FixTypes.IsInvalidTzTimestamp("20060901-23:59:60.101202303+02".AsByteArray()));
			ClassicAssert.IsFalse(FixTypes.IsInvalidTzTimestamp("20060901-23:59:60.101202303+02:31".AsByteArray()));
		}

		[Test]
		public virtual void testLeapSecondInTimeOnly()
		{
			ClassicAssert.IsFalse(FixTypes.isInvalidTimeOnly("23:59:60.101".AsByteArray()));
			ClassicAssert.IsFalse(FixTypes.isInvalidTimeOnly("23:59:60.101202".AsByteArray()));
			ClassicAssert.IsFalse(FixTypes.isInvalidTimeOnly("23:59:60.101202303".AsByteArray()));
		}

		[Test]
		public virtual void testLeapSecondInTZTimeOnly()
		{
			ClassicAssert.IsFalse(FixTypes.isInvalidTZTimeOnly("23:59:60Z".AsByteArray()));
			ClassicAssert.IsFalse(FixTypes.isInvalidTZTimeOnly("23:59:60.101Z".AsByteArray()));
			ClassicAssert.IsFalse(FixTypes.isInvalidTZTimeOnly("23:59:60.101202Z".AsByteArray()));
			ClassicAssert.IsFalse(FixTypes.isInvalidTZTimeOnly("23:59:60.101202303Z".AsByteArray()));
			ClassicAssert.IsFalse(FixTypes.isInvalidTZTimeOnly("23:59:60+02".AsByteArray()));
			ClassicAssert.IsFalse(FixTypes.isInvalidTZTimeOnly("23:59:60+02:31".AsByteArray()));
			ClassicAssert.IsFalse(FixTypes.isInvalidTZTimeOnly("23:59:60.101+02".AsByteArray()));
			ClassicAssert.IsFalse(FixTypes.isInvalidTZTimeOnly("23:59:60.101+02:31".AsByteArray()));
			ClassicAssert.IsFalse(FixTypes.isInvalidTZTimeOnly("23:59:60.101202+02".AsByteArray()));
			ClassicAssert.IsFalse(FixTypes.isInvalidTZTimeOnly("23:59:60.101202+02:31".AsByteArray()));
			ClassicAssert.IsFalse(FixTypes.isInvalidTZTimeOnly("23:59:60.101202303+02".AsByteArray()));
			ClassicAssert.IsFalse(FixTypes.isInvalidTZTimeOnly("23:59:60.101202303+02:31".AsByteArray()));
		}

		[Test]
		public virtual void TestLeapSecondInTime()
		{
			ClassicAssert.IsFalse(FixTypes.IsInvalidTime("20060901-23:59:60".AsByteArray()));
		}

		[Test]
		public virtual void TestParseLeapSecondInTimestamp()
		{
			var c = new DateTime();
			c = FixTypes.ParseTimestamp("20060901-23:59:60".AsByteArray());
			ClassicAssert.AreEqual(0, c.Hour);
			ClassicAssert.AreEqual(0, c.Minute);
			ClassicAssert.AreEqual(0, c.Second);
		}

		[Test]
		public virtual void testParseLeapSecondInTimeOnly()
		{
			var c = new DateTime();
			c = FixTypes.parseTimeOnly("23:59:60".AsByteArray());
			ClassicAssert.AreEqual(0, c.Hour);
			ClassicAssert.AreEqual(0, c.Minute);
			ClassicAssert.AreEqual(0, c.Second);
		}

		[Test]
		public virtual void testParseLeapSecondInTZTimeOnly()
		{
			var c = FixTypes.parseTZTimeOnly("23:59:60Z".AsByteArray());
			ClassicAssert.AreEqual(0, c.Hour);
			ClassicAssert.AreEqual(0, c.Minute);
			ClassicAssert.AreEqual(0, c.Second);
		}

		[Test]
		public virtual void TestParseLeapSecondInTime()
		{
			var c = FixTypes.ParseTime("20060901-23:59:60".AsByteArray());
			ClassicAssert.AreEqual(0, c.Hour);
			ClassicAssert.AreEqual(0, c.Minute);
			ClassicAssert.AreEqual(0, c.Second);
		}

		[Test]
		public virtual void TestParseLeapSecondInShortTime()
		{
			DateTimeOffset c = FixTypes.ParseShortTime("23:59:60".AsByteArray());
			ClassicAssert.AreEqual(0, c.Hour);
			ClassicAssert.AreEqual(0, c.Minute);
			ClassicAssert.AreEqual(0, c.Second);
		}
	}
}

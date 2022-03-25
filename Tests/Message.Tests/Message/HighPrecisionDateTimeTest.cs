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
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework;

namespace Epam.FixAntenna.Message.Tests
{
	[TestFixture]
	internal class HighPrecisionDateTimeTest
	{
		private const string StorageFormatMilli = "yyyyMMdd HH:mm:ss.fff - ";
		private const string StorageFormatMicro = "yyyyMMdd HH:mm:ss.ffffff - ";
		private const string StorageFormatNano = "yyyyMMdd HH:mm:ss.fffffff00 - ";

		private static string LocalTz => TimeZoneInfo.Local.GetUtcOffset(DateTime.MinValue.ToUniversalTime()) == TimeSpan.Zero ? "Z" : DateTime.Now.ToString("%K");

		private void AssertTzTimeWithoutTz(string time, string expectedTime, TimestampPrecision precision)
		{
			var buffer = time.AsByteArray();
			var res = HighPrecisionDateTimeParsers.parseTZTimeOnly(buffer, 0, buffer.Length);
			Assert.AreEqual(expectedTime, res.ToTzUniversalString(precision));
		}

		private void AssertTzTimes(string[] times, TimestampPrecision precision)
		{
			for (var i = 0; i < times.Length; i++)
			{
				var time = times[i];
				var buffer = time.AsByteArray();
				var res = HighPrecisionDateTimeParsers.parseTZTimeOnly(buffer, 0, buffer.Length);
				HighPrecisionDateTimeFormatters.formatTZTimeOnly(buffer, res, precision);
				Assert.AreEqual(time, StringHelper.NewString(buffer));
			}
		}

		private static void AssertTzTimestampWihoutTz(string time, string expectedTime, TimestampPrecision precision)
		{
			var buffer = time.AsByteArray();
			var res = HighPrecisionDateTimeParsers.ParseTzTimestamp(buffer, 0, buffer.Length);
			Assert.AreEqual(expectedTime, res.ToTzUniversalString(precision));
		}

		private void AssertTzTimestamps(string[] timestamps, string[] expectedResults, TimestampPrecision precision)
		{
			for (var i = 0; i < timestamps.Length; i++)
			{
				AssertTzTimestamp(timestamps[i], expectedResults[i], precision);
			}
		}

		private void AssertTzTimestamp(string timestamp, string expectedResult, TimestampPrecision precision)
		{
			var buffer = timestamp.AsByteArray();
			var res = HighPrecisionDateTimeParsers.ParseTzTimestamp(buffer, 0, buffer.Length);
			HighPrecisionDateTimeFormatters.FormatTzTimestamp(buffer, res, precision);
			Assert.AreEqual(expectedResult, StringHelper.NewString(buffer));
		}

		[Test]
		public virtual void TestFormatStorageTimestamp()
		{
			var dateTime =
				new DateTimeOffset(2016, 11, 15, 01, 02, 03, TimeSpan.Zero)
					.AddTicks(1234567);
			var dateTimeResult = dateTime.ToString(StorageFormatMilli);
			var buffer = new byte[24];
			HighPrecisionDateTimeFormatters.FormatStorageTimestamp(buffer, 0, dateTime, TimestampPrecision.Milli);
			Assert.AreEqual(dateTimeResult, StringHelper.NewString(buffer));

			dateTime = new DateTimeOffset(2016, 11, 15, 01, 02, 03, TimeSpan.FromMinutes(-330)).AddTicks(1234567);
			dateTimeResult = dateTime.ToString(StorageFormatMicro);
			buffer = new byte[27];
			HighPrecisionDateTimeFormatters.FormatStorageTimestamp(buffer, 0, dateTime, TimestampPrecision.Micro);
			Assert.AreEqual(dateTimeResult, StringHelper.NewString(buffer));

			dateTime = new DateTimeOffset(2016, 11, 15, 01, 02, 03, TimeSpan.FromMinutes(330)).AddTicks(1234567);
			dateTimeResult = dateTime.ToString(StorageFormatNano);
			buffer = new byte[30];
			HighPrecisionDateTimeFormatters.FormatStorageTimestamp(buffer, 0, dateTime, TimestampPrecision.Nano);
			Assert.AreEqual(dateTimeResult, StringHelper.NewString(buffer));
		}

		[Test]
		public virtual void TestParseLeapSecondInHighPrecisionTimeOnly()
		{
			var dateTime = HighPrecisionDateTimeParsers.parseTimeOnly("23:59:60.000000000".AsByteArray());
			Assert.AreEqual(0, dateTime.Hour);
			Assert.AreEqual(0, dateTime.Minute);
			Assert.AreEqual(0, dateTime.Second);
			Assert.AreEqual(0, dateTime.GetNanosecondsOfSecond());
		}

		[Test]
		public virtual void TestParseLeapSecondInHighPrecisionTimestamp()
		{
			var dateTime = HighPrecisionDateTimeParsers.ParseTimestamp("20000630-23:59:60.000000000".AsByteArray());
			Assert.AreEqual(2000, dateTime.Year);
			Assert.AreEqual(7, dateTime.Month);
			Assert.AreEqual(1, dateTime.Day);
			Assert.AreEqual(0, dateTime.Hour);
			Assert.AreEqual(0, dateTime.Minute);
			Assert.AreEqual(0, dateTime.Second);
			Assert.AreEqual(0, dateTime.GetNanosecondsOfSecond());
		}

		[Test]
		public virtual void TestParseLeapSecondInHighPrecisionTZTimeOnly()
		{
			var buffer = "23:59:60.000000000Z".AsByteArray();
			var time = HighPrecisionDateTimeParsers.parseTZTimeOnly(buffer, 0, buffer.Length);
			Assert.AreEqual(0, time.Hour);
			Assert.AreEqual(0, time.Minute);
			Assert.AreEqual(0, time.Second);
		}

		[Test]
		public virtual void TestParseLeapSecondInHighPrecisionTzTimestamp()
		{
			var buffer = "20001231-23:59:60.000000000+05:30".AsByteArray();
			var dateTime = HighPrecisionDateTimeParsers.ParseTzTimestamp(buffer, 0, buffer.Length);
			Assert.AreEqual(2001, dateTime.Year);
			Assert.AreEqual(1, dateTime.Month);
			Assert.AreEqual(1, dateTime.Day);
			Assert.AreEqual(0, dateTime.Hour);
			Assert.AreEqual(0, dateTime.Minute);
			Assert.AreEqual(0, dateTime.Second);
			Assert.AreEqual(0, dateTime.GetNanosecondsOfSecond());
		}

		[Test]
		public virtual void TestParseTimeOnly()
		{
			var expectedValue = "13:39:20";
			var time = HighPrecisionDateTimeParsers.parseTimeOnly(expectedValue.AsByteArray());
			var buffer = new byte[8];
			HighPrecisionDateTimeFormatters.formatTimeOnly(buffer, time.DateTime, TimestampPrecision.Second);
			var actualResult = StringHelper.NewString(buffer);
			Assert.AreEqual(expectedValue, actualResult);

			expectedValue = "13:39:20.001";
			time = HighPrecisionDateTimeParsers.parseTimeOnly(expectedValue.AsByteArray());
			buffer = new byte[12];
			HighPrecisionDateTimeFormatters.formatTimeOnly(buffer, time.DateTime, TimestampPrecision.Milli);
			actualResult = StringHelper.NewString(buffer);
			Assert.AreEqual(expectedValue, actualResult);

			expectedValue = "13:39:20.000001";
			time = HighPrecisionDateTimeParsers.parseTimeOnly(expectedValue.AsByteArray());
			buffer = new byte[15];
			HighPrecisionDateTimeFormatters.formatTimeOnly(buffer, time.DateTime, TimestampPrecision.Micro);
			actualResult = StringHelper.NewString(buffer);
			Assert.AreEqual(expectedValue, actualResult);

			var value = "13:39:20.000000101";
			expectedValue = "13:39:20.000000100";
			time = HighPrecisionDateTimeParsers.parseTimeOnly(value.AsByteArray());
			buffer = new byte[18];
			HighPrecisionDateTimeFormatters.formatTimeOnly(buffer, time.DateTime, TimestampPrecision.Nano);
			actualResult = StringHelper.NewString(buffer);
			Assert.AreEqual(expectedValue, actualResult);

			expectedValue = "13:39:20.123";
			time = HighPrecisionDateTimeParsers.parseTimeOnly(expectedValue.AsByteArray());
			buffer = new byte[12];
			HighPrecisionDateTimeFormatters.formatTimeOnly(buffer, time.DateTime, TimestampPrecision.Milli);
			actualResult = StringHelper.NewString(buffer);
			Assert.AreEqual(expectedValue, actualResult);

			expectedValue = "13:39:20.001001";
			time = HighPrecisionDateTimeParsers.parseTimeOnly(expectedValue.AsByteArray());
			buffer = new byte[15];
			HighPrecisionDateTimeFormatters.formatTimeOnly(buffer, time.DateTime, TimestampPrecision.Micro);
			actualResult = StringHelper.NewString(buffer);
			Assert.AreEqual(expectedValue, actualResult);

			value = "13:39:20.123456789";
			expectedValue = "13:39:20.123456800";
			time = HighPrecisionDateTimeParsers.parseTimeOnly(value.AsByteArray());
			buffer = new byte[18];
			HighPrecisionDateTimeFormatters.formatTimeOnly(buffer, time.DateTime, TimestampPrecision.Nano);
			actualResult = StringHelper.NewString(buffer);
			Assert.AreEqual(expectedValue, actualResult);
		}

		[Test]
		public virtual void TestParseTimeOnlyWithPartialFractions()
		{
			var expectedValue = "13:39:20.1";
			var time = HighPrecisionDateTimeParsers.parseTimeOnly(expectedValue.AsByteArray());
			Assert.AreEqual("0001-01-01T13:39:20.100", time.ToUniversalString(TimestampPrecision.Milli));

			expectedValue = "13:39:20.0001";
			time = HighPrecisionDateTimeParsers.parseTimeOnly(expectedValue.AsByteArray());
			Assert.AreEqual("0001-01-01T13:39:20.000100", time.ToUniversalString(TimestampPrecision.Micro));

			expectedValue = "13:39:20.0000001";
			time = HighPrecisionDateTimeParsers.parseTimeOnly(expectedValue.AsByteArray());
			Assert.AreEqual("0001-01-01T13:39:20.000000100", time.ToUniversalString(TimestampPrecision.Nano));
		}

		[Test]
		public virtual void TestParseTimestamp()
		{
			var expectedValue = "20100218-13:39:20";
			var dateTime = HighPrecisionDateTimeParsers.ParseTimestamp(expectedValue.AsByteArray());
			var buffer = new byte[17];
			HighPrecisionDateTimeFormatters.FormatTimestamp(buffer, dateTime, TimestampPrecision.Second);
			var actualResult = StringHelper.NewString(buffer);
			Assert.AreEqual(expectedValue, actualResult);

			expectedValue = "20100218-13:39:20.001";
			dateTime = HighPrecisionDateTimeParsers.ParseTimestamp(expectedValue.AsByteArray());
			buffer = new byte[21];
			HighPrecisionDateTimeFormatters.FormatTimestamp(buffer, dateTime, TimestampPrecision.Milli);
			actualResult = StringHelper.NewString(buffer);
			Assert.AreEqual(expectedValue, actualResult);

			expectedValue = "20100218-13:39:20.000001";
			dateTime = HighPrecisionDateTimeParsers.ParseTimestamp(expectedValue.AsByteArray());
			buffer = new byte[24];
			HighPrecisionDateTimeFormatters.FormatTimestamp(buffer, dateTime, TimestampPrecision.Micro);
			actualResult = StringHelper.NewString(buffer);
			Assert.AreEqual(expectedValue, actualResult);

			var value = "20100218-13:39:20.000000101";
			expectedValue = "20100218-13:39:20.000000100";
			dateTime = HighPrecisionDateTimeParsers.ParseTimestamp(value.AsByteArray());
			buffer = new byte[27];
			HighPrecisionDateTimeFormatters.FormatTimestamp(buffer, dateTime, TimestampPrecision.Nano);
			actualResult = StringHelper.NewString(buffer);
			Assert.AreEqual(expectedValue, actualResult);

			expectedValue = "20100218-13:39:20.123";
			dateTime = HighPrecisionDateTimeParsers.ParseTimestamp(expectedValue.AsByteArray());
			buffer = new byte[21];
			HighPrecisionDateTimeFormatters.FormatTimestamp(buffer, dateTime, TimestampPrecision.Milli);
			actualResult = StringHelper.NewString(buffer);
			Assert.AreEqual(expectedValue, actualResult);

			expectedValue = "20100218-13:39:20.001001";
			dateTime = HighPrecisionDateTimeParsers.ParseTimestamp(expectedValue.AsByteArray());
			buffer = new byte[24];
			HighPrecisionDateTimeFormatters.FormatTimestamp(buffer, dateTime, TimestampPrecision.Micro);
			actualResult = StringHelper.NewString(buffer);
			Assert.AreEqual(expectedValue, actualResult);

			value = "20100218-13:39:20.123456789";
			expectedValue = "20100218-13:39:20.123456800";
			dateTime = HighPrecisionDateTimeParsers.ParseTimestamp(value.AsByteArray());
			buffer = new byte[27];
			HighPrecisionDateTimeFormatters.FormatTimestamp(buffer, dateTime, TimestampPrecision.Nano);
			actualResult = StringHelper.NewString(buffer);
			Assert.AreEqual(expectedValue, actualResult);
		}

		[Test]
		public virtual void TestParseTimestampWithPartialFractions()
		{
			var expectedValue = "20100218-13:39:20.1";
			var dateTime = HighPrecisionDateTimeParsers.ParseTimestamp(expectedValue.AsByteArray());
			Assert.AreEqual("2010-02-18T13:39:20.100", dateTime.ToUniversalString(TimestampPrecision.Milli));

			expectedValue = "20100218-13:39:20.0001";
			dateTime = HighPrecisionDateTimeParsers.ParseTimestamp(expectedValue.AsByteArray());
			Assert.AreEqual("2010-02-18T13:39:20.000100", dateTime.ToUniversalString(TimestampPrecision.Micro));

			expectedValue = "20100218-13:39:20.0000001";
			dateTime = HighPrecisionDateTimeParsers.ParseTimestamp(expectedValue.AsByteArray());
			Assert.AreEqual("2010-02-18T13:39:20.000000100", dateTime.ToUniversalString(TimestampPrecision.Nano));
		}

		[Test]
		public virtual void TestParseTZTimeOnlyNanoPrecision()
		{
			var times = new[]
				{ "07:39:32.001234567Z", "07:39:32.001234567-05", "07:39:32.001234467+05", "07:39:32.001234767+05:30" };
			var expectedResults = new[]
				{ "07:39:32.001234600Z", "07:39:32.001234600-05", "07:39:32.001234500+05", "07:39:32.001234800+05:30" };
			for (var i = 0; i < times.Length; i++)
			{
				var time = times[i];
				var buffer = time.AsByteArray();
				var res = HighPrecisionDateTimeParsers.parseTZTimeOnly(buffer, 0, buffer.Length);
				HighPrecisionDateTimeFormatters.formatTZTimeOnly(buffer, res, TimestampPrecision.Nano);
				Assert.AreEqual(expectedResults[i], StringHelper.NewString(buffer));
			}
		}

		[Test]
		public virtual void TestParseTZTimeOnlyWithoutNano()
		{
			string[] times = { "07:39Z", "07:39-05", "07:39+05", "07:39+05:30" };
			AssertTzTimes(times, TimestampPrecision.Minute);

			times = new[] { "07:39:32Z", "07:39:32-05", "07:39:32+05", "07:39:32+05:30" };
			AssertTzTimes(times, TimestampPrecision.Second);

			times = new[] { "07:39:32.001Z", "07:39:32.001-05", "07:39:32.001+05", "07:39:32.001+05:30" };
			AssertTzTimes(times, TimestampPrecision.Milli);

			times = new[] { "07:39:32.001234Z", "07:39:32.001234-05", "07:39:32.001234+05", "07:39:32.001234+05:30" };
			AssertTzTimes(times, TimestampPrecision.Micro);
		}

		[Test]
		public virtual void TestParseTZTimeOnlyWithPartialFractions()
		{
			var expectedValue = "13:39:20.1Z";
			var time = HighPrecisionDateTimeParsers.parseTZTimeOnly(expectedValue.AsByteArray());
			Assert.AreEqual("0001-01-01T13:39:20.100", time.ToUniversalString(TimestampPrecision.Milli));

			expectedValue = "13:39:20.0001Z";
			time = HighPrecisionDateTimeParsers.parseTZTimeOnly(expectedValue.AsByteArray());
			Assert.AreEqual("0001-01-01T13:39:20.000100", time.ToUniversalString(TimestampPrecision.Micro));

			expectedValue = "13:39:20.0000001Z";
			time = HighPrecisionDateTimeParsers.parseTZTimeOnly(expectedValue.AsByteArray());
			Assert.AreEqual("0001-01-01T13:39:20.000000100", time.ToUniversalString(TimestampPrecision.Nano));
		}

		[Test]
		public virtual void TestParseTzTimestampNanoPrecision()
		{
			var timestamps = new[]
			{
				"20060901-07:39:32.123456789Z", "20060901-07:39:32.123456789-05", "20060901-07:39:32.123456789+05",
				"20060901-07:39:32.123456789+05:30"
			};
			var expectedResults = new[]
			{
				"20060901-07:39:32.123456800Z", "20060901-07:39:32.123456800-05", "20060901-07:39:32.123456800+05",
				"20060901-07:39:32.123456800+05:30"
			};
			AssertTzTimestamps(timestamps, expectedResults, TimestampPrecision.Nano);
		}

		[Test]
		public virtual void TestParseTzTimestampWithoutNanoPrecision()
		{
			string[] timestamps = { "20060901-07:39Z", "20060901-07:39+05", "20060901-07:39+05:30" };
			AssertTzTimestamps(timestamps, timestamps, TimestampPrecision.Minute);

			timestamps = new[]
				{ "20060901-07:39:32Z", "20060901-07:39:32-05", "20060901-07:39:32+05", "20060901-07:39:32+05:30" };
			AssertTzTimestamps(timestamps, timestamps, TimestampPrecision.Second);

			timestamps = new[]
			{
				"20060901-07:39:32.123Z", "20060901-07:39:32.123-05", "20060901-07:39:32.123+05",
				"20060901-07:39:32.123+05:30"
			};
			AssertTzTimestamps(timestamps, timestamps, TimestampPrecision.Milli);

			timestamps = new[]
			{
				"20060901-07:39:32.123456Z", "20060901-07:39:32.123456-05", "20060901-07:39:32.123456+05",
				"20060901-07:39:32.123456+05:30"
			};
			AssertTzTimestamps(timestamps, timestamps, TimestampPrecision.Micro);
		}

		[Test]
		public virtual void TestParseTzTimestampWithoutTz()
		{
			AssertTzTimestampWihoutTz("20150701-07:39", "2015-07-01T07:39" + LocalTz, TimestampPrecision.Minute);
			AssertTzTimestampWihoutTz("20150701-07:39:32", "2015-07-01T07:39:32" + LocalTz, TimestampPrecision.Second);
			AssertTzTimestampWihoutTz("20150701-07:39:32.123", "2015-07-01T07:39:32.123" + LocalTz, TimestampPrecision.Milli);
			AssertTzTimestampWihoutTz("20150701-07:39:32.123456", "2015-07-01T07:39:32.123456" + LocalTz, TimestampPrecision.Micro);
			AssertTzTimestampWihoutTz("20150701-07:39:32.123456700", "2015-07-01T07:39:32.123456700" + LocalTz, TimestampPrecision.Nano);
		}

		[Test]
		public virtual void TestParseTzTimestampWithoutTzWithPartialFractions()
		{
			var expectedValue = "20100218-13:39:20.1";
			var dateTime = HighPrecisionDateTimeParsers.ParseTzTimestamp(expectedValue.AsByteArray());
			Assert.AreEqual("2010-02-18T13:39:20.100" + LocalTz, dateTime.ToTzUniversalString(TimestampPrecision.Milli));

			expectedValue = "20100218-13:39:20.0001";
			dateTime = HighPrecisionDateTimeParsers.ParseTzTimestamp(expectedValue.AsByteArray());
			Assert.AreEqual("2010-02-18T13:39:20.000100" + LocalTz, dateTime.ToTzUniversalString(TimestampPrecision.Micro));

			expectedValue = "20100218-13:39:20.0000001";
			dateTime = HighPrecisionDateTimeParsers.ParseTzTimestamp(expectedValue.AsByteArray());
			Assert.AreEqual("2010-02-18T13:39:20.000000100" + LocalTz, dateTime.ToTzUniversalString(TimestampPrecision.Nano));
		}

		[Test]
		public virtual void TestParseTzTimestampWithPartialFractions()
		{
			var expectedValue = "20100218-13:39:20.1Z";
			var dateTime = HighPrecisionDateTimeParsers.ParseTzTimestamp(expectedValue.AsByteArray());
			Assert.AreEqual("2010-02-18T13:39:20.100Z", dateTime.ToTzUniversalString(TimestampPrecision.Milli));

			expectedValue = "20100218-13:39:20.0001Z";
			dateTime = HighPrecisionDateTimeParsers.ParseTzTimestamp(expectedValue.AsByteArray());
			Assert.AreEqual("2010-02-18T13:39:20.000100Z", dateTime.ToTzUniversalString(TimestampPrecision.Micro));

			expectedValue = "20100218-13:39:20.0000001Z";
			dateTime = HighPrecisionDateTimeParsers.ParseTzTimestamp(expectedValue.AsByteArray());
			Assert.AreEqual("2010-02-18T13:39:20.000000100Z", dateTime.ToTzUniversalString(TimestampPrecision.Nano));
		}

		[Test]
		public virtual void TestParseTzTimeWithoutTz()
		{
			AssertTzTimeWithoutTz("07:39", "0001-01-01T07:39" + LocalTz, TimestampPrecision.Minute);
			AssertTzTimeWithoutTz("07:39:32", "0001-01-01T07:39:32" + LocalTz, TimestampPrecision.Second);
			AssertTzTimeWithoutTz("07:39:32.123", "0001-01-01T07:39:32.123" + LocalTz, TimestampPrecision.Milli);
			AssertTzTimeWithoutTz("07:39:32.123456", "0001-01-01T07:39:32.123456" + LocalTz, TimestampPrecision.Micro);
			AssertTzTimeWithoutTz("07:39:32.123456789", "0001-01-01T07:39:32.123456800" + LocalTz, TimestampPrecision.Nano);
		}

		[Test]
		public virtual void TestParseTzTimeWithoutTzWithPartialFractions()
		{
			var expectedValue = "13:39:20.1";
			var time = HighPrecisionDateTimeParsers.parseTZTimeOnly(expectedValue.AsByteArray());
			Assert.AreEqual("0001-01-01T13:39:20.100" + LocalTz, time.ToTzUniversalString(TimestampPrecision.Milli));

			expectedValue = "13:39:20.0001";
			time = HighPrecisionDateTimeParsers.parseTZTimeOnly(expectedValue.AsByteArray());
			Assert.AreEqual("0001-01-01T13:39:20.000100" + LocalTz, time.ToTzUniversalString(TimestampPrecision.Micro));

			expectedValue = "13:39:20.0000001";
			time = HighPrecisionDateTimeParsers.parseTZTimeOnly(expectedValue.AsByteArray());
			Assert.AreEqual("0001-01-01T13:39:20.000000100" + LocalTz,
				time.ToTzUniversalString(TimestampPrecision.Nano));
		}
	}
}
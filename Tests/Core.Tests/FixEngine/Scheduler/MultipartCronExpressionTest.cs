// Copyright (c) 2022 EPAM Systems
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
using Epam.FixAntenna.NetCore.FixEngine.Scheduler;
using NUnit.Framework;
using Quartz;

namespace Epam.FixAntenna.NetCore.FixEngine.Scheduler
{
	internal class MultipartCronExpressionTest
	{
		private static readonly TimeZoneInfo CustomTimeZone = TimeZoneInfo.CreateCustomTimeZone("Test", TimeSpan.FromHours(3), "", "");

		private static readonly object[] TestCases =
		{
			// different time zones
			new object[] { "12/07/2022 4:33:01 +0", "0 33 4 * * ?", TimeZoneInfo.Utc, "12/07/2022 4:33:00 +0" },
			new object[] { "12/07/2022 4:33:01 +0", "0 33 4 * * ?", CustomTimeZone, "12/07/2022 1:33:00 +0" },

			// prediction for periodic cron expression
			new object[] { "12/07/2022 4:33:01 +0", "0/3 * * * * ?", TimeZoneInfo.Utc, "12/07/2022 4:33:00 +0" },
			new object[] { "12/07/2022 4:33:02 +0", "0/3 * * * * ?", TimeZoneInfo.Utc, "12/07/2022 4:33:00 +0" },
			new object[] { "12/07/2022 4:33:03 +0", "0/3 * * * * ?", TimeZoneInfo.Utc, "12/07/2022 4:33:00 +0" },
			new object[] { "12/07/2022 4:33:04 +0", "0/3 * * * * ?", TimeZoneInfo.Utc, "12/07/2022 4:33:03 +0" },

			// more complicated expressions
			new object[] { "12/07/2022 4:33:04 +0", "0 0 2,3 * * ?", TimeZoneInfo.Utc, "12/07/2022 3:00:00 +0" },
			new object[] { "12/07/2022 4:33:04 +0", "0 * 2,3 * * ?", TimeZoneInfo.Utc, "12/07/2022 3:59:00 +0" },
			new object[] { "12/07/2022 4:33:04 +0", "0 * 2,3 3/7 * ?", TimeZoneInfo.Utc, "10/07/2022 3:59:00 +0" },
			new object[] { "12/07/2022 4:33:04 +0", "4,9,30 * 2,3 3/7 * ?", TimeZoneInfo.Utc, "10/07/2022 3:59:30 +0" },
			new object[] { "12/07/2022 4:33:04 +0", "* 10-15 2,3 3/7 * ?", TimeZoneInfo.Utc, "10/07/2022 3:15:59 +0" },
			new object[] { "10/07/2022 2:11:00 +0", "* 10-15 2,3 3/7 * ?", TimeZoneInfo.Utc, "10/07/2022 2:10:59 +0" },

			// expressions within a year
			new object[] { "12/07/2022 4:33:04 +0", "0 0 1 2 3 ? 2023", TimeZoneInfo.Utc, null },
			new object[] { "12/07/2022 4:33:04 +0", "0 * 1 2 3 ? 2023", TimeZoneInfo.Utc, null },
			new object[] { "12/07/2022 4:33:04 +0", "0 0 1 2 3 ? 2020", TimeZoneInfo.Utc, "2/3/2020 1:0:0 +0" },
			new object[] { "12/07/2022 4:33:04 +0", "0 0 1 2 3 ? 1975", TimeZoneInfo.Utc, "2/3/1975 1:0:0 +0" },
			new object[] { "12/07/2022 4:33:04 +0", "0 * 1 2 3 ? 1975", TimeZoneInfo.Utc, "2/3/1975 1:59:0 +0" },

			// piped cron expressions
			new object[] { "12/07/2022 4:33:01 +0", "0 33 4 * * ?|0 32 4 * * ?", TimeZoneInfo.Utc, "12/07/2022 4:33:00 +0" },
			new object[] { "12/07/2022 4:33:01 +0", "0 32 4 * * ?|0 33 4 * * ?", TimeZoneInfo.Utc, "12/07/2022 4:33:00 +0" },
		};

		[TestCaseSource(nameof(TestCases))]
		public void TestGetTimeBefore(string dateString, string pipedCronExpressionString, TimeZoneInfo timeZone, string expectedString)
		{
			// Arrange
			var dateFormat = "d/M/yyyy H:m:s z";
			var date = DateTimeOffset.ParseExact(dateString, dateFormat, null);
			var cronExpression = new MultipartCronExpression(pipedCronExpressionString, timeZone);
			var expected = expectedString == null ? (DateTimeOffset?)null : DateTimeOffset.ParseExact(expectedString, dateFormat, null);

			// Act
			var actual = cronExpression.GetTimeBefore(date);

			// Assert
			Assert.AreEqual(expected, actual, "Wrong behaviour.");
		}

		[TestCase("0 32 4 * * ?", true)]
		[TestCase("0| 32 4 * * ?", false)]
		[TestCase("0 32 4 * * ?|0 32 4 * * ?  | 0 32 4 * * ? ", true)]
		public void TestIsValidCronExpression(string pipedCronExpression, bool expectedResult)
		{
			// Act
			var actual = MultipartCronExpression.IsValidCronExpression(pipedCronExpression);

			// Assert
			Assert.AreEqual(expectedResult, actual);
		}

		[TestCase("0 32 4 * * ?", "0 32 4 * * ?")]
		[TestCase("0 32 4 * * ?|0 33 4 * * ?", "0 32 4 * * ?", "0 33 4 * * ?")]
		[TestCase("0 32 4 * * ?|0 33 4 * * ?|0 34 4 * * ?", "0 32 4 * * ?", "0 33 4 * * ?", "0 34 4 * * ?")]
		public void TestExtractCronExpressions(string pipedCronExpression, params string[] expected)
		{
			// Act
			var actual = MultipartCronExpression.ExtractCronExpressions(pipedCronExpression);

			// Assert
			Assert.AreEqual(expected, actual);
		}

		[TestCase("12/07/2022 4:33:01 +0", "1 33 4 * * ?", true)]
		[TestCase("12/07/2022 4:33:01 +0", "0 32 4 * * ?", false)]
		[TestCase("12/07/2022 4:33:01 +0", "0 32 4 * * ?|1 33 4 * * ?", true)]
		[TestCase("12/07/2022 4:33:01 +0", "0 32 4 * * ?|0 33 4 * * ?", false)]
		[TestCase("12/07/2022 4:33:01 +1", "1 33 4 * * ?", true)]
		public void TestIsSatisfiedBy(string dateString, string pipedCronExpressionString, bool expectedResult)
		{
			// Assert
			var dateFormat = "d/M/yyyy H:m:s z";
			var date = DateTimeOffset.ParseExact(dateString, dateFormat, null);
			var timeZone = TimeZoneInfo.CreateCustomTimeZone("Test TZ", date.Offset, "", "");
			var cronExpression = new MultipartCronExpression(pipedCronExpressionString, timeZone);

			// Act
			var actual = cronExpression.IsSatisfiedBy(date);

			// Assert
			Assert.AreEqual(expectedResult, actual);
		}
	}
}

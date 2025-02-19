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
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.FixEngine.Scheduler;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Scheduler
{
	internal class ScheduleTest
	{
		private static readonly TimeSpan TenMinutes = TimeSpan.FromMinutes(10);

		private static readonly object[] TestCasesToTestIsInsideInterval =
		{
			new object[] { "12/07/2022 4:33 +1", "0 0 4 * * ?", "0 0 5 * * ?", true },
			new object[] { "12/07/2022 4:33 +1", "0 0 6 * * ?", "0 0 5 * * ?", true },
			new object[] { "12/07/2022 4:33 +1", "0 0 4 * * ?", "0 30 4 * * ?", false },
			new object[] { "12/07/2022 4:33 +1", "0 0 6 * * ?", "0 0 13 * * ?", false },
			new object[] { "12/07/2022 4:33 +1", "0/4 0 6 * * ?", "1/4 0 6 * * ?", false },
			new object[] { "12/07/2022 4:33 +1", "0 0/4 4 * * ?", "0 1/4 6 * * ?", true },

			new object[] { "12/07/2022 4:33 +1", "0 33 4 * * ?", "0 34 4 * * ?", false },
			new object[] { "12/07/2022 4:33 +1", "0 32 4 * * ?", "0 33 4 * * ?", false },

			new object[] { "12/07/2022 4:33 +2", "0 0 4 * * ?", "0 0 5 * * ?", true },
			new object[] { "12/07/2022 4:33 +2", "0 0 4 * * ?", "0 30 4 * * ?", false },

			new object[] { "12/07/2022 4:33 +1", "0 0 4 * * ? 2021", "0 0 5 * * ? 2021", false },
			new object[] { "12/07/2022 4:33 +1", "0 0 4 * * ? 2023", "0 0 5 * * ? 2023", false },
			new object[] { "12/07/2022 4:33 +1", "0 0 4 * * ? 2022", "0 0 5 * * ? 2022", true },
			new object[] { "12/07/2022 4:33 +1", "0 0 4 * * ? 2022", "0 0 5 9 10 ? 2022", true },
			new object[] { "12/07/2022 4:33 +1", "0 0 4 5 6 ? 2022", "0 0 5 9 10 ? 2022", true },
			new object[] { "12/07/2022 4:33 +1", "0 32,33 4 12 7 ? 2022", "0 0 5 9 10 ? 2022", false },

			new object[] { "12/07/2022 4:33 +1", "0 0 3 * * ?|0 0 4 * * ?", "0 0 5 * * ?|0 0 6 * * ?", true },
		};

		[TestCaseSource(nameof(TestCasesToTestIsInsideInterval))]
		public void TestIsInsideInterval(string dateString, string startTimeExpr, string stopTimeExpr, bool expected)
		{
			// Arrange
			var date = DateTimeOffset.ParseExact(dateString, "d/M/yyyy H:m z", null);
			var timeZone = TimeZoneInfo.CreateCustomTimeZone("Test", date.Offset, "", "");
			var schedule = new Schedule(startTimeExpr, stopTimeExpr, timeZone);

			// Act
			var actual = schedule.IsInsideInterval(date);

			// ClassicAssert
			ClassicAssert.AreEqual(expected, actual, $"Wrong behaviour. {dateString}, {startTimeExpr}, {stopTimeExpr}");
		}

		private static readonly object[] TestCasesToTestIsTimestampInTradingPeriod =
		{
			new object[] { -TenMinutes, TenMinutes, true },
			new object[] { -TenMinutes - TenMinutes, -TenMinutes, false },
			new object[] { TenMinutes, TenMinutes + TenMinutes, false },
		};

		[TestCaseSource(nameof(TestCasesToTestIsTimestampInTradingPeriod))]
		public void TestIsTimestampInTradingPeriod(TimeSpan beforeNow, TimeSpan afterNow, bool expected)
		{
			// Arrange
			var now = DateTimeOffset.UtcNow;
			var schedule = new Schedule(GetCronExpression(now + beforeNow), GetCronExpression(now + afterNow), TimeZoneInfo.Utc);

			// Act
			var result = schedule.IsTimestampInTradingPeriod(DateTimeHelper.CurrentMilliseconds);

			// ClassicAssert
			ClassicAssert.AreEqual(expected, result);
		}

		[TestCase("0 10 * * * ?", "0 20 * * * ?", true )]
		[TestCase(null, "0 20 * * * ?", false )]
		[TestCase("0 10 * * * ?", null, false )]
		[TestCase(null, null, false )]
		public void TestIsTimestampInTradingPeriod(string start, string end, bool expected)
		{
			// Arrange
			var schedule = new Schedule(start, end, TimeZoneInfo.Utc);

			// Act
			var result = schedule.IsTradingPeriodDefined();

			// ClassicAssert
			ClassicAssert.AreEqual(expected, result);
		}

		[TestCase("0 10 * * * ?", "0 20 * * * ?", false )]
		[TestCase(null, "0 20 * * * ?", false )]
		[TestCase("0 10 * * * ?", null, true )]
		[TestCase(null, null, false )]
		public void TestIsOnlyPeriodBeginDefined(string start, string end, bool expected)
		{
			// Arrange
			var schedule = new Schedule(start, end, TimeZoneInfo.Utc);

			// Act
			var result = schedule.IsOnlyPeriodBeginDefined();

			// ClassicAssert
			ClassicAssert.AreEqual(expected, result);
		}

		private static readonly object[] TestCasesToTestIsTimestampAfterTradingPeriodBegin =
		{
			new object[] { "0 10 * * * ?", (Func<long>)(() => DateTimeHelper.CurrentMilliseconds), true },
			new object[] { "0 10 * * * ? 1980", (Func<long>)(() => DateTimeHelper.CurrentMilliseconds), true },
			new object[] { "0 10 * * * ? 2060", (Func<long>)(() => DateTimeHelper.CurrentMilliseconds), false },
			new object[] { "0 10 * * * ?", (Func<long>)(() => 0), false },
		};

		[TestCaseSource(nameof(TestCasesToTestIsTimestampAfterTradingPeriodBegin))]
		public void TestIsTimestampAfterTradingPeriodBegin(string start, Func<long> getTimestamp,  bool expected)
		{
			// Arrange
			var schedule = new Schedule(start, null, TimeZoneInfo.Utc);

			// Act
			var result = schedule.IsTimestampAfterTradingPeriodBegin(getTimestamp());

			// ClassicAssert
			ClassicAssert.AreEqual(expected, result);
		}

		private static readonly object[] TestCasesToTestIsTimestampAfterTradingPeriodEnd =
		{
			new object[] { (Func<long>)(() => DateTimeHelper.CurrentMilliseconds), false },
			new object[] { (Func<long>)(() => 0), false },
			new object[] { (Func<long>)(() => DateTimeHelper.CurrentMilliseconds +  (long)(2 * TenMinutes.TotalMilliseconds)), true },
		};

		[TestCaseSource(nameof(TestCasesToTestIsTimestampAfterTradingPeriodEnd))]
		public void TestIsTimestampAfterTradingPeriodEnd(Func<long> getTimestamp, bool expected)
		{
			// Arrange
			var now = DateTimeOffset.UtcNow;
			var schedule = new Schedule(GetCronExpression(now - TenMinutes), GetCronExpression(now + TenMinutes), TimeZoneInfo.Utc);

			// Act
			var result = schedule.IsTimestampAfterTradingPeriodEnd(getTimestamp());

			// ClassicAssert
			ClassicAssert.AreEqual(expected, result);
		}

		private string GetCronExpression(DateTimeOffset date)
		{
			return $"0 {date.Minute} {date.Hour} * * ?";
		}
	}
}

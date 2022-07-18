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

namespace Epam.FixAntenna.NetCore.Tests.FixEngine.Scheduler
{
	internal class SessionTaskSchedulerTest
	{
		private static readonly object[] TestCases =
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
		};

		[TestCaseSource(nameof(TestCases))]
		public void TestIsInsideInterval(string dateString, string startTimeExpr, string stopTimeExpr, bool expected)
		{
			// Arrange
			var date = DateTimeOffset.ParseExact(dateString, "d/M/yyyy H:m z", null);
			var timeZone = TimeZoneInfo.CreateCustomTimeZone("Test", date.Offset, "", "");

			// Act
			var actual = SessionTaskScheduler.IsInsideInterval(date, startTimeExpr, stopTimeExpr, timeZone);

			// Assert
			Assert.AreEqual(expected, actual, $"Wrong behaviour. {dateString}, {startTimeExpr}, {stopTimeExpr}");
		}
	}
}

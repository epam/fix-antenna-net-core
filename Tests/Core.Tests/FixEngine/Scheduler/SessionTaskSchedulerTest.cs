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
using System.Linq;
using System.Threading.Tasks;
using Epam.FixAntenna.NetCore.FixEngine.Scheduler.Tasks;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using NUnit.Framework;
using Quartz;

namespace Epam.FixAntenna.NetCore.FixEngine.Scheduler
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

			new object[] { "12/07/2022 4:33 +1", "0 0 4 * * ? 2021", "0 0 5 * * ? 2021", false },
			new object[] { "12/07/2022 4:33 +1", "0 0 4 * * ? 2023", "0 0 5 * * ? 2023", false },
			new object[] { "12/07/2022 4:33 +1", "0 0 4 * * ? 2022", "0 0 5 * * ? 2022", true },
			new object[] { "12/07/2022 4:33 +1", "0 0 4 * * ? 2022", "0 0 5 9 10 ? 2022", true },
			new object[] { "12/07/2022 4:33 +1", "0 0 4 5 6 ? 2022", "0 0 5 9 10 ? 2022", true },
			new object[] { "12/07/2022 4:33 +1", "0 32,33 4 12 7 ? 2022", "0 0 5 9 10 ? 2022", false },

			new object[] { "12/07/2022 4:33 +1", "0 0 3 * * ?|0 0 4 * * ?", "0 0 5 * * ?|0 0 6 * * ?", true },
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

		[Test]
		public void TestScheduleCronTaskDoesNotScheduleWhenNoEventsInFuture()
		{
			// Arrange
			var pipedCronExpression = "0 33 4 * * ? 2020|0 33 4 * * ?";
			var scheduler = new SessionTaskScheduler(new SessionParameters());
			var expected = MultipartCronExpression.ExtractCronExpressions(pipedCronExpression).Skip(1);

			// Act
			scheduler.ScheduleCronTask<ATask>(pipedCronExpression, TimeZoneInfo.Utc);
			var actual = scheduler.GetCronExpressionsForScheduledCronTask<ATask>();
			
			// Assert
			Assert.That(actual, Is.EquivalentTo(expected));

			scheduler.Shutdown();
		}

		[Test]
		public void TestScheduleCronTaskWithMultipleCronExpressions()
		{
			// Arrange
			var pipedCronExpression = "0 33 4 * * ?|0 34 4 * * ?";
			var scheduler = new SessionTaskScheduler(new SessionParameters());
			var expected = MultipartCronExpression.ExtractCronExpressions(pipedCronExpression);

			// Act
			scheduler.ScheduleCronTask<ATask>(pipedCronExpression, TimeZoneInfo.Utc);
			var actual = scheduler.GetCronExpressionsForScheduledCronTask<ATask>();
			
			// Assert
			Assert.That(actual, Is.EquivalentTo(expected));

			scheduler.Shutdown();
		}

		private class ATask : AbstractSessionTask
		{
			protected override Task RunForSession(IJobExecutionContext context, IExtendedFixSession session)
			{
				return Task.CompletedTask;
			}
		}
	}
}

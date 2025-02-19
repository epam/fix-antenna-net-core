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
using NUnit.Framework.Legacy;
using Quartz;

namespace Epam.FixAntenna.NetCore.FixEngine.Scheduler
{
	internal class SessionTaskSchedulerTest
	{
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
			
			// ClassicAssert
			ClassicAssert.That(actual, Is.EquivalentTo(expected));

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
			
			// ClassicAssert
			ClassicAssert.That(actual, Is.EquivalentTo(expected));

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

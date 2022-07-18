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
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.FixEngine.Scheduler.Tasks;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;
using Quartz.Simpl;

namespace Epam.FixAntenna.NetCore.FixEngine.Scheduler
{
	internal class SessionTaskScheduler
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(SessionTaskScheduler));
		private readonly IScheduler _scheduler;
		private JobKey _startSessionTaskId;
		private JobKey _stopSessionTaskId ;
		private readonly SessionParameters _sessionParameters;

		internal SessionTaskScheduler(SessionParameters sessionParameters)
		{
			_sessionParameters = sessionParameters;

			// We want a separate scheduler per session, so we need to use a unique per session scheduler name.
			// SessionId is not good for that as tests can create several sessions with the same Id.
			var schedulerName = Guid.NewGuid().ToString();
			DirectSchedulerFactory.Instance.CreateScheduler(schedulerName, schedulerName, new DefaultThreadPool(), new RAMJobStore());
			_scheduler = DirectSchedulerFactory.Instance.GetScheduler(schedulerName).Result;
			_scheduler = _scheduler ?? throw new InvalidOperationException("Cannot create scheduler.");

			_scheduler.Start();
		}

		internal void ScheduleSessionStart(string startTimeExpr, TimeZoneInfo timeZone)
		{
			Log.Trace($"Add start session task {startTimeExpr}: {_sessionParameters.SessionId}");

			if (_startSessionTaskId != null)
			{
				Deschedule(_startSessionTaskId);
			}

			var (nextRun, key) = ScheduleCronJob<InitiatorSessionStartTask>(startTimeExpr, timeZone);

			Log.Trace($"{nameof(InactivityCheckTask)} will run at {nextRun:O}");

			_startSessionTaskId = key;
		}

		internal bool IsSessionStartScheduled()
		{
			return _startSessionTaskId != null && _scheduler.GetJobDetail(_startSessionTaskId).Result != null;
		}

		internal bool IsSessionStopScheduled()
		{
			return _stopSessionTaskId != null && _scheduler.GetJobDetail(_stopSessionTaskId).Result != null;
		}

		internal void ScheduleSessionStop(string stopTimeExpr, TimeZoneInfo timeZone)
		{
			Log.Trace($"Add stop session task {stopTimeExpr}: {_sessionParameters.SessionId}");

			if (_stopSessionTaskId != null)
			{
				Deschedule(_stopSessionTaskId);
			}

			var (nextRun, key) = ScheduleCronJob<InitiatorSessionStopTask>(stopTimeExpr, timeZone);

			Log.Trace($"{nameof(InactivityCheckTask)} will run at {nextRun:O}");

			_stopSessionTaskId = key;
		}

		internal void ScheduleHeartbeat(TimeSpan checkHeartbeatInterval)
		{
			var hbJob = CreateJob<InactivityCheckTask>();
			
			var hbTrigger = TriggerBuilder.Create()
				.WithSimpleSchedule(s => s
					.WithInterval(checkHeartbeatInterval)
					.WithMisfireHandlingInstructionIgnoreMisfires()
					.RepeatForever())
				.Build();
			
			var nextRun = _scheduler.ScheduleJob(hbJob, hbTrigger).Result;
			Log.Trace($"{nameof(InactivityCheckTask)} will run at {nextRun:O}");
		}

		internal void ScheduleTestRequest(TimeSpan checkTestRequestInterval)
		{
			var hbJob = CreateJob<TestRequestTask>();
			
			var hbTrigger = TriggerBuilder.Create()
				.StartAt(DateTimeOffset.Now.AddSeconds(1))
				.WithSimpleSchedule(s => s
					.WithInterval(checkTestRequestInterval)
					.WithMisfireHandlingInstructionIgnoreMisfires()
					.RepeatForever())
				.Build();
			
			var nextRun = _scheduler.ScheduleJob(hbJob, hbTrigger).Result;
			Log.Trace($"{nameof(TestRequestTask)} will run at {nextRun:O}");
		}

		internal void ScheduleSeqReset(TimeSpan resetTime, TimeZoneInfo resetTimeZone)
		{
			var srJob = CreateJob<ResetSeqNumTask>();

			var srTrigger = TriggerBuilder.Create()
				.WithDailyTimeIntervalSchedule(s => s
					.WithIntervalInHours(24)
					.StartingDailyAt(new TimeOfDay(resetTime.Hours, resetTime.Minutes, resetTime.Seconds))
					.InTimeZone(resetTimeZone))
				.Build();

			var nextRun = _scheduler.ScheduleJob(srJob, srTrigger).Result;
			Log.Trace($"{nameof(ResetSeqNumTask)} will run at {nextRun:O}");
		}

		private (DateTimeOffset nextRun, JobKey key)
			ScheduleCronJob<T>(string cronExpression, TimeZoneInfo timeZone)
			where T: IJob
		{
			var job = CreateJob<T>();
			var trigger = TriggerBuilder.Create()
				.WithCronSchedule(
					cronExpression,
					s => s.InTimeZone(timeZone)
				)
				.Build();
			var nextRun = _scheduler.ScheduleJob(job, trigger).Result;
			return (nextRun, job.Key);
		}

		private IJobDetail CreateJob<T>() where T: IJob
		{
			return JobBuilder.Create<T>()
				.UsingJobData("SessionId", _sessionParameters.SessionId.ToString())
				.Build();
		}

		internal void DescheduleSessionStartAndStop()
		{
			if (_startSessionTaskId != null)
			{
				Log.Trace($"Cancel start session task: {_sessionParameters.SessionId}");

				Deschedule(_startSessionTaskId);
				_startSessionTaskId = null;
			}

			if (_stopSessionTaskId != null)
			{
				Log.Trace($"Cancel stop session task: {_sessionParameters.SessionId}");

				Deschedule(_stopSessionTaskId);
				_stopSessionTaskId = null;
			}
		}

		private void Deschedule(JobKey key)
		{
			_scheduler.DeleteJob(key).Wait();
		}

		internal void DescheduleAllTasks()
		{
			var jobKeys = _scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup()).Result;
			_scheduler.DeleteJobs(jobKeys).Wait();
			_startSessionTaskId = null;
			_stopSessionTaskId = null;
		}

		internal void Shutdown()
		{
			_scheduler.Shutdown(false).Wait();
		}

		internal static bool IsInsideInterval(DateTimeOffset date, string startTimeExpr, string stopTimeExpr, TimeZoneInfo timeZone)
		{
			if (!IsValidCronExpression(startTimeExpr))
			{
				throw new ArgumentException($"{nameof(startTimeExpr)} is invalid: {startTimeExpr}");
			}

			if (!IsValidCronExpression(stopTimeExpr))
			{
				throw new ArgumentException($"{nameof(stopTimeExpr)} is invalid: {stopTimeExpr}");
			}

			timeZone = timeZone ?? throw new ArgumentNullException(nameof(timeZone));

			var startTimeCronExpression = new CronExpression(startTimeExpr) { TimeZone = timeZone };
			var stopTimeCronExpression = new CronExpression(stopTimeExpr) { TimeZone = timeZone };

			var startLastExecutionDate = CronPredictor.GetTimeBefore(date, startTimeCronExpression);
			var stopLastExecutionDate = CronPredictor.GetTimeBefore(date, stopTimeCronExpression);

			return startLastExecutionDate > stopLastExecutionDate
					&& !stopTimeCronExpression.IsSatisfiedBy(date)
					&& !startTimeCronExpression.IsSatisfiedBy(date);
		}

		internal static bool IsValidCronExpression(string cronExpression)
		{
			return CronExpression.IsValidExpression(cronExpression);
		}
	}
}

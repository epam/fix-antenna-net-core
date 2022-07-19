﻿// Copyright (c) 2022 EPAM Systems
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
		private readonly SessionParameters _sessionParameters;

		// We want a separate scheduler per session, so we need to use a unique per session scheduler name.
		// SessionId is not good for that as tests can create several sessions with the same Id.
		private readonly string _schedulerName = Guid.NewGuid().ToString();

		private readonly JobKey _heartbeatJobKey;
		private readonly JobKey _testRequestJobKey;
		private readonly JobKey _seqResetJobKey;
		private readonly JobKey _sessionStartJobKey;
		private readonly JobKey _sessionStopJobKey;

		internal SessionTaskScheduler(SessionParameters sessionParameters)
		{
			_sessionParameters = sessionParameters;

			DirectSchedulerFactory.Instance.CreateScheduler(_schedulerName, _schedulerName, new DefaultThreadPool(), new RAMJobStore());
			_scheduler = DirectSchedulerFactory.Instance.GetScheduler(_schedulerName).Result;
			_scheduler = _scheduler ?? throw new InvalidOperationException("Cannot create scheduler.");

			_heartbeatJobKey = new JobKey("Heartbeat", _schedulerName);
			_testRequestJobKey = new JobKey("TestRequest", _schedulerName);
			_seqResetJobKey = new JobKey("SeqReset", _schedulerName);
			_sessionStartJobKey = new JobKey("SessionStart", _schedulerName);
			_sessionStopJobKey = new JobKey("SessionStop", _schedulerName);

			_scheduler.Start();
		}

		internal void ScheduleSessionStart(string startTimeExpr, TimeZoneInfo timeZone)
		{
			Log.Trace($"Add start session task {startTimeExpr}: {_sessionParameters.SessionId}");

			ScheduleCronJob<InitiatorSessionStartTask>(startTimeExpr, timeZone, _sessionStartJobKey);
		}

		internal void ScheduleSessionStop(string stopTimeExpr, TimeZoneInfo timeZone)
		{
			Log.Trace($"Add stop session task {stopTimeExpr}: {_sessionParameters.SessionId}");

			ScheduleCronJob<InitiatorSessionStopTask>(stopTimeExpr, timeZone, _sessionStopJobKey);
		}

		internal bool IsSessionStartScheduled()
		{
			return JobExists(_sessionStartJobKey);
		}

		internal bool IsSessionStopScheduled()
		{
			return JobExists(_sessionStopJobKey);
		}

		internal void ScheduleHeartbeat(TimeSpan checkHeartbeatInterval)
		{
			Deschedule(_heartbeatJobKey);

			var job = CreateJob<InactivityCheckTask>(_heartbeatJobKey);
			
			var trigger = TriggerBuilder.Create()
				.WithSimpleSchedule(s => s
					.WithInterval(checkHeartbeatInterval)
					.WithMisfireHandlingInstructionIgnoreMisfires()
					.RepeatForever())
				.Build();
			
			var nextRun = _scheduler.ScheduleJob(job, trigger).Result;
			Log.Trace($"{nameof(InactivityCheckTask)} will run at {nextRun:O}");
		}

		internal void ScheduleTestRequest(TimeSpan checkTestRequestInterval)
		{
			Deschedule(_testRequestJobKey);

			var job = CreateJob<TestRequestTask>(_testRequestJobKey);
			
			var trigger = TriggerBuilder.Create()
				.StartAt(DateTimeOffset.Now.AddSeconds(1))
				.WithSimpleSchedule(s => s
					.WithInterval(checkTestRequestInterval)
					.WithMisfireHandlingInstructionIgnoreMisfires()
					.RepeatForever())
				.Build();
			
			var nextRun = _scheduler.ScheduleJob(job, trigger).Result;
			Log.Trace($"{nameof(TestRequestTask)} will run at {nextRun:O}");
		}

		internal void ScheduleSeqReset(TimeSpan resetTime, TimeZoneInfo resetTimeZone)
		{
			Deschedule(_seqResetJobKey);

			var job = CreateJob<ResetSeqNumTask>(_seqResetJobKey);

			var trigger = TriggerBuilder.Create()
				.WithDailyTimeIntervalSchedule(s => s
					.WithIntervalInHours(24)
					.StartingDailyAt(new TimeOfDay(resetTime.Hours, resetTime.Minutes, resetTime.Seconds))
					.InTimeZone(resetTimeZone))
				.Build();

			var nextRun = _scheduler.ScheduleJob(job, trigger).Result;
			Log.Trace($"{nameof(ResetSeqNumTask)} will run at {nextRun:O}");
		}

		private bool JobExists(JobKey jobKey)
		{
			return _scheduler.CheckExists(jobKey).Result;
		}

		private void ScheduleCronJob<T>(string cronExpression, TimeZoneInfo timeZone, JobKey jobKey)
			where T: IJob
		{
			Deschedule(jobKey);

			var job = CreateJob<T>(jobKey);
			var trigger = TriggerBuilder.Create()
				.WithCronSchedule(
					cronExpression,
					s => s.InTimeZone(timeZone)
				)
				.Build();

			var nextRun = _scheduler.ScheduleJob(job, trigger).Result;

			Log.Trace($"{nameof(T)} will run at {nextRun:O}");
		}

		private IJobDetail CreateJob<T>(JobKey jobKey) where T: IJob
		{
			return JobBuilder.Create<T>()
				.WithIdentity(jobKey)
				.UsingJobData("SessionId", _sessionParameters.SessionId.ToString())
				.Build();
		}

		internal void DescheduleSessionStartAndStop()
		{
			if (JobExists(_sessionStartJobKey))
			{
				Log.Trace($"Cancel start session task: {_sessionParameters.SessionId}");
				Deschedule(_sessionStartJobKey);
			}

			if (JobExists(_sessionStopJobKey))
			{
				Log.Trace($"Cancel stop session task: {_sessionParameters.SessionId}");
				Deschedule(_sessionStopJobKey);
			}
		}

		private void Deschedule(JobKey key)
		{
			if (JobExists(key))
			{
				_scheduler.DeleteJob(key).Wait();
			}
		}

		internal void DescheduleAllTasks()
		{
			var jobKeys = _scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup()).Result;
			_scheduler.DeleteJobs(jobKeys).Wait();
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

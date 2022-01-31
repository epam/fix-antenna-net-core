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
using System.Collections.Generic;
using System.Linq;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Logging;

namespace Epam.FixAntenna.NetCore.FixEngine.Manager.Scheduler
{
	internal class SchedulerManager
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(SchedulerManager));
		private readonly HashSet<SchedulerTask> _tasks = new HashSet<SchedulerTask>();

		/// <summary>
		/// Schedule the task.
		/// </summary>
		/// <param name="schedulerTask"> the task </param>
		/// <param name="scheduleTimestamp"> the timestamp
		/// </param>
		/// <exception cref="InvalidOperationException"> if task was scheduled.
		/// </exception>
		public virtual void Schedule(SchedulerTask schedulerTask, long scheduleTimestamp)
		{
			if (Log.IsDebugEnabled)
			{
				Log.Debug($"Schedule task: {schedulerTask}, at: {scheduleTimestamp.ToDateTimeString("O")}");
			}
			AddScheduleTask(schedulerTask, scheduleTimestamp);
		}

		/// <summary>
		/// Schedule the task.
		/// </summary>
		/// <param name="schedulerTask"> the task </param>
		/// <param name="scheduleTimestamp"> the timestamp </param>
		/// <param name="period"> the period, in milliseconds </param>
		/// <exception cref="InvalidOperationException"> if task was scheduled.
		///  </exception>
		public virtual void Schedule(SchedulerTask schedulerTask, long scheduleTimestamp, int period)
		{
			if (Log.IsDebugEnabled)
			{
				Log.Debug($"Schedule task: {schedulerTask}, at: {scheduleTimestamp.ToDateTimeString("O")}  with period in: {period / 1000} sec.");
			}
			AddScheduleTask(schedulerTask, scheduleTimestamp, period);
		}

		/// <summary>
		/// Cancel the task.
		/// </summary>
		/// <param name="schedulerTask"> the task
		///  </param>
		public virtual void Cancel(SchedulerTask schedulerTask)
		{
			if (Log.IsDebugEnabled)
			{
				Log.Debug("Cancel scheduled task: " + schedulerTask);
			}
			try
			{
				_tasks.Remove(schedulerTask);
				schedulerTask.Cancel();
			}
			catch (Exception e)
			{
				if (Log.IsDebugEnabled)
				{
					Log.Warn("Error on cancel task. Cause: " + e.Message, e);
				}
				else
				{
					Log.Warn("Error on cancel task. Cause: " + e.Message);
				}
			}
		}

		/// <summary>
		/// Shutdown the manager.
		/// </summary>
		public virtual void Shutdown()
		{
			try
			{
				foreach (var task in _tasks.ToList())
				{
					Cancel(task);
				}
			}
			catch (Exception e)
			{
				if (Log.IsDebugEnabled)
				{
					Log.Warn("Error on stop timer. Cause: " + e.Message, e);
				}
				else
				{
					Log.Warn("Error on stop timer. Cause: " + e.Message);
				}
			}
			if (Log.IsDebugEnabled)
			{
				Log.Debug("Scheduler manager was shutdown");
			}
		}

		private void AddScheduleTask(SchedulerTask schedulerTask, long timestamp, int period = 0)
		{
			if (!_tasks.Add(schedulerTask))
			{
				throw new InvalidOperationException($"Task {schedulerTask} already scheduled.");
			}
			schedulerTask.Set(timestamp, period);
		}
	}
}
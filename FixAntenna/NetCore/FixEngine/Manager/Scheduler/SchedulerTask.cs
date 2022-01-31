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
using System.Timers;
using Epam.FixAntenna.NetCore.Common;

namespace Epam.FixAntenna.NetCore.FixEngine.Manager.Scheduler
{
	internal abstract class SchedulerTask : IDisposable
	{
		private readonly string _name;

		private volatile bool _disposed;

		private Timer _timer;
		private int _period;

		protected SchedulerTask(string taskName)
		{
			_name = taskName;
		}

		public override string ToString()
		{
			return $"{GetType().Name} {{Name='{_name}'}}";
		}

		public void Set(long timestamp, int period)
		{
			_period = period;
			_timer = new Timer();
			_timer.Elapsed += Elapsed;
			_timer.AutoReset = false;

			// Determining millis to next run and set Interval to this value.
			// If 'period' provided, it will be set in the Elapsed method call.
			double millisToTime = timestamp - DateTimeHelper.CurrentMilliseconds;
			// If desired time already occured, then set task time to 1 ms.
			_timer.Interval = millisToTime <= 0 ? 1 : millisToTime;

			_timer.Enabled = true;
		}

		private void Elapsed(object sender, ElapsedEventArgs e)
		{
			// Start/stop for keeping fixed interval between runs
			_timer.Enabled = false;

			// Do workload.
			Run();

			// Setting the interval for subsequent runs.
			if (_period > 0)
			{
				_timer.Interval = _period;
				_timer.Enabled = true;
			}
		}

		public void Cancel()
		{
			_timer.Stop();
			_timer.Dispose();
		}

		public abstract void Run();

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					_timer.Stop();
					_timer.Dispose();
				}

				_disposed = true;
			}
		}
	}
}
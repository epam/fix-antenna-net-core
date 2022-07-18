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
using Quartz;

namespace Epam.FixAntenna.NetCore.FixEngine.Scheduler
{
	internal class CronPredictor
	{
		/// <summary>
		/// Find the closest time before the passed date that satisfied the cron expression.
		/// Had to implement this as GetTimeBefore from CronExpression is not implemented.
		/// 
		/// Calculation is based on the existing GetNextValidTimeAfter method and the binary search idea.
		/// Basically, we need to find an appropriate value of the function y(t) = GetNextValidTimeAfter(t).
		/// This function is non-decreasing, and this fact allows using the binary search.
		/// We want to find "t" that satisfied the cron expression and y(t) > date.
		/// </summary>
		internal static DateTimeOffset GetTimeBefore(DateTimeOffset date, CronExpression exp)
		{
			date = RemoveMilliseconds(date);
			var low = DateTimeOffset.FromUnixTimeSeconds(0);
			var up = date;
			var second = TimeSpan.FromSeconds(1);

			while (low < up - second)
			{
				var mean = low + Divide(up - low, 2);
				mean = RemoveMilliseconds(mean);

				var nextValidTime = exp.GetNextValidTimeAfter(mean);

				if (exp.IsSatisfiedBy(mean) && nextValidTime >= date)
				{
					return mean;
				}

				if (nextValidTime >= date)
				{
					up = mean;
				}
				else
				{
					low = mean;
				}
			}

			throw new Exception($"Cannot find previous valid time for the cron expression: {exp}. {nameof(date)}: {date}");
		}

		private static DateTimeOffset RemoveMilliseconds(DateTimeOffset offset)
		{
			return new DateTimeOffset(offset.Year, offset.Month, offset.Day, offset.Hour, offset.Minute, offset.Second, offset.Offset);
		}

		// Based on implementation in .NET 6.
		// Net standard 2.0 does not support division of TimeSpans.
		private static TimeSpan Divide(TimeSpan timeSpan, double divisor)
		{
			if (double.IsNaN(divisor))
			{
				throw new ArgumentException("Argument cannot be NaN", nameof(divisor));
			}

			double ticks = Math.Round(timeSpan.Ticks / divisor);
			return IntervalFromDoubleTicks(ticks);
		}

		// Based on implementation in .NET 6
		private static TimeSpan IntervalFromDoubleTicks(double ticks)
		{
			if ((ticks > long.MaxValue) || (ticks < long.MinValue) || double.IsNaN(ticks))
			{
				throw new OverflowException("TimeSpan too long");
			}

			if (ticks == long.MaxValue)
				return TimeSpan.MaxValue;
			return new TimeSpan((long)ticks);
		}
	}
}

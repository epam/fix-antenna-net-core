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

namespace Epam.FixAntenna.NetCore.FixEngine.Scheduler
{
	internal class Schedule
	{
		public MultipartCronExpression TradePeriodBegin { get; }
		public MultipartCronExpression TradePeriodEnd { get; }
		public TimeZoneInfo TimeZone { get; }

		public Schedule(string tradePeriodBegin, string tradePeriodEnd, TimeZoneInfo timeZone)
		{
			TradePeriodBegin = tradePeriodBegin == null ? null : new MultipartCronExpression(tradePeriodBegin, timeZone);
			TradePeriodEnd = tradePeriodEnd == null ? null : new MultipartCronExpression(tradePeriodEnd, timeZone);
			TimeZone = timeZone;
		}

		public bool IsTradingPeriodDefined() => TradePeriodBegin != null && TradePeriodEnd != null;
		public bool IsOnlyPeriodBeginDefined() => TradePeriodBegin != null && TradePeriodEnd == null;

		public bool IsInsideOrAtBeginningOfInterval(DateTimeOffset date)
		{
			ClassicAssertTradingPeriodDefined();

			var isIntervalStart = TradePeriodBegin.IsSatisfiedBy(date);
			var isInsideInterval = IsInsideInterval(date);

			return isIntervalStart || isInsideInterval;
		}

		/// <summary>
		/// Check if a date is inside interval defined by the schedule.
		/// If a date satisfies TradePeriodBegin or TradePeriodEnd then it's not inside the interval
		/// </summary>
		public bool IsInsideInterval(DateTimeOffset date)
		{
			ClassicAssertTradingPeriodDefined();

			var startLastExecutionDate = TradePeriodBegin.GetTimeBefore(date);
			var stopLastExecutionDate = TradePeriodEnd.GetTimeBefore(date);

			var isIntervalEnd = TradePeriodEnd.IsSatisfiedBy(date) || TradePeriodBegin.IsSatisfiedBy(date);

			if (startLastExecutionDate.HasValue && stopLastExecutionDate.HasValue)
			{
				return startLastExecutionDate > stopLastExecutionDate && !isIntervalEnd;
			}

			// there was a start but not end yet
			if (startLastExecutionDate.HasValue)
			{
				return !isIntervalEnd;
			}

			// no start was done
			return false;
		}

		public bool IsTimestampAfterTradingPeriodEnd(long utcTimestampInMilliseconds)
		{
			var nextMatchingTime = TradePeriodEnd.GetTimeAfter(DateTimeOffset.UtcNow);
			return nextMatchingTime != null &&
				utcTimestampInMilliseconds > nextMatchingTime.Value.ToUniversalTime().TotalMilliseconds();
		}

		public bool IsTimestampInTradingPeriod(long utcTimestampInMilliseconds)
		{
			if (TradePeriodBegin != null && TradePeriodEnd != null)
			{
				var now = DateTimeOffset.UtcNow;
				var prevPeriodBegin = TradePeriodBegin.GetTimeBefore(now);
				var prevPeriodEnd = TradePeriodEnd.GetTimeBefore(now);
				var nextPeriodEnd = TradePeriodEnd.GetTimeAfter(now);

				if (prevPeriodBegin != null && prevPeriodEnd != null && nextPeriodEnd != null)
				{
					return IsTimestampAfterOrSame(utcTimestampInMilliseconds, prevPeriodBegin.Value)
						&& prevPeriodBegin > prevPeriodEnd
						&& utcTimestampInMilliseconds < nextPeriodEnd.Value.ToUniversalTime().TotalMilliseconds();
				}
			}

			return false;
		}

		public bool IsTimestampAfterTradingPeriodBegin(long utcTimestampInMilliseconds)
		{
			var prevMatchingTime = TradePeriodBegin.GetTimeBefore(DateTimeOffset.UtcNow);
			return prevMatchingTime != null && IsTimestampAfterOrSame(utcTimestampInMilliseconds, prevMatchingTime.Value);
		}

		private bool IsTimestampAfterOrSame(long utcTimestampInMilliseconds, DateTimeOffset date) {
			return utcTimestampInMilliseconds >= date.ToUniversalTime().TotalMilliseconds();
		}

		private void ClassicAssertTradingPeriodDefined()
		{
			if (!IsTradingPeriodDefined())
			{
				throw new InvalidOperationException("Schedule is not fully defined");
			}
		}
	}
}
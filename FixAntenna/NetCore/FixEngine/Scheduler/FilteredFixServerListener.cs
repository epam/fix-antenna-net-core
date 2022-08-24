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
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;

namespace Epam.FixAntenna.NetCore.FixEngine.Scheduler
{
	/// <summary>
	/// Proxy listener for filtering incoming connections.
	/// </summary>
	internal class FilteredFixServerListener : IFixServerListener
	{
		private const string DenialReason = "Session was denied because of the server schedule";
		private static readonly ILog Log = LogFactory.GetLog(typeof(FilteredFixServerListener));

		private readonly IFixServerListener _parentListener;

		public FilteredFixServerListener(IFixServerListener parentListener)
		{
			_parentListener = parentListener;
		}

		public virtual void NewFixSession(IFixSession session)
		{
			var config = new ConfigurationAdapter(session.Parameters.Configuration);
			var isScheduleParsed = TryParseSchedule(config, out var schedule);

			if (isScheduleParsed && IsAllowedToConnect(schedule))
			{
				if (!schedule.IsTradingPeriodDefined())
				{
					Log.Debug($"Both {Config.TradePeriodBegin} and {Config.TradePeriodEnd} should be set to filter connections");
				}

				_parentListener.NewFixSession(session);

				if (SessionState.IsDisposed(session.SessionState)) return;

				ScheduleDisconnect(session, config, schedule);

				// Session is still inside allowed interval. Do nothing
				if (IsAllowedToConnect(schedule)) return;

				// Trade time is over, so let's disconnect.
				// The situation is possible if while connecting, we reached the end of the allowed trade period
				if (SessionState.IsNotDisconnected(session.SessionState))
				{
					session.Disconnect(DenialReason);
				}
			}
			else
			{
				if (Log.IsDebugEnabled)
				{
					Log.Debug($"Session was denied by filter: {session.Parameters}");
				}

				if (Log.IsTraceEnabled && isScheduleParsed)
				{
					Log.Trace("Incoming connections will be allowed at " + 
						$"{schedule.TradePeriodBegin.OriginalCronExpression} {schedule.TimeZone.Id}: {session.Parameters.SessionId}");
				}

				session.Dispose();
			}
		}

		private void ScheduleDisconnect(IFixSession session, ConfigurationAdapter config, Schedule schedule)
		{
			if (schedule.TradePeriodEnd == null) return;

			if (Log.IsTraceEnabled)
			{
				Log.Trace("Add 'stop' task " + 
					$"{schedule.TradePeriodEnd.OriginalCronExpression} {schedule.TimeZone.Id}: {session.Parameters.SessionId}");
			}

			var acceptorSession = (AcceptorFixSession)session;
			acceptorSession.ScheduleDisconnect(schedule.TradePeriodEnd.OriginalCronExpression, schedule.TimeZone);
		}

		private bool TryParseSchedule(ConfigurationAdapter config, out Schedule schedule)
		{
			schedule = null;

			if (config.TradePeriodBegin != null && !MultipartCronExpression.IsValidCronExpression(config.TradePeriodBegin))
			{
				Log.Error($"{Config.TradePeriodBegin} expression is invalid: {config.TradePeriodBegin}");
				return false;
			}

			if (config.TradePeriodEnd != null && !MultipartCronExpression.IsValidCronExpression(config.TradePeriodEnd))
			{
				Log.Error($"{Config.TradePeriodEnd} expression is invalid: {config.TradePeriodEnd}");
				return false;
			}

			schedule = new Schedule(config.TradePeriodBegin, config.TradePeriodEnd, config.TradePeriodTimeZone);

			return true;
		}

		private bool IsAllowedToConnect(Schedule schedule)
		{
			return !schedule.IsTradingPeriodDefined() || schedule.IsInsideOrAtBeginningOfInterval(DateTimeOffset.UtcNow);
		}
	}
}
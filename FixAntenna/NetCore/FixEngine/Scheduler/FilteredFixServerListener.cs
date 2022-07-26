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

		private readonly ConfigurationAdapter _serverConfigAdapter;
		private readonly IFixServerListener _parentListener;

		public FilteredFixServerListener(ConfigurationAdapter serverConfigAdapter, IFixServerListener parentListener)
		{
			_serverConfigAdapter = serverConfigAdapter;
			_parentListener = parentListener;
		}

		public virtual void NewFixSession(IFixSession session)
		{
			var schedule = GetSchedule(session.Parameters);

			if (IsAllowedToConnect(schedule))
			{
				_parentListener.NewFixSession(session);

				if (SessionState.IsDisposed(session.SessionState)) return;

				ScheduleDisconnect(session, schedule);

				// Session is still inside allowed interval
				if (!schedule.IsScheduleDefined() || schedule.IsNowInsideInterval()) return;

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
					Log.Debug("Session was denied by filter: " + session.Parameters);
				}

				session.Disconnect(DenialReason);
				session.Dispose();
			}
		}

		private void ScheduleDisconnect(IFixSession session, Schedule schedule)
		{
			if (schedule.TradePeriodEnd == null) return;

			var acceptorSession = (AcceptorFixSession)session;
			acceptorSession.ScheduleDisconnect(schedule.TradePeriodEnd, schedule.TimeZone);
		}

		private bool IsAllowedToConnect(Schedule schedule)
		{
			if (schedule.TradePeriodBegin != null && !SessionTaskScheduler.IsValidCronExpression(schedule.TradePeriodBegin))
			{
				Log.Error($"{Config.TradePeriodBegin} expression is invalid: {schedule.TradePeriodBegin}");
				return false;
			}

			if (schedule.TradePeriodEnd != null && !SessionTaskScheduler.IsValidCronExpression(schedule.TradePeriodEnd))
			{
				Log.Error($"{Config.TradePeriodEnd} expression is invalid: {schedule.TradePeriodEnd}");
				return false;
			}

			if (!schedule.IsScheduleDefined())
			{
				Log.Debug($"Both {Config.TradePeriodBegin} and {Config.TradePeriodEnd} should be set to filter connections");
				return true;
			}

			return schedule.IsNowInsideInterval();
		}

		private Schedule GetSchedule(SessionParameters sessionParameters)
		{
			// Th server configuration and the session configuration can be read from different config files.
			// Thus, let's check if a configuration for the session set. If not then let's use the server configuration.
			var sessionConfigAdapter = new ConfigurationAdapter(sessionParameters.Configuration);
			var sessionConfig = sessionConfigAdapter.Configuration;

			return new Schedule
			{
				TradePeriodBegin = sessionConfig.Exists(Config.TradePeriodBegin)
					? sessionConfigAdapter.TradePeriodBegin
					: _serverConfigAdapter.TradePeriodBegin,
				TradePeriodEnd = sessionConfig.Exists(Config.TradePeriodEnd)
					? sessionConfigAdapter.TradePeriodEnd
					: _serverConfigAdapter.TradePeriodEnd,
				// TimeZone have a default value, so we use the server's value only if no session level parameters are set.
				TimeZone = sessionConfig.Exists(Config.TradePeriodTimeZone)
							|| sessionConfig.Exists(Config.TradePeriodBegin)
							|| sessionConfig.Exists(Config.TradePeriodEnd)
					? sessionConfigAdapter.TradePeriodTimeZone
					: _serverConfigAdapter.TradePeriodTimeZone
			};
		}

		private class Schedule
		{
			public string TradePeriodBegin { get; set; }
			public string TradePeriodEnd { get; set; }
			public TimeZoneInfo TimeZone { get; set; }

			public bool IsScheduleDefined() => TradePeriodBegin != null && TradePeriodEnd != null;

			public bool IsNowInsideInterval()
			{
				if (!IsScheduleDefined())
				{
					throw new InvalidOperationException("Schedule is not defined");
				}

				var now = DateTimeOffset.UtcNow;
				var isIntervalStart = SessionTaskScheduler.IsCronExpressionSatisfiedBy(now, TradePeriodBegin);
				var isInsideInterval = SessionTaskScheduler.IsInsideInterval(now, TradePeriodBegin, TradePeriodEnd, TimeZone);

				return isIntervalStart || isInsideInterval;
			}
		}
	}
}
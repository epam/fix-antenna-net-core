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
			if (TryParseSchedule(session.Parameters, out var schedule) && IsAllowedToConnect(schedule))
			{
				if (!schedule.IsTradingPeriodDefined())
				{
					Log.Debug($"Both {Config.TradePeriodBegin} and {Config.TradePeriodEnd} should be set to filter connections");
				}

				_parentListener.NewFixSession(session);

				if (SessionState.IsDisposed(session.SessionState)) return;

				ScheduleDisconnect(session, schedule);

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

				session.Dispose();
			}
		}

		private void ScheduleDisconnect(IFixSession session, Schedule schedule)
		{
			if (schedule.TradePeriodEnd == null) return;

			var acceptorSession = (AcceptorFixSession)session;
			acceptorSession.ScheduleDisconnect(schedule.TradePeriodEnd.OriginalCronExpression, schedule.TimeZone);
		}

		private bool TryParseSchedule(SessionParameters sessionParameters, out Schedule schedule)
		{
			schedule = null;
			var config = new ConfigurationAdapter(sessionParameters.Configuration);

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
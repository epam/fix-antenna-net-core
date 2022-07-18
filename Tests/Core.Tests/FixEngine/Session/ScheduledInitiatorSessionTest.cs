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
using System.Collections.Generic;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.NetCore.FixEngine.Session.Common;
using Epam.FixAntenna.NetCore.FixEngine.Session.Impl;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.Tests.FixEngine.Session
{
	internal class ScheduledInitiatorSessionTest
	{
		private ScheduledInitiatorSessionForTests _session;
		private static readonly TimeZoneInfo CustomTimeZone = TimeZoneInfo.CreateCustomTimeZone("Test", TimeSpan.FromHours(3), "", "");

		private static readonly object[] TestCasesNotStartImmediately =
		{
			// schedule start and stop time by setting offset in minutes around now
			new object[] { -20, -10, "UTC"},
			new object[] { -20, -10, null},
			new object[] { -20, -10, $"UTC+{CustomTimeZone.BaseUtcOffset}" },
			new object[] { null, 10, "UTC"},
			new object[] { -20, null, "UTC"},
		};

		[TestCaseSource(nameof(TestCasesNotStartImmediately))]
		public void TestSessionNotStartImmediately(int? startTimeOffsetInMin, int? stopTimeOffsetInMin, string timeZone)
		{
			// Arrange
			_session = CreateScheduledSession(
				startTimeOffsetInMin == null ? (DateTimeOffset?)null : DateTimeOffset.UtcNow + TimeSpan.FromMinutes(startTimeOffsetInMin.Value),
				stopTimeOffsetInMin == null ? (DateTimeOffset?)null: DateTimeOffset.UtcNow + TimeSpan.FromMinutes(stopTimeOffsetInMin.Value),
				timeZone);

			// Act
			_session.Schedule();

			// Assert
			Assert.AreEqual(0, _session.ConnectionAttempts);
		}

		// schedule start and stop time by setting offset in minutes around now
		private static readonly object[] TestCasesStartImmediately =
		{
			new object[] { -20, 10, "UTC" },
			new object[] { -20, 10, null},
			new object[] { -20, 10, $"UTC+{CustomTimeZone.BaseUtcOffset}" },
		};

		[TestCaseSource(nameof(TestCasesStartImmediately))]
		public void TestSessionStartImmediately(int startTimeOffsetInMin, int stopTimeOffsetInMin, string timeZone)
		{
			// Arrange
			_session = CreateScheduledSession(
				DateTimeOffset.UtcNow + TimeSpan.FromMinutes(startTimeOffsetInMin),
				DateTimeOffset.UtcNow + TimeSpan.FromMinutes(stopTimeOffsetInMin),
				timeZone);

			// Act
			_session.Schedule();

			// Assert
			Assert.AreEqual(1, _session.ConnectionAttempts);
		}

		[Test]
		public void TestThrowWhenStartTimeIsIncorrect()
		{
			// Arrange
			var props = new Dictionary<string, string>
			{
				{ "tradePeriodBegin", "0 -1 * * * ?" },
				{ "tradePeriodEnd", "0 10 * * * ?" }
			};

			_session = CreateScheduledSession(props);

			// Act
			Assert.Throws<ArgumentException>(() => _session.Schedule());
		}

		[Test]
		public void TestThrowWhenStopTimeIsIncorrect()
		{
			// Arrange
			var props = new Dictionary<string, string>
			{
				{ "tradePeriodBegin", "0 10 * * * ?" },
				{ "tradePeriodEnd", "0 -10 * * * ?" }
			};

			_session = CreateScheduledSession(props);

			// Act, Assert
			Assert.Throws<ArgumentException>(() => _session.Schedule());
		}

		// schedule start and stop time by setting offset in minutes around now
		private static readonly object[] TestCasesSessionScheduled =
		{
			new object[] { "0 10 * * * ?", "0 20 * * * ?" , true, true},
			new object[] { null, null , false, false},
		};

		[TestCaseSource(nameof(TestCasesSessionScheduled))]
		public void TestSessionScheduled(string startTime, string stopTime,
			bool expectedIsSessionStartScheduled, bool expectedIsSessionStopScheduled)
		{
			// Arrange
			var props = new Dictionary<string, string>();
			if (startTime != null) props.Add("tradePeriodBegin", startTime);
			if (stopTime != null) props.Add("tradePeriodEnd", stopTime);

			_session = CreateScheduledSession(props);

			// Act
			_session.Schedule();

			// Assert
			Assert.AreEqual(expectedIsSessionStartScheduled, _session.IsSessionStartScheduled);
			Assert.AreEqual(expectedIsSessionStopScheduled, _session.IsSessionStopScheduled);
		}

		[TearDown]
		public void CleanUp()
		{
			_session?.Dispose();
		}

		private ScheduledInitiatorSessionForTests CreateScheduledSession(
			DateTimeOffset? startTime, DateTimeOffset? stopTime, string timeZone)
		{
			if (!DateTimeHelper.TryParseTimeZone(timeZone, out var timeZoneInfo))
			{
				timeZoneInfo = TimeZoneInfo.Utc;
			}

			var props = new Dictionary<string, string>();

			if (startTime != null)
			{
				startTime = ConvertTime(startTime.Value, timeZoneInfo);
				props.Add("tradePeriodBegin", $"0 {startTime.Value.Minute} {startTime.Value.Hour} * * ?");
			}

			if (stopTime != null)
			{
				stopTime = ConvertTime(stopTime.Value, timeZoneInfo);
				props.Add("tradePeriodEnd", $"0 {stopTime.Value.Minute} {stopTime.Value.Hour} * * ?");
			}

			if (timeZone != null)
			{
				props.Add("tradePeriodTimeZone", timeZone);
			}

			return CreateScheduledSession(props);
		}

		private ScheduledInitiatorSessionForTests CreateScheduledSession(Dictionary<string, string> configProperties)
		{
			var sessionParameters = new SessionParameters(new Config(configProperties))
			{
				Port = 13341,
				Host = "localhost"
			};

			var sessionFactory = new ScheduledInitiatorSessionFactory();

			return (ScheduledInitiatorSessionForTests)sessionFactory.CreateInitiatorSession(sessionParameters);
		}

		private static DateTimeOffset ConvertTime(DateTimeOffset time, TimeZoneInfo timeZone)
		{
			return TimeZoneInfo.ConvertTime(time, timeZone);
		}

		private class ScheduledInitiatorSessionForTests : InitiatorFixSession
		{
			internal int ConnectionAttempts { get; private set; }
			internal bool IsSessionStartScheduled => Scheduler.IsSessionStartScheduled();
			internal bool IsSessionStopScheduled => Scheduler.IsSessionStopScheduled();

			public ScheduledInitiatorSessionForTests(
				IFixMessageFactory factory,
				SessionParameters sessionParameters,
				HandlerChain fixSessionListener) : base(factory, sessionParameters, fixSessionListener)
			{
			}

			public override void Connect()
			{
				ConnectionAttempts++;
			}
		}

		private class ScheduledInitiatorSessionFactory : AbstractFixSessionFactory
		{
			public override IFixMessageFactory MessageFactory => new Fix44MessageFactory();

			public override IExtendedFixSession GetInitiatorSession(SessionParameters details, HandlerChain chain)
			{
				return new ScheduledInitiatorSessionForTests(MessageFactory, details, chain);
			}
		}
	}

}
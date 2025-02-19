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
using System.Collections.Generic;
using System.Threading;
using Epam.FixAntenna.Core.Tests;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.TestUtils;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Scheduler
{
	internal class ScheduledFixServerTest
	{
		private const int Port = 46392;
		private readonly string _sessionConfigPath = $"{Guid.NewGuid()}.properties";
		private ScheduledFixServer _server;
		private IFixSession _session;
		private AcceptorFixSession _acceptorSession;
		private CountdownEvent _connected;
		private CountdownEvent _disconnectedAbnormally;

		[SetUp]
		public void SetUp()
		{
			LogsCleaner.ClearDefaultLogs();
			InitClient();
		}

		[Test]
		public void TestScheduledServerAcceptsConnectionWhenNoScheduleSet()
		{
			// Arrange
			InitServer(new Config(new Dictionary<string, string>()));

			// Act
			_session.Connect();
			_connected.Wait(TimeSpan.FromSeconds(5));

			// ClassicAssert
			ClassicAssert.AreEqual(SessionState.Connected, _session.SessionState);
		}

		[Test]
		public void TestScheduledServerAcceptsConnectionWhenConnectionIsWithinSchedule()
		{
			// Arrange
			var now = DateTimeOffset.UtcNow;
			var tenMinutes = TimeSpan.FromMinutes(10);
			var configuration = GetConfiguration(now - tenMinutes, now + tenMinutes, "UTC");
			InitServer(configuration);

			// Act
			_session.Connect();
			_connected.Wait(TimeSpan.FromSeconds(5));

			// ClassicAssert
			ClassicAssert.AreEqual(SessionState.Connected, _session.SessionState);
		}

		[Test]
		public void TestScheduledServerSchedulesDisconnectWhenTradePeriodEndIsSet()
		{
			// Arrange
			var now = DateTimeOffset.UtcNow;
			var tenMinutes = TimeSpan.FromMinutes(10);
			var configuration = GetConfiguration(now - tenMinutes, now + tenMinutes, "UTC");
			InitServer(configuration);

			// Act
			_session.Connect();
			_connected.Wait(TimeSpan.FromSeconds(5));

			// ClassicAssert
			CheckingUtils.CheckWithinTimeout(
				() => _acceptorSession.IsDisconnectScheduled(), TimeSpan.FromSeconds(3));
		}

		[Test]
		public void TestScheduledServerSchedulesDisconnectWhenTradePeriodEndIsSetAndNoStartPeriod()
		{
			// Arrange
			var now = DateTimeOffset.UtcNow;
			var tenMinutes = TimeSpan.FromMinutes(10);
			var configuration = GetConfiguration(null, now + tenMinutes, "UTC");
			InitServer(configuration);

			// Act
			_session.Connect();
			_connected.Wait(TimeSpan.FromSeconds(5));

			// ClassicAssert
			CheckingUtils.CheckWithinTimeout(
				() => _acceptorSession.IsDisconnectScheduled(), TimeSpan.FromSeconds(3));
		}

		[Test]
		public void TestScheduledServerAcceptsConnectionWhenTradePeriodConfiguredPartly()
		{
			// Arrange
			var now = DateTimeOffset.UtcNow;
			var tenMinutes = TimeSpan.FromMinutes(10);
			var configuration = GetConfiguration(null, now - tenMinutes, "UTC");
			InitServer(configuration);

			// Act
			_session.Connect();
			_connected.Wait(TimeSpan.FromSeconds(5));

			// ClassicAssert
			ClassicAssert.AreEqual(SessionState.Connected, _session.SessionState);
		}

		[Test]
		public void TestScheduledServeDeniesConnectionWhenSchedulerParametersAreIncorrect()
		{
			// Arrange
			var configuration = new Config(new Dictionary<string, string>
			{
				{"tradePeriodBegin", "an incorrect value"},
				{"tradePeriodEnd", "an incorrect value"}
			});
			InitServer(configuration);

			// Act
			_session.Connect();
			_disconnectedAbnormally.Wait(TimeSpan.FromSeconds(5));

			// ClassicAssert
			ClassicAssert.IsTrue(_disconnectedAbnormally.IsSet);
		}

		[Test]
		public void TestScheduledServerAcceptsConnectionWhenConnectionIsWithinScheduleAndTimeZone()
		{
			// Arrange
			var timeShift = TimeSpan.FromHours(3);
			var customTimeZone = TimeZoneInfo.CreateCustomTimeZone("TestTZ", timeShift, "", "");
			var now = DateTimeOffset.UtcNow;
			var tenMinutes = TimeSpan.FromMinutes(10);
			var configuration = GetConfiguration(
				ConvertTime(now - tenMinutes, customTimeZone),
				ConvertTime(now + tenMinutes, customTimeZone),
				$"UTC+{timeShift}");

			InitServer(configuration);

			// Act
			_session.Connect();
			_connected.Wait(TimeSpan.FromSeconds(5));

			// ClassicAssert
			ClassicAssert.AreEqual(SessionState.Connected, _session.SessionState);
		}

		[Test]
		public void TestScheduledServerDeniesConnectionWhenConnectionIsOutOfSchedule()
		{
			// Arrange
			var now = DateTimeOffset.UtcNow;
			var tenMinutes = TimeSpan.FromMinutes(10);
			var twentyMinutes = TimeSpan.FromMinutes(20);
			var configuration = GetConfiguration(now - twentyMinutes, now - tenMinutes, "UTC");
			InitServer(configuration);

			// Act
			_session.Connect();
			_disconnectedAbnormally.Wait(TimeSpan.FromSeconds(5));

			// ClassicAssert
			ClassicAssert.IsTrue(_disconnectedAbnormally.IsSet);
		}

		[Test]
		public void TestScheduledServerAcceptsConnectionWhenConnectionIsWithinScheduleConfiguredForSession()
		{
			// Arrange
			var now = DateTimeOffset.UtcNow;
			var tenMinutes = TimeSpan.FromMinutes(10);
			var startTime = now - tenMinutes;
			var stopTime = now + tenMinutes;
			CreateSessionConfigFile(startTime, stopTime, 0);
			InitServer(_sessionConfigPath);

			// Act
			_session.Connect();
			_connected.Wait(TimeSpan.FromSeconds(5));

			// ClassicAssert
			ClassicAssert.AreEqual(SessionState.Connected, _session.SessionState);
		}

		[Test]
		public void TestScheduledServerDeniesConnectionWhenConnectionIsOutOfScheduleConfiguredForSession()
		{
			// Arrange
			var now = DateTimeOffset.UtcNow;
			var tenMinutes = TimeSpan.FromMinutes(10);
			var twentyMinutes = TimeSpan.FromMinutes(20);
			var startTime = now + tenMinutes;
			var stopTime = now + twentyMinutes;
			CreateSessionConfigFile(startTime, stopTime, 0);
			InitServer(_sessionConfigPath);

			// Act
			_session.Connect();
			_disconnectedAbnormally.Wait(TimeSpan.FromSeconds(5));

			// ClassicAssert
			ClassicAssert.IsTrue(_disconnectedAbnormally.IsSet);
		}

		[Test]
		public void TestScheduledServerAcceptsConnectionWhenConnectionIsWithinScheduleAndTimeZoneConfiguredForSession()
		{
			// Arrange
			var timeShift = TimeSpan.FromHours(3);
			var customTimeZone = TimeZoneInfo.CreateCustomTimeZone("TestTZ", timeShift, "", "");
			var now = DateTimeOffset.UtcNow;
			var tenMinutes = TimeSpan.FromMinutes(10);
			CreateSessionConfigFile(
				ConvertTime(now - tenMinutes, customTimeZone),
				ConvertTime(now + tenMinutes, customTimeZone),
				timeShift.Hours);

			InitServer(_sessionConfigPath);

			// Act
			_session.Connect();
			_connected.Wait(TimeSpan.FromSeconds(5));

			// ClassicAssert
			ClassicAssert.IsTrue(_connected.IsSet);
		}

		private void CreateSessionConfigFile(DateTimeOffset startTime, DateTimeOffset stopTime, int timeShiftInHours)
		{
			FileHelper.CreateConfigurationFileForSession(_sessionConfigPath, $@"
sessionIDs=testSession

sessions.testSession.senderCompID=acceptor
sessions.testSession.targetCompID=initiator
sessions.testSession.sessionType=acceptor
sessions.testSession.fixVersion=FIX.4.4
sessions.testSession.tradePeriodBegin=0 {startTime.Minute} {startTime.Hour} * * ?
sessions.testSession.tradePeriodEnd=0 {stopTime.Minute} {stopTime.Hour} * * ?
sessions.testSession.tradePeriodTimeZone=UTC+{timeShiftInHours}");
		}

		[TearDown]
		public void TearDown()
		{
			_session.Dispose();
			_server.Stop();
			FixSessionManager.DisposeAllSession();
			_connected.Dispose();
			_disconnectedAbnormally.Dispose();
			LogsCleaner.ClearDefaultLogs();
			FileHelper.DeleteConfigurationFileForSession(_sessionConfigPath);
		}

		private void InitServer(Config configuration)
		{
			_server = new ScheduledFixServer(configuration);
			_server.SetPort(Port);
			_server.SetListener(new FixServerListener(this));
			_server.Start();
		}

		private void InitServer(string sessionConfigPath)
		{
			_server = new ScheduledFixServer {ConfigPath = sessionConfigPath};
			_server.SetPort(Port);
			_server.SetListener(new FixServerListener(this));
			_server.Start();
		}

		private Config GetConfiguration(DateTimeOffset? startTime, DateTimeOffset? stopTime, string timeZone)
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

			return new Config(props);
		}

		private static DateTimeOffset ConvertTime(DateTimeOffset time, TimeZoneInfo timeZone)
		{
			return TimeZoneInfo.ConvertTime(time, timeZone);
		}

		private void InitClient()
		{
			_connected = new CountdownEvent(1);
			_disconnectedAbnormally = new CountdownEvent(1);

			var parameters = new SessionParameters
			{
				FixVersion = FixVersion.Fix44,
				Host = "localhost",
				Port = Port,
				SenderCompId = "initiator",
				TargetCompId = "acceptor"
			};

			_session = parameters.CreateInitiatorSession();
			_session.SetFixSessionListener(new InitiatorFixSessionListener(this));
		}

		private class FixServerListener : IFixServerListener
		{
			private readonly ScheduledFixServerTest _outerScope;

			public FixServerListener(ScheduledFixServerTest outerScope)
			{
				_outerScope = outerScope;
			}

			public void NewFixSession(IFixSession session)
			{
				_outerScope._acceptorSession = (AcceptorFixSession)session;
				session.Connect();
			}
		}

		private class InitiatorFixSessionListener : IFixSessionListener
		{
			private readonly ScheduledFixServerTest _outerInstance;

			public InitiatorFixSessionListener(ScheduledFixServerTest outerInstance)
			{
				_outerInstance = outerInstance;
			}

			public void OnNewMessage(FixMessage message)
			{
			}

			public void OnSessionStateChange(SessionState sessionState)
			{
				if (sessionState.Equals(SessionState.Connected))
				{
					_outerInstance._connected.Signal();
				}

				if (sessionState.Equals(SessionState.DisconnectedAbnormally))
				{
					_outerInstance._disconnectedAbnormally.Signal();
				}
			}
		}
	}
}

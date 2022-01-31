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
using System.IO;
using Epam.FixAntenna.TestUtils;
using Epam.FixAntenna.NetCore.FixEngine.Session.Reconect.Util;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.Reconect
{
	[TestFixture]
	internal class ReconnectSmokeTest : IFixSessionStateListener
	{
		private IExtendedFixSession _session;
		private FixSessionListenerHelper _helper;
		private long _count = 0;
		private const long ExpectedConnectCount = 3;
		private System.Threading.CountdownEvent _endTestLatch;

		[SetUp]
		public void Before()
		{
			_endTestLatch = new System.Threading.CountdownEvent(1);

			ClearLogs();
			_helper = new FixSessionListenerHelper();
			var parameters = new SessionParameters();
			parameters.Host = "localhost";
			parameters.Port = 12345;
			parameters.SenderCompId = "sss1";
			parameters.TargetCompId = "ttt1";
			parameters.Configuration.SetProperty(Config.AutoreconnectDelayInMs, "1");
			parameters.Configuration.SetProperty(Config.AutoreconnectAttempts, "3");
			_session = (IExtendedFixSession) parameters.CreateNewFixSession();
		}

		[TearDown]
		public virtual void After()
		{
			_session.Dispose();
			var sessions = FixSessionManager.Instance.SessionListCopy;
			foreach (var s in sessions)
			{
				try
				{
					s.Dispose();
				}
				catch (Exception)
				{
				}
			}
			ClearLogs();
		}

		public virtual void ClearLogs()
		{
			var logsCleaner = new LogsCleaner();
			logsCleaner.Clean("./logs");
			logsCleaner.Clean("./logs/backup");
		}

		[Test, Timeout(10000)]
		public virtual void CheckReconnectTries()
		{
			_session.SetFixSessionListener(_helper);
			((AbstractFixSession) _session).AddSessionStateListener(_helper);
			((AbstractFixSession) _session).AddSessionStateListener(this);
			try
			{
				_session.Connect();
			}
			catch (IOException)
			{
			}

			_endTestLatch.Wait();
		}

		public virtual void OnSessionStateChange(StateEvent stateEvent)
		{
			if (stateEvent.GetSessionState() == SessionState.Connecting || stateEvent.GetSessionState() == SessionState.Reconnecting)
			{
				_count++;
			}
			else if (stateEvent.GetSessionState() == SessionState.DisconnectedAbnormally && _count == ExpectedConnectCount)
			{
				_session.Dispose();
			}
			else if (stateEvent.GetSessionState() == SessionState.Dead)
			{
				_endTestLatch.Signal();
			}
		}
	}
}
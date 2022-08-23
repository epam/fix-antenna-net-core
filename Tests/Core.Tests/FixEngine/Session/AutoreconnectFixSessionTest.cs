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
using System.Linq;
using System.Net;
using System.Threading;
using Epam.FixAntenna.TestUtils;
using Epam.FixAntenna.NetCore.FixEngine.Session.Reconect.Util;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.NetCore.FixEngine.Transport.Server.Tcp;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	[TestFixture]
	internal class AutoreconnectFixSessionTest
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(AutoreconnectFixSessionTest));

		protected internal const long StateWaitMs = 7000;

		private static readonly int InfinityAutoreconnect = int.Parse(Config.InfinityAutoreconnect);
		private static readonly int NoAutoreconnect = int.Parse(Config.NoAutoreconnect);

		private TestServerInstance[] _servers = Array.Empty<TestServerInstance>();
		private int[] _ports = { 12345, 12346, 12347 };
		private const string Localhost = "localhost";

		private IFixSession _initiatorSession;

		[SetUp]
		public void SetUp()
		{
			ConfigurationHelper.StoreGlobalConfig();
		}

		[TearDown]
		public virtual void TearDown()
		{
			try
			{
				// Dispose initiator session first. Otherwise it will try to do reconnecting after the server shutdown
				_initiatorSession.Dispose();
				ShutdownServers();
			}
			catch (IOException e)
			{
				if (Log.IsDebugEnabled)
				{
					Log.Debug(e.Message, e);
				}

				Console.WriteLine(e.ToString());
				Console.Write(e.StackTrace);
			}
			finally
			{
				ConfigurationHelper.RestoreGlobalConfig();
			}
		}

		[Test]
		public virtual void TestThreeDestinationsFirstDownWithSingleAttempt()
		{
			SetUpWithAutoReconnect(1);
			_servers[0].Server.Stop();
			_servers[1].Server.Stop();
			CreateAndConnectInitiatorSession(Localhost, _ports);

			CheckingUtils.CheckWithinTimeout(() => _initiatorSession.SessionState.Equals(SessionState.DisconnectedAbnormally), TimeSpan.FromMilliseconds(StateWaitMs));
		}

		[Test]
		public virtual void TestThreeDestinationsFirstDownWithTwoAttempts()
		{
			SetUpWithAutoReconnect(2);
			_servers[0].Server.Stop();
			_servers[1].Server.Stop();
			CreateAndConnectInitiatorSession(Localhost, _ports);

			CheckingUtils.CheckWithinTimeout(() => _initiatorSession.SessionState.Equals(SessionState.Connected), TimeSpan.FromMilliseconds(StateWaitMs));
			CheckingUtils.CheckWithinTimeout(() => _servers[2].Session.SessionState.Equals(SessionState.Connected), TimeSpan.FromMilliseconds(StateWaitMs));
		}

		[Test]
		public virtual void TestThreeDestinationsFirstAllDownWithInfiniteAttempts()
		{
			SetUpWithAutoReconnect(InfinityAutoreconnect);
			_servers[0].Server.Stop();
			_servers[1].Server.Stop();
			_servers[2].Server.Stop();
			CreateAndConnectInitiatorSession(Localhost, _ports);
			Thread.Sleep(2000);

			Assert.IsNull(_servers[0].Session);
			Assert.IsNull(_servers[1].Session);
			Assert.IsNull(_servers[2].Session);

			_servers[1].Server.Start();
			CheckingUtils.CheckWithinTimeout(() => _initiatorSession.SessionState.Equals(SessionState.Connected), TimeSpan.FromMilliseconds(StateWaitMs));
			CheckingUtils.CheckWithinTimeout(() => _servers[1].Session.SessionState.Equals(SessionState.Connected), TimeSpan.FromMilliseconds(StateWaitMs));
			Assert.IsNull(_servers[0].Session);
			Assert.IsNull(_servers[2].Session);
		}

		[Test]
		public virtual void NoSessionMessagesShouldBeSentBeforeLogon()
		{
			SetUpWithAutoReconnect(NoAutoreconnect);
			CreateAndConnectInitiatorSession(Localhost, new[] { _ports[0] });

			CheckingUtils.CheckWithinTimeout(() => _initiatorSession.SessionState.Equals(SessionState.Connected), TimeSpan.FromMilliseconds(StateWaitMs));
			Assert.IsNotNull(_servers[0].Session);

			_initiatorSession.Disconnect(string.Empty);
			CheckingUtils.CheckWithinTimeout(() => _servers[0].Session.SessionState.Equals(SessionState.Disconnected), TimeSpan.FromMilliseconds(StateWaitMs));
			CheckingUtils.CheckWithinTimeout(() => _initiatorSession.SessionState.Equals(SessionState.Disconnected), TimeSpan.FromMilliseconds(StateWaitMs));
			_servers[0].Session.Pumper.SendOutOfTurn("0", RawFixUtil.GetFixMessage("35=0\u0001"));
			_initiatorSession.Connect();
			CheckingUtils.CheckWithinTimeout(() => _initiatorSession.SessionState.Equals(SessionState.Connected), TimeSpan.FromMilliseconds(StateWaitMs));
		}

		[Test]
		public virtual void TestTwoDestinationsFirstDown()
		{
			SetUpWithAutoReconnect(InfinityAutoreconnect);
			_servers[0].Server.Stop();
			CreateAndConnectInitiatorSession(Localhost, new[] { _ports[0], _ports[1] });
			CheckingUtils.CheckWithinTimeout(() => _servers[1].Session != null && _servers[1].Session.SessionState.Equals(SessionState.Connected), TimeSpan.FromMilliseconds(StateWaitMs));
			CheckingUtils.CheckWithinTimeout(() => _initiatorSession.SessionState.Equals(SessionState.Connected), TimeSpan.FromMilliseconds(StateWaitMs));
		}

		[Test]
		public virtual void TestSingleDestination()
		{
			SetUpWithAutoReconnect(NoAutoreconnect);
			CreateAndConnectInitiatorSession(Localhost, new[] { _ports[0] });

			CheckingUtils.CheckWithinTimeout(() => _initiatorSession.SessionState.Equals(SessionState.Connected), TimeSpan.FromMilliseconds(StateWaitMs));
			_servers[0].Session.Dispose();
			CheckingUtils.CheckWithinTimeout(() => _initiatorSession.SessionState.Equals(SessionState.DisconnectedAbnormally), TimeSpan.FromMilliseconds(StateWaitMs));
		}

		[Test]
		public virtual void TestTwoDestinations()
		{
			SetUpWithAutoReconnect(InfinityAutoreconnect); //infinite reconnect
			CreateAndConnectInitiatorSession(Localhost, new[] { _ports[0], _ports[1] });

			CheckingUtils.CheckWithinTimeout(() => _initiatorSession.SessionState.Equals(SessionState.Connected), TimeSpan.FromMilliseconds(StateWaitMs));
			CheckingUtils.CheckWithinTimeout(() => _servers[0].Session.SessionState.Equals(SessionState.Connected), TimeSpan.FromMilliseconds(StateWaitMs));
			//stop server1 and dispose session to cause destination change
			_servers[0].Session.Dispose();
			_servers[0].Server.Stop();
			CheckingUtils.CheckWithinTimeout(() => _servers[0].Session.SessionState.Equals(SessionState.Dead), TimeSpan.FromMilliseconds(StateWaitMs));
			//initiatorSession connecting to server2
			CheckingUtils.CheckWithinTimeout(() => _initiatorSession.SessionState.Equals(SessionState.Connected), TimeSpan.FromMilliseconds(StateWaitMs));
			// null check added as _servers[1].Session is not initialized at the first request
			CheckingUtils.CheckWithinTimeout(() => SessionState.IsConnected(_servers[1].Session?.SessionState), TimeSpan.FromMilliseconds(StateWaitMs));
			_servers[1].Session.Dispose();
			_servers[1].Server.Stop();
			CheckingUtils.CheckWithinTimeout(() => _servers[1].Session.SessionState.Equals(SessionState.Dead), TimeSpan.FromMilliseconds(StateWaitMs));
			CheckingUtils.CheckWithinTimeout(() => _initiatorSession.SessionState.Equals(SessionState.Reconnecting), TimeSpan.FromMilliseconds(StateWaitMs));
			//now all acceptors are stopped
			//do another rotation
			_servers[0].Server.Start();
			CheckingUtils.CheckWithinTimeout(() => _servers[0].Session.SessionState.Equals(SessionState.Connected), TimeSpan.FromMilliseconds(StateWaitMs));
			CheckingUtils.CheckWithinTimeout(() => _initiatorSession.SessionState.Equals(SessionState.Connected), TimeSpan.FromMilliseconds(StateWaitMs));
			CheckingUtils.CheckWithinTimeout(() => _servers[1].Session.SessionState.Equals(SessionState.Dead), TimeSpan.FromMilliseconds(StateWaitMs));
		}

		[Test]
		public virtual void TestSingleReconnectingDestinationWithDisconnect()
		{
			Config.GlobalConfiguration.SetProperty(Config.AutoreconnectAttempts, 0.ToString());
			Config.GlobalConfiguration.SetProperty(Config.AutoreconnectDelayInMs, 10000.ToString());
			CreateAndConnectInitiatorSession(Localhost, new[] { _ports[1] });

			CheckingUtils.CheckWithinTimeout(() => _initiatorSession.SessionState.Equals(SessionState.Reconnecting), TimeSpan.FromMilliseconds(StateWaitMs));
			_initiatorSession.Disconnect("test");
			CheckingUtils.CheckWithinTimeout(() => _initiatorSession.SessionState.Equals(SessionState.Disconnected), TimeSpan.FromMilliseconds(StateWaitMs));
		}

		private void CreateAndConnectInitiatorSession(string hostname, int[] ports)
		{
			var destinations = ports.Select(port => new DnsEndPoint(hostname, port)).ToList();

			var parameters = new SessionParameters();
			parameters.AddAllDestinations(destinations);
			_initiatorSession = StandardFixSessionFactory.GetFactory(parameters).CreateInitiatorSession(parameters);
			_initiatorSession.SetFixSessionListener(new FixSessionListenerHelper());
			_initiatorSession.Connect();
		}

		private void SetUpWithAutoReconnect(int maxAttempts)
		{
			// return to default values
			Config.GlobalConfiguration.SetProperty(Config.AutoreconnectAttempts, maxAttempts.ToString());
			Config.GlobalConfiguration.SetProperty(Config.AutoreconnectDelayInMs, 200.ToString());
			ClearLogs();
			CreateServers();
		}

		private bool ClearLogs()
		{
			var logsCleaner = new LogsCleaner();
			return logsCleaner.Clean("./logs") && logsCleaner.Clean("./logs/backup");
		}

		private void CreateServers()
		{
			_servers = new TestServerInstance[_ports.Length];
			for (var i = 0; i < _ports.Length; i++)
			{
				_servers[i] = TestServerInstance.CreateAndStart(_ports[i]);
			}
		}

		private void ShutdownServers()
		{
			foreach (var server in _servers)
			{
				TestServerInstance.Shutdown(server);
			}
		}

		private class TestServerInstance
		{
			public AbstractFixSession Session { get; set; }
			public FixServer Server { get; private set; }

			public static TestServerInstance CreateAndStart(int port)
			{
				var tsi = new TestServerInstance { Server = new FixServer() };
				tsi.Server.AddServer(port, new TcpServer(port));
				tsi.Server.SetListener(new FixServerListenerImpl(tsi));
				tsi.Server.Start();
				return tsi;
			}

			public static void Shutdown(TestServerInstance inst)
			{
				inst.Server?.Stop();
				inst.Session?.Dispose();
				inst.Session = null;
				inst.Server = null;
			}
		}

		private class FixServerListenerImpl : IFixServerListener
		{
			private readonly TestServerInstance _inst;

			public FixServerListenerImpl(TestServerInstance inst)
			{
				_inst = inst;
			}

			public void NewFixSession(IFixSession session)
			{
				try
				{
					_inst.Session = (AbstractFixSession)session;
					_inst.Session.Connect();
				}
				catch (IOException e)
				{
					Console.WriteLine(e.ToString());
					Console.Write(e.StackTrace);
				}
			}
		}
	}
}
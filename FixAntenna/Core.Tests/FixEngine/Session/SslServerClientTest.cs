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
using System.Collections;
using System.Collections.Generic;
using Epam.FixAntenna.TestUtils;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.FixEngine.Transport;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	[TestFixture]
	internal class SslServerConnectTest
	{
		private FixServer _server;
		private static int _sslPort = 1234;
		private const int StatusWaitMs = 5000;

		private static readonly Dictionary<string, string> Ssl = new Dictionary<string, string>
		{
			{Config.RequireSsl, "true"}
		};

		private static readonly Dictionary<string, string> Ssl3Protocol = new Dictionary<string, string>
		{
			{Config.SslProtocol, "Ssl3"}
		};

		private static readonly Dictionary<string, string> Tls12Protocol = new Dictionary<string, string>
		{
			{Config.SslProtocol, "Tls12"}
		};

		private static readonly Dictionary<string, string> ValidServerAndCheckPeer = new Dictionary<string, string>
		{
			{Config.SslCaCertificate, "TestCA.crt"},
			{Config.SslCertificate, "server.pfx"},
			{Config.SslCertificatePassword, "password"},
			{Config.SslCheckCertificateRevocation, "true"},
			{Config.SslValidatePeerCertificate, "true"}
		};

		private static readonly Dictionary<string, string> ValidServerDontCheckPeer = new Dictionary<string, string>
		{
			{Config.SslCertificate, "server.pfx"},
			{Config.SslCertificatePassword, "password"},
			{Config.SslCheckCertificateRevocation, "true"}, // also disabled as far as SslValidatePeerCertificate=false
			{Config.SslValidatePeerCertificate, "false"}
		};

		private static readonly Dictionary<string, string> ServerWoCaCheckPeer = new Dictionary<string, string>
		{
			//{Config.SslCaCertificate, "TestCA.crt"},
			{Config.SslCertificate, "server.pfx"},
			{Config.SslCertificatePassword, "password"},
			{Config.SslCheckCertificateRevocation, "true"},
			{Config.SslValidatePeerCertificate, "true"}
		};

		private static readonly Dictionary<string, string> WrongServerDontCheckPeer = new Dictionary<string, string>
		{
			{Config.SslCaCertificate, "TestCA.crt"},
			{Config.SslCertificate, "server_missed.pfx"},
			{Config.SslCertificatePassword, "password"},
			{Config.SslCheckCertificateRevocation, "true"}, // also disabled as far as SslValidatePeerCertificate=false
			{Config.SslValidatePeerCertificate, "false"}
		};

		private static readonly Dictionary<string, string> ServerWrongPassword = new Dictionary<string, string>
		{
			{Config.SslCaCertificate, "TestCA.crt"},
			{Config.SslCertificate, "server.pfx"},
			{Config.SslCertificatePassword, "not_password"},
			{Config.SslCheckCertificateRevocation, "true"},
			{Config.SslValidatePeerCertificate, "true"}
		};

		private static readonly Dictionary<string, string> ValidClientAndCheckPeer = new Dictionary<string, string>
		{
			{Config.SslCaCertificate, "TestCA.crt"},
			{Config.SslCertificate, "client.pfx"},
			{Config.SslCertificatePassword, "password"},
			{Config.SslCheckCertificateRevocation, "true"},
			{Config.SslValidatePeerCertificate, "true"},
			{Config.SslServerName, "localhost"}
		};

		private static readonly Dictionary<string, string> ValidClientDontCheckPeer = new Dictionary<string, string>
		{
			{Config.SslCertificate, "client.pfx"},
			{Config.SslCertificatePassword, "password"},
			{Config.SslCheckCertificateRevocation, "true"}, // also disabled as far as SslValidatePeerCertificate=false
			{Config.SslValidatePeerCertificate, "false"},
			{Config.SslServerName, "localhost"}
		};

		private static readonly Dictionary<string, string> WrongClientCertAndCheckPeer = new Dictionary<string, string>
		{
			{Config.SslCaCertificate, "TestCA.crt"},
			{Config.SslCertificate, "client_missed.pfx"},
			{Config.SslCertificatePassword, "password"},
			{Config.SslCheckCertificateRevocation, "true"},
			{Config.SslValidatePeerCertificate, "true"},
			{Config.SslServerName, "localhost"}
		};

		private static readonly Dictionary<string, string> ClientWrongPassword = new Dictionary<string, string>
		{
			{Config.SslCaCertificate, "TestCA.crt"},
			{Config.SslCertificate, "client.pfx"},
			{Config.SslCertificatePassword, "not_password"},
			{Config.SslCheckCertificateRevocation, "true"},
			{Config.SslValidatePeerCertificate, "true"},
			{Config.SslServerName, "localhost"}
		};

		private static readonly Dictionary<string, string> ExpiredClientAndCheckPeer = new Dictionary<string, string>
		{
			{Config.SslCaCertificate, "TestCA.crt"},
			{Config.SslCertificate, "expiredClient.pfx"},
			{Config.SslCertificatePassword, "password"},
			{Config.SslCheckCertificateRevocation, "true"},
			{Config.SslValidatePeerCertificate, "true"},
			{Config.SslServerName, "localhost"}
		};

		private static readonly Dictionary<string, string> ClientWoCertAndCheckPeer = new Dictionary<string, string>
		{
			{Config.SslCaCertificate, "TestCA.crt"},
			{Config.SslCheckCertificateRevocation, "true"},
			{Config.SslValidatePeerCertificate, "true"},
			{Config.SslServerName, "localhost"}
		};

		[SetUp]
		public void Setup()
		{
			ConfigurationHelper.StoreGlobalConfig();
		}

		[TearDown]
		public virtual void TearDown()
		{
			_server.Stop();
			FixSessionManager.CloseAllSession();
			FixSessionManager.DisposeAllSession();
			FixSessionManager.Instance.RemoveAllSessions();

			ConfigurationHelper.RestoreGlobalConfig();

			Assert.IsTrue(new LogsCleaner().Clean("./logs") && new LogsCleaner().Clean("./logs/backup"), "Can't clean logs after tests");

			ConnectionAuthenticator.ClearCertCache();
		}

		public static IEnumerable CanConnectCases
		{
			get
			{
				// mutually authenticated, both the server and client have valid certificate, default protocol
				yield return new TestCaseData(
					CreateParameters(Ssl, ValidServerAndCheckPeer),
					CreateInitiatorParameters(Ssl, ValidClientAndCheckPeer))
					{ TestName = "Mutually authenticated" };

				// 1 way authenticated, server has valid certificate, client doesn't, PeerValidation disabled
				yield return new TestCaseData(
					CreateParameters(Ssl, ValidServerDontCheckPeer),
					CreateInitiatorParameters(Ssl, ClientWoCertAndCheckPeer))
					{ TestName = "1 way authenticated" };
			}
		}

		public static IEnumerable CannotConnectCases
		{
			get
			{
				// server has valid certificate, but wrong password, client has valid certificate
				yield return new TestCaseData(
						CreateParameters(Ssl, ServerWrongPassword),
						CreateInitiatorParameters(Ssl, ValidClientAndCheckPeer))
				{ TestName = "Wrong password for server certificate" };

				// server has valid certificate, client too, but wrong password
				yield return new TestCaseData(
						CreateParameters(Ssl, ValidServerAndCheckPeer),
						CreateInitiatorParameters(Ssl, ClientWrongPassword))
				{ TestName = "Wrong password for client certificate" };

				// server has valid certificate, client certificate expired
				yield return new TestCaseData(
					CreateParameters(Ssl, ValidServerAndCheckPeer),
					CreateInitiatorParameters(Ssl, ExpiredClientAndCheckPeer))
				{ TestName = "Valid server, client cert expired" };

				// client has valid certificate, server doesn't
				yield return new TestCaseData(
					CreateParameters(Ssl, WrongServerDontCheckPeer),
					CreateInitiatorParameters(Ssl, ValidClientDontCheckPeer))
				{ TestName = "Invalid server certificate" };

				// both valid certificates, but CA certificate not provided
				yield return new TestCaseData(
					CreateParameters(Ssl, ServerWoCaCheckPeer),
					CreateInitiatorParameters(Ssl, ValidClientAndCheckPeer))
				{ TestName = "CA cert not provided" };

				// cannot find certificate
				yield return new TestCaseData(
					CreateParameters(Ssl, ValidServerAndCheckPeer),
					CreateInitiatorParameters(Ssl, WrongClientCertAndCheckPeer))
				{ TestName = "Cannot find certificate" };

				// different protocols
				yield return new TestCaseData(
					CreateParameters(Ssl, ValidServerAndCheckPeer, Ssl3Protocol),
					CreateInitiatorParameters(Ssl, ValidClientAndCheckPeer, Tls12Protocol))
				{ TestName = "Different protocols" };
			}
		}

		/// <summary>
		/// Test cases when connection possible.
		/// </summary>
		/// <param name="server"></param>
		/// <param name="client"></param>
		[TestCaseSource(nameof(CanConnectCases))]
		public void CanConnectTest(SessionParameters server, SessionParameters client)
		{
			CreateAndStartServer(server);

			var fixSession = client.CreateInitiatorSession();
			try
			{
				fixSession.Connect();

				CheckingUtils.CheckWithinTimeout(() => fixSession.SessionState == SessionState.Connected,
					TimeSpan.FromMilliseconds(StatusWaitMs));
			}
			finally
			{
				fixSession.Dispose();
			}
		}

		/// <summary>
		/// Test cases when connection isn't possible.
		/// </summary>
		/// <param name="server"></param>
		/// <param name="client"></param>
		[TestCaseSource(nameof(CannotConnectCases))]
		public void CannotConnectTest(SessionParameters server, SessionParameters client)
		{
			CreateAndStartServer(server);

			var fixSession = client.CreateInitiatorSession();

			fixSession.Connect();

			CheckingUtils.CheckWithinTimeout(() => fixSession.SessionState == SessionState.DisconnectedAbnormally,
				TimeSpan.FromMilliseconds(StatusWaitMs));

			fixSession.Dispose();
		}

		[Test, Category("Bug"), Property("JIRA", "BBP-26735")]
		public void DuplicatePortTest()
		{
			var clientParams = CreateInitiatorParameters(Ssl, ValidClientAndCheckPeer);
			var serverParams = CreateParameters(Ssl, ValidServerAndCheckPeer);

			_server = new FixServer(serverParams.Configuration);
			_server.SetListener(new DummyServerListener());

			// same port for regular and secure connection. Should be secure connection in result.
			Assert.DoesNotThrow(() =>
			{
				_server.Ports = new[] { _sslPort, _sslPort };
				_server.SslPorts = new[] { _sslPort, _sslPort };
				_server.Start();
			});

			var fixSession = clientParams.CreateInitiatorSession();

			try
			{
				fixSession.Connect();

				CheckingUtils.CheckWithinTimeout(() => fixSession.SessionState == SessionState.Connected,
					TimeSpan.FromMilliseconds(StatusWaitMs));
			}
			finally
			{
				fixSession.Dispose();
			}
		}

		private static SessionParameters CreateParameters(params Dictionary<string, string>[] options)
		{
			var sp = new SessionParameters();
			foreach (var dictionary in options)
			{
				AppendProperties(sp, dictionary);
			}
			return sp;
		}

		private static SessionParameters CreateInitiatorParameters(params Dictionary<string, string>[] options)
		{
			var @params = CreateParameters(options);
			@params.FixVersion = FixVersion.Fix44;
			@params.Host = "localhost";
			@params.Port = _sslPort;
			@params.SenderCompId = "client";
			@params.TargetCompId = "server";
			@params.ForceSeqNumReset = ForceSeqNumReset.Always;
			return @params;
		}

		private static void AppendProperties(SessionParameters parameters, Dictionary<string, string> options)
		{
			foreach (var option in options)
			{
				parameters.Configuration.SetProperty(option.Key, option.Value);
			}
		}

		private class DummyServerListener : IFixServerListener
		{
			public void NewFixSession(IFixSession session)
			{
				session.Connect();
			}
		}

		private void CreateAndStartServer(SessionParameters serverParams)
		{
			_server = new FixServer(serverParams.Configuration);
			_server.SslPorts = new[] { _sslPort };
			_server.SetListener(new DummyServerListener());
			_server.Start();
		}
	}
}
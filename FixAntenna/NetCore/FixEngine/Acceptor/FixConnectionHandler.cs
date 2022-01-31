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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.NetCore.FixEngine.Transport;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Acceptor
{
	internal class FixConnectionHandler : IConnectionHandler
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(FixConnectionHandler));
		private readonly ISessionTransportFactory _transportFactory;
		private readonly LogonMessageParser _logonMessageParser;
		private int _logonWaitTimeout = 5000; // ms

		private readonly SessionAcceptorStrategyHandler _sessionAcceptorStrategyHandler;
		private IConfiguredSessionRegister _configuredSessionRegister;

		public FixConnectionHandler(SessionAcceptorStrategyHandler acceptorStrategyHandler, ISessionTransportFactory transportFactory)
		{
			_sessionAcceptorStrategyHandler = acceptorStrategyHandler;
			_logonMessageParser = new LogonMessageParser();
			_transportFactory = transportFactory;
		}

		public virtual void SetFixServerListener(IFixServerListener listener)
		{
			_sessionAcceptorStrategyHandler.SetSessionListener(listener);
		}

		public virtual void SetTimeout(int timeout)
		{
			_logonWaitTimeout = timeout;
		}

		public virtual void SetConfiguredSessionRegister(IConfiguredSessionRegister configuredSessionRegister)
		{
			_configuredSessionRegister = configuredSessionRegister;
		}

		public virtual void RegisterAcceptorSession(SessionParameters sessionParameters)
		{
			_configuredSessionRegister.RegisterSession(sessionParameters);
		}

		public virtual void UnregisterAcceptorSession(SessionParameters sessionParameters)
		{
			_configuredSessionRegister.UnregisterSession(sessionParameters);
		}

		/// <inheritdoc />
		public virtual void OnConnect(ITransport transport)
		{
	//        // TODO the MAX_MESSAGE_SIZE and VALIDATE_CHECK_SUM should be taken from session parameters
	//        int maxMessageSize = Configuration.getGlobalConfiguration().getPropertyAsInt(Configuration.MAX_MESSAGE_SIZE);
	//        boolean validateCheckSum = Configuration.getGlobalConfiguration().getPropertyAsBoolean(Configuration.VALIDATE_CHECK_SUM, true);
	//        final AcceptorFIXTransport fixTransport = new AcceptorFIXTransport(transport, maxMessageSize, validateCheckSum);
			var globalConfiguration = Config.GlobalConfiguration;
			var fixTransport = _transportFactory.CreateAcceptorTransport(transport, globalConfiguration);
			try
			{
				var logon = new FixMessage();
				var readLogonTimeTicks = WaitForLogon(fixTransport, logon);
				if (readLogonTimeTicks <= 0)
				{
					return;
				}

				if (!FixMessageUtil.IsLogon(logon))
				{
					if (Log.IsInfoEnabled)
					{
						Log.Info(
							$"Initial message is not a logon: {logon.ToPrintableString()} received on local endpoint: {transport.LocalEndPoint.AsString()} from remote endpoint: {transport.RemoteEndPoint.AsString()}");
					}

					if (Log.IsDebugEnabled)
					{
						Log.Debug($"logon variable:{logon.ToPrintableString()}");
					}

					CloseTransport(fixTransport);
					return;
				}

				var parseResult = _logonMessageParser.ParseLogon(logon, transport.RemoteEndPoint.Address.AsString(), transport.LocalEndPoint.Port);
				var sessionParameters = parseResult.SessionParameters;
				sessionParameters.LogonReadTimeTicks = readLogonTimeTicks;

				if (Log.IsInfoEnabled)
				{
					Log.Info(
						$"Logon message received on local endpoint: {transport.LocalEndPoint.AsString()} from: {transport.RemoteEndPoint.AsString()} for sessionID: {sessionParameters.SessionId}");
				}

				// check if secured connection required
				var isSslConfigured = sessionParameters.Configuration.GetPropertyAsBoolean(Config.RequireSsl);
				var isConnectionSecure = transport.IsSecured;
				if (isSslConfigured && !isConnectionSecure)
				{
					Log.Error($"Session {sessionParameters.SessionId} configured as secure, but connected on unsecured connection from remote endpoint {transport.RemoteEndPoint.AsString()}.");

					CloseTransport(fixTransport);
					return;
				}

				if (Log.IsDebugEnabled)
				{
					Log.Debug($"Parameters: {sessionParameters}");
					Log.Debug($"App version: {sessionParameters.AppVersion}");
				}

				_sessionAcceptorStrategyHandler.HandleIncomingConnection(sessionParameters, fixTransport);
			}
			catch (ThreadInterruptedException)
			{
				// do nothing
				Log.Trace("Thread interrupted.");
			}
			catch (DuplicateSessionException e)
			{
				if (Log.IsDebugEnabled)
				{
					Log.Warn("DuplicatedSessionException", e);
				}
				else
				{
					Log.Warn(e);
				}

				CloseTransport(fixTransport);
			}
			catch (Exception e)
			{
				Log.Error("Exception:", e);
				CloseTransport(fixTransport);
			}
		}

		private long WaitForLogon(IFixTransport fixTransport, FixMessage login)
		{
			var messageWaiter = new LogonMessageWaiter(fixTransport, _logonWaitTimeout, login);
			if (messageWaiter.IsLogonReceived())
			{
				var readMessageTimeTicks = messageWaiter.ReadMessageTimeTicks;
				return readMessageTimeTicks > 0 ? readMessageTimeTicks : DateTimeHelper.CurrentTicks;
			}

			return -1;
		}

		private void CloseTransport(IFixTransport fixTransport)
		{
			try
			{
				fixTransport.Close();
			}
			catch (IOException e)
			{
				if (Log.IsDebugEnabled)
				{
					Log.Warn($"Ignoring exception while closing transport: {e}", e);
				}
				else
				{
					Log.Warn($"Ignoring exception while closing transport: {e}");
				}
			}
		}

		public virtual void Dispose()
		{
			try
			{
				_sessionAcceptorStrategyHandler.CloseAllRegisteredSessions();
			}
			catch (IOException e)
			{
				if (Log.IsDebugEnabled)
				{
					Log.Warn($"Ignoring exception while closing session: {e}", e);
				}
				else
				{
					Log.Warn($"Ignoring exception while closing session: {e}");
				}
			}
		}

		public virtual IList<SessionParameters> GetRegisterAcceptorSession()
		{
			return _sessionAcceptorStrategyHandler.ConfiguredSessionRegister.RegisteredSessions;
		}
	}
}
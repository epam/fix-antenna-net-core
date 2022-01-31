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
using System.Linq;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Acceptor;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.FixEngine.Transport.Server;
using Epam.FixAntenna.NetCore.FixEngine.Transport.Server.Tcp;

namespace Epam.FixAntenna.NetCore.FixEngine
{
	/// <summary>
	/// Generic FIXServer implementation.
	/// </summary>
	public class FixServer
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(FixServer));

		private readonly IConnectionHandler _connectionHandler;
		private readonly string _acceptorStrategy;
		private readonly IList<IFixServerStatusListener> _statusListeners = new List<IFixServerStatusListener>();

		private IConnectionValidator _connectionValidator;

		private readonly ConfigurationAdapter ConfigAdapter;
		private IConfiguredSessionRegister ConfiguredSessionRegister;

		/// <summary>
		/// Servers bound to ports
		/// </summary>
		private Dictionary<int, IServer> _servers = new Dictionary<int, IServer>();

		/// <summary>
		/// Creates the fix server.
		/// </summary>
		public FixServer() : this(Config.GlobalConfiguration)
		{
		}

		/// <summary>
		/// Creates the fix server.
		/// </summary>
		public FixServer(Config conf)
		{
			ConfiguredSessionRegister = new ConfiguredSessionRegisterImpl();
			ConfigAdapter = new ConfigurationAdapter(conf);

			FixSessionManager.Init();

			_acceptorStrategy = ConfigAdapter.ServerAcceptorStrategy;

			_connectionHandler = GetConnectionHandler(conf);
			var logonWaitTimeout = (int)ConfigAdapter.LogonWaitTimeout;
			_connectionHandler.SetTimeout(logonWaitTimeout);
		}

		/// <summary>
		/// Gets or sets the NIC (address) to listen to.
		/// </summary>
		public virtual string Nic { get; set; }

		/// <summary>
		/// Gets or sets the set of ports to listen to.
		/// </summary>
		public int[] Ports
		{
			get => _servers.Keys.Where(p => !ConfigAdapter.IsSslPort(p)).ToArray();
			set
			{
				if (!IsStarted)
				{
					ConfigAdapter.Configuration.SetProperty(Config.Port, string.Join(",", value));
					LoadPortsFromConfig();
				}
				else
				{
					Log.Error("Cannot set Ports while server(s) is running.");
				}
			}
		}

		/// <summary>
		/// Gets or sets the set of secured ports to listen to.
		/// </summary>
		public int[] SslPorts
		{
			get => _servers.Keys.Where(p => ConfigAdapter.IsSslPort(p)).ToArray();
			set
			{
				if (!IsStarted)
				{
					ConfigAdapter.Configuration.SetProperty(Config.SslPort, string.Join(",", value));
					LoadPortsFromConfig();
				}
				else
				{
					Log.Error("Cannot set SslPorts while server(s) is running.");
				}
			}
		}

		private FixConnectionHandler GetConnectionHandler(Config configuration)
		{
			var connectionHandler = new FixConnectionHandler(GetAcceptorStrategy(configuration), GetTransportFactory(configuration));
			connectionHandler.SetConfiguredSessionRegister(ConfiguredSessionRegister);
			return connectionHandler;
		}

		private ISessionTransportFactory GetTransportFactory(Config configuration)
		{
			return new DefaultSessionTransportFactory();
		}

		private SessionAcceptorStrategyHandler GetAcceptorStrategy(Config configuration)
		{
			try
			{
				var type = Type.GetType(_acceptorStrategy) ?? throw new InvalidOperationException();
				var acceptorStrategyHandler = (SessionAcceptorStrategyHandler)Activator.CreateInstance(type);
				acceptorStrategyHandler.Init(configuration, ConfiguredSessionRegister);
				return acceptorStrategyHandler;
			}
			catch (Exception e)
			{
				if (Log.IsErrorEnabled)
				{
					Log.Error("Can't initialize SessionAcceptorStrategyHandler. Maybe " + ConfigAdapter.ServerAcceptorStrategy + " parameter is wrong. " + e.Message);
				}

				throw new InvalidOperationException("Can't initialize SessionAcceptorStrategyHandler." + e, e);
			}
		}

		/// <summary>
		/// Replaces default TCPServer implementation with custom Server implementation.
		/// </summary>
		/// <param name="server"> implementation specified by user </param>
		/// <seealso cref="IServer"> </seealso>
		private void SetServer(IServer server)
		{
			_servers = new Dictionary<int, IServer> { { 0, server } };
		}

		internal void AddServer(int port, IServer server)
		{
			_servers.Add(port, server);
		}

		/// <summary>
		/// Sets listener.
		/// </summary>
		/// <param name="listener"> - user specified listener </param>
		public virtual void SetListener(IFixServerListener listener)
		{
			_connectionHandler.SetFixServerListener(listener);
		}

		public virtual void SetConnectionValidator(IConnectionValidator connectionValidator)
		{
			_connectionValidator = connectionValidator;
		}

		/// <summary>
		/// Sets login timeout.
		/// </summary>
		/// <param name="loginWaitTimeout"> the login timeout in mils </param>
		public virtual void SetLoginWaitTimeout(int loginWaitTimeout)
		{
			_connectionHandler.SetTimeout(loginWaitTimeout);
		}

		private int LoadPortsFromConfig()
		{
			_servers = new Dictionary<int, IServer>();
			AddPorts(ConfigAdapter.Ports);
			AddPorts(ConfigAdapter.SslPorts);
			return _servers.Count;
		}

		private void AddPorts(IEnumerable<int> ports)
		{
			foreach (var port in ports)
			{
				if (_servers.ContainsKey(port))
				{
					Log.Warn($"Server on port {port} has been configured already. Configuration will be overriden.");
					_servers[port] = new TcpServer(Nic, port, ConfigAdapter);
				}
				else
				{
					_servers.Add(port, new TcpServer(Nic, port, ConfigAdapter));
				}
			}
		}

		/// <summary>
		/// Sets the only port to listen on.
		/// </summary>
		/// <param name="port"> port to listen on </param>
		public virtual void SetPort(int port)
		{
			Ports = new []{port};
		}

		/// <summary>
		/// Actually starts the servers.
		/// </summary>
		/// <returns> true if all servers started successfully
		/// false - otherwise (server will add WARN messages with description of each problem to log) </returns>
		/// <exception cref="IOException">
		/// if unable to start the server at least on one port. In this case the first received exception will be thrown.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// if server port(s) were not set by configuration or one of those methods:
		/// <see cref="SetPorts"/> or <see cref="SetPort"/> or <see cref="SetServer"/>
		/// </exception>
		public virtual bool Start()
		{
			if ((_servers == null || !_servers.Any()) && LoadPortsFromConfig() == 0)
			{
				throw new InvalidOperationException("Cannot start FixServer. Port(s) not configured.");
			}

			var startedSuccessfully = true;
			var totalFail = true;
			RegisterConfiguredSessions();

			IOException ex = null;
			foreach (var server in _servers)
			{
				try
				{
					StartServer(server.Value, server.Key);
					totalFail = false;
				}
				catch (IOException e)
				{
					ex = e;
					startedSuccessfully = false;
					if (Log.IsDebugEnabled)
					{
						Log.Warn("Unable start server on port " + server.Key, e);
					}
					else
					{
						Log.Warn("Unable start server on port " + server.Key + ". " + e.Message);
					}
				}
			}

			if (totalFail && ex != null)
			{
				throw ex;
			}

			if (startedSuccessfully)
			{
				NotifyStarted();
			}

			return startedSuccessfully;
		}

		private void StartServer(IServer server, int port)
		{
			server.SetIncomingConnectionListener(GetConnectionListener());
			server.Start();

			if (Log.IsInfoEnabled)
			{
				Log.Info("Server started on port " + port + (ConfigAdapter.IsSslPort(port) ? " (secure)" : string.Empty));
			}
		}

		private IConnectionListener GetConnectionListener()
		{
			IConnectionListener listener = _connectionHandler;
			if (_connectionValidator != null)
			{
				listener = new ConnectionValidatorListener(_connectionHandler, _connectionValidator);
			}

			return listener;
		}

		public virtual void RegisterConfiguredSessions()
		{
			var parameters = string.IsNullOrEmpty(ConfigPath)
				? SessionParametersBuilder.BuildAcceptorSessionParametersList()
				: SessionParametersBuilder.BuildAcceptorSessionParametersList(ConfigPath);

			foreach (var item in parameters.Values)
			{
				RegisterAcceptorSession(item);
			}
		}

		/// <summary>
		/// Stops the server.
		/// </summary>
		/// <exception cref="IOException"> if stop was unsuccessful </exception>
		/// <exception cref="InvalidOperationException">
		/// if servers were not set by one of those methods: <see cref="SetPorts"/> or <see cref="SetPort"/> or <see cref="SetServer"/>
		/// </exception>
		public virtual void Stop()
		{
			if (_servers == null || !_servers.Any())
			{
				throw new InvalidOperationException("Server is not set");
			}

			foreach (var server in _servers)
			{
				try
				{
					server.Value.Stop();
				}
				catch (IOException e)
				{
					if (Log.IsDebugEnabled)
					{
						Log.Warn("Problem with stopping server on port " + server.Key, e);
					}
					else
					{
						Log.Warn("Problem with stopping server on port " + server.Key + ". " + e.Message);
					}
				}
			}

			_connectionHandler.Dispose();
			NotifyStopped();
		}

		/// <summary>
		/// Register the session parameters of acceptor.
		/// <para></para>
		/// </summary>
		/// <param name="sessionParameters"> the sessionParameters </param>
		public virtual void RegisterAcceptorSession(SessionParameters sessionParameters)
		{
			if (sessionParameters.HasPort)
			{
				if (!ValidateAcceptorSessionPort(sessionParameters))
				{
					throw new ArgumentException("Server does not listen port '" + sessionParameters.Port +
												"' which configured in FIX Session '" + sessionParameters.SessionId +
												"'");
				}
			}

			_connectionHandler.RegisterAcceptorSession((SessionParameters)sessionParameters.Clone());
		}

		private bool ValidateAcceptorSessionPort(SessionParameters sessionParameters)
		{
			return _servers.Keys.Any(i => i == sessionParameters.Port);
		}

		/// <summary>
		/// Remove registered acceptor session.
		/// Note: To remove registered acceptor session it's enough to pass SessionParameters with right SenderComId and
		/// TargetCompId for a moment.
		/// </summary>
		/// <param name="sessionParameters"> </param>
		public virtual void UnregisterAcceptorSession(SessionParameters sessionParameters)
		{
			_connectionHandler.UnregisterAcceptorSession(sessionParameters);
		}

		public virtual IList<SessionParameters> GetRegisterAcceptorSession()
		{
			return _connectionHandler.GetRegisterAcceptorSession();
		}

		/// <summary>
		/// Path to config file
		/// </summary>
		/// <value> path to config file </value>
		public virtual string ConfigPath { get; set; }

		protected virtual bool IsStarted { get; private set; }

		public virtual void AddServerStatusListener(IFixServerStatusListener statusListener)
		{
			_statusListeners.Add(statusListener);
		}

		public virtual void RemoveServerStatusListener(IFixServerStatusListener statusListener)
		{
			_statusListeners.Remove(statusListener);
		}

		private void NotifyStarted()
		{
			IsStarted = true;
			foreach (var statusListener in _statusListeners)
			{
				try
				{
					statusListener.ServerStarted();
				}
				catch (Exception e)
				{
					Log.Debug(e.Message, e);
				}
			}
		}

		private void NotifyStopped()
		{
			IsStarted = false;
			foreach (var statusListener in _statusListeners)
			{
				try
				{
					statusListener.ServerStopped();
				}
				catch (Exception e)
				{
					Log.Debug(e.Message, e);
				}
			}
		}
	}
}
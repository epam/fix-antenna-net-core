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
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;

namespace Epam.FixAntenna.NetCore.FixEngine.Transport.Server.Tcp
{
	/// <summary>
	/// TCP server implementation.
	/// </summary>
	internal class TcpServer : IServer
	{
		private static ILog _log = LogFactory.GetLog(typeof(TcpServer));
		private IConnectionListener _listener;
		private TcpListener _serverSocket;
		private ConfigurationAdapter _confAdapter;

		private object _lock = new object();

		private static int ThreadNum;

		/// <summary>
		/// Listening port of the server.
		/// </summary>
		public int Port { get; }

		/// <summary>
		/// Represents specific NIC
		/// </summary>
		public string ConnectAddress { get; }

		/// <summary>
		/// Creates server.
		/// </summary>
		/// <param name="port">server port</param>
		public TcpServer(int port)
		{
			_confAdapter = new ConfigurationAdapter(Config.GlobalConfiguration);

			Port = port;
		}

		/// <summary>
		/// Creates server.
		/// </summary>
		/// <param name="host"></param>
		/// <param name="port"></param>
		/// <param name="configAdapter"></param>
		public TcpServer(string host, int port, ConfigurationAdapter configAdapter)
		{
			_confAdapter = configAdapter;

			ConnectAddress = host;
			Port = port;
		}

		/// <inheritdoc />
		public virtual void SetIncomingConnectionListener(IConnectionListener value)
		{
			_listener = value;
		}

		/// <inheritdoc />
		public virtual void Start()
		{
			if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
				Thread.CurrentThread.Name = "TcpServer";

			try
			{
				_serverSocket = CreateServerSocket(ConnectAddress, Port);
				_serverSocket.Start();
				_serverSocket.BeginAcceptSocket(AcceptConnection, this);
			}
			catch (SocketException ex)
			{
				throw new IOException(ex.Message, ex);
			}
		}

		/// <inheritdoc />
		public virtual void Stop()
		{
			try
			{
				if (_serverSocket != null)
				{
					lock(_lock)
					{
						_serverSocket.Stop();
						_serverSocket = null;
					}
				}
			}
			catch (SocketException ex)
			{
				throw new IOException(ex.Message, ex);
			}
		}

		private void AcceptConnection(IAsyncResult ar)
		{
			TcpAcceptorTransport acceptorTransport;

			if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
				Thread.CurrentThread.Name = $"TcpServer.Thread-{++ThreadNum}";

			lock (_lock)
			{

				if (_serverSocket == null)
				{
					_log.Info("Server socket has died, exiting.");
					return;
				}

				// BeginAcceptSocket blocks a thread on Linux, hence the usage of tasks.
				// Starting new listening thread.
				Task.Factory.StartNew(() =>
				{
					try
					{
						_serverSocket.BeginAcceptSocket(AcceptConnection, this);
					}
					catch (ObjectDisposedException ode)
					{
						_log.Trace(ode.Message, ode);
					}
					catch (SocketException ex)
					{
						if (_log.IsDebugEnabled)
						{
							_log.Warn(ex.Message, ex);
						}
						else
						{
							_log.Warn(ex.Message);
						}
					}
				}, TaskCreationOptions.PreferFairness | TaskCreationOptions.LongRunning).ConfigureAwait(false);

				Socket incomingConnectionSocket = null;
				try
				{
					incomingConnectionSocket = _serverSocket.EndAcceptSocket(ar);

					if (_log.IsInfoEnabled)
					{
						_log.Info(
							$"Connection accepted from remote address: {incomingConnectionSocket.RemoteEndPoint.AsString()} on local address: {incomingConnectionSocket.LocalEndPoint.AsString()}");
					}

					acceptorTransport = new TcpAcceptorTransport(incomingConnectionSocket, _confAdapter);
					acceptorTransport.Open();
				}
				catch (ObjectDisposedException)
				{
					incomingConnectionSocket.ShutdownAndClose();
					return;
				}
				catch (Exception ex)
				{
					if (_log.IsDebugEnabled)
					{
						_log.Warn(ex.Message, ex);
					}
					else
					{
						_log.Warn(ex.Message);
					}

					incomingConnectionSocket.ShutdownAndClose();

					return;
				}
			}

			var isOpen = acceptorTransport.IsOpen;
			if (isOpen)
			{
				_listener.OnConnect(acceptorTransport);
			}
		}
	
		[DllImport("libc", SetLastError = true)]
		private static extern int setsockopt(int socket, int level, int optname, ref int optval, int optlen);

		private static void ConfigureSocketOptions(Socket socket)
		{
			const int solSocket = 1;
			const int soReuseaddr = 2;
			const int soReuseport = 15;
			var optval = 1;

			var endpoint = socket.LocalEndPoint?.ToString() ?? "unknown";

			// Set SO_REUSEADDR
			var result = setsockopt(socket.Handle.ToInt32(), solSocket, soReuseaddr, ref optval, sizeof(int));

			if (result != 0)
			{
				var errno = Marshal.GetLastWin32Error(); 
				_log.Error($"Failed to set SO_REUSEADDR on {endpoint}: errno={errno}.");
				throw new IOException($"SO_REUSEADDR configuration failed with errno {errno}.");
			}

			_log.Info($"SO_REUSEADDR enabled successfully on {endpoint}.");

			// Set SO_REUSEPORT
			result = setsockopt(socket.Handle.ToInt32(), solSocket, soReuseport, ref optval, sizeof(int));
    
			if (result == 0)
			{
				_log.Info($"SO_REUSEPORT enabled successfully on {endpoint}.");
			}
			else
			{
				var errno = Marshal.GetLastWin32Error();
				_log.Error($"Failed to set SO_REUSEPORT on {endpoint}: errno={errno}.");
				throw new IOException($"SO_REUSEPORT configuration failed with errno {errno}.");
			}
		}

		private TcpListener CreateServerSocket(string address, int port)
		{
			try
			{
				TcpListener listener;

				if (string.IsNullOrEmpty(address))
				{
					listener = TcpListener.Create(port);
				}
				else
				{
					var ip = IPAddress.Parse(address);
					listener = new TcpListener(ip, port);
				}

				if (_confAdapter.EnableSocketReusePort)
					ConfigureSocketOptions(listener.Server);

				return listener;
			}
			catch (SocketException ex)
			{
				throw new IOException(ex.Message, ex);
			}
		}

		public override string ToString()
		{
			return Convert.ToString(Port, CultureInfo.InvariantCulture);
		}
	}
}
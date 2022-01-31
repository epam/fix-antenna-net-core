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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Epam.FixAntenna.NetCore.FixEngine.Transport;
using NLog;

namespace Epam.FixAntenna.NetCore.FixEngine.Transport.Client.Tcp
{
	internal class DummyServer
	{
		private static readonly ILogger Log = LogManager.GetLogger(typeof(DummyServer).FullName);

		private readonly bool _handleConnection;
		private readonly int _port;
		private readonly string _host;
		private TcpListener _serverSocket;
		private Socket _socket;
		protected internal Thread ConnectionThread;

		internal volatile NetworkStream NetStream;

		public DummyServer(int port) : this(port, false)
		{
		}

		public DummyServer(int port, bool handleConnection)
		{
			_port = port;
			_handleConnection = handleConnection;
		}

		public DummyServer(string host, int port, bool handleConnection = false) : this(port, handleConnection)
		{
			_host = host;
		}

		public virtual void Start()
		{
			if (string.IsNullOrEmpty(_host))
			{
				_serverSocket = TcpListener.Create(_port);
			}
			else
			{
				_serverSocket = new TcpListener(IPAddress.Parse(_host), _port);
			}
			_serverSocket.Start();
			ConnectionThread = new Thread(() =>
			{
				try
				{
					while (ConnectionThread.IsAlive)
					{
						var accept = _serverSocket.AcceptSocket();
						if (_handleConnection)
						{
							HandleConnection(accept);
						}
					}
				}
				catch (Exception e)
				{
					Log.Debug(e, e.Message);
				}
			});
			ConnectionThread.Name = "DummyServer";
			ConnectionThread.Start();
		}

		public virtual void Stop()
		{
			if (ConnectionThread != null && ConnectionThread.IsAlive)
			{
				ConnectionThread.Interrupt();
			}

			if (_serverSocket != null)
			{
				_serverSocket.Server.ShutdownAndClose();
				_serverSocket.Stop();
				_serverSocket = null;
			}

			NetStream?.Close();
		}

		protected virtual void HandleConnection(Socket socket)
		{
			NetStream = new NetworkStream(socket);
			_socket = socket;
		}

		public virtual void Send(byte[] bytes)
		{
#if NET48
			NetStream.Write(bytes, 0, bytes.Length);
#else
			NetStream.Write(bytes);
#endif
		}

		public virtual bool IsOpen()
		{
			return _socket != null && _socket.Connected;
		}
	}
}
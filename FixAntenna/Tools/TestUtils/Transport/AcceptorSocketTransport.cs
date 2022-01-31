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
using System.Net.Sockets;
using System.Threading;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.FixEngine.Transport;

namespace Epam.FixAntenna.TestUtils.Transport
{
	internal class AcceptorSocketTransport : BasicSocketTransport
	{
		private const int ConnectionAttempts = 30;
		private readonly ILog _log;
		private bool _connected;
		private int _port;

		private TcpListener _serverSocket;
		private TimeSpan _timeout;

		public AcceptorSocketTransport()
		{
			_log = LogFactory.GetLog(GetType());
		}

		public override void Init(int port, TimeSpan timeout)
		{
			_port = port;
			_timeout = timeout;
		}

		public override void Open()
		{
			_serverSocket = TcpListener.Create(_port);
			_serverSocket.Start();
			new Thread(() =>
			{
				try
				{
					Socket = _serverSocket.AcceptSocketWithTimeout(_timeout);
					_log.Debug("Connection accepted");
				}
				catch (IOException e)
				{
					_log.Error(e, e);
					try
					{
						_serverSocket.Stop();
					}
					catch (IOException)
					{
						_log.Error(e, e);
					}
				}
			}).Start();
		}

		public override void SendMessage(string message)
		{
			ConnectOrDie();
			base.SendMessage(message);
		}

		public override string ReceiveMessage()
		{
			ConnectOrDie();
			return base.ReceiveMessage();
		}

		private void ConnectOrDie()
		{
			if (_connected)
			{
				return;
			}

			var attempts = 0;
			while (Socket == null && attempts++ < ConnectionAttempts)
			{
				try
				{
					Thread.Sleep(TimeSpan.FromMilliseconds(1000));
				}
				catch (ThreadInterruptedException)
				{
					// intentionally blank
				}
			}

			if (Socket == null)
			{
				throw new IOException("Nobody connected");
			}

			Input = new FixMessageReader(new NetworkStream(Socket), 1024 * 1024);
			Output = new NetworkStream(Socket);
			_connected = true;
		}

		public override void Close()
		{
			base.Close();

			try
			{
				_serverSocket?.Server.ShutdownAndClose();
				_serverSocket?.Stop();
			}
			catch (IOException e)
			{
				_log.Error(string.Empty, e);
			}

			_connected = false;
			Socket = null;
			Input = null;
			Output = null;
			_serverSocket = null;
		}
	}
}
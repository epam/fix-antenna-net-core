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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Common.Utils;

namespace Epam.FixAntenna.Tester.Transport
{
	public class AcceptorSocketTransport : BasicSocketTransport
	{
		private bool InstanceFieldsInitialized = false;

		public AcceptorSocketTransport()
		{
			if (!InstanceFieldsInitialized)
			{
				InitializeInstanceFields();
				InstanceFieldsInitialized = true;
			}
		}

		private void InitializeInstanceFields()
		{
			_log = LogFactory.GetLog(this.GetType());
		}

		private ILog _log;
		private const int CONNECTION_ATTEMPTS = 30;

		private Socket _serverSocket;
		private int _port;
		private int _timeout;
		private bool _connected = false;

		public override void Init(IDictionary<string, string> parameters)
		{
			try
			{
				_port = int.Parse(parameters[TesterParams.PORT_PARAM]);
				string t = parameters[TesterParams.TIMEOUT_PARAM];
				if (null != t)
				{
					_timeout = int.Parse(t);
				}
				else
				{
					_timeout = 2 * 60 * 1000;
				}
			}
			catch (System.FormatException e)
			{
				_log.Error("Port number error", e);
				throw new System.ArgumentException(e.Message);
			}
		}

		public override void Open()
		{
			_serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			_serverSocket.Bind(new IPEndPoint(IPAddress.Loopback, _port));
			_serverSocket.Listen(3);

			(new ConnectThread(this)).Start();
		}

		public override void SendMessage(string message)
		{
			ConnectOrDie();
			base.SendMessage(message);
		}

		public override void SendMessage(string message, IDictionary<string, string> @params)
		{
			SendMessage(message);
		}

		public override string ReceiveMessage()
		{
			ConnectOrDie();
			return base.ReceiveMessage();
		}

		public override string ReceiveMessage(IDictionary<string, string> @params)
		{
			return ReceiveMessage();
		}

		private void ConnectOrDie()
		{
			if (_connected)
			{
				return;
			}
			int attempts = 0;
			while (Socket == null && attempts++ < CONNECTION_ATTEMPTS)
			{
				Thread.Sleep(100);
			}
			if (Socket == null)
			{
				throw new IOException("Nobody connected");
			}

			var stream = new NetworkStream(Socket);
			Input = new FIXMessageReader(stream, 1024 * 1024);
			Out = stream;
			_connected = true;
		}

		public virtual void Restart()
		{
			// close socket, but not server
			_connected = false;
			Socket = null;
			Input = null;
			Out = null;

			(new ConnectThread(this)).Start();
		}
		private void Close()
		{
			_connected = false;
			_serverSocket?.Close();
			_serverSocket = null;
			if (CheckingUtils.TryCheckWithinTimeout(() => SocketUtils.IsLocalPortAvailableForBinding(_port), TimeSpan.FromSeconds(1)) != true)
			{
				_log.Error($"Local port {_port} is not available");
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				Close();
			}
			base.Dispose(disposing);
		}

		public class ConnectThread
		{
			private readonly AcceptorSocketTransport _outerInstance;

			public ConnectThread(AcceptorSocketTransport outerInstance)
			{
				this._outerInstance = outerInstance;
			}

			public void Start()
			{
				var _ = System.Threading.Tasks.Task.Factory.StartNew(this.Run, TaskCreationOptions.LongRunning);
			}

			public void Run()
			{
				try
				{
					var result = _outerInstance._serverSocket.BeginAccept(null, null);

					if (result.AsyncWaitHandle.WaitOne(_outerInstance._timeout))
					{
						_outerInstance.Socket = _outerInstance._serverSocket.EndAccept(result);
						_outerInstance._log.Debug("Connection accepted");
					}
					else
					{
						throw new SocketException((int)SocketError.TimedOut);
					}
				}
				catch (SocketException e)
				{
					_outerInstance._log.Error(e, e);
					_outerInstance._serverSocket.Close();
				}
			}
		}
	}
}
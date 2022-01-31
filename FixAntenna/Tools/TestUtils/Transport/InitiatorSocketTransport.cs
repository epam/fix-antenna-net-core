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
using System.Net.Sockets;

namespace Epam.FixAntenna.TestUtils.Transport
{
	internal class InitiatorSocketTransport : BasicSocketTransport
	{
		private readonly string _host;
		private int _port;
		private TimeSpan _timeout;

		public InitiatorSocketTransport(string host)
		{
			_host = host;
		}

		public override void Init(int port, TimeSpan timeout)
		{
			_port = port;
			_timeout = timeout;
		}

		public override void Open()
		{
			Socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
			{
				ReceiveTimeout = (int)_timeout.TotalMilliseconds
			};

			Socket.Connect(_host, _port);
			Input = new FixMessageReader(new NetworkStream(Socket), 1024 * 1024);
			Output = new NetworkStream(Socket);
		}
	}
}
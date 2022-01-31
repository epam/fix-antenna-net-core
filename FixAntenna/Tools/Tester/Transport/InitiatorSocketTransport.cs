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

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Epam.FixAntenna.NetCore.Common.Logging;

namespace Epam.FixAntenna.Tester.Transport
{
	public class InitiatorSocketTransport : BasicSocketTransport
	{
		private static readonly ILog _log = LogFactory.GetLog(typeof(InitiatorSocketTransport));

		private IPAddress _address;
		private int _port;

		public InitiatorSocketTransport()
		{
		}

		public override void Init(IDictionary<string, string> parameters)
		{
			try
			{
				string host = (string) parameters[TesterParams.HOST_PARAM];
				_log.Info(TesterParams.HOST_PARAM + ":" + host);
				if (!IPAddress.TryParse(host, out _address))
				{
					// see also TransportUtils.ToEndPoint
					_address = Dns.GetHostAddresses(host).Where(a => a.AddressFamily == AddressFamily.InterNetwork).First();
				}
				
			}
			catch (SocketException e)
			{
				_log.Warn(e, e);
				throw new System.ArgumentException(e.ToString());
			}
			string portString = (string) parameters[TesterParams.PORT_PARAM];
			_log.Info(TesterParams.PORT_PARAM + ":" + portString);
			_port = int.Parse(portString);
		}

		public override void Open()
		{
			Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			Socket.Connect(new IPEndPoint(_address, _port));
			var stream = new NetworkStream(Socket);
			Input = new FIXMessageReader(stream, 1024 * 1024);
			Out = stream;
		}
	}

}
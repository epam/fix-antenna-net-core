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

using System.IO;
using System.Net.Sockets;
using Epam.FixAntenna.NetCore.FixEngine.Transport.Client.Tcp;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Transport.Client.Tcp
{
	[TestFixture]
	internal class TcpTransportV6Test
	{
		private const int Port = 1234;
		private TcpTransport _transport;
		private DummyServer _server;

		public void SetUp(string ipAddress)
		{
			_server = new DummyServer(ipAddress, Port, true);
		}

		[TearDown]
		public void TearDown()
		{
			if (_transport != null && _transport.IsOpen)
			{
				_transport.Close();
			}

			_server.Stop();
		}

		[Test]
		public void DefaultServerHostV4Test()
		{
			SetUp(null);
			_transport = new TcpTransport("127.0.0.1", Port);

			_server.Start();

			_transport.Open();
			ClassicAssert.IsTrue(_transport.IsOpen);
		}

		[Test]
		public void DefaultServerHostV6Test()
		{
			if (!Socket.OSSupportsIPv6) return;

			SetUp(null);
			_transport = new TcpTransport("::1", Port);

			_server.Start();

			_transport.Open();
			ClassicAssert.IsTrue(_transport.IsOpen);
		}

		[Test]
		public void IpV4ConnectTest()
		{
			SetUp("127.0.0.1");
			_transport = new TcpTransport("127.0.0.1", Port);

			_server.Start();

			_transport.Open();
			ClassicAssert.IsTrue(_transport.IsOpen);
		}

		[Test]
		public void IpV6ConnectTest()
		{
			if (!Socket.OSSupportsIPv6) return;

			SetUp("::1");
			_transport = new TcpTransport("::1", Port);

			_server.Start();

			_transport.Open();
			ClassicAssert.IsTrue(_transport.IsOpen);
		}

		[Test]
		public void IpV6ToV4ConnectTest()
		{
			if (!Socket.OSSupportsIPv6) return;

			SetUp("127.0.0.1");
			_transport = new TcpTransport("::1", Port);

			_server.Start();

			ClassicAssert.Throws<IOException>(() => _transport.Open());
		}

		[Test]
		public void IpV4ToV6ConnectTest()
		{
			if (!Socket.OSSupportsIPv6) return;

			SetUp("::1");
			_transport = new TcpTransport("127.0.0.1", Port);

			_server.Start();

			ClassicAssert.Throws<IOException>(() => _transport.Open());
		}
	}
}
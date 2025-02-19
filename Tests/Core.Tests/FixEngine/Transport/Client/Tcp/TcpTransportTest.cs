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

using Epam.FixAntenna.NetCore.FixEngine.Transport;
using Epam.FixAntenna.NetCore.FixEngine.Transport.Client.Tcp;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Transport.Client.Tcp
{
	[TestFixture]
	internal class TcpTransportTest
	{
		[SetUp]
		public virtual void SetUp()
		{
			_server = new DummyServer(RemotePort, true);
		}

		[TearDown]
		public virtual void TearDown()
		{
			if (_transport != null && _transport.IsOpen)
			{
				_transport.Close();
			}

			_server.Stop();
		}

		private const int RemotePort = 1234;
		private TcpTransport _transport;
		private DummyServer _server;

		[Test]
		public virtual void TestGetHostAndPort()
		{
			_transport = new TcpTransport("127.0.0.1", RemotePort);
			ClassicAssert.AreEqual("127.0.0.1", _transport.RemoteHost);
			ClassicAssert.IsNull(_transport.RemoteEndPoint?.Address);
			ClassicAssert.AreEqual(1234, _transport.RemotePort);
			ClassicAssert.AreEqual(null, _transport.LocalEndPoint?.Port);

			_server.Start();

			_transport.Open();
			ClassicAssert.IsTrue(_transport.IsOpen);
			ClassicAssert.AreEqual("127.0.0.1", _transport.RemoteHost);
			ClassicAssert.AreEqual("127.0.0.1", _transport.RemoteEndPoint.Address.AsString());
			ClassicAssert.AreEqual(1234, _transport.RemotePort);
			ClassicAssert.AreEqual(1234, _transport.RemoteEndPoint?.Port);
			ClassicAssert.IsTrue(_transport.LocalEndPoint?.Port != null);

			_transport.Close();
			ClassicAssert.AreEqual("127.0.0.1", _transport.RemoteHost);
			ClassicAssert.IsNull(_transport.RemoteEndPoint?.Address);
			ClassicAssert.AreEqual(1234, _transport.RemotePort);
			ClassicAssert.IsTrue(_transport.LocalEndPoint?.Port == null);
		}
	}
}
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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.FixEngine.Transport;
using Epam.FixAntenna.NetCore.FixEngine.Transport.Server;
using Epam.FixAntenna.NetCore.FixEngine.Transport.Server.Tcp;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Transport.Server.Tcp
{
	[TestFixture]
	internal class TcpServerTest
	{
		[TearDown]
		public virtual void Destroy()
		{
			_server.Stop();
		}

		private static readonly IPAddress TestAddress = IPAddress.Parse("127.0.0.1");
		private const int TestPort = 3000;
		private TcpServer _server;

		private void Init()
		{
			_server = new TcpServer(TestPort);
			_server.SetIncomingConnectionListener(new TestConnectionListener());
			_server.Start();
		}

		private void EstablishThousandConnections()
		{
			var openSockets = new List<Socket>();
			try
			{
				// 150 since we use system threadpool. it is configured to have less than 1000 threads.
				for (var i = 0; i < 150; i++)
				{
#pragma warning disable CA2000 // Dispose objects before losing scope: Dispose is called in ShutdownAndClose() method
					var client = new Socket(SocketType.Stream, ProtocolType.Tcp);
#pragma warning restore CA2000 // Dispose objects before losing scope
					client.Connect(TestAddress, TestPort);
					openSockets.Add(client);
					Assert.IsTrue(client.Connected);
				}
			}
			finally
			{
				foreach (var openSocket in openSockets)
				{
					try
					{
						openSocket.ShutdownAndClose();
					}
					catch (Exception)
					{
					}
				}
			}
		}

		private class TestConnectionListener : IConnectionListener
		{
			public void OnConnect(ITransport transport)
			{
				try
				{
					Thread.Sleep(10000);
				}
				catch (ThreadInterruptedException)
				{
				}
			}
		}

		[Test]
		public void ItShouldAcceptInnerConnection()
		{
			Init();
			using (var client = new Socket(SocketType.Stream, ProtocolType.Tcp))
			{
				client.Connect(TestAddress, TestPort);
				Assert.IsTrue(client.Connected);
			}
		}

		[Test]
		public void ItShouldAcceptManyInnerConnectionsSimultaneously()
		{
			Init();
			EstablishThousandConnections();
		}

		[Test]
		public void ItShouldWorkCorrectlyAfterRestarting()
		{
			Init();
			var client = new Socket(SocketType.Stream, ProtocolType.Tcp);
			client.Connect(TestAddress, TestPort);
			Assert.IsTrue(client.Connected);

			//restart server
			_server.Stop();
			_server.Start();
			client.Close();
			CheckingUtils.CheckWithinTimeout(() => !client.Connected, TimeSpan.FromMilliseconds(5000));

#pragma warning disable CA2000 // Dispose objects before losing scope: Dispose is called in ShutdownAndClose() method
			var newClient = new Socket(SocketType.Stream, ProtocolType.Tcp);
#pragma warning restore CA2000 // Dispose objects before losing scope
			try
			{
				newClient.Connect(TestAddress, TestPort);
				Assert.IsTrue(newClient.Connected);
			}
			finally
			{
				newClient.ShutdownAndClose();
			}
		}
	}
}
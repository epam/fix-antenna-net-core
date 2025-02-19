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
using Epam.FixAntenna.NetCore.Common.Logging;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Tester.SelfTest
{
	[TestFixture]
	public class EngineUpTest
	{
		[Test]
		public virtual void TestEngineUP()
		{
			ILog log = LogFactory.GetLog(this.GetType());

	//        String home = "./TestServer";
	//        String config = "./TestServer/config/fixaj.properties";
	//        log.info("Starting test server");
	//        log.info("Init FIXAJ using home: " + home + " config: " + config);
	//        TemplateProperties fixajConfig = new TemplateProperties();
	//        fixajConfig.load(new FileInputStream(config));
	//        fixajConfig.setTemplatePair(FIXEngineB2B.FIXAJ_HOME_NAME, home);
	//        FIXEngine fixaj = new FIXEngineB2B(null, fixajConfig, false, home);
	//        Transport t = new InitiatorSocketTransport();
	//        HashMap<String, String> map = new HashMap<String, String>();
	//        map.put("port", "3000");
	//        map.put("host", "localhost");
	//        t.init(map);
	//        t.open();
	//        t.sendMessage("8=1sdfasdfdsfasdfasd");
	//        //t.receiveMessage();
	//        fixaj.closeEngine();
	//        fixaj = new FIXEngineB2B(null, fixajConfig, false, home);
	//        fixaj.closeEngine();
		}

		[Test]
		public virtual void TestSocket()
		{
			Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			server.Bind(new IPEndPoint(IPAddress.Loopback, 3000));
			server.Listen(3);


			Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			client.Connect(new IPEndPoint(IPAddress.Loopback, 3000));

			Socket connection = server.Accept();

			var clientStream = new NetworkStream(client);
			var connectionStream = new NetworkStream(connection);

			clientStream.Write(new byte[] { 1 }, 0, 1);

			var buf = new byte[1];
			int count = connectionStream.Read(buf, 0, 1);

			ClassicAssert.AreEqual(1, count);

			clientStream.Close();
			connectionStream.Close();
			server.Close();
			client.Close();

			try
			{
				Socket server2 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				server2.Bind(new IPEndPoint(IPAddress.Loopback, 3000));
				server2.Listen(3);


				Socket client2 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				client2.Connect(new IPEndPoint(IPAddress.Loopback, 3000));

				Socket connection2 = server2.Accept();

				server2.Close();
				client2.Close();
			}
			catch (SocketException e)
			{
				Console.WriteLine(e.ToString());
				Console.Write(e.StackTrace);
				ClassicAssert.Fail(e.Message);
			}
		}
	}
}
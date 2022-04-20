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

using Epam.FixAntenna.Constants.Fix42;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.Message;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Epam.FixAntenna.Samples.Latency.Server
{
	public sealed class RoundTripServer : IFixServerListener
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(RoundTripServer));
		private readonly List<IFixSession> _activeSessions = new List<IFixSession>();
		private static FixServer Server;

		public static void Main(string[] args)
		{
			if (args.Length != 2)
			{
				PrintUsageAndExit();
			}

			var host = args[0];
			var port = -1;
			try
			{
				port = int.Parse(args[1]);
			}
			catch (FormatException)
			{
				Console.WriteLine("Incorrect port");
				PrintUsageAndExit();
			}

			Init(host, port);

			Console.ReadLine();
		}

		private static void Init(string host, int port)
		{
			Server = new FixServer();
			Server.Nic = host;
			Server.SetPort(port);
			Server.SetListener(new RoundTripServer());
			Server.Start();
			Log.Info("Started");
		}

		public void NewFixSession(IFixSession session)
		{
			Log.Info("New session");
			try
			{
				// this is important! If user lost all references to his session - it will be disposed and gc eventually!
				_activeSessions.Add(session);

				session.SetFixSessionListener(new MySessionListener(this, session));
				session.Connect();
			}
			catch (Exception e)
			{
				Log.Warn(e, e);
			}
		}

		private sealed class MySessionListener : IFixSessionListener
		{
			private readonly RoundTripServer _server;

			private readonly IFixSession _session;
			private readonly FixMessage _executionReport;
			private int _execId;
			private int _orderId;
			private readonly byte[] _workMsgType = Encoding.UTF8.GetBytes("D");

			public MySessionListener(RoundTripServer server, IFixSession session)
			{
				_server = server;
				_session = session;
				var pmu = new PreparedMessageUtil(session.Parameters);
				_executionReport = pmu.PrepareMessageFromString(Encoding.UTF8.GetBytes("8=FIX.4.2\u00019=800\u000135=8\u000149=TargetCompId\u0001" + "56=SenderCompId\u000134=10000\u000152=20000809-20:20:50\u000137=00000000\u000111=90001008\u000117=00000000\u0001" + "20=0\u000139=0\u00011=10030003\u0001150=0\u000155=TESTA\u000154=1\u000138=4000\u000140=2\u000144=30\u0001" + "32=0\u000131=0\u0001151=4000\u000114=0\u00016=0\u000160=20000809-18:20:32\u0001109=111\u000110=173\u0001"), "8", new MessageStructure());
			}

			public void OnSessionStateChange(SessionState sessionState)
			{
				if (SessionState.IsDisconnected(sessionState))
				{
					_server._activeSessions.Remove(_session);
					if (_server._activeSessions.Count == 0)
					{
						try
						{
							Server.Stop();
							Log.Info("Stopped");
						}
						catch (IOException e)
						{
							Console.WriteLine(e.ToString());
							Console.Write(e.StackTrace);
						}
						Environment.Exit(0);
					}
				}
			}

			public void OnNewMessage(FixMessage message)
			{
				if (_workMsgType.SequenceEqual(message.MsgType))
				{
					_executionReport.Set(Tags.ClientID, message.GetTagValueAsString(Tags.ClientID));
					_executionReport.Set(Tags.Price, message.GetTagValueAsString(Tags.Price));
					_executionReport.Set(Tags.OrderQty, message.GetTagValueAsString(Tags.OrderQty));
					_executionReport.Set(Tags.TransactTime, message.GetTagValueAsString(Tags.TransactTime));
					_executionReport.Set(Tags.ExecID, ++_execId);
					_executionReport.Set(Tags.OrderID, ++_orderId);

					_session.SendMessage(_executionReport);
				}
				else
				{
					_session.SendMessage(message);
				}
			}
		}

		private static void PrintUsageAndExit()
		{
			Console.WriteLine("Server host port");
			Environment.Exit(1);
		}
	}
}
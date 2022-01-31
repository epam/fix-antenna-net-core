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
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.Example
{
	public class SimpleServer : IFixServerListener
	{
		private static Dictionary<string, string[]> _allowedSessionsList = new Dictionary<string, string[]>();
		private static Dictionary<string, string> _arguments;
		private static int[] _ports;
		private static string _nic;
		private static int _timeout;
		private static bool _checkConnection;

		static SimpleServer()
		{
			_allowedSessionsList["senderId"] = new []{ "localhost", "127.0.0.1" };
			_allowedSessionsList["anotherSender"] = new []{ "localhost", "127.0.0.1" };
		}

		public static int Main(string[] args)
		{
			_arguments = ParseArguments(args);

			try
			{
				// ports
				var serverPorts = _arguments.ContainsKey("p") ? _arguments["p"] : "3000";
				SetPorts(serverPorts);
				Console.WriteLine($"Server port(s): {serverPorts}");

				// timeout
				_timeout = _arguments.ContainsKey("t") ? int.Parse(_arguments["t"]) : 0;
				var timeout = _timeout > 0 ? $"{_timeout} sec." : "not set";
				Console.WriteLine($"Timeout: {timeout}");

				// NIC
				_nic = _arguments.ContainsKey("n") ? _arguments["n"] : null;
				if (!string.IsNullOrEmpty(_nic))
				{
					Console.WriteLine($"NIC: {_nic}");
				}

				// Check address and username
				_checkConnection = _arguments.ContainsKey("check");
				if (_checkConnection)
				{
					Console.WriteLine("Connection address and credentials will be validated.");
				}
			}
			catch (Exception)
			{
				Console.WriteLine("Please pass port(s), timeout, NIC and connection check flag as parameters:");
				Console.WriteLine("\tSimpleServer -p:3000,3001,3002 -t:10 -n:localhost -check");
				return 1;
			}

			// create server that will listen for TCP/IP connections on specific ports
			var server = new FixServer { Ports = _ports, Nic = _nic };
			if (_checkConnection)
			{
				server.SetConnectionValidator(new ConnectionValidator());
			}

			// setting listener for new connections
			server.SetListener(new SimpleServer());

			// this will start new thread and listen for incoming connections
			var isStarted = server.Start();
			if (isStarted)
			{
				var ports = string.Join(",", _ports);
				Console.WriteLine($"Server started on ports {ports}");
			}
			if (_nic != null && isStarted)
			{
				Console.WriteLine($" on nic {_nic}");
			}

			if (_timeout != 0)
			{
				// stop server on timeout if needed
				Console.WriteLine($"Timeout set to {_timeout} sec. Server will be stopped after that.");
				Thread.Sleep(_timeout * 1000);
			}
			else
			{
				Console.WriteLine(" ... Press ENTER to exit the program.");
				// preventing application from exiting
				Console.Read();
			}

			// this will stop the thread that listens for new connections
			Console.WriteLine("Stopping server...");
			server.Stop();
			return 0; // success code
		}

		private static void SetPorts(string arg)
		{
			var portValues = arg.Split(',');
			//read ports
			_ports = new int[portValues.Length];
			for (var i = 0; i < portValues.Length; i++)
			{
				_ports[i] = int.Parse(portValues[i]);
			}
		}

		/// <summary>
		/// This method is called for every new connection.
		/// </summary>
		/// <param name="session"> the session </param>
		public void NewFixSession(IFixSession session)
		{
			try
			{
				// setting listener for incoming messages
				var sessionParameters = session.Parameters;
				var incomingSessionIPAddress = sessionParameters.Host;
				var senderCompID = sessionParameters.TargetCompId; // our target is incoming session sender!!!

				if (_checkConnection && !IsValidAddress(incomingSessionIPAddress, senderCompID))
				{
					Console.WriteLine("Server accepts connections from localhost only!");
					throw new ArgumentException("Illegal connection attempt (localhost & SenderCompID=senderId allowed)"); // any kind of runtime exception!
				}

				if (!_checkConnection || IsValidUser(sessionParameters))
				{
					session.SetFixSessionListener(new MyFixSessionListener(session));
					// accepting connection
					session.Connect();
					Console.WriteLine("New connection accepted");
				}
				else
				{
					Console.WriteLine("Wrong user and password for session. (Please try user/pass)");
					session.Reject("Invalid user or password");
					session.Dispose();
				}
			}
			catch (IOException e)
			{
				Console.WriteLine($"IOException occurred: {e.Message}.");
			}
		}

		private bool IsValidUser(SessionParameters parameters)
		{
			//get username(553) and password(554) tags from incoming Logon
			var username = parameters.IncomingUserName;
			var password = parameters.IncomingPassword;
			//for test reason accept only user guest if user name present
			return username == null || username.Equals("user") && password.Equals("pass");
		}

		private bool IsValidAddress(string incomingSessionIPAddress, string senderCompID)
		{
			var addr = incomingSessionIPAddress;
			// remove IPv6 part
			if (IPAddress.TryParse(addr, out var ipAddr) && ipAddr.IsIPv4MappedToIPv6)
			{
				addr = ipAddr.MapToIPv4().ToString();
			}

#if NET48
			_allowedSessionsList.TryGetValue(senderCompID, out var addressList);
#else
			var addressList = _allowedSessionsList.GetValueOrDefault(senderCompID);
#endif
			return addressList?.Any(address => addr.Equals(address, StringComparison.OrdinalIgnoreCase)) ?? false;
		}

		/// <summary>
		/// Listener for incoming messages and session state changes.
		/// </summary>
		private class MyFixSessionListener : IFixSessionListener
		{
			private readonly IFixSession _session;

			public MyFixSessionListener(IFixSession session)
			{
				_session = session;
			}

			/// <summary>
			/// This method will be called every time session state is changed.
			/// </summary>
			/// <param name="sessionState"> the new session state </param>
			public void OnSessionStateChange(SessionState sessionState)
			{
				Console.WriteLine($"Session state changed: {sessionState}");
				if (SessionState.IsDisconnected(sessionState))
				{
					_session.Dispose();
					Console.WriteLine("Your session was disposed.");
				}
			}

			/// <summary>
			/// Here you can process incoming messages.
			/// </summary>
			/// <param name="message"> the new message </param>
			public void OnNewMessage(FixMessage message)
			{
				var tagVal = message.GetTag(Tags.MsgType);
				if (MsgType.News.Equals(tagVal.Buffer, tagVal.Offset, tagVal.Length))
				{
					Console.WriteLine($"New News message accepted: {message}");
					// Gets repeating groups
					var newsGroup = message.Split(MsgType.News.Text);
					// Gets head line
					var sb = new StringBuilder(message.GetTagValueAsString(MsgType.News.Headline));
					foreach (var news in newsGroup)
					{
						sb.Append(news.GetTagValueAsString(MsgType.News.Text)).Append(" ");
					}
					Console.WriteLine($"We have a good news: {sb}");
				}
				else
				{
					Console.WriteLine($"New message accepted: {message}");
					//send it back
					_session.SendMessage(message);
				}
			}
		}

		private static Dictionary<string,string> ParseArguments(string[] args)
		{
			var dict = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
			foreach (var arg in args)
			{
				var parts = arg.Split(':');
				dict.Add(parts[0].Trim('-'), parts.Length > 1 ? parts[1].Trim() : null);
			}

			return dict;
		}

		private class ConnectionValidator : IConnectionValidator
		{
			public bool Allow(IPAddress address)
			{
				Console.WriteLine($"New connection attempt from {UnmapIp6(address)}");
				return IPAddress.IsLoopback(address);
			}
		}

		private static string UnmapIp6(IPAddress address)
		{
			if (address == null) return null;

			return address.IsIPv4MappedToIPv6 ? address.MapToIPv4().ToString() : address.ToString();
		}
	}
}
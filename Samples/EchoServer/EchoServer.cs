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
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.Example
{
	/// <summary>
	/// Main application class, implementing IFixServerListener to process incoming sessions connections
	/// </summary>
	public class EchoServer : IFixServerListener
	{
		private static readonly ILog Logger = LogFactory.GetLog(typeof(EchoServer));
		
		// If configuration contains 'echoMode = true' then all received application messages will be sent back
		private static bool _echoMode;

		public static void Main(string[] args)
		{
			// Configure path where fixengine.properties is located
			if (args.Length > 0)
			{
				Config.ConfigurationDirectory = args[0];
			}

			// loading configuration file
			var configuration = new Config(Config.DefaultEngineProperties);
			// loading 'echoMode' setting
			_echoMode = bool.Parse(configuration.GetProperty("echoMode", "true"));

			// creating IFixServerListener implementing instance
			var server = new FixServer(configuration)
			{
				ConfigPath = Config.DefaultEngineProperties
			};
			server.SetListener(new EchoServer());

			var isStarted = false;

			try
			{
				isStarted = server.Start();
			}
			catch (Exception e)
			{
				Logger.Error("Cannot start FixServer.", e);
			}
			
			if (isStarted)
			{
				Logger.Info("Server started");
			}

			Logger.Info(" ... Press ENTER to exit the program.");
			Console.Read();
			server.Stop();
		}

		/// <summary>
		/// This method is invoked every time when new <see cref="IFixSession"/> is created.
		/// </summary>
		/// <param name="session"> the new session </param>
		public void NewFixSession(IFixSession session)
		{
			try
			{
				// will check username/password if configured
				if (!ValidateUser(session.Parameters))
				{
					session.Disconnect("Username or password is incorrect.");
					session.Dispose();
					return;
				}

				// Attaching new FixSessionListener with reference to the session for disposing after reconnect.
				// This functionality (disposing) is not required part, just as sample.
				session.SetFixSessionListener(new MyFixSessionListener(session));
				session.Connect();
				Logger.Info("New connection accepted");
			}
			catch (IOException e)
			{
				Logger.Error(e.Message, e);
			}
		}

		/// <summary>
		/// Check if the connecting session has configured username/password and validate it if so.
		/// </summary>
		/// <param name="parameters">Session parameters</param>
		/// <returns>true, if validation succeeded, false otherwise.</returns>
		private bool ValidateUser(SessionParameters parameters)
		{
			//get username(553) and password(554) tags from incoming Logon
			var configuredUserName = parameters.UserName;
			var configuredPassword = parameters.Password;
			var userName = parameters.IncomingUserName;
			var password = parameters.IncomingPassword;

			var validationResult = true;
			// check username if configured
			if (configuredUserName != null && !configuredUserName.Equals(userName, StringComparison.InvariantCulture))
			{
				validationResult = false;
			}
			// check password if configured
			if (validationResult && configuredPassword != null && !configuredPassword.Equals(password, StringComparison.InvariantCulture))
			{
				validationResult = false;
			}

			return validationResult;
		}

		/// <summary>
		/// Listener to process incoming messages and state changes.
		/// </summary>
		private class MyFixSessionListener : IFixSessionListener
		{
			/// <summary>
			/// Keep reference to the session to be able Dispose it after Disconnect.
			/// </summary>
			private readonly IFixSession _session;

			public MyFixSessionListener(IFixSession session)
			{
				_session = session;
			}

			/// <summary>
			/// This method will be called every time session state is changed.
			/// </summary>
			/// <param name="sessionState"> the new session state </param>
			public virtual void OnSessionStateChange(SessionState sessionState)
			{
				Logger.Info("Session state changed: " + sessionState);
				// Disposing session object if session state is Disconnected
				if (SessionState.IsDisconnected(sessionState))
				{
					_session.Dispose();
					Logger.Info("The session was disposed.");
				}
			}

			/// <summary>
			/// Here you can process incoming messages.
			/// </summary>
			/// <param name="message"> the new message </param>
			public virtual void OnNewMessage(FixMessage message)
			{
				Logger.Info("New message received: " + message);
				if (_echoMode)
				{
					//send it back
					_session.SendMessage(message);
				}
			}
		}
	}
}
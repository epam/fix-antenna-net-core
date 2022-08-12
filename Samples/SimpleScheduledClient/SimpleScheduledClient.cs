// Copyright (c) 2022 EPAM Systems
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
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.Example
{
	public class SimpleScheduledClient
	{
		private static readonly ILog Logger = LogFactory.GetLog(typeof(SimpleScheduledClient));

		public static void Main()
		{
			// loading list of pre-configured sessions from the fixengine.properties file
			var configuredSessions = SessionParametersBuilder.BuildSessionParametersList(Config.DefaultEngineProperties);
			var sessions = new List<IFixSession>();

			foreach (var sessionParams in configuredSessions.Values)
			{
				try
				{
					// create pre-configured session
					var session = sessionParams.CreateScheduledInitiatorSession();
					sessions.Add(session);

					// create and attach listener 
					session.SetFixSessionListener(new FixSessionListener());

					// schedule the session
					session.Schedule();
				}
				catch (Exception e)
				{
					Logger.Error($"Cannot schedule {sessionParams.SessionId} session.", e);
				}
			}
			// waiting user input to terminate application
			Logger.Info(" ... Press ENTER to exit the program.");
			Console.Read();
			
			// disconnect and close configured sessions on application exit
			foreach (var session in sessions)
			{
				if (SessionState.IsNotDisconnected(session.SessionState))
				{
					session.Disconnect("Shutting down...");
				}
				session.Dispose();
			}
		}

		private class FixSessionListener : IFixSessionListener
		{
			/// <inheritdoc/>
			public void OnSessionStateChange(SessionState sessionState)
			{
				Logger.Info("Session state changed:" + sessionState);
			}

			/// <inheritdoc/>
			public void OnNewMessage(FixMessage message)
			{
				Logger.Info("Message " + message.GetTagValueAsString(Tags.MsgType) + " was received ");
			}
		}
	}
}
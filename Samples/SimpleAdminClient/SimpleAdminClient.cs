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

using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.Fixicc.Message;
using System;
using System.Text;
using System.Threading;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.Example
{
	public class SimpleAdminClient : IFixSessionListener
	{
		public virtual void OnSessionStateChange(SessionState sessionState)
		{
			// this callback is called upon session state change
			Console.WriteLine($"Session state changed: {sessionState}");
		}

		public virtual void OnNewMessage(FixMessage message)
		{
			// this callback is called upon new message arrival
			Console.WriteLine($"New application level message type: {message.GetTagValueAsString(Tags.MsgType)} received ");
		}

		public static void Main(string[] args)
		{
			// creating connection parameters
			var details = new SessionParameters();
			details.FixVersion = FixVersion.Fix44;
			details.Host = "127.0.0.1";
			details.HeartbeatInterval = 30;
			details.Port = 3000;
			details.SenderCompId = "sender";
			details.TargetCompId = "admin";

			// set login
			details.UserName = "admin";

			// set password
			details.Password = "admin";

			// create session we intend to work with
			var session = details.CreateNewFixSession();

			// listener for incoming messages and session state changes
			IFixSessionListener application = new SessionListenerImpl(session);

			// setting listener for incoming messages
			session.SetFixSessionListener(application);

			// initiate connection
			session.Connect();

			// create session list message
			var sessionsList = new SessionsList
			{
				RequestID = 1L,
				SubscriptionRequestType = 0
			};

			// create message
			var message = new FixMessage();
			var sessionsListString = MessageUtils.ToXml(sessionsList);
			message.AddTag(Tags.MsgType, Encoding.UTF8.GetBytes("n"));
			message.AddTag(Tags.XmlDataLen, sessionsListString.Length);
			message.AddTag(Tags.XmlData, sessionsListString);

			// sending message
			session.SendWithChanges(message, ChangesType.AddSmhAndSmt);

			// wait some time
			Thread.Sleep(3000);

			// disconnecting
			session.Disconnect("close app");
		}

		private class SessionListenerImpl : IFixSessionListener
		{
			private IFixSession _session;

			public SessionListenerImpl(IFixSession session)
			{
				_session = session;
			}

			// this method will be called every time session state is changed
			public void OnSessionStateChange(SessionState sessionState)
			{
				Console.WriteLine($"Session state changed:{sessionState}");
				if (SessionState.IsDisconnected(sessionState))
				{
					// end this session
					_session.Dispose();
				}
			}

			// processing incoming messages
			// this callback is called upon new message arrival
			public void OnNewMessage(FixMessage message)
			{
				Console.WriteLine($"New application message: {message} received ");
			}
		}
	}
}
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
using Epam.FixAntenna.AdminTool.Tests.Smoke.Util;
using Epam.FixAntenna.Fixicc.Message;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework;

namespace Epam.FixAntenna.AdminTool.Tests.Smoke
{
	/// <summary>
	/// Initiator helper class
	/// </summary>
	internal sealed class FixInitiatorHelper : IFixSessionListener, IDisposable
	{
		private Response _response;
		private readonly string _user;
		private readonly string _password;
		private readonly string _sender;
		private readonly string _target;
		private readonly int _port;

		private readonly System.Threading.CountdownEvent _condition = new System.Threading.CountdownEvent(1);
		private readonly System.Threading.CountdownEvent _conditionResponse = new System.Threading.CountdownEvent(1);

		private const int HeartbeatInterval = 30;

		public IFixSession Session { get; private set; }

		public Response GetResponse()
		{
			try
			{
				_conditionResponse.Wait(3000);
			}
			catch (Exception e)
			{
				Assert.Fail(e.Message);
			}
			return _response;
		}

		public FixInitiatorHelper(string sender, string target, int port, string user, string password)
		{
			_sender = sender;
			_target = target;
			_user = user;
			_password = password;
			_port = port;
		}

		public void Start()
		{
			SmokeUtil.CreateTask(() =>
			{
				var details = new SessionParameters();
				details.FixVersion = FixVersion.Fix44;
				details.Host = "localhost";
				details.HeartbeatInterval = HeartbeatInterval;
				details.Port = _port;
				details.SenderCompId = _sender;
				details.TargetCompId = _target;

				// set login
				if (_user != null)
				{
					details.OutgoingLoginMessage.AddTag(553, _user);
				}
				// set password
				if (_password != null)
				{
					details.OutgoingLoginMessage.AddTag(554, _password);
				}

				// create session we intend to work with
				Session = details.CreateNewFixSession();
				Session.SetFixSessionListener(this);
				Session.Connect();
			});

			try
			{
				_condition.Wait(10000);
			}
			catch (Exception e)
			{
				Assert.Fail(e.Message);
			}
		}

		public void Stop()
		{
			if (SessionState.IsConnected(Session.SessionState))
			{
				Session.Disconnect("");
			}
		}

		public void OnNewMessage(FixMessage message)
		{
			try
			{
				_response = SmokeUtil.GetXmlData(message);
				_conditionResponse.Signal();
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
				Console.Write(e.StackTrace);
				Assert.Fail("Unknown exception : " + e.Message);
			}
			Assert.IsNotNull(_response);
		}

		public void OnSessionStateChange(SessionState sessionState)
		{
			if (sessionState == SessionState.Disconnected)
			{
				Session.Dispose();
			}
			else if (SessionState.IsConnected(sessionState))
			{
				_condition.Signal();
			}
		}

		public void SendRequest(Request request)
		{
			SmokeUtil.SendRequest(request, Session);
		}

		private bool _disposed = false;

		private void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					_condition?.Dispose();
					_conditionResponse?.Dispose();
				}

				_disposed = true;
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}
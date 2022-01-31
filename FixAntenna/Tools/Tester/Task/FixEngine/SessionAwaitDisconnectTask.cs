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

using System.Collections.Generic;
using System.Threading;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.Tester.Task.FixEngine
{
	internal sealed class SessionAwaitDisconnectTask : ITask, IFixSessionListener
	{
		private static readonly ILog _log = LogFactory.GetLog(typeof(SessionAwaitDisconnectTask));

		private string _sessionIdAttributeName;
		private CustomConcurrentDictionary<string, object> _sessions;

		public void OnSessionStateChange(SessionState sessionState)
		{
			_log.Debug("Session state changed: " + sessionState);
		}

		public void OnNewMessage(FixMessage message)
		{
			_log.Debug("Session new message: " + message.ToPrintableString());
		}

		public void Init(IDictionary<string, string> @params, CustomConcurrentDictionary<string, object> sessions)
		{
			_sessionIdAttributeName = @params["sessionIdName"];
			this._sessions = sessions;
		}

		public void DoTask()
		{
			if (_sessions.ContainsKey(_sessionIdAttributeName))
			{
				var fixSession = (IFixSession) _sessions[_sessionIdAttributeName];
				fixSession.SetFixSessionListener(this);
				_log.Debug("Wait for sessions disconnect:" + fixSession);
				while (!SessionState.IsDisconnected(fixSession.SessionState))
				{
					Thread.Sleep(100);
				}
			}
			else
			{
				throw new TaskException("Session with name '" + _sessionIdAttributeName + "' was not found.");
			}
		}

		public void Dispose()
		{
		}
	}
}
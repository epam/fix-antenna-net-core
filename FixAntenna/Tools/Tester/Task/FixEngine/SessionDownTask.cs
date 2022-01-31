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
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.Tester.Task.FixEngine
{
	internal sealed class SessionDownTask : ITask, IFixSessionListener
	{
		private static readonly ILog _log = LogFactory.GetLog(typeof(SessionDownTask));

		private string _sessionIdAttributeName;
		private CustomConcurrentDictionary<string, object> _session;

		public void OnSessionStateChange(SessionState sessionState)
		{
			if (sessionState == SessionState.Disconnected)
			{

			}
		}

		public void OnNewMessage(FixMessage message)
		{
		}

		public void Init(IDictionary<string, string> @params, CustomConcurrentDictionary<string, object> session)
		{
			_sessionIdAttributeName = @params["sessionIdName"];
			this._session = session;
		}

		public void DoTask()
		{
			var fixSession = (IFixSession) _session[_sessionIdAttributeName];
			if (fixSession != null)
			{
				fixSession.SetFixSessionListener(this);
				_log.Debug("Attempt to shutdown session:" + fixSession);
				if (!SessionState.IsDisconnected(fixSession.SessionState))
				{
					fixSession.Disconnect("User request");
				}
				fixSession.Dispose();
			}
		}

		public void Dispose()
		{
		}
	}
}
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
	public sealed class SendMessage : ITask
	{
		private static readonly ILog _log = LogFactory.GetLog(typeof(SendMessage));

		private const string MESSAGE_PARAM = "message";
		private const string SESSION_ID_NAME_PARAM = "sessionIdName";
		private const string MESSAGE_TYPE_PARAM = "messageType";

		private string _message;
		private string _sessionIdName;
		private CustomConcurrentDictionary<string, object> _session;
		private string _messageType;

		public void Init(IDictionary<string, string> @params, CustomConcurrentDictionary<string, object> session)
		{
			_message = @params[MESSAGE_PARAM];
			_messageType = @params[MESSAGE_TYPE_PARAM];
			_message = _message.Replace('#', '\x0001');
			_sessionIdName = @params[SESSION_ID_NAME_PARAM];
			this._session = session;
		}

		public void DoTask()
		{
			IFixSession fixSession = (IFixSession) _session[_sessionIdName];
			fixSession.SendMessage(_messageType, RawFixUtil.GetFixMessage(_message));
		}

		public void Dispose()
		{
		}
	}
}
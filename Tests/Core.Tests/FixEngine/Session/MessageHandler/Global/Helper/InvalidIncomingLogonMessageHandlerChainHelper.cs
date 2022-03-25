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

using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global.Helper
{
	internal sealed class InvalidIncomingLogonMessageHandlerChainHelper : AbstractGlobalMessageHandler
	{
		private readonly InvalidIncomingLogonMessageHandler _messageHandler;
		private readonly TestFixSession _session;
		private FixMessage _message;

		public InvalidIncomingLogonMessageHandlerChainHelper()
		{
			_session = new TestFixSession
			{
				SessionState = SessionState.WaitingForLogon
			};

			_messageHandler = new InvalidIncomingLogonMessageHandler
			{
				Session = _session,
				NextHandler = this
			};
		}

		public void ProcessMessage(FixMessage message)
		{
			_messageHandler.OnNewMessage(message);
		}

		public override void OnNewMessage(FixMessage message)
		{
			this._message = message;
		}

		public FixMessage GetMessage()
		{
			return _message;
		}

		public void FreeResources()
		{
			_session.Dispose();
		}
	}
}
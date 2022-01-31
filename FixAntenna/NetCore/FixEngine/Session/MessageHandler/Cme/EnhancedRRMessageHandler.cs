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

using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.PerType;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Cme
{
	internal class EnhancedRRMessageHandler : AbstractGlobalMessageHandler
	{
		private ResendRequestMessageHandler _origRRHandler = new ResendRequestMessageHandler();

		public override IExtendedFixSession Session
		{
			set
			{
				base.Session = value;
				_origRRHandler.Session = value;
			}
		}

		public override void OnNewMessage(FixMessage message)
		{
			var msgType = message.MsgType;
			if (msgType.Length == 1 && msgType[0] == (byte)'2')
			{
				ProcessRr(message);
			}
			NextHandler.OnNewMessage(message);
		}

		private void ProcessRr(FixMessage message)
		{
			_origRRHandler.OnNewMessage(message);
		}
	}
}
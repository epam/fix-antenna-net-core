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
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler
{
	internal class CompositeUserMessageHandler : AbstractGlobalMessageHandler
	{

		private readonly IList<ISessionMessageHandler> _userMessageHandlers = new List<ISessionMessageHandler>();

		public virtual bool IsEmpty => _userMessageHandlers.Count == 0;

		/// <inheritdoc />
		public override void OnNewMessage(FixMessage message)
		{
			if (!IsEmpty)
			{
				_userMessageHandlers[_userMessageHandlers.Count - 1].OnNewMessage(message);
			}

			CallNextHandler(message);
		}

		public virtual void AddUserMessageHandler(AbstractUserGlobalMessageHandler userMessageHandler)
		{
			userMessageHandler.Session = Session;

			if (!IsEmpty)
			{
				userMessageHandler.NextHandler = _userMessageHandlers[_userMessageHandlers.Count - 1];
			}

			_userMessageHandlers.Add(userMessageHandler);
		}
	}
}
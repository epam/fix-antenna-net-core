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
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler
{
	internal class CompositeSystemMessageHandler : AbstractGlobalMessageHandler
	{
		private readonly IDictionary<TagValue, ISessionMessageHandler> _sessionMessageHandlers = new Dictionary<TagValue, ISessionMessageHandler>();
		private readonly TagValue _tempMsgType = new TagValue();

		public virtual void AddSystemHandler(string msgType, ISessionMessageHandler sessionMessageHandler)
		{
			AbstractGlobalMessageHandler handler = new AbstractGlobalMessageHandlerAnonymousInnerClass(this, sessionMessageHandler);
			sessionMessageHandler.Session = Session;
			handler.Session = Session;
			handler.NextHandler = NextHandler;

			var msgTypeTagValue = new TagValue(35, msgType);
			_sessionMessageHandlers[msgTypeTagValue] = handler;
		}

		internal class AbstractGlobalMessageHandlerAnonymousInnerClass : AbstractGlobalMessageHandler
		{
			private readonly CompositeSystemMessageHandler _outerInstance;

			private ISessionMessageHandler _sessionMessageHandler;

			public AbstractGlobalMessageHandlerAnonymousInnerClass(CompositeSystemMessageHandler outerInstance, ISessionMessageHandler sessionMessageHandler)
			{
				_outerInstance = outerInstance;
				_sessionMessageHandler = sessionMessageHandler;
			}

			/// <inheritdoc />
			public override void OnNewMessage(FixMessage message)
			{
				_sessionMessageHandler.OnNewMessage(message);
				_outerInstance.NextHandler.OnNewMessage(message);
			}
		}

		/// <inheritdoc />
		public override void OnNewMessage(FixMessage message)
		{
			IFixMessageListener sessionMessageHandler;
			message.LoadTagValue(35, _tempMsgType);
			if (RawFixUtil.IsSessionLevelMessage(message)
					&& (sessionMessageHandler = _sessionMessageHandlers.GetValueOrDefault(_tempMsgType)) != null)
			{
				sessionMessageHandler.OnNewMessage(message);
			}
			else
			{
				NextHandler.OnNewMessage(message);
			}
		}
	}
}
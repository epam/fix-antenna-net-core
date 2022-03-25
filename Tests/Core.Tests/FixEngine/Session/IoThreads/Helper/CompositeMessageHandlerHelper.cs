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

using Epam.FixAntenna.TestUtils.Hooks;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Post;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.IOThreads.Helper
{
	internal class CompositeMessageHandlerHelper : ICompositeMessageHandlerListener, IFixSessionListener
	{
		private HandlerChain _handlerChain = new HandlerChain();
		private volatile FixMessage _message;
		private EventHook _eventHook = new EventHook("messageReceived", 5000);
		private SessionState _sessionState;

		public CompositeMessageHandlerHelper(IExtendedFixSession session)
		{
			_handlerChain.Session = session;
			_handlerChain.AddGlobalMessageHandler(new VersionConsistencyHandler());
			_handlerChain.AddGlobalPostProcessMessageHandler(new IncrementIncomingMessageHandler());
			_handlerChain.AddGlobalPostProcessMessageHandler(new LastProcessedSequenceMessageHandler());
			_handlerChain.AddGlobalPostProcessMessageHandler(new RestoreSequenceAfterResendRequestHandler());
			_handlerChain.AddGlobalPostProcessMessageHandler(new AppendIncomingMessageHandler());
			_handlerChain.SetUserListener(this);
		}

		public virtual void OnMessage(MsgBuf messageBuf)
		{
			_handlerChain.OnMessage(messageBuf);
		}

		public virtual void OnNewMessage(FixMessage message)
		{
			_message = message.DeepClone(true, true);
			_eventHook.RaiseEvent();
		}

		public virtual void OnSessionStateChange(SessionState sessionState)
		{
			_sessionState = sessionState;
		}

		public virtual FixMessage GetMessage()
		{
			_eventHook.IsEventRaised();
			return _message;
		}

		public virtual SessionState GetSessionState()
		{
			return _sessionState;
		}

		public virtual void Destroy()
		{
			_eventHook.RaiseEvent();
			_eventHook.ResetEvent();
		}
	}
}
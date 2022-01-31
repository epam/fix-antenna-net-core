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
using System.Collections.Generic;
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler
{
	/// <summary>
	/// The composite message handler, provides the custom message type handling.
	/// For handling specific message type,
	/// use <see cref="AddSessionMessageHandler"/> with specific handler.
	/// </summary>
	internal class CompositeMessageHandler : AbstractGlobalMessageHandler, IFixSessionListener
	{
		private readonly IDictionary<TagValue, ISessionMessageHandler> _sessionMessageHandlers = new Dictionary<TagValue, ISessionMessageHandler>();
		private readonly ISet<IFixMessageListener> _userSessionLevelMsgListeners = new HashSet<IFixMessageListener>();
		private readonly UserListenerDelegate _userListenerDelegate = new UserListenerDelegate();
		private readonly TagValue _tempMsgType = new TagValue();

		/// <summary>
		/// Adds the message handler for message type.
		/// </summary>
		/// <param name="msgType">        the message type </param>
		/// <param name="messageHandler"> the message handler </param>
		public virtual void AddSessionMessageHandler(string msgType, ISessionMessageHandler messageHandler)
		{
			messageHandler.Session = Session;
			var msgTypeTag = new TagValue(35, msgType);
			_sessionMessageHandlers[msgTypeTag] = messageHandler;
			_userListenerDelegate.Session = Session;
		}

		/// <inheritdoc />
		public override void OnNewMessage(FixMessage message)
		{
			message.LoadTagValue(35, _tempMsgType);
			if (RawFixUtil.IsSessionLevelMessage(message))
			{
				var sessionMessageHandler = _sessionMessageHandlers.GetValueOrDefault(_tempMsgType);
				sessionMessageHandler?.OnNewMessage(message);

				if (_userSessionLevelMsgListeners.Count > 0)
				{
					foreach (var listener in _userSessionLevelMsgListeners)
					{
						listener.OnNewMessage(message);
					}
				}
			}
			else
			{
				_userListenerDelegate.OnNewMessage(message);
			}
		}

		/// <inheritdoc />
		public virtual void OnSessionStateChange(SessionState sessionState)
		{
			_userListenerDelegate.OnSessionStateChange(sessionState);
		}

		/// <summary>
		/// Sets the user handler.
		/// The user handler calls if custom message handler is no exists.
		/// </summary>
		/// <param name="listener"> the user listener </param>
		public virtual void SetUserListener(IFixSessionListener listener)
		{
			_userListenerDelegate.SetUserListener(listener);
		}

		/// <summary>
		/// Sets listener to receive session level incoming messages.
		/// </summary>
		/// <param name="listener"> the user listener </param>
		public virtual void AddUserSessionMessageListener(IFixMessageListener listener)
		{
			this._userSessionLevelMsgListeners.Add(listener);
		}

		private class UserListenerDelegate : AbstractSessionMessageHandler
		{
			internal const string ApplicationNotAvailable = "Application not available";

			internal IFixSessionListener UserListener;

			public virtual void SetUserListener(IFixSessionListener userListener)
			{
				this.UserListener = userListener;
			}

			public virtual void OnSessionStateChange(SessionState sessionState)
			{
				UserListener?.OnSessionStateChange(sessionState);
			}

			/// <inheritdoc />
			public override void OnNewMessage(FixMessage message)
			{
				SendMessageToUser(message);
			}

			public virtual void SendMessageToUser(FixMessage message)
			{
				if (UserListener == null)
				{
					if (Session.Parameters.Configuration.GetPropertyAsBoolean(Config.SendRejectIfApplicationIsNotAvailable, true))
					{
						SendApplicationNotAvailableReject(Session, message);
						Session.ForcedDisconnect(DisconnectReason.Reject, ApplicationNotAvailable, false);
					}
					else
					{
						throw new NoUserHandlerException(ApplicationNotAvailable);
					}

					return;
				}

				try
				{
					UserListener.OnNewMessage(message);
				}
				catch (Exception e)
				{
					if (Log.IsDebugEnabled)
					{
						Log.Warn(e, e);
					}
					else
					{
						Log.Warn(e);
					}

					LogWarnToSession("Method OnNewMessage of user listener thrown exception: " + e.Message, new InvalidMessageException(message));
					throw;
				}
			}

			public virtual void SendApplicationNotAvailableReject(IExtendedFixSession fixSession, FixMessage message)
			{
				fixSession.SendMessageOutOfTurn(MsgType.Reject,
					fixSession.MessageFactory.GetRejectForMessageTag(message, -1, 4, ApplicationNotAvailable));
			}
		}
	}
}
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
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Pre;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Message.SpecialTags;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler
{
	/// <summary>
	/// The <c>HandlerChain</c> provides the <tt>chain of responsibility</tt> pattern.
	/// </summary>
	internal sealed class HandlerChain : AbstractSessionMessageHandler, ICompositeMessageHandlerListener, IDisposable
	{
		private const string DefaultRawTags = "96, 91, 213, 349, 351, 353, 355, 357, 359, 361, 363, 365, 446, 619, 622";
		
		private readonly CompositeMessageHandler _compositeListener = new CompositeMessageHandler();
		private readonly CompositeUserMessageHandler _compositeUserListener = new CompositeUserMessageHandler();
		private readonly List<ISessionMessageHandler> _messageHandlers = new List<ISessionMessageHandler>();
		private readonly List<PreProcessMessageHandler> _preProcessMessageHandlers = new List<PreProcessMessageHandler>();
		private readonly List<IPostProcessMessageHandler> _postProcessMessageHandlers = new List<IPostProcessMessageHandler>();

		private RawFixUtil.IRawTags RawTags;
		private IMaskedTags _maskedTags;

		private volatile bool _disposed;

		/// <summary>
		/// Creates the <c>HandlerChain</c> with composite message handler.
		/// </summary>
		public HandlerChain()
		{
			_messageHandlers.Add(_compositeListener);
			_messageHandlers.Add(_compositeUserListener);

			_compositeUserListener.NextHandler = _compositeListener;

			RawTags = RawFixUtil.CreateRawTags(DefaultRawTags);
			_maskedTags = DefaultMaskedTags.Instance;
		}

		/// <inheritdoc />
		public override IExtendedFixSession Session
		{
			set
			{
				base.Session = value;
				_compositeListener.Session = value;
				_compositeUserListener.Session = value;

				RawTags = RawFixUtil.CreateRawTags(
					Session.Parameters.Configuration
						.GetProperty(Config.RawTags, DefaultRawTags));
				_maskedTags = CustomMaskedTags.Create(
					Session.Parameters.Configuration
						.GetProperty(Config.MaskedTags));
			}
		}

		/// <summary>
		/// Adds the specific message handle.
		/// </summary>
		/// <param name="msgType">        the message type </param>
		/// <param name="messageHandler"> the message handler </param>
		public void AddSessionMessageHandler(string msgType, ISessionMessageHandler messageHandler)
		{
			_compositeListener.AddSessionMessageHandler(msgType, messageHandler);
		}

		/// <summary>
		/// Adds the global message handler.
		/// The handler adds to the end of handlers list.
		/// </summary>
		/// <param name="globalMessageHandler"> the global message handler </param>
		public void AddGlobalMessageHandler(AbstractGlobalMessageHandler globalMessageHandler)
		{
			globalMessageHandler.Session = Session;
			globalMessageHandler.NextHandler = _messageHandlers[_messageHandlers.Count - 1];
			_messageHandlers.Add(globalMessageHandler);
		}

		public void AddUserGlobalMessageHandler(AbstractUserGlobalMessageHandler userMessageHandler)
		{
			_compositeUserListener.AddUserMessageHandler(userMessageHandler);
		}

		/// <summary>
		/// Adds the global message handler.
		/// The handler adds to the stert of handlers list.
		/// </summary>
		/// <param name="globalMessageHandler"> the global message handler </param>
		public void AddGlobalPostProcessMessageHandler(AbstractGlobalPostProcessSessionMessageHandler globalMessageHandler)
		{
			globalMessageHandler.Session = Session;
			if (_postProcessMessageHandlers.Count > 0)
			{
				globalMessageHandler.SetNext(_postProcessMessageHandlers[_postProcessMessageHandlers.Count - 1]);
			}

			_postProcessMessageHandlers.Add(globalMessageHandler);
		}

		public void AddGlobalPreProcessMessageHandler(PreProcessMessageHandler globalMessageHandler)
		{
			globalMessageHandler.Session = Session;
			if (_preProcessMessageHandlers.Count > 0)
			{
				globalMessageHandler.SetNext(_preProcessMessageHandlers[_preProcessMessageHandlers.Count - 1]);
			}

			_preProcessMessageHandlers.Add(globalMessageHandler);
		}

		/// <inheritdoc />
		public override void OnNewMessage(FixMessage message)
		{
			if (Session.SessionState != SessionState.WaitingForForcedDisconnect)
			{
				_messageHandlers[_messageHandlers.Count - 1].OnNewMessage(message);
			}
			else
			{
				throw new ArgumentException("Session is in forced logoff state. Incoming message " + message.ToPrintableString() + " was ignored.");
			}
		}

		internal FixMessage DefMessageForParse = FixMessageFactory.NewInstanceFromPoolForEngineParse();

		/// <inheritdoc />
		public void OnMessage(MsgBuf messageBuf)
		{
			var message = messageBuf.FixMessage;
			if (message == null)
			{
				message = RawFixUtil.GetFixMessage(DefMessageForParse, messageBuf, RawTags, false); // throw GarbledMessageException
				message.SetFixVersion(GetFixVersionForMessage());
			}

			try
			{
				// TBD!
				if (IsNeedPreProcess())
				{
					Log.Trace("Preprocess messages");
					message = PreProcessMessage(message);
					// TBD!
					//message = message.AsByteArray();
				}

				OnNewMessage(message); // ProcessMessageException, IllegalArgumentException, NullPointerException, etc
			}
			catch (InvalidMessageException e)
			{
				if (e.IsCritical())
				{
					LogErrorToSession("Invalid message received: " + messageBuf, e);
				}
				else
				{
					LogWarnToSession("Invalid message received: " + messageBuf, e);
				}
			}
			finally
			{
				DefMessageForParse.Clear();
			}

			OnPostProcessMessage(messageBuf);
		}

		private FixVersionContainer GetFixVersionForMessage()
		{
			var fixVersion = Session.Parameters.FixVersionContainer;
			if (fixVersion.FixVersion == FixVersion.Fixt11)
			{
				fixVersion = Session.Parameters.AppVersionContainer;
			}
			return fixVersion;
		}

		private FixMessage PreProcessMessage(FixMessage message)
		{
			if (_preProcessMessageHandlers.Count > 0)
			{
				return _preProcessMessageHandlers[_preProcessMessageHandlers.Count - 1].PreProcessMessage(message);
			}
			else
			{
				return message;
			}
		}

		public void OnPostProcessMessage(MsgBuf message)
		{
			_postProcessMessageHandlers[_postProcessMessageHandlers.Count - 1].OnPostProcessMessage(message);
		}

		/// <inheritdoc />
		public void OnSessionStateChange(SessionState sessionState)
		{
			_compositeListener.OnSessionStateChange(sessionState);
		}

		/// <summary>
		/// Sets the user message compositeListener.
		/// </summary>
		/// <param name="listener"> the session compositeListener </param>
		public void SetUserListener(IFixSessionListener listener)
		{
			_compositeListener.SetUserListener(listener);
		}

		/// <summary>
		/// Sets listener to receive session level incoming messages.
		/// </summary>
		/// <param name="listener"> the user listener </param>
		public void AddInSessionMessageListener(IFixMessageListener listener)
		{
			_compositeListener.AddUserSessionMessageListener(listener);
		}

		public bool IsNeedPreProcess()
		{
			return _preProcessMessageHandlers.Count > 0;
		}


		public void Dispose()
		{
			Dispose(true);
		}

		private void Dispose(bool disposing)
		{
			if (_disposed)
			{
				return;
			}

			if (disposing)
			{
				try
				{
					DefMessageForParse.ReleaseInstance();
				}
				catch (Exception t)
				{
					if (Log.IsDebugEnabled)
					{
						Log.Warn("Can't release instance: " + t.Message, t);
					}
					else
					{
						Log.Warn("Can't release instance: " + t.Message);
					}
				}
			}

			_disposed = true;
		}
	}
}
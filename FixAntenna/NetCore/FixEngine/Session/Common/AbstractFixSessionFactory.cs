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
using System.Reflection;

using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.PerType;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Post;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Pre;
using Epam.FixAntenna.NetCore.FixEngine.Transport;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.Common
{
	/// <summary>
	/// This is abstract implementation of FixSessionFactory.
	/// This class provides the base functionality for creates initiator and acceptor session,
	/// and configurations it listeners. The user implementation should override <see cref="GetMessageFactory"/>
	/// method.
	/// </summary>
	/// <seealso cref="IFixSession"></seealso>
	/// <seealso cref="HandlerChain"></seealso>
	/// <seealso cref="Impl.StandardSessionFactory"></seealso>
	internal abstract class AbstractFixSessionFactory : IFixSessionFactory
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(AbstractFixSessionFactory));

		private FixVersion _fixVersion = FixVersion.Fix42;
		public const string SystemMessagehandlerGlobal = "system.messagehandler.global.";
		public const string SystemMessagehandlerA = "system.messagehandler.A";
		public const string SystemMessagehandler0 = "system.messagehandler.0";
		public const string SystemMessagehandler1 = "system.messagehandler.1";
		public const string SystemMessagehandler2 = "system.messagehandler.2";
		public const string SystemMessagehandler3 = "system.messagehandler.3";
		public const string SystemMessagehandler4 = "system.messagehandler.4";
		public const string SystemMessagehandler5 = "system.messagehandler.5";
		public const string UserMessagehandlerGlobal = "user.messagehandler.global.";

		private static readonly DefaultSessionTransportFactory SessionTransportFactory = new DefaultSessionTransportFactory();

		/// <summary>
		/// Gets fix version.
		/// </summary>
		public virtual FixVersion GetFixVersion()
		{
			return _fixVersion;
		}

		/// <summary>
		/// Sets fix version.
		/// </summary>
		/// <param name="fixVersion"> the fix version </param>
		public virtual void SetFixVersion(FixVersion fixVersion)
		{
			_fixVersion = fixVersion;
		}

		public virtual IFixSession CreateInitiatorSession(SessionParameters details)
		{
			var initiator = CreateSession(details, (chain, sessionParameters) => GetInitiatorSession(sessionParameters, chain));
			initiator.Init();
			return initiator;
		}

		private IFixSession CreateSession(SessionParameters details, Func<HandlerChain, SessionParameters, IExtendedFixSession> sessionProvider)
		{
#pragma warning disable CA2000 // Dispose objects before losing scope: chain object is disposed in AbstractFIXSession
			var chain = new HandlerChain();
#pragma warning restore CA2000 // Dispose objects before losing scope

			var parameters = GetClonedSessionParameters(details);
			//reset request of new sequences
			//FIXME: we are resetting flags before session was registered. It's possible that flags will be
			// reset before real connect (hidden reset)
			details.DisableInSeqNumsOnNextConnect();
			details.DisableOutSeqNumsOnNextConnect();
			IExtendedFixSession fixSession;
			fixSession = sessionProvider(chain, parameters);
			FixSessionManager.Instance.RegisterFixSession(fixSession);
			SetSessionLevelMessageHandlers(chain, parameters);
			SetGlobalMessageHandlers(chain, parameters);
			return fixSession;
		}

		private SessionParameters GetClonedSessionParameters(SessionParameters details)
		{
			try
			{
				return (SessionParameters) details.Clone();
			}
			catch (InvalidOperationException)
			{
				// clone is not supported
				return details;
			}
		}

		public virtual IExtendedFixSession GetAcceptorSession(SessionParameters details, IFixTransport transport, HandlerChain chain)
		{
			return new AcceptorFixSession(MessageFactory, details, chain, transport);
		}

		public virtual IExtendedFixSession GetInitiatorSession(SessionParameters details, HandlerChain chain)
		{
			return new AutoreconnectFixSession(MessageFactory, details, chain);
		}

		/// <inheritdoc />
		public virtual IFixSession CreateAcceptorSession(SessionParameters details)
		{
			var transport = SessionTransportFactory.CreateAcceptorTransport(null, details.Configuration);
			var acceptorSession = CreateAcceptorSession(details, transport);
			acceptorSession.Init(); //it's never been initialized so we need to do it
			return acceptorSession;
		}

		/// <inheritdoc />
		public virtual IFixSession CreateAcceptorSession(SessionParameters details, IFixTransport transport)
		{
			var acceptor = FindActiveSession(details);
			if (acceptor != null)
			{
				acceptor.ReinitSession(details, transport);
				return acceptor;
			}

			return CreateSession(details, (chain, sessionParameters) => GetAcceptorSession(sessionParameters, transport, chain));
		}

		private AcceptorFixSession FindActiveSession(SessionParameters details)
		{
			var session = FixSessionManager.Instance.Locate(details.SessionId.ToString());
			switch (session)
			{
				case null:
					return null;
				case AcceptorFixSession acceptorFixSession:
					return acceptorFixSession;
				default:
					throw new DuplicateSessionException($"Same Initiator session already exists. Duplicate session attempt: {details.SessionId}");
			}
		}

		public virtual void SetGlobalMessageHandlers(HandlerChain chain, SessionParameters details)
		{
			// order is important here!!!
			// The user global handlers can't handle the garbled message,
			// so the system global handlers must be loaded first.
			SetPostProcessMessageHandler(chain);
			LoadUserHandlers(chain, details);
			LoadSystemHandlers(chain, details);
		}

		private void LoadSystemHandlers(HandlerChain chain, SessionParameters details)
		{
			if (LoadHandlers(chain, details, SystemMessagehandlerGlobal) < 1)
			{

				// TBD!  commented out these handlers temporarily.
				//       need to revise them and clean up from "new" allocations

				// global message handlers are in reverse order here, so first one will get called last
				chain.AddGlobalMessageHandler(new MessageValidatorHandler());
				chain.AddGlobalMessageHandler(new ThrottleCheckingHandler());
				chain.AddGlobalMessageHandler(new PossDupMessageHandler());
				chain.AddGlobalMessageHandler(new InvalidIncomingLogonMessageHandler());
				chain.AddGlobalMessageHandler(new OutOfSequenceMessageHandler());
				chain.AddGlobalMessageHandler(new EnhancedTestRequestMessageHandler());
				chain.AddGlobalMessageHandler(new RrSequenceRangeResponseHandler());
				chain.AddGlobalMessageHandler(new AcceptorMissedResetOnLogonHandler());
				chain.AddGlobalMessageHandler(new QuietLogonModeHandler());
				SetGlobalSystemHandlers(chain, details); // TODO make better
				chain.AddGlobalMessageHandler(new SendingTimeAccuracyHandler());
				chain.AddGlobalMessageHandler(new VersionConsistencyHandler());
				chain.AddGlobalMessageHandler(new SenderTargetIdConsistencyHandler());
				chain.AddGlobalMessageHandler(new GarbledMessageHandler());
			}
			if (details.Configuration.GetPropertyAsBoolean(Config.EnableLoggingOfIncomingMessages, false))
			{
				chain.AddGlobalMessageHandler(new MsgLoggingHandler());
			}
		}

		public virtual void SetGlobalSystemHandlers(HandlerChain chain, SessionParameters details)
		{
			var handler = new CompositeSystemMessageHandler();
			chain.AddGlobalMessageHandler(handler);
			ISessionMessageHandler messageHandler;
			try
			{
				messageHandler = GetClassForProperty<ISessionMessageHandler>(details, SystemMessagehandler2, typeof(ResendRequestMessageHandler).FullName);
				handler.AddSystemHandler("2", messageHandler);
			}
			catch (Exception e)
			{
				Log.Fatal("Unable to load session level message handler", e);
			}
		}

		private void LoadUserHandlers(HandlerChain chain, SessionParameters details)
		{
			LoadHandlers(chain, details, UserMessagehandlerGlobal);
		}

		private void SetPostProcessMessageHandler(HandlerChain chain)
		{
			chain.AddGlobalPostProcessMessageHandler(new IncrementIncomingMessageHandler());
			chain.AddGlobalPostProcessMessageHandler(new LastProcessedSequenceMessageHandler());
			chain.AddGlobalPostProcessMessageHandler(new RestoreSequenceAfterResendRequestHandler());
			chain.AddGlobalPostProcessMessageHandler(new AppendIncomingMessageHandler());
		}

		private int LoadHandlers(HandlerChain chain, SessionParameters details, string propertyName)
		{
			int i;
			var configuration = details.Configuration;
			for (i = 0; configuration.Exists(propertyName + i); i++)
			{
				try
				{
					if (propertyName.StartsWith(UserMessagehandlerGlobal, StringComparison.Ordinal))
					{
						var globalUserMessageHandler = GetClassForProperty<AbstractUserGlobalMessageHandler>(details, propertyName + i, null);
						chain.AddUserGlobalMessageHandler(globalUserMessageHandler);
					}
					else
					{
						var globalMessageHandler = GetClassForProperty<AbstractGlobalMessageHandler>(details, propertyName + i, null);
						chain.AddGlobalMessageHandler(globalMessageHandler);
					}
				}
				catch (TargetInvocationException e)
				{
					if (Log.IsDebugEnabled)
					{
						Log.Warn("Global handler for '" + propertyName + i + "' instantiation failed", e);
					}
					else
					{
						Log.Warn("Global handler for '" + propertyName + i + "' instantiation failed. " + e.Message);
					}
				}
				catch (MemberAccessException e)
				{
					if (Log.IsDebugEnabled)
					{
						Log.Warn("Global handler for '" + propertyName + i + "' access denied", e);
					}
					else
					{
						Log.Warn("Global handler for '" + propertyName + i + "' access denied. " + e.Message);
					}
				}
				catch (TypeLoadException e)
				{
					if (Log.IsDebugEnabled)
					{
						Log.Warn("Global handler for '" + propertyName + i + "' not found", e);
					}
					else
					{
						Log.Warn("Global handler for '" + propertyName + i + "' not found. " + e.Message);
					}
				}
			}
			return i;
		}

		public virtual void SetSessionLevelMessageHandlers(HandlerChain chain, SessionParameters details)
		{
			AddSystemMessageHandler("A", chain, details, SystemMessagehandlerA, typeof(LogonMessageHandler).FullName);
			AddSystemMessageHandler("0", chain, details, SystemMessagehandler0, typeof(HeartbeatMessageHandler).FullName);
			// TODO make better
			// addSystemMessageHandler("1", chain, details, SYSTEM_MESSAGEHANDLER_1, FixAntenna.NetCore.FixEngine.Session.MessageHandler.PerType.TestRequestMessageHandler.class.getName());
			chain.AddSessionMessageHandler("1", new IgnoreMessageHandler());
			chain.AddSessionMessageHandler("2", new IgnoreMessageHandler());
			AddSystemMessageHandler("3", chain, details, SystemMessagehandler3, typeof(IgnoreMessageHandler).FullName);
			AddSystemMessageHandler("4", chain, details, SystemMessagehandler4, typeof(SequenceResetMessageHandler).FullName);
			AddSystemMessageHandler("5", chain, details, SystemMessagehandler5, typeof(LogoffMessageHandler).FullName);
		}

		private void AddSystemMessageHandler(string messageType, HandlerChain chain, SessionParameters details, string propertyName, string defaultClassName)
		{
			try
			{
				var sessionMessageHandler = GetClassForProperty<ISessionMessageHandler>(details, propertyName, defaultClassName);
				chain.AddSessionMessageHandler(messageType, sessionMessageHandler);
			}
			catch (Exception e)
			{
				Log.Fatal("Unable to load session level message handler", e);
			}
		}

		private THandlerInstance GetClassForProperty<THandlerInstance>(SessionParameters details, string propertyName, string defaultClassName)
		{
			var customLoader = details.CustomLoader;
			var configuration = details.Configuration;
			THandlerInstance handlerInstance = default;
			if (customLoader != null)
			{
				try
				{
					handlerInstance = (THandlerInstance) customLoader(propertyName);
				}
				catch (Exception e)
				{
					if (Log.IsDebugEnabled)
					{
						Log.Warn("Error occurred during class loading using property '" + propertyName + "' through custom loader. Class will be loaded by default loader", e);
					}
					else
					{
						Log.Warn("Error occurred during class loading using property '" + propertyName + "' through custom loader. Class will be loaded by default loader. " + e.Message);
					}
				}
			}
			if (handlerInstance == null)
			{
				var className = configuration.GetProperty(propertyName, defaultClassName);
				if (Log.IsTraceEnabled)
				{
					Log.Trace("Creating '" + className + "' handler...");
				}
				if (!string.IsNullOrEmpty(className))
				{
					handlerInstance = (THandlerInstance)Activator.CreateInstance(Type.GetType(className));
				}
			}
			return handlerInstance;
		}

		/// <summary>
		/// Gets message factory.
		/// </summary>
		/// <value> Method returns the message factory instance. </value>
		public abstract IFixMessageFactory MessageFactory { get; }
	}
}
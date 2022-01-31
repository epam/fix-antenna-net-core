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
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Acceptor.Autostart;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.NetCore.FixEngine.Transport;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Acceptor
{
	internal abstract class SessionAcceptorStrategyHandler
	{
		protected internal static readonly ILog Log = LogFactory.GetLog(typeof(SessionAcceptorStrategyHandler));

		protected internal IDictionary<SessionId, IExtendedFixSession> RegisteredAcceptorSessions = new Dictionary<SessionId, IExtendedFixSession>();
		protected internal IConfiguredSessionRegister ConfiguredSessionRegister;
		protected internal AutostartAcceptorSessions AutostartAcceptorSessions;
		protected internal IFixServerListener Listener;

		public virtual void SetSessionListener(IFixServerListener listener)
		{
			Listener = listener;
		}

		public virtual void Init(Config configuration, IConfiguredSessionRegister configuredSessionRegister)
		{
			ConfiguredSessionRegister = configuredSessionRegister;
			AutostartAcceptorSessions = new AutostartAcceptorSessions(configuration, configuredSessionRegister);
		}

		public abstract void HandleIncomingConnection(SessionParameters sessionParameters, IFixTransport fixTransport);

		public virtual bool IsAcceptorSessionRegistered(SessionId sessionId)
		{
			return ConfiguredSessionRegister.IsSessionRegistered(sessionId);
		}

		public virtual void CloseDeniedSession(IExtendedFixSession session)
		{
			var description = "Application not available for session " + session.Parameters.SessionId;
			CloseSession(DisconnectReason.Reject, description, session);
			Log.Warn(description);
		}

		public virtual void CloseSession(DisconnectReason reason, string description, IExtendedFixSession session)
		{
			session.Disconnect(reason, description);
			session.Dispose();
		}

		public virtual void RegisterSession(SessionId sessionId, IExtendedFixSession fixSession)
		{
			lock (RegisteredAcceptorSessions)
			{
				RegisteredAcceptorSessions[sessionId] = fixSession;
			}
		}

		public virtual void CloseAllRegisteredSessions()
		{
			lock (RegisteredAcceptorSessions)
			{
				var iterator = RegisteredAcceptorSessions.Keys.GetEnumerator();
				while (iterator.MoveNext())
				{
					var sessionId = iterator.Current;
					CloseSession(DisconnectReason.UserRequest, "Application shutdown", RegisteredAcceptorSessions[sessionId]);
				}

				RegisteredAcceptorSessions.Clear();
			}

			ConfiguredSessionRegister.DeleteAll();
		}

		public virtual void CheckSessionParameters(SessionParameters newParams, SessionParameters registeredParams)
		{
			var errors = new List<string>();
			if (!registeredParams.IsSimilar(newParams, errors))
			{
				throw new InvalidOperationException(
					"Connected session doesn't look similar to registered one: " + 
					newParams.SessionId + 
					". Session details not similar to existing one: " + 
					string.Join(Environment.NewLine, errors));
			}
		}

		public virtual void MergeSessionParameters(SessionParameters sessionParameters, SessionParameters registeredSessionParameters)
		{
			registeredSessionParameters.IncomingLoginMessage = sessionParameters.IncomingLoginMessage;
			if (registeredSessionParameters.OutgoingLoginMessage == null)
			{
				registeredSessionParameters.OutgoingLoginMessage = sessionParameters.OutgoingLoginMessage;
			}
			else
			{
				// TODO: Simplified when the FixMessage will have special methods.
				var from = sessionParameters.OutgoingLoginMessage;
				if (from != null && from.Count > 0)
				{
					var to = registeredSessionParameters.OutgoingLoginMessage;
					var tag = new TagValue();
					for (var i = 0; i < from.Count; i++)
					{
						from.LoadTagValueByIndex(i, tag);
						to.UpdateValue(tag.TagId, tag.Buffer, tag.Offset, tag.Length, IndexedStorage.MissingTagHandling.AddIfNotExists);
					}
				}
			}
			registeredSessionParameters.HeartbeatInterval = sessionParameters.HeartbeatInterval;
			registeredSessionParameters.SenderSubId = sessionParameters.SenderSubId;
			registeredSessionParameters.TargetSubId = sessionParameters.TargetSubId;
			registeredSessionParameters.SenderLocationId = sessionParameters.SenderLocationId;
			registeredSessionParameters.TargetLocationId = sessionParameters.TargetLocationId;
			registeredSessionParameters.Host = sessionParameters.Host;
			registeredSessionParameters.Port = sessionParameters.Port;
		}

		public virtual void ProcessIncomingAdminSession(IFixTransport fixTransport, SessionId sessionId, SessionParameters sessionParameters)
		{
			if (AutostartAcceptorSessions.IsAdminSession(sessionParameters))
			{
				if (!AutostartAcceptorSessions.IsAcceptedAdminSession(sessionParameters))
				{
					throw new NotAcceptedAdminSessionException("Session is not accepted: " + sessionParameters.SessionId);
				}
				CreateAdminSession(sessionParameters, fixTransport, sessionId);
			}
		}

		public virtual void CreateAdminSession(SessionParameters sessionParameters, IFixTransport fixTransport, SessionId sessionId)
		{
			var session = (IExtendedFixSession) AutostartAcceptorSessions.AutoStartAcceptorFactory.CreateSession(AutostartAcceptorSessions.GetSessionDetails(sessionParameters), sessionParameters, fixTransport);
			RegisterSession(sessionId, session);
			AutostartAcceptorSessions.GetFixServerListener(sessionParameters).NewFixSession(session);
		}
	}
}
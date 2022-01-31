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
using System.Collections.Immutable;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.FixEngine.Session;

namespace Epam.FixAntenna.NetCore.FixEngine.Manager
{
	internal sealed class ConfiguredSessionRegisterImpl : IConfiguredSessionRegister
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(ConfiguredSessionRegisterImpl));

		private readonly Dictionary<CombinedSessionId, SessionParameters> _registeredSessionParameters = new Dictionary<CombinedSessionId, SessionParameters>();
		private readonly List<IConfiguredSessionListener> _listenersConfigured = new List<IConfiguredSessionListener>();

		/// <inheritdoc />
		public void RegisterSession(SessionParameters p)
		{
			var sessionId = new CombinedSessionId(p.SenderCompId, p.TargetCompId, p.SessionQualifier);
			if (IsSessionRegistered(sessionId))
			{
				throw new DuplicateSessionException("Configured session already exists. Duplicate sessionId: " + sessionId);
			}

			RegisterSession(sessionId, p);
		}

		public void RegisterSession(SessionId sessionId, SessionParameters p)
		{
			var combinedSessionId = new CombinedSessionId(sessionId?.Sender, sessionId?.Target, sessionId?.Qualifier);
			RegisterSession(combinedSessionId, p);
		}

		private void RegisterSession(CombinedSessionId sessionId, SessionParameters sessionParameters)
		{
			_registeredSessionParameters[sessionId] = sessionParameters;
			NotifyConfiguredSessionAdd(sessionParameters);
		}

		/// <inheritdoc />
		public IList<SessionParameters> RegisteredSessions
		{
			get { return new List<SessionParameters>(_registeredSessionParameters.Values); }
		}

		/// <inheritdoc />
		public void UnregisterSession(SessionParameters p)
		{
			var sessionId = new CombinedSessionId(p.SenderCompId, p.TargetCompId, p.SessionQualifier);
			UnregisterSession(sessionId);
		}

		/// <inheritdoc />
		public bool IsSessionRegistered(string senderCompId, string targetCompId)
		{
			return IsSessionRegistered(new CombinedSessionId(senderCompId, targetCompId, null));
		}

		/// <inheritdoc />
		public bool IsSessionRegistered(SessionId sessionId)
		{
			var combinedSessionId = new CombinedSessionId(sessionId?.Sender, sessionId?.Target, sessionId?.Qualifier);
			return IsSessionRegistered(combinedSessionId);
		}

		private bool IsSessionRegistered(CombinedSessionId combinedSessionId)
		{
			return _registeredSessionParameters.ContainsKey(combinedSessionId);
		}

		/// <inheritdoc />
		public SessionParameters GetSessionParams(string senderCompId, string targetCompId)
		{
			return GetSessionParams(new CombinedSessionId(senderCompId, targetCompId, null));
		}

		/// <inheritdoc />
		public SessionParameters GetSessionParams(SessionId sessionId)
		{
			var combinedSessionId = new CombinedSessionId(sessionId?.Sender, sessionId?.Target, sessionId?.Qualifier);
			return GetSessionParams(combinedSessionId);
		}

		private SessionParameters GetSessionParams(CombinedSessionId combinedSessionId)
		{
			return _registeredSessionParameters.GetValueOrDefault(combinedSessionId);
		}

		private void UnregisterSession(CombinedSessionId combinedSessionId)
		{
			SessionParameters parameters = null;
			lock (_registeredSessionParameters)
			{
				parameters = _registeredSessionParameters.GetValueOrDefault(combinedSessionId);
				if (parameters == null) return;
				_registeredSessionParameters.Remove(combinedSessionId);
			}

			NotifyConfiguredSessionRemoved(parameters);
		}

		/// <summary>
		/// Register client ConfiguredSessionListener.
		/// </summary>
		/// <param name="listener"> </param>
		public void AddSessionManagerListener(IConfiguredSessionListener listener)
		{
			if (!_listenersConfigured.Contains(listener))
			{
				_listenersConfigured.Add(listener);
			}
		}

		/// <summary>
		/// Unregister client ConfiguredSessionListener.
		/// </summary>
		/// <param name="listener"> </param>
		public void RemoveSessionManagerListener(IConfiguredSessionListener listener)
		{
			_listenersConfigured.Remove(listener);
		}

		/// <inheritdoc />
		public void DeleteAll()
		{
			var keysCopy = _registeredSessionParameters.Keys.ToImmutableList();
			foreach (var sessionId in keysCopy)
			{
				var pars = _registeredSessionParameters[sessionId];
				if (_registeredSessionParameters.Remove(sessionId))
				{
					NotifyConfiguredSessionRemoved(pars);
				}
			}
		}

		public void NotifyConfiguredSessionAdd(SessionParameters @params)
		{
			foreach (var l in _listenersConfigured)
			{
				try
				{
					l.OnAddSession(@params);
				}
				catch (Exception e)
				{
					Log.Error("Error on call onAddSession. Cause: " + e.Message, e);
				}
			}
		}

		public void NotifyConfiguredSessionRemoved(SessionParameters @params)
		{
			foreach (var l in _listenersConfigured)
			{
				try
				{
					l.OnRemoveSession(@params);
				}
				catch (Exception e)
				{
					Log.Error("Error on call onRemoveSession. Cause: " + e.Message, e);
				}
			}
		}

		private class CombinedSessionId
		{
			private readonly string _senderCompId;
			private readonly string _targetCompId;
			private readonly string _qualifier;

			public CombinedSessionId(string senderCompId, string targetCompId, string qualifier)
			{
				_senderCompId = senderCompId?.ToLowerInvariant();
				_targetCompId = targetCompId?.ToLowerInvariant();
				_qualifier = qualifier?.ToLowerInvariant();
			}

			public override int GetHashCode()
			{
				return Tuple.Create(_senderCompId, _targetCompId, _qualifier).GetHashCode();
			}

			public override bool Equals(object obj)
			{
				if (!(obj is CombinedSessionId that))
					return false;

				return string.Equals(_targetCompId, that._targetCompId, StringComparison.Ordinal)
							&& string.Equals(_senderCompId, that._senderCompId, StringComparison.Ordinal)
							&& string.Equals(_qualifier, that._qualifier, StringComparison.Ordinal);
			}

			public override string ToString()
			{
				return $"TargetCompId: {_targetCompId}, SenderCompId: {_senderCompId}, Qualifier: {_qualifier}";
			}
		}
	}
}
// Copyright (c) 2022 EPAM Systems
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
using Epam.FixAntenna.NetCore.Common.Logging;

namespace Epam.FixAntenna.NetCore.FixEngine.Scheduler
{
	/// <summary>
	/// Storage for policies of incoming connection.
	/// </summary>
	public class AllowedSessionRegistry
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(AllowedSessionRegistry));
		private readonly HashSet<string> _allowed = new HashSet<string>();
		private readonly HashSet<string> _denied = new HashSet<string>();
		private volatile bool _allowByDefault;
		private readonly object _syncLock = new object();

		public AllowedSessionRegistry() : this(true)
		{
		}

		public AllowedSessionRegistry(bool allowByDefault)
		{
			_allowByDefault = allowByDefault;
		}

		public void AllowByDefault()
		{
			_allowByDefault = true;
		}

		public void DenyByDefault()
		{
			_allowByDefault = false;
		}

		public void AllowSessionForConnect(SessionParameters parameters)
		{
			var sessionId = parameters.SessionId.ToString();
			lock (_syncLock)
			{
				_allowed.Add(sessionId);
				_denied.Remove(sessionId);
			}
			if (Log.IsDebugEnabled)
			{
				Log.Debug("Session became allowed on server: " + parameters);
			}
		}

		public void DenySessionForConnect(SessionParameters parameters)
		{
			var sessionId = parameters.SessionId.ToString();
			lock (_syncLock)
			{
				_allowed.Remove(sessionId);
				_denied.Add(sessionId);
			}
			if (Log.IsDebugEnabled)
			{
				Log.Debug("Session became denied on server: " + parameters);
			}
		}

		public void Clean(SessionParameters parameters)
		{
			var sessionId = parameters.SessionId.ToString();
			lock (_syncLock)
			{
				_allowed.Remove(sessionId);
				_denied.Remove(sessionId);
			}
			if (Log.IsDebugEnabled)
			{
				Log.Debug("Session was removed from rules: " + parameters);
			}
		}

		public bool IsSessionAllowed(SessionParameters parameters)
		{
			var sessionId = parameters.SessionId.ToString();
			lock (_syncLock)
			{
				if (_allowByDefault)
				{
					return _allowed.Contains(sessionId) || !_denied.Contains(sessionId);
				}
				else
				{
					return _allowed.Contains(sessionId) && !_denied.Contains(sessionId);
				}
			}
		}
	}
}
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

namespace Epam.FixAntenna.NetCore.FixEngine
{
	public sealed class SessionId : ICloneable
	{
		private string _sender;
		private string _target;
		private string _qualifier;
		private string _sessionId;
		private string _customId;

		public string Sender
		{
			get => _sender;
			set
			{
				_sender = value;
				UpdateSessionId();
			}
		}

		public string Target
		{
			get => _target;
			set
			{
				_target = value;
				UpdateSessionId();
			}
		}

		public string Qualifier
		{
			get => _qualifier;
			set
			{
				_qualifier = value;
				UpdateSessionId();
			}
		}

		public string CustomSessionId
		{
			get => _customId;
			set
			{
				_customId = value;
				UpdateSessionId();
			}
		}

		public SessionId(string sender, string target, string qualifier = null, string customSessionId = null)
		{
			_sender = sender;
			_target = target;
			_qualifier = qualifier;
			_customId = customSessionId;
			UpdateSessionId();
		}

		public object Clone()
		{
			return new SessionId(Sender, Target, Qualifier, CustomSessionId);
		}

		public bool IsCustomSessionId => !string.IsNullOrEmpty(CustomSessionId);

		private void UpdateSessionId()
		{
			if (!string.IsNullOrEmpty(CustomSessionId))
			{
				_sessionId = CustomSessionId;
			}
			else
			{
				_sessionId = Sender + "-" + Target;
				if (!string.IsNullOrEmpty(Qualifier))
				{
					_sessionId += "-" + Qualifier;
				}
			}
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}

			if (!(o is SessionId that))
			{
				return false;
			}

			return _sessionId?.Equals(that._sessionId, StringComparison.Ordinal) ?? that._sessionId == null;
		}

		public override int GetHashCode()
		{
			return _sessionId.GetHashCode();
		}

		public override string ToString()
		{
			return _sessionId;
		}
	}
}
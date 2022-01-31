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
using Epam.FixAntenna.NetCore.Helpers;

namespace Epam.FixAntenna.NetCore.Validation.Utils.Cache
{
	internal class MessagesCache : ICache<string, GroupsCache>
	{
		private readonly IDictionary<string, GroupsCache> _messagesGroupsCache = new Dictionary<string, GroupsCache>();

		/// <inheritdoc />
		public virtual GroupsCache Get(string key)
		{
			_messagesGroupsCache.TryGetValue(key, out var value);
			return value;
		}

		/// <inheritdoc />
		public virtual void Put(string key, GroupsCache value)
		{
			_messagesGroupsCache[key] = value;
		}

		public object Clone()
		{
			return CloneHelper.CloneObject(this);
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}

			if (o == null || GetType() != o.GetType())
			{
				return false;
			}

			var that = (MessagesCache)o;

			if (_messagesGroupsCache != null
				? !_messagesGroupsCache.Equals(that._messagesGroupsCache)
				: that._messagesGroupsCache != null)
			{
				return false;
			}

			return true;
		}

		public override int GetHashCode()
		{
			return _messagesGroupsCache != null ? _messagesGroupsCache.GetHashCode() : 0;
		}

		public override string ToString()
		{
			return "MessagesCache{" + "messagesGroupsCashe=" + _messagesGroupsCache + '}';
		}
	}
}
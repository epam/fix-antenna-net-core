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
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Validation.Entities;

namespace Epam.FixAntenna.NetCore.Validation.Utils.Cache
{
	internal class GroupCache : AbstractCache
	{
		private readonly int _groupId;
		private readonly int _startTagId;

		public GroupCache(int groupId, int startTagId)
		{
			_groupId = groupId;
			_startTagId = startTagId;
		}

		public virtual IDictionary<int, Field> GetCache()
		{
			return MapCache;
		}

		public virtual void PutAllCache(IDictionary<int, Field> mapCache)
		{
			MapCache.PutAll(mapCache);
		}

		public virtual int GetGroupId()
		{
			return _groupId;
		}

		public virtual int GetStartTagId()
		{
			return _startTagId;
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

			var that = (GroupCache)o;

			return _groupId.Equals(that._groupId);
		}

		public override int GetHashCode()
		{
			return _groupId.GetHashCode();
		}

		public override string ToString()
		{
			return "GroupCache{" + "groupId=" + _groupId + '}';
		}

		public override object Clone()
		{
			var groupCache = CloneHelper.CloneObject(this);
			CloneInnerFields(groupCache);
			return groupCache;
		}
	}
}
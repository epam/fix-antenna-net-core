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
using Epam.FixAntenna.NetCore.Validation.Utils.Cache.Keys;

namespace Epam.FixAntenna.NetCore.Validation.Utils.Cache
{
	internal class GroupsCache : AbstractGroupsCache
	{
		private readonly IDictionary<int, ISet<BlockCacheKey>> _groupsBlocksCache =
			new Dictionary<int, ISet<BlockCacheKey>>();

		// contain RG's tags with Block tags. Inner RG and Blocks of inner RG is not included.
		private readonly IDictionary<int, ISet<int>> _groupsTagIDsWithBlocksTags = new Dictionary<int, ISet<int>>();
		private readonly IDictionary<int, ISet<int>> _groupsWithInternalCache = new Dictionary<int, ISet<int>>();

		public virtual void PutGroupCache(int parentGroupId, int groupId, GroupCache cache)
		{
			var exists = _groupsWithInternalCache.TryGetValue(parentGroupId, out var value);
			if (value == null)
			{
				value = new HashSet<int>();
			}

			value.Add(groupId);
			if (exists)
			{
				_groupsWithInternalCache[parentGroupId] = value;
			}
			else
			{
				_groupsWithInternalCache.Add(parentGroupId, value);
			}

			GroupsCaches[groupId] = cache;
			AddToGroupsTagIDsWithBlocksTags(groupId, cache.GetCache().Keys);
		}

		public virtual ISet<int> GetGroupsTagIDsWithBlocksTags(int rgTag)
		{
			_groupsTagIDsWithBlocksTags.TryGetValue(rgTag, out var value);
			return value;
		}

		public virtual void PutBlockCache(int parentGroupId, BlockCache cache)
		{
			_groupsBlocksCache.TryGetValue(parentGroupId, out var value);
			var block = cache.GetBlock();
			if (value == null)
			{
				value = new HashSet<BlockCacheKey>();
			}

			var key = new BlockCacheKey(block.Idref, cache.GetGroupId());
			value.Add(key);
			_groupsBlocksCache[parentGroupId] = value;
			BlockGroupsCaches[key] = cache;
			AddToGroupsTagIDsWithBlocksTags(parentGroupId, cache.GetCache().Keys);
		}

		public virtual GroupCache GetGroupCache(int groupId)
		{
			GroupsCaches.TryGetValue(groupId, out var value);
			return value;
		}

		public virtual GroupCache GetGroupCacheWithOutInternal(int groupId)
		{
			_groupsWithInternalCache.TryGetValue(groupId, out var keys);
			var cache = (GroupCache)GroupsCaches[groupId].Clone();
			if (keys != null)
			{
				foreach (var key in keys)
				{
					if (key == groupId)
					{
						continue;
					}

					cache.GetCache().Remove(key);
				}
			}

			return cache;
		}

		public virtual ISet<GroupCache> GetGroupCacheWithInternalGroups(int groupId)
		{
			ISet<GroupCache> groupCaches = new HashSet<GroupCache>();
			// gets root group data.
			groupCaches.Add(GroupsCaches[groupId]);
			var keys = _groupsWithInternalCache[groupId];
			if (keys != null)
			{
				foreach (var key in keys)
				{
					groupCaches.Add(GroupsCaches[key]);
				}
			}

			return groupCaches;
		}

		public virtual BlockCache GetBlockCache(BlockCacheKey blockIdRef)
		{
			BlockGroupsCaches.TryGetValue(blockIdRef, out var value);
			return value;
		}

		public virtual ISet<BlockCache> GetGroupBlocksCache(int groupId)
		{
			ISet<BlockCache> blockCaches = new HashSet<BlockCache>();
			_groupsBlocksCache.TryGetValue(groupId, out var keys);
			if (keys != null)
			{
				foreach (var key in keys)
				{
					blockCaches.Add(BlockGroupsCaches[key]);
				}
			}

			return blockCaches;
		}

		private void AddToGroupsTagIDsWithBlocksTags(int groupId, ICollection<int> idSet)
		{
			_groupsTagIDsWithBlocksTags.TryGetValue(groupId, out var value);
			if (value == null)
			{
				value = new HashSet<int>();
				_groupsTagIDsWithBlocksTags[groupId] = value;
			}

			foreach (var id in idSet)
			{
				value.Add(id);
			}
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

			var that = (GroupsCache)o;

			if (_groupsBlocksCache != null
				? !_groupsBlocksCache.Equals(that._groupsBlocksCache)
				: that._groupsBlocksCache != null)
			{
				return false;
			}

			if (_groupsWithInternalCache != null
				? !_groupsWithInternalCache.Equals(that._groupsWithInternalCache)
				: that._groupsWithInternalCache != null)
			{
				return false;
			}

			return true;
		}

		public override int GetHashCode()
		{
			var result = _groupsWithInternalCache != null ? _groupsWithInternalCache.GetHashCode() : 0;
			result = 31 * result + (_groupsBlocksCache != null ? _groupsBlocksCache.GetHashCode() : 0);
			return result;
		}

		public override string ToString()
		{
			return "GroupsCache{" + "groupsWithInternalCache=" + _groupsWithInternalCache + ", groupsBlocksCache=" +
					_groupsBlocksCache + '}';
		}

		public override object Clone()
		{
			var groupsCache = CloneHelper.CloneObject(this);
			CloneInnerFields(groupsCache);
			return groupsCache;
		}
	}
}
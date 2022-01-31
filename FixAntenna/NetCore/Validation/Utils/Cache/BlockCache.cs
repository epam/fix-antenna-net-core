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
	internal class BlockCache : AbstractCache
	{
		private readonly Block _block;
		private readonly int _groupId;

		public BlockCache(Block block, int groupId)
		{
			_block = block;
			_groupId = groupId;
		}

		public virtual IDictionary<int, Field> GetCache()
		{
			return MapCache;
		}

		public virtual Block GetBlock()
		{
			return _block;
		}

		public virtual void PutAllCache(IDictionary<int, Field> mapCache)
		{
			MapCache.PutAll(mapCache);
		}

		public virtual int GetGroupId()
		{
			return _groupId;
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

			var that = (BlockCache)o;

			if (_groupId != that._groupId)
			{
				return false;
			}

			if (_block != null ? !_block.Equals(that._block) : that._block != null)
			{
				return false;
			}

			return true;
		}

		public override int GetHashCode()
		{
			var result = _block != null ? _block.GetHashCode() : 0;
			result = 31 * result + _groupId;
			return result;
		}

		public override string ToString()
		{
			return "BlockCache{" + "block=" + _block + ", groupId=" + _groupId + '}';
		}

		public override object Clone()
		{
			var blockCache = CloneHelper.CloneObject(this);
			CloneInnerFields(blockCache);
			return blockCache;
		}
	}
}
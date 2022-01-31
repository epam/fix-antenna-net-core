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

namespace Epam.FixAntenna.NetCore.Validation.Utils.Cache.Keys
{
	internal class GroupCacheKey : IKey<int?, int?>
	{
		private readonly int? _foreignKey;
		private readonly int? _primaryKey;

		public GroupCacheKey(int? primaryKey, int? foreignKey)
		{
			_foreignKey = foreignKey;
			_primaryKey = primaryKey;
		}

		/// <inheritdoc />
		public int? GetPrimaryKey()
		{
			return null;
		}

		/// <inheritdoc />
		public int? GetForeignKey()
		{
			return null;
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

			var that = (GroupCacheKey)o;

			if (_foreignKey != null ? !_foreignKey.Equals(that._foreignKey) : that._foreignKey != null)
			{
				return false;
			}

			if (_primaryKey != null ? !_primaryKey.Equals(that._primaryKey) : that._primaryKey != null)
			{
				return false;
			}

			return true;
		}

		public override int GetHashCode()
		{
			var result = _primaryKey != null ? _primaryKey.GetHashCode() : 0;
			result = 31 * result + (_foreignKey != null ? _foreignKey.GetHashCode() : 0);
			return result;
		}

		public override string ToString()
		{
			return "GroupCacheKey{" + "primaryKey=" + _primaryKey + ", foreignKey=" + _foreignKey + '}';
		}
	}
}
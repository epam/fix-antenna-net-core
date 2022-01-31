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
	internal class BlockCacheKey : IKey<string, int>
	{
		private readonly int _foreignKey;
		private readonly string _primaryKey;

		public BlockCacheKey(string primaryKey, int foreignKey)
		{
			_primaryKey = primaryKey;
			_foreignKey = foreignKey;
		}

		/// <inheritdoc />
		public virtual int GetForeignKey()
		{
			return _foreignKey;
		}

		/// <inheritdoc />
		public virtual string GetPrimaryKey()
		{
			return _primaryKey;
		}

		public override string ToString()
		{
			return "BlockCacheKey{" + "primaryKey='" + _primaryKey + '\'' + ", foreignKey=" + _foreignKey + '}';
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

			var that = (BlockCacheKey)o;

			if (!_foreignKey.Equals(that._foreignKey))
			{
				return false;
			}

			if (_primaryKey == null)
			{
				return that._primaryKey == null;
			}

			return _primaryKey.Equals(that._primaryKey);
		}

		public override int GetHashCode()
		{
			var result = _primaryKey != null ? _primaryKey.GetHashCode() : 0;
			result = 31 * result + _foreignKey.GetHashCode();
			return result;
		}
	}
}
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
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Dictionary;

namespace Epam.FixAntenna.NetCore.Validation.Utils
{
	internal class FixUtilFactory
	{
		public static FixUtilFactory Instance { get; } = new FixUtilFactory();

		private readonly IDictionary<Key, FixUtil> _utilMap = new Dictionary<Key, FixUtil>();

		private FixUtilFactory()
		{
		}

		public static void SetLoadDescriptions(bool loadDescriptions)
		{
			FixDictionaryFactory.LoadDescriptions = loadDescriptions;
		}

		public virtual FixUtil GetFixUtil(FixVersionContainer version, FixVersionContainer appFixVersion)
		{
			var key = new Key(version, appFixVersion);
			lock (_utilMap)
			{
				if (!_utilMap.ContainsKey(key))
				{
					_utilMap[key] = new FixUtil(version, appFixVersion);
				}
			}

			return _utilMap[key];
		}

		public virtual FixUtil GetFixUtil(FixVersionContainer version)
		{
			var key = new Key(version, null);
			lock (_utilMap)
			{
				if (!_utilMap.ContainsKey(key))
				{
					_utilMap[key] = new FixUtil(version);
				}
			}

			return _utilMap[key];
		}

		public virtual void ClearResources()
		{
			lock (_utilMap)
			{
				_utilMap.Clear();
			}

			FixDictionaryFactory.Instance.CleanDictionaryCache();
		}

		internal class Key
		{
			internal FixVersionContainer AppFixVersion;
			internal FixVersionContainer Version;

			public Key(FixVersionContainer version, FixVersionContainer appFixVersion)
			{
				Version = version;
				AppFixVersion = appFixVersion;
			}

			public override bool Equals(object o)
			{
				if (this == o)
				{
					return true;
				}

				if (!(o is Key))
				{
					return false;
				}

				var key = (Key)o;

				if (AppFixVersion != null ? !AppFixVersion.Equals(key.AppFixVersion) : key.AppFixVersion != null)
				{
					return false;
				}

				if (Version != null ? !Version.Equals(key.Version) : key.Version != null)
				{
					return false;
				}

				return true;
			}

			public override int GetHashCode()
			{
				var result = Version != null ? Version.GetHashCode() : 0;
				result = 31 * result + (AppFixVersion != null ? AppFixVersion.GetHashCode() : 0);
				return result;
			}
		}
	}
}
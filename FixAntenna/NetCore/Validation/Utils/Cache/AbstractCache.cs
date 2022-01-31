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

using System.Collections;
using System.Collections.Generic;
using Epam.FixAntenna.NetCore.Validation.Entities;

namespace Epam.FixAntenna.NetCore.Validation.Utils.Cache
{
	internal abstract class AbstractCache : ICache<int, Field>
	{
		protected internal IDictionary<int, Field> MapCache = new Dictionary<int, Field>();

		/// <inheritdoc />
		public virtual Field Get(int key)
		{
			return MapCache[key];
		}

		/// <inheritdoc />
		public virtual void Put(int key, Field value)
		{
			MapCache[key] = value;
		}

		public abstract object Clone();

		public object CloneInnerFields(AbstractCache cache)
		{
			cache.MapCache = (IDictionary<int, Field>)((Hashtable)MapCache).Clone();
			return cache;
		}
	}
}
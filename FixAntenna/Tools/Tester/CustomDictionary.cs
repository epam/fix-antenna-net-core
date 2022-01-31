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

namespace Epam.FixAntenna.Tester
{
	public class CustomDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IDictionary<TKey, TValue> where TValue : class
	{
		public CustomDictionary(int capacity = 4) : base(capacity) { }

		public new TValue this[TKey key]
		{
			get => base.TryGetValue(key, out var val) ? val : null;
			set => base[key] = value;
		}
	}
}
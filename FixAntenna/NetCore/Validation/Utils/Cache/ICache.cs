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

namespace Epam.FixAntenna.NetCore.Validation.Utils.Cache
{
	internal interface ICache<T, TV> : ICloneable
	{
		/// <summary>
		/// Puts the value by key into cache.
		/// </summary>
		/// <param name="key">   Key of input value. </param>
		/// <param name="value"> The instance that Contains concrete value for input key. </param>
		void Put(T key, TV value);

		/// <summary>
		/// Returns value by input key.
		/// </summary>
		/// <param name="key"> Input key. </param>
		/// <returns> Instance of value. </returns>
		TV Get(T key);
	}
}
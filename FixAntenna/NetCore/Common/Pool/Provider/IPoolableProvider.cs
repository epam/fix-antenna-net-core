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

namespace Epam.FixAntenna.NetCore.Common.Pool.Provider
{
	/// <summary>
	/// Poolable object provider with ability to create/validate/activate/passivate/destroy
	/// </summary>
	internal interface IPoolableProvider<T>
	{
		T Create();

		void Activate(T @object);

		void Passivate(T @object);

		bool Validate(T @object);

		void Destroy(T @object);
	}
}
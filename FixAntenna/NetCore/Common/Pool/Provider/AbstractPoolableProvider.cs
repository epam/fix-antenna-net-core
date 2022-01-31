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
	/// Abstract poolable object provider
	/// </summary>
	internal abstract class AbstractPoolableProvider<T> : IPoolableProvider<T>
	{
		public abstract void Activate(T @object);

		public abstract T Create();

		public virtual bool Validate(T @object)
		{
			return true;
		}

		public virtual void Passivate(T @object)
		{
		}

		public virtual void Destroy(T @object)
		{
		}
	}
}
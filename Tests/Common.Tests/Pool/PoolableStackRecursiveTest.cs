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

using Epam.FixAntenna.NetCore.Common.Pool;
using Epam.FixAntenna.NetCore.Common.Pool.Provider;
using NUnit.Framework;

namespace Epam.FixAntenna.Common.Pool
{
	/// <summary>
	/// Pool size can be changes from 1 to 2. Test creates recursive calls for returnObject.
	/// Pool internal index should be damaged.
	/// </summary>
	[TestFixture]
	public class PoolableStackRecursiveTest
	{
		internal SynchronizedPoolableStack<ObjectC> PoolC;

		private class AbstractPoolableProviderAnonymousInnerClass : AbstractPoolableProvider<ObjectC>
		{
			private readonly PoolableStackRecursiveTest _outerInstance;

			public AbstractPoolableProviderAnonymousInnerClass(PoolableStackRecursiveTest outerInstance)
			{
				_outerInstance = outerInstance;
			}

			public override ObjectC Create()
			{
				return new ObjectC(_outerInstance);
			}

			public override void Activate(ObjectC @object)
			{
			}

			public override void Destroy(ObjectC @object)
			{
				@object.Release();
			}

			public override void Passivate(ObjectC @object)
			{
				@object.Release();
			}
		}

		internal class ObjectC
		{
			private readonly PoolableStackRecursiveTest _outerInstance;

			internal ObjectC Inner;

			public ObjectC(PoolableStackRecursiveTest outerInstance)
			{
				_outerInstance = outerInstance;
			}

			public virtual void SetInner(in ObjectC inner)
			{
				Inner = inner;
			}

			public virtual void Release()
			{
				if (Inner != null)
				{
					_outerInstance.PoolC.ReturnObject(Inner);
				}
			}
		}

		[Test]
		public virtual void RecursiveReleaseTest()
		{
			PoolC = new SynchronizedPoolableStack<ObjectC>(1, 2, new AbstractPoolableProviderAnonymousInnerClass(this));

			var leve1 = PoolC.Object;
			var level2 = PoolC.Object;
			leve1.SetInner(level2);
			level2.SetInner(PoolC.Object);

			PoolC.ReturnObject(leve1);
		}
	}
}
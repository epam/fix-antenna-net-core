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
using Epam.FixAntenna.NetCore.Common.Pool;
using Epam.FixAntenna.NetCore.Common.Pool.Provider;
using NUnit.Framework;

namespace Epam.FixAntenna.Common.Pool
{
	[TestFixture]
	public class SynchronizedPoolableStackTest
	{
		private class AbstractPoolableProviderAnonymousInnerClass : AbstractPoolableProvider<object>
		{
			private readonly SynchronizedPoolableStackTest _outerInstance;

			public AbstractPoolableProviderAnonymousInnerClass(SynchronizedPoolableStackTest outerInstance)
			{
				_outerInstance = outerInstance;
			}

			public override object Create()
			{
				return new object();
			}

			public override void Activate(object ignored)
			{
			}
		}

		[Test]
		public virtual void SmokeTest()
		{
			var initSize = 5;
			var maxSize = 10;
			IPool<object> objectPool = new SynchronizedPoolableStack<object>(initSize, maxSize,
				new AbstractPoolableProviderAnonymousInnerClass(this));
			ISet<string> hashesOfExistentObjects = new HashSet<string>();
			for (var i = 0; i < maxSize * 10; i++)
			{
				IList<object> objects = new List<object>();
				for (var j = 0; j < maxSize; j++)
				{
					var @object = objectPool.Object;
					hashesOfExistentObjects.Add(@object.GetHashCode().ToString("x"));
					objects.Add(@object);
				}

				foreach (var obj in objects)
				{
					objectPool.ReturnObject(obj);
				}
			}

			Assert.AreEqual(maxSize, hashesOfExistentObjects.Count);
		}
	}
}
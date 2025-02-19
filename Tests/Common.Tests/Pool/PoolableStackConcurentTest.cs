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
using System.Threading;
using Epam.FixAntenna.NetCore.Common.Pool;
using Epam.FixAntenna.NetCore.Common.Pool.Provider;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Common.Pool
{
	/// <summary>
	/// Test checks concurrent releasing of recursive objects.
	/// Risk: recursive calls within synchronized blocks may lead to deadlock.
	/// </summary>
	[TestFixture]
	internal class PoolableStackConcurentTest
	{
		protected SynchronizedPoolableStack<ObjectA> PoolA;
		protected SynchronizedPoolableStack<ObjectB> PoolB;

		private class AbstractPoolableProviderAnonymousInnerClass : AbstractPoolableProvider<ObjectA>
		{
			private readonly PoolableStackConcurentTest _outerInstance;

			public AbstractPoolableProviderAnonymousInnerClass(PoolableStackConcurentTest outerInstance)
			{
				_outerInstance = outerInstance;
			}

			public override ObjectA Create()
			{
				return new ObjectA(_outerInstance);
			}

			public override void Activate(ObjectA @object)
			{
			}

			public override void Destroy(ObjectA @object)
			{
				@object.Release();
			}

			public override void Passivate(ObjectA @object)
			{
				@object.Release();
			}
		}

		private class AbstractPoolableProviderAnonymousInnerClass2 : AbstractPoolableProvider<ObjectB>
		{
			private readonly PoolableStackConcurentTest _outerInstance;

			public AbstractPoolableProviderAnonymousInnerClass2(PoolableStackConcurentTest outerInstance)
			{
				_outerInstance = outerInstance;
			}

			public override ObjectB Create()
			{
				return new ObjectB(_outerInstance);
			}

			public override void Activate(ObjectB @object)
			{
			}

			public override void Destroy(ObjectB @object)
			{
				@object.Release();
			}

			public override void Passivate(ObjectB @object)
			{
				@object.Release();
			}
		}

		protected class ObjectA
		{
			private readonly PoolableStackConcurentTest _outerInstance;

			internal ObjectB InnerB;

			public ObjectA(PoolableStackConcurentTest outerInstance)
			{
				_outerInstance = outerInstance;
			}

			public virtual void MakeInnerB()
			{
				InnerB = _outerInstance.PoolB.Object;
			}

			public virtual void Release()
			{
				if (InnerB != null)
				{
					var localB = InnerB;
					InnerB = null;
					_outerInstance.PoolB.ReturnObject(localB);
				}

				Pause();
			}
		}

		protected class ObjectB
		{
			private readonly PoolableStackConcurentTest _outerInstance;

			protected ObjectA InnerA;

			public ObjectB(PoolableStackConcurentTest outerInstance)
			{
				_outerInstance = outerInstance;
			}

			public virtual void MakeInnerA()
			{
				InnerA = _outerInstance.PoolA.Object;
			}

			public virtual void Release()
			{
				if (InnerA != null)
				{
					var localA = InnerA;
					InnerA = null;
					_outerInstance.PoolA.ReturnObject(localA);
				}

				Pause();
			}
		}

		public static void Pause()
		{
			try
			{
				Thread.Sleep(50);
			}
			catch (ThreadInterruptedException e)
			{
				Console.WriteLine(e.ToString());
				Console.Write(e.StackTrace);
			}
		}

		[Test]
		[Timeout(15 * 1000)]
		public virtual void TestRecursiveReturnObject()
		{
			PoolA = new SynchronizedPoolableStack<ObjectA>(1, 5, new AbstractPoolableProviderAnonymousInnerClass(this));

			PoolB = new SynchronizedPoolableStack<ObjectB>(1, 5,
				new AbstractPoolableProviderAnonymousInnerClass2(this));

			var threadCount = 2;
			var threads = new Thread[threadCount];
			using (var countDownLatch = new CountdownEvent(threadCount))
			{
				for (var i = 0; i < threads.Length; i++)
				{
					threads[i] = new Thread(() =>
					{
						for (var j = 0; j < 5; j++)
						{
							var objectA = PoolA.Object;
							objectA.MakeInnerB();
							objectA.InnerB.MakeInnerA();
							PoolA.ReturnObject(objectA);
						}

						countDownLatch.Signal();
					});
					threads[i].Start();
				}

				countDownLatch.Wait();
			}
		}
	}
}
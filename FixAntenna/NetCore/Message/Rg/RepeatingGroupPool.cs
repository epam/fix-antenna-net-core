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

namespace Epam.FixAntenna.NetCore.Message.Rg
{
	public class RepeatingGroupPool
	{
		private static readonly IPool<RepeatingGroup> GroupPool;
		private static readonly IPool<RepeatingGroup.Entry> EntryPool;

		static RepeatingGroupPool()
		{
			GroupPool = PoolFactory.GetConcurrentBucketsPool(10, 200, 1000,
				new AbstractPoolableProviderAnonymousInnerClass());

			EntryPool = PoolFactory.GetConcurrentBucketsPool(10, 200, 1000,
				new AbstractPoolableProviderAnonymousInnerClass2());
		}

		public static RepeatingGroup RepeatingGroup => GroupPool.Object;

		public static RepeatingGroup.Entry Entry => EntryPool.Object;

		public static void ReturnObj(RepeatingGroup.Entry entry)
		{
			EntryPool.ReturnObject(entry);
		}

		public static void ReturnObj(RepeatingGroup group)
		{
			GroupPool.ReturnObject(group);
		}

		public static int EntryObjectsCreated => EntryPool.ObjectsCreated;

		public static int GroupObjectsCreated => GroupPool.ObjectsCreated;

		private class AbstractPoolableProviderAnonymousInnerClass : AbstractPoolableProvider<RepeatingGroup>
		{
			public override RepeatingGroup Create()
			{
				return new RepeatingGroup();
			}

			public override void Destroy(RepeatingGroup @object)
			{
				@object.Clear();
			}

			public override void Activate(RepeatingGroup @object)
			{
			}

			public override void Passivate(RepeatingGroup @object)
			{
				@object.Clear();
			}
		}

		private class AbstractPoolableProviderAnonymousInnerClass2 : AbstractPoolableProvider<RepeatingGroup.Entry>
		{
			public override RepeatingGroup.Entry Create()
			{
				return new RepeatingGroup.Entry();
			}

			public override void Destroy(RepeatingGroup.Entry @object)
			{
				@object.Clear();
			}

			public override void Activate(RepeatingGroup.Entry @object)
			{
			}

			public override void Passivate(RepeatingGroup.Entry @object)
			{
				@object.Clear();
			}
		}
	}
}
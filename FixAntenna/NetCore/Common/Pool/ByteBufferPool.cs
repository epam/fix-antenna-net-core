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
using Epam.FixAntenna.NetCore.Common.Pool.Provider;
using Epam.FixAntenna.NetCore.Common.Utils;

namespace Epam.FixAntenna.NetCore.Common.Pool
{
	internal class ByteBufferPool
	{
		public static int DefaultLength = 1024;

		private readonly IPool<ByteBuffer> _pool;

		private ByteBufferPool()
		{
			_pool = PoolFactory.GetConcurrentBucketsPool(100, 200, 1000,
				new AbstractPoolableProviderImpl());
		}

		public ByteBuffer DemandAndClean(int capacity)
		{
			var bb = Demand(capacity);
			bb.PutInAll(0, bb.Limit(), 0);
			return bb;
		}

		public ByteBuffer Demand(int capacity)
		{
			if (capacity < 0)
			{
				throw new ArgumentException();
			}

			var bb = _pool.Object;
			bb.Position = 0;
			bb.Limit(capacity);

			return bb;
		}

		public void Release(ByteBuffer buffer)
		{
			if (buffer == null)
			{
				return;
			}

			_pool.ReturnObject(buffer);
		}

		public static ByteBufferPool Instance { get; } = ByteBufferPoolHolder.Pool;

		private class ByteBufferPoolHolder
		{
			internal static readonly ByteBufferPool Pool = new ByteBufferPool();
		}

		private class AbstractPoolableProviderImpl : AbstractPoolableProvider<ByteBuffer>
		{
			public override ByteBuffer Create()
			{
				return new ByteBuffer(DefaultLength);
			}

			public override void Activate(ByteBuffer @object)
			{
			}
		}
	}
}
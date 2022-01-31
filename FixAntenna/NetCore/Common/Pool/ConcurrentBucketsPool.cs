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
using Epam.FixAntenna.NetCore.Common.Pool.Provider;

namespace Epam.FixAntenna.NetCore.Common.Pool
{
	internal class ConcurrentBucketsPool<T> : IPool<T> where T : class
	{
		private readonly SynchronizedPoolableStack<T>[] _buckets;
		private readonly int _numberOfBuckets;

		private int _getCounter;
		private int _returnCounter = 1; // +1 to return object into bucket that can be used to get next object

		protected internal ConcurrentBucketsPool(int numberOfBuckets, int initBucketSize, int maxBucketSize,
			IPoolableProvider<T> poolableProvider)
		{
			_numberOfBuckets = numberOfBuckets;

			_buckets = new SynchronizedPoolableStack<T>[_numberOfBuckets];
			for (var i = 0; i < _numberOfBuckets; i++)
			{
				_buckets[i] = new SynchronizedPoolableStack<T>(initBucketSize, maxBucketSize, poolableProvider);
			}
		}

		public virtual T Object
		{
			get
			{
				var index = Math.Abs(Interlocked.Increment(ref _getCounter) % _numberOfBuckets);
				return _buckets[index].Object;
			}
		}

		public virtual void ReturnObject(T @object)
		{
			var index = Math.Abs(Interlocked.Increment(ref _returnCounter) % _numberOfBuckets);
			_buckets[index].ReturnObject(@object);
		}

		public virtual int ObjectsCreated
		{
			get
			{
				var counter = 0;
				for (var i = 0; i < _numberOfBuckets; i++)
				{
					counter += _buckets[i].ObjectsCreated;
				}

				return counter;
			}
		}

		public virtual void Clean()
		{
			for (var i = 0; i < _numberOfBuckets; i++)
			{
				_buckets[i].Clean();
			}
		}

		public override string ToString()
		{
			var ret = "";
			for (var i = 0; i < _numberOfBuckets; i++)
			{
				ret += "bucket: " + i + "; " + _buckets[i].GetInfo() + "\n";
			}

			return ret;
		}
	}
}
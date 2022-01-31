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
using Epam.FixAntenna.NetCore.Common.Pool;
using Epam.FixAntenna.NetCore.Common.Pool.Provider;

namespace Epam.FixAntenna.NetCore.Message
{
	internal class RepeatingGroupStorageIntArrayPool
	{
		private const int InitialPoolSize = 20;
		private static readonly IPool<int[]>[] IntArrayPool = new IPool<int[]>[InitialPoolSize];
		private static readonly IPool<int[][]>[] TwoDimArrayPool = new IPool<int[][]>[InitialPoolSize];

		static RepeatingGroupStorageIntArrayPool()
		{
			IntArrayPoolableProvider provider;
			TwoDimIntArrayPoolableProvider twoDimProvider;

			var arraySize = 2;
			for (var i = 0; i < InitialPoolSize; i++)
			{
				provider = new IntArrayPoolableProvider(arraySize);
				twoDimProvider = new TwoDimIntArrayPoolableProvider(arraySize);
				IntArrayPool[i] = PoolFactory.GetConcurrentBucketsPool(10, 100, 0, provider);
				TwoDimArrayPool[i] = PoolFactory.GetConcurrentBucketsPool(10, 100, 0, twoDimProvider);
				arraySize <<= 1;
			}
		}

		public static int[] GetIntArrayFromPool(int size)
		{
			return IntArrayPool[GetIndex(size)].Object;
		}

		public static int[][] GetTwoDimIntArrayFromPool(int size)
		{
			return TwoDimArrayPool[GetIndex(size)].Object;
		}

		public static void ReturnObj(int[] arr)
		{
			IntArrayPool[GetIndex(arr.Length)].ReturnObject(arr);
		}

		public static void ReturnObj(int[][] arr)
		{
			TwoDimArrayPool[GetIndex(arr.Length)].ReturnObject(arr);
		}

		public static int GetIntArrayObjectsCreated(int index)
		{
			return IntArrayPool[index].ObjectsCreated;
		}

		public static int GetTwoDimArrayObjectsCreated(int index)
		{
			return TwoDimArrayPool[index].ObjectsCreated;
		}

		private static int GetIndex(int size)
		{
			if (size <= 0)
			{
				throw new ArgumentException("Size of array should be greater than 0");
			}

			var index = 0;
			while (size > 2)
			{
				size >>= 1;
				index++;
			}

			return index;
		}

		internal class TwoDimIntArrayPoolableProvider : AbstractPoolableProvider<int[][]>
		{
			internal readonly int ArraySize;

			internal TwoDimIntArrayPoolableProvider(int arraySize)
			{
				ArraySize = arraySize;
			}

			public override void Destroy(int[][] arr)
			{
				for (var i = 0; i < arr.Length; i++)
				{
					arr[i] = null;
				}
			}

			public override int[][] Create()
			{
				return new int[ArraySize][];
			}

			public override void Activate(int[][] @object)
			{
			}

			public override void Passivate(int[][] @object)
			{
				for (var i = 0; i < @object.Length; i++)
				{
					@object[i] = null;
				}
			}
		}

		internal class IntArrayPoolableProvider : AbstractPoolableProvider<int[]>
		{
			internal readonly int ArraySize;

			internal IntArrayPoolableProvider(int arraySize)
			{
				ArraySize = arraySize;
			}

			public override void Destroy(int[] arr)
			{
				for (var i = 0; i < arr.Length; i++)
				{
					arr[i] = 0;
				}
			}

			public override int[] Create()
			{
				return new int[ArraySize];
			}

			public override void Activate(int[] @object)
			{
			}

			public override void Passivate(int[] @object)
			{
				for (var i = 0; i < @object.Length; i++)
				{
					@object[i] = 0;
				}
			}
		}
	}
}
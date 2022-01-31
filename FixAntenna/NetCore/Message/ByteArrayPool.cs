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
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Common.Pool;
using Epam.FixAntenna.NetCore.Common.Pool.Provider;

namespace Epam.FixAntenna.NetCore.Message
{
	internal class ByteArrayPool
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(ByteArrayPool));

		internal static IPool<byte[]>[] Content = new IPool<byte[]>[10000];

		static ByteArrayPool()
		{
			var elems = Content.Length;
			for (var i = 0; i < elems; i++)
			{
				var provider = new ByteArrayPoolableProvider();
				provider.SetArrSize(i);
				Content[i] = PoolFactory.GetConcurrentBucketsPool(10, 200, 1000, provider);
			}
		}

		public static byte[] GetByteArrayFromPool(int size)
		{
			byte[] arr = null;
			try
			{
				arr = Content[size].Object;
			}
			catch (Exception e)
			{
				if (Log.IsDebugEnabled)
				{
					Log.Warn("Can't get new array from pool: " + e.Message, e);
				}
				else
				{
					Log.Warn("Can't get new array from pool: " + e.Message);
				}
			}

			return arr;
		}

		public static void ReturnObj(byte[] arr)
		{
			Content[arr.Length].ReturnObject(arr);
		}

		internal class ByteArrayPoolableProvider : AbstractPoolableProvider<byte[]>
		{
			public int MArrSize;

			public virtual void SetArrSize(int arrSize)
			{
				MArrSize = arrSize;
			}

			public override byte[] Create()
			{
				return new byte[MArrSize];
			}

			public override void Activate(byte[] t)
			{
			}
		}
	}
}
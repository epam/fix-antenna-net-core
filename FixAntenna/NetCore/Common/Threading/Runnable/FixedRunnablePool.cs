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

namespace Epam.FixAntenna.NetCore.Common.Threading.Runnable
{
	/// <summary>
	/// Pool have fixed size.<p/>
	/// Create new objects if pool is empty.<p/>
	/// Skip objects if the pool is full. Object should be collected by GC in this case.
	/// </summary>
	internal sealed class FixedRunnablePool<T> : IRunnablePool<T> where T : IRunnableObject
	{
		private readonly int _maxSize;
		private readonly IRunnableFactory<T> _objectFactory;
		private readonly object[] _pool;
		private int _indexNext;
		private int _minSize;

		public FixedRunnablePool(int minSize, int maxSize, IRunnableFactory<T> objectFactory)
		{
			if (minSize < 0 || maxSize < 1 || minSize > maxSize)
			{
				throw new ArgumentException("'minSize' and 'maxSize' must be more than 0. 'maxSize'>='minSize'");
			}

			_objectFactory = objectFactory ?? throw new NullReferenceException("objectFactory can't be null");
			_minSize = minSize;
			_maxSize = maxSize;
			_indexNext = minSize - 1;
			_pool = new object[maxSize];
			for (var i = 0; i < minSize; i++)
			{
				_pool[i] = CreateNew();
			}
		}

		/// <summary>
		/// Get object from pool. Create new object if pool is empty.
		/// @return
		/// </summary>
		public T Get()
		{
			lock (_pool)
			{
				if (_indexNext > 0)
				{
					return (T)_pool[_indexNext--];
				}

				// pool is empty. create new Object
				return CreateNew();
			}
		}

		/// <summary>
		/// Try return object to the pool. Skip object if the pool is full. </summary>
		/// <param name="ob"> </param>
		public void Release(T ob)
		{
			lock (_pool)
			{
				if (_indexNext + 1 >= _maxSize)
				{
					// pool is already full.
				}
				else
				{
					_pool[++_indexNext] = ob;
				}
			}
		}

		private T CreateNew()
		{
			return _objectFactory.Create(this);
		}
	}
}
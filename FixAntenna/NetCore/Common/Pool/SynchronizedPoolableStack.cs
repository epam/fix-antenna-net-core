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

namespace Epam.FixAntenna.NetCore.Common.Pool
{
	internal class SynchronizedPoolableStack<T> : IPool<T> where T : class
	{
		private readonly int _initSize;
		private readonly int _maxSize;
		private readonly IPoolableProvider<T> _poolableProvider;
		private int _created;
		private int _index;

		private T[] _items;
		private readonly object _sync = new object();

		public SynchronizedPoolableStack(int initSize, int maxSize, IPoolableProvider<T> poolableProvider)
		{
			_initSize = initSize;
			_maxSize = maxSize;
			_poolableProvider = poolableProvider;
			_items = new T[_initSize];
			_index = -1;
			_created = 0;
		}

		public T Object
		{
			get
			{
				T @object;
				lock (_sync)
				{
					if (_index >= 0)
					{
						@object = _items[_index];
						_items[_index--] = default;
						_poolableProvider.Activate(@object);
					}
					else
					{
						@object = _poolableProvider.Create();
						_created++;
					}
				}

				return @object;
			}
		}

		public virtual void ReturnObject(T @object)
		{
			if (@object == null)
			{
				return;
			}

			if (_poolableProvider.Validate(@object))
			{
				_poolableProvider.Passivate(@object);

				var toDestroy = false;
				lock (_sync)
				{
					if (_index == _items.Length - 1)
					{
						if (_maxSize == 0 || _items.Length < _maxSize)
						{
							var newLength = _maxSize == 0 ? _items.Length * 2 : Math.Min(_items.Length * 2, _maxSize);
							Array.Resize(ref _items, newLength);
							_items[++_index] = @object;
						}
						else
						{
							toDestroy = true;
						}
					}
					else
					{
						_items[++_index] = @object;
					}
				}

				if (toDestroy)
				{
					_poolableProvider.Destroy(@object);
				}
			}
			else
			{
				_poolableProvider.Destroy(@object);
			}
		}

		public virtual int ObjectsCreated => _created;

		public virtual void Clean()
		{
			lock (_sync)
			{
				for (var i = 0; i < _items.Length; i++)
				{
					_items[i] = default;
				}

				_items = new T[_initSize];
				_index = -1;
				_created = 0;
			}
		}

		public virtual string GetInfo()
		{
			return $"size: {_items.Length:D}; _index: {_index:D}; _created: {_created:D}";
		}
	}
}
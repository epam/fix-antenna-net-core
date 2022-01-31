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
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Epam.FixAntenna.Common.Pool
{
	public class SynchronizedStack<T> : ICollection<T> where T : class
	{
		private readonly int _initSize;
		private int _created;
		private int _index;
		private T[] _items;

		public SynchronizedStack(int initSize)
		{
			_initSize = initSize;
			_items = new T[_initSize];
			_index = -1;
			_created = 0;
		}

		public virtual int Count
		{
			get
			{
				lock (this)
				{
					return _index + 1;
				}
			}
		}

		public virtual void Add(T @object)
		{
			if (@object == null)
			{
				return;
			}

			lock (this)
			{
				if (_index > 100)
				{
					try
					{
						Monitor.Wait(this, TimeSpan.FromMilliseconds(0 + 1 / 1000d));
					}
					catch (Exception)
					{
						//ignore
					}
				}

				if (_index == _items.Length - 1)
				{
					var oldLength = _items.Length;
					Array.Resize(ref _items, oldLength * 2);
				}

				_items[++_index] = @object;
			}
		}

		public virtual void Clear()
		{
			lock (this)
			{
				for (var i = 0; i < _items.Length; i++)
				{
					_items[i] = null;
				}

				_items = new T[_initSize];
				_index = -1;
				_created = 0;
			}
		}

		public virtual bool Contains(T o)
		{
			throw new NotSupportedException("Not supported yet.");
		}

		public virtual IEnumerator<T> GetEnumerator()
		{
			throw new NotSupportedException("Not supported yet.");
		}

		public virtual bool Remove(T o)
		{
			throw new NotSupportedException("Not supported yet.");
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			throw new NotSupportedException();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new NotSupportedException();
		}

		bool ICollection<T>.IsReadOnly => false;

		public virtual bool IsEmpty => Count == 0;

		public virtual T Remove()
		{
			lock (this)
			{
				T @object = null;
				if (_index >= 0)
				{
					@object = _items[_index];
					_items[_index--] = null;
				}

				return @object;
			}
		}

		public override string ToString()
		{
			return string.Format("size: {0:D}; index: {1:D}; created: {2:D}", _items.Length, _index, _created);
		}
	}
}
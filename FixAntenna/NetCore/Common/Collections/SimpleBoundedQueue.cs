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

namespace Epam.FixAntenna.NetCore.Common.Collections
{
	internal class SimpleBoundedQueue<TE> : IBoundedQueue<TE> where TE : class
	{
		private readonly TE[] _elements;
		private readonly int _maxElements;

		private int _end;

		private bool _full;

		private int _start;

		public SimpleBoundedQueue() : this(32)
		{
		}

		public SimpleBoundedQueue(in int size) : this(size, false)
		{
		}

		public SimpleBoundedQueue(in bool circular) : this(32, circular)
		{
		}

		public SimpleBoundedQueue(in int size, in bool circular)
		{
			Circular = circular;
			if (size <= 0)
			{
				throw new ArgumentException("The size must be greater than 0");
			}

			_elements = new TE[size];
			_maxElements = size;
		}

		public virtual bool Circular { get; }

		public virtual bool Add(TE e)
		{
			if (null == e)
			{
				throw new ArgumentNullException(nameof(e), "Attempted to add null object to queue");
			}

			if (!Circular && _full)
			{
				throw new InvalidOperationException("Queue is full");
			}

			return Insert(e);
		}

		public virtual bool Offer(TE e)
		{
			if (null == e)
			{
				throw new ArgumentNullException(nameof(e), "Attempted to add null object to queue");
			}

			if (!Circular && _full)
			{
				return false;
			}

			return Insert(e);
		}

		public virtual TE Remove()
		{
			if (IsEmpty)
			{
				throw new InvalidOperationException("Queue is empty");
			}

			return Extract();
		}

		public virtual TE Poll()
		{
			if (IsEmpty)
			{
				return null;
			}

			return Extract();
		}

		public virtual TE Element()
		{
			if (IsEmpty)
			{
				throw new InvalidOperationException("Queue is empty");
			}

			return Peek();
		}

		public virtual TE Peek()
		{
			if (IsEmpty)
			{
				return null;
			}

			return _elements[_start];
		}

		public virtual bool IsEmpty
		{
			get { return Size == 0; }
		}

		public virtual bool IsFull
		{
			get { return !Circular && Size == _maxElements; }
		}

		public virtual int Size
		{
			get
			{
				var size = 0;

				if (_end < _start)
				{
					size = _maxElements - _start + _end;
				}
				else if (_end == _start)
				{
					size = _full ? _maxElements : 0;
				}
				else
				{
					size = _end - _start;
				}

				return size;
			}
		}

		public virtual int MaxSize
		{
			get { return _maxElements; }
		}

		private bool Insert(TE e)
		{
			if (Circular && Size == _maxElements)
			{
				Remove();
			}

			_elements[_end++] = e;

			if (_end >= _maxElements)
			{
				_end = 0;
			}

			if (_end == _start)
			{
				_full = true;
			}

			return true;
		}

		private TE Extract()
		{
			var e = _elements[_start];
			if (e != null)
			{
				_elements[_start++] = null;

				if (_start >= _maxElements)
				{
					_start = 0;
				}

				_full = false;
			}

			return e;
		}
	}
}
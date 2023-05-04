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
using System.Collections.Generic;
using System.Threading;

namespace Epam.FixAntenna.NetCore.Common.Threading.Queue
{
	internal class SynchronizeBlockingQueue<TE> : ISimpleBlockingQueue<TE>
	{
		/// <summary>
		/// The queued items
		/// </summary>
		private readonly TE[] _items;

		/// <summary>
		/// Number of items in the queue
		/// </summary>
		private int _count;

		/// <summary>
		/// items index for next put, offer, or add.
		/// </summary>
		private int _putIndex;

		/// <summary>
		/// items index for next take, poll or remove
		/// </summary>
		private int _takeIndex;

		private readonly object _sync = new object();

		public SynchronizeBlockingQueue(int limit)
		{
			_items = new TE[limit];
		}

		public virtual void Put(TE item)
		{
			lock (_sync)
			{
				if (EqualityComparer<TE>.Default.Equals(item, default))
				{
					throw new NullReferenceException();
				}

				try
				{
					while (_count == _items.Length)
					{
						Monitor.Wait(_sync);
					}
				}
				catch (ThreadInterruptedException)
				{
					Monitor.PulseAll(_sync);
					throw;
				}

				Insert(item);
				Monitor.PulseAll(_sync);
			}
		}

		public virtual TE Take()
		{
			lock (_sync)
			{
				try
				{
					while (_count == 0)
					{
						Monitor.Wait(_sync);
					}
				}
				catch (ThreadAbortException)
				{
					Monitor.PulseAll(_sync);
					throw;
				}
				catch (ThreadInterruptedException)
				{
					Monitor.PulseAll(_sync);
					throw;
				}

				var item = ExtractItem();
				Monitor.PulseAll(_sync);
				return item;
			}
		}

		// this doc comment is overridden to remove the reference to collections
		// greater in size than Integer.MAX_VALUE

		/// <summary>
		/// Returns the number of elements in this queue.
		/// </summary>
		/// <value> the number of elements in this queue </value>
		public virtual int Size
		{
			get
			{
				lock (_sync)
				{
					return _count;
				}
			}
		}

		public virtual bool IsEmpty
		{
			get { return Size == 0; }
		}

		/// <summary>
		/// Circularly increment i.
		/// </summary>
		internal int Inc(int i)
		{
			return ++i == _items.Length ? 0 : i;
		}

		/// <summary>
		/// Inserts element at current put position
		/// </summary>
		private void Insert(TE item)
		{
			_items[_putIndex] = item;
			_putIndex = Inc(_putIndex);
			++_count;
		}

		/// <summary>
		/// Extracts element at current take position.
		/// </summary>
		private TE ExtractItem()
		{
			var items = _items;
			var item = items[_takeIndex];
			items[_takeIndex] = default;
			_takeIndex = Inc(_takeIndex);
			--_count;
			return item;
		}
	}
}
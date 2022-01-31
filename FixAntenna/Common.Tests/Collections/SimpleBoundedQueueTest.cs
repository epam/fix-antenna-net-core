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
using Epam.FixAntenna.NetCore.Common.Collections;
using NUnit.Framework;

namespace Epam.FixAntenna.Common.Collections
{
	[TestFixture]
	public class SimpleBoundedQueueTest
	{
		[SetUp]
		public void Init()
		{
			_boundedQueue = new SimpleBoundedQueue<object>(Limit, false);
			_circularQueue = new SimpleBoundedQueue<object>(Limit, true);
			Assert.True(_boundedQueue.IsEmpty);
			Assert.True(_circularQueue.IsEmpty);
		}

		private IBoundedQueue<object> _boundedQueue;
		private IBoundedQueue<object> _circularQueue;
		private const int Limit = 3;

		[Test]
		public void TestAddToFullCircularQueue()
		{
			for (var i = 0; i < Limit; i++)
			{
				_circularQueue.Add(i);
			}

			Assert.True(_circularQueue.Add(Limit + 1));
		}

		[Test]
		public void TestAddToFullQueue()
		{
			for (var i = 0; i < Limit; i++)
			{
				_boundedQueue.Add(i);
			}

			Assert.Throws(Is.TypeOf<InvalidOperationException>()
				.And.Message.EqualTo("Queue is full"), () => { _boundedQueue.Add(0); });
		}

		[Test]
		public void TestElementFromEmptyQueue()
		{
			Assert.True(_boundedQueue.IsEmpty);
			Assert.Throws(Is.TypeOf<InvalidOperationException>()
				.And.Message.EqualTo("Queue is empty"), () => { _boundedQueue.Element(); });
		}

		[Test]
		public void TestIsEmpty()
		{
			Assert.AreEqual(0, _boundedQueue.Size);
			Assert.True(_boundedQueue.IsEmpty);
			Assert.AreEqual(0, _circularQueue.Size);
			Assert.True(_circularQueue.IsEmpty);
		}

		[Test]
		public void TestIsFull()
		{
			for (var i = 0; i < Limit; i++)
			{
				_boundedQueue.Add(i);
				_circularQueue.Add(i);
			}

			Assert.AreEqual(Limit, _boundedQueue.Size);
			Assert.True(_boundedQueue.IsFull);
			Assert.AreEqual(Limit, _circularQueue.Size);
			Assert.False(_circularQueue.IsFull);
		}

		[Test]
		public void TestOfferOrAdd()
		{
			_boundedQueue.Offer(1);
			Assert.AreEqual(1, _boundedQueue.Size);
			_boundedQueue.Add(2);
			Assert.AreEqual(2, _boundedQueue.Size);
		}

		[Test]
		public void TestOfferToFullQueue()
		{
			for (var i = 0; i < Limit; i++)
			{
				_boundedQueue.Offer(i);
				_circularQueue.Offer(i);
			}

			Assert.False(_boundedQueue.Offer(Limit + 1));
			Assert.True(_circularQueue.Offer(Limit + 1));
		}

		[Test]
		public void TestPeekFromEmptyQueue()
		{
			Assert.True(_boundedQueue.IsEmpty);
			Assert.Null(_boundedQueue.Peek());
		}

		[Test]
		public void TestPeekOrElement()
		{
			for (var i = 0; i < Limit; i++)
			{
				_boundedQueue.Add(i);
			}

			_boundedQueue.Peek();
			Assert.AreEqual(Limit, _boundedQueue.Size);
			_boundedQueue.Element();
			Assert.AreEqual(Limit, _boundedQueue.Size);
		}

		[Test]
		public void TestPollFromEmptyQueue()
		{
			Assert.True(_boundedQueue.IsEmpty);
			Assert.IsNull(_boundedQueue.Poll());
		}

		[Test]
		public void TestPollOrRemove()
		{
			for (var i = 0; i < Limit; i++)
			{
				_boundedQueue.Add(i);
			}

			_boundedQueue.Poll();
			Assert.AreEqual(Limit - 1, _boundedQueue.Size);
			_boundedQueue.Remove();
			Assert.AreEqual(Limit - 2, _boundedQueue.Size);
		}

		[Test]
		public void TestRemoveFromEmptyQueue()
		{
			Assert.True(_boundedQueue.IsEmpty);
			Assert.Throws(Is.TypeOf<InvalidOperationException>()
				.And.Message.EqualTo("Queue is empty"), () => { _boundedQueue.Remove(); });
		}

		[Test]
		public void TestSize()
		{
			for (var i = 0; i < Limit; i++)
			{
				_boundedQueue.Add(i);
				Assert.AreEqual(i + 1, _boundedQueue.Size);
			}
		}
	}
}
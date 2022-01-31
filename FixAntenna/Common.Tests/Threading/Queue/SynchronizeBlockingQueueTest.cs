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
using System.Text;
using System.Threading;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Threading.Queue;
using NUnit.Framework;

namespace Epam.FixAntenna.Common.Threading.Queue
{
	[TestFixture]
	public class SynchronizeBlockingQueueTest
	{
		[SetUp]
		public virtual void Init()
		{
			_queue = new SynchronizeBlockingQueue<string>(Limit);
			Assert.True(_queue.IsEmpty);
		}

		private SynchronizeBlockingQueue<string> _queue;
		private const int Limit = 3;

		private class TestThreadAnonymousInnerClass : TestThread
		{
			private readonly SynchronizeBlockingQueueTest _outerInstance;

			public TestThreadAnonymousInnerClass(SynchronizeBlockingQueueTest outerInstance, int delay, int itemsNum) :
				base(outerInstance, "TestPut", delay, itemsNum)
			{
				_outerInstance = outerInstance;
			}

			internal override void Action()
			{
				_outerInstance._queue.Take();
			}
		}

		private class TestThreadAnonymousInnerClass2 : TestThread
		{
			private readonly SynchronizeBlockingQueueTest _outerInstance;

			private int _i;

			public TestThreadAnonymousInnerClass2(SynchronizeBlockingQueueTest outerInstance, int delay,
				int actionNumber) : base(outerInstance, "TestPut", delay, actionNumber)
			{
				_outerInstance = outerInstance;
				_i = 0;
			}

			internal override void Action()
			{
				_outerInstance._queue.Put(Convert.ToString(_i++));
			}
		}

		private class TestThreadAnonymousInnerClass3 : TestThread
		{
			private readonly SynchronizeBlockingQueueTest _outerInstance;

			private int _i;

			public TestThreadAnonymousInnerClass3(SynchronizeBlockingQueueTest outerInstance, int itemsNum) : base(
				outerInstance, "TestTake", 0, itemsNum)
			{
				_outerInstance = outerInstance;
				_i = 0;
			}

			internal override void Action()
			{
				var str = _outerInstance._queue.Take();
				Assert.AreEqual(Convert.ToString(_i), str);
				_i++;
			}
		}

		private class TestThreadAnonymousInnerClass4 : TestThread
		{
			private readonly SynchronizeBlockingQueueTest _outerInstance;

			private int _i;

			public TestThreadAnonymousInnerClass4(SynchronizeBlockingQueueTest outerInstance, int itemsNum) : base(
				outerInstance, "TestPut", 0, itemsNum)
			{
				_outerInstance = outerInstance;
				_i = 0;
			}

			internal override void Action()
			{
				_outerInstance._queue.Put(Convert.ToString(_i++));
			}
		}

		private bool TimeIsCome(long time)
		{
			return DateTimeHelper.CurrentMilliseconds >= time;
		}

		protected abstract class TestThread
		{
			private readonly SynchronizeBlockingQueueTest _outerInstance;
			internal readonly StringBuilder Err = new StringBuilder();
			internal int ActionNumber;

			internal int Delay;
			public Thread Thread;

			internal TestThread(SynchronizeBlockingQueueTest outerInstance, string name, int delay, int actionNumber)
			{
				Thread = new Thread(Run);
				_outerInstance = outerInstance;
				if (!string.IsNullOrEmpty(name))
				{
					Thread.Name = name;
				}

				Delay = delay;
				ActionNumber = actionNumber;
			}

			public void Run()
			{
				try
				{
					Thread.Sleep(Delay);
				}
				catch (ThreadInterruptedException e)
				{
					lock (Err)
					{
						Err.Append(e.Message).Append(". ");
					}
				}

				for (var i = 0; i < ActionNumber; i++)
				{
					try
					{
						Action();
						Count++;
					}
					catch (Exception e)
					{
						lock (Err)
						{
							Err.Append("Action #").Append(i).Append(". ").Append(e.Message).Append(". ");
						}
					}
				}
			}

			internal abstract void Action();

			internal virtual string GetErr()
			{
				lock (Err)
				{
					return Err.ToString();
				}
			}

			internal int Count { get; private set; }
		}

		[Test]
		[Timeout(15 * 1000)]
		public virtual void TestPutLimitLock()
		{
			var itemsNum = Limit + 1;
			const int waitTime = 2000;
			TestThread takeThread = new TestThreadAnonymousInnerClass(this, waitTime, itemsNum);
			var timeBeforePut = DateTimeHelper.CurrentMilliseconds;
			var timeControlPoint = timeBeforePut + waitTime - waitTime / 10;
			// this thread sleep on waitTime millisecond before start take from queue
			takeThread.Thread.Start();
			// fill queue without lock
			for (var i = 0; i < Limit; i++)
			{
				_queue.Put(Convert.ToString(i));
			}

			Assert.IsFalse(TimeIsCome(timeControlPoint),
				"Queue fill with lock, but should without. " + takeThread.GetErr());
			// in this point test must be wait
			_queue.Put(Convert.ToString(Limit + 1));
			Assert.IsTrue(TimeIsCome(timeControlPoint),
				"Queue fill without lock, but should with. " + takeThread.GetErr());
			takeThread.Thread.Join();
			Assert.IsTrue(_queue.IsEmpty, "In this point queue must be empty");
			Assert.AreEqual(itemsNum, takeThread.Count, "Not all items were taken.");
			var errStr = takeThread.GetErr();
			Assert.IsTrue(errStr.Length == 0, "Some errors in TakeThread: " + errStr);
		}

		[Test]
		public virtual void TestPutTake()
		{
			_queue.Put("1");
			_queue.Put("2");
			_queue.Put("3");
			Assert.AreEqual(3, _queue.Size);

			Assert.AreEqual("1", _queue.Take());
			Assert.AreEqual("2", _queue.Take());
			Assert.AreEqual("3", _queue.Take());
			Assert.IsTrue(_queue.IsEmpty, "In this point queue must be empty");
		}

		[Test]
		public virtual void TestSize()
		{
			_queue.Put("1");
			Assert.AreEqual(1, _queue.Size);
			_queue.Put("2");
			_queue.Put("3");
			Assert.AreEqual(3, _queue.Size);
		}

		[Test]
		[Timeout(240 * 1000)]
		public virtual void TestStressTest()
		{
			const int itemsNum = 5 * 1000 * 1000;
			TestThread takeThread = new TestThreadAnonymousInnerClass3(this, itemsNum);
			TestThread putThread = new TestThreadAnonymousInnerClass4(this, itemsNum);
			takeThread.Thread.Start();
			putThread.Thread.Start();
			takeThread.Thread.Join();
			putThread.Thread.Join();
			Assert.IsTrue(_queue.IsEmpty, "In this point queue must be empty");
			Assert.AreEqual(itemsNum, putThread.Count, "Not all items were put.");
			Assert.AreEqual(itemsNum, takeThread.Count, "Not all items were taken.");
			var errStrTake = takeThread.GetErr();
			Assert.IsTrue(errStrTake.Length == 0, "Some errors in TakeThread: " + errStrTake);
			var errStrPut = takeThread.GetErr();
			Assert.IsTrue(errStrPut.Length == 0, "Some errors in TestPut: " + errStrPut);
		}

		[Test]
		[Timeout(15 * 1000)]
		public virtual void TestTakeEmptyLock()
		{
			const int itemsNum = 3;
			const int waitTime = 2000;
			TestThread putThread = new TestThreadAnonymousInnerClass2(this, waitTime, itemsNum);
			var timeBeforeTake = DateTimeHelper.CurrentMilliseconds;
			var timeControlPoint = timeBeforeTake + waitTime - waitTime / 10;

			// this thread sleep on waitTime millisecond before start put to queue
			putThread.Thread.Start();
			// fill queue without lock
			for (var i = 0; i < itemsNum; i++)
			{
				_queue.Take();
			}

			Assert.IsTrue(TimeIsCome(timeControlPoint),
				"Take operation from queue was without lock, but should with. " + putThread.GetErr());
			putThread.Thread.Join();
			Assert.IsTrue(_queue.IsEmpty, "In this point queue must be empty");
			Assert.AreEqual(itemsNum, putThread.Count, "Not all items were put.");
			var errStr = putThread.GetErr();
			Assert.IsTrue(errStr.Length == 0, "Some errors in TakeThread: " + errStr);
		}
	}
}
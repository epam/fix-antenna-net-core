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

using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.FixEngine.Storage.Queue;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Storage.Queue
{
	internal class InMemoryQueueTest
	{
		private InMemoryQueue<FixMessageWithType> _queue;

		[SetUp]
		public virtual void SetUp()
		{
			_queue = new InMemoryQueue<FixMessageWithType>();
			_queue.Initialize();
		}

		[TearDown]
		public virtual void TearDown()
		{
			_queue.Shutdown();
		}

		[Test]
		public virtual void EmptyQueue()
		{
			var array = _queue.ToArray();
			ClassicAssert.IsNotNull(array);
			ClassicAssert.AreEqual(0, array.Length);
		}

		[Test]
		public virtual void TestSessionOnlyMessages()
		{
			var message = new FixMessage();
			message.Set(58, "test".AsByteArray());
			_queue.AddOutOfTurn(FixMessageWithTypePoolFactory.GetFixMessageWithTypeFromPool(message, "1"));
			_queue.AddOutOfTurn(FixMessageWithTypePoolFactory.GetFixMessageWithTypeFromPool(message, "2"));
			_queue.AddOutOfTurn(FixMessageWithTypePoolFactory.GetFixMessageWithTypeFromPool(message, "3"));

			var array = _queue.ToArray();
			ClassicAssert.IsNotNull(array);
			ClassicAssert.AreEqual(3, array.Length);
			ClassicAssert.IsTrue(array[0] is FixMessageWithType);
			ClassicAssert.IsTrue(array[1] is FixMessageWithType);
			ClassicAssert.IsTrue(array[2] is FixMessageWithType);
			ClassicAssert.IsTrue(((FixMessageWithType)array[0]).MessageType.Equals("1"));
			ClassicAssert.IsTrue(((FixMessageWithType)array[1]).MessageType.Equals("2"));
			ClassicAssert.IsTrue(((FixMessageWithType)array[2]).MessageType.Equals("3"));
		}

		[Test]
		public virtual void TestAppOnlyMessages()
		{
			var message = new FixMessage();
			message.Set(58, "test".AsByteArray());
			_queue.Add(FixMessageWithTypePoolFactory.GetFixMessageWithTypeFromPool(message, "D"));
			_queue.Add(FixMessageWithTypePoolFactory.GetFixMessageWithTypeFromPool(message, "8"));

			var array = _queue.ToArray();
			ClassicAssert.IsNotNull(array);
			ClassicAssert.AreEqual(2, array.Length);
			ClassicAssert.IsTrue(array[0] is FixMessageWithType);
			ClassicAssert.IsTrue(array[1] is FixMessageWithType);
			ClassicAssert.IsTrue(((FixMessageWithType)array[0]).MessageType.Equals("D"));
			ClassicAssert.IsTrue(((FixMessageWithType)array[1]).MessageType.Equals("8"));
		}

		[Test]
		public virtual void TestSessionAndAppMessages()
		{
			var message = new FixMessage();
			message.Set(58, "test".AsByteArray());
			_queue.AddOutOfTurn(FixMessageWithTypePoolFactory.GetFixMessageWithTypeFromPool(message, "1"));
			_queue.Add(FixMessageWithTypePoolFactory.GetFixMessageWithTypeFromPool(message, "D"));
			_queue.AddOutOfTurn(FixMessageWithTypePoolFactory.GetFixMessageWithTypeFromPool(message, "3"));
			_queue.Add(FixMessageWithTypePoolFactory.GetFixMessageWithTypeFromPool(message, "8"));

			var array = _queue.ToArray();
			ClassicAssert.IsNotNull(array);
			ClassicAssert.AreEqual(4, array.Length);
			ClassicAssert.IsTrue(array[0] is FixMessageWithType);
			ClassicAssert.IsTrue(array[1] is FixMessageWithType);
			ClassicAssert.IsTrue(array[2] is FixMessageWithType);
			ClassicAssert.IsTrue(array[3] is FixMessageWithType);
			ClassicAssert.IsTrue(((FixMessageWithType)array[0]).MessageType.Equals("1"));
			ClassicAssert.IsTrue(((FixMessageWithType)array[1]).MessageType.Equals("3"));
			ClassicAssert.IsTrue(((FixMessageWithType)array[2]).MessageType.Equals("D"));
			ClassicAssert.IsTrue(((FixMessageWithType)array[3]).MessageType.Equals("8"));
		}

		[Test]
		public virtual void TestWithPollNoCommit()
		{
			var message = new FixMessage();
			message.Set(58, "test".AsByteArray());
			_queue.AddOutOfTurn(FixMessageWithTypePoolFactory.GetFixMessageWithTypeFromPool(message, "1"));
			_queue.Add(FixMessageWithTypePoolFactory.GetFixMessageWithTypeFromPool(message, "D"));

			_queue.Poll();

			var array = _queue.ToArray();
			ClassicAssert.IsNotNull(array);
			ClassicAssert.AreEqual(2, array.Length);
			ClassicAssert.IsTrue(((FixMessageWithType)array[0]).MessageType.Equals("1"));
			ClassicAssert.IsTrue(((FixMessageWithType)array[1]).MessageType.Equals("D"));
		}

		[Test]
		public virtual void TestWithPollAndCommit()
		{
			var message = new FixMessage();
			message.Set(58, "test".AsByteArray());
			_queue.AddOutOfTurn(FixMessageWithTypePoolFactory.GetFixMessageWithTypeFromPool(message, "1"));
			_queue.Add(FixMessageWithTypePoolFactory.GetFixMessageWithTypeFromPool(message, "D"));

			_queue.Poll();
			_queue.Commit();

			var array = _queue.ToArray();
			ClassicAssert.IsNotNull(array);
			ClassicAssert.AreEqual(1, array.Length);
			ClassicAssert.IsTrue(((FixMessageWithType)array[0]).MessageType.Equals("D"));
		}

		[Test]
		public virtual void TestWithPollAndCommitAll()
		{
			var message = new FixMessage();
			message.Set(58, "test".AsByteArray());
			_queue.AddOutOfTurn(FixMessageWithTypePoolFactory.GetFixMessageWithTypeFromPool(message, "1"));
			_queue.Add(FixMessageWithTypePoolFactory.GetFixMessageWithTypeFromPool(message, "D"));

			_queue.Poll();
			_queue.Commit();
			_queue.Poll();
			_queue.Commit();

			var array = _queue.ToArray();
			ClassicAssert.IsNotNull(array);
			ClassicAssert.AreEqual(0, array.Length);
		}
	}
}
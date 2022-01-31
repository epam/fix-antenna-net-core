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

using System.IO;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.FixEngine.Storage.Queue;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Storage.Queue
{
	internal class PersistentInMemoryQueueTest
	{
		private const string Filename = "testqueue.bin";
		private const string Filename2 = "1955-ICE.outq";
		private FixMessage _list;
		private PersistentInMemoryQueue<FixMessageWithType> _queue;

		[SetUp]
		public virtual void SetUp()
		{
			_list = new FixMessage();
			_list.AddTag(58, "test");
			InitializeNewFactory();
			_queue.Add(FixMessageWithTypePoolFactory.GetFixMessageWithTypeFromPool(_list, "1"));
			_queue.Add(FixMessageWithTypePoolFactory.GetFixMessageWithTypeFromPool(_list, "2"));
			_queue.Add(FixMessageWithTypePoolFactory.GetFixMessageWithTypeFromPool(_list, "3"));
			_queue.Shutdown();
		}

		[TearDown]
		public virtual void TearDown()
		{
			_queue.Shutdown();
			new FileInfo(Filename).Delete();
			new FileInfo(Filename2).Delete();
		}

		[Test]
		public virtual void TestLength()
		{
			var utils = new PersistentInMemoryQueueUtils();
			Assert.AreEqual(0, utils.GetIntByteLength(0));
		}

		[Test]
		public virtual void TestHeader()
		{
			var utils = new PersistentInMemoryQueueUtils();
			var header = utils.GetLengthHeader(0);
			Assert.AreEqual(StringHelper.NewString(new byte[] { 0 }), StringHelper.NewString(header));
		}

		[Test]
		public virtual void TestMy()
		{
			_queue = new PersistentInMemoryQueue<FixMessageWithType>(Filename2, new FixMessageWithTypeFactory());
			_queue.Initialize();
			_queue.Shutdown();
		}

		[Test]
		public virtual void TestGetIntByteLength()
		{
			var utils = new PersistentInMemoryQueueUtils();
			Assert.AreEqual(0, utils.GetIntByteLength(0));
			Assert.AreEqual(1, utils.GetIntByteLength(1));
			Assert.AreEqual(1, utils.GetIntByteLength(255));
			Assert.AreEqual(2, utils.GetIntByteLength(256));
			Assert.AreEqual(2, utils.GetIntByteLength(32767));
			Assert.AreEqual(2, utils.GetIntByteLength(65535));
			Assert.AreEqual(3, utils.GetIntByteLength(65536));
		}

		[Test]
		public virtual void TestInitialize()
		{
			InitializeNewFactory();
			//System.in.read();
			Assert.AreEqual(3, _queue.Size);
			Assert.AreEqual("1", _queue.Poll().MessageType);
		}

		[Test]
		public virtual void TestCommit()
		{
			InitializeNewFactory();
			Assert.AreEqual(3, _queue.Size);
			_queue.Poll();
			_queue.Commit();
			Assert.AreEqual(2, _queue.Size);
			Assert.AreEqual("2", _queue.Poll().MessageType);
		}

		[Test]
		public virtual void TestAdd()
		{
			InitializeNewFactory();
			_queue.Clear();
			Assert.AreEqual(0, _queue.Size);
			_queue.Add(FixMessageWithTypePoolFactory.GetFixMessageWithTypeFromPool(_list, "4"));
			Assert.AreEqual(1, _queue.Size);
			Assert.AreEqual("4", _queue.Poll().MessageType);
		}

		[Test]
		public virtual void TestInitializeCommited()
		{
			InitializeNewFactory();
			_queue.Poll();
			_queue.Commit();
			_queue.Poll();
			_queue.Commit();
			_queue.Shutdown();
			InitializeNewFactory();
			Assert.AreEqual(1, _queue.Size);
		}

		private void InitializeNewFactory()
		{
			_queue = new PersistentInMemoryQueue<FixMessageWithType>(Filename, new FixMessageWithTypeFactory());
			_queue.Initialize();
		}
	}
}
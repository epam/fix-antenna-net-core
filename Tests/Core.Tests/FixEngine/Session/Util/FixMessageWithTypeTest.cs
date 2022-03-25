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

using Epam.FixAntenna.Common.Utils;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.Util
{
	[TestFixture]
	internal class FixMessageWithTypeTest
	{
		private FixMessageWithType _list;
		private const string UserRequest = "\u00005\u000158=User request\u0001";
		private ByteBuffer _buffer = new ByteBuffer();

		[Test]
		public virtual void TestGetBytesForTypedMsg()
		{
			var fieldList = new FixMessage();
			fieldList.AddTag(58, "Hello".AsByteArray());
			fieldList.AddTag(1, "1".AsByteArray());
			_list = FixMessageWithTypePoolFactory.GetFixMessageWithTypeFromPool(fieldList, "test");
			var expected = "\u0000test\u000158=Hello\u00011=1\u0001";
			_buffer.ResetBuffer();
			_list.SerializeTo(_buffer);
			var actual = StringHelper.NewString(_buffer.GetByteArray(), 0, _buffer.Offset);
			Assert.AreEqual(expected, actual);
		}

		[Test]
		public virtual void TestGetBytesForChangesType()
		{
			var fieldList = new FixMessage();
			fieldList.AddTag(58, "Hello".AsByteArray());
			fieldList.AddTag(1, "1".AsByteArray());
			_list = FixMessageWithTypePoolFactory.GetFixMessageWithTypeFromPool(fieldList, ChangesType.AddSmhAndSmt);
			var expected = "\u0000\u0001\u000B\u000158=Hello\u00011=1\u0001";
			_buffer.ResetBuffer();
			_list.SerializeTo(_buffer);
			var actual = StringHelper.NewString(_buffer.GetByteArray(), 0, _buffer.Offset);
			Assert.AreEqual(expected, actual);
		}

		[Test]
		public virtual void TestFromBytes()
		{
			_list = new FixMessageWithType();
			_list.FromBytes("\u00000\u0001".AsByteArray(), 0, 2);
			Assert.AreEqual("0", _list.MessageType);
		}

		[Test]
		public virtual void TestFromBytes1()
		{
			_list = new FixMessageWithType();
			var encoded = "\u00000\u00018=FIX.4.0\u0001".AsByteArray();
			_list.FromBytes(encoded, 0, encoded.Length);
			Assert.AreEqual("0", _list.MessageType);
			Assert.AreEqual("FIX.4.0", _list.FixMessage.GetTag(8).StringValue);
		}

		[Test]
		public virtual void TestFromBytes2()
		{
			_list = new FixMessageWithType();
			_list.FromBytes(UserRequest.AsByteArray(), 0, UserRequest.Length);
			Assert.AreEqual("5", _list.MessageType);
			Assert.AreEqual("User request", _list.FixMessage.GetTag(58).StringValue);
		}

		[Test]
		public virtual void TestToAndFromBytesWithChangesType()
		{
			var fieldList = new FixMessage();
			fieldList.AddTag(58, "Hello");
			fieldList.AddTag(1, "1");
			var list = new FixMessageWithType();
			list.Init(fieldList, ChangesType.AddSmhAndSmt);

			_buffer.ResetBuffer();
			list.SerializeTo(_buffer);

			Assert.AreEqual("\u0000\u0001\u000B\u000158=Hello\u00011=1\u0001", StringHelper.NewString(_buffer.GetByteArray(), 0, _buffer.Offset));
			var restoredList = new FixMessageWithType();
			restoredList.FromBytes(_buffer.GetByteArray(), 0, _buffer.Offset);
			EqualUtils.RefletionEqual(list, restoredList);
		}

		[Test]
		public virtual void TestEmptyMsgWithChangesType()
		{
			var fieldList = new FixMessage();
			var list = new FixMessageWithType();
			list.Init(fieldList, ChangesType.AddSmhAndSmt);

			_buffer.ResetBuffer();
			list.SerializeTo(_buffer);

			Assert.AreEqual("\u0000\u0001\u000B\u0001", StringHelper.NewString(_buffer.GetByteArray(), 0, _buffer.Offset));
			var restoredList = new FixMessageWithType();
			restoredList.FromBytes(_buffer.GetByteArray(), 0, _buffer.Offset);
			EqualUtils.RefletionEqual(list, restoredList);
		}

		[Test]
		public virtual void TestTypedMsgWithChangesType()
		{
			var fieldList = new FixMessage();
			fieldList.AddTag(35, "0");
			var list = new FixMessageWithType();
			list.Init(fieldList, ChangesType.AddSmhAndSmt);

			_buffer.ResetBuffer();
			list.SerializeTo(_buffer);

			Assert.AreEqual("\u0000\u0001\u000B\u000135=0\u0001", StringHelper.NewString(_buffer.GetByteArray(), 0, _buffer.Offset));
			var restoredList = new FixMessageWithType();
			restoredList.FromBytes(_buffer.GetByteArray(), 0, _buffer.Offset);
			EqualUtils.RefletionEqual(list, restoredList);
		}

		[Test]
		public virtual void TestIsApplicationLevelMessage()
		{
			var list = new FixMessageWithType();
			var emptyMsg = new FixMessage();
			var sessionMsg = RawFixUtil.GetFixMessage("35=A\u0001".AsByteArray());
			var appMsg = RawFixUtil.GetFixMessage("35=W\u0001".AsByteArray());

			//test init(FixMessage content, String type)
			list.Init(null, (string) null);
			Assert.IsFalse(list.IsApplicationLevelMessage());

			list.Init(null, "A");
			Assert.IsFalse(list.IsApplicationLevelMessage());

			list.Init(null, "W");
			Assert.IsTrue(list.IsApplicationLevelMessage());


			list.Init(emptyMsg, (string) null);
			Assert.IsFalse(list.IsApplicationLevelMessage());

			list.Init(emptyMsg, "A");
			Assert.IsFalse(list.IsApplicationLevelMessage());

			list.Init(emptyMsg, "W");
			Assert.IsTrue(list.IsApplicationLevelMessage());

			list.Init(appMsg, (string) null);
			Assert.IsTrue(list.IsApplicationLevelMessage());

			list.Init(appMsg, "A");
			Assert.IsFalse(list.IsApplicationLevelMessage());

			list.Init(appMsg, "W");
			Assert.IsTrue(list.IsApplicationLevelMessage());


			//test (FixMessage content, ChangesType changesType)
			list.Init(null, (ChangesType?) null);
			Assert.IsFalse(list.IsApplicationLevelMessage());

			list.Init(null, ChangesType.DeleteAndAddSmhAndSmt);
			Assert.IsFalse(list.IsApplicationLevelMessage());


			list.Init(emptyMsg, (ChangesType?) null);
			Assert.IsFalse(list.IsApplicationLevelMessage());

			list.Init(emptyMsg, ChangesType.DeleteAndAddSmhAndSmt);
			Assert.IsFalse(list.IsApplicationLevelMessage());


			list.Init(sessionMsg, (ChangesType?) null);
			Assert.IsFalse(list.IsApplicationLevelMessage());

			list.Init(sessionMsg, ChangesType.DeleteAndAddSmhAndSmt);
			Assert.IsFalse(list.IsApplicationLevelMessage());


			list.Init(appMsg, (ChangesType?) null);
			Assert.IsTrue(list.IsApplicationLevelMessage());

			list.Init(appMsg, ChangesType.DeleteAndAddSmhAndSmt);
			Assert.IsTrue(list.IsApplicationLevelMessage());
		}
	}
}
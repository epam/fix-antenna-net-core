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
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Message.Storage;
using NUnit.Framework;

namespace Epam.FixAntenna.Message.Tests
{
	[TestFixture]
	internal class FixMessageUnitTest
	{
		[TearDown]
		public virtual void TearDown()
		{
		}

		private const string LogonMessage =
			"8=FIX.4.3\u00019=94\u000135=A\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u000198=0\u0001108=600\u000110=124\u0001";

		private byte[] GetFilledArray(int size, byte val)
		{
			var bytes = new byte[size];
#if NET48
			for (var i = 0; i < bytes.Length; i++)
			{
				bytes[i] = val;
			}
#else
			Array.Fill(bytes, val);
#endif
			return bytes;
		}

		private void AssertFieldsEquals(FixMessage expected, FixMessage actual)
		{
			var size = expected.Length;
			Assert.AreEqual(size, actual.Length, "Invalid size ");
			for (var i = 0; i < size; i++)
			{
				var expectedField = expected[i];
				var actualField = actual[i];
				Assert.IsTrue(expectedField.Equals(actualField),
					"Tags no equals. Expected " + expectedField.ToString() + " but there is " + actualField.ToString());
			}
		}

		private FixMessage GetParsedMessage(string message)
		{
			return RawFixUtil.GetFixMessage(message.AsByteArray(), true, false);
		}

		[Test]
		public virtual void TestCreateFromPoolForInternalUsage()
		{
			var fixFields = FixMessageFactory.NewInstanceFromPool(false);
			Assert.IsFalse(fixFields.IsUserOwned);
			Assert.IsTrue(fixFields.IsOriginatingFromPool);
			Assert.IsFalse(fixFields.IsPreparedMessage);
			Assert.IsFalse(fixFields.IsMessageIncomplete);
			Assert.IsTrue(fixFields.IsEmpty);
			Assert.IsFalse(fixFields.NeedCloneOnSend);
			Assert.IsTrue(fixFields.NeedReleaseAfterSend);
		}

		[Test]
		public virtual void TestCreateFromPoolForUser()
		{
			var fixFields = FixMessageFactory.NewInstanceFromPool(true);
			Assert.IsTrue(fixFields.IsUserOwned);
			Assert.IsTrue(fixFields.IsOriginatingFromPool);
			Assert.IsFalse(fixFields.IsPreparedMessage);
			Assert.IsFalse(fixFields.IsMessageIncomplete);
			Assert.IsTrue(fixFields.IsEmpty);
			Assert.IsTrue(fixFields.NeedCloneOnSend);
			Assert.IsFalse(fixFields.NeedReleaseAfterSend);
		}

		[Test]
		public virtual void TestDeepCopyOfParsedAndChangedMessage()
		{
			var parsedMsg = GetParsedMessage(LogonMessage);
			parsedMsg.AddTag(55, "AAA");
			parsedMsg.AddTag(56, GetFilledArray(ArenaMessageStorage.MaxBytesInArenaStorage * 2, (byte)'B'));
			parsedMsg.AddTag(57, GetFilledArray(ArenaMessageStorage.MaxBytesInArenaStorage * 2, (byte)'C'));
			parsedMsg.GetTag(56);

			var clonedMsg = parsedMsg.DeepClone(true, true);
			AssertFieldsEquals(parsedMsg, clonedMsg);
		}

		[Test]
		public virtual void TestDeepCopyOfParsedMessage()
		{
			var parsedMsg = GetParsedMessage(LogonMessage);
			var clonedMsg = parsedMsg.DeepClone(true, true);
			AssertFieldsEquals(parsedMsg, clonedMsg);
		}

		[Test]
		public virtual void TestSerializationAfterDeepCopyWithArenaTags()
		{
			var messageBytes = "35=D\u000149=BLP\u0001".AsByteArray();
			var message = RawFixUtil.GetFixMessage(messageBytes);
			message.IsPreparedMessage = true;
			message.Set(49, "AAAA");
			var message2 = message.DeepClone(true, false);
			var bytes = message2.AsByteArray();
			Assert.AreEqual("35=D\u000149=AAAA\u0001", StringHelper.NewString(bytes));
		}

		[Test]
		public virtual void TestSerializationAfterDeepCopyWithLeadingArenaTag()
		{
			var messageBytes = "35=D\u000149=BLP\u0001".AsByteArray();
			var message = RawFixUtil.GetFixMessage(messageBytes);
			message.IsPreparedMessage = true;
			message.Set(35, "AAAA");
			var message2 = message.DeepClone(true, false);
			var bytes = message2.AsByteArray();
			Assert.AreEqual("35=AAAA\u000149=BLP\u0001", StringHelper.NewString(bytes));
		}
	}
}
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

using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework;

namespace Epam.FixAntenna.Message.Tests
{
	[TestFixture]
	internal class FixMessageCopyTest
	{
		private const string Message =
			"8=FIX.4.3\u00019=94\u000135=A\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u000198=0\u0001108=600\u000110=124\u0001";

		private void AssertFieldsInStorage(FixMessage msg, int storageType)
		{
			for (var i = 0; i < msg.Count; i++)
			{
				var stType = msg.GetStorageType(i);
				Assert.AreEqual(storageType, stType,
					"Tag " + msg.GetTagIndex(i) + " has storage type " + stType + " instead of " + storageType);
			}
		}

		private FixMessage GetParsedMessage(string message)
		{
			return RawFixUtil.GetFixMessage(message.AsByteArray(), true, false);
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

		[Test]
		public virtual void DeepCopyArenaStorageTest()
		{
			var m1 = new FixMessage();
			m1.AddTag(1, "FIXCONNECT");
			m1.AddTag(58, "junkid not active");

			var m2 = new FixMessage();
			m2.AddTag(11, "930474883731271");
			m2.AddTag(58, "Invalid Price");

			m2.DeepCopyTo(m1);
			Assert.That(m2.ToString(), Is.EqualTo(m1.ToString()));
		}

		[Test]
		public virtual void TestDeepCopyIndependence()
		{
			var parsedMsg = GetParsedMessage(Message);
			Assert.IsFalse(parsedMsg.Standalone);
			AssertFieldsInStorage(parsedMsg, FieldIndex.FlagOrigbufStorage);

			var clonedMsg = parsedMsg.DeepClone(true, true);
			AssertFieldsEquals(parsedMsg, clonedMsg);

			Assert.IsTrue(clonedMsg.Standalone);
			AssertFieldsInStorage(clonedMsg, FieldIndex.FlagArenaStorage);

			Assert.IsNull(clonedMsg.GetOriginalStorage().Buffer,
				"Cloned message shouldn't have link to original buffer");

			((AbstractFixMessage)clonedMsg).Clear();
			clonedMsg.AddTag(1, (long)1);
			clonedMsg.AddTag(2, (long)2);
			Assert.AreEqual(Message, parsedMsg.ToString());
		}

		[Test]
		public virtual void TestDoubleDeepCopyIndependence()
		{
			var parsedMsg = GetParsedMessage(Message);
			var clonedMsg1 = parsedMsg.DeepClone(true, true);
			var clonedMsg2 = clonedMsg1.DeepClone(true, true);

			Assert.IsTrue(clonedMsg2.Standalone);
			AssertFieldsInStorage(clonedMsg2, FieldIndex.FlagArenaStorage);

			Assert.IsFalse(
				clonedMsg1.GetArenaStorage().Buffer.GetHashCode() ==
				clonedMsg2.GetArenaStorage().Buffer.GetHashCode(),
				"cloned message should have copy of arena storage");
		}
	}
}
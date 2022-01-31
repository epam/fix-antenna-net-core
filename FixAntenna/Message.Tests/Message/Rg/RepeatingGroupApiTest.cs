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
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Message.Format;
using Epam.FixAntenna.NetCore.Message.Rg;
using NUnit.Framework;

namespace Epam.FixAntenna.Message.Tests.Rg
{
	[TestFixture]
	internal class RepeatingGroupApiTest
	{
		[SetUp]
		public virtual void CreateMessage()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"454=1\u0001455=5\u0001456=abc\u0001" + "232=2\u0001233=N\u0001234=8.23\u0001233=9\u0001234=9\u0001" +
				"518=1\u0001519=10\u0001520=11\u0001" + "555=1\u0001600=123\u0001604=1\u0001605=124\u0001" +
				"10=124\u0001";
			Msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());
		}

		[SetUp]
		public virtual void CreateMessageForUpdate()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"454=1\u0001455=5\u0001456=abc\u0001" + "232=2\u0001233=N\u0001234=8.23\u0001233=9\u0001234=9\u0001" +
				"518=1\u0001519=10\u0001520=11\u000110=124\u0001";
			MsgForUpdate = RawFixUtil.GetFixMessage(executionReport.AsByteArray());
			MsgForUpdate = RawFixUtil.IndexRepeatingGroup(MsgForUpdate, true);

			var group = MsgForUpdate.AddRepeatingGroupAtIndex(9, 123, false);
			var entry = group.AddEntry();

			var tvForAdd = MsgForUpdate.GetTag(456);
			tvForAdd.TagId = 124;

			entry.AddTag(tvForAdd);
			entry.AddTag(125, "bytevalue".AsByteArray());
			entry.AddTag(126, false);
			entry.AddTag(127, "bytevalue".AsByteArray(), 2, 4);
			entry.AddTag(128, 1.0001, 4);
			entry.AddTag(129, 25);
			entry.AddTag(130, "value");
			var date = DateTime.SpecifyKind(new DateTime(2015, 10, 7), DateTimeKind.Utc);

			entry.AddCalendarTag(131, date, FixDateFormatterFactory.FixDateType.Date40);

			entry = group.AddEntry();
			entry.AddTag(124, 163);
			entry.AddTag(126, true);
			entry.AddTag(129, 35);
		}

		internal FixMessage Msg;
		internal FixMessage MsgForUpdate;

		private void Update(RepeatingGroup.Entry entry, IndexedStorage.MissingTagHandling missingTagHandling)
		{
			var tvForUpdate = MsgForUpdate.GetTag(125);
			tvForUpdate.TagId = 124;

			entry.UpdateValue(tvForUpdate, missingTagHandling);
			entry.UpdateValue(125, "updatedValue".AsByteArray(), missingTagHandling);
			entry.UpdateValue(126, true, missingTagHandling);
			entry.UpdateValue(127, "updateValue".AsByteArray(), 2, 4, missingTagHandling);
			entry.UpdateValue(128, 2.0058, 4, missingTagHandling);
			entry.UpdateValue(129, 123, missingTagHandling);
			entry.UpdateValue(130, "updatedStrValue", missingTagHandling);

			var date = DateTime.SpecifyKind(new DateTime(2014, 3, 3), DateTimeKind.Utc);
			entry.UpdateCalendarValue(131, date, FixDateFormatterFactory.FixDateType.Date40, missingTagHandling);
		}

		[Test]
		public virtual void GetRepeatingGroupTest()
		{
			Assert.AreEqual("232=2\u0001233=N\u0001234=8.23\u0001233=9\u0001234=9\u0001",
				Msg.GetRepeatingGroup(232).ToString());
			Assert.AreEqual("232=2\u0001233=N\u0001234=8.23\u0001233=9\u0001234=9\u0001",
				Msg.GetRepeatingGroupAtIndex(12).ToString());
		}

		[Test]
		public virtual void RemoveRepeatingGroupTest()
		{
			Msg.RemoveRepeatingGroup(232);
			var expected =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"454=1\u0001455=5\u0001456=abc\u0001" + "518=1\u0001519=10\u0001520=11\u0001" +
				"555=1\u0001600=123\u0001604=1\u0001605=124\u0001" + "10=124\u0001";
			Assert.AreEqual(expected, Msg.ToString());

			Msg.RemoveRepeatingGroupAtIndex(9);
			expected =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"518=1\u0001519=10\u0001520=11\u0001" + "555=1\u0001600=123\u0001604=1\u0001605=124\u0001" +
				"10=124\u0001";

			Assert.AreEqual(expected, Msg.ToString());
		}

		[Test]
		public virtual void TestAddMethods()
		{
			var group = Msg.AddRepeatingGroupAtIndex(9, 123, false);
			var entry = group.AddEntry();

			var tvForAdd =  Msg.GetTag(456);
			tvForAdd.TagId =124;

			entry.AddTag(tvForAdd);
			entry.AddTag(125, "bytevalue".AsByteArray());
			entry.AddTag(126, false);
			entry.AddTag(127, "bytevalue".AsByteArray(), 2, 4);
			entry.AddTag(128, 1.0001, 4);
			entry.AddTag(129, 25);
			entry.AddTag(130, "value");

			var date = DateTime.SpecifyKind(new DateTime(2015, 10, 7), DateTimeKind.Utc);
			entry.AddCalendarTag(131, date, FixDateFormatterFactory.FixDateType.Date40);

			var expectedMessage =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"123=1\u0001124=abc\u0001125=bytevalue\u0001126=N\u0001127=teva\u0001128=1.0001\u0001129=25\u0001130=value\u0001131=20151007\u0001" +
				"454=1\u0001455=5\u0001456=abc\u0001" + "232=2\u0001233=N\u0001234=8.23\u0001233=9\u0001234=9\u0001" +
				"518=1\u0001519=10\u0001520=11\u0001" + "555=1\u0001600=123\u0001604=1\u0001605=124\u0001" +
				"10=124\u0001";
			Assert.AreEqual(expectedMessage, Msg.ToString());
		}

		[Test]
		public virtual void TestGetByIndex()
		{
			var group = Msg.GetRepeatingGroup(454);
			var entry = group.GetEntry(0);
			var expectedTagValue = new TagValue();
			var actualTagValue = new TagValue();

			Msg.LoadTagValue(455, expectedTagValue);
			entry.LoadTagValueByIndex(0, actualTagValue);
			Assert.AreEqual(expectedTagValue, actualTagValue);

			var expectedStrValue = Msg.GetTagValueAsString(456);
			var actualStrValue = entry.GetTagValueAsStringAtIndex(1);
			Assert.AreEqual(expectedStrValue, actualStrValue);

			var expectedBuffValue = new StringBuilder();
			var actualBuffValue = new StringBuilder();
			Msg.GetTagValueAsStringBuff(456, expectedBuffValue);
			entry.GetTagValueAsStringBuffAtIndex(1, actualBuffValue);
			Assert.AreEqual(expectedBuffValue.ToString(), actualBuffValue.ToString());

			var expectedBytesValue = Msg.GetTagValueAsBytes(456);
			var actualBytesValue = entry.GetTagValueAsBytesAtIndex(1);
			Assert.That(actualBytesValue, Is.EquivalentTo(expectedBytesValue));

			Msg.GetTagValueAsBytes(456, expectedBytesValue, 0);
			entry.GetTagValueAsBytesAtIndex(1, actualBytesValue, 0);
			Assert.That(actualBytesValue, Is.EquivalentTo(expectedBytesValue));

			var expectedByteValue = Msg.GetTagValueAsByte(455);
			var actualByteValue = entry.GetTagValueAsByteAtIndex(0);
			Assert.AreEqual(expectedByteValue, actualByteValue);

			expectedByteValue = Msg.GetTagValueAsByte(456, 1);
			actualByteValue = entry.GetTagValueAsByteAtIndex(1, 1);
			Assert.AreEqual(expectedByteValue, actualByteValue);

			group = Msg.GetRepeatingGroup(232);
			entry = group.GetEntry(0);
			var expectedBoolValue = Msg.GetTagValueAsBool(233);
			var actualBoolValue = entry.GetTagValueAsBoolAtIndex(0);
			Assert.AreEqual(expectedBoolValue, actualBoolValue);

			var expectedDoubleValue = Msg.GetTagValueAsDouble(234);
			var actualDoubleValue = entry.GetTagValueAsDoubleAtIndex(1);
			Assert.AreEqual(expectedDoubleValue, actualDoubleValue, 0.00001);

			entry = group.GetEntry(1);
			var expectedLongValue = Msg.GetTagValueAsLong(233, 2);
			var actualLongValue = entry.GetTagValueAsLongAtIndex(0);
			Assert.AreEqual(expectedLongValue, actualLongValue);

			group = Msg.GetRepeatingGroup(555);
			entry = group.GetEntry(0);
			Assert.IsTrue(entry.IsGroupTagAtIndex(1));
			Assert.IsFalse(entry.IsGroupTagAtIndex(0));
		}

		[Test]
		public virtual void TestGetters()
		{
			var group = Msg.GetRepeatingGroup(454);
			var entry = group.GetEntry(0);
			var actualTagValue = new TagValue();
			var expectedTagValue = new TagValue();

			Msg.LoadTagValue(455, expectedTagValue);
			entry.LoadTagValue(455, actualTagValue);
			Assert.AreEqual(expectedTagValue, actualTagValue);

			var expectedStrValue = Msg.GetTagValueAsString(456);
			var actualStrValue = entry.GetTagValueAsString(456);
			Assert.AreEqual(expectedStrValue, actualStrValue);

			var expectedBuffValue = new StringBuilder();
			var actualBuffValue = new StringBuilder();
			Msg.GetTagValueAsStringBuff(456, expectedBuffValue);
			entry.GetTagValueAsStringBuff(456, actualBuffValue);
			Assert.AreEqual(expectedBuffValue.ToString(), actualBuffValue.ToString());

			var expectedBytesValue = Msg.GetTagValueAsBytes(456);
			var actualBytesValue = entry.GetTagValueAsBytes(456);
			Assert.That(actualBytesValue, Is.EquivalentTo(expectedBytesValue));

			Msg.GetTagValueAsBytes(456, expectedBytesValue, 0);
			entry.GetTagValueAsBytes(456, actualBytesValue, 0);
			Assert.That(actualBytesValue, Is.EquivalentTo(expectedBytesValue));

			var expectedByteValue = Msg.GetTagValueAsByte(455);
			var actualByteValue = entry.GetTagValueAsByte(455);
			Assert.AreEqual(expectedByteValue, actualByteValue);

			expectedByteValue = Msg.GetTagValueAsByte(456, 1);
			actualByteValue = entry.GetTagValueAsByte(456, 1);
			Assert.AreEqual(expectedByteValue, actualByteValue);

			group = Msg.GetRepeatingGroup(232);
			entry = group.GetEntry(0);
			var expectedBoolValue = Msg.GetTagValueAsBool(233);
			var actualBoolValue = entry.GetTagValueAsBool(233);
			Assert.AreEqual(expectedBoolValue, actualBoolValue);

			var expectedDoubleValue = Msg.GetTagValueAsDouble(234);
			var actualDoubleValue = entry.GetTagValueAsDouble(234);
			Assert.AreEqual(expectedDoubleValue, actualDoubleValue, 0.00001);

			entry = group.GetEntry(1);
			var expectedLongValue = Msg.GetTagValueAsLong(233, 2);
			var actualLongValue = entry.GetTagValueAsLong(233);
			Assert.AreEqual(expectedLongValue, actualLongValue);

			Assert.AreEqual(false, entry.IsTagExists(123));
			Assert.AreEqual(true, entry.IsTagExists(233));

			Assert.IsTrue(Msg.IsRepeatingGroupExists(555));
			group = Msg.GetRepeatingGroup(555);
			entry = group.GetEntry(0);
			Assert.IsTrue(entry.IsRepeatingGroupExists(604));
		}

		[Test]
		public virtual void TestRemoveTagAtIndex()
		{
			var group = Msg.GetRepeatingGroup(555);
			var entry = group.GetEntry(0);

			entry.RemoveTagAtIndex(1);
			var expectedMessage =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"454=1\u0001455=5\u0001456=abc\u0001" + "232=2\u0001233=N\u0001234=8.23\u0001233=9\u0001234=9\u0001" +
				"518=1\u0001519=10\u0001520=11\u0001" + "555=1\u0001600=123\u0001" + "10=124\u0001";
			Assert.AreEqual(expectedMessage, Msg.ToString());
		}

		[Test]
		public virtual void TestUpdateMethodsAddIfNotExists()
		{
			var group = MsgForUpdate.GetRepeatingGroup(123);
			@group.Validation = false;
			var entry = group.GetEntry(1);
			Update(entry, IndexedStorage.MissingTagHandling.AddIfNotExists);
			var expectedMessage =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"123=2\u0001124=abc\u0001125=bytevalue\u0001126=N\u0001127=teva\u0001128=1.0001\u0001129=25\u0001130=value\u0001131=20151007\u0001" +
				"124=bytevalue\u0001126=Y\u0001129=123\u0001125=updatedValue\u0001127=date\u0001128=2.0058\u0001130=updatedStrValue\u0001131=20140303\u0001" +
				"454=1\u0001455=5\u0001456=abc\u0001" + "232=2\u0001233=N\u0001234=8.23\u0001233=9\u0001234=9\u0001" +
				"518=1\u0001519=10\u0001520=11\u000110=124\u0001";
			Assert.AreEqual(expectedMessage, MsgForUpdate.ToString());
		}

		[Test]
		public virtual void TestUpdateMethodsAlwaysAdd()
		{
			var group = MsgForUpdate.GetRepeatingGroup(123);
			@group.Validation = false;
			var entry = group.AddEntry();
			Update(entry, IndexedStorage.MissingTagHandling.AlwaysAdd);
			var expectedMessage =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001"
				+ "123=3\u0001124=abc\u0001125=bytevalue\u0001126=N\u0001127=teva\u0001128=1.0001\u0001129=25\u0001130=value\u0001131=20151007\u0001"
				+ "124=163\u0001126=Y\u0001129=35\u0001"
				+ "124=bytevalue\u0001125=updatedValue\u0001126=Y\u0001127=date\u0001128=2.0058\u0001129=123\u0001130=updatedStrValue\u0001131=20140303\u0001"
				+ "454=1\u0001455=5\u0001456=abc\u0001"
				+ "232=2\u0001233=N\u0001234=8.23\u0001233=9\u0001234=9\u0001"
				+ "518=1\u0001519=10\u0001520=11\u000110=124\u0001";
			Assert.AreEqual(expectedMessage, MsgForUpdate.ToString());
		}

		[Test]
		public virtual void TestUpdateMethodsDontAddIfNotExists()
		{
			var group = MsgForUpdate.GetRepeatingGroup(123);
			var entry = group.GetEntry(1);
			Update(entry, IndexedStorage.MissingTagHandling.DontAddIfNotExists);
			var expectedMessage =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"123=2\u0001124=abc\u0001125=bytevalue\u0001126=N\u0001127=teva\u0001128=1.0001\u0001129=25\u0001130=value\u0001131=20151007\u0001" +
				"124=bytevalue\u0001126=Y\u0001129=123\u0001" + "454=1\u0001455=5\u0001456=abc\u0001" +
				"232=2\u0001233=N\u0001234=8.23\u0001233=9\u0001234=9\u0001" +
				"518=1\u0001519=10\u0001520=11\u000110=124\u0001";
			Assert.AreEqual(expectedMessage, MsgForUpdate.ToString());
		}
	}
}
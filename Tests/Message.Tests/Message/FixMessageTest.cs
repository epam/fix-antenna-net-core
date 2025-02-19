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
using System.Text;
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Message.Format;
using Epam.FixAntenna.NetCore.Message.Storage;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Message.Tests
{
	internal class FixMessageTest
	{
		private const string Message =
			"8=FIX.4.3\u00019=94\u000135=A\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u000198=0\u0001108=600\u000110=124\u0001";

		private const string MessageNews =
			"8=FIX.4.3\u00019=94\u000135=C\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u000133=2\u000158=1\u000158=2\u000110=124\u0001";

		private const string MsgHbi =
			"8=FIX.4.4\u00019=66\u000135=0\u000134=15\u000149=SNDR\u000152=20121225-11:55:47.669\u000156=TRGT\u0001112=123456789\u000110=063\u0001";

		private DateTime _calendarUtc;
		private string _expectedDateTime;
		private FixMessage _message;
		private FixMessage _msgHbiList;
		private FixMessage _newsMessage;

		[SetUp]
		public void Before()
		{
			_message = RawFixUtil.GetFixMessage(Message.AsByteArray());
			_newsMessage = RawFixUtil.GetFixMessage(MessageNews.AsByteArray());
			_msgHbiList = RawFixUtil.GetFixMessage(MsgHbi.AsByteArray());
			_calendarUtc = new DateTime(2001, 3, 3, 4, 5, 6, DateTimeKind.Utc);
			_expectedDateTime = "20010303-04:05:06";
		}

		[Test]
		public void TestToString()
		{
			_msgHbiList.IsPreparedMessage = false;
			ClassicAssert.AreEqual(MsgHbi, _msgHbiList.ToString());
			_msgHbiList.IsPreparedMessage = true;
			ClassicAssert.AreEqual(MsgHbi, _msgHbiList.ToString());
		}

		[Test]
		public void TestToPrintableString()
		{
			_msgHbiList.IsPreparedMessage = false;
			_msgHbiList.AddTagAtIndex(5, 58, "Mickey Mouse");
			_msgHbiList.AddTagAtIndex(6, Tags.Password, "password");
			var expected =
				"8=FIX.4.4 | 9=66 | 35=0 | 34=15 | 49=SNDR | 58=Mickey Mouse | 554=******** | 52=20121225-11:55:47.669 | 56=TRGT | 112=123456789 | 10=063 | ";
			ClassicAssert.AreEqual(expected, _msgHbiList.ToPrintableString());
			_msgHbiList.IsPreparedMessage = true;
			ClassicAssert.AreEqual(expected, _msgHbiList.ToPrintableString());
		}

		[Test]
		public void TestParseFromString()
		{
			ClassicAssertEqualsString(8, "FIX.4.4", _msgHbiList);
			ClassicAssert.AreEqual(15, _msgHbiList.GetTagAsInt(34));
			ClassicAssert.AreEqual(0, _msgHbiList.GetTagAsInt(35));
			ClassicAssertEqualsString(49, "SNDR", _msgHbiList);
			ClassicAssertEqualsString(10, "063", _msgHbiList);
			ClassicAssert.AreEqual(63, _msgHbiList.GetTagAsInt(10));
		}

		//------------------------ SET METHOD --------------------//

		// StringBuffer

		[Test]
		public void TestSetStringBufferShorterValue()
		{
			TestSetStringBuffer(112, new StringBuilder("11"));
		}

		[Test]
		public void TestSetStringBufferLongerValue()
		{
			TestSetStringBuffer(112,
				new StringBuilder("Test").Append("Test").Append("Test").Append("Test").Append("Test").Append("Test"));
		}

		[Test]
		public void TestSetAtIndexStringBufferLongerValue()
		{
			var value = new StringBuilder("Test").Append("Test").Append("Test").Append("Test").Append("Test");
			_msgHbiList.SetAtIndex(7, value.ToString().AsByteArray());
			var expected = ReplaceFieldValue(112, value.ToString(), MsgHbi);
			ClassicAssert.AreEqual(expected, _msgHbiList.ToString());
		}

		[Test]
		public void TestSetOccurrenceStringBuffer()
		{
			var value = new StringBuilder("Test").Append("Test").Append("Test").Append("Test").Append("Test");
			_newsMessage.Set(58, 2, value.ToString().AsByteArray());
			var expected = ReplaceFieldValue(58, 2, value.ToString(), MessageNews);
			ClassicAssert.AreEqual(expected, _newsMessage.ToString());
		}

		[Test]
		public void TestSetStringBufferShorterValuePreparedMessage()
		{
			_msgHbiList.IsPreparedMessage = true;
			TestSetStringBufferShorterValue();
		}

		[Test]
		public void TestSetStringBufferLongerValuePreparedMessage()
		{
			_msgHbiList.IsPreparedMessage = true;
			TestSetStringBufferLongerValue();
		}

		private void TestSetStringBuffer(int tagId, StringBuilder value)
		{
			_msgHbiList.Set(tagId, value);
			var expected = ReplaceFieldValue(tagId, value.ToString(), MsgHbi);
			ClassicAssert.AreEqual(expected, _msgHbiList.ToString());
		}

		// Boolean

		[Test]
		public void TestSetBooleanShorterValue()
		{
			TestSetBoolean(112, true);
		}

		[Test]
		public void TestSetBooleanLongerValue()
		{
			var msgStr = ReplaceFieldValue(112, "1", MsgHbi);
			_msgHbiList = RawFixUtil.GetFixMessage(msgStr.AsByteArray());
			TestSetBoolean(112, false);
		}

		[Test]
		public void TestSetBooleanShorterValuePreparedMessage()
		{
			_msgHbiList.IsPreparedMessage = true;
			TestSetBoolean(112, true);
		}

		[Test]
		public void TestSetBooleanLongerValuePreparedMessage()
		{
			var msgStr = ReplaceFieldValue(112, "1", MsgHbi);
			_msgHbiList = RawFixUtil.GetFixMessage(msgStr.AsByteArray());
			_msgHbiList.IsPreparedMessage = true;
			TestSetBoolean(112, false);
		}

		[Test]
		public void TestSetAtIndexBooleanValue()
		{
			var value = true;
			_msgHbiList.SetAtIndex(7, value);
			ClassicAssert.AreEqual(ReplaceFieldValue(112, value ? "Y" : "N", MsgHbi), _msgHbiList.ToString());
		}

		[Test]
		public virtual void TestSetOccurrenceBoolean()
		{
			var value = true;
			_newsMessage.Set(58, 2, value);
			ClassicAssert.AreEqual(ReplaceFieldValue(58, 2, value ? "Y" : "N", MessageNews), _newsMessage.ToString());
		}

		private void TestSetBoolean(int tagId, bool value)
		{
			_msgHbiList.Set(tagId, value);
			ClassicAssert.AreEqual(ReplaceFieldValue(tagId, value ? "Y" : "N", MsgHbi), _msgHbiList.ToString());
		}

		// byte[]

		[Test]
		public virtual void TestSetByteArrayShorterValue()
		{
			TestSetByteArray(112, "1".AsByteArray());
		}

		[Test]
		public virtual void TestSetByteArrayLongerValue()
		{
			TestSetByteArray(112, "TestTestTestTestTestTestTestTestTestTestTest".AsByteArray());
		}

		[Test]
		public virtual void TestSetByteArrayShorterValuePreparedMessage()
		{
			_msgHbiList.IsPreparedMessage = true;
			TestSetByteArrayShorterValue();
		}

		[Test]
		public virtual void TestSetByteArrayLongerValuePreparedMessage()
		{
			_msgHbiList.IsPreparedMessage = true;
			TestSetByteArrayLongerValue();
		}

		private void TestSetByteArray(int tagId, byte[] value)
		{
			_msgHbiList.Set(tagId, value);
			ClassicAssert.AreEqual(ReplaceFieldValue(tagId, StringHelper.NewString(value), MsgHbi), _msgHbiList.ToString());
		}

		[Test]
		public virtual void TestSetOccurrenceByteArray()
		{
			var value = "Test".AsByteArray();
			_newsMessage.Set(58, 2, value);
			ClassicAssert.AreEqual(ReplaceFieldValue(58, 2, StringHelper.NewString(value), MessageNews),
				_newsMessage.ToString());
		}

		// byte[], offset, length,

		[Test]
		public virtual void TestSetBitByteArrayShorterValue()
		{
			TestSetByteArray(112, "TestTestTestTestTestTestTestTestTestTestTest".AsByteArray(), 5, 1);
		}

		[Test]
		public virtual void TestSetBitByteArrayLongerValue()
		{
			TestSetByteArray(112, "TestTestTestTestTestTestTestTestTestTestTest".AsByteArray(), 5, 20);
		}

		[Test]
		public virtual void TestSetBitByteArrayShorterValuePreparedMessage()
		{
			_msgHbiList.IsPreparedMessage = true;
			TestSetBitByteArrayShorterValue();
		}

		[Test]
		public virtual void TestSetBitByteArrayLongerValuePreparedMessage()
		{
			_msgHbiList.IsPreparedMessage = true;
			TestSetBitByteArrayLongerValue();
		}

		[Test]
		public virtual void TestSetOccurrenceBitByteArray()
		{
			var value = "TestTestTestTestTestTestTestTestTestTestTest".AsByteArray();
			_newsMessage.Set(58, 2, value, 5, 20);
			ClassicAssert.AreEqual(ReplaceFieldValue(58, 2, StringHelper.NewString(value, 5, 20), MessageNews),
				_newsMessage.ToString());
		}

		private void TestSetByteArray(int tagId, byte[] value, int offset, int length)
		{
			_msgHbiList.Set(tagId, value, offset, length);
			ClassicAssert.AreEqual(ReplaceFieldValue(tagId, StringHelper.NewString(value, offset, length), MsgHbi),
				_msgHbiList.ToString());
		}

		// int

		[Test]
		public virtual void TestSetIntegerShorterValue()
		{
			TestSetInt(112, "123", 123);
		}

		[Test]
		public virtual void TestSetIntegerLongerValue()
		{
			var msgStr = ReplaceFieldValue(112, "1", MsgHbi);
			_msgHbiList = RawFixUtil.GetFixMessage(msgStr.AsByteArray());
			TestSetInt(112, "123456789", 123456789);
		}

		[Test]
		public virtual void TestSetIntegerShorterValuePreparedMessage()
		{
			var msgStr = ReplaceFieldValue(112, "1234", MsgHbi);
			_msgHbiList = RawFixUtil.GetFixMessage(msgStr.AsByteArray());
			_msgHbiList.IsPreparedMessage = true;
			TestSetInt(112, "0012", 12);
		}

		[Test]
		public virtual void TestSetIntegerLongerValuePreparedMessage()
		{
			var msgStr = ReplaceFieldValue(112, "1", MsgHbi);
			_msgHbiList = RawFixUtil.GetFixMessage(msgStr.AsByteArray());
			_msgHbiList.IsPreparedMessage = true;
			TestSetInt(112, "123", 123);
		}

		[Test]
		public virtual void TestSetOccurrenceInteger()
		{
			var value = 1234;
			_newsMessage.Set(58, 2, (long)value);
			ClassicAssert.AreEqual(ReplaceFieldValue(58, 2, Convert.ToString(value), MessageNews), _newsMessage.ToString());
		}

		private void TestSetInt(int tagId, string expectedValue, int value)
		{
			_msgHbiList.Set(tagId, value);
			ClassicAssert.AreEqual(ReplaceFieldValue(tagId, expectedValue, MsgHbi), _msgHbiList.ToString());
		}

		// double

		[Test]
		public virtual void TestSetDoubleShorterValue()
		{
			TestSetDouble(112, "1.123", 1.123d, 3);
			TestSetDouble(112, "1.123", 1.12345d, 3);
		}

		[Test]
		public virtual void TestSetDoubleLongerValue()
		{
			var msgStr = ReplaceFieldValue(112, "1", MsgHbi);
			_msgHbiList = RawFixUtil.GetFixMessage(msgStr.AsByteArray());
			TestSetDouble(112, "1.12", 1.12d, 3);
		}

		[Test]
		public virtual void TestSetDoubleShorterValuePreparedMessage()
		{
			var msgStr = ReplaceFieldValue(112, "1.12345", MsgHbi);
			_msgHbiList = RawFixUtil.GetFixMessage(msgStr.AsByteArray());
			_msgHbiList.IsPreparedMessage = true;
			TestSetDouble(112, "001.123", 1.123d, 3);
		}

		[Test]
		public virtual void TestSetDoubleLongerValuePreparedMessage()
		{
			var msgStr = ReplaceFieldValue(112, "1", MsgHbi);
			_msgHbiList = RawFixUtil.GetFixMessage(msgStr.AsByteArray());
			_msgHbiList.IsPreparedMessage = true;
			TestSetDouble(112, "1.123", 1.123d, 3);
		}

		[Test]
		public virtual void TestSetOccurrenceDouble()
		{
			var value = 1.123d;
			_newsMessage.Set(58, 2, value, 3);
			ClassicAssert.AreEqual(ReplaceFieldValue(58, 2, "1.123", MessageNews), _newsMessage.ToString());
		}

		private void TestSetDouble(int tagId, string expectedValue, double value, int precision)
		{
			_msgHbiList.Set(tagId, value, precision);
			ClassicAssert.AreEqual(ReplaceFieldValue(tagId, expectedValue, MsgHbi), _msgHbiList.ToString());
		}

		// TIME

		[Test]
		public virtual void TestSetTimeShorterValue()
		{
			var msgStr = ReplaceFieldValue(112, "TestTestTestTestTestTestTestTestTest", MsgHbi);
			_msgHbiList = RawFixUtil.GetFixMessage(msgStr.AsByteArray());
			TestSetTime(112, _expectedDateTime, _calendarUtc, FixDateFormatterFactory.FixDateType.Time40);
		}

		[Test]
		public virtual void TestSetTimeLongerValue()
		{
			var msgStr = ReplaceFieldValue(112, "1", MsgHbi);
			_msgHbiList = RawFixUtil.GetFixMessage(msgStr.AsByteArray());
			TestSetTime(112, _expectedDateTime, _calendarUtc, FixDateFormatterFactory.FixDateType.Time40);
		}

		[Test]
		public virtual void TestSetTimeShorterValuePreparedMessage()
		{
			var msgStr = ReplaceFieldValue(112, "TestTestTestTestTestTestTestTestTest", MsgHbi);
			_msgHbiList = RawFixUtil.GetFixMessage(msgStr.AsByteArray());
			_msgHbiList.IsPreparedMessage = true;
			TestSetTime(112, _expectedDateTime, _calendarUtc, FixDateFormatterFactory.FixDateType.Time40);
		}

		[Test]
		public virtual void TestSetTimeLongerValuePreparedMessage()
		{
			var msgStr = ReplaceFieldValue(112, "1", MsgHbi);
			_msgHbiList = RawFixUtil.GetFixMessage(msgStr.AsByteArray());
			_msgHbiList.IsPreparedMessage = true;
			TestSetTime(112, _expectedDateTime, _calendarUtc, FixDateFormatterFactory.FixDateType.Time40);
		}

		[Test]
		public virtual void TestSetOccurrenceTime()
		{
			_newsMessage.SetCalendarValue(58, 2, _calendarUtc, FixDateFormatterFactory.FixDateType.Time40);
			ClassicAssert.AreEqual(ReplaceFieldValue(58, 2, _expectedDateTime, MessageNews), _newsMessage.ToString());
		}

		private void TestSetTime(int tagId, string expectedValue, DateTime value,
			FixDateFormatterFactory.FixDateType type)
		{
			_msgHbiList.SetCalendarValue(tagId, value, type);
			ClassicAssert.AreEqual(ReplaceFieldValue(tagId, expectedValue, MsgHbi), _msgHbiList.ToString());
		}

		//------------------------ GET METHOD --------------------//

		[Test]
		public virtual void TestGetAsBoolean()
		{
			_msgHbiList = RawFixUtil.GetFixMessage(ReplaceFieldValue(112, "Y", MsgHbi).AsByteArray());
			var actualValue = _msgHbiList.GetTagValueAsBool(112);
			ClassicAssert.IsTrue(actualValue);

			_msgHbiList.Set(112, false);
			actualValue = _msgHbiList.GetTagValueAsBool(112);
			ClassicAssert.IsFalse(actualValue);
		}

		[Test]
		public virtual void TestGetAsByte()
		{
			_msgHbiList = RawFixUtil.GetFixMessage(ReplaceFieldValue(112, "123", MsgHbi).AsByteArray());
			var actualValue = _msgHbiList.GetTagValueAsByte(112, 2);
			ClassicAssert.AreEqual('3', actualValue);

			_msgHbiList.Set(112, "321".AsByteArray());
			actualValue = _msgHbiList.GetTagValueAsByte(112, 2);
			ClassicAssert.AreEqual('1', actualValue);
		}

		[Test]
		public virtual void TestGetAsLong()
		{
			_msgHbiList = RawFixUtil.GetFixMessage(ReplaceFieldValue(112, "123", MsgHbi).AsByteArray());
			var actualValue = _msgHbiList.GetTagValueAsLong(112);
			ClassicAssert.AreEqual(123, actualValue);

			_msgHbiList.Set(112, 321);
			actualValue = _msgHbiList.GetTagValueAsLong(112);
			ClassicAssert.AreEqual(321, actualValue);
		}

		[Test]
		public virtual void TestGetAsDouble()
		{
			_msgHbiList = RawFixUtil.GetFixMessage(ReplaceFieldValue(112, "1.23", MsgHbi).AsByteArray());
			var actualValue = _msgHbiList.GetTagValueAsDouble(112);
			ClassicAssert.AreEqual(1.23d, actualValue, 1e-15);

			_msgHbiList.Set(112, 3.21d, 2);
			actualValue = _msgHbiList.GetTagValueAsDouble(112);
			ClassicAssert.AreEqual(3.21d, actualValue, 1e-15);
		}

		[Test]
		public virtual void TestGetTagForByteBuffer()
		{
			var bb = new ByteBuffer();
			_newsMessage.GetTagValueAtIndex(_newsMessage.GetTagIndex(49), bb);
			_newsMessage.GetTagValueAtIndex(_newsMessage.GetTagIndex(56), bb);
			ClassicAssert.AreEqual("targetsender".Length, bb.Offset);
			ClassicAssert.AreEqual("targetsender", StringHelper.NewString(bb.GetBulk()));
		}

		[Test]
		public virtual void TestIsTagValueEqual()
		{
			ClassicAssert.IsTrue(_newsMessage.IsTagValueEqual(49, "target".AsByteArray()));
			ClassicAssert.IsFalse(_newsMessage.IsTagValueEqual(56, "target".AsByteArray()));
			//unknown tag
			ClassicAssert.IsFalse(_newsMessage.IsTagValueEqual(50000, "target".AsByteArray()));
		}

		[Test]
		public virtual void TestHasTagValue()
		{
			_msgHbiList = RawFixUtil.GetFixMessage(ReplaceFieldValue(112, "123", MsgHbi).AsByteArray());
			ClassicAssert.IsTrue(_msgHbiList.HasTagValue(112));

			//check empty value
			_msgHbiList = RawFixUtil.GetFixMessage(ReplaceFieldValue(112, "", MsgHbi).AsByteArray());
			ClassicAssert.IsFalse(_msgHbiList.HasTagValue(112));

			//check spaces
			_msgHbiList = RawFixUtil.GetFixMessage(ReplaceFieldValue(112, " ", MsgHbi).AsByteArray());
			ClassicAssert.IsTrue(_msgHbiList.HasTagValue(112));

			//check unknown tag
			ClassicAssert.IsFalse(_msgHbiList.HasTagValue(50000));
		}

		//------------------------ OLD API METHOD --------------------//

		[Test]
		public virtual void TestSplit()
		{
			var splitList = _newsMessage.Split(58);
			ClassicAssert.AreEqual(2, splitList.Count);

			var groupEntry = splitList[0];
			ClassicAssert.AreEqual(1, groupEntry.Length);
			ClassicAssertEqualsString(58, "1", groupEntry);

			groupEntry = splitList[1];
			// without dictionary can't detect end of RG entry
			//        ClassicAssertEquals(1, groupEntry.size());
			ClassicAssertEqualsString(58, "2", groupEntry);
		}

		[Test]
		public virtual void TestGetMsgType()
		{
			ClassicAssert.That("0".AsByteArray(), Is.EquivalentTo(_msgHbiList.MsgType));
			_msgHbiList.Set(35, "AB".AsByteArray());
			ClassicAssert.That("AB".AsByteArray(), Is.EquivalentTo(_msgHbiList.MsgType));
		}

		[Test]
		public virtual void TestGetMsgSeqNumber()
		{
			ClassicAssert.AreEqual(15, _msgHbiList.MsgSeqNumber);
		}

		[Test]
		public virtual void TestGetIndex()
		{
			ClassicAssert.AreEqual(35, _msgHbiList[2].TagId);
		}

		[Test]
		public virtual void TestClone()
		{
			var clone = (FixMessage)_msgHbiList.Clone();
			ClassicAssert.AreEqual(_msgHbiList, clone);
			ClassicAssert.AreEqual(_msgHbiList.ToString(), clone.ToString());
			ClassicAssert.IsFalse(ReferenceEquals(_msgHbiList, clone));
		}

		[Test]
		public virtual void TestAddList()
		{
			var msg = new FixMessage();
			msg.AddTag(1, "1");
			msg.AddTag(2, "2");

			var list = new List<TagValue>
			{
				new TagValue(3, "3"),
				new TagValue(4, "4")
			};

			msg.AddAll(list);

			ClassicAssert.AreEqual("1=1\u00012=2\u00013=3\u00014=4\u0001", msg.ToString());
		}

		[Test]
		public virtual void TestGetTag()
		{
			var f = _msgHbiList.GetTag(35);
			ClassicAssert.IsNotNull(f);
			ClassicAssert.That(f.Value, Is.EquivalentTo("0".AsByteArray()));
		}

		[Test]
		public virtual void TestGetTagIfFieldNotExist()
		{
			ClassicAssert.IsNull(_msgHbiList.GetTag(555));
		}

		[Test]
		public virtual void TestGetTagOccurrence()
		{
			var f = _newsMessage.GetTag(58, 2);
			ClassicAssert.IsNotNull(f);
			ClassicAssert.That(f.Value, Is.EquivalentTo("2".AsByteArray()));
		}

		[Test]
		public virtual void TestGetTagOccurrenceIfFieldNotExist()
		{
			ClassicAssert.IsNull(_msgHbiList.GetTag(555, 2));
		}

		[Test]
		public virtual void TestGetTagOccurrenceOutOfOccurrence()
		{
			ClassicAssert.IsNull(_msgHbiList.GetTag(58, 3));
		}

		[Test]
		public virtual void AddByteArray()
		{
			_msgHbiList.AddTag(777, "ABC".AsByteArray());
			ClassicAssert.AreEqual(MsgHbi + "777=ABC\u0001", _msgHbiList.ToString());
		}

		[Test]
		public virtual void AddStringField()
		{
			_msgHbiList.AddTag(777, "ABC");
			ClassicAssert.AreEqual(MsgHbi + "777=ABC\u0001", _msgHbiList.ToString());
		}

		[Test]
		public virtual void TestGetStringValue()
		{
			ClassicAssert.AreEqual("123456789", _msgHbiList.GetTagValueAsString(112));
		}

		[Test]
		public virtual void TestGetStringValueIfFieldNotExist()
		{
			ClassicAssert.IsNull(_msgHbiList.GetTagValueAsString(555));
		}

		[Test]
		public virtual void TestToByteArray()
		{
			ClassicAssert.That(_msgHbiList.AsByteArray(), Is.EquivalentTo(MsgHbi.AsByteArray()));
		}

		[Test]
		public virtual void TestToByteArrayAndReturnNextPosition()
		{
			var array = new byte[1000];
			var offset = 100;
			var nextPos = offset + MsgHbi.Length;
			ClassicAssert.AreEqual(nextPos, _msgHbiList.ToByteArrayAndReturnNextPosition(array, offset));
			ClassicAssert.AreEqual(MsgHbi, StringHelper.NewString(array, offset, MsgHbi.Length));
		}

		[Test]
		public virtual void TestToByteArrayAndReturnNextPositionExcludedField()
		{
			var msg = new FixMessage();
			msg.AddTag(8, 44L);
			msg.AddTag(9, 50L);
			msg.AddTag(10, 150L);
			var array = new byte[100];
			var offset = 10;

			var expectedMsg = "8=44\u000110=150\u0001";
			var nextPos = offset + expectedMsg.Length;
			int[] excludedFields = { 9 };
			ClassicAssert.AreEqual(nextPos, msg.ToByteArrayAndReturnNextPosition(array, offset, excludedFields));
			ClassicAssert.AreEqual(expectedMsg, StringHelper.NewString(array, offset, expectedMsg.Length));
		}

		[Test]
		public virtual void TestGetRawLength()
		{
			ClassicAssert.AreEqual(MsgHbi.Length, _msgHbiList.RawLength);
		}

		[Test]
		public virtual void TestGetRawLengthAddField()
		{
			_msgHbiList.AddTag(1, (long)1);
			ClassicAssert.AreEqual(MsgHbi.Length + 4, _msgHbiList.RawLength);
		}

		[Test]
		public virtual void TestGetRawLengthAddFieldInPreparedMsg()
		{
			_msgHbiList.AddTag(1, (long)1);
			_msgHbiList.IsPreparedMessage = true;
			ClassicAssert.AreEqual(MsgHbi.Length + 4, _msgHbiList.RawLength);
		}

		[Test]
		public virtual void TestGetRawLengthUpdateField()
		{
			var lengthBeforeUpdate = _msgHbiList.GetTagLength(112);
			_msgHbiList.Set(112, 1);
			ClassicAssert.AreEqual(MsgHbi.Length - lengthBeforeUpdate + 1, _msgHbiList.RawLength);
		}

		[Test]
		public virtual void TestGetRawLengthUpdateFieldInPreparedMsg()
		{
			_msgHbiList.IsPreparedMessage = true;
			_msgHbiList.Set(112, 1);
			ClassicAssert.AreEqual(MsgHbi.Length, _msgHbiList.RawLength,
				"In prepared message length must be without change");
		}

		[Test]
		public virtual void TestGetRawLengthUpdateLongFieldInPreparedMsg()
		{
			var lengthBeforeUpdate = _msgHbiList.GetTagLength(112);
			_msgHbiList.IsPreparedMessage = true;
			var newValue = "1234567890123456";
			_msgHbiList.Set(112, newValue.AsByteArray());
			ClassicAssert.AreEqual(MsgHbi.Length - lengthBeforeUpdate + newValue.Length, _msgHbiList.RawLength);
		}

		[Test]
		public virtual void TestGetMsgVersion()
		{
			ClassicAssert.AreEqual(FixVersion.Fix44, _msgHbiList.MsgVersion);
		}

		[Test]
		public virtual void TestAddFixField()
		{
			_msgHbiList.AddTag(55, "VV");
			ClassicAssert.AreEqual(MsgHbi + "55=VV\u0001", _msgHbiList.ToString());
		}
		//------------------------ LIST API --------------------//

		[Test]
		public virtual void TestSetAtIndexFixField()
		{
			_msgHbiList[0] = new TagValue(55, "VV");
			ClassicAssert.AreEqual(MsgHbi.Replace("8=FIX.4.4", "55=VV"), _msgHbiList.ToString());
		}

		[Test]
		public virtual void TestAddAtEndFixField()
		{
			_msgHbiList.AddTag(55, "VV");
			ClassicAssert.AreEqual(MsgHbi + "55=VV\u0001", _msgHbiList.ToString());
		}

		[Test]
		public virtual void TestAddAtIndexFixField()
		{
			_msgHbiList.AddTagAtIndex(0, 55, "VV");
			ClassicAssert.AreEqual("55=VV\u0001" + MsgHbi, _msgHbiList.ToString());
		}

		[Test]
		public virtual void TestAddCollection() //TODO: is it duplicate of TestAddList??
		{
			var msg = new FixMessage();
			msg.AddTag(1, "1");
			msg.AddTag(2, "2");

			var list = new List<TagValue>();
			list.Add(new TagValue(3, "3"));
			list.Add(new TagValue(4, "4"));

			msg.AddAll(list);

			ClassicAssert.AreEqual("1=1\u00012=2\u00013=3\u00014=4\u0001", msg.ToString());
		}

		[Test]
		public virtual void TestAddMessageList()
		{
			var otherList = new FixMessage();
			otherList.AddTag(1, (long)1);
			otherList.AddTag(2, (long)2);
			_msgHbiList.Add(otherList);
			ClassicAssert.AreEqual(MsgHbi + "1=1\u00012=2\u0001", _msgHbiList.ToString());
		}

		[Test]
		public virtual void TestAddCollectionAtIndex()
		{
			var msg = new FixMessage();
			msg.AddTag(1, "1");
			msg.AddTag(2, "2");

			var list = new List<TagValue>
			{
				new TagValue(3, "3"),
				new TagValue(4, "4")
			};

			msg.AddAll(1, list);

			ClassicAssert.AreEqual("1=1\u00013=3\u00014=4\u00012=2\u0001", msg.ToString());
		}

		[Test]
		public virtual void TestGetTagValueByIndex()
		{
			ClassicAssert.AreEqual(new TagValue(9, "66"), _msgHbiList[1]);
		}

		[Test]
		public virtual void TestContains()
		{
			ClassicAssert.IsTrue(_msgHbiList.Contains(new TagValue(9, "66")));
			ClassicAssert.IsFalse(_msgHbiList.Contains(new TagValue(777, "11")));
		}

		[Test]
		public virtual void TestIndexOf()
		{
			ClassicAssert.AreEqual(1, _msgHbiList.IndexOf(new TagValue(9, "66")));
		}

		[Test]
		public virtual void TestRemoveAtIndex()
		{
			for (var i = 0; i < _msgHbiList.Length; i++)
			{
				ClassicAssert.AreEqual(
					_msgHbiList.GetTagValueAsStringAtIndex(i),
					_msgHbiList[i].StringValue,
					"Tag " + _msgHbiList.GetTagIdAtIndex(i) + " has invalid cache in message " + _msgHbiList.ToPrintableString());
			}

			var sizeBefore = _msgHbiList.Length;
			var removedField = _msgHbiList.Remove(0);
			ClassicAssert.AreEqual(new TagValue(8, "FIX.4.4"), removedField);
			ClassicAssert.AreEqual(sizeBefore, _msgHbiList.Length + 1);
			ClassicAssert.AreEqual(MsgHbi.Replace("8=FIX.4.4\u0001", ""), _msgHbiList.ToString());

			for (var i = 0; i < _msgHbiList.Length; i++)
			{
				ClassicAssert.AreEqual(
					_msgHbiList.GetTagValueAsStringAtIndex(i),
					_msgHbiList[i].StringValue,
					"Tag " + _msgHbiList.GetTagIdAtIndex(i) + " has invalid cache in message " + _msgHbiList.ToPrintableString());
			}

			//check that cache correctly cleaned - add tag
			_msgHbiList.AddTag(1111, "1111");
			for (var i = 0; i < _msgHbiList.Length; i++)
			{
				ClassicAssert.AreEqual(
					_msgHbiList.GetTagValueAsStringAtIndex(i),
					_msgHbiList[i].StringValue,
					"Tag " + _msgHbiList.GetTagIdAtIndex(i) + " has invalid cache in message " + _msgHbiList.ToPrintableString());
			}
		}

		[Test]
		public virtual void TestRemoveTagId()
		{
			var sizeBefore = _msgHbiList.Length;
			ClassicAssert.IsTrue(_msgHbiList.RemoveTag(8));
			ClassicAssert.AreEqual(sizeBefore, _msgHbiList.Length + 1);
			ClassicAssert.AreEqual(MsgHbi.Replace("8=FIX.4.4\u0001", ""), _msgHbiList.ToString());
		}

		[Test]
		public virtual void TestRemoveTagIdWithInnerHashReindex()
		{
			var msg =
				"8=FIX.4.4\u00019=64\u000135=A\u000149=TA\u000156=Darcy\u000134=1\u000152=20081225-14:54:31\u000198=1\u0001108=60\u0001141=Y\u000110=240\u0001";
			var fixList = RawFixUtil.GetFixMessage(msg.AsByteArray());
			var sizeBefore = fixList.Length;

			ClassicAssert.IsTrue(fixList.RemoveTag(35));
			ClassicAssert.AreEqual(sizeBefore, fixList.Length + 1);
			ClassicAssert.AreEqual(msg.Replace("35=A\u0001", ""), fixList.ToString());
			// after remove tag 35 for this message will be reindex inner hash index
			ClassicAssert.IsTrue(fixList.IsTagExists(98), "After reindex tag 98 is invisible");
		}

		[Test]
		public virtual void TestRemoveTagIdWithInnerHashReindex2()
		{
			var msg =
				"8=FIX.4.4\u00019=64\u000135=A\u000149=TA\u000156=Darcy\u000134=1\u000152=20081225-14:54:31\u000198=1\u0001108=60\u0001141=Y\u000110=240\u0001";
			var fixList = RawFixUtil.GetFixMessage(msg.AsByteArray());
			var sizeBefore = fixList.Length;

			ClassicAssert.IsTrue(fixList.RemoveTag(34));
			ClassicAssert.AreEqual(sizeBefore, fixList.Length + 1);
			ClassicAssert.AreEqual(msg.Replace("34=1\u0001", ""), fixList.ToString());
			// after remove tag 35 for this message will be reindex inner hash index
			ClassicAssert.IsTrue(fixList.IsTagExists(98), "After reindex tag 98 is invisible");
		}

		[Test]
		public virtual void TestRemoveTagIdAfterClone()
		{
			var sizeBefore = _msgHbiList.Length;
			var clonedList = _msgHbiList.DeepClone(true, false);
			ClassicAssert.IsTrue(clonedList.RemoveTag(8));
			ClassicAssert.AreEqual(sizeBefore, clonedList.Length + 1);
			ClassicAssert.AreEqual(MsgHbi.Replace("8=FIX.4.4\u0001", ""), clonedList.ToString());
		}

		[Test]
		public virtual void TestRemoveFromRg()
		{
			var list = new FixMessage();
			list.AddTag(1, (long)1);
			list.AddTag(2, (long)22);
			list.AddTag(2, 333);
			list.RemoveTag(2); // remove first occurrence of tagId=2
			ClassicAssert.IsTrue(list.IsTagExists(2), "can't find tag after operation remove"); // find tagId=2
			ClassicAssert.AreEqual(1, list.GetTagIndex(2)); // find tagId=2
			ClassicAssert.AreEqual(333, list.GetTagAsInt(2));
			list.RemoveTag(2); // remove second occurrence of tagId=2
			ClassicAssert.IsFalse(list.IsTagExists(2));
		}

		[Test]
		public virtual void TestRemoveWithOccurrence()
		{
			var msgStr = "8=FIX.4.4\x00019=2363\x000135=8\x000134=973\x000149=Endpoint_1\x000156=Endpoint_2\x0001" +
						"52=20170412-21:30:00.454\x0001" + "555=2\x0001" + "600=NQ_FM7.CM\x0001611=20170616\x0001" +
						"600=ES_SESM7_ESU7.CM\x0001611=20170616\x0001" + "75=20170616\x0001";
			var msg = RawFixUtil.GetFixMessage(msgStr.AsByteArray());
			msg.RemoveTag(611, 2);
			//check that hash is still correct to address last tag
			ClassicAssert.AreEqual("20170616", msg.GetTagValueAsString(75));
			ClassicAssert.AreEqual(
				"8=FIX.4.4 | 9=2363 | 35=8 | 34=973 | 49=Endpoint_1 | 56=Endpoint_2 | 52=20170412-21:30:00.454 |" +
				" 555=2 | 600=NQ_FM7.CM | 611=20170616 | 600=ES_SESM7_ESU7.CM | 75=20170616 | ",
				msg.ToPrintableString());
		}

		[Test]
		public virtual void TestRemoveTagIdFromLongMsg()
		{
			var longMsgStr =
				"8=FIXT.1.1\u00019=5529\u000135=AE\u00011128=9\u000149=STRING\u000156=STRING\u0001115=STRING\u0001128=STRING\u000190=1\u000191=D\u000134=1\u000150=STRING\u0001142=STRING\u000157=STRING\u0001143=STRING\u0001116=STRING\u0001144=STRING\u0001129=STRING\u0001145=STRING\u000143=N\u000197=N\u000152=20010101-01:01:01.001\u0001122=20010101-01:01:01.001\u0001212=1\u0001213=D\u0001347=STRING\u0001369=1\u0001627=1\u0001628=STRING\u0001629=20010101-01:01:01.001\u0001630=1\u00011129=STRING\u00011180=STRING\u00011181=1\u00011350=1\u00011352=Y\u0001571=STRING\u00011003=STRING\u00011040=STRING\u00011041=STRING\u00011042=STRING\u0001487=0\u0001856=0\u0001939=0\u0001568=STRING\u0001828=0\u0001829=0\u0001855=0\u00011123=0\u00011124=0\u00011125=20010101\u00011126=STRING\u00011127=STRING\u0001830=STRING\u0001150=0\u0001748=1\u0001912=N\u0001325=N\u0001263=0\u0001572=STRING\u0001881=STRING\u0001818=STRING\u0001820=STRING\u0001880=STRING\u000117=STRING\u0001527=STRING\u0001378=0\u0001570=N\u0001423=1\u00011116=1\u00011117=STRING\u00011118=B\u00011119=1\u00011120=1\u00011121=STRING\u00011122=1\u00011015=0\u0001716=ITD\u0001717=STRING\u00011430=E\u00011300=STRING\u00011301=DSMD\u000155=STRING\u000165=STRING\u000148=STRING\u000122=1\u0001454=1\u0001455=STRING\u0001456=1\u0001460=1\u00011227=STRING\u00011151=STRING\u0001461=STRING\u0001167=FUT\u0001762=STRING\u0001200=200101w2\u0001541=20010101\u00011079=01:01:01.001+03:30\u0001966=STRING\u00011049=R\u0001965=1\u0001224=20010101\u00011449=FR\u00011450=SD\u00011451=1.1\u00011452=1.1\u00011457=1.1\u00011458=1.1\u0001225=20010101\u0001239=FUT\u0001226=1\u0001227=1.1\u0001228=1.1\u0001255=STRING\u0001543=STRING\u0001470=AF\u0001471=STRING\u0001472=STRING\u0001240=20010101\u0001202=1.1\u0001947=AFA\u0001967=1.1\u0001968=1.1\u00011478=1\u00011479=1\u00011480=1.1\u00011481=1\u0001206=C\u0001231=1.1\u00011435=0\u00011439=0\u0001969=1.1\u00011146=1.1\u0001996=Bbl\u00011147=1.1\u00011191=Bbl\u00011192=1.1\u00011193=C\u00011194=0\u00011482=1\u00011195=1.1\u00011196=STD\u00011197=EQTY\u00011198=0\u00011199=1.1\u00011200=1.1\u0001201=0\u00011244=Y\u00011242=Y\u0001997=H\u0001223=1.1\u0001207=DSMD\u0001970=1\u0001971=1\u0001106=STRING\u0001348=1\u0001349=D\u0001107=STRING\u0001350=1\u0001351=D\u00011184=1\u00011185=X\u00011186=STRING\u0001691=STRING\u0001667=200101w2\u0001875=1\u0001876=STRING\u0001864=1\u0001865=1\u0001866=20010101\u00011145=20010101-01:01:01.001\u0001867=1.1\u0001868=STRING\u0001873=20010101\u0001874=20010101\u00011018=1\u00011019=STRING\u00011050=B\u00011051=1\u00011052=1\u00011053=STRING\u00011054=1\u00011483=1\u00011484=1\u00011485=1.1\u00011486=1.1\u00011487=1\u00011488=1.1\u00011489=1\u00011490=1\u00011491=1\u00011492=20010101-01:01:01.001\u00011493=20010101-01:01:01.001\u00011494=1\u00011495=01:01:01.001\u00011496=01:01:01.001\u0001913=STRING\u0001914=STRING\u0001915=20010101\u0001918=AFA\u0001788=1\u0001916=20010101\u0001917=20010101\u0001919=0\u0001898=1.1\u0001854=0\u0001235=AFTERTAX\u0001236=1.1\u0001701=20010101\u0001696=20010101\u0001697=1.1\u0001698=1\u0001711=1\u0001311=STRING\u0001312=STRING\u0001309=STRING\u0001305=1\u0001457=1\u0001458=STRING\u0001459=1\u0001462=1\u0001463=STRING\u0001310=FUT\u0001763=STRING\u0001313=200101w2\u0001542=20010101\u00011213=01:01:01.001+03:30\u0001241=20010101\u00011453=FR\u00011454=SD\u00011455=1.1\u00011456=1.1\u00011459=1.1\u00011460=1.1\u0001242=20010101\u0001243=FUT\u0001244=1\u0001245=1.1\u0001246=1.1\u0001256=STRING\u0001595=STRING\u0001592=AF\u0001593=STRING\u0001594=STRING\u0001247=20010101\u0001316=1.1\u0001941=AFA\u0001317=C\u0001436=1.1\u00011437=0\u00011441=0\u0001998=Bbl\u00011423=1.1\u00011424=Bbl\u00011425=1.1\u00011000=H\u00011419=0\u0001435=1.1\u0001308=DSMD\u0001306=STRING\u0001362=1\u0001363=D\u0001307=STRING\u0001364=1\u0001365=D\u0001877=STRING\u0001878=STRING\u0001972=1.1\u0001318=AFA\u0001879=1.1\u0001975=2\u0001973=1.1\u0001974=FIXED\u0001810=1.1\u0001882=1.1\u0001883=1.1\u0001884=1.1\u0001885=1.1\u0001886=1.1\u0001887=1\u0001888=AMT\u0001889=STRING\u00011044=1.1\u00011045=1.1\u00011046=D\u00011038=1.1\u00011058=1\u00011059=STRING\u00011060=B\u00011061=1\u00011062=1\u00011063=STRING\u00011064=1\u00011039=STRING\u0001315=0\u0001822=STRING\u0001823=STRING\u000132=1.1\u000131=1.1\u00011056=1.1\u000115=AFA\u0001120=AFA\u0001669=1.1\u0001194=1.1\u0001195=1.1\u00011071=1.1\u000130=DSMD\u000175=20010101\u0001715=20010101\u00016=1.1\u0001218=1.1\u0001220=AFA\u0001221=EONIA\u0001222=STRING\u0001662=1.1\u0001663=1\u0001699=STRING\u0001761=1\u0001819=0\u0001753=1\u0001707=CASH\u0001708=1.1\u00011055=STRING\u0001442=1\u0001824=STRING\u0001555=1\u0001600=STRING\u0001601=STRING\u0001602=STRING\u0001603=1\u0001604=1\u0001605=STRING\u0001606=1\u0001607=1\u0001608=STRING\u0001609=FUT\u0001764=STRING\u0001610=200101w2\u0001611=20010101\u00011212=01:01:01.001+03:30\u0001248=20010101\u0001249=20010101\u0001250=FUT\u0001251=1\u0001252=1.1\u0001253=1.1\u0001257=STRING\u0001599=STRING\u0001596=AF\u0001597=STRING\u0001598=STRING\u0001254=20010101\u0001612=1.1\u0001942=AFA\u0001613=C\u0001614=1.1\u00011436=0\u00011440=0\u0001999=Bbl\u00011224=1.1\u00011421=Bbl\u00011422=1.1\u00011001=H\u00011420=0\u0001615=1.1\u0001616=DSMD\u0001617=STRING\u0001618=1\u0001619=D\u0001620=STRING\u0001621=1\u0001622=D\u0001623=1.1\u0001624=1\u0001556=AFA\u0001740=STRING\u0001739=20010101\u0001955=200101w2\u0001956=20010101\u00011358=0\u00011017=-1\u0001566=1.1\u0001687=1.1\u0001690=1\u0001990=STRING\u00011152=1\u0001683=1\u0001688=AMT\u0001689=STRING\u0001564=C\u0001565=0\u0001539=1\u0001524=STRING\u0001525=B\u0001538=1\u0001804=1\u0001545=STRING\u0001805=1\u0001654=STRING\u0001587=0\u0001588=20010101\u0001637=1.1\u0001675=AFA\u00011073=1.1\u00011074=1.1\u00011075=1.1\u00011379=1.1\u00011381=1.1\u00011383=1.1\u00011384=0 1\u00011418=1.1\u00011342=1\u00011330=STRING\u00011331=STRING\u00011332=STRING\u00011333=1\u00011334=1\u00011335=STRING\u00011336=1\u00011344=STRING\u00011337=FUT\u00011338=STRING\u00011339=200101w2\u00011345=20010101\u00011405=01:01:01.001+03:30\u00011340=1.1\u00011391=C\u00011343=0\u00011341=DSMD\u00011392=STRING\u000160=20010101-01:01:01.001\u0001768=1\u0001769=20010101-01:01:01.001\u0001770=1\u0001771=STRING\u00011033=A\u00011034=1\u00011035=ADD AON\u000163=0\u000164=20010101\u0001987=20010101\u0001573=0\u0001574=M3\u0001552=1\u000154=1\u00011427=STRING\u00011428=1\u00011429=0\u00011009=1\u00011005=STRING\u00011006=STRING\u00011007=STRING\u000183=1\u00011008=0\u0001430=1\u00011154=AFA\u00011155=AFA\u0001453=1\u0001448=STRING\u0001447=B\u0001452=1\u0001802=1\u0001523=STRING\u0001803=1\u00011=STRING\u0001660=1\u0001581=1\u000181=0\u0001575=N\u0001576=1\u0001577=0\u0001578=STRING\u0001579=STRING\u0001376=STRING\u0001377=N\u0001582=1\u0001336=1\u0001625=1\u0001943=STRING\u000112=1.1\u000113=1\u0001479=AFA\u0001497=N\u0001157=1\u0001230=20010101\u0001158=1.1\u0001159=1.1\u0001738=1.1\u0001920=1.1\u0001921=1.1\u0001922=1.1\u0001238=1.1\u0001237=1.1\u0001118=1.1\u0001119=1.1\u0001155=1.1\u0001156=M\u000177=C\u000158=STRING\u0001354=1\u0001355=D\u0001752=1\u0001518=1\u0001519=1\u0001520=1.1\u0001521=AFA\u0001232=1\u0001233=AMT\u0001234=STRING\u0001136=1\u0001137=1.1\u0001138=AFA\u0001139=1\u0001891=0\u0001825=STRING\u0001826=0\u0001591=0\u000170=STRING\u000178=1\u000179=STRING\u0001661=1\u0001736=AFA\u0001467=STRING\u0001756=1\u0001757=STRING\u0001758=B\u0001759=1\u0001806=1\u0001760=STRING\u0001807=1\u000180=1.1\u0001993=STRING\u00011002=1\u0001989=STRING\u00011136=STRING\u00011016=1\u00011012=20010101-01:01:01.001\u00011013=1\u00011014=STRING\u00011158=1\u00011164=1\u0001781=1\u0001782=STRING\u0001783=B\u0001784=1\u0001801=1\u0001785=STRING\u0001786=1\u00011072=1.1\u00011057=Y\u00011139=STRING\u00011115=1\u00011444=1\u000137=STRING\u0001198=STRING\u000111=STRING\u0001526=STRING\u000166=STRING\u00011080=STRING\u00011081=0\u00011431=0\u000140=1\u000144=1.1\u000199=1.1\u000118=0 1\u000139=0\u000138=1.1\u0001152=1.1\u0001516=1.1\u0001468=0\u0001469=1.1\u0001151=1.1\u000114=1.1\u000159=0\u0001126=20010101-01:01:01.001\u00011138=1.1\u00011082=1.1\u00011083=1\u00011084=1\u00011085=1.1\u00011086=1.1\u00011087=1.1\u00011088=1.1\u0001528=A\u0001529=1 2\u0001775=0\u00011432=1\u0001821=STRING\u00011093=1\u0001483=20010101-01:01:01.001\u0001586=20010101-01:01:01.001\u00011188=1.1\u00011380=1.1\u00011190=1.1\u00011382=1.1\u0001797=Y\u00011387=1\u00011388=1\u00011389=Y\u0001852=N\u00011390=0\u0001853=0\u0001994=STRING\u00011011=STRING\u0001779=20010101-01:01:01.001\u0001991=1.1\u00011132=20010101-01:01:01.001+03:30\u00011134=Y\u0001381=1.1\u00011328=STRING\u00011329=1.1\u000193=1\u000189=D\u000110=219\u0001";
			var list = RawFixUtil.GetFixMessage(longMsgStr.AsByteArray());

			var sizeBefore = list.Length;
			ClassicAssert.IsTrue(list.RemoveTag(8));
			ClassicAssert.AreEqual(sizeBefore, list.Length + 1);
			ClassicAssert.AreEqual(longMsgStr.Replace("8=FIXT.1.1\u0001", ""), list.ToString());
		}

		[Test]
		public virtual void TestRemoveTagIdFromClonedLongMsg()
		{
			var longMsgStr =
				"8=FIXT.1.1\u00019=5529\u000135=AE\u00011128=9\u000149=STRING\u000156=STRING\u0001115=STRING\u0001128=STRING\u000190=1\u000191=D\u000134=1\u000150=STRING\u0001142=STRING\u000157=STRING\u0001143=STRING\u0001116=STRING\u0001144=STRING\u0001129=STRING\u0001145=STRING\u000143=N\u000197=N\u000152=20010101-01:01:01.001\u0001122=20010101-01:01:01.001\u0001212=1\u0001213=D\u0001347=STRING\u0001369=1\u0001627=1\u0001628=STRING\u0001629=20010101-01:01:01.001\u0001630=1\u00011129=STRING\u00011180=STRING\u00011181=1\u00011350=1\u00011352=Y\u0001571=STRING\u00011003=STRING\u00011040=STRING\u00011041=STRING\u00011042=STRING\u0001487=0\u0001856=0\u0001939=0\u0001568=STRING\u0001828=0\u0001829=0\u0001855=0\u00011123=0\u00011124=0\u00011125=20010101\u00011126=STRING\u00011127=STRING\u0001830=STRING\u0001150=0\u0001748=1\u0001912=N\u0001325=N\u0001263=0\u0001572=STRING\u0001881=STRING\u0001818=STRING\u0001820=STRING\u0001880=STRING\u000117=STRING\u0001527=STRING\u0001378=0\u0001570=N\u0001423=1\u00011116=1\u00011117=STRING\u00011118=B\u00011119=1\u00011120=1\u00011121=STRING\u00011122=1\u00011015=0\u0001716=ITD\u0001717=STRING\u00011430=E\u00011300=STRING\u00011301=DSMD\u000155=STRING\u000165=STRING\u000148=STRING\u000122=1\u0001454=1\u0001455=STRING\u0001456=1\u0001460=1\u00011227=STRING\u00011151=STRING\u0001461=STRING\u0001167=FUT\u0001762=STRING\u0001200=200101w2\u0001541=20010101\u00011079=01:01:01.001+03:30\u0001966=STRING\u00011049=R\u0001965=1\u0001224=20010101\u00011449=FR\u00011450=SD\u00011451=1.1\u00011452=1.1\u00011457=1.1\u00011458=1.1\u0001225=20010101\u0001239=FUT\u0001226=1\u0001227=1.1\u0001228=1.1\u0001255=STRING\u0001543=STRING\u0001470=AF\u0001471=STRING\u0001472=STRING\u0001240=20010101\u0001202=1.1\u0001947=AFA\u0001967=1.1\u0001968=1.1\u00011478=1\u00011479=1\u00011480=1.1\u00011481=1\u0001206=C\u0001231=1.1\u00011435=0\u00011439=0\u0001969=1.1\u00011146=1.1\u0001996=Bbl\u00011147=1.1\u00011191=Bbl\u00011192=1.1\u00011193=C\u00011194=0\u00011482=1\u00011195=1.1\u00011196=STD\u00011197=EQTY\u00011198=0\u00011199=1.1\u00011200=1.1\u0001201=0\u00011244=Y\u00011242=Y\u0001997=H\u0001223=1.1\u0001207=DSMD\u0001970=1\u0001971=1\u0001106=STRING\u0001348=1\u0001349=D\u0001107=STRING\u0001350=1\u0001351=D\u00011184=1\u00011185=X\u00011186=STRING\u0001691=STRING\u0001667=200101w2\u0001875=1\u0001876=STRING\u0001864=1\u0001865=1\u0001866=20010101\u00011145=20010101-01:01:01.001\u0001867=1.1\u0001868=STRING\u0001873=20010101\u0001874=20010101\u00011018=1\u00011019=STRING\u00011050=B\u00011051=1\u00011052=1\u00011053=STRING\u00011054=1\u00011483=1\u00011484=1\u00011485=1.1\u00011486=1.1\u00011487=1\u00011488=1.1\u00011489=1\u00011490=1\u00011491=1\u00011492=20010101-01:01:01.001\u00011493=20010101-01:01:01.001\u00011494=1\u00011495=01:01:01.001\u00011496=01:01:01.001\u0001913=STRING\u0001914=STRING\u0001915=20010101\u0001918=AFA\u0001788=1\u0001916=20010101\u0001917=20010101\u0001919=0\u0001898=1.1\u0001854=0\u0001235=AFTERTAX\u0001236=1.1\u0001701=20010101\u0001696=20010101\u0001697=1.1\u0001698=1\u0001711=1\u0001311=STRING\u0001312=STRING\u0001309=STRING\u0001305=1\u0001457=1\u0001458=STRING\u0001459=1\u0001462=1\u0001463=STRING\u0001310=FUT\u0001763=STRING\u0001313=200101w2\u0001542=20010101\u00011213=01:01:01.001+03:30\u0001241=20010101\u00011453=FR\u00011454=SD\u00011455=1.1\u00011456=1.1\u00011459=1.1\u00011460=1.1\u0001242=20010101\u0001243=FUT\u0001244=1\u0001245=1.1\u0001246=1.1\u0001256=STRING\u0001595=STRING\u0001592=AF\u0001593=STRING\u0001594=STRING\u0001247=20010101\u0001316=1.1\u0001941=AFA\u0001317=C\u0001436=1.1\u00011437=0\u00011441=0\u0001998=Bbl\u00011423=1.1\u00011424=Bbl\u00011425=1.1\u00011000=H\u00011419=0\u0001435=1.1\u0001308=DSMD\u0001306=STRING\u0001362=1\u0001363=D\u0001307=STRING\u0001364=1\u0001365=D\u0001877=STRING\u0001878=STRING\u0001972=1.1\u0001318=AFA\u0001879=1.1\u0001975=2\u0001973=1.1\u0001974=FIXED\u0001810=1.1\u0001882=1.1\u0001883=1.1\u0001884=1.1\u0001885=1.1\u0001886=1.1\u0001887=1\u0001888=AMT\u0001889=STRING\u00011044=1.1\u00011045=1.1\u00011046=D\u00011038=1.1\u00011058=1\u00011059=STRING\u00011060=B\u00011061=1\u00011062=1\u00011063=STRING\u00011064=1\u00011039=STRING\u0001315=0\u0001822=STRING\u0001823=STRING\u000132=1.1\u000131=1.1\u00011056=1.1\u000115=AFA\u0001120=AFA\u0001669=1.1\u0001194=1.1\u0001195=1.1\u00011071=1.1\u000130=DSMD\u000175=20010101\u0001715=20010101\u00016=1.1\u0001218=1.1\u0001220=AFA\u0001221=EONIA\u0001222=STRING\u0001662=1.1\u0001663=1\u0001699=STRING\u0001761=1\u0001819=0\u0001753=1\u0001707=CASH\u0001708=1.1\u00011055=STRING\u0001442=1\u0001824=STRING\u0001555=1\u0001600=STRING\u0001601=STRING\u0001602=STRING\u0001603=1\u0001604=1\u0001605=STRING\u0001606=1\u0001607=1\u0001608=STRING\u0001609=FUT\u0001764=STRING\u0001610=200101w2\u0001611=20010101\u00011212=01:01:01.001+03:30\u0001248=20010101\u0001249=20010101\u0001250=FUT\u0001251=1\u0001252=1.1\u0001253=1.1\u0001257=STRING\u0001599=STRING\u0001596=AF\u0001597=STRING\u0001598=STRING\u0001254=20010101\u0001612=1.1\u0001942=AFA\u0001613=C\u0001614=1.1\u00011436=0\u00011440=0\u0001999=Bbl\u00011224=1.1\u00011421=Bbl\u00011422=1.1\u00011001=H\u00011420=0\u0001615=1.1\u0001616=DSMD\u0001617=STRING\u0001618=1\u0001619=D\u0001620=STRING\u0001621=1\u0001622=D\u0001623=1.1\u0001624=1\u0001556=AFA\u0001740=STRING\u0001739=20010101\u0001955=200101w2\u0001956=20010101\u00011358=0\u00011017=-1\u0001566=1.1\u0001687=1.1\u0001690=1\u0001990=STRING\u00011152=1\u0001683=1\u0001688=AMT\u0001689=STRING\u0001564=C\u0001565=0\u0001539=1\u0001524=STRING\u0001525=B\u0001538=1\u0001804=1\u0001545=STRING\u0001805=1\u0001654=STRING\u0001587=0\u0001588=20010101\u0001637=1.1\u0001675=AFA\u00011073=1.1\u00011074=1.1\u00011075=1.1\u00011379=1.1\u00011381=1.1\u00011383=1.1\u00011384=0 1\u00011418=1.1\u00011342=1\u00011330=STRING\u00011331=STRING\u00011332=STRING\u00011333=1\u00011334=1\u00011335=STRING\u00011336=1\u00011344=STRING\u00011337=FUT\u00011338=STRING\u00011339=200101w2\u00011345=20010101\u00011405=01:01:01.001+03:30\u00011340=1.1\u00011391=C\u00011343=0\u00011341=DSMD\u00011392=STRING\u000160=20010101-01:01:01.001\u0001768=1\u0001769=20010101-01:01:01.001\u0001770=1\u0001771=STRING\u00011033=A\u00011034=1\u00011035=ADD AON\u000163=0\u000164=20010101\u0001987=20010101\u0001573=0\u0001574=M3\u0001552=1\u000154=1\u00011427=STRING\u00011428=1\u00011429=0\u00011009=1\u00011005=STRING\u00011006=STRING\u00011007=STRING\u000183=1\u00011008=0\u0001430=1\u00011154=AFA\u00011155=AFA\u0001453=1\u0001448=STRING\u0001447=B\u0001452=1\u0001802=1\u0001523=STRING\u0001803=1\u00011=STRING\u0001660=1\u0001581=1\u000181=0\u0001575=N\u0001576=1\u0001577=0\u0001578=STRING\u0001579=STRING\u0001376=STRING\u0001377=N\u0001582=1\u0001336=1\u0001625=1\u0001943=STRING\u000112=1.1\u000113=1\u0001479=AFA\u0001497=N\u0001157=1\u0001230=20010101\u0001158=1.1\u0001159=1.1\u0001738=1.1\u0001920=1.1\u0001921=1.1\u0001922=1.1\u0001238=1.1\u0001237=1.1\u0001118=1.1\u0001119=1.1\u0001155=1.1\u0001156=M\u000177=C\u000158=STRING\u0001354=1\u0001355=D\u0001752=1\u0001518=1\u0001519=1\u0001520=1.1\u0001521=AFA\u0001232=1\u0001233=AMT\u0001234=STRING\u0001136=1\u0001137=1.1\u0001138=AFA\u0001139=1\u0001891=0\u0001825=STRING\u0001826=0\u0001591=0\u000170=STRING\u000178=1\u000179=STRING\u0001661=1\u0001736=AFA\u0001467=STRING\u0001756=1\u0001757=STRING\u0001758=B\u0001759=1\u0001806=1\u0001760=STRING\u0001807=1\u000180=1.1\u0001993=STRING\u00011002=1\u0001989=STRING\u00011136=STRING\u00011016=1\u00011012=20010101-01:01:01.001\u00011013=1\u00011014=STRING\u00011158=1\u00011164=1\u0001781=1\u0001782=STRING\u0001783=B\u0001784=1\u0001801=1\u0001785=STRING\u0001786=1\u00011072=1.1\u00011057=Y\u00011139=STRING\u00011115=1\u00011444=1\u000137=STRING\u0001198=STRING\u000111=STRING\u0001526=STRING\u000166=STRING\u00011080=STRING\u00011081=0\u00011431=0\u000140=1\u000144=1.1\u000199=1.1\u000118=0 1\u000139=0\u000138=1.1\u0001152=1.1\u0001516=1.1\u0001468=0\u0001469=1.1\u0001151=1.1\u000114=1.1\u000159=0\u0001126=20010101-01:01:01.001\u00011138=1.1\u00011082=1.1\u00011083=1\u00011084=1\u00011085=1.1\u00011086=1.1\u00011087=1.1\u00011088=1.1\u0001528=A\u0001529=1 2\u0001775=0\u00011432=1\u0001821=STRING\u00011093=1\u0001483=20010101-01:01:01.001\u0001586=20010101-01:01:01.001\u00011188=1.1\u00011380=1.1\u00011190=1.1\u00011382=1.1\u0001797=Y\u00011387=1\u00011388=1\u00011389=Y\u0001852=N\u00011390=0\u0001853=0\u0001994=STRING\u00011011=STRING\u0001779=20010101-01:01:01.001\u0001991=1.1\u00011132=20010101-01:01:01.001+03:30\u00011134=Y\u0001381=1.1\u00011328=STRING\u00011329=1.1\u000193=1\u000189=D\u000110=219\u0001";
			var list = RawFixUtil.GetFixMessage(longMsgStr.AsByteArray());
			var clonedList = list.DeepClone(true, false);
			var sizeBefore = clonedList.Length;
			ClassicAssert.IsTrue(clonedList.RemoveTag(8));
			ClassicAssert.AreEqual(sizeBefore, clonedList.Length + 1);
			ClassicAssert.AreEqual(longMsgStr.Replace("8=FIXT.1.1\u0001", ""), clonedList.ToString());
		}

		[Test]
		public virtual void TestRemoveByObject()
		{
			var sizeBefore = _msgHbiList.Length;
			ClassicAssert.IsTrue(_msgHbiList.Remove(new TagValue(8, "FIX.4.4")));
			ClassicAssert.AreEqual(sizeBefore, _msgHbiList.Length + 1);
			ClassicAssert.AreEqual(MsgHbi.Replace("8=FIX.4.4\u0001", ""), _msgHbiList.ToString());
		}

		[Test]
		public virtual void TestIterable()
		{
			var count = 0;
			int[] tags = { 8, 9, 35, 34, 49, 52, 56, 112, 10 };
			foreach (var f in _msgHbiList)
			{
				ClassicAssert.AreEqual(tags[count], f.TagId);
				count++;
			}

			ClassicAssert.AreEqual(9, count);
			// test reset iterator
			count = 0;
			foreach (var f in _msgHbiList)
			{
				ClassicAssert.AreEqual(tags[count], f.TagId);
				count++;
			}

			ClassicAssert.AreEqual(9, count);
		}

		[Test]
		public virtual void TestClear()
		{
			ClassicAssert.IsTrue(_msgHbiList.Length > 0);
			ClassicAssert.IsFalse(_msgHbiList.IsEmpty);
			((AbstractFixMessage)_msgHbiList).Clear();
			ClassicAssert.IsTrue(_msgHbiList.IsEmpty);
			ClassicAssert.IsTrue(_msgHbiList.Length == 0);

			ClassicAssert.AreEqual("", _msgHbiList.ToString());
		}

		[Test]
		public virtual void TestPreparedMessageClear()
		{
			_msgHbiList.IsPreparedMessage = true;
			((AbstractFixMessage)_msgHbiList).Clear();
			ClassicAssert.IsTrue(_msgHbiList.IsEmpty);
			ClassicAssert.IsTrue(_msgHbiList.Length == 0);
			ClassicAssert.AreEqual("", _msgHbiList.ToString());
			ClassicAssert.IsFalse(_msgHbiList.IsPreparedMessage);
		}

		[Test]
		public virtual void TestAddAtIndexPlacedAtStartOfIndexHash()
		{
			var tagId = 1342;
			var msg = RawFixUtil.GetFixMessage(
				"1330=STRING\u00011331=STRING\u00011332=STRING\u00011333=1\u00011344=STRING\u00011337=FUT\u00011338=STRING\u00011339=200101w2\u00011345=20010101\u00011405=01:01:01.001+03:30\u00011340=1.1\u00011391=C\u00011343=0\u00011341=DSMD\u00011392=STRING\u0001"
					.AsByteArray());
			ClassicAssert.IsFalse(msg.IsTagExists(tagId));
			msg.AddTagAtIndex(0, tagId, "TEST");
			ClassicAssert.AreEqual(tagId, msg.GetTagIdAtIndex(0));
			ClassicAssert.IsTrue(msg.IsTagExists(tagId));
		}

		[Test]
		public virtual void TestRemoveTagPlacedAtStartOfIndexHash()
		{
			//place tag at index start
			var tagId = 1342;
			var msg = RawFixUtil.GetFixMessage(
				"1330=STRING\u00011331=STRING\u00011332=STRING\u00011333=1\u00011344=STRING\u00011337=FUT\u00011338=STRING\u00011339=200101w2\u00011345=20010101\u00011405=01:01:01.001+03:30\u00011340=1.1\u00011391=C\u00011343=0\u00011341=DSMD\u00011392=STRING\u0001"
					.AsByteArray());
			ClassicAssert.IsFalse(msg.IsTagExists(tagId));
			msg.AddTagAtIndex(0, tagId, "TEST");
			ClassicAssert.IsTrue(msg.IsTagExists(tagId));

			//remove inserted tag
			msg.RemoveTag(tagId);
			ClassicAssert.IsFalse(msg.IsTagExists(tagId));
		}

		[Test]
		public virtual void TestIsMessageType()
		{
			var list = new FixMessage();
			list.Set(Tags.MsgType, "A".AsByteArray());
			ClassicAssert.IsTrue(FixMessageUtil.IsMessageType(list, "A".AsByteArray()));
		}

		[Test]
		public virtual void TestChecksum()
		{
			ClassicAssert.AreEqual(_message.CalculateChecksum(), _message.GetTagAsInt(Tags.CheckSum));
		}

		[Test]
		public virtual void TestLength()
		{
			ClassicAssert.AreEqual(_message.CalculateBodyLength(), _message.GetTagAsInt(Tags.BodyLength));
		}

		[Test]
		public virtual void TestTwoFields()
		{
			var list = new FixMessage();
			list.Set(1, 1);
			list.Set(2, 2);
			var oneFieldBytes = new byte[1000];
			ClassicAssert.AreEqual(list.ToByteArrayAndReturnNextPosition(oneFieldBytes, 0), 8);
		}

		[Test]
		public virtual void testOneLongField()
		{
			var list = new FixMessage();
			list.Set(1000, 1000);
			var oneFieldBytes = new byte[1000];
			ClassicAssert.AreEqual(list.ToByteArrayAndReturnNextPosition(oneFieldBytes, 0), 10);

			list = new FixMessage();
			list.Set(1000, 1000);
			ClassicAssert.That("1000=1000\x0001".AsByteArray(), Is.EquivalentTo(list.AsByteArray()));
		}

		[Test]
		public virtual void TestRemoveTag()
		{
			var sizeBefore = _message.Length;
			ClassicAssert.IsTrue(_message.RemoveTag(8));
			ClassicAssert.AreEqual(sizeBefore, _message.Length + 1);
		}

		[Test]
		public virtual void TestCheckInvalidOccurrence()
		{
			ClassicAssert.Throws<ArgumentException>(() => _newsMessage.GetTagValueAsLong(49, 0));
		}

		[Test]
		public virtual void TestCheckTagOccurrence()
		{
			ClassicAssert.AreEqual("1", _newsMessage.GetTagValueAsString(58, 1));
			ClassicAssert.AreEqual("2", _newsMessage.GetTagValueAsString(58, 2));
		}

		[Test]
		public virtual void TestProcessGroup()
		{
			var groupEntries = _newsMessage.ExtractGroup(33, 58, new[] { 58, 354 });
			ClassicAssert.IsTrue(groupEntries.Count == 2, "The groups count should be equals to 2");
		}

		[Test]
		public virtual void TestSplitMessage()
		{
			var groups = _newsMessage.Split(58);
			ClassicAssert.IsTrue(groups.Count == 2, "The groups count should be equals to 2");
		}

		[Test]
		public virtual void TestSplitMessageAsList()
		{
			var groups = _newsMessage.SplitAsList(58);
			ClassicAssert.IsTrue(groups.Count == 2, "The groups count should be equals to 2");
		}

		[Test]
		public virtual void TestDeepClone()
		{
			ClassicAssertEqualsValue(_msgHbiList, _msgHbiList.DeepClone(false, false));
			ClassicAssertEqualsValue(_msgHbiList, _msgHbiList.DeepClone(true, false));
			ClassicAssertEqualsValue(_msgHbiList, _msgHbiList.DeepClone(false, true));
			ClassicAssertEqualsValue(_msgHbiList, _msgHbiList.DeepClone(true, true));
		}

		[Test]
		public virtual void TestDeepCloneTwice()
		{
			var clone1 = _msgHbiList.DeepCopyTo(new FixMessage());
			clone1.RemoveTag(8);

			var clone2 = clone1.DeepCopyTo(new FixMessage());
		}

		[Test]
		public virtual void TestParsedMessageClear()
		{
			var parsedMessage = RawFixUtil.GetFixMessage(Message.AsByteArray());
			ClassicAssert.IsFalse(parsedMessage.GetOriginalStorage().IsEmpty, "original storage should not be empty");
			ClassicAssert.IsTrue(parsedMessage.GetArenaStorage().IsEmpty, "arena storage should be empty");
			ClassicAssert.IsTrue(parsedMessage.GetPerFieldStorage().IsEmpty, "perfield storage should be empty");
			((AbstractFixMessage)parsedMessage).Clear();
			ClassicAssert.IsTrue(parsedMessage.GetOriginalStorage().IsEmpty, "original storage should be empty");
			ClassicAssert.IsTrue(parsedMessage.GetArenaStorage().IsEmpty, "arena storage should be empty");
			ClassicAssert.IsTrue(parsedMessage.GetPerFieldStorage().IsEmpty, "perfield storage should be empty");
		}

		[Test]
		public virtual void TestBuiltMessageClear()
		{
			var msg = new FixMessage();
			//will be added to arena
			msg.AddTag(1, (long)1);
			ClassicAssert.IsTrue(msg.GetOriginalStorage().IsEmpty, "original storage should be empty");
			ClassicAssert.IsFalse(msg.GetArenaStorage().IsEmpty, "arena storage should not be empty");
			ClassicAssert.IsTrue(msg.GetPerFieldStorage().IsEmpty, "perfield storage should be empty");
			((AbstractFixMessage)msg).Clear();
			ClassicAssert.IsTrue(msg.GetOriginalStorage().IsEmpty, "original storage should be empty");
			ClassicAssert.IsTrue(msg.GetArenaStorage().IsEmpty, "arena storage should be empty");
			ClassicAssert.IsTrue(msg.GetPerFieldStorage().IsEmpty, "perfield storage should be empty");
		}

		[Test]
		public virtual void TestLargeMessageClear()
		{
			var msg = new FixMessage();
			//will be added to arena
			msg.AddTag(1, new byte[ArenaMessageStorage.MaxBytesInArenaStorage]);
			//will be added to perfield, because arena is full
			msg.AddTag(2, (long)1);

			ClassicAssert.IsTrue(msg.GetOriginalStorage().IsEmpty, "original storage should be empty");
			ClassicAssert.IsFalse(msg.GetArenaStorage().IsEmpty, "arena storage should be empty");
			ClassicAssert.IsFalse(msg.GetPerFieldStorage().IsEmpty, "perfield storage should be empty");
			((AbstractFixMessage)msg).Clear();
			ClassicAssert.IsTrue(msg.GetOriginalStorage().IsEmpty, "original storage should be empty");
			ClassicAssert.IsTrue(msg.GetArenaStorage().IsEmpty, "arena storage should be empty");
			ClassicAssert.IsTrue(msg.GetPerFieldStorage().IsEmpty, "perfield storage should be empty");
		}

		[Test]
		public virtual void TestLargeMessageUpdate()
		{
			var msg = new FixMessage();
			//add several fields to arena
			msg.AddTag(1, "A");
			var value2 = new byte[ArenaMessageStorage.MaxBytesInArenaStorage];
#if NET48
			for (var i = 0; i < value2.Length; i++)
			{
				value2[i] = (byte)'B';
			}
#else
			Array.Fill(value2, (byte)'B');
#endif
			msg.AddTag(2, value2);
			//check that all values in arena storage
			ClassicAssert.IsTrue(msg.GetOriginalStorage().IsEmpty, "original storage should be empty");
			ClassicAssert.IsFalse(msg.GetArenaStorage().IsEmpty, "arena storage should be empty");
			ClassicAssert.IsTrue(msg.GetPerFieldStorage().IsEmpty, "perfield storage should be empty");

			//value will be placed to perfield, because arena is full
			msg.Set(1, "CC");
			ClassicAssert.IsTrue(msg.GetOriginalStorage().IsEmpty, "original storage should be empty");
			ClassicAssert.IsFalse(msg.GetArenaStorage().IsEmpty, "arena storage should be empty");
			ClassicAssert.IsFalse(msg.GetPerFieldStorage().IsEmpty, "perfield storage should be empty");

			ClassicAssert.AreEqual("CC", msg.GetTagValueAsString(1));
			var tagIndex = msg.GetTagIndex(1);
			ClassicAssert.AreEqual(FieldIndex.FlagPerfieldStorage, msg.GetStorageType(tagIndex),
				"new value should be in perfield storage");
		}

		private void ClassicAssertEqualsValue(FixMessage fixList1, FixMessage fixList2)
		{
			ClassicAssert.IsFalse(fixList1 == fixList2);
			ClassicAssert.AreEqual(fixList1.Length, fixList2.Length);
			var tagValue = new TagValue();
			var tagValueClone = new TagValue();
			for (var i = 0; i < fixList1.Length; i++)
			{
				fixList1.LoadTagValueByIndex(i, tagValue);
				fixList2.LoadTagValueByIndex(i, tagValueClone);
				ClassicAssert.IsFalse(tagValue.Buffer == tagValueClone.Buffer);
				ClassicAssertEqualsValue(tagValue, tagValueClone);
			}

			ClassicAssert.AreEqual(fixList1.ToString(), fixList2.ToString());
		}

		private void ClassicAssertEqualsValue(TagValue tagValue1, TagValue tagValue2)
		{
			ClassicAssert.AreEqual(tagValue1.TagId, tagValue2.TagId, "Cloned TagID must be equals");
			ClassicAssert.AreEqual(tagValue1.Length, tagValue2.Length, "Length of cloned value must be equals");
			for (var i = 0; i < tagValue1.Length; i++)
			{
				ClassicAssert.AreEqual(tagValue1.Buffer[i + tagValue1.Offset], tagValue2.Buffer[i + tagValue2.Offset],
					"Cloned value must be equals");
			}
		}

		private string ReplaceFieldValue(int tagId, string value, string msg)
		{
			return ReplaceFieldValue(tagId, 1, value, msg);
		}

		private string ReplaceFieldValue(int tagId, int occurrence, string value, string msg)
		{
			var fieldPrefix = "" + '\u0001' + tagId + '=';
			var startIndex = 0;
			for (var i = 0; i < occurrence; i++)
			{
				startIndex = msg.IndexOf(fieldPrefix, startIndex, StringComparison.Ordinal);
				if (startIndex < 0)
				{
					return msg;
				}

				startIndex += fieldPrefix.Length;
			}

			var endIndex = msg.IndexOf('\u0001', startIndex);
			if (endIndex < 0)
			{
				endIndex = msg.Length;
			}

			return msg.Substring(0, startIndex) + value + msg.Substring(endIndex, msg.Length - endIndex);
		}

		private void ClassicAssertEqualsString(int tagId, string expectedValue, FixMessage message)
		{
			var actualValue = message.GetTag(tagId);
			ClassicAssert.AreEqual(expectedValue, actualValue.StringValue);
		}

		[Test]
		public virtual void TestAddDouble()
		{
			var fMessage = RawFixUtil.GetFixMessageFromPool(true);

			FillMessage(fMessage, "20130508-13:51:55.003", "22806579", "86601753734398", '1', '1', '1', 300, 700);
			ClassicAssert.AreEqual(
				"56=MM00|60=20130508-13:51:55.003|17=22806579|11=86601753734398|37=86601753734398|55=SPY|54=1|39=1|150=1|20=0|6=1.1|14=300|151=700|40=2|47=A|59=0|44=1.1|110=0|31=1.1|32=100|35=8|",
				fMessage.ToString().Replace('\u0001', '|'));
		}

		private void FillMessage(FixMessage fMessage, string transactTimestamp, string execId, string clOrdId,
			char side, char status, char execType, int cumQty, int leavesQty)
		{
			((AbstractFixMessage)fMessage).Clear();
			fMessage.Set(56, "MM00");
			fMessage.AddTag(60, transactTimestamp);
			fMessage.AddTag(17, execId);
			fMessage.AddTag(11, clOrdId);
			fMessage.AddTag(37, clOrdId); // TE should give me a OrderID, man....test only
			fMessage.AddTag(55, "SPY");
			fMessage.AddTag(54, side);
			fMessage.AddTag(39, status);
			fMessage.AddTag(150, execType);
			var transType = '0';
			fMessage.AddTag(20, transType);
			var avgpx = 1.1 - 0.0000001;
			fMessage.AddTag(6, avgpx, 4);
			fMessage.AddTag(14, cumQty);
			fMessage.AddTag(151, leavesQty);
			var orderType = '2';
			fMessage.AddTag(40, orderType);
			var capacity = 'A';
			fMessage.AddTag(47, capacity);
			var tif = '0';
			fMessage.AddTag(59, tif);
			fMessage.AddTag(44, "1.1");
			fMessage.AddTag(110, (long)0);
			fMessage.AddTag(31, "1.1");
			var lastQty = 100;
			fMessage.AddTag(32, lastQty);
			fMessage.Set(35, "8");
		}

		[Test]
		public virtual void TestDeepCopyWithReducingSize()
		{
			var emptyList = new FixMessage();
			for (var i = 0; i < 100; i++)
			{
				_message.AddTag(1, (long)1);
			}

			emptyList.DeepCopyTo(_message);
			ClassicAssert.AreEqual(0, _message.RawLength);
		}

		[Test]
		public virtual void TestDeepCloneToBiggerMsg()
		{
			var source = new FixMessage();
			var dest = new FixMessage();

			for (var i = 100; i < 110; i++)
			{
				source.AddTag(i, i);
			}

			ClassicAssert.IsFalse(source.GetTagIndex(100) == -1);

			for (var i = 100; i < 200; i++)
			{
				dest.AddTag(i, i);
			}

			ClassicAssert.IsFalse(dest.GetTagIndex(100) == -1);

			((AbstractFixMessage)dest).Clear();

			source.DeepCopyTo(dest);

			// Tag 100 is present in source as well as dest, AND YET, it fails!
			ClassicAssert.IsFalse(dest.GetTagIndex(100) == -1);

			// dest.getTagValueAsLong(100) throws FieldNotFoundException
		}

		[Test]
		public virtual void CheckTagExistsAtIndex()
		{
			var size = _message.Count;
			//index is starting from 0, so the last element has index [size-1]
			ClassicAssert.Throws<IndexOutOfRangeException>(() => _message.GetTagIdAtIndex(size));
		}

		[Test]
		public virtual void TestFieldUpdateInPlace()
		{
			var orderMsg = new FixMessage();

			orderMsg.AddTag(11, "1432205135565-1");
			orderMsg.AddTag(55, "IBM");

			//update in arena storage
			orderMsg.Set(11, "1432205135565-1");

			var copy = orderMsg.DeepClone(true, false);
			ClassicAssert.That(copy.ToString(), Is.EqualTo(orderMsg.ToString()));
		}

		[Test]
		public virtual void TestFix50VersionParsing()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIXT.1.1");
			msg.Set(35, "D");
			msg.Set(1128, 7);
			ClassicAssert.AreEqual(FixVersion.Fixt11, msg.MsgVersion);
			ClassicAssert.AreEqual(FixVersionContainer.GetFixVersionContainer(FixVersion.Fix50), msg.GetFixVersion());
		}

		[Test]
		public virtual void TestFix50Sp1VersionParsing()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIXT.1.1");
			msg.Set(35, "D");
			msg.Set(1128, 8);
			ClassicAssert.AreEqual(FixVersion.Fixt11, msg.MsgVersion);
			ClassicAssert.AreEqual(FixVersionContainer.GetFixVersionContainer(FixVersion.Fix50Sp1), msg.GetFixVersion());
		}

		[Test]
		public virtual void TestFix50Sp2VersionParsing()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIXT.1.1");
			msg.Set(35, "D");
			msg.Set(1128, 9);
			ClassicAssert.AreEqual(FixVersion.Fixt11, msg.MsgVersion);
			ClassicAssert.AreEqual(FixVersionContainer.GetFixVersionContainer(FixVersion.Fix50Sp2), msg.GetFixVersion());
		}

		[Test]
		public virtual void ShouldReturnCharSequence()
		{
			var msg = new FixMessage();
			var testTagValue = "value";
			msg.Set(58, testTagValue);
			var reusableString = new ReusableString();
			msg.GetTagValueAsReusableString(reusableString, 58);
			ClassicAssert.IsTrue(reusableString.Equals(testTagValue));
		}

		[Test]
		public virtual void ShouldReuseCharSequence()
		{
			var msg = new FixMessage();
			var reusableString = new ReusableString();
			var testTagValue1 = "value1";
			var testTagValue2 = "value11";
			msg.Set(58, testTagValue1);
			msg.GetTagValueAsReusableString(reusableString, 58);
			ClassicAssert.IsTrue(reusableString.Equals(testTagValue1));
			msg.Set(58, testTagValue2);
			msg.GetTagValueAsReusableString(reusableString, 58);
			ClassicAssert.IsTrue(reusableString.Equals(testTagValue2));
		}

		[Test]
		public virtual void ShouldReturnCharSequenceAtIndex()
		{
			var msg = new FixMessage();
			var testTagValue1 = "value1";
			var testTagValue2 = "value2";
			var reusableString = new ReusableString();
			msg.AddTag(58, testTagValue1);
			msg.AddTag(58, testTagValue2);
			msg.GetTagValueAsReusableString(reusableString, 58, 1);
			ClassicAssert.IsTrue(reusableString.Equals(testTagValue1));
			msg.GetTagValueAsReusableString(reusableString, 58, 2);
			ClassicAssert.IsTrue(reusableString.Equals(testTagValue2));
		}

		[Test]
		public virtual void SetLongerDoubleThenReserved()
		{
			var message = RawFixUtil.GetFixMessage("1=1234.5678\u00012=1234.5678\u0001".AsByteArray());
			//set longer value
			message.Set(1, 12345.69999, 4);
			message.Set(2, 12345.69999, 4);
			ClassicAssert.AreEqual("1=12345.7\u00012=12345.7\u0001", message.ToString());

			//check previous
			message.Set(1, 1234.5678, 4);
			message.Set(2, 1234.5678, 4);
			ClassicAssert.AreEqual("1=1234.5678\u00012=1234.5678\u0001", message.ToString());

			//set longer value again
			message.Set(1, 1234567.89999, 4);
			message.Set(2, 1234567.89999, 4);
			ClassicAssert.AreEqual("1=1234567.9\u00012=1234567.9\u0001", message.ToString());

			//check previous
			message.Set(1, 1234.5678, 4);
			message.Set(2, 1234.5678, 4);
			ClassicAssert.AreEqual("1=1234.5678\u00012=1234.5678\u0001", message.ToString());

			//set longer value again
			message.Set(1, 123456789.11111, 4);
			message.Set(2, 123456789.11111, 4);
			ClassicAssert.AreEqual("1=123456789.1111\u00012=123456789.1111\u0001", message.ToString());
		}
	}
}
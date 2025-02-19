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
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Message.Tests
{
	[TestFixture]
	internal class RawFixUtilTest
	{
		private readonly byte[] _testMessage =
			"8=FIX.4.2\u00019=146\u000135=5\u000149=target\u000156=sender\u000134=9\u000152=20080908-04:35:00.158\u000158=Sequence number of incoming message is less than expected. Expected Sequence number: 13\u000110=106\u0001"
				.AsByteArray();

		private readonly byte[] _messageWithRg =
			"1128=8\u000135=d\u000149=CME\u000134=118\u000152=20071120212527215\u0001911=173\u0001864=2\u0001865=5\u0001866=20070615\u00011145=133000000\u0001865=7\u0001866=20071221\u00011145=143000000\u00011150=58350\u00011151=QCN\u000155=QN\u0001107=QCNZ7-NQZ7\u000148=600102\u000122=8\u0001461=FMIXSX\u0001462=5\u0001207=XCME\u0001827=2\u0001947=USD\u0001562=1\u00011140=400\u000115=USD\u00011141=1\u00011022=GBX\u0001264=5\u00011142=F\u0001762=IS\u00011143=500\u00011144=0\u0001870=4\u0001871=24\u0001872=1\u0001871=24\u0001872=3\u0001871=24\u0001872=4\u0001871=24\u0001872=11\u0001200=200712\u0001969=25\u00019787=0.01\u0001555=2\u0001600=[N/A]\u0001623=1\u0001602=85516\u0001603=8\u0001624=1\u0001600=[N/A]\u0001623=1\u0001602=21033\u0001603=8\u0001624=2\u0001"
				.AsByteArray();

		private readonly byte[] _messageWithRawMessage =
			"8=FIX.4.4\u00019=194\u000135=C\u000149=SNDR\u000156=TRGT\u000134=2\u000152=20030203-16:20:05\u0001164=111111111\u000194=0\u0001147=Encoded Fields Test\u000133=2\u000158=Line 1\u000158=Line 2\u000195=69\u000196=8=FIX.4.4\u00019=47\u000135=0\u000149=SNDR\u000156=TRGT\u000134=2\u000152=20081225-18:32:15\u000110=138\u0001\u000110=092\u0001"
				.AsByteArray();

		private readonly string _etalonRawMessage =
			"8=FIX.4.4\u00019=47\u000135=0\u000149=SNDR\u000156=TRGT\u000134=2\u000152=20081225-18:32:15\u000110=138\u0001";

		public const string GarbledMessageWithInvalidRawTagLength =
			"8=FIX.4.4\u00019=133\u000135=8\u000149=SNDR\u000156=TRGT\u000134=002\u0001212=4\u000152=20030203-16:09:30\u0001213=TEST\u000137=11111111\u000117=11\u000139=0\u0001150=0\u000155=TESTB\u000154=1\u000138=1000\u0001151=1000\u000114=0\u00016=0\u000110=189\u0001";

		public const string GarbledMessageInvalidTagAtEnd =
			"8=FIX.4.4\u00019=209\u000135=AK\u000149=SNDR\u000156=TRGT\u000134=2\u000150=30737\u000152=20030204-08:46:14\u0001664=0001\u0001666=0\u0001773=1\u0001665=4\u000160=20060217-10:00:00\u000175=20060217\u000155=TESTA\u000180=1000\u000154=1\u0001862=2\u0001528=A\u0001863=400\u0001528=P\u0001863=600\u000179=KTierney\u00016=20\u0001381=20000\u0001118=2000\u000110";

		public const string GarbledMessageNotSohAtEnd = GarbledMessageInvalidTagAtEnd + "=125";

		public const string GarbledMessageInvalidField =
			"8=FIX.4.2\u00019=50\u000135=0\u000134=2\u000149=TW\u000156=ISLD\u000152=20081219-07:43:36\u0001112\u000110=185\u0001";

		public const string GarbledMessage1 = "GARBLED=3\u0001";
		public const string GarbledMessage2 = "\u0001GARBLED\u0001";

		public const string MessageWithInvalidLenField =
			"8=FIX.4.4\u00019=102\u000135=7\u000149=SNDR\u000156=TRGT\u000134=2\u000152=20100511-11:33:34.024\u00012=20\u00015=N\u000155=TESTB\u00014=T\u000153=1000000\u000193=:\u000189=123456789\u000110=088\u0001";

		private void TestStartValue(bool fromStart, string message, int tagId)
		{
			var subField = "\u0001" + tagId + "=";
			var expectValueIndex = message.IndexOf(subField, StringComparison.Ordinal) + subField.Length;
			int index;
			if (fromStart)
			{
				index = RawFixUtil.GetStartValueFromStartBuffer(message.AsByteArray(), 0, message.Length, tagId);
			}
			else
			{
				index = RawFixUtil.GetStartValueFromEndBuffer(message.AsByteArray(), 0, message.Length, tagId);
			}

			ClassicAssert.AreEqual(expectValueIndex, index);
		}

		[Test]
		public virtual void GetRawValuesFromGarbledMessages()
		{
			var buffer = "81225-1".AsByteArray();
			var value = RawFixUtil.GetRawValue(buffer, 0, buffer.Length, 35);
			ClassicAssert.IsNull(value);
			value = RawFixUtil.GetRawValue(buffer, 0, buffer.Length, 34);
			ClassicAssert.IsNull(value);

			buffer = "8:32:15\u000110=13".AsByteArray();
			value = RawFixUtil.GetRawValue(buffer, 0, buffer.Length, 35);
			ClassicAssert.IsNull(value);
			value = RawFixUtil.GetRawValue(buffer, 0, buffer.Length, 34);
			ClassicAssert.IsNull(value);
		}

		[Test]
		public virtual void GetSequenceTagFromBatch()
		{
			var message1 =
				"8=FIX.4.4\u00019=73\u000135=h\u000134=42\u000149=Test\u000156=Server\u000152=20081205-16:01:53.078\u0001336=PRE-OPEN\u0001340=2\u000110=130\u0001";
			var message2 =
				"8=FIX.4.4\u00019=73\u000135=h\u000134=43\u000149=Test\u000156=Server\u000152=20081205-16:01:53.078\u0001336=PRE-OPEN\u0001340=2\u000110=130\u0001";
			var message3 =
				"8=FIX.4.4\u00019=73\u000135=h\u000134=44\u000149=Test\u000156=Server\u000152=20081205-16:01:53.078\u0001336=PRE-OPEN\u0001340=2\u000110=130\u0001";
			var oneMessLength = message1.Length;
			var seqNum = RawFixUtil.GetSequenceNumber(message1.AsByteArray(), 0, oneMessLength);
			ClassicAssert.AreEqual(42, seqNum);

			seqNum = RawFixUtil.GetSequenceNumber((message1 + message2).AsByteArray(), oneMessLength, oneMessLength);
			ClassicAssert.AreEqual(43, seqNum);

			seqNum = RawFixUtil.GetSequenceNumber((message1 + message2 + message3).AsByteArray(), 2 * oneMessLength,
				oneMessLength);
			ClassicAssert.AreEqual(44, seqNum);
		}

		[Test]
		public virtual void GetStartValueFromEndBuff()
		{
			var message =
				"8=FIX.4.4\u00019=73\u000135=h\u000134=42\u000149=Test\u000156=Server\u000152=20081205-16:01:53.078\u0001336=PRE-OPEN\u0001340=2\u000110=130\u0001";
			TestStartValue(false, message, 9);
			TestStartValue(false, message, 34);
			TestStartValue(false, message, 336);
		}

		[Test]
		public virtual void GetStartValueFromStartBuff()
		{
			var message =
				"8=FIX.4.4\u00019=73\u000135=h\u000134=42\u000149=Test\u000156=Server\u000152=20081205-16:01:53.078\u0001336=PRE-OPEN\u0001340=2\u000110=130\u0001";
			TestStartValue(true, message, 9);
			TestStartValue(true, message, 34);
			TestStartValue(true, message, 336);
		}

		[Test]
		public virtual void ParseMessageWithInvalidLenField()
		{
			RawFixUtil.GetFixMessage(MessageWithInvalidLenField.AsByteArray());
		}

		[Test]
		public virtual void ParseStartOfMessageWithInvalidEndBuffer()
		{
			var msg = RawFixUtil.GetFixMessageUntilTagsExists("8=FIX.4.4\u00019=73\u0001".AsByteArray());
			ClassicAssert.IsTrue(msg.Length == 2);
			msg = RawFixUtil.GetFixMessageUntilTagsExists("8=FIX.4.4\u00019=73\u0001fdgsdf".AsByteArray());
			ClassicAssert.IsTrue(msg.Length == 2);
		}

		[Test]
		public virtual void ParseTagWithEqualsSign()
		{
			var msg =
				"8=FIX.4.2\u00019=58\u000135=0\u000149=Sender=CompID\u000156=TargetCompID\u000134=143550\u000152=20101126-13:39:50.563\u000110=185\u0001";
			var fieldList = RawFixUtil.GetFixMessage(msg.AsByteArray());
			ClassicAssert.That("Sender=CompID".AsByteArray(), Is.EquivalentTo(fieldList.GetTag(49).Value));
			ClassicAssert.AreEqual("Sender=CompID", fieldList.GetTag(49).StringValue);
		}

		[Test]
		public virtual void ParsingToLargeSecureDataLen()
		{
			var secureMsg =
				"8=FIX.4.4\u00019=94\u000135=D\u000149=TRGT\u000156=SNDR\u000134=2\u000150=30737\u000197=Y\u000152=20110303-13:55:03.691\u0001369=6\u000190=15\u000191=IIZZKK\u000110=160\u000110=160\u0001";
			var msg = RawFixUtil.GetFixMessage(secureMsg.AsByteArray());
			ClassicAssert.AreEqual(15, msg.GetTag(90).LongValue);
			ClassicAssert.AreEqual("IIZZKK\u000110=160\u000110=160", msg.GetTag(91).StringValue);
			ClassicAssert.IsNull(msg.GetTag(10));
		}

		[Test]
		public virtual void ParsingToLowSecureDataLen()
		{
			var secureMsg =
				"8=FIX.4.4\u00019=94\u000135=D\u000149=TRGT\u000156=SNDR\u000134=2\u000150=30737\u000197=Y\u000152=20110303-13:55:03.691\u0001369=6\u000190=11\u000191=IIZZKK\u000110=160\u000110=160\u0001";
			var msg = RawFixUtil.GetFixMessage(secureMsg.AsByteArray());
			ClassicAssert.AreEqual(11, msg.GetTag(90).LongValue);
			ClassicAssert.AreEqual("IIZZKK\u000110=160", msg.GetTag(91).StringValue);
		}

		[Test]
		public virtual void ParsingValidSecureDataLen()
		{
			var secureMsg =
				"8=FIX.4.4\u00019=94\u000135=D\u000149=TRGT\u000156=SNDR\u000134=2\u000150=30737\u000197=Y\u000152=20110303-13:55:03.691\u0001369=6\u000190=13\u000191=IIZZKK\u000110=160\u000110=160\u0001";
			var msg = RawFixUtil.GetFixMessage(secureMsg.AsByteArray());
			ClassicAssert.AreEqual(13, msg.GetTag(90).LongValue);
			ClassicAssert.AreEqual("IIZZKK\u000110=160", msg.GetTag(91).StringValue);
		}

		[Test]
		public virtual void TestCheckSumAndLength()
		{
			ClassicAssert.AreEqual(106, RawFixUtil.GetFixMessage(_testMessage).CalculateChecksum());
			ClassicAssert.AreEqual(146, RawFixUtil.GetFixMessage(_testMessage).CalculateBodyLength());
		}

		[Test]
		public virtual void TestCheckSumForLargeMessage()
		{
			//100Mb
			var largeBuff = new byte[100 * 1024 * 1024];
#if NET48
			for (var i = 0; i < largeBuff.Length; i++)
			{
				largeBuff[i] = (byte)0xff;
			}
#else
			Array.Fill(largeBuff, (byte)0xff);
#endif
			var largeMsg = new FixMessage();
			largeMsg.AddTag(111, largeBuff);

			long actual = largeMsg.CalculateChecksum();
			ClassicAssert.IsTrue(actual > 0, "Checksum overflow");
			ClassicAssert.AreEqual(209, actual);
		}

		[Test]
		public virtual void TestGarbled2()
		{
			ClassicAssert.Throws<GarbledMessageException>(() => { RawFixUtil.GetFixMessage(GarbledMessage2.AsByteArray()); });
		}

		[Test]
		public virtual void TestGarbledMessageWithoutStartAndEndTags()
		{
			ClassicAssert.Throws<GarbledMessageException>(() => { RawFixUtil.GetFixMessage(GarbledMessage1.AsByteArray()); });
		}

		[Test]
		public virtual void TestGetAllRawValue()
		{
			var allRawValues = RawFixUtil.GetAllRawValues(_messageWithRg, 0, _messageWithRg.Length, 872);
			ClassicAssert.IsNotNull(allRawValues);
			ClassicAssert.AreEqual(4, allRawValues.Count);
			string[] expectedValues = { "1", "3", "4", "11" };
			for (var i = 0; i < expectedValues.Length; i++)
			{
				var expectedValue = expectedValues[i];
				var actualValue = StringHelper.NewString(allRawValues[i]);
				ClassicAssert.AreEqual(expectedValue, actualValue);
			}
		}

		[Test]
		public virtual void TestGetAllRawValueReturnsEmptyArrayForNonExistentTags()
		{
			var allRawValues = RawFixUtil.GetAllRawValues(_messageWithRg, 0, _messageWithRg.Length, 72);
			ClassicAssert.IsNotNull(allRawValues);
			ClassicAssert.AreEqual(0, allRawValues.Count);
		}

		[Test]
		public virtual void TestGetFieldListForRawFieldsWithoutLength()
		{
			ClassicAssert.Throws<GarbledMessageException>(() =>
			{
				RawFixUtil.GetFixMessage(GarbledMessageWithInvalidRawTagLength.AsByteArray());
			});
		}

		[Test]
		public virtual void TestGetFieldListWithout35Tag()
		{
			var list = RawFixUtil.GetFixMessage("8=FIX.4.1\u00019=0\u0001".AsByteArray());
			ClassicAssert.AreEqual(2, list.Length);
		}

		[Test]
		public virtual void TestGetFixMessageForRawFields()
		{
			var rawMessage = RawFixUtil.GetFixMessage(_messageWithRawMessage).GetTag(96).StringValue;
			ClassicAssert.AreEqual(_etalonRawMessage, rawMessage);
		}

		[Test]
		public virtual void TestGetFixMessageForRawFroMiddleOfByteBuffer()
		{
			var msgBuffer =
				"!!!!!!!!!!8=FIX.4.2\u00019=110\u000135=B\u000134=4\u000149=acceptor\u000156=initiator\u000152=20110110-15:43:32.470\u0001148=Hello there:2\u000133=3\u000158=line1\u000158=line2\u000158=line3\u000110=001\u0001!!!!!!!!!";
			var rawMessage = RawFixUtil.GetFixMessage(msgBuffer.AsByteArray(), 10, 133);
			ClassicAssert.AreEqual(13, rawMessage.Length);
		}

		[Test]
		public virtual void TestGetRawValueForRawTags()
		{
			var buffer =
				"8=FIX.4.0\u00019=0\u000135=B\u000149=C\u000156=C\u000190=1\u000191=D\u000134=0\u000150=C\u000157=C\u000152=20020101-00:00:00\u0001122=20020101-00:00:00\u000142=20020101-00:00:00\u000161=0\u000146=C\u000133=1\u000158=C\u000195=1\u000196=D\u000110=000\u0001"
					.AsByteArray();
			var list = RawFixUtil.GetFixMessage(buffer);
			ClassicAssert.AreEqual("D", list.GetTag(96).StringValue);
		}

		[Test]
		public virtual void TestGetRawValueForRawTags1()
		{
			var buffer =
				"8=FIX.4.0\u00019=0\u000135=B\u000149=C\u000156=C\u000190=1\u000191=D\u000134=0\u000150=C\u000157=C\u000152=20020101-00:00:00\u0001122=20020101-00:00:00\u000142=20020101-00:00:00\u000161=0\u000146=C\u000133=1\u000158=C\u000195=5\u000196=D==\u0001=\u000110=000\u0001"
					.AsByteArray();
			var list = RawFixUtil.GetFixMessage(buffer);
			ClassicAssert.AreEqual("D==\u0001=", list.GetTag(96).StringValue);
		}

		[Test]
		public virtual void TestGetRawValueLastValue()
		{
			var buffer = "\u000110=0\u0001".AsByteArray();
			var value = RawFixUtil.GetRawValue(buffer, 0, buffer.Length, 10);
			ClassicAssert.IsNotNull(value);
			ClassicAssert.That("0".AsByteArray(), Is.EquivalentTo(value));
		}

		[Test]
		public virtual void TestGetRawValueReturnsCorrectValue()
		{
			ClassicAssert.AreEqual("9",
				StringHelper.NewString(RawFixUtil.GetRawValue(_testMessage, 0, _testMessage.Length, 34)));
			ClassicAssert.AreEqual("target",
				StringHelper.NewString(RawFixUtil.GetRawValue(_testMessage, 0, _testMessage.Length, 49)));
		}

		[Test]
		public virtual void TestGetRawValueReturnsNullForNonExistentTags()
		{
			ClassicAssert.IsNull(RawFixUtil.GetRawValue(_testMessage, 0, _testMessage.Length, 4));
		}

		[Test]
		public virtual void TestGetSequenceNumberWith340Tag()
		{
			ClassicAssert.That("9".AsByteArray(),
				Is.EquivalentTo(RawFixUtil.GetRawValue(_testMessage, 0, _testMessage.Length, 34)));
		}

		[Test]
		public virtual void TestGetValueFromGarbledMessageWithInternalSequencyNumberField()
		{
			var buffer = "34=2\u000134=\u000110=190\u0001".AsByteArray();
			var value = RawFixUtil.GetRawValue(buffer, 0, buffer.Length, 34, true);
			ClassicAssert.IsNotNull(value);
			long? sequence = FixTypes.ParseInt(value);
			ClassicAssert.IsTrue(sequence.Equals(2L));
		}

		[Test]
		public virtual void TestMessageHasEqualsAtEnd()
		{
			ClassicAssert.Throws<GarbledMessageException>(() =>
			{
				var message = GarbledMessageInvalidTagAtEnd + "=";
				RawFixUtil.GetFixMessage(message.AsByteArray());
			});
		}

		[Test]
		public virtual void testMessageHasInvalid_112_field()
		{
			ClassicAssert.Throws<GarbledMessageException>(() =>
			{
				RawFixUtil.GetFixMessage(GarbledMessageInvalidField.AsByteArray());
			});
		}

		[Test]
		public virtual void TestMessageHasInvalidTagAtEnd()
		{
			ClassicAssert.Throws<GarbledMessageException>(() =>
			{
				RawFixUtil.GetFixMessage(GarbledMessageInvalidTagAtEnd.AsByteArray());
			});
		}

		[Test]
		public virtual void TestNoSohAtTheEndOfMessage()
		{
			ClassicAssert.Throws<GarbledMessageException>(() =>
			{
				RawFixUtil.GetFixMessage(GarbledMessageNotSohAtEnd.AsByteArray());
			});
		}

		[Test]
		public virtual void TestParseLogoutMessage42()
		{
			var message =
				"8=FIX.4.2\u00019=146\u000135=5\u000149=target\u000156=sender\u000134=9\u000152=20080908-04:35:00.158\u000158=Sequence number of incoming message is less than expected. Expected Sequence number: 13\u0001167=\u0001==1\u000115=2\u000110=106\u0001"
					.AsByteArray();
			RawFixUtil.GetFixMessage(message);
		}
	}
}
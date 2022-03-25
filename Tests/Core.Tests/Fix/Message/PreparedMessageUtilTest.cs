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
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework;
using ValueType = Epam.FixAntenna.NetCore.FixEngine.ValueType;

namespace Epam.FixAntenna.Fix.Message
{
	internal class PreparedMessageUtilTest
	{
		internal MessageStructure Ms = new MessageStructure();
		internal PreparedMessageUtil Pmu;
		internal SessionParameters SessionParams;

		[SetUp]
		public virtual void Initialize()
		{
			SessionParams = new SessionParameters();
			SessionParams.Port = 12000;
			SessionParams.Host = "localhost";
			SessionParams.TargetCompId = "admin";
			SessionParams.SenderCompId = "admin3";
			SessionParams.SenderSubId = "ssId";
			SessionParams.TargetSubId = "ee";
			Pmu = new PreparedMessageUtil(SessionParams);
			Ms.Reserve(1, 1);
		}

		[Test]
		public virtual void TestPrepareFromStructure()
		{
			var ms = new MessageStructure();
			ms.Reserve(1, 1);
			ms.Reserve(2, MessageStructure.VariableLength);
			var preparedMessage = Pmu.PrepareMessage("A", ms);
			var expected =
				"8=FIX.4.2\u00019=091\u000135=A\u000134=     \u000149=admin3\u000156=admin\u000150=ssId\u0001"
							+ "57=ee\u000152=                     \u000198=0\u0001108=30\u00011= \u00012=\u000110=   \u0001";
			Assert.AreEqual(expected, StringHelper.NewString(preparedMessage.AsByteArray()));
			Assert.AreEqual(expected, preparedMessage.ToString());
		}

		[Test]
		public virtual void TestPrepareFromStructureWithHeaderField()
		{
			var ms = new MessageStructure();
			ms.Reserve(35, 1);
			ms.Reserve(9, 3);
			ms.Reserve(34, MessageStructure.VariableLength);
			var preparedMessage = Pmu.PrepareMessage("A", ms);
			var expected =
				"8=FIX.4.2\u00019=079\u000135=A\u000134=\u000149=admin3\u000156=admin\u000150=ssId\u000157=ee"
							+ "\u000152=                     \u000198=0\u0001108=30\u000110=   \u0001";
			Assert.AreEqual(expected, StringHelper.NewString(preparedMessage.AsByteArray()));
			Assert.AreEqual(expected, preparedMessage.ToString());
		}

		[Test]
		public virtual void TestPrepareFromTemplate()
		{
			var ms = new MessageStructure();
			ms.Reserve(1, 1);
			ms.Reserve(2, MessageStructure.VariableLength);

			var list = new FixMessage();
			list.AddTag(1, "TestValue1");
			list.AddTag(2, "TestValue2");
			list.AddTag(3, "TestValue3");

			var preparedMessage = Pmu.PrepareMessage(list, "A", ms);
			var expected =
				"8=FIX.4.2\u00019=123\u000135=A\u000134=     \u000149=admin3\u000156=admin\u000150=ssId\u0001"
							+ "57=ee\u000152=                     \u000198=0\u0001108=30\u00011=TestValue1\u00012=TestValue2\u0001"
							+ "3=TestValue3\u000110=   \u0001";
			Assert.AreEqual(expected, StringHelper.NewString(preparedMessage.AsByteArray()));
			Assert.AreEqual(expected, preparedMessage.ToString());
		}

		[Test]
		public virtual void TestPrepareFromLongMsgTemplate()
		{
			var tagValueLength = 1000;
			var ms = new MessageStructure();
			ms.Reserve(1, tagValueLength);

			var list = new FixMessage();
			var tagValue = new byte[tagValueLength];
			tagValue.Fill((byte)'B');
			list.AddTag(1, tagValue);

			var preparedMessage = Pmu.PrepareMessage(list, "A", ms);
			var expected =
				"8=FIX.4.2\u00019=1087\u000135=A\u000134=     \u000149=admin3\u000156=admin\u000150=ssId\u000157=ee\u000152=                     \u000198=0\u0001108=30\u00011=BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB\u000110=   \u0001";
			Assert.AreEqual(expected, StringHelper.NewString(preparedMessage.AsByteArray()));
			Assert.AreEqual(expected, preparedMessage.ToString());
		}

		[Test]
		public virtual void TestPrepareFromStructureTimestampVersion40()
		{
			SessionParams.FixVersion = FixVersion.Fix40;
			Pmu = new PreparedMessageUtil(SessionParams);
			var preparedMessage = Pmu.PrepareMessage("A", new MessageStructure());
			var expected =
				"8=FIX.4.0\u00019=080\u000135=A\u000134=     \u000149=admin3\u000156=admin\u000150=ssId\u0001"
							+ "57=ee\u000152=                 \u000198=0\u0001108=30\u000110=   \u0001";
			Assert.AreEqual(expected, StringHelper.NewString(preparedMessage.AsByteArray()));
			Assert.AreEqual(expected, preparedMessage.ToString());
		}

		[Test]
		public virtual void TestPrepareFromStructureTimestampVersion44()
		{
			SessionParams.FixVersion = FixVersion.Fix44;
			Pmu = new PreparedMessageUtil(SessionParams);
			var preparedMessage = Pmu.PrepareMessage("A", new MessageStructure());
			var expected =
				"8=FIX.4.4\u00019=084\u000135=A\u000134=     \u000149=admin3\u000156=admin\u000150=ssId\u0001"
							+ "57=ee\u000152=                     \u000198=0\u0001108=30\u000110=   \u0001";
			Assert.AreEqual(expected, StringHelper.NewString(preparedMessage.AsByteArray()));
			Assert.AreEqual(expected, preparedMessage.ToString());
		}

		[Test]
		public virtual void TestPrepareFromTemplateWithHeaderFields()
		{
			var ms = new MessageStructure();
			ms.Reserve(35, 1);
			ms.Reserve(9, 3, ValueType.Long);
			ms.Reserve(34, 2, ValueType.Long);
			ms.Reserve(1, 5, ValueType.Long);

			var list = new FixMessage();
			list.AddTag(35, "A");
			list.AddTag(9, (long)1);
			list.AddTag(34, (long)1);
			list.AddTag(49, "S");
			list.AddTag(56, "T");
			list.AddTag(1, "AA");


			var preparedMessage = Pmu.PrepareMessage(list, ms);
			var expected = "8=FIX.4.2\u00019=089\u000135=A\u000134=  \u000149=admin3\u000156=admin\u000150=ssId\u0001"
							+ "57=ee\u000152=                     \u000198=0\u0001108=30\u00011=     \u000110=   \u0001";
			Assert.AreEqual(expected, StringHelper.NewString(preparedMessage.AsByteArray()));
			Assert.AreEqual(expected, preparedMessage.ToString());
			Assert.IsTrue(preparedMessage.IsMessageBufferContinuous, "Message isn't placed in single buffer");

			preparedMessage.Set(9, 1234);
			var updatedExpected = "8=FIX.4.2\u00019=1234\u000135=A\u000134=  \u000149=admin3\u000156=admin\u000150=ssId"
									+ "\u000157=ee\u000152=                     \u000198=0\u0001108=30\u00011=     \u000110=   \u0001";
			Assert.AreEqual(updatedExpected, StringHelper.NewString(preparedMessage.AsByteArray()));
			Assert.IsFalse(preparedMessage.IsMessageBufferContinuous, "Message is placed in single buffer");
		}

		[Test]
		public virtual void TestPrepareFromStrTemplateWithHeaderField()
		{
			var ms = new MessageStructure();
			ms.Reserve(35, 1);
			ms.Reserve(9, 3, ValueType.Long);
			ms.Reserve(34, MessageStructure.VariableLength, ValueType.Long);

			var template = "8=FIX.4.2\u00019=72\u000135=A\u000134=1\u000149=initiator\u000156=acceptor\u0001"
							+ "52=20121113-16:42:26.398\u000198=0\u0001108=30\u000110=037\u0001";


			var preparedMessage = Pmu.PrepareMessageFromString(template.AsByteArray(), "A", ms);
			var expected = "8=FIX.4.2\u00019=080\u000135=A\u000134=1\u000149=admin3\u000156=admin\u000150=ssId\u0001"
							+ "57=ee\u000152=                     \u000198=0\u0001108=30\u000110=037\u0001";
			Assert.AreEqual(expected, StringHelper.NewString(preparedMessage.AsByteArray()));
			Assert.AreEqual(expected, preparedMessage.ToString());
			Assert.IsFalse(preparedMessage.IsMessageBufferContinuous, "Message is placed in single buffer");

			preparedMessage.Set(9, 1234);
			var updatedExpected = "8=FIX.4.2\u00019=1234\u000135=A\u000134=1\u000149=admin3\u000156=admin\u0001"
									+ "50=ssId\u000157=ee\u000152=                     \u000198=0\u0001108=30\u000110=037\u0001";
			Assert.AreEqual(updatedExpected, StringHelper.NewString(preparedMessage.AsByteArray()));
		}

		[Test]
		public virtual void TestFromLagerStructure()
		{
			Assert.Throws<PreparedMessageException>(() =>
			{
				var ms = new MessageStructure();
				ms.Reserve(108, 2);
				//next fields are absent in template - error
				ms.Reserve(1, 3);
				ms.Reserve(2, MessageStructure.VariableLength);

				var template = "8=FIX.4.2\u00019=72\u000135=A\u000134=1\u000149=initiator\u000156=acceptor\u0001" +
								"52=20121113-16:42:26.398\u000198=0\u0001108=30\u000110=01F\u0001";

				var preparedMessage = Pmu.PrepareMessageFromString(template.AsByteArray(), "A", ms);
			});
		}

		[Test]
		public virtual void TestSeveralSequentialFieldsWithUnlimSize()
		{
			var ms = new MessageStructure();
			ms.Reserve(38, MessageStructure.VariableLength);
			ms.Reserve(39, MessageStructure.VariableLength);
			var pm = Pmu.PrepareMessage("D", ms);
			var expected =
				"8=FIX.4.2\u00019=080\u000135=D\u000134=     \u000149=admin3\u000156=admin\u000150=ssId\u000157=ee\u000152=                     \u000138=\u000139=\u000110=   \u0001";
			Assert.AreEqual(expected, pm.ToString());
		}

		[Test]
		public virtual void TestPrepareMessageWithWrongStructure()
		{
			Assert.Throws<PreparedMessageException>(() =>
			{
				var list = new FixMessage();
				list.AddTag(1, "TestValue1");
				var ms = new MessageStructure();
				ms.Reserve(1, 9);
				//field 2 is not defined in structure - this is mistake!
				ms.Reserve(2, 5);
				var pm = Pmu.PrepareMessage(list, "A", ms);
			});
		}

		[Test]
		public virtual void TestMsgWithoutMsgType()
		{
			Assert.Throws<FieldNotFoundException>(() =>
			{
				var ms = new MessageStructure();
				ms.Reserve(1, 1);

				var list = new FixMessage();
				list.AddTag(1, "TestValue1");

				var preparedMessage = Pmu.PrepareMessage(list, ms);
			});
		}

		[Test]
		public virtual void TestLongMessage()
		{
			//String mdTemplate = "8=FIX.4.4\u00019=0000000\u000135=W\u000134=2301\u000149=SPOTEX\u000156=GSCLIENT\u000152=20130313-12:33:25.016\u0001262=MD1\u000155=EUR/USD\u0001268=10\u0001269=0\u0001270=1.29893\u0001271=2000000\u0001269=0\u0001270=1.29886\u0001271=2000000\u0001269=0\u0001270=1.29879\u0001271=2000000\u0001269=0\u0001270=1.29872\u0001271=2000000\u0001269=0\u0001270=1.28997\u0001271=2500000\u0001269=0\u0001270=1.28775\u0001271=2700000\u0001269=1\u0001270=1.29912\u0001271=2000000\u0001269=1\u0001270=1.29919\u0001271=2000000\u0001269=1\u0001270=1.29926\u0001271=2000000\u0001269=1\u0001270=1.29933\u0001271=2000000\u000110=113\u0001";
			var mdTemplate =
				"8=FIX.4.2\u00019=158\u000135=W\u000134=     \u000149=admin3\u000156=admin\u000150=ssId\u000157=ee\u000152=                     \u0001262=MD1\u000155=EUR/USD\u0001268=10\u0001"
								+ "269=0\u0001270=1.29893\u0001271=2000000\u0001"
								+ "269=0\u0001270=1.29886\u0001271=2000000\u000110=113\u0001";
			var ms = new MessageStructure();
			var pm = Pmu.PrepareMessageFromString(mdTemplate.AsByteArray(), ms);
			Assert.AreEqual(mdTemplate, pm.ToString());
		}

		[Test]
		public virtual void SetShorterVarLengthValue()
		{
			var ms = new MessageStructure();
			ms.ReserveString(55, MessageStructure.VariableLength);

			var fixFields = Pmu.PrepareMessage("D", ms);
			fixFields.Set(55, "12");
			fixFields.Set(55, "1"); // <- exception on 2.16.2 here!!!

			Assert.AreEqual("8=FIX.4.2\u00019=076\u000135=D\u000134=     \u000149=admin3\u000156=admin\u0001"
							+ "50=ssId\u000157=ee\u000152=                     \u000155=1\u000110=   \u0001",
				fixFields.ToString());
		}

		[Test]
		public virtual void SetLongerVarLengthValue()
		{
			var ms = new MessageStructure();
			ms.ReserveString(55, MessageStructure.VariableLength);

			var fixFields = Pmu.PrepareMessage("D", ms);
			fixFields.Set(55, "12");
			fixFields.Set(55, "1234");

			Assert.AreEqual("8=FIX.4.2\u00019=076\u000135=D\u000134=     \u000149=admin3\u000156=admin\u0001"
							+ "50=ssId\u000157=ee\u000152=                     \u000155=1234\u000110=   \u0001",
				fixFields.ToString());
		}

		[Test]
		public virtual void SetShorterStringValue()
		{
			var ms = new MessageStructure();
			ms.ReserveString(55, 3);

			var fixFields = Pmu.PrepareMessage("D", ms);
			fixFields.Set(55, "123");
			Assert.AreEqual("8=FIX.4.2\u00019=079\u000135=D\u000134=     \u000149=admin3\u000156=admin\u0001"
							+ "50=ssId\u000157=ee\u000152=                     \u000155=123\u000110=   \u0001",
				fixFields.ToString());

			fixFields.Set(55, "12");
			Assert.AreEqual("8=FIX.4.2\u00019=079\u000135=D\u000134=     \u000149=admin3\u000156=admin\u0001"
							+ "50=ssId\u000157=ee\u000152=                     \u000155=12 \u000110=   \u0001",
				fixFields.ToString());
		}

		[Test]
		public virtual void SetLongerStringValue()
		{
			var ms = new MessageStructure();
			ms.ReserveString(55, 3);

			var fixFields = Pmu.PrepareMessage("D", ms);

			//set longer value
			fixFields.Set(55, "1234");
			Assert.AreEqual("8=FIX.4.2\u00019=079\u000135=D\u000134=     \u000149=admin3\u000156=admin\u0001"
							+ "50=ssId\u000157=ee\u000152=                     \u000155=1234\u000110=   \u0001",
				fixFields.ToString());

			//set shorter value
			fixFields.Set(55, "1");
			Assert.AreEqual("1", fixFields.GetTagValueAsString(55));
			Assert.AreEqual("8=FIX.4.2\u00019=079\u000135=D\u000134=     \u000149=admin3\u000156=admin\u0001"
							+ "50=ssId\u000157=ee\u000152=                     \u000155=1\u000110=   \u0001",
				fixFields.ToString());
		}

		[Test]
		public virtual void ItShouldBePossiblyToSetShorterStringThenPreviousTime()
		{
			var ms = new MessageStructure();
			ms.ReserveString(55, MessageStructure.VariableLength);
			ms.ReserveString(11, MessageStructure.VariableLength);

			var fixFields = Pmu.PrepareMessage("D", ms);
			fixFields.Set(55, "123456");
			fixFields.Set(55, "1234");
			fixFields.Set(55, "12345");
			Assert.AreEqual("12345", fixFields.GetTagValueAsString(55));
		}

		[Test]
		public virtual void TestPrepareFromTemplateWithNegativeValues()
		{
			var ms = new MessageStructure();
			ms.Reserve(35, 1);
			ms.Reserve(10001, 15);
			ms.Reserve(10002, 15);
			ms.Reserve(10003, 15, ValueType.Long);
			ms.Reserve(10004, 15, ValueType.Long);
			ms.Reserve(10005, 15, ValueType.Double);
			ms.Reserve(10006, 15, ValueType.Double);

			var list = new FixMessage();
			list.Set(35, "A");
			list.Set(10001, 1);
			list.Set(10002, 1);
			list.Set(10003, 1);
			list.Set(10004, 1);
			list.Set(10005, 1);
			list.Set(10006, 1);

			var preparedMessage = Pmu.PrepareMessage(list, ms);

			preparedMessage.Set(35, "A");
			preparedMessage.Set(10001, 1234.567, 4);
			preparedMessage.Set(10002, -56789.123, 4);
			preparedMessage.Set(10003, 1234);
			preparedMessage.Set(10004, -56789);
			preparedMessage.Set(10005, 1234.56789, 4);
			preparedMessage.Set(10006, -56789.123456, 4);

			Assert.AreEqual(1234.567, preparedMessage.GetTagValueAsDouble(10001));
			Assert.AreEqual(-56789.123, preparedMessage.GetTagValueAsDouble(10002));
			Assert.AreEqual(1234, preparedMessage.GetTagValueAsLong(10003));
			Assert.AreEqual(-56789, preparedMessage.GetTagValueAsLong(10004));
			Assert.AreEqual(1234.5679, preparedMessage.GetTagValueAsDouble(10005));
			Assert.AreEqual(-56789.1235, preparedMessage.GetTagValueAsDouble(10006));

			var expected =
				"8=FIX.4.2\u00019=216\u000135=A\u000134=     \u000149=admin3\u000156=admin\u000150=ssId\u0001"
							+ "57=ee\u000152=                     \u000198=0\u0001108=30\u0001"
							+ "10001=00000001234.567\u000110002=-0000056789.123\u000110003=000000000001234\u0001"
							+ "10004=-00000000056789\u000110005=0000001234.5679\u000110006=-000056789.1235\u000110=   \u0001";
			Assert.AreEqual(expected, StringHelper.NewString(preparedMessage.AsByteArray()));
			Assert.AreEqual(expected, preparedMessage.ToString());
			Assert.IsTrue(preparedMessage.IsMessageBufferContinuous, "Message isn't placed in single buffer");
		}

		[Test]
		public virtual void TestFixTypeFormatsWithPadding()
		{
			var buffer = new byte[10];
			var span = buffer.AsSpan();

			var length = FixTypes.FormatIntWithPadding(123456, 10, span);
			Assert.AreEqual(10, length);
			Assert.AreEqual("0000123456", StringHelper.NewString(buffer));

			span.Clear();
			length = FixTypes.FormatIntWithPadding(-12345, 10, span);
			Assert.AreEqual(10, length);
			Assert.AreEqual("-000012345", StringHelper.NewString(buffer));

			buffer = new byte[15];
			FixTypes.FormatDoubleWithPadding(123.45, 5, 15, buffer, 0);
			Assert.AreEqual("000000000123.45", StringHelper.NewString(buffer));

			buffer = new byte[15];
			FixTypes.FormatDoubleWithPadding(-123.4567, 3, 15, buffer, 0);
			Assert.AreEqual("-0000000123.457", StringHelper.NewString(buffer));
		}

		[Test]
		public virtual void TestDoubleOverflow()
		{
			var msg = "8=FIX.4.4\u00019=319\u000135=X\u000134=2140928\u000149=SGM-MARKET\u000156=SGM-MARKETA\u0001"
						+ "52=20190109-18:18:21.366\u0001268=02\u0001"
						+ "279=0\u0001278=00000003\u0001270=1\u0001"
						+ "279=1\u0001278=00000002\u0001270=1\u0001"
						+ "10=125\u0001";
			var list = RawFixUtil.GetFixMessage(msg.AsByteArray());

			var structure = new MessageStructure();
			structure.Reserve(270, 8);
			structure.Reserve(270, 8);
			var template = Pmu.PrepareMessage(list, structure);

			// rounded double is exact 8 chars length, if not rounded - length is 12 (5 numbers after dot)
			template.Set(270, 1, 111111.1999999, 5);
			template.Set(270, 2, 111111.1999999, 5);

			Assert.AreEqual(111111.2, template.GetTagValueAsDouble(270, 1));
			Assert.AreEqual(111111.2, template.GetTagValueAsDouble(270, 2));

			//repeat with the same value again and make sure that nothing corrupted
			template.Set(270, 1, 111111.1999999, 5);
			template.Set(270, 2, 111111.1999999, 5);

			Assert.AreEqual(111111.2, template.GetTagValueAsDouble(270, 1));
			Assert.AreEqual(111111.2, template.GetTagValueAsDouble(270, 2));

			//one more check with normal value
			template.Set(270, 1, 11.1111, 5);
			template.Set(270, 2, 11.1111, 5);

			Assert.AreEqual(11.1111, template.GetTagValueAsDouble(270, 1));
			Assert.AreEqual(11.1111, template.GetTagValueAsDouble(270, 2));
		}
	}
}
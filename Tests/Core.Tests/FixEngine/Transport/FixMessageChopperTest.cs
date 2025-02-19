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
using System.IO;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Transport
{
    internal class FixMessageChopperTest : AbstractMessageChopperTest
	{
		public override IMessageChopper GetInstanceChopper(byte[] messages, int[] parts, int maxMessageSize, int optimalBufferLength, bool validateCheckSum)
		{
			return new FixMessageChopper(new ReadStreamTransport(new MemoryStream(messages), parts), maxMessageSize, optimalBufferLength, validateCheckSum, false);
		}

		public override IMessageChopper GetInstanceChopper(byte[] messages, int maxMessageSize, int optimalBufferLength, bool validateCheckSum, bool markInMessageTime, int milliseconds)
		{
			var inputStreamMock = GetDelayedInputStreamMock(milliseconds, messages);
			return new FixMessageChopper(new ReadStreamTransport(inputStreamMock, new int[]{ messages.Length }), maxMessageSize, optimalBufferLength, true, true);
		}

		[Test]
		public virtual void RegressionOfBug12193()
		{
			var msg1 = "8=FIX.4.4\u00019=209\u000135=AK\u000149=SNDR\u000156=TRGT\u000134=2\u000150=30737\u000152=20030204-08:46:14\u0001664=0001\u0001666=0\u0001773=1\u0001665=4\u000160=20060217-10:00:00\u000175=20060217\u000155=TESTA\u000180=1000\u000154=1\u0001862=2\u0001528=A\u0001863=400\u0001528=P\u0001863=600\u000179=KTierney\u00016=20\u0001381=20000\u0001118=2000\u000110=125";
			var msg2 = "8=FIX.4.4\u00019=209\u000135=AK\u000149=SNDR\u000156=TRGT\u000134=3\u000150=30737\u000152=20030204-08:46:14\u0001664=0001\u0001666=0\u0001773=1\u0001665=4\u000160=20060217-10:00:00\u000175=20060217\u000155=TESTA\u000180=1000\u000154=1\u0001862=2\u0001528=A\u0001863=400\u0001528=P\u0001863=600\u000179=KTierney\u00016=20\u0001381=20000\u0001118=2000\u000110=126\u0001";
			var concatenatedMessages = CombineMessages(new string[]{ msg1, msg2 });
			var messageChopper = GetInstanceChopper(concatenatedMessages.AsByteArray(), 1024, 100);
			var buf = new MsgBuf();
			do
			{
				messageChopper.ReadMessage(buf);
			} while (messageChopper.IsMessageGarbled);
			var readMessage = StringHelper.NewString(buf.Buffer, buf.Offset, buf.Length);
			ClassicAssert.That(readMessage, Is.EqualTo(msg2));
			ClassicAssertEndOfFile(messageChopper);
		}

		[Test]
		public virtual void OneGarbledMessage()
		{
			var msg = CreateValidMessage();
			for (var i = 1; i < msg.Length; i++)
			{
				TestChoppingWithEndOfFile(new string[]{ msg.Substring(0, i) }, new MessageState[]{ MessageState.Garbled });
			}
		}

		[Test]
		public virtual void GarbledMessageThenNonGarbledMessage()
		{
			var msg = CreateValidMessage();
			for (var i = 1; i < msg.Length; i++)
			{
				TestChopping(new string[]{ msg.Substring(0, i), msg }, new MessageState[]{ MessageState.Garbled, MessageState.NonGarbled });
			}
		}

		[Test]
		public virtual void GarbledMessageWithTooBigBodyLengthThenNonGarbledMessage()
		{
			var msg1 = CreateValidMessageAndReplaceBodyLengthTo(CreateValidMessage().Length);
			var msg2 = CreateValidMessage();
			TestChopping(new string[]{ msg1, msg2 }, new MessageState[]{ MessageState.Garbled, MessageState.NonGarbled });
		}

		[Test]
		public virtual void GarbledMessageWithTooLowBodyLengthThenNonGarbledMessage()
		{
			var msg1 = CreateValidMessageAndReplaceBodyLengthTo(1);
			var msg2 = CreateValidMessage();
			TestChopping(new string[]{ msg1, msg2 }, new MessageState[]{ MessageState.Garbled, MessageState.NonGarbled });
		}

		[Test]
		public virtual void GarbledMessageWithZeroBodyLengthThenNonGarbledMessage()
		{
			var msg1 = CreateValidMessageAndReplaceBodyLengthTo(0);
			var msg2 = CreateValidMessage();
			TestChopping(new string[]{ msg1, msg2 }, new MessageState[]{ MessageState.Garbled, MessageState.NonGarbled });
		}

		[Test]
		public virtual void InvalidBeginStringTag()
		{
			var msg = CreateValidMessageAndReplaceField(8, "9");
			TestErrorReportingWithoutEof(msg, GarbledMessageError.Field8TagExpected, 0);
		}

		[Test]
		public virtual void InvalidTagAndValueSeparatorOfBeginStringField()
		{
			var msg = CreateValidMessageAndReplaceField(8, "8#FIX.4.0\u0001");
			TestErrorReportingWithoutEof(msg, GarbledMessageError.Field8TagValueDelimiterExpected, 1);
		}

		[Test]
		public virtual void InvalidBodyLengthTag()
		{
			var msg = CreateValidMessageAndReplaceField(9, "7=10\u0001");
			TestErrorReportingWithoutEof(msg, GarbledMessageError.Field9TagExpected, 10);
		}

		[Test]
		public virtual void InvalidTagAndValueSeparatorOfBodyLengthField()
		{
			var msg = CreateValidMessageAndReplaceField(9, "9#10\u0001");
			TestErrorReportingWithoutEof(msg, GarbledMessageError.Field9TagValueDelimiterExpected, 11);
		}

		[Test]
		public virtual void InvalidBodyLengthValue()
		{
			var msg = CreateValidMessageAndReplaceField(9, "9=A\u0001");
			TestErrorReportingWithoutEof(msg, GarbledMessageError.Field9DecimalValueExpected, 12);
		}

		[Test]
		public virtual void InvalidChecksumTag()
		{
			var msg = CreateValidMessageAndReplaceField(10, "11=217\u0001");
			TestErrorReportingWithoutEof(msg, GarbledMessageError.Field10TagExpected, msg.Length - 6);
		}

		[Test]
		public virtual void InvalidChecksumValue()
		{
			var msg = CreateValidMessageAndReplaceField(10, "10=333\u0001");
			TestErrorReporting(msg, GarbledMessageError.Field10InvalidChecksum, msg.Length - 4);
		}

		[Test]
		public virtual void InvalidTagAndValueSeparatorOfChecksumField()
		{
			var msg = CreateValidMessageAndReplaceField(10, "10#217\u0001");
			TestErrorReportingWithoutEof(msg, GarbledMessageError.Field10TagValueDelimiterExpected, msg.Length - 5);
		}

		[Test]
		public virtual void TooLongChecksumValue()
		{
			var msg = CreateValidMessageAndReplaceField(10, "10=0217\u0001");
			TestErrorReportingWithoutEof(msg, GarbledMessageError.Field10FieldDelimiterExpected, msg.Length - 2);
		}

		[Test]
		public virtual void EmptyChecksumValue()
		{
			var msg = CreateValidMessageAndReplaceField(10, "10=\u0001");
			TestErrorReportingWithoutEof(msg, GarbledMessageError.Field10DecimalValueExpected, msg.Length - 1);
		}

		[Test]
		public virtual void ShortChecksumValue()
		{
			var msg = CreateValidMessageAndReplaceField(10, "10=00\u0001");
			TestErrorReportingWithoutEof(msg, GarbledMessageError.Field10DecimalValueExpected, msg.Length - 3);
		}

		[Test]
		public virtual void LastFieldSeparatorMissed()
		{
			var validMsg = CreateValidMessage();
			var msg = validMsg.Substring(0, validMsg.Length - 1);
			TestErrorReportingWithoutEof(msg, GarbledMessageError.Field10FieldDelimiterExpected, msg.Length);
		}

		[Test]
		public virtual void InvalidLastFieldSeparator()
		{
			var validMsg = CreateValidMessage();
			var msg = validMsg.Substring(0, validMsg.Length - 1) + "\u0000";
			TestErrorReportingWithoutEof(msg, GarbledMessageError.Field10FieldDelimiterExpected, msg.Length - 1);
		}

		/// <summary>
		/// there was a problem with pasing messages from linux telnet - it adds extra '/n' to the end of line </summary>
		/// <exception cref="IOException"> </exception>
		[Test]
		public virtual void CheckGarbageAfterLogon()
		{
			var msg = "8=FIXT.1.1\u00019=80\u000135=A\u000134=1\u000149=CLIENT1\u000152=20160816-09:16:15.107\u000156=SERVER\u000198=0\u0001108=5\u0001141=Y\u00011137=9\u000110=091\u0001";
			var garbage = "\n";

			var messages = msg + garbage;
			var messageChopper = GetInstanceChopper(messages.AsByteArray(), 1024, 100);
			var msgBuf = new MsgBuf();
			messageChopper.ReadMessage(msgBuf);
			var received = StringHelper.NewString(msgBuf.Buffer, msgBuf.Offset, msgBuf.Length);
			ClassicAssert.AreEqual(msg, received);
			try
			{
				messageChopper.ReadMessage(msgBuf);
			}
			catch (IOException)
			{
			}

			ClassicAssert.IsTrue(messageChopper.IsMessageGarbled);
			ClassicAssert.AreEqual(messageChopper.Error, GarbledMessageError.Field8TagExpected);
		}

		[Test]
		public virtual void CheckReadingTimeForInValidMessages()
		{
			var inValidMessage = "8=FIX.4.2\u00019=98\u000135=B\u000134=9\u000149=SCHB\u000156=BLP";
			var validMessage = "8=FIX.4.2\u00019=98\u000135=B\u000134=9\u000149=SCHB\u000156=BLP\u000152=20180802-08:05:57.913\u0001148=Hello there\u000133=3\u000158=line1\u000158=line2\u000158=line3\u000110=107\u0001";
			var messages = new string[]{ inValidMessage, validMessage };
			var msg = CombineMessages(messages);
			var msgBytes = msg.AsByteArray();
			var maxMessageSize = validMessage.AsByteArray().Length;
			var milliseconds = 100;
			// changing optimalBufferLength value can be used for getting single message or all messages for one reading
			var optimalBufferLength = validMessage.AsByteArray().Length;

			var messageChopper = GetInstanceChopper(msgBytes, maxMessageSize, optimalBufferLength, true, true, milliseconds);

			var inMessage = ReadAsString(messageChopper, new MsgBuf());
			var timeEnd = DateTimeHelper.CurrentTicks;
			ClassicAssert.AreEqual(messages[0], inMessage);
			ClassicAssert.Less(Math.Abs(timeEnd - messageChopper.MessageReadTimeInTicks), milliseconds * TimeSpan.TicksPerMillisecond);
			inMessage = ReadAsString(messageChopper, new MsgBuf());
			timeEnd = DateTimeHelper.CurrentTicks;
			ClassicAssert.AreEqual(messages[1], inMessage);
			ClassicAssert.Less(Math.Abs(timeEnd - messageChopper.MessageReadTimeInTicks), 2 * TimeSpan.TicksPerMillisecond);
		}
	}
}
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
using System.Text;
using System.Threading;
using Epam.FixAntenna.Fix.Message;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Transport
{
    [TestFixture]
	internal abstract class AbstractMessageChopperTest
	{
		internal const string Msg1 = "8=FIX.4.2\u00019=269\u000135=8\u0001128=IAEIR5\u000149=INET\u000156=IAEID3\u000143=N\u000134=737\u000152=20100831-13:23:39\u0001122=20100831-13:23:39\u000150=INET\u000111=*003YeG001\u000137=4366053\u000117=-489178\u000120=0\u0001150=0\u000139=0\u0001109=IAEI\u000155=UPRO\u000154=1\u000138=200\u000132=0\u000131=0.0\u0001151=200\u000114=0\u00016=0.0\u00011=CMT2\u000144=117.8900\u000140=2\u000147=P\u000129=4\u000176=INET\u000118=N\u000159=0\u000158=Open\u000110=064\u0001";
		internal static readonly FixMessage Message = RawFixUtil.GetFixMessage(Msg1.AsByteArray());

		/// <param name="messages"> raw data for test </param>
		/// <returns> instance of chopper </returns>
		public virtual IMessageChopper GetInstanceChopper(byte[] messages)
		{
			return GetInstanceChopper(messages, new int[]{ messages.Length }, 1024 * 1024, 512, true);
		}

		public virtual IMessageChopper GetInstanceChopper(byte[] messages, int[] parts)
		{
			return GetInstanceChopper(messages, parts, 1024 * 1024, 512, true);
		}

		public virtual IMessageChopper GetInstanceChopper(byte[] messages, int maxMessageSize, int optimalBufferLength)
		{
			return GetInstanceChopper(messages, new int[]{ messages.Length }, maxMessageSize, optimalBufferLength, true);
		}

		public virtual IMessageChopper GetInstanceChopper(byte[] messages, int maxMessageSize, int optimalBufferLength, bool validateCheckSum)
		{
			return GetInstanceChopper(messages, new int[]{ messages.Length }, maxMessageSize, optimalBufferLength, validateCheckSum);
		}

		public abstract IMessageChopper GetInstanceChopper(byte[] messages, int[] parts, int maxMessageSize, int optimalBufferLength, bool validateCheckSum);

		public abstract IMessageChopper GetInstanceChopper(byte[] messages, int maxMessageSize, int optimalBufferLength, bool validateCheckSum, bool markInMessageTime, int milliseconds);

		[Test]
		public virtual void TestCheckSum()
		{
			var msg = CreateValidMessage();
			var messageChopper = GetInstanceChopper(CreateValidMessage().AsByteArray(), 1024, 10000, true);
			var buf = new MsgBuf();

			messageChopper.ReadMessage(buf);
			Assert.IsFalse(messageChopper.IsMessageGarbled);
			var readMessage = StringHelper.NewString(buf.Buffer, buf.Offset, buf.Length);
			Assert.That(readMessage, Is.EqualTo(msg));
		}

		[Test]
		public virtual void ReceivedALotOfMessages()
		{
			var msgNum = 1000;
			var messages = GenerateManyMessages(msgNum);
			var messageChopper = GetInstanceChopper(messages.AsByteArray());

			var buf = new MsgBuf();
			for (var i = 0; i < msgNum; i++)
			{
				messageChopper.ReadMessage(buf);
				UpdateMessage(i);
				AssertValidMessage(buf, Message.AsByteArray());
				buf.FixMessage.Clear();
			}
			AssertEndOfFile(messageChopper);
		}

		[Test]
		public virtual void TestShiftBuffer()
		{
			var msgNum = 1000;
			var messages = GenerateManyMessages(msgNum);
			var messageChopper = GetInstanceChopper(messages.AsByteArray());

			var buf = new MsgBuf();
			var alwaysShiftBuf = true;
			var alwaysIncreaseBuf = true;
			var previousOffset = 0;
			for (var i = 0; i < msgNum; i++)
			{
				messageChopper.ReadMessage(buf);
				if (buf.Offset != 0)
				{
					alwaysShiftBuf = false;
				}
				if (buf.Offset < previousOffset)
				{
					alwaysIncreaseBuf = false;
				}
				previousOffset = buf.Offset;
				buf.FixMessage.Clear();
			}
			Assert.IsFalse(alwaysShiftBuf, "Chopper make unnecessary shift");
			Assert.IsFalse(alwaysIncreaseBuf, "Chopper don't make shift");
			AssertEndOfFile(messageChopper);
		}

		[Test]
		public virtual void CheckMaxMessageSize()
		{
			try
			{
				var messages = Message.ToString();
				var messageChopper = GetInstanceChopper(messages.AsByteArray(), 10, 10000);
				Assert.Throws<GarbledMessageException>(() => messageChopper.ReadMessage(new MsgBuf()));
			}
			catch (IOException e)
			{
				Assert.That(e.Message, Does.StartWith(MessageChopperFields.MessageIsTooLongError));
				throw;
			}
		}

		[Test]
		public virtual void SkipMaxMessageSizeCheck()
		{
			var messages = Message.ToString();
			var messageChopper = GetInstanceChopper(messages.AsByteArray(), -1, 10000);

			var readMessage = ReadAsString(messageChopper, new MsgBuf());
			Assert.That(readMessage, Is.EqualTo(Message.ToString()));
			AssertEndOfFile(messageChopper);
		}

		private string GenerateManyMessages(int size)
		{
			var sb = new StringBuilder();
			for (var i = 0; i < size; i++)
			{
				UpdateMessage(i);
				sb.Append(Message.ToString());
			}
			return sb.ToString();
		}

		private void UpdateMessage(int i)
		{
			Message.Set(34, i);
			Message.Set(9, Message.CalculateBodyLength());
			Message.Set(10, FixTypes.FormatCheckSum(Message.CalculateChecksum()));
		}

		[Test]
		public virtual void TooLongMessage()
		{
			var tooLongMsg = "8=FIX.4.4\u00019=209\u000135=AK\u000149=SNDR\u000156=TRGT\u000134=3\u000150=30737\u000152=20030204-08:46:14\u0001664=0001\u0001666=0\u0001773=1\u0001665=4\u000160=20060217-10:00:00\u000175=20060217\u000155=TESTA\u000180=1000\u000154=1\u0001862=2\u0001528=A\u0001863=400\u0001528=P\u0001863=600\u000179=KTierney\u00016=20\u0001381=20000\u0001118=2000\u000110=126\u0001";
			var msg = CreateValidMessage();
			var concatenatedMessages = CombineMessages(new string[]{ tooLongMsg, msg });
			var messageChopper = GetInstanceChopper(concatenatedMessages.AsByteArray(), 100, 100);
			var buf = new MsgBuf();
			try
			{
				messageChopper.ReadMessage(buf);
				Assert.Fail("Expected IOException with following error message: " + MessageChopperFields.MessageIsTooLongError);
			}
			catch (GarbledMessageException ex)
			{
				Console.WriteLine(ex.ToString());
				Console.Write(ex.StackTrace);
				Assert.That(ex.Message, Does.StartWith(MessageChopperFields.MessageIsTooLongError));
			}
			string readMessage;
			do
			{
				readMessage = ReadAsString(messageChopper, buf);
			} while (messageChopper.IsMessageGarbled);
			Assert.That(readMessage, Is.EqualTo(msg));
			AssertEndOfFile(messageChopper);
		}

		[Test]
		public virtual void MessageIsLongerThatOptimalBuffer()
		{
			var longMsg = "8=FIX.4.4\u00019=21\u000135=0\u000149=TRGT\u000156=SNDR\u000110=157\u0001";
			var messageChopper = GetInstanceChopper(longMsg.AsByteArray(), 1024 * 1024, longMsg.Length / 3);
			var buf = new MsgBuf();
			messageChopper.ReadMessage(buf);
			AssertValidMessage(buf, longMsg.AsByteArray());
		}

		[Test]
		public virtual void OneNonGarbledMessage()
		{
			var msg = CreateValidMessage();
			TestChopping(new string[]{ msg }, new MessageState[]{ MessageState.NonGarbled });
			TestErrorReporting(msg, null, -1);
		}

		[Test]
		public virtual void TwoNonGarbledMessages()
		{
			var msg = CreateValidMessage();
			TestChopping(new string[]{ msg, msg }, new MessageState[]{ MessageState.NonGarbled, MessageState.NonGarbled });
		}

		[Test]
		public virtual void TestValidateCheckSumFlag()
		{
			var expectedMessage = CreateValidMessageAndReplaceField(10, "10=333\u0001");

			var messageChopper = GetInstanceChopper(expectedMessage.AsByteArray(), 1024, 100, false);
			var actualMessage = ReadAsString(messageChopper, new MsgBuf());
			Assert.IsNull(messageChopper.Error, "Should be no parsing errors but " + messageChopper.Error);
			Assert.AreEqual(expectedMessage, actualMessage);
			AssertEndOfFile(messageChopper);
		}

		[Test]
		public virtual void TestStopParse()
		{
			var instance = GetInstanceChopper(Msg1.AsByteArray(), 1024 * 1024, 512, true);
			instance.SetUserParserListener(new FixParserListenerAnonymousInnerClass());
			var list = ReadMessage(instance, new MsgBuf());
			Assert.AreEqual(12, list.Length);
			foreach (var f in list)
			{
				Assert.IsTrue(ParseRequiredTags.IsRequired(f.TagId));
			}
		}

		private class FixParserListenerAnonymousInnerClass : IFixParserListener
		{
			public void OnMessageStart()
			{
			}

			public void OnMessageEnd()
			{
			}

			public FixParserListenerParseControl OnTag(int tag, byte[] buffer, int offset, int length)
			{
				return FixParserListenerParseControl.StopParse;
			}
		}

		[Test]
		public virtual void TestStopConditionParse()
		{
			var instance = GetInstanceChopper(Msg1.AsByteArray(), 1024 * 1024, 512, true);
			instance.SetUserParserListener(new FixParserListenerAnonymousInnerClass2());
			var list = ReadMessage(instance, new MsgBuf());
			Assert.AreEqual(14, list.Length);
			foreach (var f in list)
			{
				if (!ParseRequiredTags.IsRequired(f.TagId))
				{
					Assert.IsTrue(f.TagId == 11 || f.TagId == 37);
				}
			}
		}

		private class FixParserListenerAnonymousInnerClass2 : IFixParserListener
		{
			public FixParserListenerAnonymousInnerClass2()
			{
				_stop = false;
			}

			private bool _stop;

			public void OnMessageStart()
			{
			}

			public void OnMessageEnd()
			{
			}

			public FixParserListenerParseControl OnTag(int tag, byte[] buffer, int offset, int length)
			{
				if (_stop)
				{
					return FixParserListenerParseControl.StopParse;
				}
				if (tag == 37)
				{
					_stop = true;
				}
				return FixParserListenerParseControl.Continue;
			}
		}

		[Test]
		public virtual void TestIgnoreConditionParse()
		{
			var instance = GetInstanceChopper(Msg1.AsByteArray(), 1024 * 1024, 512, true);
			instance.SetUserParserListener(new FixParserListenerAnonymousInnerClass3());

			var actual = ReadMessage(instance, new MsgBuf());
			var expected = RawFixUtil.GetFixMessage(Msg1.AsByteArray());

			Assert.AreEqual(expected.Length - 1, actual.Length);
			foreach (var f in expected)
			{
				if (f.TagId == 37)
				{
					Assert.IsFalse(actual.IsTagExists(f.TagId));
				}
				else
				{
					Assert.IsTrue(actual.IsTagExists(f.TagId));
				}
			}
		}

		private class FixParserListenerAnonymousInnerClass3 : IFixParserListener
		{
			public void OnMessageStart()
			{
			}

			public void OnMessageEnd()
			{
			}

			public FixParserListenerParseControl OnTag(int tag, byte[] buffer, int offset, int length)
			{
				return tag == 37 ? FixParserListenerParseControl.IgnoreTag : FixParserListenerParseControl.Continue;
			}
		}

		[Test]
		public virtual void XmlTagParsing()
		{
			var msg = "8=FIX.4.4\u00019=79\u000135=CE\u000134=2\u000149=senderId\u000156=targetId\u000152=20140602-09:40:31.012\u0001212=8\u0001213=<t>t</t>\u000110=232\u0001";
			var messageChopper = GetInstanceChopper(msg.AsByteArray(), 1024, 100);
			var buf = new MsgBuf();
			do
			{
				messageChopper.ReadMessage(buf);
			} while (messageChopper.IsMessageGarbled);
			var readMessage = StringHelper.NewString(buf.Buffer, buf.Offset, buf.Length);
			Assert.That(readMessage, Is.EqualTo(msg));
			Assert.That(buf.FixMessage.ToString(), Is.EqualTo(msg));
			Assert.That(buf.FixMessage.GetTagValueAsString(213), Is.EqualTo("<t>t</t>"));
			AssertEndOfFile(messageChopper);
		}

		[Test] public virtual void RawTagParsing()
		{
			var msg = "8=FIX.4.4\u00019=127\u000135=B\u000134=2\u000149=senderId\u000156=targetId\u000152=20151204-08:36:01.580\u0001148=Hello there\u000133=3\u000158=line1\u000158=line2\u000158=line3\u000195=10\u000196=test\u0001test\u0001\u000110=034\u0001";
			var messageChopper = GetInstanceChopper(msg.AsByteArray(), 1024, 1024);
			var buf = new MsgBuf();
			do
			{
				messageChopper.ReadMessage(buf);
			} while (messageChopper.IsMessageGarbled);
			var readMessage = StringHelper.NewString(buf.Buffer, buf.Offset, buf.Length);
			Assert.That(readMessage, Is.EqualTo(msg));
			Assert.That(buf.FixMessage.ToString(), Is.EqualTo(msg));
			Assert.That(buf.FixMessage.GetTagValueAsString(96), Is.EqualTo("test\u0001test\u0001"));
			AssertEndOfFile(messageChopper);
		}

		[Test]
		public virtual void RawTagWithTooBigLengthValue()
		{
			try
			{
				var msg = "8=FIX.4.4\u00019=127\u000135=B\u000134=2\u000149=senderId\u000156=targetId\u000152=20151204-08:36:01.580\u0001148=Hello there\u000133=3\u000158=line1\u000158=line2\u000158=line3\u000195=" + int.MaxValue + "\u000196=test\u0001test\u0001\u000110=034\u0001";
				var messageChopper = GetInstanceChopper(msg.AsByteArray(), 1024, 1024);
				var buf = new MsgBuf();
				Assert.Throws<GarbledMessageException>(() =>
				{
					do
					{
						messageChopper.ReadMessage(buf);
					} while (messageChopper.IsMessageGarbled);
				});
			}
			catch (Exception ex)
			{
				Console.Write(ex.StackTrace);
				throw;
			}
		}

		public virtual string CreateValidMessage()
		{
			return "8=FIX.4.0\u00019=5\u000135=j\u000110=217\u0001";
		}

		public virtual string CreateValidMessageAndReplaceBodyLengthTo(int bodyLength)
		{
			return CreateValidMessage().ReplaceAll("9=\\d+\u0001", "9=" + bodyLength + "\u0001");
		}

		public virtual string CreateValidMessageAndReplaceField(int tag, string newField)
		{
			return CreateValidMessage().ReplaceAll(tag + "=.+\u0001", newField);
		}

		public virtual void TestChopping(string[] expectedMessages, MessageState[] expectedMessageStates)
		{
			var concatenatedMessages = CombineMessages(expectedMessages);
			var messageChopper = GetInstanceChopper(concatenatedMessages.AsByteArray(), 1024, 100);
			var msgBuf = new MsgBuf();
			for (int i = 0, length = expectedMessages.Length; i < length; i++)
			{
				var readMessage = ReadAsString(messageChopper, msgBuf);
				var messageState = messageChopper.IsMessageGarbled ? MessageState.Garbled : MessageState.NonGarbled;
				Assert.That(messageState + ": " + readMessage, Is.EqualTo(expectedMessageStates[i] + ": " + expectedMessages[i]));
				if (messageState == MessageState.Garbled)
				{
					Assert.IsTrue(msgBuf.FixMessage.IsMessageIncomplete, "Message is garbled. Must be flag 'incomplete'");
				}
				else
				{
					Assert.IsFalse(msgBuf.FixMessage.IsMessageIncomplete, "Message is not garbled, but have flag 'incomplete'");
					Assert.AreEqual(expectedMessages[i], msgBuf.FixMessage.ToString());
					Assert.AreEqual(expectedMessages[i], IndexedStorageTestHelper.GetMessageBufferAsString(msgBuf.FixMessage));
				}
				msgBuf.FixMessage.Clear();
			}
			AssertEndOfFile(messageChopper);
		}

		public virtual void AssertValidMessage(MsgBuf msgBuf, byte[] expectedBytes)
		{
			AssertValidMessage(msgBuf, expectedBytes, 0, expectedBytes.Length);
		}

		public virtual void AssertValidMessage(MsgBuf msgBuf, byte[] expectedBytes, int offset, int length)
		{
			Assert.IsFalse(msgBuf.FixMessage.IsMessageIncomplete, "Message is not garbled, but have flag 'incomplete'");
			var expectedMsg = StringHelper.NewString(expectedBytes, offset, length);
			Assert.AreEqual(expectedMsg, StringHelper.NewString(msgBuf.Buffer, msgBuf.Offset, msgBuf.Length));
			Assert.AreEqual(expectedMsg, msgBuf.FixMessage.ToString());
			Assert.AreEqual(expectedMsg, IndexedStorageTestHelper.GetMessageBufferAsString(msgBuf.FixMessage));
		}

		public virtual void TestChoppingWithEndOfFile(string[] expectedMessages, MessageState[] expectedMessageStates)
		{
			var concatenatedMessages = CombineMessages(expectedMessages);
			var messageChopper = GetInstanceChopper(concatenatedMessages.AsByteArray(), 1024, 100);
			try
			{
				var msgBuf = new MsgBuf();
				for (int i = 0, length = expectedMessages.Length; i < length; i++)
				{
					var readMessage = ReadAsString(messageChopper, msgBuf);
					var messageState = messageChopper.IsMessageGarbled ? MessageState.Garbled : MessageState.NonGarbled;
					Assert.That(messageState + ": " + readMessage, Is.EqualTo(expectedMessageStates[i] + ": " + expectedMessages[i]));
				}
				Assert.Fail("Should be EOF error");
			}
			catch (IOException e)
			{
				Assert.That(e.Message, Is.EqualTo(MessageChopperFields.EofReadError));
			}
		}

		public virtual void TestErrorReporting(string expectedMessage, GarbledMessageError expectedError, int expectedErrorPosition)
		{
			var messageChopper = GetInstanceChopper(expectedMessage.AsByteArray(), 1024, 100);
			var message = ReadAsString(messageChopper, new MsgBuf());
			Assert.That(Format(message, messageChopper.Error, messageChopper.ErrorPosition), Is.EqualTo(Format(expectedMessage, expectedError, expectedErrorPosition)));
			AssertEndOfFile(messageChopper);
		}

		public virtual void TestErrorReportingWithoutEof(string expectedMessage, GarbledMessageError expectedError, int expectedErrorPosition)
		{
			var messageChopper = GetInstanceChopper((expectedMessage + CreateValidMessage()).AsByteArray(), 1024, 100);
			var message = ReadAsString(messageChopper, new MsgBuf());
			Assert.That(Format(message, messageChopper.Error, messageChopper.ErrorPosition), Is.EqualTo(Format(expectedMessage, expectedError, expectedErrorPosition)));

			ReadAsString(messageChopper, new MsgBuf()); // this is valid message
			AssertEndOfFile(messageChopper);
		}

		public virtual string Format(string message, GarbledMessageError error, int errorPosition)
		{
			return error?.Message + " [Position=" + errorPosition + "]: " + message;
		}

		public virtual string CombineMessages(string[] expectedSlices)
		{
			var buffer = new StringBuilder();
			foreach (var expectedSlice in expectedSlices)
			{
				buffer.Append(expectedSlice);
			}
			return buffer.ToString();
		}

		public enum MessageState
		{
			Garbled,
			NonGarbled
		}

		public virtual void AssertEndOfFile(IMessageChopper messageChopper)
		{
			try
			{
				var buf = new MsgBuf();
				messageChopper.ReadMessage(buf);
				Assert.Fail("Expected IOException with following error message: " + MessageChopperFields.EofReadError + " but occurred: " + StringHelper.NewString(buf.Buffer, buf.Offset, buf.Length));
			}
			catch (IOException ex)
			{
				Assert.That(ex.Message, Is.EqualTo(MessageChopperFields.EofReadError));
			}
		}

		public virtual string ReadAsString(IMessageChopper messageChopper, MsgBuf msgBuf)
		{
			messageChopper.ReadMessage(msgBuf);
			return StringHelper.NewString(msgBuf.Buffer, msgBuf.Offset, msgBuf.Length);
		}

		public virtual FixMessage ReadMessage(IMessageChopper messageChopper, MsgBuf msgBuf)
		{
			messageChopper.ReadMessage(msgBuf);
			return msgBuf.FixMessage;
		}

		private class SlowStream : MemoryStream
		{
			private readonly int _delay;
			public SlowStream(byte[] data, int milliseconds) : base(data)
			{
				_delay = milliseconds;
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				Thread.Sleep(_delay);
				return base.Read(buffer, offset, count);
			}
		}

		public virtual Stream GetDelayedInputStreamMock(int milliseconds, byte[] msgBytes)
		{
			return new SlowStream(msgBytes, milliseconds);
		}

		[Test]
		public virtual void CheckReadingTimeForValidMessages()
		{
			var validMessage = "8=FIX.4.2\u00019=98\u000135=B\u000134=9\u000149=SCHB\u000156=BLP\u000152=20180802-08:05:57.913\u0001148=Hello there\u000133=3\u000158=line1\u000158=line2\u000158=line3\u000110=107\u0001";
			var messages = new string[]{ validMessage, validMessage, validMessage };
			var msg = CombineMessages(messages);
			var msgBytes = msg.AsByteArray();
			var maxMessageSize = validMessage.AsByteArray().Length;
			var milliseconds = 100;
			// changing optimalBufferLength value can be used for getting single message or all messages for one reading
			var optimalBufferLength = validMessage.AsByteArray().Length;

			var messageChopper = GetInstanceChopper(msgBytes, maxMessageSize, optimalBufferLength, true, true, milliseconds);

			for (var i = 0; i < messages.Length; i++)
			{
				var inMessage = ReadAsString(messageChopper, new MsgBuf());
				var timeEnd = DateTimeHelper.CurrentTicks;
				Assert.AreEqual(messages[i], inMessage);
				Assert.Less(Math.Abs(timeEnd - messageChopper.MessageReadTimeInTicks), milliseconds * TimeSpan.TicksPerMillisecond);
			}
		}
	}
}
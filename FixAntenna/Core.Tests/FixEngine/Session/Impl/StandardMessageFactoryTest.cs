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

using Epam.FixAntenna.Constants.Fixt11;
using NUnit.Framework;
using System;
using System.IO;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Session.Impl;
using Epam.FixAntenna.NetCore.FixEngine.Transport;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Message.Format;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.Impl
{
	[TestFixture]
	internal class StandardMessageFactoryTest
	{
		private StandardMessageFactory _messageFactory;
		private SessionParameters _sessionParameters;
		private FixSessionRuntimeState _runtimeState;
		private FixMessage _message;
		private ByteBuffer _byteBuffer;
		private SerializationContext _serializationContext;

		[SetUp]
		public void Before()
		{
			_byteBuffer = new ByteBuffer();
			_message = new FixMessage();
			_messageFactory = new StandardMessageFactory();
			_sessionParameters = new SessionParameters();
			_sessionParameters.SenderLocationId = "SenderLocationId";
			_sessionParameters.TargetLocationId = "TargetLocationId";
			_sessionParameters.SenderSubId = "SenderSubId";
			_sessionParameters.TargetSubId = "TargetSubId";
			_sessionParameters.FixVersion = FixVersion.Fixt11;
			_sessionParameters.AppVersion = FixVersion.Fix44;
			_messageFactory.SetSessionParameters(_sessionParameters);
			_messageFactory.SetRuntimeState(new FixSessionRuntimeState());
			_serializationContext = new SerializationContext(new TestSendingTimeFormatter("20170629-23:00:03.202"));
			_runtimeState = new FixSessionRuntimeState();
			_runtimeState.OutSeqNum = 1;
			_runtimeState.InSeqNum = 1;
			_messageFactory.SetRuntimeState(_runtimeState);
	//        messageFactory.setMessageEncryption(EncryptionFactory.createEncryption(Configuration.GetGlobalConfiguration(), EncryptionType.None, "", "", 1000));
		}

		[Test]
		public virtual void GetMessageAsIs()
		{
			_messageFactory.Serialize(null, _message, _byteBuffer, _serializationContext);
			Assert.IsTrue(_message.AsByteArray().Length == _byteBuffer.Offset);

			_message = StandardMessageFactoryHelper.GetFullMessage(1, 2);

			_byteBuffer = new ByteBuffer();
			_messageFactory.Serialize(null, _message, _byteBuffer, _serializationContext);
			Assert.That(_message.AsByteArray(), Is.EqualTo(StandardMessageFactoryHelper.GetBytesFromBuffer(_byteBuffer)));
		}

		[Test]
		public virtual void GetMessageAsIsInNewApi()
		{
			_messageFactory.Serialize(_message, null, _byteBuffer, _serializationContext);
			Assert.IsTrue(_message.AsByteArray().Length == _byteBuffer.Offset);

			_message = StandardMessageFactoryHelper.GetFullMessage(1, 2);

			_byteBuffer = new ByteBuffer();
			_messageFactory.Serialize(null, _message, _byteBuffer, _serializationContext);
			Assert.That(_message.AsByteArray(), Is.EqualTo(StandardMessageFactoryHelper.GetBytesFromBuffer(_byteBuffer)));
		}

		[Test]
		public virtual void GetUpdateMessage()
		{
			_message = StandardMessageFactoryHelper.GetFullMessage(8, 9, Tags.MsgType, Tags.SenderCompID, Tags.TargetCompID, Tags.SenderLocationID, Tags.TargetLocationID, Tags.SenderSubID, Tags.TargetSubID, Tags.MsgSeqNum, Tags.SendingTime, Tags.CheckSum);
			var byteBuffer = new ByteBuffer();
			_messageFactory.Serialize("", _message, byteBuffer, _serializationContext);
			var updatedByteMessage = StandardMessageFactoryHelper.GetBytesFromBuffer(byteBuffer);
			var updatedMessage = RawFixUtil.GetFixMessage(updatedByteMessage);
			AssertTagValue(updatedMessage, Tags.SenderCompID, "Sender");
			AssertTagValue(updatedMessage, Tags.TargetCompID, "Target");
			AssertTagValue(updatedMessage, Tags.SenderLocationID, "SenderLocationId");
			AssertTagValue(updatedMessage, Tags.TargetLocationID, "TargetLocationId");
			AssertTagValue(updatedMessage, Tags.SenderSubID, "SenderSubId");
			AssertTagValue(updatedMessage, Tags.TargetSubID, "TargetSubId");
			AssertTagValue(updatedMessage, Tags.MsgSeqNum, 1);
			Assert.IsNotNull(updatedMessage.GetTagValueAsString(Tags.SendingTime));
			AssertTagValue(updatedMessage, Tags.CheckSum, _message.CalculateChecksum());
			Assert.IsNotNull(updatedMessage.GetTagValueAsString(Tags.CheckSum));

			var msg = StringHelper.NewString(updatedByteMessage);
			var lengthTag = "\u00019=" + updatedMessage.GetTagValueAsString(9);
			var lengthTagIndex = msg.IndexOf(lengthTag, StringComparison.Ordinal);
			var msgLength = msg.Length - 7 - lengthTagIndex - 1 - lengthTag.Length;
			AssertTagValue(updatedMessage, 9, msgLength);
		}

		[Test]
		public virtual void GetUpdateMessageInNewApi()
		{
			_message = StandardMessageFactoryHelper.GetFullMessage(8, Tags.MsgType, Tags.SenderCompID, Tags.TargetCompID, Tags.SenderLocationID, Tags.TargetLocationID, Tags.SenderSubID, Tags.TargetSubID, Tags.MsgSeqNum, Tags.SendingTime, 1, 2, Tags.CheckSum);
			_messageFactory.Serialize(_message, ChangesType.UpdateSmhAndSmt, _byteBuffer, _serializationContext);
			var updatedMessage = RawFixUtil.GetFixMessage(StandardMessageFactoryHelper.GetBytesFromBuffer(_byteBuffer));
			AssertTagValue(updatedMessage, Tags.SenderCompID, "Sender");
			AssertTagValue(updatedMessage, Tags.TargetCompID, "Target");
			AssertTagValue(updatedMessage, Tags.SenderLocationID, "SenderLocationId");
			AssertTagValue(updatedMessage, Tags.TargetLocationID, "TargetLocationId");
			AssertTagValue(updatedMessage, Tags.SenderSubID, "SenderSubId");
			AssertTagValue(updatedMessage, Tags.TargetSubID, "TargetSubId");
			AssertTagValue(updatedMessage, Tags.MsgSeqNum, 1);
			Assert.IsNotNull(updatedMessage.GetTagValueAsString(Tags.SendingTime));
			AssertTagValue(updatedMessage, 1, 1);
			AssertTagValue(updatedMessage, 2, 2);
			Assert.IsNotNull(updatedMessage.GetTagValueAsString(Tags.CheckSum));
		}

		[Test]
		public virtual void WrapUserContent()
		{
			_message = StandardMessageFactoryHelper.GetFullMessage(1, 2);

			_messageFactory.Serialize("0", _message, _byteBuffer, _serializationContext);
			var updatedMessage = RawFixUtil.GetFixMessage(StandardMessageFactoryHelper.GetBytesFromBuffer(_byteBuffer));
			AssertTagValue(updatedMessage, Tags.SenderCompID, "Sender");
			AssertTagValue(updatedMessage, Tags.TargetCompID, "Target");
			AssertTagValue(updatedMessage, Tags.SenderLocationID, "SenderLocationId");
			AssertTagValue(updatedMessage, Tags.TargetLocationID, "TargetLocationId");
			AssertTagValue(updatedMessage, Tags.SenderSubID, "SenderSubId");
			AssertTagValue(updatedMessage, Tags.TargetSubID, "TargetSubId");
			AssertTagValue(updatedMessage, Tags.MsgSeqNum, 1);
			Assert.IsNotNull(updatedMessage.GetTagValueAsString(Tags.SendingTime));
			AssertTagValue(updatedMessage, 1, 1);
			AssertTagValue(updatedMessage, 2, 2);
			Assert.IsNotNull(updatedMessage.GetTagValueAsString(Tags.CheckSum));
		}

		[Test]
		public virtual void WrapUserContentInNewApi()
		{
			_message = StandardMessageFactoryHelper.GetFullMessage(Tags.MsgType, 1, 2);

			_messageFactory.Serialize(_message, ChangesType.AddSmhAndSmt, _byteBuffer, _serializationContext);
			var updatedMessage = RawFixUtil.GetFixMessage(StandardMessageFactoryHelper.GetBytesFromBuffer(_byteBuffer));
			AssertTagValue(updatedMessage, Tags.SenderCompID, "Sender");
			AssertTagValue(updatedMessage, Tags.TargetCompID, "Target");
			AssertTagValue(updatedMessage, Tags.SenderLocationID, "SenderLocationId");
			AssertTagValue(updatedMessage, Tags.TargetLocationID, "TargetLocationId");
			AssertTagValue(updatedMessage, Tags.SenderSubID, "SenderSubId");
			AssertTagValue(updatedMessage, Tags.TargetSubID, "TargetSubId");
			AssertTagValue(updatedMessage, Tags.MsgSeqNum, 1);
			Assert.IsNotNull(updatedMessage.GetTagValueAsString(Tags.SendingTime));
			AssertTagValue(updatedMessage, 1, 1);
			AssertTagValue(updatedMessage, 2, 2);
			AssertTagValue(updatedMessage, Tags.CheckSum, updatedMessage.CalculateChecksum());
			Assert.IsNotNull(updatedMessage.GetTagValueAsString(Tags.CheckSum));
		}

		[Test]
		public virtual void WrapUserContentWithLastProcessedFlagInNewApi()
		{
			_sessionParameters.Configuration.SetProperty(Config.IncludeLastProcessed, "true");
			_messageFactory.SetSessionParameters(_sessionParameters);

			_runtimeState.InSeqNum = 100;
			_messageFactory.SetRuntimeState(_runtimeState);

			_message = StandardMessageFactoryHelper.GetFullMessage(Tags.MsgType, 1);

			var byteBuffer = new ByteBuffer(1024);
			_messageFactory.Serialize(_message, ChangesType.AddSmhAndSmt, byteBuffer, _serializationContext);
			var updatedByteMessage = StandardMessageFactoryHelper.GetBytesFromBuffer(byteBuffer);
			var updatedMessage = RawFixUtil.GetFixMessage(updatedByteMessage);
			AssertTagValue(updatedMessage, 1, 1);
			AssertTagValue(updatedMessage, 369, 99);
			Assert.IsNotNull(updatedMessage.GetTagValueAsString(Tags.CheckSum));
		}

		[Test]
		public virtual void TestSafeSetValue()
		{
			_message.AddTag(8, "FIX.4.2");

			StandardMessageFactory.SafeSetValue(_message, 8, "FIX.4.3");
			AssertTagValue(_message, 8, "FIX.4.3");

			StandardMessageFactory.SafeSetValue(_message, Tags.CheckSum, "10");
			Assert.IsNull(_message.GetTag(Tags.CheckSum));
		}

		[Test]
		public virtual void TestSafeSetValueAfter()
		{
			_message.AddTag(8, "FIX.4.2");

			StandardMessageFactory.SafeSetValueAfter(_message, 8, 10, "10".AsByteArray());
			AssertTagValue(_message, Tags.CheckSum, "10");

			StandardMessageFactory.SafeSetValueAfter(_message, 100, Tags.CheckSum, "10".AsByteArray());
			AssertTagValue(_message, Tags.CheckSum, "10");

			var indexOf = _message.GetTagIndex(Tags.CheckSum);
			Assert.AreEqual(1, indexOf);
		}

		[Test]
		public virtual void CheckIfMessageCompleted()
		{
			var message = _messageFactory.CompleteMessage("2", new FixMessage());
			StandardMessageFactoryHelper.CheckFields(message, new int[]{ Tags.BeginString, Tags.BodyLength, Tags.MsgType, Tags.MsgSeqNum, Tags.SenderCompID, Tags.TargetCompID, Tags.SenderSubID, Tags.TargetSubID, Tags.SenderLocationID, Tags.TargetLocationID, Tags.SendingTime, Tags.CheckSum });
		}

		[Test]
		public void CheckSendingTimeTagOrder()
		{
			var content = new FixMessage();
			content.AddTag(Tags.GapFillFlag, "Y");
			content.AddTag(Tags.NewSeqNo, 10);
			var message = _messageFactory.CompleteMessage("4", content);
			StandardMessageFactoryHelper.CheckFields(message, new []
			{
				Tags.BeginString,
				Tags.BodyLength,
				Tags.MsgType,
				Tags.MsgSeqNum,
				Tags.SenderCompID,
				Tags.TargetCompID,
				Tags.SenderSubID,
				Tags.TargetSubID,
				Tags.SenderLocationID,
				Tags.TargetLocationID,
				Tags.SendingTime,
				Tags.GapFillFlag,
				Tags.NewSeqNo,
				Tags.CheckSum
			});
			var tag123i = message.GetTagIndex(Tags.GapFillFlag);
			var tag52i = message.GetTagIndex(Tags.SendingTime);
			Assert.That(tag52i, Is.LessThan(tag123i));
		}

		[Test]
		public virtual void WrapUserContentWithLastProcessedFlagInNewApiInBatch()
		{
			_byteBuffer = new ByteBuffer(2024);
			_sessionParameters.Configuration.SetProperty(Config.IncludeLastProcessed, "true");
			_messageFactory.SetSessionParameters(_sessionParameters);
			_runtimeState.InSeqNum = 100;

			_message = StandardMessageFactoryHelper.GetFullMessage(Tags.MsgType, 1);
			_messageFactory.Serialize(_message, ChangesType.AddSmhAndSmt, _byteBuffer, _serializationContext);
			_message = StandardMessageFactoryHelper.GetFullMessage(Tags.MsgType, 1);
			_messageFactory.Serialize(_message, ChangesType.AddSmhAndSmt, _byteBuffer, _serializationContext);

			var updatedByteMessage = StandardMessageFactoryHelper.GetBytesFromBuffer(_byteBuffer);
			var fixMessageChopper = new FixMessageChopper(new MemoryStream(updatedByteMessage), 500, 1000);
			var buf = new MsgBuf();
			fixMessageChopper.ReadMessage(buf);
			CheckMessage(buf);

			fixMessageChopper.ReadMessage(buf);
			CheckMessage(buf);
		}

		private void CheckMessage(MsgBuf buf)
		{
			var updatedMessage = RawFixUtil.GetFixMessage(buf);
			AssertTagValue(updatedMessage, 1, 1);
			AssertTagValue(updatedMessage, 369, 99);
			Assert.IsNotNull(updatedMessage.GetTagValueAsString(9));
			Assert.IsNotNull(updatedMessage.GetTagValueAsString(Tags.MsgSeqNum));
			Assert.IsNotNull(updatedMessage.GetTagValueAsString(Tags.CheckSum));
		}

		[Test]
		public virtual void WrapUserContentWithLastProcessedFlagInNewApiInBatch1()
		{
			_byteBuffer = new ByteBuffer(2024);
			_sessionParameters.Configuration.SetProperty(Config.IncludeLastProcessed, "true");
			_runtimeState.InSeqNum = 100;
			_messageFactory.SetSessionParameters(_sessionParameters);

			_message = StandardMessageFactoryHelper.GetFullMessage(9, Tags.MsgType, Tags.MsgSeqNum, Tags.SenderCompID, Tags.TargetCompID, Tags.SendingTime, 1, Tags.CheckSum);
			_message.Set(Tags.MsgType, "0");
			_message.AddTagAtIndex(0, 8, "FIX.4.4".AsByteArray());
			_messageFactory.Serialize(_message, ChangesType.UpdateSmhAndSmt, _byteBuffer, _serializationContext);

			_message = StandardMessageFactoryHelper.GetFullMessage(9, Tags.MsgType, Tags.MsgSeqNum, Tags.SenderCompID, Tags.TargetCompID, Tags.SendingTime, 1, Tags.CheckSum);
			_message.Set(Tags.MsgType, "0");
			_message.AddTagAtIndex(0, 8, "FIX.4.4".AsByteArray());
			_messageFactory.Serialize(_message, ChangesType.UpdateSmhAndSmt, _byteBuffer, _serializationContext);

			var updatedByteMessage = StandardMessageFactoryHelper.GetBytesFromBuffer(_byteBuffer);
			var fixMessageChopper = new FixMessageChopper(new MemoryStream(updatedByteMessage), 500, 1000);
			var buf = new MsgBuf();
			fixMessageChopper.ReadMessage(buf);
			CheckMessage(buf);

			fixMessageChopper.ReadMessage(buf);
			CheckMessage(buf);
		}

		[Test]
		public virtual void TestBigMessage()
		{
			_sessionParameters.Configuration.SetProperty(Config.IncludeLastProcessed, "true");
			_sessionParameters.IncomingSequenceNumber = 100;
			_messageFactory.SetSessionParameters(_sessionParameters);

			var tags = new int[999];
			for (var i = 0; i < 999; ++i)
			{
				tags[i] = i + 1;
			}

			_message = StandardMessageFactoryHelper.GetFullMessage(tags);

			var initBufferLength = 1;
			var byteBuffer = new ByteBuffer(initBufferLength);
			_messageFactory.Serialize(_message, ChangesType.AddSmhAndSmt, byteBuffer, _serializationContext);

			byteBuffer = new ByteBuffer(initBufferLength);
			_messageFactory.Serialize(_message, ChangesType.UpdateSmhAndSmt, byteBuffer, _serializationContext);

			byteBuffer = new ByteBuffer(initBufferLength);
			_messageFactory.Serialize(_message, ChangesType.UpdateSmhAndSmtDonotUpdateSndr, byteBuffer, _serializationContext);

			byteBuffer = new ByteBuffer(initBufferLength);
			_messageFactory.Serialize("", _message, byteBuffer, _serializationContext);

			byteBuffer = new ByteBuffer(initBufferLength);
			_messageFactory.Serialize("B", _message, byteBuffer, _serializationContext);
		}

		[Test]
		public virtual void TestSerializingOfPreparedMessage()
		{
			var pmu = new PreparedMessageUtil(_sessionParameters);
			var ms = new MessageStructure();
			ms.Reserve(Tags.ApplVerID, 1);
			ms.Reserve(Tags.MessageEncoding, 5);
			ms.Reserve(Tags.Signature, 5);
			ms.Reserve(Tags.SignatureLength, 1);

			ms.Reserve(44, 5);
			ms.Reserve(38, MessageStructure.VariableLength);
			ms.Reserve(39, MessageStructure.VariableLength);
			ms.Reserve(11, 3);
			ms.Reserve(12, 3);
			ms.Reserve(58, 2);
			var preparedMessage = pmu.PrepareMessage("D", ms);
			preparedMessage.Set(Tags.ApplVerID, 6);
			preparedMessage.Set(Tags.MessageEncoding, "utf-8");
			preparedMessage.Set(Tags.Signature, "12345");
			preparedMessage.Set(Tags.SignatureLength, 5);
			preparedMessage.Set(44, 1234);
			preparedMessage.Set(39, "AA");
			preparedMessage.Set(11, "BBB");
			preparedMessage.Set(12, "CC");

			//8=FIX.4.29=35=D34=49=Sender56=Target50=SenderSubId57=TargetSubId142=SenderLocationId
			//143=TargetLocationId52=                     44=123438=39=AA11=BBB12=CC58=  10=   
			_messageFactory.Serialize("", preparedMessage, _byteBuffer, _serializationContext);

			var bytesFromBuffer = StandardMessageFactoryHelper.GetBytesFromBuffer(_byteBuffer);
			var msg = StringHelper.NewString(bytesFromBuffer);
			var serializedMessage = RawFixUtil.GetFixMessage(bytesFromBuffer);
			AssertTagValue(serializedMessage, Tags.SenderCompID, "Sender");
			AssertTagValue(serializedMessage, Tags.TargetCompID, "Target");
			AssertTagValue(serializedMessage, Tags.SenderLocationID, "SenderLocationId");
			AssertTagValue(serializedMessage, Tags.TargetLocationID, "TargetLocationId");
			AssertTagValue(serializedMessage, Tags.SenderSubID, "SenderSubId");
			AssertTagValue(serializedMessage, Tags.TargetSubID, "TargetSubId");
			AssertTagValue(serializedMessage, Tags.MsgSeqNum, "00001");
			AssertTagValue(serializedMessage, Tags.MsgType, "D");
			AssertTagValue(serializedMessage, Tags.CheckSum, serializedMessage.CalculateChecksum());

			AssertTagValue(serializedMessage, 44, "01234");
			AssertTagValue(serializedMessage, 38, "");
			AssertTagValue(serializedMessage, 39, "AA");
			AssertTagValue(serializedMessage, 11, "BBB");
			AssertTagValue(serializedMessage, 12, "CC ");
			AssertTagValue(serializedMessage, 58, "  ");

			_byteBuffer.ResetBuffer();
			var content = FixMessageFactory.NewInstanceFromPool();
			content.AddAll(serializedMessage);
			_messageFactory.Serialize(content, ChangesType.DeleteAndAddSmhAndSmt, _byteBuffer, _serializationContext);

			bytesFromBuffer = StandardMessageFactoryHelper.GetBytesFromBuffer(_byteBuffer);
			Assert.AreEqual("8=FIXT.1.1\u00019=197\u000135=D\u000134=1\u000149=Sender\u000156=Target\u000150=SenderSubId" + "\u000157=TargetSubId\u0001142=SenderLocationId\u0001143=TargetLocationId\u000152=20170629-23:00:03.202" + "\u00011128=6\u0001347=utf-8\u000189=12345\u000193=5" + "\u000144=01234\u000138=\u000139=AA\u000111=BBB\u000112=CC \u000158=  \u000110=154\u0001", StringHelper.NewString(bytesFromBuffer));
		}

		[Test]
		public virtual void TestCheckSumSerialization()
		{
			_message = StandardMessageFactoryHelper.GetFullMessage(8, 9, Tags.MsgType, Tags.SenderCompID, Tags.TargetCompID, Tags.SenderLocationID, Tags.TargetLocationID, Tags.SenderSubID, Tags.TargetSubID, Tags.MsgSeqNum, Tags.SendingTime);
			//checksum with value's length == 1
			_message.AddTag(Tags.CheckSum, (long)0);
			//serialize as is
			_messageFactory.Serialize(null, "", _message, _byteBuffer, _serializationContext);
			var msg = RawFixUtil.GetFixMessage(StandardMessageFactoryHelper.GetBytesFromBuffer(_byteBuffer));
			Assert.AreEqual(3, msg.GetTagValueLength(Tags.CheckSum));
		}


		[Test]
		public virtual void TestUpdateExceptCompIdsSerialization()
		{
			_message = StandardMessageFactoryHelper.GetFullMessage(8, 9, Tags.MsgType, Tags.MsgSeqNum, Tags.SendingTime, 1, 2, Tags.CheckSum);
			_messageFactory.Serialize(_message, ChangesType.UpdateSmhAndSmtExceptCompids, _byteBuffer, _serializationContext);
			var updatedMessage = RawFixUtil.GetFixMessage(StandardMessageFactoryHelper.GetBytesFromBuffer(_byteBuffer));
			AssertTagValue(updatedMessage, Tags.SenderCompID, "Sender");
			AssertTagValue(updatedMessage, Tags.TargetCompID, "Target");
			AssertTagValue(updatedMessage, Tags.SenderLocationID, "SenderLocationId");
			AssertTagValue(updatedMessage, Tags.TargetLocationID, "TargetLocationId");
			AssertTagValue(updatedMessage, Tags.SenderSubID, "SenderSubId");
			AssertTagValue(updatedMessage, Tags.TargetSubID, "TargetSubId");
			AssertTagValue(updatedMessage, Tags.MsgSeqNum, 1);
			Assert.IsNotNull(updatedMessage.GetTagValueAsString(Tags.SendingTime));
			AssertTagValue(updatedMessage, 1, 1);
			AssertTagValue(updatedMessage, 2, 2);
			Assert.IsNotNull(updatedMessage.GetTagValueAsString(Tags.CheckSum));
			AssertTagValue(updatedMessage, 9, 136);
		}


		private void AssertTagValue(FixMessage fixMessage, int tagId, string expectedValue)
		{
			Assert.AreEqual(expectedValue, fixMessage.GetTagValueAsString(tagId),
				"Wrong value for tag " + tagId + ": " + fixMessage.ToString());
		}

		private void AssertTagValue(FixMessage fixMessage, int tagId, long expectedValue)
		{
			Assert.AreEqual(expectedValue, fixMessage.GetTagAsInt(tagId), "Wrong value for tag " + tagId);
		}

		private class TestSendingTimeFormatter : ISendingTime
		{
			internal readonly byte[] Date;

			public TestSendingTimeFormatter(string date)
			{
				Date = date.AsByteArray();
			}

			public byte[] CurrentDateValue
			{
				get { return Date; }
			}

			public int FormatLength => Date.Length;
		}

		[Test]
		public virtual void TestSendingTime()
		{
			Assert.AreEqual(typeof(SendingTimeSecond), GetSessionSendingTime(TimestampPrecision.Second.ToString()).GetType());
			Assert.AreEqual(typeof(SendingTimeMilli), GetSessionSendingTime(TimestampPrecision.Milli.ToString()).GetType());
			Assert.AreEqual(typeof(SendingTimeMicro), GetSessionSendingTime(TimestampPrecision.Micro.ToString()).GetType());
			Assert.AreEqual(typeof(SendingTimeNano), GetSessionSendingTime(TimestampPrecision.Nano.ToString()).GetType());
			//default
			Assert.AreEqual(typeof(SendingTimeMilli), GetSessionSendingTime("").GetType());
		}

		private ISendingTime GetSessionSendingTime(string timestampsPrecisionInTagsOption)
		{
			var messageFactory = new StandardMessageFactory();
			var @params = new SessionParameters();
			@params.Configuration.SetProperty(Config.TimestampsPrecisionInTags, timestampsPrecisionInTagsOption);
			messageFactory.SetSessionParameters(@params);
			return messageFactory.SendingTime;
		}

		[Test]
		public virtual void TestUpdatedSessionParameters()
		{
			var byteBuffer = new ByteBuffer();
			_messageFactory.Serialize("D", new FixMessage(), byteBuffer, _serializationContext);
			var updatedByteMessage = StandardMessageFactoryHelper.GetBytesFromBuffer(byteBuffer);
			var updatedMessage = RawFixUtil.GetFixMessage(updatedByteMessage);
			AssertTagValue(updatedMessage, Tags.SenderCompID, "Sender");
			AssertTagValue(updatedMessage, Tags.TargetCompID, "Target");
			AssertTagValue(updatedMessage, Tags.SenderLocationID, "SenderLocationId");
			AssertTagValue(updatedMessage, Tags.TargetLocationID, "TargetLocationId");
			AssertTagValue(updatedMessage, Tags.SenderSubID, "SenderSubId");
			AssertTagValue(updatedMessage, Tags.TargetSubID, "TargetSubId");

			var newSessionParams = new SessionParameters();
			newSessionParams.SenderCompId = "NewSender";
			newSessionParams.TargetCompId = "NewTarget";
			_messageFactory.SetSessionParameters(newSessionParams);

			byteBuffer.ResetBuffer();
			_messageFactory.Serialize("D", new FixMessage(), byteBuffer, _serializationContext);
			updatedByteMessage = StandardMessageFactoryHelper.GetBytesFromBuffer(byteBuffer);
			updatedMessage = RawFixUtil.GetFixMessage(updatedByteMessage);
			AssertTagValue(updatedMessage, Tags.SenderCompID, "NewSender");
			AssertTagValue(updatedMessage, Tags.TargetCompID, "NewTarget");
			Assert.IsFalse(updatedMessage.IsTagExists(Tags.SenderSubID));
			Assert.IsFalse(updatedMessage.IsTagExists(Tags.SenderLocationID));
			Assert.IsFalse(updatedMessage.IsTagExists(Tags.TargetSubID));
			Assert.IsFalse(updatedMessage.IsTagExists(Tags.TargetLocationID));
		}
	}
}
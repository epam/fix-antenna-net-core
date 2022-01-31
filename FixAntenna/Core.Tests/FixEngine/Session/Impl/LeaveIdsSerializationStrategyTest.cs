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
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Session.Impl;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Message.Format;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.Impl
{
	[TestFixture]
	internal class LeaveIdsSerializationStrategyTest
	{
		protected internal static readonly ILog Log = LogFactory.GetLog(typeof(LeaveIdsSerializationStrategyTest));

		private LeaveIdsSerializationStrategy _strategy;
		private SessionParameters _sessionParameters;
		private FixSessionRuntimeState _runtimeState;
		private FixMessage _message;
		private ByteBuffer _byteBuffer;
		private SerializationContext _serializationContext;

		[SetUp]
		public void Before()
		{
			_strategy = new LeaveIdsSerializationStrategy();

			_byteBuffer = new ByteBuffer();
			_message = new FixMessage();

			_sessionParameters = new SessionParameters();
			_sessionParameters.SenderCompId = "SenderCompId";
			_sessionParameters.TargetCompId = "TargetCompId";
			_sessionParameters.SenderSubId = "SenderSubId";
			_sessionParameters.TargetSubId = "TargetSubId";
			_sessionParameters.SenderLocationId = "SenderLocationId";
			_sessionParameters.TargetLocationId = "TargetLocationId";
			_sessionParameters.UserName = "username";
			_sessionParameters.Password = "pass";

			_serializationContext = new SerializationContextStub();
			_runtimeState = new FixSessionRuntimeState();
			_runtimeState.OutSeqNum = 1;
			_runtimeState.InSeqNum = 1;
			_runtimeState.OutgoingLogon = _sessionParameters.OutgoingLoginMessage;
		}


		[Test]
		public virtual void TestSerializeWithExistIds()
		{
			_strategy.Init(_sessionParameters);

			_message = StandardMessageFactoryHelper.GetFullMessage(Tags.BeginString, Tags.SenderCompID, Tags.TargetCompID, Tags.SenderLocationID, Tags.TargetLocationID, Tags.SenderSubID, Tags.TargetSubID, Tags.MsgSeqNum, Tags.SendingTime, 1, 2, Tags.CheckSum);
			_message.AddTagAtIndex(1, Tags.MsgType, "D");
			var beforeFixString = _message.ToPrintableString();
			Log.Debug("ORIGINAL : " + beforeFixString);

			_strategy.Serialize(_message, _byteBuffer, _serializationContext, _runtimeState);
			var bytesFromBuffer = StandardMessageFactoryHelper.GetBytesFromBuffer(_byteBuffer);

			var serializedMsg = PrepareMsgString(bytesFromBuffer);
			Assert.AreEqual("8=FIX.4.2\u00019=83\u000135=D\u000134=1\u0001" + "49=49\u000156=56\u000150=50\u000157=57\u0001142=142\u0001143=143\u0001" + "52=20160510-10:39:37.987\u00011=1\u00012=2\u000110=009\u0001", serializedMsg);
			//try to parse
			var serialized = RawFixUtil.GetFixMessage(bytesFromBuffer);
			Log.Debug("SERIALIZED: " + serialized.ToPrintableString());

			Assert.AreEqual(beforeFixString, _message.ToPrintableString(), "Message was changed during serialization");
			CheckMsgLenght(serializedMsg, serialized);
		}

		[Test]
		public virtual void TestSerializeWithExistCompIds()
		{
			_strategy.Init(_sessionParameters);

			_message = StandardMessageFactoryHelper.GetFullMessage
				(Tags.BeginString, Tags.SenderCompID, Tags.TargetCompID, Tags.MsgSeqNum, Tags.SendingTime, 1, 2, Tags.CheckSum);
			_message.AddTagAtIndex(1, Tags.MsgType, "D");
			var beforeFixString = _message.ToPrintableString();
			Log.Debug("ORIGINAL : " + beforeFixString);

			_strategy.Serialize(_message, _byteBuffer, _serializationContext, _runtimeState);
			var bytesFromBuffer = StandardMessageFactoryHelper.GetBytesFromBuffer(_byteBuffer);

			var serializedMsg = PrepareMsgString(bytesFromBuffer);
			Assert.AreEqual("8=FIX.4.2\u00019=55\u000135=D\u000134=1\u0001" + "49=49\u000156=56\u0001" + "52=20160510-10:39:37.987\u00011=1\u00012=2\u000110=016\u0001", serializedMsg);
			//try to parse
			var serialized = RawFixUtil.GetFixMessage(bytesFromBuffer);
			Log.Debug("SERIALIZED: " + serialized.ToPrintableString());

			Assert.AreEqual(beforeFixString, _message.ToPrintableString(), "Message was changed during serialization");

			CheckMsgLenght(serializedMsg, serialized);
		}

		[Test]
		public virtual void TestSerializeWithoutCompIds()
		{
			_strategy.Init(_sessionParameters);

			_message = StandardMessageFactoryHelper.GetFullMessage(Tags.BeginString, Tags.SenderLocationID, Tags.TargetLocationID, Tags.SenderSubID, Tags.TargetSubID, Tags.MsgSeqNum, Tags.SendingTime, 1, 2, Tags.CheckSum);
			_message.AddTagAtIndex(1, Tags.MsgType, "D");
			var beforeFixString = _message.ToPrintableString();
			Log.Debug("ORIGINAL : " + beforeFixString);

			_strategy.Serialize(_message, _byteBuffer, _serializationContext, _runtimeState);
			var bytesFromBuffer = StandardMessageFactoryHelper.GetBytesFromBuffer(_byteBuffer);

			var serializedMsg = PrepareMsgString(bytesFromBuffer);
			Assert.AreEqual("8=FIX.4.2\u00019=147\u000135=D\u000134=1\u0001" + "49=SenderCompId\u000156=TargetCompId\u000150=SenderSubId\u000157=TargetSubId\u0001" + "142=SenderLocationId\u0001143=TargetLocationId\u0001" + "52=20160510-10:39:37.987\u00011=1\u00012=2\u000110=172\u0001", serializedMsg);
			//try to parse
			var serialized = RawFixUtil.GetFixMessage(bytesFromBuffer);
			Log.Debug("SERIALIZED: " + serialized.ToPrintableString());
			Assert.AreEqual(beforeFixString, _message.ToPrintableString(), "Message was changed during serialization");

			CheckMsgLenght(serializedMsg, serialized);
		}

		[Test]
		public virtual void TestSerializeLogon()
		{
			_strategy.Init(_sessionParameters);

			_message = StandardMessageFactoryHelper.GetFullMessage(Tags.BeginString, Tags.BodyLength, Tags.MsgSeqNum, Tags.SenderCompID, Tags.TargetCompID, Tags.SenderSubID, Tags.TargetSubID, Tags.SenderLocationID, Tags.TargetLocationID, Tags.SendingTime, 1, 2, Tags.CheckSum);
			_message.AddTagAtIndex(2, Tags.MsgType, "A");
			_message.Set(8, "FIX.4.2");
			_message.Set(9, "000");
			_message.Set(34, "0");
			_message.Set(52, "00000000-00:00:00.000");
			var beforeFixString = _message.ToPrintableString();
			Log.Debug("ORIGINAL : " + beforeFixString);

			_strategy.Serialize(_message, _byteBuffer, _serializationContext, _runtimeState);
			var bytesFromBuffer = StandardMessageFactoryHelper.GetBytesFromBuffer(_byteBuffer);

			var serializedMsg = PrepareMsgString(bytesFromBuffer);
			Assert.AreEqual("8=FIX.4.2\u00019=117\u000135=A\u000134=1\u0001"
											+ "49=49\u000156=56\u000150=50\u000157=57\u0001142=142\u0001143=143\u0001"
											+ "52=20160510-10:39:37.987\u000198=0\u0001"
											+ "108=30\u0001553=username\u0001554=pass\u00011=1\u00012=2\u000110=027\u0001", serializedMsg);
			//try to parse
			var serialized = RawFixUtil.GetFixMessage(bytesFromBuffer);
			Log.Debug("SERIALIZED: " + serialized.ToPrintableString());

			Assert.AreEqual(beforeFixString, _message.ToPrintableString(), "Message was changed during serialization");

			CheckMsgLenght(serializedMsg, serialized);
		}

		[Test]
		public virtual void TestSerializeWithoutIds()
		{
			_strategy.Init(_sessionParameters);

			_message = StandardMessageFactoryHelper.GetFullMessage(Tags.BeginString, Tags.MsgSeqNum, Tags.SendingTime, 1, 2, Tags.CheckSum);
			_message.AddTagAtIndex(1, Tags.MsgType, "D");
			var beforeFixString = _message.ToPrintableString();
			Log.Debug("ORIGINAL : " + beforeFixString);

			_strategy.Serialize(_message, _byteBuffer, _serializationContext, _runtimeState);
			var bytesFromBuffer = StandardMessageFactoryHelper.GetBytesFromBuffer(_byteBuffer);

			var serializedMsg = PrepareMsgString(bytesFromBuffer);
			Assert.AreEqual("8=FIX.4.2\u00019=147\u000135=D\u000134=1\u0001" + "49=SenderCompId\u000156=TargetCompId\u000150=SenderSubId\u000157=TargetSubId\u0001" + "142=SenderLocationId\u0001143=TargetLocationId\u0001" + "52=20160510-10:39:37.987\u00011=1\u00012=2\u000110=172\u0001", serializedMsg);
			//try to parse
			var serialized = RawFixUtil.GetFixMessage(bytesFromBuffer);
			Log.Debug("SERIALIZED: " + serialized.ToPrintableString());

			Assert.AreEqual(beforeFixString, _message.ToPrintableString(), "Message was changed during serialization");

			CheckMsgLenght(serializedMsg, serialized);
		}

		[Test]
		public virtual void TestSerializeWithSenderIds()
		{
			_strategy.Init(_sessionParameters);

			_message = StandardMessageFactoryHelper.GetFullMessage(Tags.BeginString, Tags.BodyLength, Tags.MsgSeqNum, Tags.SenderCompID, Tags.SenderSubID, Tags.SenderLocationID, Tags.SendingTime, 1, 2, Tags.CheckSum);
			_message.AddTagAtIndex(2, Tags.MsgType, "D");
			_message.Set(8, "FIX.4.2");
			_message.Set(9, "000");
			_message.Set(34, "0");
			_message.Set(52, "00000000-00:00:00.000");
			var beforeFixString = _message.ToPrintableString();
			Log.Debug("ORIGINAL : " + beforeFixString);

			_strategy.Serialize(_message, _byteBuffer, _serializationContext, _runtimeState);
			var bytesFromBuffer = StandardMessageFactoryHelper.GetBytesFromBuffer(_byteBuffer);

			var serializedMsg = PrepareMsgString(bytesFromBuffer);
			Assert.AreEqual("8=FIX.4.2\u00019=115\u000135=D\u000134=1\u000149=49\u000156=TargetCompId\u0001" + "50=50\u000157=TargetSubId\u0001142=142\u0001143=TargetLocationId\u0001" + "52=20160510-10:39:37.987\u00011=1\u00012=2\u000110=244\u0001", serializedMsg);
			//try to parse
			var serialized = RawFixUtil.GetFixMessage(bytesFromBuffer);
			Log.Debug("SERIALIZED: " + serialized.ToPrintableString());

			Assert.AreEqual(beforeFixString, _message.ToPrintableString(), "Message was changed during serialization");

			CheckMsgLenght(serializedMsg, serialized);
		}

		[Test]
		public virtual void TestSerializeWithTargetIds()
		{
			_strategy.Init(_sessionParameters);

			_message = StandardMessageFactoryHelper.GetFullMessage(Tags.BeginString, Tags.BodyLength, Tags.MsgSeqNum, Tags.TargetCompID, Tags.TargetSubID, Tags.TargetLocationID, Tags.SendingTime, 1, 2, Tags.CheckSum);
			_message.AddTagAtIndex(2, Tags.MsgType, "D");
			_message.Set(8, "FIX.4.2");
			_message.Set(9, "000");
			_message.Set(34, "0");
			_message.Set(52, "00000000-00:00:00.000");
			var beforeFixString = _message.ToPrintableString();
			Log.Debug("ORIGINAL : " + beforeFixString);

			_strategy.Serialize(_message, _byteBuffer, _serializationContext, _runtimeState);
			var bytesFromBuffer = StandardMessageFactoryHelper.GetBytesFromBuffer(_byteBuffer);

			var serializedMsg = PrepareMsgString(bytesFromBuffer);
			Assert.AreEqual("8=FIX.4.2\u00019=115\u000135=D\u000134=1\u000149=SenderCompId\u000156=56\u0001" + "50=SenderSubId\u000157=57\u0001142=SenderLocationId\u0001143=143\u0001" + "52=20160510-10:39:37.987\u00011=1\u00012=2\u000110=232\u0001", serializedMsg);
			//try to parse
			var serialized = RawFixUtil.GetFixMessage(bytesFromBuffer);
			Log.Debug("SERIALIZED: " + serialized.ToPrintableString());

			Assert.AreEqual(beforeFixString, _message.ToPrintableString(), "Message was changed during serialization");

			CheckMsgLenght(serializedMsg, serialized);
		}

		[Test]
		public virtual void TestSerializeWithCompIds()
		{
			_strategy.Init(_sessionParameters);

			_message = StandardMessageFactoryHelper.GetFullMessage(Tags.BeginString, Tags.BodyLength, Tags.MsgSeqNum, Tags.SenderCompID, Tags.TargetCompID, Tags.SendingTime, 1, 2, Tags.CheckSum);
			_message.AddTagAtIndex(2, Tags.MsgType, "D");
			_message.Set(8, "FIX.4.2");
			_message.Set(9, "00");
			_message.Set(34, "0");
			_message.Set(52, "00000000-00:00:00.000");
			var beforeFixString = _message.ToPrintableString();
			Log.Debug("ORIGINAL : " + beforeFixString);

			_strategy.Serialize(_message, _byteBuffer, _serializationContext, _runtimeState);
			var bytesFromBuffer = StandardMessageFactoryHelper.GetBytesFromBuffer(_byteBuffer);

			var serializedMsg = PrepareMsgString(bytesFromBuffer);
			Assert.AreEqual("8=FIX.4.2\u00019=55\u000135=D\u000134=1\u000149=49\u000156=56\u0001" + "52=20160510-10:39:37.987\u00011=1\u00012=2\u000110=016\u0001", serializedMsg);
			//try to parse
			var serialized = RawFixUtil.GetFixMessage(bytesFromBuffer);
			Log.Debug("SERIALIZED: " + serialized.ToPrintableString());

			Assert.AreEqual(beforeFixString, _message.ToPrintableString(), "Message was changed during serialization");

			CheckMsgLenght(serializedMsg, serialized);
		}

		[Test]
		public virtual void TestSerializeWithTargetForCompIds()
		{
			_sessionParameters = new SessionParameters();
			_sessionParameters.SenderCompId = "SenderCompId";
			_sessionParameters.TargetCompId = "TargetCompId";
			_strategy.Init(_sessionParameters);

			_message = StandardMessageFactoryHelper.GetFullMessage(Tags.BeginString, Tags.BodyLength, Tags.MsgSeqNum, Tags.TargetCompID, Tags.TargetSubID, Tags.TargetLocationID, Tags.SendingTime, 1, 2, Tags.CheckSum);
			_message.AddTagAtIndex(2, Tags.MsgType, "D");
			_message.Set(8, "FIX.4.2");
			_message.Set(9, "00");
			_message.Set(34, "0");
			_message.Set(52, "00000000-00:00:00.000");
			var beforeFixString = _message.ToPrintableString();
			Log.Debug("ORIGINAL : " + beforeFixString);

			_strategy.Serialize(_message, _byteBuffer, _serializationContext, _runtimeState);
			var bytesFromBuffer = StandardMessageFactoryHelper.GetBytesFromBuffer(_byteBuffer);

			var serializedMsg = PrepareMsgString(bytesFromBuffer);
			Assert.AreEqual("8=FIX.4.2\u00019=79\u000135=D\u000134=1\u000149=SenderCompId\u000156=56\u0001" + "57=57\u0001143=143\u000152=20160510-10:39:37.987\u00011=1\u00012=2\u000110=202\u0001", serializedMsg);
			//try to parse
			var serialized = RawFixUtil.GetFixMessage(bytesFromBuffer);
			Log.Debug("SERIALIZED: " + serialized.ToPrintableString());

			Assert.AreEqual(beforeFixString, _message.ToPrintableString(), "Message was changed during serialization");

			CheckMsgLenght(serializedMsg, serialized);
		}


		[Test]
		public virtual void TestSerializeEmptyMessage()
		{
			_strategy.Init(_sessionParameters);

			_message = new FixMessage();
			_message.AddTag(Tags.MsgType, "D");
			var beforeFixString = _message.ToPrintableString();
			Log.Debug("ORIGINAL : " + beforeFixString);

			_strategy.Serialize(_message, _byteBuffer, _serializationContext, _runtimeState);
			var bytesFromBuffer = StandardMessageFactoryHelper.GetBytesFromBuffer(_byteBuffer);

			var serializedMsg = PrepareMsgString(bytesFromBuffer);
			Assert.AreEqual("8=FIX.4.2\u00019=139\u000135=D\u000134=1\u000149=SenderCompId\u0001" + "56=TargetCompId\u0001" + "50=SenderSubId\u000157=TargetSubId\u0001142=SenderLocationId\u0001143=TargetLocationId\u0001" + "52=20160510-10:39:37.987\u000110=107\u0001", serializedMsg);
			//try to parse
			var serialized = RawFixUtil.GetFixMessage(bytesFromBuffer);
			Log.Debug("SERIALIZED: " + serialized.ToPrintableString());

			Assert.AreEqual(beforeFixString, _message.ToPrintableString(), "Message was changed during serialization");

			CheckMsgLenght(serializedMsg, serialized);
		}

		[Test]
		public virtual void TestSerialize2BytesMsgType()
		{
			_sessionParameters.FixVersion = FixVersion.Fix44;
			_strategy.Init(_sessionParameters);

			_message = new FixMessage();
			_message.AddTag(Tags.MsgType, "AA");
			var beforeFixString = _message.ToPrintableString();
			Log.Debug("ORIGINAL : " + beforeFixString);

			_strategy.Serialize(_message, _byteBuffer, _serializationContext, _runtimeState);
			var bytesFromBuffer = StandardMessageFactoryHelper.GetBytesFromBuffer(_byteBuffer);

			var serializedMsg = PrepareMsgString(bytesFromBuffer);
			Assert.AreEqual("8=FIX.4.4\u00019=140\u000135=AA\u000134=1\u000149=SenderCompId\u0001" + "56=TargetCompId\u0001" + "50=SenderSubId\u000157=TargetSubId\u0001142=SenderLocationId\u0001143=TargetLocationId\u0001" + "52=20160510-10:39:37.987\u000110=163\u0001", serializedMsg);
			//try to parse
			var serialized = RawFixUtil.GetFixMessage(bytesFromBuffer);
			Log.Debug("SERIALIZED: " + serialized.ToPrintableString());

			Assert.AreEqual(beforeFixString, _message.ToPrintableString(), "Message was changed during serialization");

			CheckMsgLenght(serializedMsg, serialized);
		}


		[Test]
		public virtual void TestSerializeFix50Sp2()
		{
			_sessionParameters.FixVersion = FixVersion.Fixt11;
			_sessionParameters.AppVersion = FixVersion.Fix44;
			_sessionParameters.Configuration.SetProperty(Config.IncludeLastProcessed, "YES");

			_strategy.Init(_sessionParameters);

			_message = StandardMessageFactoryHelper.GetFullMessage(Tags.BeginString, Tags.BodyLength, Tags.MsgSeqNum, Tags.SenderCompID, Tags.TargetCompID, Tags.SenderSubID, Tags.TargetSubID, Tags.SenderLocationID, Tags.TargetLocationID, Tags.SendingTime, 1, 2, Tags.CheckSum);
			_message.AddTagAtIndex(2, Tags.MsgType, "A");
			_message.Set(8, "FIXT.1.1");
			_message.Set(9, "000");
			_message.Set(34, "0");
			_message.Set(52, "00000000-00:00:00.000");

			var beforeFixString = _message.ToPrintableString();
			Log.Debug("ORIGINAL : " + beforeFixString);

			_strategy.Serialize(_message, _byteBuffer, _serializationContext, _runtimeState);
			var bytesFromBuffer = StandardMessageFactoryHelper.GetBytesFromBuffer(_byteBuffer);

			var serializedMsg = PrepareMsgString(bytesFromBuffer);
			Assert.AreEqual(
				"8=FIXT.1.1\u00019=130\u000135=A\u000134=1\u0001"
				+ "49=49\u000156=56\u000150=50\u000157=57\u0001142=142\u0001143=143\u0001"
				+ "369=0\u000152=20160510-10:39:37.987\u00011137=6\u000198=0\u0001108=30\u0001"
				+ "553=username\u0001554=pass\u0001"
				+ "1=1\u00012=2\u000110=182\u0001", serializedMsg);
			//try to parse
			var serialized = RawFixUtil.GetFixMessage(bytesFromBuffer);
			Log.Debug("SERIALIZED: " + serialized.ToPrintableString());

			Assert.AreEqual(beforeFixString, _message.ToPrintableString(), "Message was changed during serialization");

			CheckMsgLenght(serializedMsg, serialized);
		}

		private string PrepareMsgString(byte[] bytesFromBuffer)
		{
			var serializedMsg = StringHelper.NewString(bytesFromBuffer);
	//        serializedMsg = serializedMsg.replaceFirst("\u000152=\\d{8}-\\d{2}:\\d{2}:\\d{2}.\\d{3}\u0001", "\u000152=00000000-00:00:00.000\u0001");
	//        serializedMsg = serializedMsg.replaceFirst("\u000110=.*?\u0001", "\u000110=000\u0001");
			return serializedMsg;
		}

		private void CheckMsgLenght(string serializedMsg, FixMessage serialized)
		{
			var length = serialized.GetTagValueAsLong(9);
			var lengthTag = "\u00019=" + length + "\u0001";
			var start = serializedMsg.IndexOf(lengthTag, StringComparison.Ordinal) + lengthTag.Length;
			var end = serializedMsg.IndexOf("\u000110=", StringComparison.Ordinal) + 1;
			var content = serializedMsg.Substring(start, end - start);
			//System.out.println(content.Replace("\u0001", "|"));
			Assert.AreEqual(length, content.Length);
		}

		private class SerializationContextStub : SerializationContext
		{
			internal IFixDateFormatter Formatter = FixDateFormatterFactory.GetFixDateFormatter(FixDateFormatterFactory.FixDateType.UtcTimestampWithMillis);
			internal DateTimeOffset Calendar;
			internal byte[] SendingTimeBufMs = new byte[21];

			public SerializationContextStub()
			{
				Init();
			}

			public void Init()
			{
				Calendar = DateTimeOffset.FromUnixTimeMilliseconds(1462876777987L);
			}

			public override byte[] CurrentDateValue
			{
				get
				{
					Formatter.Format(Calendar, SendingTimeBufMs, 0);
					return SendingTimeBufMs;
				}
			}

			public override int FormatLength => SendingTimeBufMs.Length;
		}
	}
}
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

using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.PerType;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.TestUtils;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Core.Tests.FixEngine.Session.Impl
{
	[TestFixture, Category("Feature")]
	internal class SeqNumLengthTest
	{
		private AbstractFixSessionHelper _sessionHelper;

		[SetUp]
		public void SetUp()
		{
			ConfigurationHelper.StoreGlobalConfig();
			ClassicAssert.IsTrue(ClearLogs(), "Can't clean logs before tests");

			var config = Config.GlobalConfiguration;
			config.SetProperty(Config.SeqNumLength, "9");
			config.SetProperty(Config.IncludeLastProcessed, "true");
			config.SetProperty(Config.HandleSeqnumAtLogon, "true");

			_sessionHelper = new AbstractFixSessionHelper();
			_sessionHelper.PrepareForConnect();
		}

		[TearDown]
		public void TearDown()
		{
			_sessionHelper?.Dispose();
			ConfigurationHelper.RestoreGlobalConfig();
			ClassicAssert.IsTrue(ClearLogs(), "Can't clean logs after tests");
		}

		private static bool ClearLogs()
		{
			return new LogsCleaner().Clean("./logs") && new LogsCleaner().Clean("./logs/backup");
		}

		[Test, Property("JIRA", "BBP-24520")]
		public void CheckFields_34_369_789_Test()
		{
			_sessionHelper.Parameters.OutgoingLoginMessage.Clear();
			_sessionHelper.SessionState = SessionState.Connected;
			_sessionHelper.ResetQueue();

			_sessionHelper.ResetSequenceNumbers(false);
			var logon = _sessionHelper.GetMessageFromQueue();
			ClassicAssert.IsNotNull(logon);
			var buffer = new ByteBuffer();
			_sessionHelper.MessageFactory.Serialize(null, "", logon, buffer, new SerializationContext());
			logon = RawFixUtil.GetFixMessage(buffer.GetByteArray(), 0, buffer.Offset);

			// 1 -> 000001
			var seqNum = logon.GetTag(Tags.MsgSeqNum).StringValue;
			ClassicAssert.That(seqNum, Is.EqualTo("000000001"));
			// -1 -> -000001
			var lastSeqNum = logon.GetTag(Tags.LastMsgSeqNumProcessed)?.StringValue;
			ClassicAssert.That(lastSeqNum, Is.EqualTo("-00000001"));
			// 0 -> 000000
			var nextSeqNum = logon.GetTag(Tags.NextExpectedMsgSeqNum).StringValue;
			ClassicAssert.That(nextSeqNum, Is.EqualTo("000000000"));
		}

		[Test, Property("JIRA", "BBP-24520")]
		public void CheckField_45_Test()
		{
			var handler = new SequenceResetMessageHandler();
			handler.Session = _sessionHelper;
			_sessionHelper.ResetQueue();

			_sessionHelper.InSeqNum = 100;
			var sequenceReset = new FixMessage();
			sequenceReset.AddTag(Tags.MsgType, "4");
			sequenceReset.AddTag(Tags.MsgSeqNum, (long)100);
			sequenceReset.AddTag(Tags.NewSeqNo, (long)50);
			handler.OnNewMessage(sequenceReset);

			var reject = _sessionHelper.GetMessageFromQueue();
			ClassicAssert.IsNotNull(reject);
			var nextSeqNum = reject.GetTag(Tags.RefSeqNum).StringValue;
			ClassicAssert.That(nextSeqNum, Is.EqualTo("000000100"));
		}

		[Test, Property("JIRA", "BBP-24520")]
		public void CheckField_36_Test()
		{
			var message = RawFixUtil.GetFixMessage("8=FIX.4.4\u00019=109\u000135=2\u000149=TRGT\u000156=SNDR\u000134=2\u000152=20100723-11:03:44.995\u00017=2000\u000116=0\u000110=190\u0001".AsByteArray());
			var handler = new ResendRequestMessageHandler();
			var session = new TestFixSession();
			handler.Session = session;
			session.RuntimeState.OutSeqNum = 5000;

			handler.OnNewMessage(message);

			ClassicAssert.IsTrue(session.Messages.Count > 0);
			var msg = session.Messages[0];
			ClassicAssert.AreEqual(4, msg.GetTagAsInt(Tags.MsgType));
			var newSeqNum = msg.GetTagValueAsString(Tags.NewSeqNo);
			ClassicAssert.That(newSeqNum, Is.EqualTo("000005000"));
		}

		[Test, Property("JIRA", "BBP-24520")]
		public void CheckField_7_16_Test()
		{
			var session = new TestFixSession();
			session.SequenceManager.ApplyInSeqNum(5);
			var handler = new OutOfSequenceMessageHandler();
			handler.Session = session;

			var message = new FixMessage();
			message.AddTag(8, "FIX.4.4");
			message.AddTag(35, "0");
			message.AddTag(34, "7");
			message.AddTag(10, "20");

			handler.OnNewMessage(message);

			var resendSeqMessage = session.Messages[0];
			ClassicAssert.IsNotNull(resendSeqMessage);

			var beginSeqNo = resendSeqMessage.GetTagValueAsString(Tags.BeginSeqNo);
			ClassicAssert.That(beginSeqNo, Is.EqualTo("000000005"));

			var endSeqNo = resendSeqMessage.GetTagValueAsString(Tags.EndSeqNo);
			ClassicAssert.That(endSeqNo, Is.EqualTo("000000000"));
		}
	}
}

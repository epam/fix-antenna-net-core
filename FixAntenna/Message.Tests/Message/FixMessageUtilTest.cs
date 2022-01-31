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
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework;

namespace Epam.FixAntenna.Message.Tests
{
	[TestFixture]
	internal class FixMessageUtilTest
	{
		private void CheckRrMessage(bool expected, string posDup, string gapFill, bool seqToHigh)
		{
			var posDupField = ReferenceEquals(posDup, null) ? "" : "43=" + posDup + "\u0001";
			var gapFillField = ReferenceEquals(gapFill, null) ? "" : "123=" + gapFill + "\u0001";
			var msg = "8=FIX.4.2\u00019=110\u000135=4\u000134=34653\u000149=initiator\u000156=acceptor\u0001" +
					"52=20110125-13:28:54.931\u0001" + posDupField + "122=20110125-13:28:54.883\u0001" + gapFillField +
					"" + "36=34655\u000110=217\u0001";
			var parsedMsg = RawFixUtil.GetFixMessage(msg.AsByteArray());
			var actualResult = FixMessageUtil.IsIgnorableMsg(parsedMsg);
			Assert.AreEqual(expected, actualResult,
				"Invalid case for posDup=" + posDup + ", gapFill=" + gapFill + " and seqToHigh=" + seqToHigh);
		}

		private FixMessage UpdateMessage(FixMessage msg, int tagId, byte[] value)
		{
			msg.UpdateValue(tagId, value, IndexedStorage.MissingTagHandling.AddIfNotExists);
			return msg;
		}

		private FixMessage GetTestMessage()
		{
			var msg = "8=FIX.4.2\u00019=110\u000135=4\u000134=34653\u000149=initiator\u000156=acceptor\u0001" +
					"52=20110125-13:28:54.931\u0001122=20110125-13:28:54.883\u0001" + "36=34655\u000110=217\u0001";
			return RawFixUtil.GetFixMessage(msg.AsByteArray());
		}

		[Test]
		public virtual void TestIsEqualIgnoreOrder()
		{
			var msg1 = new FixMessage();
			msg1.AddTag(1, "1");
			msg1.AddTag(2, "2");
			msg1.AddTag(3, "3");

			var msg2 = new FixMessage();
			msg2.AddTag(1, "1");
			msg2.AddTag(3, "3");
			msg2.AddTag(2, "2");

			Assert.IsTrue(FixMessageUtil.IsEqualIgnoreOrder(msg1, msg2));

			// change value. Now messages is not equals.
			msg2 = UpdateMessage(msg2, 2, "5".AsByteArray());
			Assert.IsFalse(FixMessageUtil.IsEqualIgnoreOrder(msg1, msg2));
		}

		[Test]
		public virtual void TestIsGapFill()
		{
			var tagId = Tags.GapFillFlag;
			var msg = UpdateMessage(GetTestMessage(), tagId, "Y".AsByteArray());
			Assert.IsTrue(FixMessageUtil.IsGapFill(msg));

			msg = UpdateMessage(GetTestMessage(), tagId, "N".AsByteArray());
			Assert.IsFalse(FixMessageUtil.IsGapFill(msg));

			msg = GetTestMessage();
			msg.RemoveTag(tagId);
			Assert.IsFalse(FixMessageUtil.IsGapFill(msg));
		}

		[Test]
		public virtual void testIsIgnorableMsg_ResendRequest()
		{
			var rrMsg = "8=FIX.4.2 | 9=110 | 35=2 | 34=34653 | 49=initiator | 56=acceptor | " +
						"52=20110125-13:28:54.931 | 43=Y | 7=1 | 16=0 | 10=217 | ";
			var rr = RawFixUtil.GetFixMessage(rrMsg.Replace(" | ", "\u0001").AsByteArray());
			Assert.IsTrue(FixMessageUtil.IsIgnorableMsg(rr), "Duplicated ResendRequest message should be ignorable");
			rr.RemoveTag(Tags.PossDupFlag);
			Assert.IsFalse(FixMessageUtil.IsIgnorableMsg(rr),
				"Non-duplicated ResendRequest message shouldn't be ignorable");
		}

		[Test]
		public virtual void testIsIgnorableMsg_ResetLogon()
		{
			var logonMsg = "8=FIX.4.2 | 9=110 | 35=A | 34=1 | 49=initiator | 56=acceptor | " +
							"52=20110125-13:28:54.931 | 141=Y | 108=30 | 10=217 | ";
			var logon = RawFixUtil.GetFixMessage(logonMsg.Replace(" | ", "\u0001").AsByteArray());
			Assert.IsTrue(FixMessageUtil.IsIgnorableMsg(logon), "Sequence reset Logon message should be ignorable");

			var wrongLogon = (FixMessage)logon.Clone();
			wrongLogon.Set(Tags.MsgSeqNum, 2);
			Assert.IsFalse(FixMessageUtil.IsIgnorableMsg(wrongLogon),
				"Wrong sequence reset Logon  message shouldn't be ignorable");

			var wrongLogon2 = (FixMessage)logon.Clone();
			wrongLogon2.Set(Tags.ResetSeqNumFlag, 'N');
			Assert.IsFalse(FixMessageUtil.IsIgnorableMsg(wrongLogon2),
				"Wrong sequence reset Logon  message shouldn't be ignorable");

			wrongLogon2.RemoveTag(Tags.ResetSeqNumFlag);
			Assert.IsFalse(FixMessageUtil.IsIgnorableMsg(wrongLogon2),
				"Wrong sequence reset Logon  message shouldn't be ignorable");
		}

		[Test]
		public virtual void testIsIgnorableMsg_SequenceReset()
		{
			var seqResetMsg = "8=FIX.4.2 | 9=110 | 35=4 | 34=34653 | 49=initiator | 56=acceptor | " +
							"52=20110125-13:28:54.931 | 123=N | 36=34655 | 10=217 | ";
			var seqReset = RawFixUtil.GetFixMessage(seqResetMsg.Replace(" | ", "\u0001").AsByteArray());
			Assert.IsTrue(FixMessageUtil.IsIgnorableMsg(seqReset), "SequenceReset message should be ignorable");
			seqReset.Set(Tags.GapFillFlag, "Y");
			Assert.IsFalse(FixMessageUtil.IsIgnorableMsg(seqReset), "GapFill message shouldn't be ignorable");
		}

		[Test]
		public virtual void TestIsLogin()
		{
			var tagId = Tags.MsgType;
			var msg = UpdateMessage(GetTestMessage(), tagId, "A".AsByteArray());
			Assert.IsTrue(FixMessageUtil.IsLogon(msg));

			msg = UpdateMessage(GetTestMessage(), tagId, "N".AsByteArray());
			Assert.IsFalse(FixMessageUtil.IsLogon(msg));
		}

		[Test]
		public virtual void TestIsLogout()
		{
			var tagId = Tags.MsgType;
			var msg = UpdateMessage(GetTestMessage(), tagId, "5".AsByteArray());
			Assert.IsTrue(FixMessageUtil.IsLogout(msg));

			msg = UpdateMessage(GetTestMessage(), tagId, "N".AsByteArray());
			Assert.IsFalse(FixMessageUtil.IsLogout(msg));
		}

		[Test]
		public virtual void TestIsMessageType()
		{
			byte[] value1 = { (byte)'D' };
			var msg = UpdateMessage(GetTestMessage(), 35, value1);
			Assert.IsTrue(FixMessageUtil.IsMessageType(msg, value1));
			Assert.IsFalse(FixMessageUtil.IsMessageType(msg, "A".AsByteArray()));

			var value2 = "AA".AsByteArray();
			msg = UpdateMessage(msg, 35, value2);
			Assert.IsTrue(FixMessageUtil.IsMessageType(msg, value2));
			Assert.IsFalse(FixMessageUtil.IsMessageType(msg, "BB".AsByteArray()));
		}

		[Test]
		public virtual void TestIsPosDup()
		{
			var tagId = Tags.PossDupFlag;
			var msg = UpdateMessage(GetTestMessage(), tagId, "Y".AsByteArray());
			Assert.IsTrue(FixMessageUtil.IsPosDup(msg));

			msg = UpdateMessage(GetTestMessage(), tagId, "N".AsByteArray());
			Assert.IsFalse(FixMessageUtil.IsPosDup(msg));

			msg = GetTestMessage();
			msg.RemoveTag(tagId);
			Assert.IsFalse(FixMessageUtil.IsPosDup(msg));
		}

		[Test]
		public virtual void TestIsResetLogon()
		{
			var tagId = Tags.MsgType;

			var msg = UpdateMessage(GetTestMessage(), tagId, "A".AsByteArray());
			msg = UpdateMessage(msg, Tags.ResetSeqNumFlag, "Y".AsByteArray());
			msg = UpdateMessage(msg, Tags.MsgSeqNum, "1".AsByteArray());
			Assert.IsTrue(FixMessageUtil.IsResetLogon(msg), "Logon with ResetSeqNumFlag 'Y', seqNum=1");

			msg = UpdateMessage(msg, Tags.ResetSeqNumFlag, "N".AsByteArray());
			Assert.IsFalse(FixMessageUtil.IsResetLogon(msg), "Logon with ResetSeqNumFlag 'N', seqNum=1");

			msg = UpdateMessage(msg, Tags.ResetSeqNumFlag, "Y".AsByteArray());
			msg = UpdateMessage(msg, Tags.MsgSeqNum, "5".AsByteArray());
			Assert.IsFalse(FixMessageUtil.IsResetLogon(msg), "Logon with ResetSeqNumFlag 'Y', seqNum=5");

			msg.RemoveTag(Tags.ResetSeqNumFlag);
			msg = UpdateMessage(msg, Tags.MsgSeqNum, "1".AsByteArray());
			Assert.IsFalse(FixMessageUtil.IsResetLogon(msg), "Logon without ResetSeqNumFlag, seqNum=1");

			msg = UpdateMessage(GetTestMessage(), tagId, "0".AsByteArray());
			msg = UpdateMessage(msg, Tags.ResetSeqNumFlag, "Y".AsByteArray());
			msg = UpdateMessage(msg, Tags.MsgSeqNum, "1".AsByteArray());
			Assert.IsFalse(FixMessageUtil.IsResetLogon(msg), "Not Logon with ResetSeqNumFlag 'Y'");

			msg.RemoveTag(Tags.ResetSeqNumFlag);
			Assert.IsFalse(FixMessageUtil.IsResetLogon(msg), "Not Logon without ResetSeqNumFlag");
		}

		[Test]
		public virtual void TestIsSeqReset()
		{
			var tagId = Tags.MsgType;
			var msg = UpdateMessage(GetTestMessage(), tagId, "4".AsByteArray());
			Assert.IsTrue(FixMessageUtil.IsSeqReset(msg));

			msg = UpdateMessage(GetTestMessage(), tagId, "N".AsByteArray());
			Assert.IsFalse(FixMessageUtil.IsSeqReset(msg));
		}

		[Test]
		public virtual void TestIsTagValueEqualsBytes()
		{
			var value = "abc".AsByteArray();
			var tagId = 49;
			var msg = UpdateMessage(GetTestMessage(), tagId, value);
			Assert.IsTrue(FixMessageUtil.IsTagValueEquals(msg, tagId, value));
			Assert.IsFalse(FixMessageUtil.IsTagValueEquals(msg, tagId, "BB111".AsByteArray()));
		}

		[Test]
		public virtual void TestIsTagValueEqualsString()
		{
			var value = "abc";
			var tagId = 49;
			var msg = UpdateMessage(GetTestMessage(), tagId, value.AsByteArray());
			Assert.IsTrue(FixMessageUtil.IsTagValueEquals(msg, tagId, value));
			Assert.IsFalse(FixMessageUtil.IsTagValueEquals(msg, tagId, "BB111"));
		}

		[Test]
		public virtual void TestResentResetGapFillWithHighSeq()
		{
			//GapFill was resent - should be processed according to sequence
			CheckRrMessage(false, "Y", "Y", true);

			//GapFill - should be processed according to sequence
			CheckRrMessage(false, "N", "Y", true);
			CheckRrMessage(false, null, "Y", true);

			//Sequence Reset was resent - should be processed anyway
			CheckRrMessage(true, "Y", "N", true);
			CheckRrMessage(true, "Y", null, true);

			//Sequence Reset - should be processed anyway
			CheckRrMessage(true, "N", "N", true);
			CheckRrMessage(true, null, "N", true);
			CheckRrMessage(true, "N", null, true);
			CheckRrMessage(true, null, null, true);

			//GapFill was resent - should be processed according to sequence
			CheckRrMessage(false, "Y", "Y", false);

			//GapFill - should be processed according to sequence
			CheckRrMessage(false, "N", "Y", false);
			CheckRrMessage(false, null, "Y", false);

			//Sequence Reset was resent - should be processed anyway
			CheckRrMessage(true, "Y", "N", false);
			CheckRrMessage(true, "Y", null, false);

			//Sequence Reset - should be processed anyway
			CheckRrMessage(true, "N", "N", false);
			CheckRrMessage(true, null, "N", false);
			CheckRrMessage(true, "N", null, false);
			CheckRrMessage(true, null, null, false);
		}
	}
}
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

using Epam.FixAntenna.AdminTool.Tests.Util;
using NUnit.Framework;

namespace Epam.FixAntenna.AdminTool.Tests.Xml
{
	internal class TestXMLContent : XMLGenericHelper
	{
		[SetUp]
		public void Before()
		{
			LogAppender.Clear();
		}

		[TearDown]
		public void After()
		{
			LogAppender.AssertIfErrorExist();
		}

		[Test]
		public void TestSessionStatCommand()
		{
			var sessionStat =
				"<SessionStat>\n" +
				"\t<SenderCompID>TestSender</SenderCompID>\n" +
				"\t<TargetCompID>TestTarget</TargetCompID>\n" +
				"</SessionStat>";
			Execute(sessionStat);
		}

		[Test]
		public void TestSessionListCommand()
		{
			var sessionList = "<SessionsList />";
			Execute(sessionList);
		}

		[Test]
		public void TestSessionStatusCommand()
		{
			var sessionList =
				"<SessionStatus>\n" +
				"\t<SenderCompID>TestSender</SenderCompID>\n" +
				"\t<TargetCompID>TestTarget</TargetCompID>\n" +
				"</SessionStatus>";
			Execute(sessionList);
		}

		[Test]
		public void TestReceivedStatCommand()
		{
			var sessionList = "<ReceivedStat />";
			Execute(sessionList);
		}

		[Test]
		public void TestSentStatCommand()
		{
			var sessionList = "<SentStat />";
			Execute(sessionList);
		}

		[Test]
		public void TestProceedStatCommand()
		{
			var sessionList = "<ProceedStat />";
			Execute(sessionList);
		}

		[Test]
		public void TestDeleteCommand()
		{
			var sessionList =
				"<Delete>\n" +
				"\t<SenderCompID>TestSender</SenderCompID>\n" +
				"\t<TargetCompID>TestTarget</TargetCompID>\n" +
				"\t<SendLogout>true</SendLogout>\n" +
				"\t<LogoutReason>Evacuation</LogoutReason>\n" +
				"</Delete>";
			Execute(sessionList);
		}

		[Test]
		public void TestToBackupCommand()
		{
			var sessionList =
				"<ToBackup>\n" +
				"\t<SenderCompID>TestSender</SenderCompID>\n" +
				"\t<TargetCompID>TestTarget</TargetCompID>\n" +
				"</ToBackup>";
			Execute(sessionList);
		}

		[Test]
		public void TestChangeSeqNumCommand()
		{
			var sessionList =
				"<ChangeSeqNum>\n" +
				"\t<SenderCompID>TestSender</SenderCompID>\n" +
				"\t<TargetCompID>TestTarget</TargetCompID>\n" +
				"\t<InSeqNum>100</InSeqNum>\n" +
				"\t<OutSeqNum>100</OutSeqNum>\n" +
				"</ChangeSeqNum>";
			Execute(sessionList);
		}

		[Test]
		public void TestResetSeqNumCommand()
		{
			var sessionList =
				"<ResetSeqNum>\n" +
				"\t<SenderCompID>TestSender</SenderCompID>\n" +
				"\t<TargetCompID>TestTarget</TargetCompID>\n" +
				"</ResetSeqNum>";
			Execute(sessionList);
		}

		[Test]
		public void TestTestRequestCommand()
		{
			var sessionList =
				"<TestRequest> \n" +
				"\t<SenderCompID>TestSender</SenderCompID>\n" +
				"\t<TargetCompID>TestTarget</TargetCompID>\n" +
				"\t<TestReqID>12345576</TestReqID>\n" +
				"</TestRequest>";
			Execute(sessionList);
		}

		[Test]
		public void TestHeartbeatCommand()
		{
			var sessionList =
				"<Heartbeat>\n" +
				"\t<SenderCompID>TestSender</SenderCompID>\n" +
				"\t<TargetCompID>TestTarget</TargetCompID>\n" +
				"</Heartbeat>";
			Execute(sessionList);
		}

		[Test]
		public void TestSendMessageCommand()
		{
			var sessionList =
				"<SendMessage>\n" +
				"\t<SenderCompID>TestSender</SenderCompID>\n" +
				"\t<TargetCompID>TestTarget</TargetCompID>\n" +
				" \t<Message>8=FIX.4.2\u00019=056\u000135=8\u000149=THEM\u000156=US\u000137=1\u000117=1\u000134=530\u000111=00492-0476\u0001150=0\u000139=0\u000110=161\u0001</Message>\n" +
				"</SendMessage>";
			Execute(sessionList);
		}

		[Test]
		public void TestDeleteAllCommand()
		{
			var sessionList =
				"<DeleteAll>\n" +
				"\t<SendLogout>true</SendLogout>\n" + "" +
				"\t<LogoutReason>Evacuation</LogoutReason>\n" +
				"</DeleteAll>";
			Execute(sessionList);
		}

		[Test]
		public void TestHelpCommand()
		{
			var sessionList = "<Help />";
			Execute(sessionList);
		}
	}
}
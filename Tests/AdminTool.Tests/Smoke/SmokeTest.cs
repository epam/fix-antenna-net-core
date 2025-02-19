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
using Epam.FixAntenna.Fixicc.Message;
using Epam.FixAntenna.NetCore.FixEngine;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.AdminTool.Tests.Smoke
{
	internal class SmokeTest
	{
		private FixInitiatorHelper _initiatorHelper;
		private FixAcceptorHelper _acceptorHelper;

		private const string Target = "admin1";
		private const string User = Target;
		private const string Password = Target;
		private const string Sender = "sender";
		private const int Port = 12345;

		[SetUp]
		public void SetUp()
		{
			_acceptorHelper = new FixAcceptorHelper(Target, Sender, Port);
			_acceptorHelper.Start();

			_initiatorHelper = new FixInitiatorHelper(Sender, Target, Port, User, Password);
			_initiatorHelper.Start();

			_acceptorHelper.WaitForStartup();
		}

		[TearDown]
		public void TearDown()
		{
			_initiatorHelper.Stop();
			_acceptorHelper.Stop();
			_initiatorHelper.Dispose();
			_acceptorHelper.Dispose();
			LogAppender.Clear();
		}

		[Test, Timeout(10000)]
		public void TestProcess()
		{
			var adminSession = _acceptorHelper.Session;
			ClassicAssert.IsNotNull(adminSession, "acceptor not started");

			var fixSession = _initiatorHelper.Session;
			ClassicAssert.IsNotNull(fixSession, "initiator not started");

			var sessionStatus = new SessionStatus();
			sessionStatus.RequestID = 1L;
			sessionStatus.SenderCompID = adminSession.Parameters.SenderCompId;
			sessionStatus.TargetCompID = adminSession.Parameters.TargetCompId;

			_initiatorHelper.SendRequest(sessionStatus);
			var response = _initiatorHelper.GetResponse();

			ClassicAssert.IsNotNull(response, "no response");
			ClassicAssert.AreEqual(1L, response.RequestID);
			ClassicAssert.AreEqual(ResultCode.OperationSuccess.Code, response.ResultCode);

			var sessionStatusResponse = response.SessionStatusData;
			ClassicAssert.IsNotNull(sessionStatusResponse);
			ClassicAssert.AreEqual(Target, sessionStatusResponse.SenderCompID);
			ClassicAssert.AreEqual(Sender, sessionStatusResponse.TargetCompID);
			ClassicAssert.IsNotNull(sessionStatusResponse.InSeqNum);
			ClassicAssert.IsNotNull(sessionStatusResponse.OutSeqNum);
			ClassicAssert.IsNotNull(sessionStatusResponse.Status);
			ClassicAssert.AreEqual(SessionState.Connected.ToString(), sessionStatusResponse.Status);
		}
	}
}
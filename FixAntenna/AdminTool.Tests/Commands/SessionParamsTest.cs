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

using Epam.FixAntenna.Fixicc.Message;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using NUnit.Framework;

namespace Epam.FixAntenna.AdminTool.Tests.Commands
{
	internal class SessionParamsTest : AdminToolHelper
	{
		private SessionParams _sessionParams;

		[SetUp]
		public void Setup()
		{
			base.Before();
			_sessionParams = new SessionParams();
			RequestID = GetNextRequest();
			_sessionParams.RequestID = RequestID;

			FixSession = FindAdminSession();
		}

		[Test]
		public void TestSessionParams()
		{
			_sessionParams.SenderCompID = TestSessionID.Sender;
			_sessionParams.TargetCompID = TestSessionID.Target;

			var response = GetReponse(_sessionParams);

			var sessionParamsData = response.SessionParamsData;

			// sender and target
			Assert.AreEqual(sessionParamsData.SenderCompID, TestSessionID.Sender);
			Assert.AreEqual(sessionParamsData.TargetCompID, TestSessionID.Target);

			// session type
			Assert.AreEqual(SessionRole.INITIATOR, sessionParamsData.Role);

			ValidateSessionParams(GetSession(TestSessionID).Parameters, sessionParamsData.ExtraSessionParams);
		}

		[Test]
		public void TestSessionParamsSessionQualifier()
		{
			_sessionParams.SenderCompID = TestSessionIdQualifier.Sender;
			_sessionParams.TargetCompID = TestSessionIdQualifier.Target;
			_sessionParams.SessionQualifier = TestSessionIdQualifier.Qualifier;

			var response = GetReponse(_sessionParams);

			var sessionParamsData = response.SessionParamsData;
			// sender and target
			Assert.AreEqual(sessionParamsData.SenderCompID, TestSessionIdQualifier.Sender);
			Assert.AreEqual(sessionParamsData.TargetCompID, TestSessionIdQualifier.Target);
			Assert.AreEqual(sessionParamsData.SessionQualifier, TestSessionIdQualifier.Qualifier);

			// session type
			Assert.AreEqual(SessionRole.INITIATOR, sessionParamsData.Role);

			ValidateSessionParams(GetSession(TestSessionIdQualifier).Parameters, sessionParamsData.ExtraSessionParams);
		}

		[Test]
		public void TestSessionParamsSessionQualifierAndLogonQualifierTag()
		{
			_sessionParams.SenderCompID = TestSessionIdQualifier.Sender;
			_sessionParams.TargetCompID = TestSessionIdQualifier.Target;
			_sessionParams.SessionQualifier = TestSessionIdQualifier.Qualifier;

			var response = GetReponse(_sessionParams);

			var sessionParamsData = response.SessionParamsData;
			// sender and target
			Assert.AreEqual(sessionParamsData.SenderCompID, TestSessionIdQualifier.Sender);
			Assert.AreEqual(sessionParamsData.TargetCompID, TestSessionIdQualifier.Target);
			Assert.AreEqual(sessionParamsData.SessionQualifier, TestSessionIdQualifier.Qualifier);

			// session type
			Assert.AreEqual(SessionRole.INITIATOR, sessionParamsData.Role);

			var sessionParameters = GetSession(TestSessionIdQualifier).Parameters;
			var configuration = sessionParameters.Configuration;
			var outgoingLoginFixMsg = sessionParameters.OutgoingLoginMessage;
			var configuredQualifierTag = configuration.GetPropertyAsInt(Config.LogonMessageSessionQualifierTag);

			Assert.AreEqual(configuredQualifierTag.ToString(), sessionParamsData.ExtraSessionParams.LogonMessageSessionQualifierTag);

			Assert.AreEqual(outgoingLoginFixMsg.GetTagValueAsString(configuredQualifierTag), sessionParamsData.SessionQualifier);

			ValidateSessionParams(sessionParameters, sessionParamsData.ExtraSessionParams);
		}


		private void ValidateSessionParams(SessionParameters testedSessionParameters, ExtraSessionParams extraSessionParams)
		{
			Assert.AreEqual(extraSessionParams.Username, testedSessionParameters.OutgoingLoginMessage.GetTagValueAsString(553));
			Assert.AreEqual(extraSessionParams.Password, testedSessionParameters.OutgoingLoginMessage.GetTagValueAsString(554));
			Assert.AreEqual(extraSessionParams.ForcedReconnect, testedSessionParameters.Configuration.GetPropertyAsInt(Config.AutoreconnectAttempts) >= 0);
			Assert.AreEqual(extraSessionParams.EnableMessageRejecting, testedSessionParameters.Configuration.GetPropertyAsBoolean(Config.EnableMessageRejecting));
			var batchSize = testedSessionParameters.Configuration.GetPropertyAsInt(Config.MaxMessagesToSendInBatch, 0);
			if (batchSize > 0)
			{
				Assert.IsNotNull(extraSessionParams.MaxMessagesAmountInBunch);
				Assert.AreEqual(extraSessionParams.MaxMessagesAmountInBunch.Value, batchSize);
			}
			else
			{
				Assert.IsNull(extraSessionParams.MaxMessagesAmountInBunch);
			}
		}

		[Test]
		public void TestNoParamRequestId()
		{
			_sessionParams.SenderCompID = TestSessionID.Sender;
			_sessionParams.TargetCompID = TestSessionID.Target;

			_sessionParams.RequestID = null;

			var response = GetReponse(_sessionParams);
			Assert.AreEqual(ResultCode.OperationInvalidArgument.Code, response.ResultCode);
		}
	}
}
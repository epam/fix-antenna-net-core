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
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using NUnit.Framework; 
using NUnit.Framework.Legacy;
using ForceSeqNumReset = Epam.FixAntenna.Fixicc.Message.ForceSeqNumReset;

namespace Epam.FixAntenna.AdminTool.Tests.Commands
{
	internal class AcceptorParamTest : AdminToolHelper
	{
		private CreateAcceptor _createAcceptor;

		private static object[] TestData =
		{
			new object[] { "", "", "", "", "target", "sender" },
			new object[] { null, null, null, null, "target", "sender" },
			new object[] { "senderSubId", "targetSubId", "senderLocId", "targetLocId", "target", "sender" },
			new object[] { "", null, "", null, "target", "sender" }
		};

		[SetUp]
		public void Setup()
		{
			base.Before();
			RequestID = GetNextRequest();
			_createAcceptor = new CreateAcceptor { RequestID = RequestID };
			FixSession = FindAdminSession();
		}

		[TearDown]
		public void TearDown()
		{
			FixSessionManager.DisposeAllSession();
		}

		private void FillValidRequest(CreateAcceptor createInitiator, string senderSubId, string targetSubId, string senderLocId, string targetLocId, string targetCompID, string senderCompId)
		{
			createInitiator.TargetCompID = targetCompID;
			createInitiator.SenderCompID = senderCompId;
			createInitiator.Version = "FIX44";
			createInitiator.ExtraSessionParams = new ExtraSessionParams();

			createInitiator.ExtraSessionParams.CyclicSwitchBackupConnection = true;
			createInitiator.ExtraSessionParams.EnableAutoSwitchToBackupConnection =true;
			createInitiator.ExtraSessionParams.ForceSeqNumReset = ForceSeqNumReset.ON;
			createInitiator.ExtraSessionParams.EnableMessageRejecting = true;
			createInitiator.ExtraSessionParams.StorageType = StorageType.TRANSIENT;

			createInitiator.ExtraSessionParams.SenderSubID = senderSubId;
			createInitiator.ExtraSessionParams.TargetSubID = targetSubId;
			createInitiator.ExtraSessionParams.SenderLocationID = senderLocId;
			createInitiator.ExtraSessionParams.TargetLocationID = targetLocId;
		}

		private void ClassicAssertValidSession(CreateAcceptor exceptedSessionData, SessionParameters actualSessionParameters)
		{
			ClassicAssert.AreEqual(actualSessionParameters.SenderCompId, exceptedSessionData.SenderCompID);
			ClassicAssert.AreEqual(actualSessionParameters.TargetCompId, exceptedSessionData.TargetCompID);
			ClassicAssert.AreEqual(actualSessionParameters.ForceSeqNumReset, NetCore.FixEngine.ForceSeqNumReset.OneTime);
			var configurationAdaptor = new ConfigurationAdapter(actualSessionParameters.Configuration);
			ClassicAssert.IsTrue(configurationAdaptor.IsAutoSwitchToBackupConnectionEnabled);
			ClassicAssert.IsTrue(configurationAdaptor.IsCyclicSwitchBackupConnectionEnabled);
			ClassicAssert.IsTrue(configurationAdaptor.IsEnableMessageRejecting);
			ClassicAssert.IsTrue
			(
				configurationAdaptor.StorageFactoryClass.Contains("Memory"),
				configurationAdaptor.StorageFactoryClass
			);
		}

		[TestCaseSource(nameof(TestData))]
		public void Test(string senderSubId, string targetSubId, string senderLocId, string targetLocId, string targetCompID, string senderCompId)
		{
			FillValidRequest(_createAcceptor, senderSubId, targetSubId, senderLocId, targetLocId, targetCompID, senderCompId);
			var created = new SyncFlag(false);
			ConfiguredSessionRegister.AddSessionManagerListener(new ConfiguredSessionListenerAnonymousInnerClass(this, created));

			var response = GetReponse(_createAcceptor);
			ClassicAssert.AreEqual(ResultCode.OperationSuccess.Code, response.ResultCode);
			ClassicAssert.IsTrue(created.Value);

			LogAppender.Clear();
			FixSessionManager.DisposeAllSession();
		}

		private class ConfiguredSessionListenerAnonymousInnerClass : IConfiguredSessionListener
		{
			private readonly AcceptorParamTest _outerInstance;
			private SyncFlag _created;

			public ConfiguredSessionListenerAnonymousInnerClass(AcceptorParamTest outerInstance, SyncFlag created)
			{
				_outerInstance = outerInstance;
				_created = created;
			}

			public void OnAddSession(SessionParameters sessionParameters)
			{
				if (sessionParameters.SenderCompId.Equals("sender")
						&& sessionParameters.TargetCompId.Equals("target"))
				{
					_outerInstance.ClassicAssertValidSession(_outerInstance._createAcceptor, sessionParameters);
					_created.Value = true;
				}
			}

			public void OnRemoveSession(SessionParameters @params)
			{
			}
		}
	}
}
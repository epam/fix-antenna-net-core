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
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using NUnit.Framework; 
using NUnit.Framework.Legacy;
using ForceSeqNumReset = Epam.FixAntenna.NetCore.FixEngine.ForceSeqNumReset;

namespace Epam.FixAntenna.AdminTool.Tests.Commands
{
	/// <summary>
	/// Junit test class for CreateAcceptor command
	/// </summary>
	internal class CreateAcceptorTest : AdminToolHelper
	{
		private CreateAcceptor _createAcceptor;

		[SetUp]
		public void Setup()
		{
			Before();
			_createAcceptor = new CreateAcceptor();
			RequestID = GetNextRequest();
			_createAcceptor.RequestID = RequestID;

			FixSession = FindAdminSession();
		}


		[TearDown]
		public void TearDown()
		{
			FixSessionManager.DisposeAllSession();
		}

		[Test]
		public void TestCreateAcceptorWithoutRequestID()
		{
			_createAcceptor.RequestID = null;
			var response = GetReponse(_createAcceptor);
			ClassicAssert.AreEqual(ResultCode.OperationInvalidArgument.Code, response.ResultCode);
		}

		[Test]
		public void TestInvalidSenderTarget()
		{
			var response = GetReponse(_createAcceptor);
			ClassicAssert.AreEqual(ResultCode.OperationInvalidArgument.Code, response.ResultCode);
		}

		[Test]
		public void TestInvalidVersion()
		{
			_createAcceptor.TargetCompID = "target";
			_createAcceptor.SenderCompID = "sender";

			var response = GetReponse(_createAcceptor);
			ClassicAssert.AreEqual(ResultCode.OperationInvalidArgument.Code, response.ResultCode);
		}

		[Test]
		public void TestCreateAcceptor()
		{
			FillValidRequest(_createAcceptor);
			var created = new SyncFlag(false);
			ConfiguredSessionRegister.AddSessionManagerListener(new ConfiguredSessionListenerAnonymousInnerClass(this, created));

			var response = GetReponse(_createAcceptor);
			ClassicAssert.AreEqual(ResultCode.OperationSuccess.Code, response.ResultCode);
			ClassicAssert.IsTrue(created.Value);
			ClassicAssert.IsNotNull(ConfiguredSessionRegister.GetSessionParams(new SessionId("sender", "target")));
			LogAppender.Clear();
			FixSessionManager.DisposeAllSession();
		}

		private class ConfiguredSessionListenerAnonymousInnerClass : IConfiguredSessionListener
		{
			private readonly CreateAcceptorTest _test;
			private readonly SyncFlag _created;

			public ConfiguredSessionListenerAnonymousInnerClass(CreateAcceptorTest test, SyncFlag created)
			{
				_test = test;
				_created = created;
			}

			public void OnAddSession(SessionParameters sessionParameters)
			{
				if (sessionParameters.SenderCompId.Equals("sender")
						&& sessionParameters.TargetCompId.Equals("target"))
				{
					_test.ClassicAssertValidSession(_test._createAcceptor, sessionParameters);
					_created.Value = true;
				}
			}

			public void OnRemoveSession(SessionParameters @params)
			{
			}
		}

		[Test]
		public void TestCreateAcceptorWithQualifier()
		{

			FillValidRequest(_createAcceptor);
			_createAcceptor.SessionQualifier = "idT";

			var created = new SyncFlag(false);
			ConfiguredSessionRegister.AddSessionManagerListener(new ConfiguredSessionListenerAnonymousInnerClass2(this, created));

			var response = GetReponse(_createAcceptor);
			ClassicAssert.AreEqual(ResultCode.OperationSuccess.Code, response.ResultCode);
			ClassicAssert.IsTrue(created.Value);
			ClassicAssert.IsNull(ConfiguredSessionRegister.GetSessionParams(new SessionId("sender", "target")));
			ClassicAssert.IsNotNull(ConfiguredSessionRegister.GetSessionParams(new SessionId("sender", "target", "idT")));
			LogAppender.Clear();
			FixSessionManager.DisposeAllSession();
		}

		private class ConfiguredSessionListenerAnonymousInnerClass2 : IConfiguredSessionListener
		{
			private readonly CreateAcceptorTest _outerInstance;
			private readonly SyncFlag _created;

			public ConfiguredSessionListenerAnonymousInnerClass2(CreateAcceptorTest outerInstance, SyncFlag created)
			{
				_outerInstance = outerInstance;
				_created = created;
			}

			public void OnAddSession(SessionParameters sessionParameters)
			{
				if (sessionParameters.SenderCompId.Equals("sender")
						&& sessionParameters.TargetCompId.Equals("target")
						&& sessionParameters.SessionQualifier.Equals("idT"))
				{
					_outerInstance.ClassicAssertValidSession(_outerInstance._createAcceptor, sessionParameters);
					_created.Value = true;
				}
			}

			public void OnRemoveSession(SessionParameters @params)
			{
			}
		}

		[Test]
		public void TestCreateAcceptorWithQualifierAndLogonQualifierTag()
		{
			const string qualifierVal = "idT";
			const int qualifierTag = 9021;

			FillValidRequest(_createAcceptor);
			//add options for qualifier tag customization
			_createAcceptor.SessionQualifier = qualifierVal;
			_createAcceptor.ExtraSessionParams.LogonMessageSessionQualifierTag = qualifierTag.ToString();

			var acceptorCreated = new SyncFlag(false);
			ConfiguredSessionRegister.AddSessionManagerListener(new ConfiguredSessionListenerAnonymousInnerClass3(this, qualifierVal, qualifierTag, acceptorCreated));

			var response = GetReponse(_createAcceptor);
			ClassicAssert.AreEqual(ResultCode.OperationSuccess.Code, response.ResultCode);
			ClassicAssert.IsTrue(acceptorCreated.Value);
			ClassicAssert.IsNull(ConfiguredSessionRegister.GetSessionParams(new SessionId("sender", "target")));
			ClassicAssert.IsNotNull(ConfiguredSessionRegister.GetSessionParams(new SessionId("sender", "target", "idT")));
			LogAppender.Clear();
			FixSessionManager.DisposeAllSession();
		}

		private class ConfiguredSessionListenerAnonymousInnerClass3 : IConfiguredSessionListener
		{
			private readonly CreateAcceptorTest _outerInstance;

			private string _qualifierVal;
			private int _qualifierTag;
			private SyncFlag _acceptorCreated;

			public ConfiguredSessionListenerAnonymousInnerClass3(CreateAcceptorTest outerInstance, string qualifierVal, int qualifierTag, SyncFlag acceptorCreated)
			{
				_outerInstance = outerInstance;
				_qualifierVal = qualifierVal;
				_qualifierTag = qualifierTag;
				_acceptorCreated = acceptorCreated;
			}

			public void OnAddSession(SessionParameters sessionParameters)
			{
				var acceptorQualifierTag = sessionParameters.Configuration.GetPropertyAsInt(Config.LogonMessageSessionQualifierTag);

				var acceptorQualifierTagVal = sessionParameters.OutgoingLoginMessage.GetTagValueAsString(acceptorQualifierTag);

				//check that it's requested session
				if (sessionParameters.SenderCompId.Equals("sender")
						&& sessionParameters.TargetCompId.Equals("target")
						&& sessionParameters.SessionQualifier.Equals(_qualifierVal)
						&& acceptorQualifierTag == _qualifierTag
						&& acceptorQualifierTagVal.Equals(_qualifierVal))
				{
					//session found
					_outerInstance.ClassicAssertValidSession(_outerInstance._createAcceptor, sessionParameters);
					_acceptorCreated.Value = true;
				}
			}

			public void OnRemoveSession(SessionParameters @params)
			{
			}
		}

		private void ClassicAssertValidSession(CreateAcceptor exceptedSessionData, SessionParameters actualSessionParameters)
		{
			ClassicAssert.AreEqual(exceptedSessionData.SenderCompID, actualSessionParameters.SenderCompId);
			ClassicAssert.AreEqual(exceptedSessionData.TargetCompID, actualSessionParameters.TargetCompId);
			ClassicAssert.AreEqual(ForceSeqNumReset.OneTime, actualSessionParameters.ForceSeqNumReset);

			var configurationAdaptor = new ConfigurationAdapter(actualSessionParameters.Configuration);
			ClassicAssert.IsTrue(configurationAdaptor.IsAutoSwitchToBackupConnectionEnabled);
			ClassicAssert.IsTrue(configurationAdaptor.IsCyclicSwitchBackupConnectionEnabled);
			ClassicAssert.IsTrue(configurationAdaptor.IsEnableMessageRejecting);
			ClassicAssert.IsTrue(
				configurationAdaptor.StorageFactoryClass.Contains("Memory"),
				configurationAdaptor.StorageFactoryClass);
		}

		private void FillValidRequest(CreateAcceptor createAcceptor)
		{
			createAcceptor.TargetCompID = "target";
			createAcceptor.SenderCompID = "sender";
			createAcceptor.Version = "FIX44";

			createAcceptor.ExtraSessionParams = new ExtraSessionParams
			{
				CyclicSwitchBackupConnection = true,
				EnableAutoSwitchToBackupConnection = true,
				ForceSeqNumReset = FixAntenna.Fixicc.Message.ForceSeqNumReset.ON,
				EnableMessageRejecting = true,
				StorageType = StorageType.TRANSIENT
			};
		}
	}
}
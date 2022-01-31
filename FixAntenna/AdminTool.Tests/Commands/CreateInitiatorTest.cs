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

using System.IO;
using System.Linq;
using System.Threading;
using Epam.FixAntenna.AdminTool.Tests.Util;
using Epam.FixAntenna.Fixicc.Message;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework;
using ForceSeqNumReset = Epam.FixAntenna.NetCore.FixEngine.ForceSeqNumReset;

namespace Epam.FixAntenna.AdminTool.Tests.Commands
{
	/// <summary>
	/// Junit test class for CreateInitiator command
	/// </summary>
	internal class CreateInitiatorTest : AdminToolHelper
	{
		private bool InstanceFieldsInitialized = false;

		public CreateInitiatorTest()
		{
			if (!InstanceFieldsInitialized)
			{
				InitializeInstanceFields();
				InstanceFieldsInitialized = true;
			}
		}

		private void InitializeInstanceFields()
		{
			_log = LogFactory.GetLog(GetType());
		}

		private ILog _log;
		private const int REMOTE_PORT = 33333;
		private FixServer _server;

		private CreateInitiator _createInitiator;

		[SetUp]
		public override void Before()
		{
			base.Before();
			_server = new FixServer();
			_server.SetPort(REMOTE_PORT);
			_server.Start();

			_server.SetListener(new ResetSeqServerListener(this));

			_createInitiator = new CreateInitiator();
			RequestID = GetNextRequest();
			_createInitiator.RequestID = RequestID;

			FixSession = FindAdminSession();
		}

		private class ResetSeqServerListener : IFixServerListener
		{
			private CreateInitiatorTest _test;
			public ResetSeqServerListener(CreateInitiatorTest test)
			{
				_test = test;
			}

			public void NewFixSession(IFixSession session)
			{
				try
				{
					session.ResetSequenceNumbers();
					session.Connect();
				}
				catch (IOException)
				{
					session.Disconnect("disconnect");
				}
			}
		}


		[TearDown]
		public override void After()
		{
			base.After();
			_server.Stop();
			LogAppender.Clear();
		}

		[Test]
		public void TestCreateInitiatorWithoutRequestID()
		{
			_createInitiator.RequestID = null;
			var response = GetReponse(_createInitiator);
			Assert.AreEqual(ResultCode.OperationInvalidArgument.Code, response.ResultCode);
		}

		[Test]
		public void TestInvalidSenderTarget()
		{
			var response = GetReponse(_createInitiator);
			Assert.AreEqual(ResultCode.OperationInvalidArgument.Code, response.ResultCode);
		}

		[Test]
		public void TestInvalidVersion()
		{
			_createInitiator.TargetCompID = "target";
			_createInitiator.SenderCompID = "sender";

			var response = GetReponse(_createInitiator);
			Assert.AreEqual(ResultCode.OperationInvalidArgument.Code, response.ResultCode);
		}

		[Test]
		public void TestInvalidHostPort()
		{
			_createInitiator.TargetCompID = "target";
			_createInitiator.SenderCompID = "sender";
			_createInitiator.Version = "FIX44";
			_createInitiator.RemotePort = -1;
			_createInitiator.RemoteHost = "r";

			var response = GetReponse(_createInitiator);
			Assert.AreEqual(ResultCode.OperationInvalidArgument.Code, response.ResultCode);
		}

		[Test]
		public void TestEncryptedMethod()
		{
			CheckValidEncryptedMethod(EncryptMethod.NONE);

			CheckInvalidEncryptedMethod(EncryptMethod.DES);
			CheckInvalidEncryptedMethod(EncryptMethod.PGP_DES_MD5);
			CheckInvalidEncryptedMethod(EncryptMethod.PKCS);
			CheckInvalidEncryptedMethod(EncryptMethod.PKCS_DES);
			CheckInvalidEncryptedMethod(EncryptMethod.PGP_DES);
			CheckInvalidEncryptedMethod(EncryptMethod.PEM_DES_MD5);

			LogAppender.Clear();
		}

		private void CheckInvalidEncryptedMethod(EncryptMethod method)
		{
			FillValidRequest(_createInitiator);
			_createInitiator.ExtraSessionParams.EncryptMethod = method;

			var response = GetReponse(_createInitiator);
			Assert.AreEqual(ResultCode.OperationInvalidArgument.Code, response.ResultCode, method + " doesn't supported.");
			Assert.AreEqual("Failed to execute `CreateInitiator` command: Unsupported " + "encryption method for session: " + method + ". Please choose NONE.", response.Description, method + " doesn't supported.");
			FixSessionManager.DisposeAllSession();
			_createInitiator.ExtraSessionParams.EncryptMethod = null;
		}

		private void CheckValidEncryptedMethod(EncryptMethod method)
		{
			FillValidRequest(_createInitiator);
			_createInitiator.ExtraSessionParams.EncryptMethod = method;

			var response = GetReponse(_createInitiator);
			Assert.AreEqual(ResultCode.OperationSuccess.Code, response.ResultCode, method + " should be supported.");
			Assert.AreEqual("CreateInitiator completed.", response.Description, method + " should be supported.");

			//wait till created session connected (it will be easier to dispose it)
			using (var countDownLatch = new CountdownEvent(1))
			{
				FixSessionManager.Instance.SessionListCopy.First(s => _createInitiator.SenderCompID.Equals(s.Parameters.SenderCompId) && _createInitiator.TargetCompID.Equals(s.Parameters.TargetCompId))?.SetFixSessionListener(new FIXSessionListenerAnonymousInnerClass(this, countDownLatch));

				countDownLatch.Wait(1000);
			}

			FixSessionManager.DisposeAllSession();
			_createInitiator.ExtraSessionParams.EncryptMethod = null;
		}

		private class FIXSessionListenerAnonymousInnerClass : IFixSessionListener
		{
			private readonly CreateInitiatorTest _outerInstance;
			private CountdownEvent _countDownLatch;

			public FIXSessionListenerAnonymousInnerClass(CreateInitiatorTest outerInstance, CountdownEvent countDownLatch)
			{
				_outerInstance = outerInstance;
				_countDownLatch = countDownLatch;
			}

			public void OnSessionStateChange(SessionState sessionState)
			{
				if (sessionState == SessionState.Connected)
				{
					_outerInstance._log.Debug("Created session connected");
					_countDownLatch.Signal();
				}
			}

			public void OnNewMessage(FixMessage message)
			{
			}
		}

		[Test]
		public void TestCreateInitiator()
		{
			FillValidRequest(_createInitiator);

			var created = new SyncFlag(false);
			FixSessionManager.Instance.RegisterSessionManagerListener(new FixSessionListListenerAnonymousInnerClass(this, created));

			var response = GetReponse(_createInitiator);
			Assert.AreEqual(ResultCode.OperationSuccess.Code, response.ResultCode);
			Assert.IsTrue(created.Value);

			//wait till created session connected (it will be easier to dispose it)
			using (var countDownLatch = new CountdownEvent(1))
			{
				FixSessionManager.Instance.SessionListCopy
					.First(s => _createInitiator.SenderCompID.Equals(s.Parameters.SenderCompId) && _createInitiator.TargetCompID.Equals(s.Parameters.TargetCompId))?.SetFixSessionListener(new FixSessionListenerAnonymousInnerClass2(this, countDownLatch));
				countDownLatch.Wait(1000);
			}
		}

		private class FixSessionListListenerAnonymousInnerClass : IFixSessionListListener
		{
			private readonly CreateInitiatorTest _outerInstance;
			private SyncFlag _created;

			public FixSessionListListenerAnonymousInnerClass(CreateInitiatorTest outerInstance, SyncFlag created)
			{
				_outerInstance = outerInstance;
				_created = created;
			}

			public void OnAddSession(IExtendedFixSession fixSession)
			{

				var sessionParameters = fixSession.Parameters;
				if (sessionParameters.SenderCompId.Equals("sender")
						&& sessionParameters.TargetCompId.Equals("target"))
				{
					_outerInstance.AssertValidSession(_outerInstance._createInitiator, sessionParameters);
					_created.Value = true;
				}
			}

			public void OnRemoveSession(IExtendedFixSession fixSession)
			{
			}
		}

		private class FixSessionListenerAnonymousInnerClass2 : IFixSessionListener
		{
			private readonly CreateInitiatorTest _outerInstance;
			private CountdownEvent _countDownLatch;

			public FixSessionListenerAnonymousInnerClass2(CreateInitiatorTest outerInstance, CountdownEvent countDownLatch)
			{
				_outerInstance = outerInstance;
				_countDownLatch = countDownLatch;
			}

			public void OnSessionStateChange(SessionState sessionState)
			{
				if (sessionState == SessionState.Connected)
				{
					_outerInstance._log.Debug("Created session connected");
					_countDownLatch.Signal();
				}
			}

			public void OnNewMessage(FixMessage message)
			{
			}
		}

		[Test]
		public void TestCreateInitiatorWithQualifier()
		{
			FillValidRequest(_createInitiator);
			_createInitiator.SessionQualifier = "idT";

			var created = new SyncFlag(false);
			FixSessionManager.Instance.RegisterSessionManagerListener(new FixSessionListListenerAnonymousInnerClass2(this, created));

			var response = GetReponse(_createInitiator);
			Assert.AreEqual(ResultCode.OperationSuccess.Code, response.ResultCode);
			Assert.IsTrue(created.Value);

			//wait till created session connected (it will be easier to dispose it)
			using (var countDownLatch = new CountdownEvent(1))
			{
				FixSessionManager.Instance.SessionListCopy.First(s => _createInitiator.SenderCompID.Equals(s.Parameters.SenderCompId) && _createInitiator.TargetCompID.Equals(s.Parameters.TargetCompId))?.SetFixSessionListener(new FixSessionListenerAnonymousInnerClass3(this, countDownLatch));

				countDownLatch.Wait(1000);
			}
		}

		private class FixSessionListListenerAnonymousInnerClass2 : IFixSessionListListener
		{
			private readonly CreateInitiatorTest _outerInstance;
			private SyncFlag _created;

			public FixSessionListListenerAnonymousInnerClass2(CreateInitiatorTest outerInstance, SyncFlag created)
			{
				_outerInstance = outerInstance;
				_created = created;
			}

			public void OnAddSession(IExtendedFixSession fixSession)
			{

				var sessionParameters = fixSession.Parameters;
				//log.Info("onAddSession for " + sessionParameters);
				if ("sender".Equals(sessionParameters.SenderCompId)
						&& "target".Equals(sessionParameters.TargetCompId)
						&& "idT".Equals(sessionParameters.SessionQualifier))
				{
					_outerInstance.AssertValidSession(_outerInstance._createInitiator, sessionParameters);
					_created.Value = true;
				}
			}

			public void OnRemoveSession(IExtendedFixSession fixSession)
			{
			}
		}

		internal class FixSessionListenerAnonymousInnerClass3 : IFixSessionListener
		{
			private readonly CreateInitiatorTest _outerInstance;
			CountdownEvent _countDownLatch;

			public FixSessionListenerAnonymousInnerClass3(CreateInitiatorTest outerInstance, CountdownEvent countDownLatch)
			{
				_outerInstance = outerInstance;
				_countDownLatch = countDownLatch;
			}

			public void OnSessionStateChange(SessionState sessionState)
			{
				if (sessionState == SessionState.Connected)
				{
					_outerInstance._log.Debug("Created session connected");
					_countDownLatch.Signal();
				}
			}

			public void OnNewMessage(FixMessage message)
			{
			}
		}

		[Test]
		public void TestCreateInitiatorFromBackup()
		{
			var backupServer = new FixServer();
			var primaryServer = new FixServer();
			var primaryCountDownLatch = new CountdownEvent(1);
			var backupCountDownLatch = new CountdownEvent(1);
			try
			{
				var primaryPort = 5555;
				StartTestingServer(primaryServer, primaryPort, primaryCountDownLatch);
				var backupPort = 7777;
				StartTestingServer(backupServer, backupPort, backupCountDownLatch);

				FillValidRequest(_createInitiator);
				_createInitiator.RemotePort = primaryPort;
				_createInitiator.Backup = new Backup();
				_createInitiator.Backup.RemotePort = backupPort;
				_createInitiator.Backup.RemoteHost = "localhost";
				_createInitiator.Backup.ActiveConnection = ActiveConnection.BACKUP;

				var response = GetReponse(_createInitiator);
				Assert.AreEqual(ResultCode.OperationSuccess.Code, response.ResultCode);
				Assert.IsTrue(backupCountDownLatch.Wait(1000));
				Assert.IsFalse(primaryCountDownLatch.Wait(1000));
			}
			finally
			{
				backupServer.Stop();
				primaryServer.Stop();
				primaryCountDownLatch.Dispose();
				backupCountDownLatch.Dispose();
			}
		}

		private void AssertValidSession(CreateInitiator exceptedSessionData, SessionParameters actualSessionParameters)
		{
			Assert.AreEqual(exceptedSessionData.SenderCompID, actualSessionParameters.SenderCompId);
			Assert.AreEqual(exceptedSessionData.TargetCompID, actualSessionParameters.TargetCompId);
			Assert.AreEqual(ForceSeqNumReset.OneTime, actualSessionParameters.ForceSeqNumReset);

			var configurationAdaptor = new ConfigurationAdapter(actualSessionParameters.Configuration);
			Assert.IsTrue(configurationAdaptor.IsAutoSwitchToBackupConnectionEnabled);
			Assert.IsTrue(configurationAdaptor.IsCyclicSwitchBackupConnectionEnabled);
			Assert.IsTrue(configurationAdaptor.IsEnableMessageRejecting);
			Assert.IsTrue(configurationAdaptor.StorageFactoryClass.Contains("Memory"), configurationAdaptor.StorageFactoryClass);
		}

		private void FillValidRequest(CreateInitiator createInitiator)
		{
			createInitiator.TargetCompID = "target";
			createInitiator.SenderCompID = "sender";
			createInitiator.Version = "FIX44";
			createInitiator.RemotePort = REMOTE_PORT;
			var extraSessionParams = new ExtraSessionParams();
			extraSessionParams.ReconnectMaxTries = 0;
			createInitiator.ExtraSessionParams = extraSessionParams;
			createInitiator.RemoteHost = "localhost";

			createInitiator.ExtraSessionParams.CyclicSwitchBackupConnection = true;
			createInitiator.ExtraSessionParams.EnableAutoSwitchToBackupConnection = true;
			createInitiator.ExtraSessionParams.ForceSeqNumReset = FixAntenna.Fixicc.Message.ForceSeqNumReset.ON;
			createInitiator.ExtraSessionParams.EnableMessageRejecting = true;
			createInitiator.ExtraSessionParams.StorageType = StorageType.TRANSIENT;
		}
		private void StartTestingServer(FixServer server, int port, CountdownEvent countDownLatch)
		{
			server.SetPort(port);
			server.Start();
			server.SetListener(new TestServerListener(countDownLatch));
		}

		private class TestServerListener : IFixServerListener
		{
			private CountdownEvent _latch;

			public TestServerListener(CountdownEvent latch)
			{
				_latch = latch;
			}

			public void NewFixSession(IFixSession session)
			{
				try
				{
					session.ResetSequenceNumbers();
					session.Connect();
					_latch.Signal();
				}
				catch (IOException)
				{
					session.Disconnect("disconnect");
				}
			}
		}
	}
}
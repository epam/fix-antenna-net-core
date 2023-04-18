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
using System.Reflection;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.Util
{
	[TestFixture]
	internal class SessionParametersBuilderTest
	{
		[SetUp]
		public void SetUp()
		{
		}

		[Test]
		public virtual void TestBuildSimilarFromXmlFile()
		{
			TestSimilarFromFile("FixEngine/Session/Util/sample_config_list.xml");
		}

		[Test]
		public virtual void TestBuildSimilarFromPropertiesFile()
		{
			TestSimilarFromFile("FixEngine/Session/Util/sample_config_list.properties");
		}

		[Test]
		public virtual void TestBuildNotSimilarFromXmlFile()
		{
			TestNotSimilarFromFile("FixEngine/Session/Util/sample_config_list.xml");
		}

		[Test]
		public virtual void TestBuildNotSimilarPropertiesFile()
		{
			TestNotSimilarFromFile("FixEngine/Session/Util/sample_config_list.properties");
		}

		[Test]
		public virtual void TestReadingParametersFromXmlFile()
		{
			TestParametersFromFile("FixEngine/Session/Util/sample_config_list.xml");
		}

		[Test]
		public virtual void TestReadingParametersFromPropertiesFile()
		{
			TestParametersFromFile("FixEngine/Session/Util/sample_config_list.properties");
		}

		[Test]
		public virtual void TestBuildParametersListFromXmlFile()
		{
			BuildParametersList("FixEngine/Session/Util/sample_config_list.xml");
		}

		[Test]
		public virtual void TestBuildParametersListFromPropertiesFile()
		{
			BuildParametersList("FixEngine/Session/Util/sample_config_list.properties");
		}

		[Test]
		public virtual void TestBuildAcceptorParameters()
		{
			var sessionParameters = SessionParametersBuilder.BuildAcceptorSessionParametersList("FixEngine/Session/Util/sample_config_sessiontypes.properties");
			Assert.AreEqual(2, sessionParameters.Count);
			Assert.IsNotNull(sessionParameters["acceptorSession"]);
			Assert.IsNotNull(sessionParameters["defaultSession"]);
		}

		[Test]
		public virtual void TestCaseInsensitivityWhenBuildingSessionParameters()
		{
			// Act
			var sessionParameters = SessionParametersBuilder.BuildInitiatorSessionParametersList("FixEngine/Session/Util/sample_config_case_insensitivity.properties");

			// Assert
			var parameters = sessionParameters["testSession"];
			Assert.IsNotNull(parameters);

			Assert.AreEqual(12345, parameters.Port);
			Assert.AreEqual(31, parameters.HeartbeatInterval);
			Assert.AreEqual("EchoServer", parameters.TargetCompId);
			Assert.AreEqual("ConnectToGateway", parameters.SenderCompId);
			Assert.AreEqual("FIX.4.4", parameters.FixVersion.MessageVersion);
		}

		[Test]
		public virtual void TestUsingEnvironmentVariables()
		{
			// Arrange
			const string envVariable = "FANET_sessions__default__senderSubID";
			const string envVariableValue = "aValue";
			Environment.SetEnvironmentVariable(envVariable, envVariableValue, EnvironmentVariableTarget.Process);

			// Act
			var sessionParameters = SessionParametersBuilder.BuildInitiatorSessionParametersList("FixEngine/Session/Util/sample_config_case_insensitivity.properties");

			// Assert
			try
			{
				var parameters = sessionParameters["testSession"];
				Assert.IsNotNull(parameters);
				Assert.AreEqual(envVariableValue, parameters.SenderSubId);
			}
			finally
			{
				Environment.SetEnvironmentVariable(envVariable, null, EnvironmentVariableTarget.Process);
			}
		}

		[Test]
		public virtual void TestEnvironmentVariablesPriorityWhenNoDefaultValueSet()
		{
			// Arrange
			const string envVariableGlobal = "FANET_forceSeqNumReset";
			const string envVariableGlobalValue = "Always";
			Environment.SetEnvironmentVariable(envVariableGlobal, envVariableGlobalValue, EnvironmentVariableTarget.Process);

			// Arrange
			const string envVariableSessionSpecific = "FANET_sessions__testSession__forceSeqNumReset";
			const string envVariableSessionSpecificValue = "OneTime";
			Environment.SetEnvironmentVariable(envVariableSessionSpecific, envVariableSessionSpecificValue, EnvironmentVariableTarget.Process);

			// Act
			var sessionParameters = SessionParametersBuilder.BuildInitiatorSessionParametersList("FixEngine/Session/Util/sample_config_env_var_priority.properties");

			// Assert
			try
			{
				var testSessionParameters = sessionParameters["testSession"];
				Assert.IsNotNull(testSessionParameters);
				Assert.AreEqual(envVariableSessionSpecificValue, testSessionParameters.Configuration.GetProperty("forceSeqNumReset"));

				var secondSessionParameters = sessionParameters["secondSession"];
				Assert.IsNotNull(secondSessionParameters);
				Assert.AreEqual(envVariableGlobalValue, secondSessionParameters.Configuration.GetProperty("forceSeqNumReset"));
			}
			finally
			{
				Environment.SetEnvironmentVariable(envVariableGlobal, null, EnvironmentVariableTarget.Process);
				Environment.SetEnvironmentVariable(envVariableSessionSpecific, null, EnvironmentVariableTarget.Process);
			}
		}

		[Test]
		public virtual void TestEnvironmentVariablesPriorityWhenDefaultValueSet()
		{
			// Arrange
			const string envVariableSessionDefault = "FANET_sessions__default__forceSeqNumReset";
			const string envVariableSessionDefaultValue = "OneTime";
			Environment.SetEnvironmentVariable(envVariableSessionDefault, envVariableSessionDefaultValue, EnvironmentVariableTarget.Process);

			// Arrange
			const string envVariableSessionSpecific = "FANET_sessions__testSession__forceSeqNumReset";
			const string envVariableSessionSpecificValue = "OneTime";
			Environment.SetEnvironmentVariable(envVariableSessionSpecific, envVariableSessionSpecificValue, EnvironmentVariableTarget.Process);

			// Act
			var sessionParameters = SessionParametersBuilder.BuildInitiatorSessionParametersList("FixEngine/Session/Util/sample_config_env_var_priority.properties");

			// Assert
			try
			{
				var testSessionParameters = sessionParameters["testSession"];
				Assert.IsNotNull(testSessionParameters);
				Assert.AreEqual(envVariableSessionSpecificValue, testSessionParameters.Configuration.GetProperty("forceSeqNumReset"));

				var secondSessionParameters = sessionParameters["secondSession"];
				Assert.IsNotNull(secondSessionParameters);
				Assert.AreEqual(envVariableSessionSpecificValue, secondSessionParameters.Configuration.GetProperty("forceSeqNumReset"));
			}
			finally
			{
				Environment.SetEnvironmentVariable(envVariableSessionSpecific, null, EnvironmentVariableTarget.Process);
				Environment.SetEnvironmentVariable(envVariableSessionDefault, null, EnvironmentVariableTarget.Process);
			}
		}

		[Test]
		public virtual void TestEnvironmentVariablesPriorityWhenAllLevelsSet()
		{
			// Arrange
			const string envVariableGlobal = "FANET_forceSeqNumReset";
			const string envVariableGlobalValue = "Never";
			Environment.SetEnvironmentVariable(envVariableGlobal, envVariableGlobalValue, EnvironmentVariableTarget.Process);

			// Arrange
			const string envVariableSessionDefault = "FANET_sessions__default__forceSeqNumReset";
			const string envVariableSessionDefaultValue = "OneTime";
			Environment.SetEnvironmentVariable(envVariableSessionDefault, envVariableSessionDefaultValue, EnvironmentVariableTarget.Process);

			// Arrange
			const string envVariableSessionSpecific = "FANET_sessions__testSession__forceSeqNumReset";
			const string envVariableSessionSpecificValue = "OneTime";
			Environment.SetEnvironmentVariable(envVariableSessionSpecific, envVariableSessionSpecificValue, EnvironmentVariableTarget.Process);

			// Act
			var sessionParameters = SessionParametersBuilder.BuildInitiatorSessionParametersList("FixEngine/Session/Util/sample_config_env_var_priority.properties");

			// Assert
			try
			{
				var testSessionParameters = sessionParameters["testSession"];
				Assert.IsNotNull(testSessionParameters);
				Assert.AreEqual(envVariableSessionSpecificValue, testSessionParameters.Configuration.GetProperty("forceSeqNumReset"));

				var secondSessionParameters = sessionParameters["secondSession"];
				Assert.IsNotNull(secondSessionParameters);
				Assert.AreEqual(envVariableSessionSpecificValue, secondSessionParameters.Configuration.GetProperty("forceSeqNumReset"));
			}
			finally
			{
				Environment.SetEnvironmentVariable(envVariableGlobal, null, EnvironmentVariableTarget.Process);
				Environment.SetEnvironmentVariable(envVariableSessionSpecific, null, EnvironmentVariableTarget.Process);
				Environment.SetEnvironmentVariable(envVariableSessionDefault, null, EnvironmentVariableTarget.Process);
			}
		}

		[Test]
		public virtual void TestBuildInitiatorParameters()
		{
			var sessionParameters = SessionParametersBuilder.BuildInitiatorSessionParametersList("FixEngine/Session/Util/sample_config_sessiontypes.properties");
			Assert.AreEqual(2, sessionParameters.Count);
			Assert.IsNotNull(sessionParameters["initiatorSession"]);
			Assert.IsNotNull(sessionParameters["defaultSession"]);
		}

		[Test]
		public virtual void TestRedefiningConfiguration()
		{
			var conf = "FixEngine/Session/Util/sample_config_test.properties";
			var custom = SessionParametersBuilder.BuildSessionParameters(conf, "custom");
			Assert.AreEqual("500", custom.Configuration.GetProperty(Config.MaxMessageSize));

			var global = SessionParametersBuilder.BuildSessionParameters(conf, "global");
			Assert.IsTrue(global.Configuration.GetPropertyAsBytesLength(Config.MaxMessageSize) > 0);

		}

		[Test]
		public virtual void TestDuplicatedSessions()
		{
			var conf = "FixEngine/Session/Util/duplicate_session.properties";
			var session1 = SessionParametersBuilder.BuildSessionParameters(conf, "session1");
			var session2 = SessionParametersBuilder.BuildSessionParameters(conf, "session2");

			Assert.AreEqual("Epam.FixAntenna.NetCore.FixEngine.Storage.InMemoryStorageFactory", session1.Configuration.GetProperty(Config.StorageFactory));

			Assert.AreEqual("Epam.FixAntenna.NetCore.FixEngine.Storage.FilesystemStorageFactory", session2.Configuration.GetProperty(Config.StorageFactory));

		}

		[Test]
		public virtual void TestClonedSession()
		{
			var conf = "FixEngine/Session/Util/sample_config_test.properties";
			var session1 = SessionParametersBuilder.BuildSessionParameters(conf, "custom");
			var session2 = (SessionParameters)session1.Clone();

			Assert.AreEqual(session1.SenderCompId, session2.SenderCompId);
			Assert.AreEqual(session1.TargetCompId, session2.TargetCompId);

			session2.SenderCompId = "newCustomSenderCompID";
			session2.TargetCompId = "newCustomTargetCompID";

			Assert.That(session1.SenderCompId, Is.Not.EqualTo(session2.SenderCompId));
			Assert.That(session1.TargetCompId, Is.Not.EqualTo(session2.TargetCompId));
		}

		private void BuildParametersList(string file)
		{
			var sessionParameters = SessionParametersBuilder.BuildSessionParametersList(file);
			Assert.IsNotNull(sessionParameters["sessionID1"]);
			Assert.IsNotNull(sessionParameters["sessionID2"]);
			Assert.IsNotNull(sessionParameters["sessionID1"]);
		}

		private void TestSimilarFromFile(string file)
		{
			var p1 = BuildParameters(file);
			var p2 = BuildSimilarParameters(p1, file);
			Assert.IsTrue(p1.IsSimilar(p2));
			Assert.IsTrue(p2.IsSimilar(p1));
		}

		private void TestNotSimilarFromFile(string file)
		{
			var p1 = BuildParameters(file);
	//        assertNotSimilar(p1, "host", "otherHost");
			AssertNotSimilar(p1, nameof(SessionParameters.SenderCompId), "s1");
			AssertNotSimilar(p1, nameof(SessionParameters.SenderSubId), "ss1");
			AssertNotSimilar(p1, nameof(SessionParameters.SenderLocationId), "sl1");
			AssertNotSimilar(p1, nameof(SessionParameters.TargetCompId), "t1");
			AssertNotSimilar(p1, nameof(SessionParameters.TargetSubId), "ts1");
			AssertNotSimilar(p1, nameof(SessionParameters.TargetLocationId), "tl1");
			//assertNotSimilar(p1, "heartbeatInterval", 100);

			AssertNotSimilar(p1, nameof(SessionParameters.FixVersion), FixVersion.Fix50);
			AssertNotSimilar(p1, nameof(SessionParameters.AppVersion), FixVersion.Fix41);

	//        FixMessage fixMessage = getSimpleFixMessage(1, 1);
	//        assertNotSimilar(p1, "fixMessage", fixMessage);
	//
	//        Properties p = new Properties();
	//        p.SetProperty("b", "b");
	//        Configuration c = new Configuration(p);
	//        assertNotSimilar(p1, "configuration", c);
		}

		private FixMessage GetSimpleFixMessage(int tag, int val)
		{
			var message = new FixMessage();
			message.AddTag(tag, val.ToString());
			return message;
		}

		private void AssertNotSimilar(SessionParameters p, string pName, object pVal)
		{
			var p2 = BuildNotSimilarParameters(p, "FixEngine/Session/Util/sample_config_list.xml", pName, pVal);
			Assert.IsFalse(p.IsSimilar(p2), "Session parameters " + pName + " is different and object should not be similar");
			var p3 = BuildNotSimilarParameters(p, "FixEngine/Session/Util/sample_config_list.properties", pName, pVal);
			Assert.IsFalse(p.IsSimilar(p3), "Session parameters " + pName + " is different and object should not be similar");
		}

		private SessionParameters BuildSimilarParameters(SessionParameters p1, string file)
		{
			var p = BuildParameters(file);
			p.Port = 4321;
			var outMessage = GetSimpleFixMessage(2, 2);
			p.IncomingLoginMessage = outMessage;
			var inMessage = GetSimpleFixMessage(3, 3);
			p.IncomingLoginMessage = inMessage;
			p.LastSeqNumResetTimestamp = 100;
			return p;
		}

		private SessionParameters BuildNotSimilarParameters(SessionParameters p, string file, string pName, object pVal)
		{
			var p2 = BuildSimilarParameters(p, file);
			var propInfo = typeof(SessionParameters).GetProperty(pName, BindingFlags.Instance | BindingFlags.Public);
			propInfo?.SetValue(p2, pVal);
			return p2;
		}

		private SessionParameters BuildParameters(string file)
		{
			var sessionParameters = SessionParametersBuilder.BuildSessionParameters(file, "sessionID1");
			return sessionParameters;
		}

		public virtual void TestParametersFromFile(string file)
		{
			var sessionParameters = SessionParametersBuilder.BuildSessionParameters(file, "sessionID1");
			Assert.IsTrue(sessionParameters != null);
			Assert.IsTrue("sessionID1".Equals(sessionParameters.SessionId.ToString()));
			Assert.IsTrue("SenderCompID".Equals(sessionParameters.SenderCompId));
			Assert.IsTrue("SenderSubID".Equals(sessionParameters.SenderSubId));
			Assert.IsTrue("TargetCompID".Equals(sessionParameters.TargetCompId));
			Assert.IsTrue("TargetSubID".Equals(sessionParameters.TargetSubId));
			Assert.IsTrue("SenderLocationID".Equals(sessionParameters.SenderLocationId));
			Assert.IsTrue("TargetLocationID".Equals(sessionParameters.TargetLocationId));
			Assert.IsTrue("localhost".Equals(sessionParameters.Host));
			Assert.IsTrue(sessionParameters.Port == 1234);

			var sessionParameters2 = SessionParametersBuilder.BuildSessionParameters(file, "sessionID2");
			Assert.IsTrue(sessionParameters2 != null);
			Assert.IsTrue("sessionID2".Equals(sessionParameters2.SessionId.ToString()));
			Assert.IsTrue("SenderCompID2".Equals(sessionParameters2.SenderCompId));
			Assert.IsTrue("SenderSubID2".Equals(sessionParameters2.SenderSubId));
			Assert.IsTrue("TargetCompID2".Equals(sessionParameters2.TargetCompId));
			Assert.IsTrue("TargetSubID2".Equals(sessionParameters2.TargetSubId));
			Assert.IsTrue("SenderLocationID2".Equals(sessionParameters2.SenderLocationId));
			Assert.IsTrue("TargetLocationID2".Equals(sessionParameters2.TargetLocationId));
			Assert.IsTrue("localhost2".Equals(sessionParameters2.Host));
			Assert.IsTrue(sessionParameters2.Port == 1234);
	//        <appVersion>6</appVersion>
			Assert.IsTrue(sessionParameters2.AppVersion == FixVersion.Fix50Sp1);
	//        <fixVersion>FIXT.1.1</fixVersion>
			Assert.IsTrue(sessionParameters2.FixVersion.CompareTo(FixVersion.Fixt11) == 0);
	//        <incomingSequenceNumber>1</incomingSequenceNumber>
			Assert.IsTrue(sessionParameters2.IncomingSequenceNumber == 1);
	//        <outgoingSequenceNumber>1</outgoingSequenceNumber>
			Assert.IsTrue(sessionParameters2.OutgoingSequenceNumber == 1);
	//        <processedIncomingSequenceNumber>1</processedIncomingSequenceNumber>
			Assert.IsTrue(sessionParameters2.IncomingSequenceNumber == 1);
	//        <heartbeatInterval>30</heartbeatInterval>
			Assert.IsTrue(sessionParameters2.HeartbeatInterval == 30);
	//        <lastSeqNumResetTimestamp>1</lastSeqNumResetTimestamp>
			Assert.IsTrue(sessionParameters2.LastSeqNumResetTimestamp == 1);
	//        <fixMessage></fixMessage>
			Assert.IsTrue(sessionParameters2.UserDefinedFields.Length == 0);
	//        <incomingLoginFixMessage></incomingLoginFixMessage>
			Assert.IsTrue(sessionParameters2.IncomingLoginMessage.Length == 0);
	//        <outgoingLoginFixMessage></outgoingLoginFixMessage>
			Assert.IsTrue(sessionParameters2.OutgoingLoginMessage.Length == 2);
	/*        <autostart>
	            <acceptor>
	                <commands>
	                    <package></package>
	                </commands>
	                <admin>
	                    <login>admin</login>
	                    <password>admin</password>
	                    <ip>*</ip>
	                    <fixServerListener>FixAntenna.AdminTool.AdminTool</fixServerListener>
	                    <storageType>Transient</storageType>
	                </admin>
	                <admin1>
	                    <login>admin1</login>
	                    <password>admin1</password>
	                    <ip>*</ip>
	                    <fixServerListener>FixAntenna.AdminTool.AdminTool</fixServerListener>
	                </admin1>
	            </acceptor>
	        </autostart>*/
			Assert.IsTrue("admin".Equals(sessionParameters2.Configuration.GetProperty("autostart.acceptor.admin.login")));
			Assert.IsTrue("admin".Equals(sessionParameters2.Configuration.GetProperty("autostart.acceptor.admin.password")));
			Assert.IsTrue("admin1".Equals(sessionParameters2.Configuration.GetProperty("autostart.acceptor.admin1.login")));
			Assert.IsTrue("admin1".Equals(sessionParameters2.Configuration.GetProperty("autostart.acceptor.admin1.password")));
	//        <rawTags>96, 91, 213, 349, 351, 353, 355, 357, 359, 361, 363, 365, 446, 619, 622</rawTags>
			Assert.IsTrue(sessionParameters2.Configuration.GetProperty(Config.RawTags).Contains("353"));
	//        <resendRequestNumberOfMessagesLimit>0</resendRequestNumberOfMessagesLimit>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsInt(Config.ResendRequestNumberOfMessagesLimit, -1) == 0);
	//        <maxRequestResendInBlock>10</maxRequestResendInBlock>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsInt(Config.MaxRequestResendInBlock, -1) == 10);
	//        <maxDelayToSendAfterLogon>50</maxDelayToSendAfterLogon>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsInt(Config.MaxDelayToSendAfterLogon, -1) == 50);
	//        <resetOnSwitchToBackup>false</resetOnSwitchToBackup>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsBoolean(Config.ResetOnSwitchToBackup, true) == false);
	//        <resetOnSwitchToPrimary>false</resetOnSwitchToPrimary>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsBoolean(Config.ResetOnSwitchToPrimary, true) == false);
	//        <cyclicSwitchBackupConnection>true</cyclicSwitchBackupConnection>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsBoolean(Config.CyclicSwitchBackupConnection, false) == true);
	//        <enableAutoSwitchToBackupConnection>true</enableAutoSwitchToBackupConnection>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsBoolean(Config.EnableAutoSwitchToBackupConnection, false) == true);
	//        <forcedLogoffTimeout>2</forcedLogoffTimeout>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsInt(Config.ForcedLogoffTimeout, -1) == 2);
	//        <autoreconnectDelayInMs>1000</autoreconnectDelayInMs>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsInt(Config.AutoreconnectDelayInMs, -1) == 1000);
	//        <autoreconnectAttempts>-1</autoreconnectAttempts>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsInt(Config.AutoreconnectAttempts, 0) == -1);
	//        <maxMessageSize>1048576</maxMessageSize>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsBytesLength(Config.MaxMessageSize, -1) == 1048576);
	//        <sendRejectIfApplicationIsNotAvailable>true</sendRejectIfApplicationIsNotAvailable>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsBoolean(Config.SendRejectIfApplicationIsNotAvailable, false) == true);
	//        <inMemoryQueue>false</inMemoryQueue>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsBoolean(Config.InMemoryQueue, true) == false);
	//        <loginWaitTimeout>5000</loginWaitTimeout>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsInt(Config.LoginWaitTimeout, -1) == 5000);
	//        <heartbeatReasonableTransmissionTime>200</heartbeatReasonableTransmissionTime>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsInt(Config.HbtReasonableTransmissionTime, -1) == 200);
	//        <includeLastProcessed>false</includeLastProcessed>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsBoolean(Config.IncludeLastProcessed, true) == false);
	//        <queueThresholdSize>0</queueThresholdSize>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsInt(Config.QueueThresholdSize, -1) == 0);
	//        <maxMessagesToSendInBatch>10</maxMessagesToSendInBatch>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsInt(Config.MaxMessagesToSendInBatch, -1) == 10);
	//        <outgoingStorageIndexed>true</outgoingStorageIndexed>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsBoolean(Config.OutgoingStorageIndexed, false) == true);
	//        <enableMessageRejecting>false</enableMessageRejecting>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsBoolean(Config.EnableMessageRejecting, true) == false);
	/*        <fa>
	            <home>.</home>
	        </fa>*/
			Assert.IsTrue(".".Equals(sessionParameters2.Configuration.GetProperty("fa.home")));
	//        <storageDirectory>${fa.home}/logs</storageDirectory>
			Assert.IsTrue(sessionParameters2.Configuration.GetProperty(Config.StorageDirectory, "").Contains("logs"));
	//        <storageCleanupMode>None</storageCleanupMode>
			Assert.IsTrue("None".Equals(sessionParameters2.Configuration.GetProperty(Config.StorageCleanupMode, "")));
	//        <storageBackupDir>${fa.home}/logs/backup</storageBackupDir>
			Assert.IsTrue(sessionParameters2.Configuration.GetProperty(Config.StorageBackupDir, "").Contains("logs"));
	//        <timestampsInLogs>true</timestampsInLogs>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsBoolean(Config.TimestampsInLogs, false) == true);
	//        <performResetSeqNumTime>false</performResetSeqNumTime>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsBoolean(Config.PerformResetSeqNumTime, true) == false);
	//        <resetSequenceTime>00:00:00</resetSequenceTime>
			Assert.IsTrue("00:00:00".Equals(sessionParameters2.Configuration.GetProperty(Config.ResetSequenceTime, "")));
	//        <resetSequenceTimeZone>UTC</resetSequenceTimeZone>
			Assert.IsTrue("UTC".Equals(sessionParameters2.Configuration.GetProperty(Config.ResetSequenceTimeZone, "")));
	//        <intraDaySeqNumReset>false</intraDaySeqNumReset>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsBoolean(Config.IntraDaySeqnumReset, true) == false);
	//        <incomingLogFile>{0}-{1}.in</incomingLogFile>
			Assert.IsTrue(sessionParameters2.Configuration.GetProperty(Config.IncomingLogFile, "").Contains("in"));
	//        <outgoingLogFile>{0}-{1}.out</outgoingLogFile>
			Assert.IsTrue(sessionParameters2.Configuration.GetProperty(Config.OutgoingLogFile, "").Contains("out"));
	//        <backupIncomingLogFile>{0}-{1}-{2}.in</backupIncomingLogFile>
			Assert.IsTrue(sessionParameters2.Configuration.GetProperty(Config.BackupIncomingLogFile, "").Contains("in"));
	//        <backupOutgoingLogFile>{0}-{1}-{2}.out</backupOutgoingLogFile>
			Assert.IsTrue(sessionParameters2.Configuration.GetProperty(Config.BackupOutgoingLogFile, "").Contains("out"));
	//        <sessionInfoFile>{0}-{1}.properties</sessionInfoFile>
			Assert.IsTrue(sessionParameters2.Configuration.GetProperty(Config.SessionInfoFile, "").Contains("properties"));
	//        <outgoingQueueFile>{0}-{1}.outq</outgoingQueueFile>
			Assert.IsTrue(sessionParameters2.Configuration.GetProperty(Config.OutgoingQueueFile, "").Contains("outq"));
	//        <enableSSL>false</enableSSL>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsBoolean(Config.RequireSsl, true) == false);
	//        <enableNagle>true</enableNagle>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsBoolean(Config.EnableNagle, false) == true);
	//        <checkSendingTimeAccuracy>true</checkSendingTimeAccuracy>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsBoolean(Config.CheckSendingTimeAccuracy, false) == true);
	//        <reasonableDelayInMs>120000</reasonableDelayInMs>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsInt(Config.Delay, -1) == 120000);
	//        <measurementAccuracyInMs>1</measurementAccuracyInMs>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsInt(Config.Accuracy, -1) == 1);
	//        <validation>true</validation>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsBoolean(Config.Validation, false) == true);
	//        <origSendingTimeChecking>true</origSendingTimeChecking>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsBoolean(Config.OrigSendingTimeChecking, false) == true);
	//        <wellformenessValidation>true</wellformenessValidation>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsBoolean(Config.WelformedValidator, false) == true);
	//        <allowedFieldsValidation>true</allowedFieldsValidation>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsBoolean(Config.AllowedFieldsValidation, false) == true);
	//        <requiredFieldsValidation>true</requiredFieldsValidation>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsBoolean(Config.RequiredFieldsValidation, false) == true);
	//        <fieldOrderValidation>true</fieldOrderValidation>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsBoolean(Config.FieldOrderValidation, false) == true);
	//        <duplicateFieldsValidation>true</duplicateFieldsValidation>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsBoolean(Config.DuplicateFieldsValidation, false) == true);
	//        <fieldTypeValidation>true</fieldTypeValidation>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsBoolean(Config.FieldTypeValidation, false) == true);
	//        <groupValidation>true</groupValidation>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsBoolean(Config.GroupValidation, false) == true);
	//        <conditionalValidation>true</conditionalValidation>
			Assert.IsTrue(sessionParameters2.Configuration.GetPropertyAsBoolean(Config.ConditionalValidation, false) == true);
	//        <encryptionConfig>${fa.home}/encryption/encryption.cfg</encryptionConfig>
			Assert.IsTrue(sessionParameters2.Configuration.GetProperty(Config.EncryptionCfgFile, "").Contains(".cfg"));
	//        <encryptionMode>None</encryptionMode>
			Assert.IsTrue("None".Equals(sessionParameters2.Configuration.GetProperty(Config.EncryptionMode, "")));
	//        <username>username</username>
			Assert.IsTrue("username".Equals(sessionParameters2.UserName));
	//        <password>password</password>
			Assert.IsTrue("password".Equals(sessionParameters2.Password));
	//        <maskedTags>554, 925</maskedTags>
			Assert.IsTrue("554, 925".Equals(sessionParameters2.Configuration.GetProperty(Config.MaskedTags)));
		}
	}
}
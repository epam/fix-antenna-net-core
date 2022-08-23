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
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text;

using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.Message;

using NUnit.Framework;

namespace Epam.FixAntenna.FixEngine
{
	[TestFixture]
	internal class SessionParametersTest
	{

		private SessionParameters _sessionParameters;
		private string _host = "localhost1";

		[SetUp]
		public void SetUp()
		{
			_sessionParameters = new SessionParameters();
			_sessionParameters.Host = _host;
			_sessionParameters.Port = 1000;
		}

		[Test]
		public void TestCloned()
		{
			var clonedParameters = (SessionParameters)_sessionParameters.Clone();
			Assert.IsTrue(clonedParameters.Equals(_sessionParameters));
			Assert.AreNotSame(clonedParameters, _sessionParameters);
		}

		[Test]
		public void TestSetGetForceSeqNumReset()
		{
			_sessionParameters.ForceSeqNumReset = ForceSeqNumReset.Always;
			Assert.AreEqual(ForceSeqNumReset.Always, _sessionParameters.ForceSeqNumReset);
		}

		[Test]
		public void TestSimilar()
		{
			var p1 = BuildParameters();
			var p2 = BuildSimilarParameters(p1);
			Assert.IsTrue(p1.IsSimilar(p2));
			Assert.IsTrue(p2.IsSimilar(p1));
		}

		[Test]
		public void TestSimilarNoAppVersion()
		{
			var p1 = BuildParameters();
			var p2 = BuildSimilarParameters(p1);
			p2.AppVersionContainer = (FixVersionContainer)null;

			Assert.IsFalse(p1.IsSimilar(p2));
			Assert.IsFalse(p2.IsSimilar(p1));
		}

		[Test]
		public void TestSimilarCustomSessionId()
		{
			var p1 = BuildParameters();
			p1.SetSessionId("sID");
			var p2 = BuildSimilarParameters(p1);
			p2.SetSessionId("sID");
			Assert.IsTrue(p1.IsSimilar(p2));
			Assert.IsTrue(p2.IsSimilar(p1));
		}

		[Test]
		public void TestSimilarWithCustomFixVersion()
		{
			var p1 = BuildParameters();
			p1.FixVersionContainer = new FixVersionContainer("NEW_ID", p1.FixVersion, null);
			var p2 = BuildSimilarParameters(p1);
			Assert.IsTrue(p1.IsSimilar(p2));
			Assert.IsTrue(p2.IsSimilar(p1));
		}

		[Test]
		public void TestNotSimilar()
		{
			var p1 = BuildParameters();

			AssertNotSimilar(p1, nameof(SessionParameters.SenderCompId), "s1");
			AssertNotSimilar(p1, nameof(SessionParameters.SenderSubId), "ss1");
			AssertNotSimilar(p1, nameof(SessionParameters.SenderLocationId), "sl1");
			AssertNotSimilar(p1, nameof(SessionParameters.TargetCompId), "t1");
			AssertNotSimilar(p1, nameof(SessionParameters.TargetSubId), "ts1");
			AssertNotSimilar(p1, nameof(SessionParameters.TargetLocationId), "tl1");
			AssertNotSimilar(p1, nameof(SessionParameters.SessionQualifier), "sq1");

			AssertNotSimilar(p1, nameof(SessionParameters.FixVersion), FixVersion.Fix50);
			AssertNotSimilar(p1, nameof(SessionParameters.AppVersion), FixVersion.Fix41);
		}

		private FixMessage GetSimpleFixMessage(int tag, int val)
		{
			var message = new FixMessage();
			message.AddTag(tag, val.ToString());
			return message;
		}

		private void AssertNotSimilar(SessionParameters p, string name, object value)
		{
			var p2 = BuildNotSimilarParameters(p, name, value);
			Assert.IsFalse(p.IsSimilar(p2), $"Session parameters {name} is different and object should not be similar");
		}

		private SessionParameters BuildSimilarParameters(SessionParameters p1)
		{
			var p = BuildParameters();
			p.Host = p1.Host + "A";
			p.Port = p1.Port + 1;
			var outMessage = GetSimpleFixMessage(2, 2);
			p.IncomingLoginMessage = outMessage;
			var inMessage = GetSimpleFixMessage(3, 3);
			p.IncomingLoginMessage = inMessage;
			p.LastSeqNumResetTimestamp = 100;
			p.HeartbeatInterval = p1.HeartbeatInterval + 10;
			return p;
		}

		private SessionParameters BuildNotSimilarParameters(SessionParameters p, string name, object pVal)
		{
			var p2 = BuildSimilarParameters(p);
			var propInfo = typeof(SessionParameters).GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
			propInfo?.SetValue(p2, pVal);
			return p2;
		}

		private SessionParameters BuildNotSimilarParametersUseSetterMethod(SessionParameters p, string methodName, object value)
		{
			var p2 = BuildSimilarParameters(p);
			var methodInfo = typeof(SessionParameters).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new []{value.GetType()}, null);
			methodInfo?.Invoke(p2, new[] { value });
			return p2;
		}

		private SessionParameters BuildParameters()
		{
			var p = new Properties();
			p.SetProperty("a", "a");
			var c = new Config(p.ToDictionary());

			var sessionParameters = new SessionParameters
			{
				Host = _host,
				Port = 1234,
				SenderCompId = "SenderCompID",
				SenderSubId = "SenderSubID",
				SenderLocationId = "SenderLocationID",
				TargetCompId = "TargetCompID",
				TargetSubId = "TargetSubID",
				TargetLocationId = "TargetLocationID",
				AppVersion = FixVersion.Fix44,
				FixVersion = FixVersion.Fixt11,
				IncomingSequenceNumber = 1,
				OutgoingSequenceNumber = 1,
				HeartbeatInterval = 30,
				UserDefinedFields = new FixMessage(),
				LastSeqNumResetTimestamp = 1,
				Configuration = c,
				IncomingLoginMessage = new FixMessage()
			};

			return sessionParameters;
		}

		[Test]
		public void InvalidSessionIdLength()
		{
			var sessionParameters = new SessionParameters();
			var id = new StringBuilder("");
			for (var i = 0; i < SessionParameters.MaxSessionIdLength + 1; i++)
			{
				id.Append("a");
			}
			Assert.Throws<ArgumentException>(() => sessionParameters.SetSessionId(id.ToString()));
		}

		[Test]
		public void ValidSessionIdLength()
		{
			var sessionParameters = new SessionParameters();
			var id = new StringBuilder("");
			for (var i = 0; i < SessionParameters.MaxSessionIdLength; i++)
			{
				id.Append("a");
			}
			sessionParameters.SetSessionId(id.ToString());
		}

		[Test]
		public void ValidSessionName()
		{
			var sessionParameters = new SessionParameters();
			var id = new StringBuilder("_a.B 1-");
			sessionParameters.SetSessionId(id.ToString());
		}

		[Test]
		public void InvalidSessionName()
		{
			var sessionParameters = new SessionParameters();
			var id = new StringBuilder("a+a");
			Assert.Throws<ArgumentException>(() => sessionParameters.SetSessionId(id.ToString()));
		}

		[Test]
		public virtual void SetNewFormatDestinationTest()
		{
			var properties = new Dictionary<string, string>();
			properties["socketConnectAddress_0"] = "127.0.0.1:1111";
			properties["socketConnectAddress_1"] = "127.0.0.1:2222";
			properties["socketConnectAddress_2"] = "127.0.0.1:3333";

			var sessionParameters = new SessionParameters();
			sessionParameters.SetDestinationsIfPresent(properties);

			var destinations = sessionParameters.Destinations;
			Assert.AreEqual(3, destinations.Count);
			Assert.IsTrue(destinations.Contains(new DnsEndPoint("127.0.0.1", 1111)));
			Assert.IsTrue(destinations.Contains(new DnsEndPoint("127.0.0.1", 2222)));
			Assert.IsTrue(destinations.Contains(new DnsEndPoint("127.0.0.1", 3333)));
		}

		/// <summary>
		/// If both formats are presened in properties, new format should be used,
		/// old format should  be skipped.
		/// </summary>
		[Test]
		public void SetBothFormatDestinationTest()
		{
			var properties = new Dictionary<string, string>();
			properties["socketConnectAddress_0"] = "127.0.0.1:1111";
			properties["socketConnectAddress_1"] = "127.0.0.1:2222";
			properties["socketConnectAddress_2"] = "127.0.0.1:3333";
			properties["backupHost"] = "127.0.0.1";
			properties["backupPort"] = "1111";

			var sessionParameters = new SessionParameters();
			sessionParameters.SetDestinationsIfPresent(properties);

			var destinations = sessionParameters.Destinations;
			Assert.AreEqual(3, destinations.Count);
			Assert.IsTrue(destinations.Contains(new DnsEndPoint("127.0.0.1", 1111)));
			Assert.IsTrue(destinations.Contains(new DnsEndPoint("127.0.0.1", 2222)));
			Assert.IsTrue(destinations.Contains(new DnsEndPoint("127.0.0.1", 3333)));
		}

		[Test]
		public void TestSessionParametersWithDefaultFixVersions()
		{
			var config = Config.GlobalConfiguration;
			var sessionParameters = new SessionParameters(config);

			sessionParameters.FixVersion = FixVersion.Fix40;
			sessionParameters.AppVersion = FixVersion.Fix40;
			Assert.AreEqual(FixVersion.Fix40, sessionParameters.FixVersionContainer.FixVersion);
			Assert.AreEqual(FixVersion.Fix40, sessionParameters.AppVersionContainer.FixVersion);

			sessionParameters.FixVersion = FixVersion.Fix41;
			sessionParameters.AppVersion = FixVersion.Fix41;
			Assert.AreEqual(FixVersion.Fix41, sessionParameters.FixVersionContainer.FixVersion);
			Assert.AreEqual(FixVersion.Fix41, sessionParameters.AppVersionContainer.FixVersion);

			sessionParameters.FixVersion = FixVersion.Fix42;
			sessionParameters.AppVersion = FixVersion.Fix42;
			Assert.AreEqual(FixVersion.Fix42, sessionParameters.FixVersionContainer.FixVersion);
			Assert.AreEqual(FixVersion.Fix42, sessionParameters.AppVersionContainer.FixVersion);

			sessionParameters.FixVersion = FixVersion.Fix43;
			sessionParameters.AppVersion = FixVersion.Fix43;
			Assert.AreEqual(FixVersion.Fix43, sessionParameters.FixVersionContainer.FixVersion);
			Assert.AreEqual(FixVersion.Fix43, sessionParameters.AppVersionContainer.FixVersion);

			sessionParameters.FixVersion = FixVersion.Fix44;
			sessionParameters.AppVersion = FixVersion.Fix44;
			Assert.AreEqual(FixVersion.Fix44, sessionParameters.FixVersionContainer.FixVersion);
			Assert.AreEqual(FixVersion.Fix44, sessionParameters.AppVersionContainer.FixVersion);

			sessionParameters.FixVersion = FixVersion.Fix50;
			sessionParameters.AppVersion = FixVersion.Fix50;
			Assert.AreEqual(FixVersion.Fix50, sessionParameters.FixVersionContainer.FixVersion);
			Assert.AreEqual(FixVersion.Fix50, sessionParameters.AppVersionContainer.FixVersion);

			sessionParameters.FixVersion = FixVersion.Fix50Sp1;
			sessionParameters.AppVersion = FixVersion.Fix50Sp1;
			Assert.AreEqual(FixVersion.Fix50Sp1, sessionParameters.FixVersionContainer.FixVersion);
			Assert.AreEqual(FixVersion.Fix50Sp1, sessionParameters.AppVersionContainer.FixVersion);

			sessionParameters.FixVersion = FixVersion.Fix50Sp2;
			sessionParameters.AppVersion = FixVersion.Fix50Sp2;
			Assert.AreEqual(FixVersion.Fix50Sp2, sessionParameters.FixVersionContainer.FixVersion);
			Assert.AreEqual(FixVersion.Fix50Sp2, sessionParameters.AppVersionContainer.FixVersion);

			sessionParameters.FixVersion = FixVersion.Fixt11;
			sessionParameters.AppVersion = FixVersion.Fixt11;
			Assert.AreEqual(FixVersion.Fixt11, sessionParameters.FixVersionContainer.FixVersion);
			Assert.AreEqual(FixVersion.Fixt11, sessionParameters.AppVersionContainer.FixVersion);
		}

		[Test]
		public void TestSessionParametersWithCustomFixVersions()
		{
			var props = new Properties();

			props.Put("customFixVersions", "FIX42Custom,FIX44Custom");

			props.Put("customFixVersion.FIX42Custom.fixVersion", "FIX.4.2");
			props.Put("customFixVersion.FIX42Custom.fileName", "fixdic42-custom.xml");

			props.Put("customFixVersion.FIX44Custom.fixVersion", "FIX.4.4");
			props.Put("customFixVersion.FIX44Custom.fileName", "fixdic44-custom.xml");

			var config = new Config(props.ToDictionary());
			var sessionParameters = new SessionParameters(config);

			sessionParameters.FixVersionFromString("FIX42Custom");
			sessionParameters.AppVersionFromString("FIX42Custom");

			//check that custom FIX version does\t overwrite default
			Assert.AreEqual(FixVersion.Fix42, sessionParameters.FixVersionContainer.FixVersion);
			Assert.AreEqual("fixdic42-custom.xml", sessionParameters.FixVersionContainer.DictionaryFile);
			Assert.IsNull(sessionParameters.FixVersionContainer.ExtensionFile);

			Assert.AreEqual(FixVersion.Fix42, sessionParameters.AppVersionContainer.FixVersion);
			Assert.AreEqual("fixdic42-custom.xml", sessionParameters.AppVersionContainer.DictionaryFile);
			Assert.IsNull(sessionParameters.AppVersionContainer.ExtensionFile);

			sessionParameters.FixVersionFromString("FIX44Custom");
			sessionParameters.AppVersionFromString("FIX44Custom");
			Assert.AreEqual(FixVersion.Fix44, sessionParameters.FixVersionContainer.FixVersion);
			Assert.AreEqual("fixdic44-custom.xml", sessionParameters.FixVersionContainer.DictionaryFile);
			Assert.IsNull(sessionParameters.FixVersionContainer.ExtensionFile);

			Assert.AreEqual(FixVersion.Fix44, sessionParameters.AppVersionContainer.FixVersion);
			Assert.AreEqual("fixdic44-custom.xml", sessionParameters.AppVersionContainer.DictionaryFile);
			Assert.IsNull(sessionParameters.AppVersionContainer.ExtensionFile);
		}

		[Test]
		public void TestSessionParametersWithCustomDictionariesForFixVersion()
		{
			var props = new Properties();

			props.Put("validation.FIX42.additionalDictionaryFileName", "fixdic42-custom.xml");
			props.Put("validation.FIX42.additionalDictionaryUpdate", "false");

			props.Put("validation.FIX44.additionalDictionaryFileName", "fixdic44-custom.xml");
			props.Put("validation.FIX44.additionalDictionaryUpdate", "true");

			var config = new Config(props.ToDictionary());
			var sessionParameters = new SessionParameters(config);

			sessionParameters.FixVersion = FixVersion.Fix42;
			sessionParameters.AppVersion = FixVersion.Fix42;

			Assert.AreEqual(FixVersion.Fix42, sessionParameters.FixVersionContainer.FixVersion);
			Assert.AreEqual("fixdic42-custom.xml", sessionParameters.FixVersionContainer.DictionaryFile);
			Assert.IsNull(sessionParameters.FixVersionContainer.ExtensionFile);

			Assert.AreEqual(FixVersion.Fix42, sessionParameters.AppVersionContainer.FixVersion);
			Assert.AreEqual("fixdic42-custom.xml", sessionParameters.AppVersionContainer.DictionaryFile);
			Assert.IsNull(sessionParameters.AppVersionContainer.ExtensionFile);

			sessionParameters.FixVersion = FixVersion.Fix44;
			sessionParameters.AppVersion = FixVersion.Fix44;

			Assert.AreEqual(FixVersion.Fix44, sessionParameters.FixVersionContainer.FixVersion);
			Assert.AreEqual("fixdic44.xml", sessionParameters.FixVersionContainer.DictionaryFile);
			Assert.AreEqual("fixdic44-custom.xml", sessionParameters.FixVersionContainer.ExtensionFile);

			Assert.AreEqual(FixVersion.Fix44, sessionParameters.AppVersionContainer.FixVersion);
			Assert.AreEqual("fixdic44.xml", sessionParameters.AppVersionContainer.DictionaryFile);
			Assert.AreEqual("fixdic44-custom.xml", sessionParameters.AppVersionContainer.ExtensionFile);
		}

		[Test]
		public void TestRegisteredConfiguredSessions()
		{
			var config = Config.GlobalConfiguration;
			var props = new Properties();

			props.Put("customFixVersions", "FIX42Custom,FIX44Custom");

			props.Put("customFixVersion.FIX42Custom.fixVersion", "FIX.4.2");
			props.Put("customFixVersion.FIX42Custom.fileName", "fixdic42-custom.xml");

			props.Put("customFixVersion.FIX44Custom.fixVersion", "FIX.4.4");
			props.Put("customFixVersion.FIX44Custom.fileName", "fixdic44-custom.xml");

			props.Put("sessionIDs", "S1-C1,S1-C2,S1-C3");

			props.Put("sessions.S1-C1.sessionType", "acceptor");
			props.Put("sessions.S1-C1.fixVersion", "FIX42Custom");
			props.Put("sessions.S1-C1.senderCompID", "S1");
			props.Put("sessions.S1-C1.targetCompID", "C1");
			props.Put("sessions.S1-C1.storageFactory", "com.epam.fixengine.storage.FilesystemStorageFactory");
			props.Put("sessions.S1-C1.preferredSendingMode", "sync_noqueue");

			props.Put("sessions.S1-C2.sessionType", "acceptor");
			props.Put("sessions.S1-C2.fixVersion", "FIX44Custom");
			props.Put("sessions.S1-C2.senderCompID", "S1");
			props.Put("sessions.S1-C2.targetCompID", "C2");
			props.Put("sessions.S1-C2.storageFactory", "com.epam.fixengine.storage.InMemoryStorageFactory");
			props.Put("sessions.S1-C2.preferredSendingMode", "sync");

			props.Put("sessions.S1-C3.sessionType", "acceptor");
			props.Put("sessions.S1-C3.fixVersion", "FIX.4.2");
			props.Put("sessions.S1-C3.senderCompID", "S1");
			props.Put("sessions.S1-C3.targetCompID", "C3");
			props.Put("sessions.S1-C3.storageFactory", "com.epam.fixengine.storage.InMemoryStorageFactory");
			props.Put("sessions.S1-C3.preferredSendingMode", "async");

			config.AddAllProperties(props.ToDictionary());

			var acceptorSessions = SessionParametersBuilder.BuildAcceptorSessionParametersList();

			var p = acceptorSessions["S1-C1"];
			Assert.IsNotNull(p);
			Assert.AreEqual(FixVersion.Fix42, p.FixVersionContainer.FixVersion);
			Assert.AreEqual("fixdic42-custom.xml", p.FixVersionContainer.DictionaryFile);
			Assert.IsNull(p.FixVersionContainer.ExtensionFile);
		
			p = acceptorSessions["S1-C2"];
			Assert.IsNotNull(p);
			Assert.AreEqual(FixVersion.Fix44, p.FixVersionContainer.FixVersion);
			Assert.AreEqual("fixdic44-custom.xml", p.FixVersionContainer.DictionaryFile);
			Assert.IsNull(p.FixVersionContainer.ExtensionFile);

			p = acceptorSessions["S1-C3"];
			Assert.IsNotNull(p);
			Assert.AreEqual(FixVersion.Fix42, p.FixVersionContainer.FixVersion);
			Assert.AreEqual("fixdic42.xml", p.FixVersionContainer.DictionaryFile);
			Assert.IsNull(p.FixVersionContainer.ExtensionFile);
		}

		[Test]
		public void TestSetIncorrectLastSeqNumReset(){
			var props = new Properties();
			props.Put("lastSeqNumResetTimestamp", "test");

			var sessionParameters = new SessionParameters();
			Assert.Throws<ArgumentException>(() => sessionParameters.FromProperties(props.ToDictionary()));
		}
	}
}

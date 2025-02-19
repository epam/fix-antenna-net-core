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
using System.Globalization;
using System.IO;
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Session.Impl;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.Impl
{
	[TestFixture]
	internal class CmeSecureLogonStrategyTest
	{
		private string _fileName;
		[SetUp]
		public void CreateTempFile()
		{
			_fileName = Path.GetTempFileName();
			ClassicAssert.That(_fileName, Does.Exist);
		}

		[TearDown]
		public void DeleteTempFile()
		{
			File.Delete(_fileName);
			ClassicAssert.That(_fileName, Does.Not.Exist);
		}

		[Test]
		public virtual void EmptyFileShouldBeParsedWithException()
		{
			var fileContent = "";
			File.WriteAllBytes(_fileName, fileContent.AsByteArray());
			ClassicAssert.Throws<IOException>(() => CmeSecureLogonStrategy.ParseKeys(_fileName));
		}

		[Test]
		public virtual void FileOnlyWithHeaderShouldBeParsedCorrectly()
		{
			var fileContent =
					"Session ID           Access ID                      Secret Key                         "
				+ "                Creation Date        Expiration Date      Environment          \n" + "\n";
			File.WriteAllBytes(_fileName, fileContent.AsByteArray());
			var keys = CmeSecureLogonStrategy.ParseKeys(_fileName);
			ClassicAssert.IsTrue(keys.Count == 0);
		}

		[Test]
		public virtual void OneKeyLineShouldBeParsedCorrectly()
		{
			var fileContent =
				"Session ID           Access ID                      Secret Key                         " +
				"                Creation Date        Expiration Date      Environment          \n" + "\n" +
				"U89                  dipfNf8kZOOuk7Wz0ZUn           gyJYeacu9T--a1tak4i5S-pBXxwCNK_wUqp9fh5usJ8     " +
				"   2018-01-23           2019-01-23           NEWRELEASE           \n" + "\n";
			File.WriteAllBytes(_fileName, fileContent.AsByteArray());
			var keysMap = CmeSecureLogonStrategy.ParseKeys(_fileName);
			ClassicAssert.IsNotNull(keysMap);
			var u89Parameters = keysMap["U89"];
			ClassicAssert.IsNotNull(u89Parameters);
			ClassicAssert.AreEqual("dipfNf8kZOOuk7Wz0ZUn", u89Parameters[CmeSecureLogonStrategy.AccessIdParamName]);
			ClassicAssert.AreEqual("gyJYeacu9T--a1tak4i5S-pBXxwCNK_wUqp9fh5usJ8", u89Parameters[CmeSecureLogonStrategy.SecretKeyParamName]);
			ClassicAssert.AreEqual("2018-01-23", u89Parameters[CmeSecureLogonStrategy.CreationDateParamName]);
			ClassicAssert.AreEqual("2019-01-23", u89Parameters[CmeSecureLogonStrategy.ExpirationDateParamName]);
			ClassicAssert.AreEqual("NEWRELEASE", u89Parameters[CmeSecureLogonStrategy.EnvironmentParamName]);
		}

		[Test]
		public virtual void ThereShouldBeTakenKeyWithTheHighestExpirationDateIfThereAreSeveralKeys()
		{
			var fileContent =
				"Session ID           Access ID                      Secret Key                         " +
				"                Creation Date        Expiration Date      Environment          \n" + "\n" +
				"U89                  1AID                           1SK                                " +
				"                2018-01-23           2018-06-23           NEWRELEASE           \n" +
				"U89                  2AID                           2SK                                " +
				"                2018-02-28           2019-10-23           NEWRELEASE           \n" +
				"U89                  3AID                           3SK                                " +
				"                2019-01-20           2019-05-20           NEWRELEASE           \n" + "\n";
			File.WriteAllBytes(_fileName, fileContent.AsByteArray());
			var keysMap = CmeSecureLogonStrategy.ParseKeys(_fileName);
			ClassicAssert.IsNotNull(keysMap);
			var u89Parameters = keysMap["U89"];
			ClassicAssert.IsNotNull(u89Parameters);
			ClassicAssert.AreEqual("2AID", u89Parameters[CmeSecureLogonStrategy.AccessIdParamName]);
			ClassicAssert.AreEqual("2SK", u89Parameters[CmeSecureLogonStrategy.SecretKeyParamName]);
			ClassicAssert.AreEqual("2018-02-28", u89Parameters[CmeSecureLogonStrategy.CreationDateParamName]);
			ClassicAssert.AreEqual("2019-10-23", u89Parameters[CmeSecureLogonStrategy.ExpirationDateParamName]);
			ClassicAssert.AreEqual("NEWRELEASE", u89Parameters[CmeSecureLogonStrategy.EnvironmentParamName]);
		}

		[Test]
		public virtual void TwoKeysWithNoSpaceAtTheEndShouldBeParsedCorrectly()
		{
			var fileContent =
				"Session ID           Access ID                      Secret Key                         " +
				"                Creation Date        Expiration Date      Environment          \n" +
				"U89                  dipfNf8kZOOuk7Wz0ZUn           gyJYeacu9T--a1tak4i5S-pBXxwCNK_wUqp9fh5usJ8     " +
				"   2018-01-23           2059-01-23           NEWRELEASE \n" +
				"K92                  T57MHVRf4CCBnRHCL5f7           BWpJWioyXPdafvJb93EbwFed1EyxE24DzCw6k6ATOHk     " +
				"   2018-02-16           2059-02-16           NEWRELEASE\n";
			File.WriteAllBytes(_fileName, fileContent.AsByteArray());
			var keysMap = CmeSecureLogonStrategy.ParseKeys(_fileName);
			ClassicAssert.IsNotNull(keysMap);
			var u89Parameters = keysMap["K92"];
			ClassicAssert.IsNotNull(u89Parameters);
		}

		[Test]
		public virtual void TwoKeyLineShouldBeParsedCorrectly()
		{
			var fileContent =
				"Session ID           Access ID                      Secret Key                         " +
				"                Creation Date        Expiration Date      Environment          \n" + "\n" +
				"U89                  dipfNf8kZOOuk7Wz0ZUn           gyJYeacu9T--a1tak4i5S-pBXxwCNK_wUqp9fh5usJ8     " +
				"   2018-01-23           2019-01-23           NEWRELEASE           \n" + "\n" +
				"U90                  1                              2                                               " +
				"   3                    4                    5           \n" + "\n";
			File.WriteAllBytes(_fileName, fileContent.AsByteArray());
			var keysMap = CmeSecureLogonStrategy.ParseKeys(_fileName);
			ClassicAssert.IsNotNull(keysMap);
			var u89Parameters = keysMap["U89"];
			ClassicAssert.IsNotNull(u89Parameters);
			ClassicAssert.AreEqual("dipfNf8kZOOuk7Wz0ZUn", u89Parameters[CmeSecureLogonStrategy.AccessIdParamName]);
			ClassicAssert.AreEqual("gyJYeacu9T--a1tak4i5S-pBXxwCNK_wUqp9fh5usJ8", u89Parameters[CmeSecureLogonStrategy.SecretKeyParamName]);
			ClassicAssert.AreEqual("2018-01-23", u89Parameters[CmeSecureLogonStrategy.CreationDateParamName]);
			ClassicAssert.AreEqual("2019-01-23", u89Parameters[CmeSecureLogonStrategy.ExpirationDateParamName]);
			ClassicAssert.AreEqual("NEWRELEASE", u89Parameters[CmeSecureLogonStrategy.EnvironmentParamName]);
			var u90Parameters = keysMap["U90"];
			ClassicAssert.IsNotNull(u90Parameters);
			ClassicAssert.AreEqual("1", u90Parameters[CmeSecureLogonStrategy.AccessIdParamName]);
			ClassicAssert.AreEqual("2", u90Parameters[CmeSecureLogonStrategy.SecretKeyParamName]);
			ClassicAssert.AreEqual("3", u90Parameters[CmeSecureLogonStrategy.CreationDateParamName]);
			ClassicAssert.AreEqual("4", u90Parameters[CmeSecureLogonStrategy.ExpirationDateParamName]);
			ClassicAssert.AreEqual("5", u90Parameters[CmeSecureLogonStrategy.EnvironmentParamName]);
		}

		[Test]
		public virtual void ThreeKeyLineShouldBeParsedCorrectly()
		{
			var fileContent =
				"Session ID           Access ID                      Secret Key                         " +
				"                Creation Date        Expiration Date      Environment          \n" + "\n" +
				"U89                  dipfNf8kZOOuk7Wz0ZUn           " +
				"gyJYeacu9T--a1tak4i5S-pBXxwCNK_wUqp9fh5usJ8        2018-01-23           2019-01-23   " +
				"        NEWRELEASE           \n" + "\n" +
				"U90                  1                              2                                  " +
				"                3                    4                    5           \n" + "\n" +
				"U91                  6                              7                                  " +
				"                8                    9                    10           \n" + "\n";
			File.WriteAllBytes(_fileName, fileContent.AsByteArray());
			var keysMap = CmeSecureLogonStrategy.ParseKeys(_fileName);
			ClassicAssert.IsNotNull(keysMap);
			var u89Parameters = keysMap["U89"];
			ClassicAssert.IsNotNull(u89Parameters);
			ClassicAssert.AreEqual("dipfNf8kZOOuk7Wz0ZUn", u89Parameters[CmeSecureLogonStrategy.AccessIdParamName]);
			ClassicAssert.AreEqual("gyJYeacu9T--a1tak4i5S-pBXxwCNK_wUqp9fh5usJ8", u89Parameters[CmeSecureLogonStrategy.SecretKeyParamName]);
			ClassicAssert.AreEqual("2018-01-23", u89Parameters[CmeSecureLogonStrategy.CreationDateParamName]);
			ClassicAssert.AreEqual("2019-01-23", u89Parameters[CmeSecureLogonStrategy.ExpirationDateParamName]);
			ClassicAssert.AreEqual("NEWRELEASE", u89Parameters[CmeSecureLogonStrategy.EnvironmentParamName]);
			var u90Parameters = keysMap["U90"];
			ClassicAssert.IsNotNull(u90Parameters);
			ClassicAssert.AreEqual("1", u90Parameters[CmeSecureLogonStrategy.AccessIdParamName]);
			ClassicAssert.AreEqual("2", u90Parameters[CmeSecureLogonStrategy.SecretKeyParamName]);
			ClassicAssert.AreEqual("3", u90Parameters[CmeSecureLogonStrategy.CreationDateParamName]);
			ClassicAssert.AreEqual("4", u90Parameters[CmeSecureLogonStrategy.ExpirationDateParamName]);
			ClassicAssert.AreEqual("5", u90Parameters[CmeSecureLogonStrategy.EnvironmentParamName]);
			var u91Parameters = keysMap["U90"];
			ClassicAssert.IsNotNull(u91Parameters);
			ClassicAssert.AreEqual("1", u91Parameters[CmeSecureLogonStrategy.AccessIdParamName]);
			ClassicAssert.AreEqual("2", u91Parameters[CmeSecureLogonStrategy.SecretKeyParamName]);
			ClassicAssert.AreEqual("3", u91Parameters[CmeSecureLogonStrategy.CreationDateParamName]);
			ClassicAssert.AreEqual("4", u91Parameters[CmeSecureLogonStrategy.ExpirationDateParamName]);
			ClassicAssert.AreEqual("5", u91Parameters[CmeSecureLogonStrategy.EnvironmentParamName]);
		}

		[Test]
		public virtual void ItShouldCalculateAdditionalTagsCorrectly()
		{
			var fileContent =
				"Session ID           Access ID                      Secret Key                         " +
				"                Creation Date        Expiration Date      Environment          \n" +
				"B08                  oNDxM33uE81ohWuVRtWT           " +
				"2HlZ7exg8jTKltXnURjKvm3GS5iF5n4ClzHiEm_ocv4        2018-01-23           2059-01-23   " +
				"        NEWRELEASE           \n" + "\n";
			File.WriteAllBytes(_fileName, fileContent.AsByteArray());
			var secureLogonStrategy = new CmeSecureLogonStrategy();
			var testSessionParameters = new SessionParameters();
			var testConfiguration = testSessionParameters.Configuration;
			testConfiguration.SetProperty(Config.CmeSecureKeysFile, _fileName);
			testConfiguration.SetProperty(Config.LogonCustomizationStrategy, Config.CmeSecureLogonStrategy);
			secureLogonStrategy.SetSessionParameters(testSessionParameters);
			FixMessage logonMessage = new LogonFixMessage1();
			secureLogonStrategy.CompleteLogon(logonMessage);
			ClassicAssert.AreEqual(20, logonMessage.GetTagAsInt(CmeSecureLogonStrategy.EncodedTextLenTag));
			ClassicAssert.AreEqual("oNDxM33uE81ohWuVRtWT", logonMessage.GetTagValueAsString(CmeSecureLogonStrategy.EncodedTextTag));
			ClassicAssert.AreEqual("CME-1-SHA-256", logonMessage.GetTagValueAsString(CmeSecureLogonStrategy.EncryptedPasswordMethodTag));
			ClassicAssert.AreEqual("oHZ2Dx1ihFAp7kHOFcJPkijm27xfApJFp-ZhsSCxr3s", logonMessage.GetTagValueAsString(CmeSecureLogonStrategy.EncryptedPasswordTag));
			ClassicAssert.AreEqual(43, logonMessage.GetTagAsInt(CmeSecureLogonStrategy.EncryptedPasswordLenTag));
		}

		private class LogonFixMessage1 : FixMessage
		{
			public LogonFixMessage1()
			{
				AddTag(Tags.MsgSeqNum, (long)1);
				AddTag(Tags.SenderCompID, "B08004N");
				AddTag(Tags.SenderSubID, "MSG");
				AddTag(Tags.SendingTime, "20170623-14:48:42.855");
				AddTag(Tags.TargetSubID, "92");
				AddTag(Tags.HeartBtInt, (long)60);
				AddTag(Tags.SenderLocationID, "US,NY");
				AddTag(Tags.LastMsgSeqNumProcessed, (long)0);
				AddTag(CmeSecureLogonStrategy.AppSystemNameTag, "BRIO");
				AddTag(CmeSecureLogonStrategy.TradingSystemVersionTag, "8.0");
				AddTag(CmeSecureLogonStrategy.AppSystemVendorTag, "ABC");
			}
		}

		[Test]
		public virtual void ItShouldCalculateAdditionalTagsCorrectly2()
		{
			var fileContent =
				"Session ID           Access ID                      Secret Key                         " +
				"                Creation Date        Expiration Date      Environment          \n" + "\n" +
				"U89                  dipfNf8kZOOuk7Wz0ZUn           " +
				"gyJYeacu9T--a1tak4i5S-pBXxwCNK_wUqp9fh5usJ8        2018-01-23           2059-01-23   " +
				"        NEWRELEASE           \n" + "\n";
			File.WriteAllBytes(_fileName, fileContent.AsByteArray());
			var secureLogonStrategy = new CmeSecureLogonStrategy();
			var testSessionParameters = new SessionParameters();
			var testConfiguration = testSessionParameters.Configuration;
			testConfiguration.SetProperty(Config.CmeSecureKeysFile, _fileName);
			testConfiguration.SetProperty(Config.LogonCustomizationStrategy, Config.CmeSecureLogonStrategy);
			secureLogonStrategy.SetSessionParameters(testSessionParameters);
			FixMessage logonMessage = new LogonFixMessage2();
			secureLogonStrategy.CompleteLogon(logonMessage);
			ClassicAssert.AreEqual(20, logonMessage.GetTagAsInt(CmeSecureLogonStrategy.EncodedTextLenTag));
			ClassicAssert.AreEqual("dipfNf8kZOOuk7Wz0ZUn", logonMessage.GetTagValueAsString(CmeSecureLogonStrategy.EncodedTextTag));
			ClassicAssert.AreEqual("CME-1-SHA-256", logonMessage.GetTagValueAsString(CmeSecureLogonStrategy.EncryptedPasswordMethodTag));
			ClassicAssert.AreEqual(43, logonMessage.GetTagAsInt(CmeSecureLogonStrategy.EncryptedPasswordLenTag));
			ClassicAssert.AreEqual("V9Xf6SV45lT0-x2fGs7aKJXk5j4SKEkXWmrLAVFNgGA", logonMessage.GetTagValueAsString(CmeSecureLogonStrategy.EncryptedPasswordTag));
		}

		private class LogonFixMessage2 : FixMessage
		{
			public LogonFixMessage2()
			{
				AddTag(Tags.MsgSeqNum, (long)1);
				AddTag(Tags.SenderCompID, "U89004N");
				AddTag(Tags.SenderSubID, "0J4L");
				AddTag(Tags.SendingTime, "20180420-14:21:52.697");
				AddTag(Tags.TargetSubID, "G");
				AddTag(Tags.HeartBtInt, (long)30);
				AddTag(Tags.SenderLocationID, "US");
				AddTag(Tags.LastMsgSeqNumProcessed, (long)0);
				AddTag(CmeSecureLogonStrategy.AppSystemNameTag, "FIXAntenna .NET Core");
				AddTag(CmeSecureLogonStrategy.TradingSystemVersionTag, "2.18.1");
				AddTag(CmeSecureLogonStrategy.AppSystemVendorTag, "B2BITS");
			}
		}

		[Test]
		public virtual void RedundantTagsShouldBeRemovedIfItExists()
		{
			var fileContent =
				"Session ID           Access ID                      Secret Key                         " +
				"                Creation Date        Expiration Date      Environment          \n" +
				"B08                  oNDxM33uE81ohWuVRtWT           2HlZ7exg8jTKltXnURjKvm3GS5iF5n4ClzHiEm_ocv4     " +
				"   2018-01-23           2059-01-23           NEWRELEASE           \n" + "\n";
			File.WriteAllBytes(_fileName, fileContent.AsByteArray());
			var secureLogonStrategy = new CmeSecureLogonStrategy();
			var testSessionParameters = new SessionParameters();
			var testConfiguration = testSessionParameters.Configuration;
			testConfiguration.SetProperty(Config.CmeSecureKeysFile, _fileName);
			testConfiguration.SetProperty(Config.LogonCustomizationStrategy, Config.CmeSecureLogonStrategy);
			secureLogonStrategy.SetSessionParameters(testSessionParameters);
			FixMessage logonMessage = new LogonFixMessage3();
			secureLogonStrategy.CompleteLogon(logonMessage);
			Console.WriteLine(logonMessage.ToPrintableString());
			ClassicAssert.IsFalse(logonMessage.IsTagExists(Tags.EncryptMethod));
			ClassicAssert.IsFalse(logonMessage.IsTagExists(Tags.RawDataLength));
			ClassicAssert.IsFalse(logonMessage.IsTagExists(Tags.RawData));
		}

		private class LogonFixMessage3 : FixMessage
		{
			public LogonFixMessage3()
			{
				AddTag(Tags.MsgSeqNum, (long)1);
				AddTag(Tags.SenderCompID, "B08004N");
				AddTag(Tags.SenderSubID, "MSG");
				AddTag(Tags.SendingTime, "20170623-14:48:42.855");
				AddTag(Tags.TargetSubID, "92");
				AddTag(Tags.HeartBtInt, (long)60);
				AddTag(Tags.SenderLocationID, "US,NY");
				AddTag(Tags.LastMsgSeqNumProcessed, (long)0);
				AddTag(CmeSecureLogonStrategy.AppSystemNameTag, "BRIO");
				AddTag(CmeSecureLogonStrategy.TradingSystemVersionTag, "8.0");
				AddTag(CmeSecureLogonStrategy.AppSystemVendorTag, "ABC");
				AddTag(Tags.EncryptMethod, (long)0);
				AddTag(Tags.RawDataLength, (long)0);
				AddTag(Tags.RawData, (long)0);
			}
		}

		[Test]
		public virtual void StrategyShouldNotBeAppliedIfKeyIsExpired()
		{
			var fileContent =
				"Session ID           Access ID                      Secret Key                         " +
				"                Creation Date        Expiration Date      Environment          \n" +
				"B08                  oNDxM33uE81ohWuVRtWT           " +
				"2HlZ7exg8jTKltXnURjKvm3GS5iF5n4ClzHiEm_ocv4        2018-01-23           " +
				DateTime.Now.AddDays(-1).ToString(CmeSecureLogonStrategy.DateFormat, CultureInfo.InvariantCulture) +
				"           NEWRELEASE           \n" + "\n";
			File.WriteAllBytes(_fileName, fileContent.AsByteArray());
			var secureLogonStrategy = new CmeSecureLogonStrategy();
			var testSessionParameters = new SessionParameters();
			var testConfiguration = testSessionParameters.Configuration;
			testConfiguration.SetProperty(Config.CmeSecureKeysFile, _fileName);
			testConfiguration.SetProperty(Config.LogonCustomizationStrategy, Config.CmeSecureLogonStrategy);
			secureLogonStrategy.SetSessionParameters(testSessionParameters);
			FixMessage logonMessage = new LogonFixMessage4();
			secureLogonStrategy.CompleteLogon(logonMessage);
			ClassicAssert.IsFalse(logonMessage.IsTagExists(CmeSecureLogonStrategy.EncodedTextLenTag));
			ClassicAssert.IsFalse(logonMessage.IsTagExists(CmeSecureLogonStrategy.EncodedTextTag));
			ClassicAssert.IsFalse(logonMessage.IsTagExists(CmeSecureLogonStrategy.EncryptedPasswordMethodTag));
			ClassicAssert.IsFalse(logonMessage.IsTagExists(CmeSecureLogonStrategy.EncryptedPasswordLenTag));
			ClassicAssert.IsFalse(logonMessage.IsTagExists(CmeSecureLogonStrategy.EncryptedPasswordTag));
		}

		private class LogonFixMessage4 : FixMessage
		{
			public LogonFixMessage4()
			{
				AddTag(Tags.SenderCompID, "B08004N");
			}
		}

		[Test]
		public virtual void FileOnlyWithHeaderShouldBeParsedCorrectlyNoEnv()
		{
			var fileContent =
				"Session ID           Access ID                      Secret Key                         " +
				"                Creation Date        Expiration Date      \n" + "\n";
			File.WriteAllBytes(_fileName, fileContent.AsByteArray());
			var keys = CmeSecureLogonStrategy.ParseKeys(_fileName);
			ClassicAssert.IsTrue(keys.Count == 0);
		}

		[Test]
		public virtual void OneKeyLineShouldBeParsedCorrectlyNoEnv()
		{
			var fileContent =
				"Session ID           Access ID                      Secret Key                         " +
				"                Creation Date        Expiration Date      \n" + "\n" +
				"U89                  dipfNf8kZOOuk7Wz0ZUn           gyJYeacu9T--a1tak4i5S-pBXxwCNK_wUqp9fh5usJ8     " +
				"   2018-01-23           2019-01-23           \n" + "\n";
			File.WriteAllBytes(_fileName, fileContent.AsByteArray());
			var keysMap = CmeSecureLogonStrategy.ParseKeys(_fileName);
			ClassicAssert.IsNotNull(keysMap);
			var u89Parameters = keysMap["U89"];
			ClassicAssert.IsNotNull(u89Parameters);
			ClassicAssert.AreEqual("dipfNf8kZOOuk7Wz0ZUn", u89Parameters[CmeSecureLogonStrategy.AccessIdParamName]);
			ClassicAssert.AreEqual("gyJYeacu9T--a1tak4i5S-pBXxwCNK_wUqp9fh5usJ8", u89Parameters[CmeSecureLogonStrategy.SecretKeyParamName]);
			ClassicAssert.AreEqual("2018-01-23", u89Parameters[CmeSecureLogonStrategy.CreationDateParamName]);
			ClassicAssert.AreEqual("2019-01-23", u89Parameters[CmeSecureLogonStrategy.ExpirationDateParamName]);
			ClassicAssert.AreEqual(null, u89Parameters.GetValueOrDefault(CmeSecureLogonStrategy.EnvironmentParamName));
		}
	}
}
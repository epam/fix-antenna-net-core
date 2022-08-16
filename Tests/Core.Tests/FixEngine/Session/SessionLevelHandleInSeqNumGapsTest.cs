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
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.TestUtils;

using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	[TestFixture]
	internal class SessionLevelHandleInSeqNumGapsTest
	{
		const string Sender = "Sender";
		const string Target = "Target";

		private AcceptorFixSessionHelper _helper;

		[SetUp]
		public void SetUp()
		{
			Assert.IsTrue(ClearLogs(), "Can't clean logs before tests");
			ConfigurationHelper.StoreGlobalConfig();
			// Speed up test execution. Otherwise _helper.Session.Dispose() takes 30 seconds
			Config.GlobalConfiguration.SetProperty(Config.ReadingThreadShutdownTimeout, "1");
			_helper = new AcceptorFixSessionHelper(GetTestSessionParameters());
		}

		[TearDown]
		public virtual void TearDown()
		{
			_helper.Session.Dispose();
			_helper.Transport.Close();
			ConfigurationHelper.RestoreGlobalConfig();
			Assert.IsTrue(ClearLogs(), "Can't clean logs after tests");
		}

		public virtual bool ClearLogs()
		{
			var logsCleaner = new LogsCleaner();
			return logsCleaner.Clean("./logs") && logsCleaner.Clean("./logs/backup");
		}

		[Test]
		public virtual void SmokeTest()
		{
			// arrange
			_helper.SetIncomingLogon(GetIncomingLogon());

			// act
			_helper.Session.Connect();
			var logonReply = _helper.Transport.ReadMessageFromSession();

			// assert
			Assert.AreEqual("A", logonReply.GetTagValueAsString(35));
		}

		private SessionParameters GetTestSessionParameters()
		{
			var details = new SessionParameters();

			details.FixVersion = FixVersion.Fix44;
			details.SenderCompId = Sender;
			details.TargetCompId = Target;

			return details;
		}

		private FixMessage GetIncomingLogon()
		{
			var msg = new FixMessage();
			msg.AddTag(8, "FIX.4.4");
			msg.AddTag(9, 1);
			msg.AddTag(35, "A");
			msg.AddTag(34, 1);
			msg.AddTag(49, Target);
			msg.AddTag(56, Sender);
			msg.AddTag(52, DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff"));
			msg.AddTag(108, 30);
			msg.AddTag(141, "Y");
			msg.AddTag(10, 123);
			return msg;
		}
	}
}
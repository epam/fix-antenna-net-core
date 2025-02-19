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

using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.TestUtils;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.Util
{
	[TestFixture]
	internal class ConfigurationAdaptorTest
	{
		[SetUp]
		public void SetUp()
		{
			ConfigurationHelper.StoreGlobalConfig();
		}

		[TearDown]
		public virtual void TearDown()
		{
			//restore config
			ConfigurationHelper.RestoreGlobalConfig();
		}

		/// <summary>
		/// # Reset sequences on switch to backup
		/// resetOnSwitchToBackup=false
		/// <p/>
		/// # Reset sequences on switch back to primary connection
		/// resetOnSwitchToPrimary=true
		/// <p/>
		/// # Enable switch to primary connection, default value true
		/// cyclicSwitchBackupConnection=true
		/// <p/>
		/// # Enable auto switch to backup connection, default value true
		/// enableAutoSwitchToBackupConnection=false
		/// <p/>
		/// # Server acceptor strategy
		/// default is Epam.FixAntenna.NetCore.FixEngine.Acceptor.AllowNonRegisteredAcceptorStrategyHandler
		/// </summary>
		[Test]
		public virtual void TestCheckBackParameters()
		{
			var globalConfiguration = Config.GlobalConfiguration;
			globalConfiguration.SetProperty(Config.ResetOnSwitchToBackup, "false");
			globalConfiguration.SetProperty(Config.ResetOnSwitchToPrimary, "true");
			globalConfiguration.SetProperty(Config.CyclicSwitchBackupConnection, "true");
			globalConfiguration.SetProperty(Config.EnableAutoSwitchToBackupConnection, "false");
			
			var configuration = new ConfigurationAdapter(globalConfiguration);

			ClassicAssert.IsFalse(configuration.IsResetOnSwitchToBackupEnabled);

			ClassicAssert.IsTrue(configuration.IsResetOnSwitchToPrimaryEnabled);

			ClassicAssert.IsTrue(configuration.IsCyclicSwitchBackupConnectionEnabled);

			ClassicAssert.IsFalse(configuration.IsAutoSwitchToBackupConnectionEnabled);

			ClassicAssert.AreEqual("Epam.FixAntenna.NetCore.FixEngine.Acceptor.AllowNonRegisteredAcceptorStrategyHandler", configuration.ServerAcceptorStrategy);
		}

		[Test]
		public virtual void TestDefaultForMaxMessagesToSendInBatch()
		{
			var conf = (Config) Config.GlobalConfiguration.Clone();
			conf.SetProperty(Config.MaxMessagesToSendInBatch, "0");

			var confAdapter = new ConfigurationAdapter(conf);

			var maxMessagesToSendInBatch = confAdapter.MaxMessagesToSendInBatch;
			ClassicAssert.IsTrue(maxMessagesToSendInBatch > 0, Config.MaxMessagesToSendInBatch + " has invalid value: " + maxMessagesToSendInBatch);
		}
	}
}
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
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.TestUtils;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Configuration.Tests
{
	[TestFixture]
	public class SslConfigurationTest
	{
		[SetUp]
		public void Setup()
		{
			ConfigurationHelper.StoreGlobalConfig();
		}

		[TearDown]
		public virtual void TearDown()
		{
			ConfigurationHelper.RestoreGlobalConfig();
		}

		/// <summary>
		/// Tests exception when wrong SslProtocol is configured
		/// </summary>
		[Test]
		public void WrongProtocolConfigurationTest()
		{
			var cfg = Config.GlobalConfiguration;
			cfg.SetProperty(Config.SslProtocol, "Tls42");
			var adapter = new ConfigurationAdapter(cfg);
			ClassicAssert.Throws<ArgumentException>(() => adapter.SslProtocol.ToString());
		}

		/// <summary>
		/// Tests default values for SSL related configuration options - booleans
		/// </summary>
		[TestCase(Config.RequireSsl, ExpectedResult = false)]
		[TestCase(Config.SslCheckCertificateRevocation, ExpectedResult = true)]
		[TestCase(Config.SslValidatePeerCertificate, ExpectedResult = true)]
		public bool BooleanOptionsTest(string name)
		{
			return Config.GlobalConfiguration.GetPropertyAsBoolean(name);
		}

		/// <summary>
		/// Tests default values for SSL related configuration options - strings
		/// </summary>
		[TestCase(Config.SslCaCertificate, ExpectedResult = "")]
		[TestCase(Config.SslCertificate, ExpectedResult = "")]
		[TestCase(Config.SslCertificatePassword, ExpectedResult = "")]
		[TestCase(Config.SslProtocol, ExpectedResult = "None")]
		[TestCase(Config.SslServerName, ExpectedResult = "")]
		public string StringOptionsTest(string name)
		{
			return Config.GlobalConfiguration.GetProperty(name);
		}
	}
}

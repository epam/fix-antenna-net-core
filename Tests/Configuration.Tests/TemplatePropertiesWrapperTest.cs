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
using Epam.FixAntenna.NetCore.Configuration;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Configuration
{
	using Properties = Dictionary<string, string>;

	public class TemplatePropertiesWrapperTest
	{
		public const string FaHome = "fa.home";
		private TemplatePropertiesWrapper _configuration;

		private Properties _props;

		[SetUp]
		public virtual void Before()
		{
			_props = new Properties();
			_props.Add(Config.StorageDirectory, "${" + FaHome + "}/logs");
		}

		/// <summary>
		/// Test checks path with linux notation.
		/// </summary>
		[Test]
		public virtual void GetTemplatePropertyForLinuxNotation()
		{
			_props.Add(FaHome, "root/");
			_configuration = new TemplatePropertiesWrapper(_props);

			var value = _configuration.GetProperty(Config.StorageDirectory);
			Assert.AreEqual(value, "root//logs");
		}

		[Test]
		public virtual void testReplacementOnTheFly()
		{
			try
			{
				_props.Add(FaHome, "root/");
				_configuration = new TemplatePropertiesWrapper(_props);
				Environment.SetEnvironmentVariable(FaHome, "test/");
				var value = _configuration.GetProperty(Config.StorageDirectory);
				Assert.AreEqual("test//logs", value);
			}
			finally
			{
				Environment.SetEnvironmentVariable(FaHome, null);
			}
		}

		[Test]
		public virtual void GetProperty()
		{
			_configuration = new TemplatePropertiesWrapper(_props);
			var value = _configuration.GetProperty(Config.RequireSsl);
			Assert.IsNull(value);
		}

		[Test]
		public virtual void GetTemplatePropertyFromEnv()
		{
			_configuration = new TemplatePropertiesWrapper(_props);
			_configuration.Put(Config.StorageDirectory, "${TEMP}/log");
			var value = _configuration.GetProperty(Config.StorageDirectory);
			Assert.AreNotSame("tmp//logs", value);
		}

		[Test]
		public virtual void GetDefaultTemplateProperty()
		{
			_configuration = new TemplatePropertiesWrapper(_props);
			var value = _configuration.GetProperty("Hi", "hi");
			Assert.AreEqual(value, "hi");
		}

		/// <summary>
		/// Test checks path with windows notation.
		/// </summary>
		[Test]
		public virtual void GetTemplatePropertyForWindowsNotation()
		{
			_props.Add("DOTNET.home", "D:\\j");
			_props.Add(FaHome, "${DOTNET.home}");
			_configuration = new TemplatePropertiesWrapper(_props);

			var value = _configuration.GetProperty(Config.StorageDirectory);
			Assert.AreEqual("${DOTNET.home}/logs", value);
		}

		/// <summary>
		/// Test checks situation when fa.home doesn't exists.
		/// </summary>
		[Test]
		public virtual void GetTemplatePropertyWithoutFixHomeProperty()
		{
			_configuration = new TemplatePropertiesWrapper(_props);
			var value = _configuration.GetProperty(Config.StorageDirectory, ".");
			Assert.AreEqual("/logs", value);
		}

		/// <summary>
		/// This test checks stack overflow situation.
		/// </summary>
		[Test]
		public virtual void GetTemplatePropertyWithExistInvalidTemplate()
		{
			_props.Add(FaHome, "${storageDirectory}");
			_configuration = new TemplatePropertiesWrapper(_props);

			var value = _configuration.GetProperty(Config.StorageDirectory);
			Assert.AreEqual("${storageDirectory}/logs", value);
		}

		[Test]
		public void TestPropertiesKeys()
		{
			_props.Add("notLowCaseKey", "notLowCaseValue");
			_configuration = new TemplatePropertiesWrapper(_props);

			var keys = _configuration.GetKeys();

			Assert.AreEqual("notLowCaseValue", _configuration.GetProperty("notLowCaseKey"));
			Assert.AreEqual("notLowCaseValue", _configuration.GetProperty("notlowcasekey"));
			Assert.IsTrue(keys.Contains("notLowCaseKey"));
		}
	}
}
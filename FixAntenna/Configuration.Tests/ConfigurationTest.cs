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
	public class ConfigurationTest
	{
		private IDictionary<string, string> _props;

		[SetUp]
		public virtual void Before()
		{
			_props = new Dictionary<string, string>();
		}

		[Test]
		public virtual void TestGetPropertyAsBoolean()
		{
			_props["boolflagYes"] = "Yes";
			_props["boolflagNo"] = "No";
			_props["boolflagTrue"] = "True";
			_props["boolflagTrueWithSpace"] = " True ";
			_props["boolflagFalse"] = "False";

			_props["boolflagyes"] = "yes";
			_props["boolflagno"] = "no";
			_props["boolflagtrue"] = "true";
			_props["boolflagtruewithspace"] = " true ";
			_props["boolflagfalse"] = "false";
			_props["boolflagBadValue"] = "activate";
			var _configuration = new Config(_props);

			Assert.IsTrue(_configuration.GetPropertyAsBoolean("boolflagYes"));
			Assert.IsTrue(_configuration.GetPropertyAsBoolean("boolflagYes", true));
			Assert.IsTrue(_configuration.GetPropertyAsBoolean("boolflagYes", false));

			Assert.IsFalse(_configuration.GetPropertyAsBoolean("boolflagNo"));
			Assert.IsFalse(_configuration.GetPropertyAsBoolean("boolflagNo", true));
			Assert.IsFalse(_configuration.GetPropertyAsBoolean("boolflagNo", false));

			Assert.IsTrue(_configuration.GetPropertyAsBoolean("boolflagTrue"));
			Assert.IsTrue(_configuration.GetPropertyAsBoolean("boolflagTrue", true));
			Assert.IsTrue(_configuration.GetPropertyAsBoolean("boolflagTrue", false));

			Assert.IsTrue(_configuration.GetPropertyAsBoolean("boolflagTrueWithSpace"));
			Assert.IsTrue(_configuration.GetPropertyAsBoolean("boolflagTrueWithSpace", true));
			Assert.IsTrue(_configuration.GetPropertyAsBoolean("boolflagTrueWithSpace", false));

			Assert.IsFalse(_configuration.GetPropertyAsBoolean("boolflagFalse"));
			Assert.IsFalse(_configuration.GetPropertyAsBoolean("boolflagFalse", true));
			Assert.IsFalse(_configuration.GetPropertyAsBoolean("boolflagFalse", false));

			Assert.IsTrue(_configuration.GetPropertyAsBoolean("boolflagyes"));
			Assert.IsTrue(_configuration.GetPropertyAsBoolean("boolflagyes", true));
			Assert.IsTrue(_configuration.GetPropertyAsBoolean("boolflagyes", false));

			Assert.IsFalse(_configuration.GetPropertyAsBoolean("boolflagno"));
			Assert.IsFalse(_configuration.GetPropertyAsBoolean("boolflagno", true));
			Assert.IsFalse(_configuration.GetPropertyAsBoolean("boolflagno", false));

			Assert.IsTrue(_configuration.GetPropertyAsBoolean("boolflagtrue"));
			Assert.IsTrue(_configuration.GetPropertyAsBoolean("boolflagtrue", true));
			Assert.IsTrue(_configuration.GetPropertyAsBoolean("boolflagtrue", false));

			Assert.IsTrue(_configuration.GetPropertyAsBoolean("boolflagtruewithspace"));
			Assert.IsTrue(_configuration.GetPropertyAsBoolean("boolflagtruewithspace", true));
			Assert.IsTrue(_configuration.GetPropertyAsBoolean("boolflagtruewithspace", false));

			Assert.IsFalse(_configuration.GetPropertyAsBoolean("boolflagfalse"));
			Assert.IsFalse(_configuration.GetPropertyAsBoolean("boolflagfalse", true));
			Assert.IsFalse(_configuration.GetPropertyAsBoolean("boolflagfalse", false));

			Assert.IsFalse(_configuration.GetPropertyAsBoolean("boolflagNon"));
			Assert.IsTrue(_configuration.GetPropertyAsBoolean("boolflagNon", true));
			Assert.IsFalse(_configuration.GetPropertyAsBoolean("boolflagNon", false));

			Assert.IsFalse(_configuration.GetPropertyAsBoolean("boolflagBadValue"));
			Assert.IsTrue(_configuration.GetPropertyAsBoolean("boolflagBadValue", true));
			Assert.IsFalse(_configuration.GetPropertyAsBoolean("boolflagBadValue", false));
		}

		[Test]
		public virtual void TestGetPropertyInDiffCase()
		{
			_props["lowCase"] = "Yes";
			var configuration = new Config(_props);
			Assert.IsTrue(configuration.GetPropertyAsBoolean("LowCase", false));
		}

		[Test]
		public virtual void TestGetPropertyAsByteLength()
		{
			IList<ByteLengthValue> valueMatrix = new List<ByteLengthValue>();
			valueMatrix.Add(new ByteLengthValue("bl_No", "10", 10));
			valueMatrix.Add(new ByteLengthValue("bl_No_with_space", " 10 ", 10));
			valueMatrix.Add(new ByteLengthValue("bl_b", "10b", 10));
			valueMatrix.Add(new ByteLengthValue("bl_b_with_space", " 10b ", 10));
			valueMatrix.Add(new ByteLengthValue("bl_B", "10B", 10));
			valueMatrix.Add(new ByteLengthValue("bl_kb", "10kb", 10 * 1024));
			valueMatrix.Add(new ByteLengthValue("bl_Kb", "10Kb", 10 * 1024));
			valueMatrix.Add(new ByteLengthValue("bl_KB", "10KB", 10 * 1024));
			valueMatrix.Add(new ByteLengthValue("bl_mb", "10mb", 10 * 1024 * 1024));
			valueMatrix.Add(new ByteLengthValue("bl_Mb", "10Mb", 10 * 1024 * 1024));
			valueMatrix.Add(new ByteLengthValue("bl_MB", "10MB", 10 * 1024 * 1024));

			valueMatrix.Add(new ByteLengthValue("bl_gb", "10gb", 10L * 1024 * 1024 * 1024));
			valueMatrix.Add(new ByteLengthValue("bl_Gb", "10Gb", 10L * 1024 * 1024 * 1024));
			valueMatrix.Add(new ByteLengthValue("bl_GB", "10GB", 10L * 1024 * 1024 * 1024));

			// unsupported suffix
			valueMatrix.Add(new ByteLengthValue("bl_space_MB", "10 MB", 10 * 1024 * 1024));

			valueMatrix.Add(new ByteLengthValue("bl_Ab", "10Ab", -1));
			valueMatrix.Add(new ByteLengthValue("bl_MMb", "10MMb", -1));
			valueMatrix.Add(new ByteLengthValue("bl_k", "10k", -1));

			foreach (var value in valueMatrix)
			{
				_props[value.PropertyName] = value.StrValue;
			}

			var configuration = new Config(_props);
			foreach (var value in valueMatrix)
			{
				Assert.AreEqual(value.Result, configuration.GetPropertyAsBytesLength(value.PropertyName, -1),
					"Some problem with parse property: " + value.PropertyName);
			}
		}

		[Test]
		public virtual void TestGetPropertyAsInt()
		{
			_props["intFlag"] = "10";
			_props["intFlagWithSpace"] = " 10 ";
			_props["intFlagWithoutValue"] = "  ";
			var configuration = new Config(_props);

			Assert.AreEqual(10, configuration.GetPropertyAsInt("intFlag"));
			Assert.AreEqual(10, configuration.GetPropertyAsInt("intFlagWithSpace", 5));
			Assert.AreEqual(5, configuration.GetPropertyAsInt("intFlagWithoutValue", 5));
		}

		[Test]
		public virtual void TestGetIntOutOfRange()
		{
			// this value do not fit to validator.
			var userValue = -10;
			_props[Config.MaxDelayToSendAfterLogon] = Convert.ToString(userValue);
			var configuration = new Config(_props);
			// as result value must be default value
			var resultValue =
				configuration.GetPropertyAsInt(Config.MaxDelayToSendAfterLogon, 0, int.MaxValue, true);
			Assert.IsNotNull(resultValue);
			Assert.IsFalse(resultValue == 0);
			Assert.IsFalse(resultValue == -1);
			Assert.IsFalse(resultValue == userValue);
		}
		
		[Test]
		public virtual void TestExists()
		{
			_props["intFlag"] = "10";
			var configuration = new Config(_props);
			Assert.IsTrue(configuration.Exists("intFlag"));
			Assert.IsTrue(configuration.Exists("intflag"));
			Assert.IsTrue(configuration.Exists("INTFLAG"));
		}

		[Test]
		public void TestConfigurationPropertiesForUpCaseKey()
		{
			_props["sessionIDs"] = "sessionID1,sessionID2,sessionID3";

			var configuration = new Config(_props);

			Assert.IsNotNull(configuration.GetProperty("sessionIDs"));
			Assert.AreEqual("sessionID1,sessionID2,sessionID3", configuration.GetProperty("sessionIDs"));

			var properties = configuration.Properties;
			Assert.IsNotNull(properties["sessionIDs"]);
			Assert.AreEqual("sessionID1,sessionID2,sessionID3", properties["sessionIDs"]);
		}

		[Test]
		public void TestInitCustomFixVersionConfiguration()
		{
			_props["customFixVersions"] = "FIX42Custom,FIX44Custom";

			_props["customFixVersion.FIX42Custom.fixVersion"] = "FIX.4.2";
			_props["customFixVersion.FIX42Custom.fileName"] = "fixdic42-custom1.xml";

			_props["customFixVersion.FIX44Custom.fixVersion"] = "FIX.4.4";
			_props["customFixVersion.FIX44Custom.fileName"] = "fixdic44-custom.xml";

			var configuration = new Config(_props);

			Assert.IsNotNull(configuration.GetCustomFixVersionConfig("FIX42Custom"));
			Assert.AreEqual("FIX.4.2", configuration.GetCustomFixVersionConfig("FIX42Custom").FixVersion);
			Assert.AreEqual("fixdic42-custom1.xml", configuration.GetCustomFixVersionConfig("FIX42Custom").FileName);

			Assert.IsNotNull(configuration.GetCustomFixVersionConfig("FIX44Custom"));
			Assert.AreEqual("FIX.4.4", configuration.GetCustomFixVersionConfig("FIX44Custom").FixVersion);
			Assert.AreEqual("fixdic44-custom.xml", configuration.GetCustomFixVersionConfig("FIX44Custom").FileName);
		}


		[Test]
		public void TestSetAllPropertiesForCustomFixVersions()
		{
			_props["customFixVersions"] = "FIX42Custom";
			_props["customFixVersion.FIX42Custom.fixVersion"] = "FIX.4.2";
			_props["customFixVersion.FIX42Custom.fileName"] = "fixdic42-custom2.xml";
			var configuration = new Config(_props);

			Assert.IsNotNull(configuration.GetCustomFixVersionConfig("FIX42Custom"));
			Assert.AreEqual("FIX.4.2", configuration.GetCustomFixVersionConfig("FIX42Custom").FixVersion);
			Assert.AreEqual("fixdic42-custom2.xml", configuration.GetCustomFixVersionConfig("FIX42Custom").FileName);

			var newProps = new Dictionary<string, string>();
			newProps["customFixVersions"] = "FIX44Custom";
			newProps["customFixVersion.FIX44Custom.fixVersion"] = "FIX.4.4";
			newProps["customFixVersion.FIX44Custom.fileName"] = "fixdic44-custom.xml";

			configuration.SetAllProperties(newProps);

			Assert.IsNull(configuration.GetCustomFixVersionConfig("FIX42Custom"));
			Assert.IsNotNull(configuration.GetCustomFixVersionConfig("FIX44Custom"));
			Assert.AreEqual("FIX.4.4", configuration.GetCustomFixVersionConfig("FIX44Custom").FixVersion);
			Assert.AreEqual("fixdic44-custom.xml", configuration.GetCustomFixVersionConfig("FIX44Custom").FileName);
		}

		[Test]
		public void TestAddAllPropertiesForCustomFixVersions()
		{
			_props["customFixVersions"] = "FIX42Custom,FIX44Custom";

			_props["customFixVersion.FIX42Custom.fixVersion"] = "FIX.4.2";
			_props["customFixVersion.FIX42Custom.fileName"] = "fixdic42-custom3.xml";

			_props["customFixVersion.FIX44Custom.fixVersion"] = "FIX.4.4";
			_props["customFixVersion.FIX44Custom.fileName"] = "fixdic44-custom.xml";

			var configuration = (Config)Config.GlobalConfiguration.Clone();
			configuration.AddAllProperties(_props);

			Assert.IsNotNull(configuration.GetCustomFixVersionConfig("FIX42Custom"));
			Assert.AreEqual("FIX.4.2", configuration.GetCustomFixVersionConfig("FIX42Custom").FixVersion);
			Assert.AreEqual("fixdic42-custom3.xml", configuration.GetCustomFixVersionConfig("FIX42Custom").FileName);

			Assert.IsNotNull(configuration.GetCustomFixVersionConfig("FIX44Custom"));
			Assert.AreEqual("FIX.4.4", configuration.GetCustomFixVersionConfig("FIX44Custom").FixVersion);
			Assert.AreEqual("fixdic44-custom.xml", configuration.GetCustomFixVersionConfig("FIX44Custom").FileName);
		}

		[Test]
		public void TestSetPropertieForCustomFixVersions()
		{
			var configuration = (Config)Config.GlobalConfiguration.Clone();

			//set only names - no custom version
			configuration.SetProperty("customFixVersions", "FIX42Custom,FIX44Custom");
			Assert.IsNull(configuration.GetCustomFixVersionConfig("FIX42Custom"));
			Assert.IsNull(configuration.GetCustomFixVersionConfig("FIX44Custom"));

			//add properties for custom FIX4.2 - check that it is parsed
			configuration.SetProperty("customFixVersion.FIX42Custom.fixVersion", "FIX.4.2");
			configuration.SetProperty("customFixVersion.FIX42Custom.fileName", "fixdic42-custom4.xml");
			Assert.IsNotNull(configuration.GetCustomFixVersionConfig("FIX42Custom"));
			Assert.IsNull(configuration.GetCustomFixVersionConfig("FIX44Custom"));
			Assert.AreEqual("FIX.4.2", configuration.GetCustomFixVersionConfig("FIX42Custom").FixVersion);
			Assert.AreEqual("fixdic42-custom4.xml", configuration.GetCustomFixVersionConfig("FIX42Custom").FileName);

			//add properties for custom FIX4.4 - check that both are available
			configuration.SetProperty("customFixVersion.FIX44Custom.fixVersion", "FIX.4.4");
			configuration.SetProperty("customFixVersion.FIX44Custom.fileName", "fixdic44-custom.xml");

			Assert.IsNotNull(configuration.GetCustomFixVersionConfig("FIX42Custom"));
			Assert.AreEqual("FIX.4.2", configuration.GetCustomFixVersionConfig("FIX42Custom").FixVersion);
			Assert.AreEqual("fixdic42-custom4.xml", configuration.GetCustomFixVersionConfig("FIX42Custom").FileName);

			Assert.IsNotNull(configuration.GetCustomFixVersionConfig("FIX44Custom"));
			Assert.AreEqual("FIX.4.4", configuration.GetCustomFixVersionConfig("FIX44Custom").FixVersion);
			Assert.AreEqual("fixdic44-custom.xml", configuration.GetCustomFixVersionConfig("FIX44Custom").FileName);
		}

		[Test]
		public void TestClone()
		{
			var globalConfiguration = Config.GlobalConfiguration;
			var cleanGlobalConfig = (Config)globalConfiguration.Clone();

			_props["customFixVersions"] = "FIX42Custom";
			_props["customFixVersion.FIX42Custom.fixVersion"] = "FIX.4.2";
			_props["customFixVersion.FIX42Custom.fileName"] = "fixdic42-custom4.xml";

			var configuration1 = (Config)globalConfiguration.Clone();
			configuration1.SetAllProperties(_props);

			var configuration2 = (Config)globalConfiguration.Clone();
			Assert.IsNull(configuration2.GetProperty(Config.CustomFixVersions));
			Assert.IsNull(configuration2.GetCustomFixVersionConfig("FIX42Custom"));

			try 
			{
				globalConfiguration.SetAllProperties(_props);
				var configuration3 = (Config)globalConfiguration.Clone();
				Assert.AreEqual("FIX42Custom", configuration3.GetProperty(Config.CustomFixVersions));
				Assert.IsNotNull(configuration3.GetCustomFixVersionConfig("FIX42Custom"));
			} 
			finally 
			{
				//restore config
				Config.GlobalConfiguration.SetAllProperties(cleanGlobalConfig.Properties);
			}
		}

		[Test]
		public void MaxMessageSizeWrongEvTest()
		{
			const string evName = "FANET_maxMessageSize";
			var configuration = new Config(_props);
			
			// backup old value
			var oldEvValue = Environment.GetEnvironmentVariable(evName, EnvironmentVariableTarget.Process);
			// set new value
			Environment.SetEnvironmentVariable(evName, " ", EnvironmentVariableTarget.Process);

			var actualValue = configuration.GetProperty(Config.MaxMessageSize);

			try
			{
				Assert.AreEqual("1Mb", actualValue);
			}
			finally
			{
				// restore old value
				Environment.SetEnvironmentVariable(evName, oldEvValue, EnvironmentVariableTarget.Process);
			}
		}

		[Test]
		public void MaxMessageSizeCorrectEvTest()
		{
			const string evName = "FANET_maxMessageSize";
			const string evValue = "2Mb";
			var configuration = new Config(_props);

			// backup old value
			var oldEvValue = Environment.GetEnvironmentVariable(evName, EnvironmentVariableTarget.Process);
			// set new value
			Environment.SetEnvironmentVariable(evName, evValue, EnvironmentVariableTarget.Process);

			var actualValue = configuration.GetProperty(Config.MaxMessageSize);

			try
			{
				Assert.AreEqual(evValue, actualValue);
			}
			finally
			{
				// restore old value
				Environment.SetEnvironmentVariable(evName, oldEvValue, EnvironmentVariableTarget.Process);
			}
		}

		private class ByteLengthValue
		{
			internal readonly string PropertyName;
			internal readonly long Result;
			internal readonly string StrValue;

			internal ByteLengthValue(string propertyName, string strValue, long result)
			{
				PropertyName = propertyName;
				StrValue = strValue;
				Result = result;
			}

			public override string ToString()
			{
				return "ByteLengthValue{" +
						"propertyName='" + PropertyName + '\'' +
						", strValue='" + StrValue + '\'' +
						", result=" + Result +
						'}';
			}
		}
	}
}
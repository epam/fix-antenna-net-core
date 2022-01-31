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

using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Configuration
{
	public class FixVersionContainerFactoryTest
	{
		[Test]
		public virtual void FindUpdatedVersion()
		{
			var config = GetConfig();
			config.SetProperty("validation.FIX44.additionalDictionaryFileName", "custom44.xml");
			var fixVersion = FixVersionContainerFactory.GetFixVersionContainer(config, FixVersion.Fix44);
			var expected = new FixVersionContainer("FIX44", FixVersion.Fix44, "fixdic44.xml", "custom44.xml");

			CheckEquals(fixVersion, expected);
		}

		[Test]
		public virtual void FindUpdatedVersion2()
		{
			var config = GetConfig();
			config.SetProperty("validation.FIX44.additionalDictionaryFileName", "custom44.xml");
			config.SetProperty("validation.FIX44.additionalDictionaryUpdate", "true");
			var fixVersion = FixVersionContainerFactory.GetFixVersionContainer(config, FixVersion.Fix44);
			var expected = new FixVersionContainer("FIX44", FixVersion.Fix44, "fixdic44.xml", "custom44.xml");

			CheckEquals(fixVersion, expected);
		}

		[Test]
		public virtual void FindReloadedVersion()
		{
			var config = GetConfig();
			config.SetProperty("validation.FIX44.additionalDictionaryFileName", "custom44.xml");
			config.SetProperty("validation.FIX44.additionalDictionaryUpdate", "false");
			var fixVersion = FixVersionContainerFactory.GetFixVersionContainer(config, FixVersion.Fix44);
			var expected = new FixVersionContainer("FIX44", FixVersion.Fix44, "custom44.xml");

			CheckEquals(fixVersion, expected);
		}

		[Test]
		public virtual void FindCustomReloadedVersion()
		{
			var config = GetConfig();
			config.SetProperty("validation.FIX44Custom.additionalDictionaryFileName", "custom44.xml");
			config.SetProperty("validation.FIX44Custom.additionalDictionaryUpdate", "false");
			var fixVersion = FixVersionContainerFactory.GetFixVersionContainer("FIX44Custom", config, FixVersion.Fix44);
			var expected = new FixVersionContainer("FIX44Custom", FixVersion.Fix44, "custom44.xml");

			CheckEquals(fixVersion, expected);
		}

		[Test]
		public void TestCustomFixVersionById()
		{
			var config = GetConfig();
			config.SetProperty("customFixVersions", "FIX42Custom");

			config.SetProperty("customFixVersion.FIX42Custom.fixVersion", "FIX.4.2");
			config.SetProperty("customFixVersion.FIX42Custom.fileName", "fixdic42-custom.xml");

			var builtVersion = FixVersionContainerFactory.GetFixVersionContainer("FIX42Custom", config);
			var expected = new FixVersionContainer("FIX42Custom", FixVersion.Fix42, "fixdic42-custom.xml");

			CheckEquals(expected, builtVersion);
		}

		[Test]
		public void TestDefaultFixVersionById()
		{
			var config = GetConfig();

			//mimic custom as default
			config.SetProperty("customFixVersions", "FIX42");
			config.SetProperty("customFixVersion.FIX42.fixVersion", "FIX.4.2");
			config.SetProperty("customFixVersion.FIX42.fileName", "fixdic42-custom.xml");

			var builtVersion = FixVersionContainerFactory.GetFixVersionContainer(FixVersion.Fix42.Id, config);
			var expected = new FixVersionContainer("FIX42", FixVersion.Fix42, "fixdic42.xml");

			CheckEquals(expected, builtVersion);
		}

		private void CheckEquals(FixVersionContainer expected, FixVersionContainer actual)
		{
			Assert.AreEqual(expected.DictionaryId, actual.DictionaryId);
			Assert.AreEqual(expected.FixVersion, actual.FixVersion);
			Assert.AreEqual(expected.DictionaryFile, actual.DictionaryFile);
			Assert.AreEqual(expected.ExtensionFile, actual.ExtensionFile);
		}

		private Config GetConfig()
		{
			return (Config)Config.GlobalConfiguration.Clone();
		}
	}
}
// Copyright (c) 2022 EPAM Systems
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
using System.IO;
using Epam.FixAntenna.NetCore.Common.ResourceLoading;
using Epam.FixAntenna.NetCore.Configuration;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Common.ResourceLoading
{
	public class ConfiguredDirResourceLoaderTest
	{
		[Test]
		public void LoadResourceWithNoConfigurationDirSetTest()
		{
			// Arrange
			Config.ConfigurationDirectory = null;
			var loader = new ConfiguredDirResourceLoader(new DummyResourceLoader());

			// Act, ClassicAssert
			ClassicAssert.Throws<ResourceNotFoundException>(() => loader.LoadResource("fixengine.properties"));
		}

		[Test]
		public void LoadResourceWhenNoResourceExistsInConfiguredDirTest()
		{
			// Arrange
			Config.ConfigurationDirectory = TestContext.CurrentContext.TestDirectory;
			var loader = new ConfiguredDirResourceLoader(new DummyResourceLoader());

			// Act, ClassicAssert
			ClassicAssert.Throws<ResourceNotFoundException>(() => loader.LoadResource($"fixengine_{Guid.NewGuid()}.properties"));
		}

		[Test]
		public void LoadExistingResourceFromConfiguredDirTest()
		{
			// Arrange
			var filename = $"fixengine_{Guid.NewGuid()}.properties";
			var filepath = Path.Combine(TestContext.CurrentContext.TestDirectory, filename);
			var file = File.CreateText(filepath);
			file.Close();
			Config.ConfigurationDirectory = TestContext.CurrentContext.TestDirectory;
			var loader = new ConfiguredDirResourceLoader(new DummyResourceLoader());

			// Act
			var stream = loader.LoadResource(filepath);

			// ClassicAssert
			try
			{
				ClassicAssert.IsNotNull(stream);
			}
			finally
			{
				stream.Close();
				File.Delete(filepath);
			}
		}
	}
}

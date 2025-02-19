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
	public class ResourceLoaderTest
	{
		[Test]
		public void LoadResourceWithNoConfigurationDirSetTest()
		{
			// Arrange
			Config.ConfigurationDirectory = null;

			// Act, ClassicAssert
			ClassicAssert.Throws<ResourceNotFoundException>(
				() => ResourceLoader.DefaultLoader.LoadResource($"fixengine_{Guid.NewGuid()}.properties")
			);
		}

		[Test]
		public void LoadResourceWhenNoResourceExistsInConfiguredDirTest()
		{
			// Arrange
			Config.ConfigurationDirectory = TestContext.CurrentContext.TestDirectory;

			// Act, ClassicAssert
			ClassicAssert.Throws<ResourceNotFoundException>(
				() => ResourceLoader.DefaultLoader.LoadResource($"fixengine_{Guid.NewGuid()}.properties")
			);
		}

		[Test]
		public void LoadExistingResourceFromConfiguredDirTest()
		{
			// Arrange
			var filename = $"fixengine_{Guid.NewGuid()}.properties";
			var path = Path.Combine(TestContext.CurrentContext.TestDirectory, Guid.NewGuid().ToString());
			Directory.CreateDirectory(path);
			var filepath = Path.Combine(path, filename);
			var file = File.CreateText(filepath);
			file.Close();
			Config.ConfigurationDirectory = TestContext.CurrentContext.TestDirectory;

			// Act
			var stream = ResourceLoader.DefaultLoader.LoadResource(filepath);

			// ClassicAssert
			try
			{
				ClassicAssert.IsNotNull(stream);
			}
			finally
			{
				stream.Close();
				Directory.Delete(path, true);
			}
		}
	}
}

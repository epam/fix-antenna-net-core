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

using System.IO;
using Epam.FixAntenna.NetCore.Configuration;

namespace Epam.FixAntenna.NetCore.Common.ResourceLoading
{
	/// <summary>
	/// Looks for resources within configured directory.
	/// </summary>
	internal class ConfiguredDirResourceLoader : GenericFileResourceLoader
	{
		/// <summary>
		/// Create <c>ConfiguredDirResourceLoader</c>.
		/// </summary>
		/// <param name="nextLoader"> the next nextLoader </param>
		public ConfiguredDirResourceLoader(ResourceLoader nextLoader) : base(string.Empty, nextLoader)
		{
		}

		public override Stream LoadResource(string resourceName)
		{
			return string.IsNullOrWhiteSpace(Config.ConfigurationDirectory)
				? NextLoader.LoadResource(resourceName)
				: base.LoadResource(resourceName);
		}

		protected override string GetResourcePath(string resourceName)
		{
			return Path.Combine(Config.ConfigurationDirectory, resourceName);
		}
	}
}
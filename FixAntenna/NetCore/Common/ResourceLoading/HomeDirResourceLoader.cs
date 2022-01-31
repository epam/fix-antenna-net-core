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
using System.IO;

namespace Epam.FixAntenna.NetCore.Common.ResourceLoading
{
	/// <summary>
	/// Looking for resource in user home directory.
	/// </summary>
	internal class HomeDirResourceLoader : GenericFileResourceLoader
	{
		private static readonly string HomeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

		/// <summary>
		/// Create the <c>HomeDirResourceLoader</c>.
		/// The next nextLoader is DummyResourceLoader.
		/// </summary>
		public HomeDirResourceLoader() : this(new DummyResourceLoader())
		{
		}

		/// <summary>
		/// Create the <c>HomeDirResourceLoader</c>.
		/// </summary>
		/// <param name="nextLoader"> the next nextLoader </param>
		public HomeDirResourceLoader(ResourceLoader nextLoader) : this(HomeDir, nextLoader)
		{
		}

		/// <summary>
		/// Create the <c>HomeDirResourceLoader</c>.
		/// </summary>
		/// <param name="innerPath"> path inside user home directory; should not contain resource name </param>
		/// <param name="nextLoader"> the next nextLoader </param>
		public HomeDirResourceLoader(string innerPath, ResourceLoader nextLoader)
			: base(Path.Combine(HomeDir, innerPath), nextLoader)
		{
		}
	}
}
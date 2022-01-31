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

using System.IO;

namespace Epam.FixAntenna.NetCore.Common.ResourceLoading
{
	/// <summary>
	/// Looks for resources within current directory
	/// </summary>
	internal class CurrentDirResourceLoader : GenericFileResourceLoader
	{
		/// <summary>
		/// Create <c>CurrentDirResourceLoader</c>.
		/// The next nextLoader is DummyResourceLoader.
		/// </summary>
		public CurrentDirResourceLoader() : this(new DummyResourceLoader())
		{
		}

		/// <summary>
		/// Create <c>CurrentDirResourceLoader</c>.
		/// </summary>
		/// <param name="nextLoader"> the next nextLoader </param>
		public CurrentDirResourceLoader(ResourceLoader nextLoader) : this(string.Empty, nextLoader)
		{
		}

		/// <summary>
		/// Create <c>CurrentDirResourceLoader</c>.
		/// </summary>
		/// <param name="innerPath"> path inside current directory; should not contain resource name </param>
		/// <param name="nextLoader"> the next nextLoader </param>
		public CurrentDirResourceLoader(string innerPath, ResourceLoader nextLoader)
			: base(Path.Combine(".", innerPath), nextLoader)
		{
		}
	}
}
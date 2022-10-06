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
using Epam.FixAntenna.NetCore.Common.Logging;

namespace Epam.FixAntenna.NetCore.Common.ResourceLoading
{
	/// <summary>
	/// Standard interface to implement by every <c>ResourceLoader</c> strategy.
	/// </summary>
	internal abstract class ResourceLoader
	{
		/// <summary>
		/// Default resource loader. <br/>
		/// Order of looking for resources: <br/>
		/// 1. Files inside configured directory <br/>
		/// 2. Files inside current directory <br/>
		/// 3. Files inside home directory <br/>
		/// 4. Embedded resources inside libraries from method callstack.
		/// </summary>
		public static readonly ResourceLoader DefaultLoader =
			new ConfiguredDirResourceLoader(
				new CurrentDirResourceLoader(
					new HomeDirResourceLoader(
						new EmbeddedResourceLoader())));

		/// <summary>
		/// Default resource loader for dictionaries. <br/>
		/// Order of looking for dictionaries:
		/// 1. Files inside current directory <br/>
		/// 2. Embedded resources inside libraries from method callstack
		/// </summary>
		public static readonly ResourceLoader DictionaryLoader =
			new DictionaryLoader("Dictionaries", 
				new LibraryDirResourceLoader("Dictionaries", 
					new EmbeddedResourceLoader("Dictionaries", 
						new DummyResourceLoader())));

		protected ResourceLoader()
		{
			Log = LogFactory.GetLog(GetType().FullName);
		}

		protected ILog Log { get; }


		/// <summary>
		/// Load resource
		/// </summary>
		/// <param name="resourceName">resource name </param>
		/// <returns>Stream with resource</returns>
		/// <exception cref="ResourceNotFoundException">if resource is not found and no parent ResourceLoader is available</exception>
		public abstract Stream LoadResource(string resourceName);
	}
}
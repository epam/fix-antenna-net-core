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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Epam.FixAntenna.NetCore.Common.ResourceLoading
{
	/// <summary>
	/// Looks for embedded resources within libraries that are in the callstack.
	/// </summary>
	internal class EmbeddedResourceLoader : ResourceLoader
	{
		private readonly string _innerPath;
		private readonly ResourceLoader _nextNextLoader;

		/// <summary>
		/// Create <c>EmbeddedResourceLoader</c>.
		/// The next nextLoader is <c>DummyResourceLoader</c>.
		/// </summary>
		public EmbeddedResourceLoader() : this(string.Empty, new DummyResourceLoader())
		{
		}

		/// <summary>
		/// Create <c>EmbeddedResourceLoader</c>.
		/// </summary>
		public EmbeddedResourceLoader(string innerPath) : this(innerPath, new DummyResourceLoader())
		{
		}

		/// <summary>
		/// Create <c>EmbeddedResourceLoader</c>.
		/// </summary>
		public EmbeddedResourceLoader(string innerPath, ResourceLoader nextLoader)
		{
			_innerPath = innerPath;
			_nextNextLoader = nextLoader;
		}

		/// <summary>
		/// Look for embedded resource within libraries that are in the callstack of this method
		/// </summary>
		/// <param name="resourceName">Resource name</param>
		/// <returns>Stream with resource</returns>
		public override Stream LoadResource(string resourceName)
		{
			if (string.IsNullOrWhiteSpace(resourceName))
			{
				throw new ArgumentNullException(nameof(resourceName));
			}

			var searchName = Path.Combine(_innerPath, resourceName).Replace('\\', '/').Replace('/', '.');
			var currentAssembly = Assembly.GetExecutingAssembly();
			var currentStackFrames = new StackTrace().GetFrames() ??
									throw new InvalidOperationException("Could not get frames of current stack trace");
			var callerAssemblies = currentStackFrames
				.Select(x => x.GetMethod().ReflectedType?.Assembly)
				.Where(x => x != null)
				.Distinct()
				.Where(x => x.GetReferencedAssemblies().Any(y => y.FullName == currentAssembly.FullName))
				.Reverse()
				.ToList();

			callerAssemblies.Add(currentAssembly);

			foreach (var assembly in callerAssemblies)
			{
				var tryName = assembly.GetName().Name + "." + searchName;
				var stream = assembly.GetManifestResourceStream(tryName);
				if (stream != null)
				{
					if (Log.IsInfoEnabled)
					{
						Log.Info("Load resource:" + resourceName + " as " + tryName);
					}

					return stream;
				}
			}

			Log.Trace("Embedded resource loader failed to load: " + resourceName + "; resource name for search: " + searchName);
			return _nextNextLoader.LoadResource(resourceName);
		}
	}
}
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
	internal class GenericFileResourceLoader : ResourceLoader
	{
		private readonly string _path;
		private readonly ResourceLoader _nextNextLoader;

		protected ResourceLoader NextLoader => _nextNextLoader;
		/// <summary>
		/// Create the <c>GenericFileResourceLoader</c>.
		/// The next nextLoader is DummyResourceLoader.
		/// </summary>
		/// <param name="path"> path; should not contain resource name </param>
		public GenericFileResourceLoader(string path) : this(path, new DummyResourceLoader())
		{
		}

		/// <summary>
		/// Create the <c>GenericFileResourceLoader</c>.
		/// </summary>
		/// <param name="path"> path; should not contain resource name </param>
		/// <param name="nextLoader"> the next nextLoader </param>
		public GenericFileResourceLoader(string path, ResourceLoader nextLoader)
		{
			_path = path;
			_nextNextLoader = nextLoader;
		}

		/// <inheritdoc />
		public override Stream LoadResource(string resourceName)
		{
			var resourcePath = GetResourcePath(resourceName);
			try
			{
				if (!File.Exists(resourcePath))
				{
					Log.Trace($"Generic file resource loader failed to load {resourcePath} as file does not exist");
					return _nextNextLoader.LoadResource(resourceName);
				}

				var stream = new BufferedStream(new FileStream(resourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
				if (Log.IsInfoEnabled)
				{
					Log.Debug("Load resource:" + resourcePath);
				}

				return stream;
			}
			catch (FileNotFoundException)
			{
				Log.Trace("Generic file resource loader failed to load: " + resourcePath);
				return _nextNextLoader.LoadResource(resourceName);
			}
		}

		/// <summary>
		/// Get resource path according to which file would be loaded
		/// </summary>
		/// <param name="resourceName"></param>
		/// <returns></returns>
		protected virtual string GetResourcePath(string resourceName) => new DirectoryInfo(Path.Combine(_path, resourceName)).FullName;
	}
}
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

namespace Epam.FixAntenna.NetCore.FixEngine.Storage
{
	/// <summary>
	/// The default file locator implementation.
	/// </summary>
	internal class DefaultLogFileLocator : ILogFileLocator
	{
		private readonly string _directory;
		private readonly string _nameTemplate;

		/// <summary>
		/// Creates the <c>DefaultLogFileLocator</c>.
		/// </summary>
		public DefaultLogFileLocator(string dir, string nameTemplate)
		{
			_directory = dir;
			_nameTemplate = nameTemplate;
		}

		public virtual string GetDirectory()
		{
			return _directory;
		}

		public virtual string GetNameTemplate()
		{
			return _nameTemplate;
		}

		/// <inheritdoc />
		public virtual string GetFileName(SessionParameters details)
		{
			var fileName = string.Format(_nameTemplate, details.SessionId.ToString(), details.SenderCompId,
				details.TargetCompId, string.Empty, details.SessionQualifier);
			return Path.Combine(_directory, fileName);
		}
	}
}
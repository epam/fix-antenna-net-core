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
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Core.Tests
{
	static class FileHelper
	{
		public static int CountMatchedFiles(string dir, string mask, string[] matches = null)
		{
			if (string.IsNullOrWhiteSpace(dir))
				throw new ArgumentException("Directory name is not set.", nameof(dir));

			var regexMatches = Array.Empty<Regex>();
			if (matches != null)
			{
				regexMatches = matches.Select(match => new Regex(mask + match)).ToArray();
			}

			var defaultMask = regexMatches.Length == 0 ? mask ?? "*" : "*";
			return new DirectoryInfo(dir).GetFiles(defaultMask).Count(file => regexMatches.Length == 0 || regexMatches.Any(r => r.IsMatch(file.Name)));
		}

		public static void CreateConfigurationFileForSession(string filename, string content)
		{
			File.WriteAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, filename), content);
		}

		public static void DeleteConfigurationFileForSession(string filename)
		{
			var path = Path.Combine(TestContext.CurrentContext.TestDirectory, filename);
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}
}

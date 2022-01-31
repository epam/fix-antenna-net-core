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

using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using System.Linq;
using System.Xml;
using System;

namespace Epam.FixAntenna.Tester.NUnit
{
	public class XmlCaseReader
	{
		static XmlCaseReader()
		{
			// crazy fix for crazy NUnit
			// NUnit on net48 doesn't follow NUnitGlobalConfig.cs [OneTimeSetUp] while running inside Visual Studio
			// PLS check later and drop this ctor if problem fixed
			if (Environment.CurrentDirectory != TestContext.CurrentContext.TestDirectory)
			{
				Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
			}
		}

		public static IEnumerable<TestCaseData> GetCases(in string path)
		{
			var cases = new List<TestCaseData>();

			if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
				ReadCasesFromDirTree(path, cases);
			else
				cases.Add(CreateCase(path));

			return cases;
		}

		private static void ReadCasesFromDirTree(string path, List<TestCaseData> cases)
		{
			var files = Directory.GetFiles(path).Where(fn => fn.EndsWith(".xml", System.StringComparison.OrdinalIgnoreCase));
			foreach (var fn in files)
				cases.Add(CreateCase(fn));

			var dirs = Directory.GetDirectories(path);
			foreach (var d in dirs)
				ReadCasesFromDirTree(d, cases);
		}

		private static TestCaseData CreateCase(string fileName)
		{
			var testData = new TestCaseData(fileName);

			var reason = LoadDisableReason(fileName);

			if (!string.IsNullOrEmpty(reason))
			{
				testData.Ignore(reason);
			}
			
			return testData;
		}

		private static string LoadDisableReason(string fileName)
		{
			try
			{
				XmlReaderSettings settings = new XmlReaderSettings()
				{
					XmlResolver = new XmlUrlResolver(),
					ValidationType = ValidationType.DTD,
					DtdProcessing = DtdProcessing.Parse,
					IgnoreComments = true,
				};

				using (var reader = XmlReader.Create(fileName, settings))
				{
					XmlDocument doc = new XmlDocument();
					doc.Load(reader);
					XmlNode node = doc.SelectSingleNode("//cases/case"); // TODO: process all cases
					return  node?.Attributes.GetNamedItem("disableReason")?.InnerText;
				}
			}
			catch (Exception)	{ }
			return null;
		}
	}
}
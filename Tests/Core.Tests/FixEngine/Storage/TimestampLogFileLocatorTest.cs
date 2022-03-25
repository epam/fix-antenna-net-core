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
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Storage;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Storage
{
	internal class TimestampLogFileLocatorTest
	{
		[Test]
		public virtual void TestSessionIdAndTimeStamp()
		{
			var locator = new TimestampLogFileLocator(Path.DirectorySeparatorChar + "logs", "{0}-{3}.in");
			var p = new SessionParameters();
			p.SenderCompId = "s";
			p.TargetCompId = "t";
			var fileName = locator.GetFileName(p);
			var template = GetTemplate("s-t-");
			Assert.That(fileName, Does.Match(template),
				$"File name '{fileName}' do not matches with template '{template}'");
		}

		[Test]
		public virtual void TestFileName()
		{
			var locator = new TimestampLogFileLocator(Path.DirectorySeparatorChar + "logs", "{1}-{2}-{4}{3}.in");
			var p = new SessionParameters();
			p.SenderCompId = "s";
			p.TargetCompId = "t";
			p.SessionQualifier = "q";
			var fileName = locator.GetFileName(p);
			var template = GetTemplate("s-t-q");
			Assert.That(fileName, Does.Match(template),
				$"File name '{fileName}' do not matches with template '{template}'");
		}

		[Test]
		public virtual void TestFileNameEmptyQualifier()
		{
			var locator = new TimestampLogFileLocator(Path.DirectorySeparatorChar + "logs", "{1}-{2}-{4}{3}.in");
			var p = new SessionParameters();
			p.SenderCompId = "s";
			p.TargetCompId = "t";
			var fileName = locator.GetFileName(p);
			var template = GetTemplate("s-t-");
			Assert.That(fileName, Does.Match(template),
				$"File name '{fileName}' do not matches with template '{template}'");
		}

		private string GetTemplate(string prefix)
		{
			string template;
			if (Path.DirectorySeparatorChar == '\\')
			{
				template = "\\" + Path.DirectorySeparatorChar + "logs" + "\\" + Path.DirectorySeparatorChar + prefix +
							"\\d{15}.in";
			}
			else
			{
				template = Path.DirectorySeparatorChar + "logs" + Path.DirectorySeparatorChar + prefix + "\\d{15}.in";
			}

			return template;
		}
	}
}
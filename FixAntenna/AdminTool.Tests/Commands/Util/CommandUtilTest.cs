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
using Epam.FixAntenna.AdminTool.Commands.Util;
using Epam.FixAntenna.NetCore.Common;
using NUnit.Framework;

namespace Epam.FixAntenna.AdminTool.Tests.Commands.Util
{
	internal class CommandUtilTest
	{
		[Test]
		public void TestGetFixVersion()
		{
			Assert.AreEqual(FixVersion.Fix40, CommandUtil.GetFixVersion("FIX40"));
			Assert.AreEqual(FixVersion.Fix42, CommandUtil.GetFixVersion("FIX42"));
			Assert.AreEqual(FixVersion.Fix44, CommandUtil.GetFixVersion("FIX44"));
		}

		[Test]
		public void TestGetFixVersionTransport()
		{
			Assert.AreEqual(FixVersion.Fixt11, CommandUtil.GetFixVersion("FIXT11"));
			Assert.AreEqual(FixVersion.Fixt11, CommandUtil.GetFixVersion("FIXT11:FIX40"));
			Assert.AreEqual(FixVersion.Fixt11, CommandUtil.GetFixVersion("FIXT11:FIX50SP2"));
		}

		[Test]
		public void TestGetAppVersion()
		{
			Assert.AreEqual(FixVersion.Fix40, CommandUtil.GetAppVersion("FIXT11:FIX40"));
			Assert.AreEqual(FixVersion.Fix44, CommandUtil.GetAppVersion("FIXT11:FIX44"));
			Assert.AreEqual(FixVersion.Fix50, CommandUtil.GetAppVersion("FIXT11:FIX50"));
			Assert.AreEqual(FixVersion.Fix50Sp2, CommandUtil.GetAppVersion("FIXT11:FIX50SP2"));
		}

		[Test]
		public void TestGetSupportedVersions()
		{
			var versions = CommandUtil.GetSupportedVersions();
			var versionsMap = new HashSet<string>(versions);

			Assert.AreEqual(versions.Count, 13);
			Assert.IsNotNull(versionsMap.Contains("FIX40"));
			Assert.IsNotNull(versionsMap.Contains("FIX41"));
			Assert.IsNotNull(versionsMap.Contains("FIX42"));
			Assert.IsNotNull(versionsMap.Contains("FIX43"));
			Assert.IsNotNull(versionsMap.Contains("FIX44"));
			Assert.IsNotNull(versionsMap.Contains("FIXT11:FIX40"));
			Assert.IsNotNull(versionsMap.Contains("FIXT11:FIX41"));
			Assert.IsNotNull(versionsMap.Contains("FIXT11:FIX42"));
			Assert.IsNotNull(versionsMap.Contains("FIXT11:FIX43"));
			Assert.IsNotNull(versionsMap.Contains("FIXT11:FIX44"));
			Assert.IsNotNull(versionsMap.Contains("FIXT11:FIX50"));
			Assert.IsNotNull(versionsMap.Contains("FIXT11:FIX50SP1"));
			Assert.IsNotNull(versionsMap.Contains("FIXT11:FIX50SP2"));
		}
	}
}
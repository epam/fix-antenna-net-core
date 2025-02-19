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
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.AdminTool.Tests.Commands.Util
{
	internal class CommandUtilTest
	{
		[Test]
		public void TestGetFixVersion()
		{
			ClassicAssert.AreEqual(FixVersion.Fix40, CommandUtil.GetFixVersion("FIX40"));
			ClassicAssert.AreEqual(FixVersion.Fix42, CommandUtil.GetFixVersion("FIX42"));
			ClassicAssert.AreEqual(FixVersion.Fix44, CommandUtil.GetFixVersion("FIX44"));
		}

		[Test]
		public void TestGetFixVersionTransport()
		{
			ClassicAssert.AreEqual(FixVersion.Fixt11, CommandUtil.GetFixVersion("FIXT11"));
			ClassicAssert.AreEqual(FixVersion.Fixt11, CommandUtil.GetFixVersion("FIXT11:FIX40"));
			ClassicAssert.AreEqual(FixVersion.Fixt11, CommandUtil.GetFixVersion("FIXT11:FIX50SP2"));
		}

		[Test]
		public void TestGetAppVersion()
		{
			ClassicAssert.AreEqual(FixVersion.Fix40, CommandUtil.GetAppVersion("FIXT11:FIX40"));
			ClassicAssert.AreEqual(FixVersion.Fix44, CommandUtil.GetAppVersion("FIXT11:FIX44"));
			ClassicAssert.AreEqual(FixVersion.Fix50, CommandUtil.GetAppVersion("FIXT11:FIX50"));
			ClassicAssert.AreEqual(FixVersion.Fix50Sp2, CommandUtil.GetAppVersion("FIXT11:FIX50SP2"));
		}

		[Test]
		public void TestGetSupportedVersions()
		{
			var versions = CommandUtil.GetSupportedVersions();
			var versionsMap = new HashSet<string>(versions);

			ClassicAssert.AreEqual(versions.Count, 13);
			ClassicAssert.IsNotNull(versionsMap.Contains("FIX40"));
			ClassicAssert.IsNotNull(versionsMap.Contains("FIX41"));
			ClassicAssert.IsNotNull(versionsMap.Contains("FIX42"));
			ClassicAssert.IsNotNull(versionsMap.Contains("FIX43"));
			ClassicAssert.IsNotNull(versionsMap.Contains("FIX44"));
			ClassicAssert.IsNotNull(versionsMap.Contains("FIXT11:FIX40"));
			ClassicAssert.IsNotNull(versionsMap.Contains("FIXT11:FIX41"));
			ClassicAssert.IsNotNull(versionsMap.Contains("FIXT11:FIX42"));
			ClassicAssert.IsNotNull(versionsMap.Contains("FIXT11:FIX43"));
			ClassicAssert.IsNotNull(versionsMap.Contains("FIXT11:FIX44"));
			ClassicAssert.IsNotNull(versionsMap.Contains("FIXT11:FIX50"));
			ClassicAssert.IsNotNull(versionsMap.Contains("FIXT11:FIX50SP1"));
			ClassicAssert.IsNotNull(versionsMap.Contains("FIXT11:FIX50SP2"));
		}
	}
}
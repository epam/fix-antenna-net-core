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

using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.FixEngine;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.FixEngine
{
	internal class FixVersionUtilsTest
	{
		[Test]
		public virtual void GetFixtVersionFixtVersionValid()
		{
			var fixVersionStr = "FIXT.1.1:FIX.4.4";
			var fixVersionUtils = new FixVersionUtils(fixVersionStr);
			var fixtVersion11 = fixVersionUtils.GetFixtVersion();
			ClassicAssert.IsNotNull(fixtVersion11,
				"FixVersion can't be null. FIXTVersion not parse from string \"" + fixVersionStr + "\"");
			ClassicAssert.AreEqual(FixVersion.Fixt11, fixtVersion11,
				"FIX version must be FIXT.1.1. FIXTVersion not parse from string \"" + fixVersionStr + "\"");

			var fixVersion44 = fixVersionUtils.GetFixtVersion();
			ClassicAssert.IsNotNull(fixtVersion11,
				"FixVersion can't be null. FixVersion not parse from string \"" + fixVersionStr + "\"");
			ClassicAssert.AreEqual(FixVersion.Fixt11, fixtVersion11,
				"FIX version must be FIX.4.4. FixVersion not parse from string \"" + fixVersionStr + "\"");
		}

		[Test]
		public virtual void GetFixtVersionFixtVersionNotExist()
		{
			var fixVersionStr = "FIX.4.4";
			var fixVersionUtils = new FixVersionUtils(fixVersionStr);
			var fixtVersion11 = fixVersionUtils.GetFixtVersion();
			ClassicAssert.IsNull(fixtVersion11,
				"FixVersion must be null. FIXTVersion not declared in string \"" + fixVersionStr + "\"");
		}

		[Test]
		public virtual void GetFixtVersionFixtVersionNotValid()
		{
			ClassicAssert.Throws<ArgumentException>(() =>
			{
				var fixVersionStr = "NOT.VALID.VALUE.TEST:FIX.4.4";
				var fixVersionUtils = new FixVersionUtils(fixVersionStr);
				var fixtVersion11 = fixVersionUtils.GetFixtVersion();
			});
		}

		[Test]
		public virtual void GetFixVersionFixVersionValid()
		{
			var fixVersionStr = "FIX.5.0";
			var fixVersionUtils = new FixVersionUtils(fixVersionStr);
			var fixVersion50 = fixVersionUtils.GetFixVersion();
			ClassicAssert.IsNotNull(fixVersion50,
				"FixVersion can't be null. FixVersion not parse from string \"" + fixVersionStr + "\"");
			ClassicAssert.AreEqual(FixVersion.Fix50, fixVersion50,
				"FIX version must be FIX.5.0. FixVersion not parse from string \"" + fixVersionStr + "\"");

			var fixtVersionStr = "FIXT.1.1:FIX.4.4";
			var fixtVersionUtils = new FixVersionUtils(fixtVersionStr);
			var fixVersion44 = fixtVersionUtils.GetFixVersion();
			ClassicAssert.IsNotNull(fixVersion44,
				"FixVersion can't be null. FixVersion not parse from string \"" + fixVersionStr + "\"");
			ClassicAssert.AreEqual(FixVersion.Fix44, fixVersion44,
				"FIX version must be FIX.4.4. FixVersion not parse from string \"" + fixVersionStr + "\"");
		}

		[Test]
		public virtual void GetFixVersionFixVersionNotValid()
		{
			ClassicAssert.Throws<ArgumentException>(() =>
			{
				var fixVersionStr = "NOT.VALID.VALUE.TEST";
				var fixVersionUtils = new FixVersionUtils(fixVersionStr);
				var fixVersion11 = fixVersionUtils.GetFixVersion();
			});
		}
	}
}
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

using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Validation.Utils;
using NUnit.Framework;

namespace Epam.FixAntenna.Fix.Validation.Engine.Utils
{
	[TestFixture]
	public class FixUtilFactoryTest
	{
		private FixUtil _fixUtil;

		private FixUtil BuildFixUtil(FixVersion fixVersion)
		{
			var fixVersionContainer = FixVersionContainer.GetFixVersionContainer(fixVersion);
			return FixUtilFactory.Instance.GetFixUtil(fixVersionContainer);
		}

		private FixUtil BuildFixUtil(FixVersion fixVersion, FixVersion appVersion)
		{
			var fixVersionContainer = FixVersionContainer.GetFixVersionContainer(fixVersion);
			var appVersionContainer = FixVersionContainer.GetFixVersionContainer(appVersion);
			return BuildFixUtil(fixVersionContainer, appVersionContainer);
		}

		private FixUtil BuildFixUtil(FixVersionContainer fixVersionContainer, FixVersionContainer appVersionContainer)
		{
			return FixUtilFactory.Instance.GetFixUtil(fixVersionContainer, appVersionContainer);
		}

		[Test]
		public virtual void Test43()
		{
			_fixUtil = BuildFixUtil(FixVersion.Fix43);
			Assert.IsNotNull(_fixUtil);

			Assert.IsTrue(_fixUtil.GetFieldsByMessageType("C").Count > 0);
		}

		[Test]
		public virtual void TestCaching()
		{
			var v1 = new FixVersionContainer("Fix40custom", FixVersion.Fix40,
				"custom/fixdic40custom.xml");
			var fixUtil1 = BuildFixUtil(v1, null);

			var v2 = new FixVersionContainer("Fix40custom", FixVersion.Fix40,
				"custom/fixdic40custom.xml");
			var fixUtil2 = BuildFixUtil(v2, null);

			Assert.That(fixUtil1, Is.SameAs(fixUtil2));
		}

		[Test]
		public virtual void TestGetCustomFix40()
		{
			var versionContainer = new FixVersionContainer("Fix40custom", FixVersion.Fix40,
				"custom/fixdic40custom.xml");
			_fixUtil = BuildFixUtil(versionContainer, null);
			Assert.IsTrue(_fixUtil.GetFieldsByMessageType("0").Count > 0);
		}

		[Test]
		public virtual void TestGetFixUtil40()
		{
			_fixUtil = BuildFixUtil(FixVersion.Fix40);
			Assert.IsNotNull(_fixUtil);

			Assert.IsTrue(_fixUtil.GetFieldsByMessageType("C").Count > 0);
		}

		[Test]
		public virtual void TestGetFixUtil41()
		{
			_fixUtil = BuildFixUtil(FixVersion.Fix41);
			Assert.IsNotNull(_fixUtil);

			Assert.IsTrue(_fixUtil.GetFieldsByMessageType("C").Count > 0);
		}

		[Test]
		public virtual void TestGetFixUtil42()
		{
			_fixUtil = BuildFixUtil(FixVersion.Fix42);
			Assert.IsNotNull(_fixUtil);

			Assert.IsTrue(_fixUtil.GetFieldsByMessageType("C").Count > 0);
		}

		[Test]
		public virtual void TestGetFixUtil44()
		{
			_fixUtil = BuildFixUtil(FixVersion.Fix44);
			Assert.IsNotNull(_fixUtil);

			Assert.IsTrue(_fixUtil.GetFieldsByMessageType("C").Count > 0);
		}

		[Test]
		public virtual void TestGetFixUtil50()
		{
			_fixUtil = BuildFixUtil(FixVersion.Fix50);
			Assert.IsNotNull(_fixUtil);

			Assert.IsTrue(_fixUtil.GetFieldsByMessageType("C").Count > 0);
		}

		[Test]
		public virtual void TestGetFixUtil50Fixt()
		{
			_fixUtil = BuildFixUtil(FixVersion.Fixt11, FixVersion.Fix50);
			Assert.IsNotNull(_fixUtil);

			Assert.IsTrue(_fixUtil.GetFieldsByMessageType("A").Count > 0);
		}

		[Test]
		public virtual void TestGetFixUtil50Sp1()
		{
			_fixUtil = BuildFixUtil(FixVersion.Fix50Sp1);
			Assert.IsNotNull(_fixUtil);

			Assert.IsTrue(_fixUtil.GetFieldsByMessageType("C").Count > 0);
		}

		[Test]
		public virtual void TestGetFixUtil50Sp1Fixt()
		{
			_fixUtil = BuildFixUtil(FixVersion.Fixt11, FixVersion.Fix50Sp1);
			Assert.IsNotNull(_fixUtil);

			Assert.IsTrue(_fixUtil.GetFieldsByMessageType("A").Count > 0);
		}

		[Test]
		public virtual void TestGetFixUtil50Sp2()
		{
			_fixUtil = BuildFixUtil(FixVersion.Fix50Sp2);
			Assert.IsNotNull(_fixUtil);

			Assert.IsTrue(_fixUtil.GetFieldsByMessageType("C").Count > 0);
		}

		[Test]
		public virtual void TestGetFixUtil50Sp2Fixt()
		{
			_fixUtil = BuildFixUtil(FixVersion.Fixt11, FixVersion.Fix50Sp2);
			Assert.IsNotNull(_fixUtil);

			Assert.IsTrue(_fixUtil.GetFieldsByMessageType("A").Count > 0);
		}

		[Test]
		public virtual void TestNoCachingForDifferentVersions()
		{
			var v1 = new FixVersionContainer("Fix40custom1", FixVersion.Fix40,
				"custom/fixdic40custom.xml");
			var fixUtil1 = BuildFixUtil(v1, null);

			var v2 = new FixVersionContainer("Fix40custom2", FixVersion.Fix40,
				"custom/fixdic40custom.xml");
			var fixUtil2 = BuildFixUtil(v2, null);

			Assert.AreNotSame(fixUtil1, fixUtil2);
		}
	}
}
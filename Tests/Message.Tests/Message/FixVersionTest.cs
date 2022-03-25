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

using NUnit.Framework;

namespace Epam.FixAntenna.Message.Tests
{
	/// <summary>
	/// This test used for test FIXVersion
	/// </summary>
	[TestFixture]
	internal class FixVersionTest
	{
		[Test]
		public virtual void TestCompareWithFix41()
		{
			var fix41 = FixVersion.Fix41;

			Assert.IsTrue(fix41.CompareTo(FixVersion.Fix41) == 0);
			Assert.IsTrue(FixVersion.Fix41.CompareTo(fix41) == 0);

			Assert.IsTrue(fix41.CompareTo(FixVersion.Fix42) < 0);
			Assert.IsTrue(FixVersion.Fix42.CompareTo(fix41) > 0);

			Assert.IsTrue(fix41.CompareTo(FixVersion.Fix43) < 0);
			Assert.IsTrue(FixVersion.Fix43.CompareTo(fix41) > 0);

			Assert.IsTrue(fix41.CompareTo(FixVersion.Fix44) < 0);
			Assert.IsTrue(FixVersion.Fix44.CompareTo(fix41) > 0);

			Assert.IsTrue(fix41.CompareTo(FixVersion.Fix50) < 0);
			Assert.IsTrue(FixVersion.Fix50.CompareTo(fix41) > 0);

			Assert.IsTrue(fix41.CompareTo(FixVersion.Fix50Sp1) < 0);
			Assert.IsTrue(FixVersion.Fix50Sp1.CompareTo(fix41) > 0);

			Assert.IsTrue(fix41.CompareTo(FixVersion.Fix50Sp2) < 0);
			Assert.IsTrue(FixVersion.Fix50Sp2.CompareTo(fix41) > 0);

			Assert.IsTrue(fix41.CompareTo(FixVersion.Fixt11) < 0);
			Assert.IsTrue(FixVersion.Fixt11.CompareTo(fix41) > 0);
		}

		[Test]
		public virtual void TestCompareWithFix44()
		{
			var fix44 = FixVersion.Fix44;

			Assert.IsTrue(fix44.CompareTo(FixVersion.Fix41) > 0);
			Assert.IsTrue(FixVersion.Fix41.CompareTo(fix44) < 0);

			Assert.IsTrue(fix44.CompareTo(FixVersion.Fix42) > 0);
			Assert.IsTrue(FixVersion.Fix42.CompareTo(fix44) < 0);

			Assert.IsTrue(fix44.CompareTo(FixVersion.Fix43) > 0);
			Assert.IsTrue(FixVersion.Fix43.CompareTo(fix44) < 0);

			Assert.IsTrue(fix44.CompareTo(FixVersion.Fix44) == 0);
			Assert.IsTrue(FixVersion.Fix44.CompareTo(fix44) == 0);

			Assert.IsTrue(fix44.CompareTo(FixVersion.Fix50) < 0);
			Assert.IsTrue(FixVersion.Fix50.CompareTo(fix44) > 0);

			Assert.IsTrue(fix44.CompareTo(FixVersion.Fix50Sp1) < 0);
			Assert.IsTrue(FixVersion.Fix50Sp1.CompareTo(fix44) > 0);

			Assert.IsTrue(fix44.CompareTo(FixVersion.Fix50Sp2) < 0);
			Assert.IsTrue(FixVersion.Fix50Sp2.CompareTo(fix44) > 0);

			Assert.IsTrue(fix44.CompareTo(FixVersion.Fixt11) < 0);
			Assert.IsTrue(FixVersion.Fixt11.CompareTo(fix44) > 0);
		}

		[Test]
		public virtual void TestCompareWithFixt()
		{
			var fixt = FixVersion.Fixt11;

			Assert.IsTrue(fixt.CompareTo(FixVersion.Fix41) > 0);
			Assert.IsTrue(FixVersion.Fix41.CompareTo(fixt) < 0);

			Assert.IsTrue(fixt.CompareTo(FixVersion.Fix42) > 0);
			Assert.IsTrue(FixVersion.Fix42.CompareTo(fixt) < 0);

			Assert.IsTrue(fixt.CompareTo(FixVersion.Fix43) > 0);
			Assert.IsTrue(FixVersion.Fix43.CompareTo(fixt) < 0);

			Assert.IsTrue(fixt.CompareTo(FixVersion.Fix44) > 0);
			Assert.IsTrue(FixVersion.Fix44.CompareTo(fixt) < 0);

			Assert.IsTrue(fixt.CompareTo(FixVersion.Fix50) > 0);
			Assert.IsTrue(FixVersion.Fix50.CompareTo(fixt) < 0);

			Assert.IsTrue(fixt.CompareTo(FixVersion.Fix50Sp1) > 0);
			Assert.IsTrue(FixVersion.Fix50Sp1.CompareTo(fixt) < 0);

			Assert.IsTrue(fixt.CompareTo(FixVersion.Fix50Sp2) > 0);
			Assert.IsTrue(FixVersion.Fix50Sp2.CompareTo(fixt) < 0);

			Assert.IsTrue(fixt.CompareTo(FixVersion.Fixt11) == 0);
			Assert.IsTrue(FixVersion.Fixt11.CompareTo(fixt) == 0);
		}
	}
}
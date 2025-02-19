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

using System.Reflection;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Configuration
{
	public class FixVersionContainerTest
	{
		[Test]
		public virtual void TestEqualsContractMet()
		{
			ClassicAssert.AreEqual(BuildDefault(), BuildDefault());
			ClassicAssert.IsFalse(BuildDefault().Equals(BuildCustom(nameof(FixVersionContainer.DictionaryId), "custom")));
			ClassicAssert.AreEqual(BuildDefault(), BuildCustom(nameof(FixVersionContainer.FixVersion), FixVersion.Fix50));
			ClassicAssert.AreEqual(BuildDefault(), BuildCustom(nameof(FixVersionContainer.DictionaryFile), "dict"));
			ClassicAssert.AreEqual(BuildDefault(), BuildCustom(nameof(FixVersionContainer.ExtensionFile), "ext"));
		}

		[Test]
		public virtual void TestHashCodeContractMet()
		{
			ClassicAssert.AreEqual(BuildDefault().GetHashCode(), BuildDefault().GetHashCode());
			ClassicAssert.IsTrue(BuildDefault().GetHashCode() != BuildCustom(nameof(FixVersionContainer.DictionaryId), "custom").GetHashCode());
			ClassicAssert.AreEqual(BuildDefault().GetHashCode(), BuildCustom(nameof(FixVersionContainer.FixVersion), FixVersion.Fix50).GetHashCode());
			ClassicAssert.AreEqual(BuildDefault().GetHashCode(), BuildCustom(nameof(FixVersionContainer.DictionaryFile), "dict").GetHashCode());
			ClassicAssert.AreEqual(BuildDefault().GetHashCode(), BuildCustom(nameof(FixVersionContainer.ExtensionFile), "ext").GetHashCode());
		}

		[Test]
		public virtual void ItShouldDetermineFixVersionAutomaticallyIfItIsUnknownT11()
		{
			var fixVersionContainer = FixVersionContainer.NewBuilder().SetDictionaryFile("fixdict11.xml").Build();
			ClassicAssert.AreEqual(FixVersion.Fixt11, fixVersionContainer.FixVersion);
		}

		[Test]
		public virtual void ItShouldDetermineFixVersionAutomaticallyIfItIsUnknown44()
		{
			var fixVersionContainer = FixVersionContainer.NewBuilder().SetDictionaryFile("fixdic44.xml").Build();
			ClassicAssert.AreEqual(FixVersion.Fix44, fixVersionContainer.FixVersion);
		}

		private FixVersionContainer BuildDefault()
		{
			return FixVersionContainer.GetFixVersionContainer(FixVersion.Fix40);
		}

		private FixVersionContainer BuildCustom(string property, object value)
		{
			var versionContainer = BuildDefault();
			var declaredField = typeof(FixVersionContainer).GetProperty(property, BindingFlags.Instance | BindingFlags.Public);
			declaredField.SetValue(versionContainer, value);
			return versionContainer;
		}
	}
}
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
using Epam.FixAntenna.NetCore.Dictionary;
using Epam.FixAntenna.NetCore.Validation.Entities;
using NUnit.Framework;

namespace Epam.FixAntenna.Fix.Dictionary
{
	public class FixDictionaryFactoryTest
	{
		private readonly FixDictionaryFactory _factory = FixDictionaryFactory.Instance;

		[TearDown]
		public virtual void After()
		{
			_factory.CleanDictionaryCache();
		}

		[Test]
		public virtual void TestGetDictionaries()
		{
			var fixVersionContainer = FixVersionContainer.GetFixVersionContainer(FixVersion.Fix41);
			var dictionaries = _factory.GetDictionaries(fixVersionContainer, null);
			Assert.IsNotNull(dictionaries);
			Assert.IsNotNull(dictionaries.Dictionaries);
			Assert.AreEqual(dictionaries.Dictionaries.Count, 1);
		}

		[Test]
		public virtual void TestReloadDictionaries()
		{
			var baseFix40 = FixVersionContainer.GetFixVersionContainer(FixVersion.Fix40);
			var dictionaries = _factory.GetDictionaries(baseFix40, null);
			var msgDefCount = ((Fixdic)dictionaries.Dictionaries[0]).Msgdic.Msgdef.Count;
			Assert.AreEqual(27, msgDefCount);

			//reload dictionary
			var customFix40 = new FixVersionContainer(baseFix40.DictionaryId, FixVersion.Fix40,
				"Custom/fixdic40custom.xml");
			_factory.LoadDictionary(customFix40, null);
			dictionaries = _factory.GetDictionaries(baseFix40, null);
			var newMsgDefCount = ((Fixdic)dictionaries.Dictionaries[0]).Msgdic.Msgdef.Count;
			Assert.AreEqual(2, newMsgDefCount);
		}
	}
}
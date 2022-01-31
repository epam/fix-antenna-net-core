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
using Epam.FixAntenna.NetCore.Validation;
using Epam.FixAntenna.NetCore.Validation.Entities;
using NUnit.Framework;

namespace Epam.FixAntenna.Fix.Validation.Engine
{
	[TestFixture]
	public class ValidationEngineTest
	{
		[TearDown]
		public virtual void After()
		{
			var fix42 = FixVersionContainer.GetFixVersionContainer(FixVersion.Fix42);
			FixDictionaryFactory.Instance.LoadDictionary(fix42, null);
		}

		[Test]
		[Ignore("Additional dictionaries is not supported yet.")]
		public virtual void TestPreloadAndUpdateDictionary()
		{
			var fix42 = FixVersionContainer.GetFixVersionContainer(FixVersion.Fix42);
			var dictionariesBefore = FixDictionaryFactory.Instance.GetDictionaries(fix42, null);
			var fixdicBefore = (Fixdic)dictionariesBefore.Dictionaries[0];
			Assert.AreEqual(400, fixdicBefore.Fielddic.Fielddef.Count);
			Assert.AreEqual(46, fixdicBefore.Msgdic.Msgdef.Count);

			ValidationEngine.PreloadDictionary(FixVersion.Fix42, "minimal/test42min.xml", true);
			ValidationEngine.PreloadDictionary(FixVersion.Fix42, "minimal/test42minadditional.xml",
				false);

			var dictionaries = FixDictionaryFactory.Instance.GetDictionaries(fix42, null);
			var fixdic = (Fixdic)dictionaries.Dictionaries[0];
			Assert.AreEqual(3, fixdic.Fielddic.Fielddef.Count);
			Assert.AreEqual(1, fixdic.Msgdic.Msgdef.Count);
			Assert.AreEqual(3, fixdic.Msgdic.Msgdef[0].FieldOrDescrOrAlias.Count);
		}

		[Test]
		public virtual void TestPreloadDictionary()
		{
			var fix42 = FixVersionContainer.GetFixVersionContainer(FixVersion.Fix42);
			var dictionariesBefore = FixDictionaryFactory.Instance.GetDictionaries(fix42, null);
			var fixdicBefore = (Fixdic)dictionariesBefore.Dictionaries[0];
			Assert.AreEqual(400, fixdicBefore.Fielddic.Fielddef.Count);
			Assert.AreEqual(46, fixdicBefore.Msgdic.Msgdef.Count);

			ValidationEngine.PreloadDictionary(FixVersion.Fix42, "minimal/test42min.xml", true);

			var dictionaries = FixDictionaryFactory.Instance.GetDictionaries(fix42, null);
			var fixdic = (Fixdic)dictionaries.Dictionaries[0];
			Assert.AreEqual(2, fixdic.Fielddic.Fielddef.Count);
			Assert.AreEqual(1, fixdic.Msgdic.Msgdef.Count);
			Assert.AreEqual(2, fixdic.Msgdic.Msgdef[0].FieldOrDescrOrAlias.Count);
		}
	}
}
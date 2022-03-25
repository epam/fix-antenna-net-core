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
using System.Collections.Generic;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Dictionary;
using Epam.FixAntenna.NetCore.Validation.Entities;
using NUnit.Framework;

namespace Epam.FixAntenna.Fix.Dictionary
{
	public class DictionaryBuilderTest
	{
		internal DictionaryBuilder Builder = new DictionaryBuilder();

		[TearDown]
		public void After()
		{
			Builder.CleanCache();
		}

		[Test]
		public void BuildFix44()
		{
			var fixVersionContainer = FixVersionContainer.GetFixVersionContainer(FixVersion.Fix44);
			var fixdic = (Fixdic)Builder.BuildDictionary(fixVersionContainer, false);
			IList<Valblockdef> valblockdefList = fixdic.Fielddic.Valblockdef;
			foreach (var varBlockDef in valblockdefList)
			{
				if ("IOIQty".Equals(varBlockDef.Name))
				{
					IList<object> itemOrRangeOrDescr = varBlockDef.ItemOrRangeOrDescr;
					Assert.AreEqual(4, itemOrRangeOrDescr.Count);
				}
			}
		}

		[Test]
		public void BuildAdditionalFix44Custom()
		{
			var fixVersionContainer =
				new FixVersionContainer("FIX44Custom", FixVersion.Fix44, "Additional/custom_44.xml");
			var fixdic = (Fixdic)Builder.BuildDictionary(fixVersionContainer, false);
			var fielddic = fixdic.Fielddic;
			Assert.AreEqual(3, fielddic.Fielddef.Count);
			Assert.AreEqual(3, fixdic.Msgdic.Msgdef.Count);
		}

		[Test]
		public void TestReloadDictionary()
		{
			var base40 = FixVersionContainer.GetFixVersionContainer(FixVersion.Fix40);
			var baseFiXdic = (Fixdic)Builder.BuildDictionary(base40, false);
			Assert.AreEqual(27, baseFiXdic.Msgdic.Msgdef.Count);

			var custom40 = new FixVersionContainer(base40.DictionaryId, FixVersion.Fix40,
				"Custom/fixdic40custom.xml");
			var customFiXdic = (Fixdic)Builder.BuildDictionary(custom40, true);
			Assert.AreEqual(2, customFiXdic.Msgdic.Msgdef.Count);
		}

		[Test]
		public void BuildQuickFix44()
		{
			var fixVersionContainer = new FixVersionContainer("QFIX44", FixVersion.Fix44, "Additional/qFIX44.xml");
			var fixdic = (Fixdic)Builder.BuildDictionary(fixVersionContainer, false);
			var fielddic = fixdic.Fielddic;
			Assert.AreEqual(912, fielddic.Fielddef.Count);
			Assert.AreEqual(93, fixdic.Msgdic.Msgdef.Count);
		}

		[Test]
		public void TestQfixType()
		{
			var fixVersionContainer = new FixVersionContainer("QFIX44", FixVersion.Fix44, "qfix-wrong-type.xml");
			Assert.Throws(typeof(InvalidOperationException), () => Builder.BuildDictionary(fixVersionContainer, false));
		}

		[Test]
		public void TestQfixMajor()
		{
			var fixVersionContainer = new FixVersionContainer("QFIX44", FixVersion.Fix44, "qfix-wrong-major.xml");
			Assert.Throws(typeof(InvalidOperationException), () => Builder.BuildDictionary(fixVersionContainer, false));
		}

		[Test]
		public void TestQfixMinor()
		{
			var fixVersionContainer = new FixVersionContainer("QFIX44", FixVersion.Fix44, "qfix-wrong-minor.xml");
			Assert.Throws(typeof(InvalidOperationException), () => Builder.BuildDictionary(fixVersionContainer, false));
		}

		[Test]
		public void TestQfixIdForSystemFileName()
		{
			var fixVersionContainer = new FixVersionContainer("FIX44", FixVersion.Fix44, "Additional/fixdic44.xml");
			var fixdic = (Fixdic)Builder.BuildDictionary(fixVersionContainer, true);
			var fielddic = fixdic.Fielddic;
			Assert.AreEqual(1, fielddic.Fielddef.Count);
			Assert.AreEqual(1, fixdic.Msgdic.Msgdef.Count);
		}
	}
}
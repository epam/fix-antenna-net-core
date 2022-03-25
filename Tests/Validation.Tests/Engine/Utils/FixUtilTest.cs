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

using System.Collections;
using System.Collections.Generic;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Dictionary;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation.Entities;
using Epam.FixAntenna.NetCore.Validation.Exceptions;
using Epam.FixAntenna.NetCore.Validation.Utils;
using Epam.FixAntenna.Validation.Tests.Engine.Validation.Test;

using NUnit.Framework;

namespace Epam.FixAntenna.Validation.Tests.Engine.Utils
{
	[TestFixture]
	internal class FixUtilTest : GenericValidationTestStub
	{
		private FixUtil _util;

		private IList<object> FingGroup(IList<object> list, int leadingTagId)
		{
			for (var i = 0; i < list.Count; i++)
			{
				var obj = list[i];
				if (obj is Fielddef && ((Fielddef)obj).Tag == leadingTagId)
				{
					var nextObj = list[i + 1];
					if (nextObj is IList)
					{
						return (IList<object>)nextObj;
					}

					return null;
				}
			}

			//not found
			return null;
		}

		private bool IsTagExist(int[] tags, Fielddef fielddef)
		{
			foreach (var tag in tags)
			{
				if (fielddef.Tag == tag)
				{
					return true;
				}
			}

			return false;
		}

		public override FixInfo GetFixInfo()
		{
			return new FixInfo(FixVersion.Fix42);
		}

		public virtual FixInfo GetFixInfo(FixVersion fixVersion)
		{
			return new FixInfo(fixVersion);
		}

		private FixUtil BuildFixUtil(FixVersion fixVersion)
		{
			var fixVersionContainer = FixVersionContainer.GetFixVersionContainer(fixVersion);
			return FixUtilFactory.Instance.GetFixUtil(fixVersionContainer);
		}

		[Test]
		public virtual void FindGroup()
		{
			_util = BuildFixUtil(FixVersion.Fix42);
			var group = _util.FindGroup("D", 79);
			Assert.IsNotNull(group, "Group not found");
		}

		[Test]
		public virtual void GetGroupFieldDefs()
		{
			_util = BuildFixUtil(FixVersion.Fix42);
			var list = _util.GetGroupFieldDefs("D", 79);
			Assert.AreEqual(2, list.Count, "Not all fields in group");
		}

		[Test]
		public virtual void GetGroupTagsWithInternalGroups()
		{
			var fixMessage = RawFixUtil.GetFixMessage(
				"1310=1\u00011301=DSMD\u00011300=STRING\u00011205=1\u00011206=1.1\u00011207=1.1\u00011208=1.1\u00011209=0\u00011234=1\u00011093=1\u00011231=1.1\u00011306=0\u00011148=1.1\u00011149=1.1\u00011150=1.1\u0001827=0\u0001562=1.1\u00011140=1.1\u00011143=1.1\u00011144=0\u00011245=AFA\u0001561=1.1\u00011377=0\u00011378=0\u0001423=1\u00011309=1\u0001336=1\u0001625=1\u00011237=1\u000140=1\u00011239=1\u000159=0\u00011232=1\u00011308=0\u00011235=1\u00011142=STRING\u0001574=M3\u00011141=1\u00011022=STRING\u0001264=0\u00011021=1\u00011312=1\u00011210=1\u00011211=STRING\u00011201=1\u00011223=STRING\u00011202=1.1\u00011203=1.1\u00011204=1.1\u00011304=0\u00011236=1\u00011222=STRING\u00011303=0\u00011302=0\u00011241=200101w2\u00011226=200101w2\u00011229=1\u0001"
					.AsByteArray());
			_util = BuildFixUtil(FixVersion.Fix50Sp2);

			var allTags1301 = _util.GetGroupTagsWithInternalGroups("BP", 1310, fixMessage);
			Assert.AreEqual(56, allTags1301.Count);

			var tickRulesGroup = _util.GetGroupTagsWithInternalGroups("BP", 1205, fixMessage);
			Assert.AreEqual(4, tickRulesGroup.Count);
		}

		[Test]
		public virtual void GetGroupTagsWithOutInternalGroups()
		{
			var fixMessage = RawFixUtil.GetFixMessage(
				"1310=1\u00011301=DSMD\u00011300=STRING\u00011205=1\u00011206=1.1\u00011207=1.1\u00011208=1.1\u00011209=0\u00011234=1\u00011093=1\u00011231=1.1\u00011306=0\u00011148=1.1\u00011149=1.1\u00011150=1.1\u0001827=0\u0001562=1.1\u00011140=1.1\u00011143=1.1\u00011144=0\u00011245=AFA\u0001561=1.1\u00011377=0\u00011378=0\u0001423=1\u00011309=1\u0001336=1\u0001625=1\u00011237=1\u000140=1\u00011239=1\u000159=0\u00011232=1\u00011308=0\u00011235=1\u00011142=STRING\u0001574=M3\u00011141=1\u00011022=STRING\u0001264=0\u00011021=1\u00011312=1\u00011210=1\u00011211=STRING\u00011201=1\u00011223=STRING\u00011202=1.1\u00011203=1.1\u00011204=1.1\u00011304=0\u00011236=1\u00011222=STRING\u00011303=0\u00011302=0\u00011241=200101w2\u00011226=200101w2\u00011229=1\u0001"
					.AsByteArray());
			_util = BuildFixUtil(FixVersion.Fix50Sp2);

			var onlyTags1301 = _util.GetGroupTagsWithOutInternalGroups("BP", 1310, fixMessage);
			Assert.AreEqual(16, onlyTags1301.Count);
		}

		[Test]
		public virtual void GetInnerInBlockGroupFieldDefs()
		{
			_util = BuildFixUtil(FixVersion.Fix44);
			var list = _util.GetGroupFieldDefs("D", 448);
			Assert.AreEqual(6, list.Count, "Not all fields in group");
		}

		[Test]
		public virtual void GetInnerInGroupGroupFieldDefs()
		{
			_util = BuildFixUtil(FixVersion.Fix44);
			var list = _util.GetGroupFieldDefs("D", 524);
			Assert.AreEqual(6, list.Count, "Not all fields in group");
		}

		[Test]
		public virtual void GetMessageFieldDefHier()
		{
			_util = BuildFixUtil(FixVersion.Fix42);
			var list = _util.GetMessageFieldDefHier("D");
			Assert.AreEqual(103, list.Count, "Not all fields in group");
		}

		[Test]
		public virtual void GetMessageFieldDefHierHasNestedGroupInBoby()
		{
			_util = BuildFixUtil(FixVersion.Fix44);
			var list = _util.GetMessageFieldDefHier("V");
			var symRgList = FingGroup(list, 146);
			Assert.IsNotNull(symRgList, "Required group is missed");
			var securityAltRgList = FingGroup(symRgList, 454);
			Assert.IsNotNull(securityAltRgList, "Nested group is missed");
			Assert.AreEqual(2, securityAltRgList.Count, "Nested group has invalid size");
		}

		[Test]
		public virtual void GetMessageFieldDefHierHasNestedGroupInSmh()
		{
			_util = BuildFixUtil(FixVersion.Fix44);
			var list = _util.GetMessageFieldDefHier("V");
			var noHopsRgList = FingGroup(list, 627);
			Assert.IsNotNull(noHopsRgList, "Required group is missed");
			Assert.AreEqual(3, noHopsRgList.Count, "Nested group has invalid size");
		}

		/// <summary>
		/// test getting tag id by field name
		/// </summary>
		[Test]
		public virtual void TestGetTagIdByFieldName()
		{
			_util = BuildFixUtil(FixVersion.Fix42);
			var tagId = _util.GetFieldTagByName("NetMoney");
			Assert.AreEqual(118, tagId, "Error in getting tag id by field name: normal sensitive");

			tagId = _util.GetFieldTagByName("netmoney");
			Assert.AreEqual(118, tagId, "Error in getting tag id by field name: lower case");

			tagId = _util.GetFieldTagByName("NETMONEY");
			Assert.AreEqual(118, tagId, "Error in getting tag id by field name: upper case");

			var exception = false;
			try
			{
				tagId = _util.GetFieldTagByName("NETMONEY1");
			}
			catch (DictionaryRuntimeException e)
			{
				exception = true;
				Assert.AreEqual("Unknown field name [NETMONEY1] in FIX version [FIX.4.2]", e.Message,
					"Error in getting tag id by field name: exception message error");
			}

			Assert.IsTrue(exception, "Error in getting tag id by field name: exception not thrown");
		}

		[Test]
		public virtual void TestGetTypeByFieldTagOrName()
		{
			_util = BuildFixUtil(FixVersion.Fix42);
			var typeByTag = _util.GetFieldTypeByFieldTag(118);
			Assert.AreEqual("Amt", typeByTag, "Invalid field type");
			var typeByName = _util.GetFieldTypeByFieldName("NetMoney");
			Assert.AreEqual("Amt", typeByName, "Invalid field type");
		}

		/// <summary>
		/// test used for test hasGroup method
		/// </summary>
		[Test]
		public virtual void TestHasGroupMethod51()
		{
			_util = BuildFixUtil(FixVersion.Fix50Sp1);
			var startTag = _util.GetStartTagForGroup("6", 454);
			Assert.AreEqual(455, startTag, "Error on get start tag for group");
		}

		/// <summary>
		/// test used for test hasGroup method
		/// </summary>
		[Test]
		public virtual void TestHasGroupMethod52()
		{
			_util = BuildFixUtil(FixVersion.Fix50Sp2);
			var startTag = _util.GetStartTagForGroup("6", 454);
			Assert.AreEqual(455, startTag, "Error on get start tag for group");
		}

		[Test]
		public virtual void TestIsGroupTag()
		{
			_util = BuildFixUtil(FixVersion.Fix42);
			var isGroup = _util.IsGroupTag("i", 295);
			Assert.IsTrue(isGroup);
		}

		[Test]
		public virtual void TestIsGroupTagInBlock()
		{
			_util = BuildFixUtil(FixVersion.Fix44);
			var isGroup = _util.IsGroupTag("D", 539);
			Assert.IsTrue(isGroup);
		}

		/// <summary>
		/// This test used for test PrepareFieldsDefinitions method
		/// </summary>
		[Test]
		public virtual void TestPrepareFieldsDefinitionsMethod()
		{
			var factory = FixDictionaryFactory.Instance;
			var fixVersionContainer = FixVersionContainer.GetFixVersionContainer(FixVersion.Fix44);
			var dictionaries = factory.GetDictionaries(fixVersionContainer, null);
			_util = new FixUtil(fixVersionContainer);

			_util.PrepareFieldsDefinitions(dictionaries);
			foreach (var t in dictionaries.Dictionaries)
			{
				var fiXdic = (Fixdic)t;
				var allTags = _util.GetAllTags();
				foreach (var fielddef in fiXdic.Fielddic.Fielddef)
				{
					if (!IsTagExist(allTags, fielddef))
					{
						Assert.Fail("tags collection does not contains tag: " + fielddef.Tag);
					}
				}
			}
		}
	}
}
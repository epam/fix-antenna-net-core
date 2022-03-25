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
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Message.Rg.Exceptions;
using NUnit.Framework;

namespace Epam.FixAntenna.Message.Tests.Rg
{
	[TestFixture]
	internal class RepeatingGroupModifyValidationTest
	{
		[SetUp]
		public virtual void CreateMessage()
		{
			_msgWithoutGroup = RawFixUtil.GetFixMessage(_executionReportWithoutGroup.AsByteArray());
			_msgWithoutGroup = RawFixUtil.IndexRepeatingGroup(_msgWithoutGroup, true);
			_msgWithGroup = RawFixUtil.GetFixMessage(ExecutionReportWithGroup.AsByteArray());
			_msgWithGroup = RawFixUtil.IndexRepeatingGroup(_msgWithGroup, true);
		}

		private readonly FixVersionContainer _version = FixVersionContainer.GetFixVersionContainer(FixVersion.Fix43);
		private readonly string _msgType = "8";

		private readonly string _executionReportWithoutGroup =
			"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001" +
			"115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u000110=124\u0001";

		private FixMessage _msgWithoutGroup;
		private FixMessage _msgWithGroup;

		internal string ExecutionReportWithGroup =
			"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
			"454=1\u0001455=5\u0001456=6\u0001" + "232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001" +
			"555=2\u0001600=12\u0001603=13\u0001" +
			"604=1\u0001605=14\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
			"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001" +
			"539=2\u0001524=23\u0001524=24\u0001525=25\u000110=124\u0001";

		[Test]
		public virtual void AddGroupToRemovedEntry()
		{
			var group = _msgWithGroup.GetRepeatingGroup(555);
			var entry = group.GetEntry(0);
			entry.Remove();

			Assert.Throws<InvalidOperationException>(() => entry.AddRepeatingGroup(604),
				"Entry was deleted. You should create new entry");
		}

		[Test]
		public virtual void AddMessageToRemovedGroup()
		{
			var group = _msgWithGroup.GetRepeatingGroup(555);
			group.Remove();

			Assert.Throws<InvalidOperationException>(() => group.AddEntry(),
				"Group was removed. You should create new group");
		}

		[Test]
		public virtual void AddTagToRemovedEntry()
		{
			var group = _msgWithGroup.GetRepeatingGroup(555);
			var entry = group.GetEntry(0);
			entry.Remove();

			Assert.Throws<InvalidOperationException>(() => entry.AddTag(600, true),
				"Entry was deleted. You should create new entry");
		}

		[Test]
		public virtual void DuplicateGroupIsNotAllowedInEmptyMessage()
		{
			var group = _msgWithoutGroup.AddRepeatingGroupAtIndex(9, 454, true);
			var entry = group.AddEntry();
			entry.AddTag(455, 12);
			entry.AddTag(456, 234);

			Assert.Throws<DuplicateGroupException>(() => _msgWithGroup.AddRepeatingGroupAtIndex(9, 454, true),
				new DuplicateGroupException(454, _version, _msgType).Message);
		}

		[Test]
		public virtual void DuplicateGroupIsNotAllowedInNestedGroup()
		{
			var group = _msgWithGroup.GetRepeatingGroup(555);
			var entry = group.GetEntry(0);
			Assert.Throws<DuplicateGroupException>(() => entry.AddRepeatingGroup(604),
				new DuplicateGroupException(604, _version, _msgType).Message);
		}

		[Test]
		public virtual void DuplicateGroupIsNotAllowedWhenModifyMessage()
		{
			Assert.Throws<DuplicateGroupException>(() => _msgWithGroup.AddRepeatingGroupAtIndex(9, 454, true),
				new DuplicateGroupException(454, _version, _msgType).Message);
		}

		[Test]
		public virtual void DuplicateTagsIsNotAllowedInExistedEntryExistedGroup()
		{
			var group = _msgWithGroup.GetRepeatingGroup(454);
			var entry = group.GetEntry(0);
			Assert.Throws<DuplicateTagException>(() => entry.AddTag(456, 123),
				new DuplicateTagException(454, 456, _version, _msgType).Message);
		}

		[Test]
		public virtual void DuplicateTagsIsNotAllowedInNewEntryExistedGroup()
		{
			var group = _msgWithGroup.GetRepeatingGroup(454);
			var entry = group.AddEntry();
			entry.AddTag(455, 12);
			entry.AddTag(456, 234);

			Assert.Throws<DuplicateTagException>(() => entry.AddTag(456, 24),
				new DuplicateTagException(454, 456, _version, _msgType).Message);
		}

		[Test]
		public virtual void DuplicateTagsIsNotAllowedInNewEntryNestedGroup()
		{
			var group = _msgWithGroup.GetRepeatingGroup(555);
			var entry = group.GetEntry(0).GetRepeatingGroup(604).GetEntry(0);
			Assert.Throws<DuplicateTagException>(() => entry.AddTag(605, 123),
				new DuplicateTagException(604, 605, _version, _msgType).Message);
		}

		[Test]
		public virtual void DuplicateTagsIsNotAllowedInNewGroup()
		{
			var group = _msgWithoutGroup.AddRepeatingGroupAtIndex(9, 454, true);
			var entry = group.AddEntry();
			entry.AddTag(455, 232);
			entry.AddTag(456, 623);

			Assert.Throws<DuplicateTagException>(() => entry.AddTag(456, 123),
				new DuplicateTagException(454, 456, _version, _msgType).Message);
		}

		[Test]
		public virtual void FillGroupAfterRemoveEntry()
		{
			var group = _msgWithGroup.GetRepeatingGroup(555);
			var entry = group.AddEntry();
			var group604 = entry.AddRepeatingGroup(604);
			entry.Remove();

			Assert.Throws<InvalidOperationException>(() => group604.AddEntry().AddTag(605, 123),
				"Group was removed. You should create new group");
		}

		[Test]
		public virtual void OnlyAllowedTagsCanBeAddedInsideExistedEntryOfExistedGroup()
		{
			var group = _msgWithGroup.GetRepeatingGroup(454);
			var entry = group.GetEntry(0);
			Assert.Throws<UnresolvedGroupTagException>(() => entry.AddTag(457, 12),
				new UnresolvedGroupTagException(457, 454, _version, _msgType).Message);
		}

		[Test]
		public virtual void OnlyAllowedTagsCanBeAddedInsideExistedEntryOfExistedNestedGroup()
		{
			var group = _msgWithGroup.GetRepeatingGroup(555);
			var entry = group.GetEntry(0);
			var subGroup = entry.GetRepeatingGroup(604);
			var subEntry = subGroup.GetEntry(0);
			Assert.Throws<UnresolvedGroupTagException>(() => subEntry.AddTag(251, 123),
				new UnresolvedGroupTagException(251, 604, _version, _msgType).Message);
		}

		[Test]
		public virtual void OnlyAllowedTagsCanBeAddedInsideNewEntryOfExistedGroup()
		{
			var group = _msgWithGroup.GetRepeatingGroup(454);
			var entry = group.AddEntry();
			entry.AddTag(455, 5);
			Assert.Throws<UnresolvedGroupTagException>(() => entry.AddTag(457, 12),
				new UnresolvedGroupTagException(457, 454, _version, _msgType).Message);
		}

		[Test]
		public virtual void OnlyAllowedTagsCanBeAddedInsideNewEntryOfExistedNestedGroup()
		{
			var group = _msgWithGroup.GetRepeatingGroup(555);
			var entry = group.GetEntry(0);
			var subGroup = entry.GetRepeatingGroup(604);
			var subEntry = subGroup.AddEntry();
			Assert.Throws<UnresolvedGroupTagException>(() => subEntry.AddTag(251, 123),
				new UnresolvedGroupTagException(251, 604, _version, _msgType).Message);
		}

		[Test]
		public virtual void OnlyAllowedTagsCanBeAddedInsideNewGroup()
		{
			var group = _msgWithoutGroup.AddRepeatingGroupAtIndex(9, 454, true);
			var entry = group.AddEntry();
			entry.AddTag(455, 5);
			Assert.Throws<UnresolvedGroupTagException>(() => entry.AddTag(457, 12),
				new UnresolvedGroupTagException(457, 454, _version, _msgType).Message);
		}

		[Test]
		public virtual void OnlyGroupWithAllowedLeadingTagsCanBeAddedInsideExistedEntry()
		{
			var group = _msgWithGroup.GetRepeatingGroup(555);
			var entry = group.GetEntry(0);
			Assert.Throws<InvalidLeadingTagException>(() => entry.AddRepeatingGroup(603),
				new InvalidLeadingTagException(603, _version, _msgType).Message);
		}

		[Test]
		public virtual void OnlyGroupWithAllowedLeadingTagsCanBeAddedInsideMsgWithoutGroups()
		{
			Assert.Throws<InvalidLeadingTagException>(() => _msgWithoutGroup.AddRepeatingGroupAtIndex(9, 123, true),
				new InvalidLeadingTagException(123, _version, _msgType).Message);
		}

		[Test]
		public virtual void TryModifyLeadingTagValue()
		{
			var group = _msgWithGroup.GetRepeatingGroup(555);

			Assert.Throws<ArgumentException>(
				() => group.GetEntry(0)
					.UpdateValue(604, 111, IndexedStorage.MissingTagHandling.DontAddIfNotExists),
				"Trying to update leading tag value. It's impossible because leading tags are self-maintaining.");
		}
	}
}
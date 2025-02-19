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

using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Message.Rg;
using Epam.FixAntenna.NetCore.Message.Rg.Exceptions;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Message.Tests.Rg
{
	[TestFixture]
	internal class RepeatingGroupTest
	{
		[SetUp]
		public virtual void CreateMessage()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"454=1\u0001455=5\u0001456=6\u0001" +
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001" +
				"518=1\u0001519=10\u0001520=11\u000110=124\u0001";
			Msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());
		}

		[TearDown]
		public virtual void ReleaseMessage()
		{
			Msg.ReleaseInstance();
		}

		internal FixMessage Msg;

		//RG 1.2

		[Test]
		public virtual void AddEntry()
		{
			var group = RepeatingGroupPool.RepeatingGroup;
			Msg.GetRepeatingGroup(454, group);

			ClassicAssert.AreEqual("454=1 | 455=5 | 456=6 | ", group.ToPrintableString());

			var entry = group.AddEntry();
			entry.AddTag(455, 111);
			entry.AddTag(456, 112);
			ClassicAssert.AreEqual("454=2 | 455=5 | 456=6 | 455=111 | 456=112 | ", group.ToPrintableString());

			group.Release();

			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg,
				"454=2\u0001455=5\u0001456=6\u0001455=111\u0001456=112\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg,
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "518=1\u0001519=10\u0001520=11\u0001", null);

			var fixMsgWithNewGroup =
				"8=FIX.4.3 | 9=94 | 35=8 | 49=target | 56=sender | 115=onBehalf | 34=1 | 50=senderSub | 52=20080212-04:15:18.308 | " +
				"454=2 | 455=5 | 456=6 | 455=111 | 456=112 | " + "232=2 | 233=7 | 234=8 | 233=9 | 234=9 | " +
				"518=1 | 519=10 | 520=11 | " + "10=124 | ";
			ClassicAssert.AreEqual(fixMsgWithNewGroup, Msg.ToPrintableString());
		}

		[Test]
		public virtual void CopyMsgWithInvalidatedRg()
		{
			var rg = Msg.AddRepeatingGroup(453);
			var entry = rg.AddEntry();
			entry.AddTag(448, "test");
			entry.AddTag(447, "test");
			entry.AddTag(452, "11");
			Msg.InvalidateRepeatingGroupIndex();
			var cloned = Msg.DeepClone(true, true);
		}

		[Test]
		public virtual void CreateFlatRgAtEnd1()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"454=1\u0001455=5\u0001456=6\u0001" + "232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001" +
				"518=1\u0001519=10\u0001520=11\u0001";
			Msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());
			//msg.addRepeatingGroup(leadingTag) will add group to the end of the current message
			var group = Msg.AddRepeatingGroup(382);

			//append entry to the end of group
			var entry = group.AddEntry();

			entry.AddTag(375, 3);
			entry.AddTag(437, 4);

			//Insert entry at index  - in this case new entry will be first
			entry = group.AddEntry(0);
			entry.AddTag(375, 1);
			entry.AddTag(337, 2);


			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg,
				"382=2\u0001375=1\u0001337=2\u0001375=3\u0001437=4\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "454=1\u0001455=5\u0001456=6\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg,
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "518=1\u0001519=10\u0001520=11\u0001", null);

			var fixMsgWithNewGroup =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001454=1\u0001455=5\u0001456=6\u0001232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001518=1\u0001519=10\u0001520=11\u0001" +
				"382=2\u0001375=1\u0001337=2\u0001375=3\u0001437=4\u0001";
			ClassicAssert.AreEqual(fixMsgWithNewGroup, Msg.ToString());
		}

		[Test]
		public virtual void CreateFlatRgAtEnd2()
		{
			ClassicAssert.IsFalse(Msg.IsTagExists(382));
			var group = Msg.AddRepeatingGroupAtIndex(20, 382, false);

			//append entry to the end of group
			var entry = group.AddEntry();
			entry.AddTag(375, 3);
			entry.AddTag(437, 4);

			//Insert entry at index  - in this case new entry will be first
			entry = group.AddEntry(0);
			entry.AddTag(375, 1);
			entry.AddTag(337, 2);

			entry = group.AddEntry(1);
			entry.AddTag(375, 222);
			entry.AddTag(337, 333);

			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg,
				"382=3\u0001375=1\u0001337=2\u0001375=222\u0001337=333\u0001375=3\u0001437=4\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "454=1\u0001455=5\u0001456=6\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg,
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "518=1\u0001519=10\u0001520=11\u0001", null);
			var fixMsgWithNewGroup =
				"8=FIX.4.3 | 9=94 | 35=8 | 49=target | 56=sender | 115=onBehalf | 34=1 | 50=senderSub | 52=20080212-04:15:18.308 | " +
				"454=1 | 455=5 | 456=6 | " + "232=2 | 233=7 | 234=8 | 233=9 | 234=9 | " + "518=1 | 519=10 | 520=11 | " +
				"382=3 | 375=1 | 337=2 | 375=222 | 337=333 | 375=3 | 437=4 | 10=124 | ";
			ClassicAssert.AreEqual(fixMsgWithNewGroup, Msg.ToPrintableString());
		}

		[Test]
		public virtual void CreateFlatRgAtStart()
		{
			ClassicAssert.AreEqual(
				"8=FIX.4.3 | 9=94 | 35=8 | 49=target | 56=sender | 115=onBehalf | 34=1 | 50=senderSub | " +
				"52=20080212-04:15:18.308 | " + "454=1 | 455=5 | 456=6 | " +
				"232=2 | 233=7 | 234=8 | 233=9 | 234=9 | " + "518=1 | 519=10 | 520=11 | " + "10=124 | ",
				Msg.ToPrintableString());
			var group = Msg.AddRepeatingGroupAtIndex(9, 382, false);

			//append entry to the end of group
			var entry = group.AddEntry();
			entry.AddTag(375, 3);
			entry.AddTag(437, 4);

			//Insert entry at index  - in this case new entry will be first
			entry = group.AddEntry(0);
			entry.AddTag(375, 1);
			entry.AddTag(337, 2);

			entry = group.AddEntry(1);
			entry.AddTag(375, 222);
			entry.AddTag(337, 333);

			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg,
				"382=3\u0001375=1\u0001337=2\u0001375=222\u0001337=333\u0001375=3\u0001437=4\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "454=1\u0001455=5\u0001456=6\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg,
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "518=1\u0001519=10\u0001520=11\u0001", null);

			var fixMsgWithNewGroup =
				"8=FIX.4.3 | 9=94 | 35=8 | 49=target | 56=sender | 115=onBehalf | 34=1 | 50=senderSub | 52=20080212-04:15:18.308 | " +
				"382=3 | 375=1 | 337=2 | 375=222 | 337=333 | 375=3 | 437=4 | " + "454=1 | 455=5 | 456=6 | " +
				"232=2 | 233=7 | 234=8 | 233=9 | 234=9 | " + "518=1 | 519=10 | 520=11 | 10=124 | ";
			ClassicAssert.AreEqual(fixMsgWithNewGroup, Msg.ToPrintableString());
		}

		[Test]
		public virtual void CreateHierarchicalRg()
		{
			var group = Msg.AddRepeatingGroupAtIndex(20, 555, false);
			//====== Entry 1 ============//
			var entry = group.AddEntry();

			entry.AddTag(600, 111);
			var subGroup = entry.AddRepeatingGroup(604);

			var subGroupEntry = subGroup.AddEntry();
			subGroupEntry.AddTag(605, 222);
			subGroupEntry.AddTag(606, 223);

			subGroupEntry = subGroup.AddEntry();
			subGroupEntry.AddTag(605, 224);
			subGroupEntry.AddTag(606, 225);

			entry.AddTag(607, 112);

			//====== Entry 2 ============//
			entry = group.AddEntry();

			entry.AddTag(600, 121);
			entry.AddTag(601, 122);

			subGroup = entry.AddRepeatingGroup(604);
			subGroupEntry = subGroup.AddEntry();
			subGroupEntry.AddTag(605, 232);
			subGroupEntry.AddTag(606, 233);

			subGroupEntry = subGroup.AddEntry();
			subGroupEntry.AddTag(605, 234);
			subGroupEntry.AddTag(606, 235);

			entry.AddTag(607, 123);

			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "454=1\u0001455=5\u0001456=6\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg,
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "518=1\u0001519=10\u0001520=11\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg,
				"555=2\u0001600=111\u0001604=2\u0001607=112\u0001600=121\u0001601=122\u0001604=2\u0001607=123\u0001",
				null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg,
				"604=2\u0001605=222\u0001606=223\u0001605=224\u0001606=225\u0001",
				Msg.GetRepeatingGroup(555).GetEntry(0).GetRepeatingGroup(604));
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg,
				"604=2\u0001605=232\u0001606=233\u0001605=234\u0001606=235\u0001",
				Msg.GetRepeatingGroup(555).GetEntry(1).GetRepeatingGroup(604));

			var fixMsgWithNewGroup =
				"8=FIX.4.3 | 9=94 | 35=8 | 49=target | 56=sender | 115=onBehalf | 34=1 | 50=senderSub | 52=20080212-04:15:18.308 | " +
				"454=1 | 455=5 | 456=6 | " + "232=2 | 233=7 | 234=8 | 233=9 | 234=9 | " + "518=1 | 519=10 | 520=11 | " +
				"555=2 | " + "600=111 | 604=2 | 605=222 | 606=223 | 605=224 | 606=225 | 607=112 | " +
				"600=121 | 601=122 | 604=2 | 605=232 | 606=233 | 605=234 | 606=235 | 607=123 | " + "10=124 | ";
			ClassicAssert.AreEqual(fixMsgWithNewGroup, Msg.ToPrintableString());
		}

		[Test]
		public virtual void CreateHierarchicalRgAtStart()
		{
			var group = Msg.AddRepeatingGroupAtIndex(9, 555, false);

			//====== Entry 1 ============//
			var entry = group.AddEntry();

			entry.AddTag(600, 111);
			var subGroup = entry.AddRepeatingGroup(604);

			var subGroupEntry = subGroup.AddEntry();
			subGroupEntry.AddTag(605, 222);
			subGroupEntry.AddTag(606, 223);

			subGroupEntry = subGroup.AddEntry();
			subGroupEntry.AddTag(605, 224);
			subGroupEntry.AddTag(606, 225);

			entry.AddTag(607, 112);

			//====== Entry 2 ============//
			entry = group.AddEntry();

			entry.AddTag(600, 121);
			entry.AddTag(601, 122);

			subGroup = entry.AddRepeatingGroup(604);
			subGroupEntry = subGroup.AddEntry();
			subGroupEntry.AddTag(605, 232);
			subGroupEntry.AddTag(606, 233);

			subGroupEntry = subGroup.AddEntry();
			subGroupEntry.AddTag(605, 234);
			subGroupEntry.AddTag(606, 235);

			entry.AddTag(607, 123);

			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "454=1\u0001455=5\u0001456=6\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg,
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "518=1\u0001519=10\u0001520=11\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg,
				"555=2\u0001600=111\u0001604=2\u0001607=112\u0001600=121\u0001601=122\u0001604=2\u0001607=123\u0001",
				null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg,
				"604=2\u0001605=222\u0001606=223\u0001605=224\u0001606=225\u0001",
				Msg.GetRepeatingGroup(555).GetEntry(0).GetRepeatingGroup(604));
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg,
				"604=2\u0001605=232\u0001606=233\u0001605=234\u0001606=235\u0001",
				Msg.GetRepeatingGroup(555).GetEntry(1).GetRepeatingGroup(604));

			var fixMsgWithNewGroup =
				"8=FIX.4.3 | 9=94 | 35=8 | 49=target | 56=sender | 115=onBehalf | 34=1 | 50=senderSub | 52=20080212-04:15:18.308 | " +
				"555=2 | " + "600=111 | 604=2 | 605=222 | 606=223 | 605=224 | 606=225 | 607=112 | " +
				"600=121 | 601=122 | 604=2 | 605=232 | 606=233 | 605=234 | 606=235 | 607=123 | " +
				"454=1 | 455=5 | 456=6 | " + "232=2 | 233=7 | 234=8 | 233=9 | 234=9 | " + "518=1 | 519=10 | 520=11 | " +
				"10=124 | ";
			ClassicAssert.AreEqual(fixMsgWithNewGroup, Msg.ToPrintableString());
		}

		[Test]
		public virtual void CreateTwoGroupsWithEmptyEntry()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			var group1 = msg.AddRepeatingGroup(123);
			var entry1 = group1.AddEntry();
			var group2 = entry1.AddRepeatingGroup(456);
			var entry2 = group2.AddEntry();

			ClassicAssert.AreEqual("", group1.AddEntry().ToPrintableString());
			ClassicAssert.AreEqual("", group2.AddEntry().ToPrintableString());
			ClassicAssert.AreEqual("", group2.AddEntry().ToPrintableString());
			ClassicAssert.AreEqual("", group1.AddEntry().ToPrintableString());
			ClassicAssert.AreEqual("", entry1.ToPrintableString());
			ClassicAssert.AreEqual("", entry2.ToPrintableString());
			ClassicAssert.AreEqual("", group1.ToPrintableString());
			ClassicAssert.AreEqual("", group2.ToPrintableString());
		}

		[Test]
		public virtual void GetSizeForNestdRg()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			var group = msg.AddRepeatingGroup(123);
			ClassicAssert.AreEqual(0, @group.Count);
			ClassicAssert.AreEqual("8=FIX.4.4 | 35=8 | ", msg.ToPrintableString());

			var entry = group.AddEntry();
			ClassicAssert.AreEqual(1, @group.Count);
			ClassicAssert.AreEqual(0, group.GetLeadingTagValue());
			ClassicAssert.AreEqual(0, entry.Count);

			var nestdGroup = entry.AddRepeatingGroup(456);
			ClassicAssert.AreEqual(0, nestdGroup.Count);
			ClassicAssert.AreEqual(0, nestdGroup.GetLeadingTagValue());
			ClassicAssert.AreEqual("8=FIX.4.4 | 35=8 | ", msg.ToPrintableString());

			group.RemoveEntry(entry);
			ClassicAssert.AreEqual(0, @group.Count);
			ClassicAssert.AreEqual("8=FIX.4.4 | 35=8 | ", msg.ToPrintableString());

			//rg should be accessible after removing entries
			group = msg.GetRepeatingGroup(123);
			ClassicAssert.AreEqual(0, @group.Count);
			ClassicAssert.AreEqual(0, group.GetLeadingTagValue());

			entry = group.AddEntry();
			ClassicAssert.AreEqual(1, @group.Count);
			ClassicAssert.AreEqual(0, group.GetLeadingTagValue());
			ClassicAssert.AreEqual(0, entry.Count);

			nestdGroup = entry.AddRepeatingGroup(456);
			ClassicAssert.AreEqual(0, nestdGroup.Count);
			ClassicAssert.AreEqual(0, nestdGroup.GetLeadingTagValue());
			var entry2 = nestdGroup.AddEntry();
			ClassicAssert.AreEqual(0, entry2.Count);
			ClassicAssert.AreEqual(1, nestdGroup.Count);
			ClassicAssert.AreEqual(0, nestdGroup.GetLeadingTagValue());
			var nestedGroup2 = entry2.AddRepeatingGroup(789);
			ClassicAssert.AreEqual(0, nestedGroup2.Count);
			ClassicAssert.AreEqual(0, nestedGroup2.GetLeadingTagValue());
			ClassicAssert.AreEqual("8=FIX.4.4 | 35=8 | ", msg.ToPrintableString());
			nestdGroup.RemoveEntry(entry2);
			ClassicAssert.AreEqual("8=FIX.4.4 | 35=8 | ", msg.ToPrintableString());
			entry.Remove();
			ClassicAssert.AreEqual("8=FIX.4.4 | 35=8 | ", msg.ToPrintableString());
		}

		[Test]
		public virtual void GetSizeNewGroupAndEntry()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			var group = msg.AddRepeatingGroup(123);
			ClassicAssert.AreEqual(0, @group.Count);

			var entry = group.AddEntry();
			ClassicAssert.AreEqual(1, @group.Count);
			ClassicAssert.AreEqual(0, group.GetLeadingTagValue());
			ClassicAssert.AreEqual(0, entry.Count);
			entry.AddTag(1234, 123);
			ClassicAssert.AreEqual(1, entry.Count);
		}

		//RG 1.6

		[Test]
		public virtual void Iterate()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"454=2\u0001455=5\u0001456=6\u0001455=321\u0001456=123\u0001" +
				"555=5\u0001" +
				"600=12\u0001603=13\u0001604=1\u0001605=14\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001539=2\u0001524=23\u0001524=24\u0001525=25\u0001" +
				"600=12\u0001539=1\u0001524=23\u0001" +
				"600=12\u0001539=1\u0001524=23\u0001" +
				"600=12\u0001539=1\u0001524=23\u0001" +
				"10=124\u0001";
			Msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());
			var group = Msg.GetRepeatingGroup(555);

			string[] entries =
			{
				"600=12 | 603=13 | 604=1 | 605=14 | 251=15 | 539=2 | 524=16 | 525=17 | 524=18 | ",
				"600=19 | 603=20 | 604=2 | 605=21 | 605=32 | 251=22 | 539=2 | 524=23 | 524=24 | 525=25 | ",
				"600=12 | 539=1 | 524=23 | ",
				"600=12 | 539=1 | 524=23 | ",
				"600=12 | 539=1 | 524=23 | "
			};
			var index = 0;

			//iterate entries
			foreach (var entry in group)
			{
				ClassicAssert.AreEqual(entries[index++], entry.ToPrintableString());
			}

			//iterate tags
			group = Msg.GetRepeatingGroup(454);
			string[][] expectedTagValue =
			{
				new[] { "5", "6" },
				new[] { "321", "123" }
			};

			var entryIndex = 0;
			foreach (var entry in group)
			{
				for (var tagIndex = 0; tagIndex < entry.Count; tagIndex++)
				{
					ClassicAssert.AreEqual(expectedTagValue[entryIndex][tagIndex], entry.GetTagValueAsStringAtIndex(tagIndex));
				}

				entryIndex++;
			}
		}

		[Test]
		public virtual void manipulationWithThreeGroups_1()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			var group1 = msg.AddRepeatingGroup(123);
			var entry1 = group1.AddEntry();
			entry1.AddTag(1234, 1);
			entry1.AddTag(1236, 2);

			var entry11 = group1.AddEntry();
			entry11.AddTag(1234, 11);

			var group2 = entry1.AddRepeatingGroup(456);
			var entry2 = group2.AddEntry();

			var group3 = entry2.AddRepeatingGroup(789);
			var entry3 = group3.AddEntry();

			ClassicAssert.AreEqual("8=FIX.4.4 | 35=8 | " + "123=2 | " + "1234=1 | 1236=2 | " + "1234=11 | ",
				msg.ToPrintableString());

			entry1.AddTag(1111, 1);
			entry2.AddTag(2222, 2);
			entry2.AddTag(2223, 4);
			entry1.AddTag(1112, 3);
			entry3.AddTag(3333, 3);

			ClassicAssert.AreEqual(
				"8=FIX.4.4 | 35=8 | 123=2 | " +
				"1234=1 | 1236=2 | 456=1 | 789=1 | 3333=3 | 2222=2 | 2223=4 | 1111=1 | 1112=3 | " + "1234=11 | ",
				msg.ToPrintableString());

			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg,
				"123=2\u00011234=1\u00011236=2\u0001456=1\u00011111=1\u00011112=3\u00011234=11\u0001",
				msg.GetRepeatingGroup(123));
			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg, "456=1\u0001789=1\u00012222=2\u00012223=4\u0001",
				msg.GetRepeatingGroup(123).GetEntry(0).GetRepeatingGroup(456));
			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg, "789=1\u00013333=3\u0001",
				msg.GetRepeatingGroup(123).GetEntry(0).GetRepeatingGroup(456).GetEntry(0).GetRepeatingGroup(789));

			entry3.RemoveTag(3333);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg,
				"123=2\u00011234=1\u00011236=2\u0001456=1\u00011111=1\u00011112=3\u00011234=11\u0001",
				msg.GetRepeatingGroup(123));
			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg, "456=1\u00012222=2\u00012223=4\u0001",
				msg.GetRepeatingGroup(123).GetEntry(0).GetRepeatingGroup(456));
			ClassicAssert.AreEqual(
				"8=FIX.4.4 | 35=8 | 123=2 | " + "1234=1 | 1236=2 | 456=1 | 2222=2 | 2223=4 | 1111=1 | 1112=3 | " +
				"1234=11 | ", msg.ToPrintableString());

			entry3.AddTag(3333, 3);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg,
				"123=2\u00011234=1\u00011236=2\u0001456=1\u00011111=1\u00011112=3\u00011234=11\u0001",
				msg.GetRepeatingGroup(123));
			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg, "456=1\u0001789=1\u00012222=2\u00012223=4\u0001",
				msg.GetRepeatingGroup(123).GetEntry(0).GetRepeatingGroup(456));
			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg, "789=1\u00013333=3\u0001",
				msg.GetRepeatingGroup(123).GetEntry(0).GetRepeatingGroup(456).GetEntry(0).GetRepeatingGroup(789));
			entry3.RemoveTag(3333);

			var entry111 = group1.AddEntry();
			entry111.AddTag(1234, 111);
			ClassicAssert.AreEqual(
				"8=FIX.4.4 | 35=8 | 123=3 | 1234=1 | 1236=2 | 456=1 | 2222=2 | 2223=4 | 1111=1 | 1112=3 | 1234=11 | 1234=111 | ",
				msg.ToPrintableString());

			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg,
				"123=3\u00011234=1\u00011236=2\u0001456=1\u00011111=1\u00011112=3\u00011234=11\u00011234=111\u0001",
				msg.GetRepeatingGroup(123));
			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg, "456=1\u00012222=2\u00012223=4\u0001",
				msg.GetRepeatingGroup(123).GetEntry(0).GetRepeatingGroup(456));
			entry3.Remove();
		}

		[Test]
		public virtual void manipulationWithThreeGroups_2()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			var group1 = msg.AddRepeatingGroup(123);
			var entry1 = group1.AddEntry();
			var group2 = entry1.AddRepeatingGroup(456);
			var entry2 = group2.AddEntry();

			var group3 = entry2.AddRepeatingGroup(789);
			var entry3 = group3.AddEntry();

			ClassicAssert.AreEqual("8=FIX.4.4 | 35=8 | ", msg.ToPrintableString());

			entry3.AddTag(3333, 3);
			entry2.AddTag(2222, 2);
			entry1.AddTag(1111, 1);
			entry1.AddTag(1112, 3);
			entry2.AddTag(2223, 4);

			ClassicAssert.AreEqual("8=FIX.4.4 | 35=8 | 123=1 | 456=1 | 789=1 | 3333=3 | 2222=2 | 2223=4 | 1111=1 | 1112=3 | ",
				msg.ToPrintableString());

			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg, "123=1\u0001456=1\u00011111=1\u00011112=3\u0001",
				msg.GetRepeatingGroup(123));
			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg, "456=1\u0001789=1\u00012222=2\u00012223=4\u0001",
				msg.GetRepeatingGroup(123).GetEntry(0).GetRepeatingGroup(456));
			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg, "789=1\u00013333=3\u0001",
				msg.GetRepeatingGroup(123).GetEntry(0).GetRepeatingGroup(456).GetEntry(0).GetRepeatingGroup(789));

			entry3.RemoveTag(3333);
			ClassicAssert.AreEqual("8=FIX.4.4 | 35=8 | 123=1 | 456=1 | 2222=2 | 2223=4 | 1111=1 | 1112=3 | ",
				msg.ToPrintableString());
			entry3.AddTag(3333, 3);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg, "789=1\u00013333=3\u0001",
				msg.GetRepeatingGroup(123).GetEntry(0).GetRepeatingGroup(456).GetEntry(0).GetRepeatingGroup(789));
			entry3.RemoveTag(3333);
			ClassicAssert.AreEqual("8=FIX.4.4 | 35=8 | 123=1 | 456=1 | 2222=2 | 2223=4 | 1111=1 | 1112=3 | ",
				msg.ToPrintableString());
			ClassicAssert.AreEqual("", msg.GetRepeatingGroup(789).ToPrintableString());
		}

		[Test]
		public virtual void manipulationWithThreeGroups_3()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			var group1 = msg.AddRepeatingGroup(123);
			var entry1 = group1.AddEntry();
			var group2 = entry1.AddRepeatingGroup(456);
			var entry2 = group2.AddEntry();

			var group3 = entry2.AddRepeatingGroup(789);
			var entry3 = group3.AddEntry();

			ClassicAssert.AreEqual("8=FIX.4.4 | 35=8 | ", msg.ToPrintableString());

			entry3.AddTag(3333, 3);
			entry3.RemoveTag(3333);
			ClassicAssert.AreEqual("8=FIX.4.4 | 35=8 | ", msg.ToPrintableString());
			entry2.AddTag(2222, 2);
			entry1.AddTag(1111, 1);
			entry1.AddTag(1112, 3);
			entry2.AddTag(2223, 4);
			entry3.AddTag(3333, 3);

			ClassicAssert.AreEqual("8=FIX.4.4 | 35=8 | 123=1 | 456=1 | 789=1 | 3333=3 | 2222=2 | 2223=4 | 1111=1 | 1112=3 | ",
				msg.ToPrintableString());

			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg, "123=1\u0001456=1\u00011111=1\u00011112=3\u0001",
				msg.GetRepeatingGroup(123));
			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg, "456=1\u0001789=1\u00012222=2\u00012223=4\u0001",
				msg.GetRepeatingGroup(123).GetEntry(0).GetRepeatingGroup(456));
			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg, "789=1\u00013333=3\u0001",
				msg.GetRepeatingGroup(123).GetEntry(0).GetRepeatingGroup(456).GetEntry(0).GetRepeatingGroup(789));
		}

		[Test]
		public virtual void ManuslParseCustomDict()
		{
			var msgStr = "8=FIX.4.4 | 9=59 | 35=0 | 34=4 | 49=senderId | 56=targetId | 52=20170224-14:05:33.285 | " +
						"199=2 | 104=a | 104=b | 10=034 | ";

			var customMessage = RawFixUtil.GetFixMessage(msgStr.Replace(" | ", "\u0001").AsByteArray());
			var customFixVersion =
				new FixVersionContainer("custom40", FixVersion.Fix40, "custom/fixdic40custom.xml");

			// do manual parse
			RawFixUtil.IndexRepeatingGroup(customMessage, customFixVersion, "0");
			ClassicAssert.IsTrue(customMessage.IsRepeatingGroupExists(199));
		}

		[Test]
		public virtual void NoExceptionWhenTryGetNonGroupTag()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			var group = msg.AddRepeatingGroup(123);
			ClassicAssert.IsNull(msg.GetRepeatingGroup(35));
			ClassicAssert.IsNotNull(msg.GetRepeatingGroup(123));
			group = msg.GetRepeatingGroup(123);
			var entry = group.AddEntry();
			entry.AddTag(111, 111);
			entry.AddRepeatingGroup(456);
			ClassicAssert.IsNull(msg.GetRepeatingGroup(111));
			ClassicAssert.IsNotNull(msg.GetRepeatingGroup(456));
		}

		[Test]
		public virtual void ParseCustomDict()
		{
			var msgStr = "8=FIX.4.4 | 9=59 | 35=0 | 34=4 | 49=senderId | 56=targetId | 52=20170224-14:05:33.285 | " +
						"199=2 | 104=a | 104=b | 10=034 | ";

			var message = RawFixUtil.GetFixMessage(msgStr.Replace(" | ", "\u0001").AsByteArray());
			ClassicAssert.IsFalse(message.IsRepeatingGroupExists(199));

			var customMessage = RawFixUtil.GetFixMessage(msgStr.Replace(" | ", "\u0001").AsByteArray());
			var customFixVersion =
				new FixVersionContainer("custom40", FixVersion.Fix40, "custom/fixdic40custom.xml");
			FixMessageTestHelper.SetFixVersion(customMessage, customFixVersion);
			ClassicAssert.IsTrue(customMessage.IsRepeatingGroupExists(199));
		}

		[Test]
		public virtual void ParseMsgWithoutTrailer()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"382=2\u0001375=1\u0001337=2\u0001375=3\u0001437=4\u0001" + "454=1\u0001455=5\u0001456=6\u0001" +
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001" + "518=1\u0001519=10\u0001520=11\u0001" +
				"555=2\u0001600=12\u0001603=13\u0001" +
				"604=1\u0001605=14\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001" +
				"539=2\u0001524=23\u0001524=24\u0001525=25\u0001";
			Msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());

			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg,
				"382=2\u0001375=1\u0001337=2\u0001375=3\u0001437=4\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "454=1\u0001455=5\u0001456=6\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg,
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "518=1\u0001519=10\u0001520=11\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg,
				"555=2\u0001600=12\u0001603=13\u0001604=1\u0001251=15\u0001539=2\u0001600=19\u0001603=20\u0001604=2\u0001251=22\u0001539=2\u0001",
				null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "604=1\u0001605=14\u0001",
				Msg.GetRepeatingGroup(555).GetEntry(0).GetRepeatingGroup(604));
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "539=2\u0001524=16\u0001525=17\u0001524=18\u0001",
				Msg.GetRepeatingGroup(555).GetEntry(0).GetRepeatingGroup(539));
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "604=2\u0001605=21\u0001605=32\u0001",
				Msg.GetRepeatingGroup(555).GetEntry(1).GetRepeatingGroup(604));
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "539=2\u0001524=23\u0001524=24\u0001525=25\u0001",
				Msg.GetRepeatingGroup(555).GetEntry(1).GetRepeatingGroup(539));
		}

		[Test]
		public virtual void ParseMsgWithTrailer()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"382=2\u0001375=1\u0001337=2\u0001375=3\u0001437=4\u0001" + "454=1\u0001455=5\u0001456=6\u0001" +
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001" + "518=1\u0001519=10\u0001520=11\u0001" +
				"555=2\u0001600=12\u0001603=13\u0001" +
				"604=1\u0001605=14\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001" +
				"539=2\u0001524=23\u0001524=24\u0001525=25\u000110=124\u0001";
			Msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());

			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg,
				"382=2\u0001375=1\u0001337=2\u0001375=3\u0001437=4\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "454=1\u0001455=5\u0001456=6\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg,
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "518=1\u0001519=10\u0001520=11\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg,
				"555=2\u0001600=12\u0001603=13\u0001604=1\u0001251=15\u0001539=2\u0001600=19\u0001603=20\u0001604=2\u0001251=22\u0001539=2\u0001",
				null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "604=1\u0001605=14\u0001",
				Msg.GetRepeatingGroup(555).GetEntry(0).GetRepeatingGroup(604));
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "539=2\u0001524=16\u0001525=17\u0001524=18\u0001",
				Msg.GetRepeatingGroup(555).GetEntry(0).GetRepeatingGroup(539));
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "604=2\u0001605=21\u0001605=32\u0001",
				Msg.GetRepeatingGroup(555).GetEntry(1).GetRepeatingGroup(604));
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "539=2\u0001524=23\u0001524=24\u0001525=25\u0001",
				Msg.GetRepeatingGroup(555).GetEntry(1).GetRepeatingGroup(539));
		}

		[Test]
		public virtual void ParseWithThreeEntry()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"555=3\u0001600=12\u0001603=13\u0001" +
				"604=1\u0001605=14\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001" +
				"539=2\u0001524=23\u0001524=24\u0001525=25\u0001" + "600=26\u0001603=27\u0001" +
				"604=1\u0001605=28\u0001251=29\u0001539=2\u0001524=30\u0001525=31\u0001524=32\u000110=124\u0001";

			Msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg,
				"555=3\u0001" + "600=12\u0001603=13\u0001604=1\u0001251=15\u0001539=2\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001251=22\u0001539=2\u0001" +
				"600=26\u0001603=27\u0001604=1\u0001251=29\u0001539=2\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "604=1\u0001605=14\u0001",
				Msg.GetRepeatingGroup(555).GetEntry(0).GetRepeatingGroup(604));
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "539=2\u0001524=16\u0001525=17\u0001524=18\u0001",
				Msg.GetRepeatingGroup(555).GetEntry(0).GetRepeatingGroup(539));
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "604=2\u0001605=21\u0001605=32\u0001",
				Msg.GetRepeatingGroup(555).GetEntry(1).GetRepeatingGroup(604));
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "539=2\u0001524=23\u0001524=24\u0001525=25\u0001",
				Msg.GetRepeatingGroup(555).GetEntry(1).GetRepeatingGroup(539));
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "604=1\u0001605=28\u0001",
				Msg.GetRepeatingGroup(555).GetEntry(2).GetRepeatingGroup(604));
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "539=2\u0001524=30\u0001525=31\u0001524=32\u0001",
				Msg.GetRepeatingGroup(555).GetEntry(2).GetRepeatingGroup(539));
		}

		[Test]
		public virtual void PrintPrintForEmptyNestedGroup()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			var group = msg.AddRepeatingGroup(123);
			var entry = group.AddEntry();
			entry.AddTag(1, 1);
			var nestedGroup = entry.AddRepeatingGroup(456);
			ClassicAssert.AreEqual("8=FIX.4.4 | 35=8 | 123=1 | 1=1 | ", msg.ToPrintableString());
		}

		[Test]
		public virtual void ReInitGroup()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			msg.AddRepeatingGroup(123);
			var group = msg.GetRepeatingGroup(123);

			var entry = group.AddEntry();
			entry.AddTag(111, 111);
			entry.AddRepeatingGroup(456);

			var nestedGroup = entry.GetRepeatingGroup(456);

			nestedGroup.AddEntry().AddTag(222, 222);

			ClassicAssert.AreEqual("8=FIX.4.4 | 35=8 | 123=1 | 111=111 | 456=1 | 222=222 | ", msg.ToPrintableString());
			ClassicAssert.AreEqual("123=1 | 111=111 | 456=1 | 222=222 | ", msg.GetRepeatingGroup(123).ToPrintableString());
			ClassicAssert.AreEqual("456=1 | 222=222 | ",
				msg.GetRepeatingGroup(123).GetEntry(0).GetRepeatingGroup(456).ToPrintableString());
		}

		[Test]
		public virtual void RemoveEmptyRepeatingGroupByLeadingTag()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			ClassicAssert.IsFalse(msg.IsRepeatingGroupExists(123));
			var group = msg.AddRepeatingGroup(123);
			ClassicAssert.IsTrue(msg.IsRepeatingGroupExists(123));

			msg.RemoveRepeatingGroup(123);
			ClassicAssert.IsFalse(msg.IsRepeatingGroupExists(123));

			group = msg.AddRepeatingGroup(123);
			var entry = group.AddEntry();

			var nestedGroup = entry.AddRepeatingGroup(456);

			entry.RemoveRepeatingGroup(456);
			ClassicAssert.IsFalse(entry.IsRepeatingGroupExists(456));
			entry.AddRepeatingGroup(456);
		}

		[Test]
		public virtual void RemoveEntry()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"382=2\u0001375=1\u0001337=2\u0001375=3\u0001437=4\u0001" + "454=1\u0001455=5\u0001456=6\u0001" +
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001" + "518=1\u0001519=10\u0001520=11\u0001" +
				"555=2\u0001600=12\u0001603=13\u0001" +
				"604=1\u0001605=14\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001" +
				"539=2\u0001524=23\u0001524=24\u0001525=25\u000110=124\u0001";
			Msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());

			var group = RepeatingGroupPool.RepeatingGroup;
			Msg.GetRepeatingGroup(555, group);
			ClassicAssert.AreEqual(
				"555=2 | " + "600=12 | 603=13 | 604=1 | 605=14 | 251=15 | 539=2 | 524=16 | 525=17 | 524=18 | " +
				"600=19 | 603=20 | 604=2 | 605=21 | 605=32 | 251=22 | 539=2 | 524=23 | 524=24 | 525=25 | ",
				group.ToPrintableString());

			group.RemoveEntry(0);
			ClassicAssert.AreEqual(
				"555=1 | 600=19 | 603=20 | 604=2 | 605=21 | 605=32 | 251=22 | 539=2 | 524=23 | 524=24 | 525=25 | ",
				group.ToPrintableString());

			group.Release();

			group = Msg.GetRepeatingGroup(232);
			ClassicAssert.AreEqual("232=2 | 233=7 | 234=8 | 233=9 | 234=9 | ", group.ToPrintableString());
			var entry = group.GetEntry(1);
			entry.Remove();
			ClassicAssert.AreEqual("232=1 | 233=7 | 234=8 | ", group.ToPrintableString());

			group.RemoveEntry(0);
			ClassicAssert.AreEqual("", group.ToPrintableString());

			group = Msg.GetRepeatingGroup(454);
			ClassicAssert.AreEqual("454=1 | 455=5 | 456=6 | ", group.ToPrintableString());
			entry = group.GetEntry(0);
			group.RemoveEntry(entry);
			ClassicAssert.AreEqual("", group.ToPrintableString());

			group = Msg.GetRepeatingGroup(382);
			ClassicAssert.AreEqual("382=2 | 375=1 | 337=2 | 375=3 | 437=4 | ", group.ToPrintableString());
			entry = group.GetEntry(0);
			var entry1 = group.GetEntry(1);

			entry.Remove();
			entry1.Remove();
			ClassicAssert.AreEqual("", group.ToPrintableString());

			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "518=1\u0001519=10\u0001520=11\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg,
				"555=1\u0001600=19\u0001603=20\u0001604=2\u0001251=22\u0001539=2\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "604=2\u0001605=21\u0001605=32\u0001",
				Msg.GetRepeatingGroup(555).GetEntry(0).GetRepeatingGroup(604));
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "539=2\u0001524=23\u0001524=24\u0001525=25\u0001",
				Msg.GetRepeatingGroup(555).GetEntry(0).GetRepeatingGroup(539));

			var fixMsgWithNewGroup =
				"8=FIX.4.3 | 9=94 | 35=8 | 49=target | 56=sender | 115=onBehalf | 34=1 | 50=senderSub | 52=20080212-04:15:18.308 | " +
				"518=1 | 519=10 | 520=11 | " + "555=1 | 600=19 | 603=20 | 604=2 | 605=21 | 605=32 | " +
				"251=22 | 539=2 | 524=23 | 524=24 | 525=25 | 10=124 | ";
			ClassicAssert.AreEqual(fixMsgWithNewGroup, Msg.ToPrintableString());
		}

		[Test]
		public virtual void RemoveGroup()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"518=1\u0001519=10\u0001520=11\u0001" + "555=2\u0001" +
				"600=12\u0001603=13\u0001604=1\u0001605=14\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001539=2\u0001524=23\u0001524=24\u0001525=25\u0001" +
				"10=124\u0001";
			Msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());
			var group = Msg.GetRepeatingGroup(555);
			ClassicAssert.AreEqual(
				"555=2 | " + "600=12 | 603=13 | 604=1 | 605=14 | 251=15 | 539=2 | 524=16 | 525=17 | 524=18 | " +
				"600=19 | 603=20 | 604=2 | 605=21 | 605=32 | 251=22 | 539=2 | 524=23 | 524=24 | 525=25 | ",
				group.ToPrintableString());
			group.Remove();

			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "518=1\u0001519=10\u0001520=11\u0001", null);

			var expected =
				"8=FIX.4.3 | 9=94 | 35=8 | 49=target | 56=sender | 115=onBehalf | 34=1 | 50=senderSub | 52=20080212-04:15:18.308 | " +
				"518=1 | 519=10 | 520=11 | " + "10=124 | ";
			ClassicAssert.AreEqual(expected, Msg.ToPrintableString());
		}

		[Test]
		public virtual void RemoveGroupInOrder()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"518=1\u0001519=10\u0001520=11\u0001" + "555=2\u0001" +
				"600=12\u0001603=13\u0001604=1\u0001605=14\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001539=2\u0001524=23\u0001524=24\u0001525=25\u0001" +
				"10=124\u0001";
			Msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());
			var group = Msg.GetRepeatingGroup(555);
			ClassicAssert.AreEqual(
				"555=2 | " + "600=12 | 603=13 | 604=1 | 605=14 | 251=15 | 539=2 | 524=16 | 525=17 | 524=18 | " +
				"600=19 | 603=20 | 604=2 | 605=21 | 605=32 | 251=22 | 539=2 | 524=23 | 524=24 | 525=25 | ",
				group.ToPrintableString());
			group.RemoveEntry(0);
			group.RemoveEntry(0);

			var expected =
				"8=FIX.4.3 | 9=94 | 35=8 | 49=target | 56=sender | 115=onBehalf | 34=1 | 50=senderSub | 52=20080212-04:15:18.308 | " +
				"518=1 | 519=10 | 520=11 | 10=124 | ";
			ClassicAssert.AreEqual(expected, Msg.ToPrintableString());

			group = Msg.GetRepeatingGroup(555);
			ClassicAssert.IsNotNull(group);
		}

		[Test]
		public virtual void RemoveTagBeforeRgForClonedMessage()
		{
			var strMsg =
				("8=FIX.4.4 | 35=D | 49=CLIENT | 58=TEXT | 453=1 | 448=TRADER | 447=D | 452=11 | 999=TEXT | " + "")
				.Replace(" | ", "\u0001");

			var msg = RawFixUtil.GetFixMessage(strMsg.AsByteArray());
			msg.IsRepeatingGroupExists(453);

			var clone = (FixMessage)msg.Clone();
			clone.IsRepeatingGroupExists(453);

			clone.RemoveTag(Tags.BeginString);

			ClassicAssert.AreEqual("35=D | 49=CLIENT | 58=TEXT | 453=1 | 448=TRADER | 447=D | 452=11 | 999=TEXT | ",
				clone.ToPrintableString());
		}

		[Test]
		public virtual void ReuseEntry()
		{
			var msgStr = "8=FIX.4.3\u000135=8\u000110=124\u0001";
			var msg = RawFixUtil.GetFixMessage(msgStr.AsByteArray());

			var rgForAdd = msg.AddRepeatingGroupAtIndex(2, 454);
			var entryForAdd = rgForAdd.AddEntry();
			entryForAdd.AddTag(455, 5);

			var entryForGet = rgForAdd.GetEntry(0);
			var entryForGetFromPool = RepeatingGroupPool.Entry;
			rgForAdd.GetEntry(0, entryForGetFromPool);

			ClassicAssert.AreSame(entryForAdd, entryForGet);
			ClassicAssert.AreNotSame(entryForAdd, entryForGetFromPool);
		}

		[Test]
		public virtual void ReuseGroup()
		{
			var msgStr = "8=FIX.4.3\u000135=8\u000110=124\u0001";
			var msg = RawFixUtil.GetFixMessage(msgStr.AsByteArray());

			var rgForAdd = msg.AddRepeatingGroupAtIndex(2, 454);
			var entry = rgForAdd.AddEntry();
			entry.AddTag(455, 5);

			var rgForGetFromPool = RepeatingGroupPool.RepeatingGroup;
			var rgForGet = msg.GetRepeatingGroup(454);
			msg.GetRepeatingGroup(454, rgForGetFromPool);

			ClassicAssert.AreSame(rgForAdd, rgForGet);
			ClassicAssert.AreNotSame(rgForGetFromPool, rgForGet);
		}

		[Test]
		public virtual void SaveEntriesInMixedMode()
		{
			var group = Msg.AddRepeatingGroupAtIndex(9, 250, false);
			var firstEntry = group.AddEntry();
			var secondEntry = group.AddEntry();

			secondEntry.AddTag(21, 21);
			firstEntry.AddTag(21, 11);
			secondEntry.AddTag(22, 22);
			firstEntry.AddTag(22, 12);
			secondEntry.AddTag(23, 23);
			firstEntry.AddTag(23, 13);

			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg,
				"250=2\u000121=11\u000122=12\u000123=13\u000121=21\u000122=22\u000123=23\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "454=1\u0001455=5\u0001456=6\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg,
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "518=1\u0001519=10\u0001520=11\u0001", null);

			var expectedMessage =
				"8=FIX.4.3 | 9=94 | 35=8 | 49=target | 56=sender | 115=onBehalf | 34=1 | 50=senderSub | 52=20080212-04:15:18.308 | " +
				"250=2 | 21=11 | 22=12 | 23=13 | 21=21 | 22=22 | 23=23 | " + "454=1 | 455=5 | 456=6 | " +
				"232=2 | 233=7 | 234=8 | 233=9 | 234=9 | " + "518=1 | 519=10 | 520=11 | " + "10=124 | ";
			ClassicAssert.AreEqual(expectedMessage, Msg.ToPrintableString());
		}

		[Test]
		public virtual void SaveEntriesInMixedMode2()
		{
			var group = Msg.AddRepeatingGroupAtIndex(9, 250, false);
			var firstEntry = group.AddEntry();
			var secondEntry = group.AddEntry();

			firstEntry.AddTag(21, 11);
			secondEntry.AddTag(21, 21);
			firstEntry.AddTag(22, 12);
			secondEntry.AddTag(22, 22);
			firstEntry.AddTag(23, 13);
			secondEntry.AddTag(23, 23);

			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg,
				"250=2\u000121=11\u000122=12\u000123=13\u000121=21\u000122=22\u000123=23\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "454=1\u0001455=5\u0001456=6\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg,
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "518=1\u0001519=10\u0001520=11\u0001", null);

			var expectedMessage =
				"8=FIX.4.3 | 9=94 | 35=8 | 49=target | 56=sender | 115=onBehalf | 34=1 | 50=senderSub | 52=20080212-04:15:18.308 | " +
				"250=2 | 21=11 | 22=12 | 23=13 | 21=21 | 22=22 | 23=23 | " + "454=1 | 455=5 | 456=6 | " +
				"232=2 | 233=7 | 234=8 | 233=9 | 234=9 | " + "518=1 | 519=10 | 520=11 | " + "10=124 | ";
			ClassicAssert.AreEqual(expectedMessage, Msg.ToPrintableString());
		}

		[Test]
		public virtual void SaveNewestEntryFirst1()
		{
			var group = Msg.AddRepeatingGroupAtIndex(9, 250, false);
			var firstEntry = group.AddEntry();
			var secondEntry = group.AddEntry();

			secondEntry.AddTag(21, 21);
			secondEntry.AddTag(22, 22);
			secondEntry.AddTag(23, 23);

			firstEntry.AddTag(21, 11);
			firstEntry.AddTag(22, 12);
			firstEntry.AddTag(23, 13);

			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg,
				"250=2\u000121=11\u000122=12\u000123=13\u000121=21\u000122=22\u000123=23\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "454=1\u0001455=5\u0001456=6\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg,
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "518=1\u0001519=10\u0001520=11\u0001", null);

			var expectedMessage =
				"8=FIX.4.3 | 9=94 | 35=8 | 49=target | 56=sender | 115=onBehalf | 34=1 | 50=senderSub | 52=20080212-04:15:18.308 | " +
				"250=2 | 21=11 | 22=12 | 23=13 | 21=21 | 22=22 | 23=23 | " + "454=1 | 455=5 | 456=6 | " +
				"232=2 | 233=7 | 234=8 | 233=9 | 234=9 | " + "518=1 | 519=10 | 520=11 | " + "10=124 | ";
			ClassicAssert.AreEqual(expectedMessage, Msg.ToPrintableString());
		}

		[Test]
		public virtual void SecondAddSameNestedRgThrowException()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			var parent = msg.AddRepeatingGroup(123);
			var entry = parent.AddEntry();
			var nested1 = entry.AddRepeatingGroup(345);

			//throw exception as duplicate group
			ClassicAssert.Throws<DuplicateGroupException>(() =>
			{
				var nested2 = entry.AddRepeatingGroup(345);
			});
		}

		[Test]
		public virtual void SecondAddSameNestedRgThrowException2()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			var parent = msg.AddRepeatingGroup(123);
			var entry = parent.AddEntry();
			var nested1 = entry.AddRepeatingGroup(345);
			nested1.AddEntry().AddTag(1, 1);
			nested1.RemoveEntry(0);

			//throw exception as duplicate group
			ClassicAssert.Throws<DuplicateGroupException>(() =>
			{
				var nested2 = entry.AddRepeatingGroup(345);
			});
		}

		[Test]
		public virtual void SecondAddSameRgThrowException()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			var group1 = msg.AddRepeatingGroup(123);

			//throw exception as duplicate group
			ClassicAssert.Throws<DuplicateGroupException>(() =>
			{
				var group2 = msg.AddRepeatingGroup(123);
			});
		}

		[Test]
		public virtual void SecondAddSameRgThrowException2()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			var group1 = msg.AddRepeatingGroup(123);

			//throw exception as duplicate group
			ClassicAssert.Throws<DuplicateGroupException>(() =>
			{
				var group2 = msg.AddRepeatingGroup(123);
			});
		}

		[Test]
		public virtual void TestAddGroupBeforeRg()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "D");
			msg.Set(49, "CLIENT");
			msg.Set(58, "TEXT");

			var rg = msg.AddRepeatingGroup(453);
			var rge = rg.AddEntry();
			rge.AddTag(448, "TRADER");
			rge.AddTag(447, "D");
			rge.AddTag(452, "11");

			msg.AddTag(999, "TEXT");
			ClassicAssert.AreEqual(
				"8=FIX.4.4 | 35=D | 49=CLIENT | 58=TEXT | 453=1 | 448=TRADER | 447=D | 452=11 | 999=TEXT | ",
				msg.ToPrintableString());

			ClassicAssert.IsNotNull(msg.IsTagExists(453), "Tag 453 should exist");

			var rg2 = msg.AddRepeatingGroupAtIndex(msg.GetTagIndex(58),
				111); // THIS REMOVAL CAUSES FURTHER RG ADDITIONS TO FAIL
			var entry = rg2.AddEntry();
			entry.AddTag(222, "222");
			ClassicAssert.AreEqual(
				"8=FIX.4.4 | 35=D | 49=CLIENT | 111=1 | 222=222 | 58=TEXT | " +
				"453=1 | 448=TRADER | 447=D | 452=11 | 999=TEXT | ", msg.ToPrintableString());

			// Add another party entry
			rg = msg.GetRepeatingGroup(453);

			rge = rg.AddEntry();
			rge.AddTag(448, "Mickey");
			rge.AddTag(447, "D");
			rge.AddTag(452, "3");
			ClassicAssert.AreEqual(
				"8=FIX.4.4 | 35=D | 49=CLIENT | 111=1 | 222=222 | 58=TEXT | 453=2 | 448=TRADER | 447=D | 452=11 | " +
				"448=Mickey | 447=D | 452=3 | 999=TEXT | ", msg.ToPrintableString());

			ClassicAssert.AreEqual(2, rg.Count, "Incorrect entries");
		}

		[Test]
		public virtual void TestAddRemoveEmptyRepeatingGroupEithEmptyEntry()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");
			var legGroup = msg.AddRepeatingGroup(454);
			var entry = legGroup.AddEntry();
			msg.RemoveRepeatingGroup(454);
			msg.AddRepeatingGroup(454);
		}

		[Test]
		public virtual void TestAddTagBeforeRg()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "D");
			msg.Set(49, "CLIENT");
			msg.Set(58, "TEXT");

			var rg = msg.AddRepeatingGroup(453);
			var rge = rg.AddEntry();
			rge.AddTag(448, "TRADER");
			rge.AddTag(447, "D");
			rge.AddTag(452, "11");

			msg.AddTag(999, "TEXT");
			ClassicAssert.AreEqual(
				"8=FIX.4.4 | 35=D | 49=CLIENT | 58=TEXT | " + "453=1 | 448=TRADER | 447=D | 452=11 | 999=TEXT | ",
				msg.ToPrintableString());

			ClassicAssert.IsNotNull(msg.IsTagExists(453), "Tag 453 should exist");

			msg.AddTagAtIndex(msg.GetTagIndex(58), 158, (long)158);
			ClassicAssert.AreEqual(
				"8=FIX.4.4 | 35=D | 49=CLIENT | 158=158 | 58=TEXT | " +
				"453=1 | 448=TRADER | 447=D | 452=11 | 999=TEXT | ", msg.ToPrintableString());

			// Add another party entry
			rg = msg.GetRepeatingGroup(453);

			rge = rg.AddEntry();
			rge.AddTag(448, "Mickey");
			rge.AddTag(447, "D");
			rge.AddTag(452, "3");
			ClassicAssert.AreEqual(
				"8=FIX.4.4 | 35=D | 49=CLIENT | 158=158 | 58=TEXT | 453=2 | 448=TRADER | 447=D | 452=11 | " +
				"448=Mickey | 447=D | 452=3 | 999=TEXT | ", msg.ToPrintableString());

			ClassicAssert.AreEqual(2, rg.Count, "Incorrect entries");
		}

		[Test]
		public virtual void TestCopyFullMessage()
		{
			var copy = new FixMessage();
			Msg.DeepCopyTo(copy);

			var rgOriginal = Msg.GetRepeatingGroup(232);
			var rgCopy = copy.GetRepeatingGroup(232);

			rgOriginal.RemoveEntry(0);

			var entry = rgCopy.AddEntry();
			entry.AddTag(233, 111);
			entry.AddTag(234, 222);

			ClassicAssert.AreEqual(
				"8=FIX.4.3 | 9=94 | 35=8 | 49=target | 56=sender | 115=onBehalf | 34=1 | 50=senderSub | 52=20080212-04:15:18.308 | " +
				"454=1 | 455=5 | 456=6 | " + "232=1 | 233=9 | 234=9 | " + "518=1 | 519=10 | 520=11 | 10=124 | ",
				Msg.ToPrintableString());
			ClassicAssert.AreEqual(
				"8=FIX.4.3 | 9=94 | 35=8 | 49=target | 56=sender | 115=onBehalf | 34=1 | 50=senderSub | 52=20080212-04:15:18.308 | " +
				"454=1 | 455=5 | 456=6 | " + "232=3 | 233=7 | 234=8 | 233=9 | 234=9 | 233=111 | 234=222 | " +
				"518=1 | 519=10 | 520=11 | 10=124 | ", copy.ToPrintableString());
		}

		[Test]
		public virtual void TestEntryToByteArray()
		{
			var msgStr = "555=2\u0001600=12\u0001603=13\u0001" +
						"604=1\u0001605=14\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
						"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001" +
						"539=2\u0001524=23\u0001524=24\u0001525=25\u0001";
			Msg = RawFixUtil.GetFixMessage(msgStr.AsByteArray());
			Msg = RawFixUtil.IndexRepeatingGroup(Msg, FixVersion.Fix43, "8");
			var expected =
				"600=12\u0001603=13\u0001604=1\u0001605=14\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001";
			ClassicAssert.That(Msg.GetRepeatingGroup(555).GetEntry(0).AsByteArray(), Is.EquivalentTo(expected.AsByteArray()));
		}

		[Test]
		public virtual void TestGetOrAddAtIndexMethods()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");
			msg.Set(10, "12");

			ClassicAssert.IsFalse(msg.IsRepeatingGroupExists(123));
			var group = msg.GetOrAddRepeatingGroupAtIndex(123, 2);
			ClassicAssert.IsNotNull(group);
			ClassicAssert.IsTrue(msg.IsRepeatingGroupExists(123));

			var entry = group.AddEntry();
			entry.AddTag(1111, 15);

			ClassicAssert.AreEqual("8=FIX.4.4 | 35=8 | 123=1 | 1111=15 | 10=12 | ", msg.ToPrintableString());
			group.Remove();

			ClassicAssert.IsFalse(msg.IsRepeatingGroupExists(123));
			group = RepeatingGroupPool.RepeatingGroup;

			msg.GetOrAddRepeatingGroupAtIndex(123, 2, group);
			ClassicAssert.IsNotNull(group);
			ClassicAssert.IsTrue(msg.IsRepeatingGroupExists(123));

			entry = group.AddEntry();
			entry.AddTag(1111, 15);

			ClassicAssert.AreEqual("8=FIX.4.4 | 35=8 | 123=1 | 1111=15 | 10=12 | ", msg.ToPrintableString());
		}

		[Test]
		public virtual void TestGetOrAddMethods()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			ClassicAssert.IsFalse(msg.IsRepeatingGroupExists(123));
			var group = msg.GetOrAddRepeatingGroup(123);
			ClassicAssert.IsNotNull(group);
			ClassicAssert.IsTrue(msg.IsRepeatingGroupExists(123));

			var entry = group.AddEntry();
			entry.AddTag(1111, 15);
			ClassicAssert.IsFalse(entry.IsRepeatingGroupExists(456));
			var nestedGroup = entry.GetOrAddRepeatingGroup(456);
			ClassicAssert.IsTrue(entry.IsRepeatingGroupExists(456));
			nestedGroup.AddEntry().AddTag(2222, 25);

			ClassicAssert.AreEqual("8=FIX.4.4 | 35=8 | 123=1 | 1111=15 | 456=1 | 2222=25 | ", msg.ToPrintableString());
			group.Remove();
			ClassicAssert.AreEqual("8=FIX.4.4 | 35=8 | ", msg.ToPrintableString());

			ClassicAssert.IsFalse(msg.IsRepeatingGroupExists(123));
			group = RepeatingGroupPool.RepeatingGroup;
			nestedGroup = RepeatingGroupPool.RepeatingGroup;

			msg.GetOrAddRepeatingGroup(123, group);
			ClassicAssert.IsNotNull(group);
			ClassicAssert.IsTrue(msg.IsRepeatingGroupExists(123));

			entry = group.AddEntry();
			entry.AddTag(1111, 15);
			ClassicAssert.IsFalse(entry.IsRepeatingGroupExists(456));
			entry.GetOrAddRepeatingGroup(456, nestedGroup);
			ClassicAssert.IsTrue(entry.IsRepeatingGroupExists(456));
			nestedGroup.AddEntry().AddTag(2222, 25);

			ClassicAssert.AreEqual("8=FIX.4.4 | 35=8 | 123=1 | 1111=15 | 456=1 | 2222=25 | ", msg.ToPrintableString());
		}

		[Test]
		public virtual void TestGetSize()
		{
			var group = Msg.GetRepeatingGroup(232);
			ClassicAssert.AreEqual("232=2 | 233=7 | 234=8 | 233=9 | 234=9 | ", group.ToPrintableString());
			ClassicAssert.AreEqual(2, @group.Count);
			group.RemoveEntry(0);
			ClassicAssert.AreEqual(1, @group.Count);
			group.RemoveEntry(0);
			ClassicAssert.AreEqual(0, @group.Count);

			var entry = Msg.GetRepeatingGroup(454).GetEntry(0);
			ClassicAssert.AreEqual("455=5 | 456=6 | ", entry.ToPrintableString());
			ClassicAssert.AreEqual(2, entry.Count);
			entry.RemoveTag(455);
			ClassicAssert.AreEqual(1, entry.Count);
			entry.RemoveTag(456);
			ClassicAssert.AreEqual(0, entry.Count);
			entry.AddTag(455, 123);
			ClassicAssert.AreEqual(1, entry.Count);
		}

		[Test]
		public virtual void TestGettersForRemovedGroups()
		{
			ClassicAssert.IsFalse(Msg.IsRepeatingGroupExists(555));
			var group = Msg.AddRepeatingGroup(555);
			ClassicAssert.IsTrue(Msg.IsRepeatingGroupExists(555));

			var entry = group.AddEntry();

			ClassicAssert.IsFalse(entry.IsRepeatingGroupExists(604));
			entry.AddTag(600, "delimiter");
			ClassicAssert.IsFalse(entry.IsRepeatingGroupExists(604));
			var nestedGroup = entry.AddRepeatingGroup(604);
			ClassicAssert.IsTrue(entry.IsRepeatingGroupExists(604));

			var nestedEntry = nestedGroup.AddEntry();
			nestedEntry.AddTag(605, "nested group delimiter");
			ClassicAssert.AreEqual("555=1 | 600=delimiter | 604=1 | 605=nested group delimiter | ", group.ToPrintableString());

			group.Remove();

			ClassicAssert.IsNull(Msg.GetRepeatingGroup(555));
			ClassicAssert.IsFalse(nestedEntry.IsRepeatingGroupExists(604));
			ClassicAssert.IsFalse(Msg.IsRepeatingGroupExists(555));
			ClassicAssert.IsFalse(Msg.IsTagExists(604));
			ClassicAssert.IsFalse(Msg.IsTagExists(555));
		}

		[Test]
		public virtual void TestIndexTwoInnerGroup()
		{
			var msgStr = "555=1\u0001600=12\u0001603=13\u0001" +
						"604=1\u0001605=14\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001251=15\u0001";
			Msg = RawFixUtil.GetFixMessage(msgStr.AsByteArray());
			Msg = RawFixUtil.IndexRepeatingGroup(Msg, FixVersion.Fix43, "8");

			ClassicAssert.AreEqual(msgStr, Msg.GetRepeatingGroup(555).ToString());
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg,
				"555=1\u0001600=12\u0001603=13\u0001604=1\u0001539=2\u0001251=15\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "604=1\u0001605=14\u0001",
				Msg.GetRepeatingGroup(555).GetEntry(0).GetRepeatingGroup(604));
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "539=2\u0001524=16\u0001525=17\u0001524=18\u0001",
				Msg.GetRepeatingGroup(555).GetEntry(0).GetRepeatingGroup(539));
		}

		[Test]
		public virtual void TestInvalidCounterTagThrowException()
		{
			var newOrderTestMessage = RawFixUtil.GetFixMessage(
				("8=FIXT.1.1\u00019=290\u000135=D\u000149=mystery_om61\u0001" +
				"56=UBS_FRC_OM61\u000134=2\u000152=20180214-10:17:46\u000111=TTf0ef7\u000144=106.85\u000138=80000\u0001110=10000\u0001" +
				"40=2\u00015010=1\u000148=US94106LAY56\u000122=4\u000154=1\u000160=20180214-10:17:32\u000159=0\u0001423=1\u0001" +
				"453=2\u0001448=artem.holubiev@ubs.com\u0001447=D\u0001452=12\u0001448=56462\u0001447=D\u0001452=24\u0001" +
				"448=UBS_BP\u0001447=D\u0001452=73\u0001802=1\u0001523=UBS_BP\u0001803=32\u000110=134\u0001")
				.AsByteArray());
			ClassicAssert.Throws<InvalidLeadingTagValueException>(() =>
			{
				RawFixUtil.IndexRepeatingGroup(newOrderTestMessage,
					FixVersionContainer.GetFixVersionContainer(FixVersion.Fix50Sp2), "D");
			});
		}

		[Test]
		public virtual void TestMessageClean()
		{
			var strMsgWithoutRg =
				"8=FIX.4.4 | 9=107 | 35=B | 34=2 | 49=senderId | 56=targetId | 52=20170222-12:52:35.628 | " +
				"148=Hello there | 33=3 | 58=line1 | 58=line2 | 58=line3 | 10=232 | ";
			var strMsgWithRg =
				"8=FIX.4.4 | 9=243 | 35=D | 34=3 | 49=senderId | 56=targetId | 52=20170222-12:52:35.629 | " +
				"11=1686085644 | 21=1 | 55=FFIZ7 | 54=2 | 60=20170221-18:06:35 | 40=2 | 44=6735 | 38=1 | 22=5 | " +
				"1=80029 | 58=Route=US EXCHANGE | " +
				"453=1 | 448=JAVID | 447=D | 452=11 | 48=FFIZ7 | 200=201712 | 207=IFLL | 59=0 | 167=FUT | 1028=N | 10=01E | ";

			var msg = FixMessageFactory.NewInstanceFromPoolForEngineParse();

			var buffer = strMsgWithoutRg.Replace(" | ", "\u0001").AsByteArray();
			RawFixUtil.GetFixMessage(msg, buffer, 0, buffer.Length, new DefaultRawTags());
			ClassicAssert.AreEqual(strMsgWithoutRg, msg.ToPrintableString());
			//call to RG API build RG index for this message
			ClassicAssert.IsFalse(msg.IsRepeatingGroupExists(453));

			// clear message - RG index should be cleaned too
			((AbstractFixMessage)msg).Clear();

			buffer = strMsgWithRg.Replace(" | ", "\u0001").AsByteArray();
			RawFixUtil.GetFixMessage(msg, buffer, 0, buffer.Length, new DefaultRawTags());
			//call to RG API build RG index for this NEW message
			ClassicAssert.AreEqual(strMsgWithRg, msg.ToPrintableString());
			ClassicAssert.IsTrue(msg.IsRepeatingGroupExists(453));
		}

		[Test]
		public virtual void TestParse()
		{
			var msgStr =
				"8=FIX.4.4\u000135=8\u0001555=1\u0001600=ZNZ5\u0001604=2\u0001605=TYZ5\u0001606=5\u0001605=TYZ5 Comdty\u0001606=A\u0001";
			var list = RawFixUtil.GetFixMessage(msgStr.AsByteArray());
			var leg1 = list.GetRepeatingGroup(555).GetEntry(0);
			ClassicAssert.AreEqual("600=ZNZ5 | 604=2 | 605=TYZ5 | 606=5 | 605=TYZ5 Comdty | 606=A | ",
				leg1.ToPrintableString());

			var group = leg1.GetRepeatingGroup(604);
			ClassicAssert.AreEqual("604=2 | 605=TYZ5 | 606=5 | 605=TYZ5 Comdty | 606=A | ", group.ToPrintableString());

			group.Remove();

			group = leg1.AddRepeatingGroup(604);
			var secAltId1 = group.AddEntry();
			secAltId1.UpdateValue(605, "TYZ5", IndexedStorage.MissingTagHandling.AddIfNotExists);
			secAltId1.UpdateValue(606, "5", IndexedStorage.MissingTagHandling.AddIfNotExists);

			var secAltId2 = group.AddEntry();
			secAltId2.UpdateValue(605, "TYZ5 Comdty", IndexedStorage.MissingTagHandling.AddIfNotExists);
			secAltId2.UpdateValue(606, "A", IndexedStorage.MissingTagHandling.AddIfNotExists);


			ClassicAssert.AreEqual(
				"8=FIX.4.4 | 35=8 | 555=1 | 600=ZNZ5 | 604=2 | 605=TYZ5 | 606=5 | 605=TYZ5 Comdty | 606=A | ",
				list.ToPrintableString());
			ClassicAssert.AreEqual("600=ZNZ5 | 604=2 | 605=TYZ5 | 606=5 | 605=TYZ5 Comdty | 606=A | ",
				leg1.ToPrintableString());

			ClassicAssert.AreEqual(2, leg1.GetTagValueAsLong(604));
			ClassicAssert.AreEqual("TYZ5", secAltId1.GetTagValueAsString(605));
			ClassicAssert.AreEqual("5", secAltId1.GetTagValueAsString(606));
			ClassicAssert.AreEqual("TYZ5 Comdty", secAltId2.GetTagValueAsString(605));
			ClassicAssert.AreEqual("A", secAltId2.GetTagValueAsString(606));
		}

		[Test]
		public virtual void TestParsingDeepNested()
		{
			var msgStr = "8=FIXT.1.1|9=725|35=AE|1128=9|34=2417|49=FFASTFILL_UAT|52=20171018-12:26:59.515|56"
						+ "=RBS_UK_UAT_TFC|17=FIM7JRJjtd0|31=1723.5|32=9|55=TP|60=20171018-13:36:00|75=20171018|167"
						+ "=FUT|200=201712|207=OSE|442=1|487=2|571=20171018.577|572=20171018.354|715=20171018|779"
						+ "=20171018-12:26:58.293|819=0|828=0|1003=403823794/0|1188=0|10053=1|10056=Y|10111=80029"
						+ "|10112=1C95C19FE|10113=0.00|10114=024|10115=EU_NEWEDGE_FUT|10116=AVGSEV|10117=1C95C19FE"
						+ "|10118=0.00|10119=024|10120=EU_NEWEDGE_FUT|1907=1|1903"
						+ "=1030434505NELDN17101723224860702463578001C95C19FE|1906=0|552=1|54=2|453=2|448=RBS|452=4"
						+ "|802=1|523=RBS|803=1|448=Manual|452=21|12=0|13=1|78=1|79=80029|756=4|757=80029|759=24|757"
						+ "=80029|759=45|757=House|759=38|806=1|760=House|807=26|757=RBS|759=78|80=9|37=FIM7JRJjtd0"
						+ "|10=176|";
			var msg = RawFixUtil.GetFixMessage(msgStr.Replace("|", "\x0001").AsByteArray());
			var noSides = msg.GetRepeatingGroup(552);
			ClassicAssert.IsNotNull(noSides);
			ClassicAssert.AreEqual(
				"552=1 | 54=2 | 453=2 | 448=RBS | 452=4 | 802=1 | 523=RBS | 803=1 | 448=Manual | 452=21 | 12=0 " +
				"| 13=1 | 78=1 | 79=80029 | 756=4 | 757=80029 | 759=24 | 757=80029 | 759=45 | 757=House | " +
				"759=38 | 806=1 | 760=House | 807=26 | 757=RBS | 759=78 | 80=9 | 37=FIM7JRJjtd0 | ",
				noSides.ToPrintableString());
		}

		[Test]
		public virtual void TestParsingNestedGroup()
		{
			var msgRaw =
				"8=FIX.4.3\u00019=516\u000135=s\u000149=SCORE5\u000156=FIXDBS\u000134=841\u000152=20170411-19:40:20.797\u0001" +
				"100=EDGO\u000140=2\u000144=0.03\u000155=MSFT\u000160=20170411-19:40:20.784\u0001167=OPT\u0001548=BDAA0010-20170411\u0001549=2\u0001" +
				"552=2\u0001" + "54=1\u000111=test\u0001" + "78=1\u0001" + "79=1\u000180=1800\u0001" +
				"54=2\u000111=test\u0001" + "78=1\u0001" + "79=1\u000180=1800\u0001" + "550=1\u000110=229\u0001";

			var msg = RawFixUtil.GetFixMessage(msgRaw.AsByteArray());
			msg = RawFixUtil.IndexRepeatingGroup(msg, true);
			var group = RepeatingGroupPool.RepeatingGroup; // Or new RepeatingGroup() or something else
			var entry = RepeatingGroupPool.Entry;
			msg.GetRepeatingGroup(552, group); // Leading tag of Repeating Group

			group.GetEntry(0, entry); //Number of entry in group

			var nestedGroup = RepeatingGroupPool.RepeatingGroup;
			entry.GetRepeatingGroup(78, nestedGroup);

			var nestedEntry = RepeatingGroupPool.Entry;
			nestedGroup.GetEntry(0, nestedEntry);
			ClassicAssert.AreEqual("79=1 | 80=1800 | ", nestedEntry.ToPrintableString());
		}

		[Test]
		public virtual void TestPrintForEmptyGroup()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			var group = msg.AddRepeatingGroup(123);
			ClassicAssert.AreEqual("8=FIX.4.4 | 35=8 | ", msg.ToPrintableString());
		}

		[Test]
		public virtual void TestReindexInvalid()
		{
			var msg = new FixMessage();
			msg.AddTag(8, "FIX.5.0");
			msg.AddTag(35, "W");
			msg.AddTag(55, "EUR/USD");
			msg.AddTag(268, (long)2);
			msg.AddTag(269, (long)0);
			msg.AddTag(453, (long)0);
			msg.AddTag(5555, (long)0);
			msg.AddTag(269, (long)1);
			msg.AddTag(10, (long)0);

			var bytes = msg.AsByteArray();
			var fixMessage = RawFixUtil.GetFixMessage(bytes);
			fixMessage = RawFixUtil.IndexRepeatingGroup(fixMessage, false);

			ClassicAssert.Throws<InvalidLeadingTagValueException>(() => fixMessage = RawFixUtil.IndexRepeatingGroup(fixMessage, true));
			var repeatingGroup = fixMessage.GetRepeatingGroup(268);
		}

		[Test]
		public virtual void TestRemoveEmptyGroup()
		{
			var raw = "8=FIX.4.4|9=533|35=8|34=139|49=RBS_UK_UAT_TFC|52=20170525-10:56:44.695|56=RBS_UK_UAT_TES|" +
					"1=U7B008|11=2201071|14=0|17=20170525.44|19=20170525.43|31=98.695|32=5|37=UO000027FD001CH|" +
					"39=0|54=1|55=ED|58=UDVRBS|60=20170525-11:56:16|75=20170525|150=G|151=0|198=776324604|" +
					"200=201709|207=CME|526=A81097021|527=|715=20170525|828=0|10056=N|10124=|10125=|10126=|" +
					"10127=|10128=|10129=3|10130=2|10131=0|10132=0|10133=0|" +
					"453=6|448=415|452=4|802=1|523=RBS_Firm_415|803=1|448=EUDENT|452=12|448=CME|452=21|448=OB.FID-GUI.US|452=62|448=U7B008|452=3|448=RBS|452=78|10=010|18990=113078119551451798.8.20170525.44|18991=Seals|18994=UAT|18992=UK|461=FXXXS|";
			raw = raw.Replace("|", "\u0001");
			var msg = RawFixUtil.GetFixMessage(raw.AsByteArray());

			//add empty group
			var legGroup = msg.AddRepeatingGroup(454);

			//remove empty group
			msg.RemoveRepeatingGroup(454);
		}

		[Test]
		public virtual void TestRemoveGroupBeforeRg()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "D");
			msg.Set(49, "CLIENT");
			msg.Set(58, "TEXT");

			var rgBefore = msg.AddRepeatingGroup(111);
			var entry = rgBefore.AddEntry();
			entry.AddTag(222, "222");

			var rg = msg.AddRepeatingGroup(453);
			var rge = rg.AddEntry();
			rge.AddTag(448, "TRADER");
			rge.AddTag(447, "D");
			rge.AddTag(452, "11");

			msg.AddTag(999, "TEXT");
			ClassicAssert.AreEqual(
				"8=FIX.4.4 | 35=D | 49=CLIENT | 58=TEXT | 111=1 | 222=222 | " +
				"453=1 | 448=TRADER | 447=D | 452=11 | 999=TEXT | ", msg.ToPrintableString());

			ClassicAssert.IsNotNull(msg.IsTagExists(453), "Tag 453 should exist");

			msg.RemoveRepeatingGroup(111);
			ClassicAssert.AreEqual(
				"8=FIX.4.4 | 35=D | 49=CLIENT | 58=TEXT | " + "453=1 | 448=TRADER | 447=D | 452=11 | 999=TEXT | ",
				msg.ToPrintableString());

			// Add another party entry
			rg = msg.GetRepeatingGroup(453);

			rge = rg.AddEntry();
			rge.AddTag(448, "Mickey");
			rge.AddTag(447, "D");
			rge.AddTag(452, "3");
			ClassicAssert.AreEqual(
				"8=FIX.4.4 | 35=D | 49=CLIENT | 58=TEXT | " +
				"453=2 | 448=TRADER | 447=D | 452=11 | 448=Mickey | 447=D | 452=3 | 999=TEXT | ",
				msg.ToPrintableString());

			ClassicAssert.AreEqual(2, rg.Count, "Incorrect entries");
		}

		[Test]
		public virtual void TestRemoveTagBeforeRg()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "D");
			msg.Set(49, "CLIENT");
			msg.Set(58, "TEXT");

			var rg = msg.AddRepeatingGroup(453);
			var rge = rg.AddEntry();
			rge.AddTag(448, "TRADER");
			rge.AddTag(447, "D");
			rge.AddTag(452, "11");

			msg.AddTag(999, "TEXT");
			ClassicAssert.AreEqual(
				"8=FIX.4.4 | 35=D | 49=CLIENT | 58=TEXT | 453=1 | 448=TRADER | 447=D | 452=11 | 999=TEXT | ",
				msg.ToPrintableString());

			ClassicAssert.IsNotNull(msg.IsTagExists(453), "Tag 453 should exist");

			msg.RemoveTag(58); // THIS REMOVAL CAN CAUSES FURTHER RG ADDITIONS TO FAIL
			ClassicAssert.AreEqual("8=FIX.4.4 | 35=D | 49=CLIENT | 453=1 | 448=TRADER | 447=D | 452=11 | 999=TEXT | ",
				msg.ToPrintableString());

			// Add another party entry
			rg = msg.GetRepeatingGroup(453);

			rge = rg.AddEntry();
			rge.AddTag(448, "Mickey");
			rge.AddTag(447, "D");
			rge.AddTag(452, "3");
			ClassicAssert.AreEqual(
				"8=FIX.4.4 | 35=D | 49=CLIENT | 453=2 | 448=TRADER | 447=D | 452=11 | " +
				"448=Mickey | 447=D | 452=3 | 999=TEXT | ", msg.ToPrintableString());

			ClassicAssert.AreEqual(2, rg.Count, "Incorrect entries");
		}

		[Test]
		public virtual void TestRgToByteArray()
		{
			var msgStr = "555=2\u0001600=12\u0001603=13\u0001" +
						"604=1\u0001605=14\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
						"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001" +
						"539=2\u0001524=23\u0001524=24\u0001525=25\u0001";
			Msg = RawFixUtil.GetFixMessage(msgStr.AsByteArray());
			Msg = RawFixUtil.IndexRepeatingGroup(Msg, FixVersion.Fix43, "8");
			ClassicAssert.That(Msg.GetRepeatingGroup(555).ToByteArray(), Is.EquivalentTo(Msg.AsByteArray()));
		}

		[Test]
		public virtual void TestToString()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"555=2\u0001600=12\u0001603=13\u0001" +
				"604=1\u0001605=14\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001" +
				"539=2\u0001524=23\u0001524=24\u0001525=25\u0001";
			Msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());
			var group = Msg.GetRepeatingGroup(555);

			var expectedToString = "555=2\u0001600=12\u0001603=13\u0001" +
									"604=1\u0001605=14\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
									"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001" +
									"539=2\u0001524=23\u0001524=24\u0001525=25\u0001";
			var expectedToPrintableString = "555=2 | 600=12 | 603=13 | " +
											"604=1 | 605=14 | 251=15 | 539=2 | 524=16 | 525=17 | 524=18 | " +
											"600=19 | 603=20 | 604=2 | 605=21 | 605=32 | 251=22 | " +
											"539=2 | 524=23 | 524=24 | 525=25 | ";

			ClassicAssert.AreEqual(expectedToString, group.ToString());
			ClassicAssert.AreEqual(expectedToPrintableString, group.ToPrintableString());
		}

		[Test]
		public virtual void TestZeroRepeatingGroup()
		{
			var msg = new FixMessage();
			msg.AddTag(8, "FIX.5.0");
			msg.AddTag(35, "W");
			msg.AddTag(55, "EUR/USD");
			msg.AddTag(268, (long)0);
			msg.AddTag(10, (long)0);

			var bytes = msg.AsByteArray();

			var fixMessage = RawFixUtil.GetFixMessage(bytes);

			fixMessage = RawFixUtil.IndexRepeatingGroup(fixMessage, false);
			var repeatingGroup = fixMessage.GetRepeatingGroup(268);
			ClassicAssert.IsNull(repeatingGroup);

			fixMessage = RawFixUtil.IndexRepeatingGroup(fixMessage, true);
			repeatingGroup = fixMessage.GetRepeatingGroup(268);
			ClassicAssert.IsNull(repeatingGroup);
		}

		[Test]
		public virtual void TestZeroRepeatingSubGroup()
		{
			var msg = new FixMessage();
			msg.AddTag(8, "FIX.5.0");
			msg.AddTag(35, "W");
			msg.AddTag(55, "EUR/USD");
			msg.AddTag(268, (long)2);
			msg.AddTag(269, (long)0);
			msg.AddTag(453, (long)0);
			msg.AddTag(269, (long)1);
			msg.AddTag(10, (long)0);

			var bytes = msg.AsByteArray();
			var fixMessage = RawFixUtil.GetFixMessage(bytes);


			fixMessage = RawFixUtil.IndexRepeatingGroup(fixMessage, false);
			var repeatingGroup = fixMessage.GetRepeatingGroup(268);
			ClassicAssert.IsNotNull(repeatingGroup);
			ClassicAssert.IsNotNull(repeatingGroup.GetEntry(0));
			ClassicAssert.IsNull(repeatingGroup.GetEntry(0).GetRepeatingGroup(453));
			ClassicAssert.IsNotNull(repeatingGroup.GetEntry(1));
			ClassicAssert.IsNull(repeatingGroup.GetEntry(1).GetRepeatingGroup(453));

			fixMessage = RawFixUtil.IndexRepeatingGroup(fixMessage, true);
			repeatingGroup = fixMessage.GetRepeatingGroup(268);
			ClassicAssert.IsNotNull(repeatingGroup);
			ClassicAssert.IsNotNull(repeatingGroup.GetEntry(0));
			ClassicAssert.IsNull(repeatingGroup.GetEntry(0).GetRepeatingGroup(453));
			ClassicAssert.IsNotNull(repeatingGroup.GetEntry(1));
			ClassicAssert.IsNull(repeatingGroup.GetEntry(1).GetRepeatingGroup(453));
		}

		[Test]
		public virtual void ThirdNestedLevel1()
		{
			var group = Msg.AddRepeatingGroupAtIndex(9, 250, false);

			var entry = group.AddEntry();
			entry.AddTag(22, 11);

			var firstSubGroup = entry.AddRepeatingGroup(251);
			var firstSubEntry = firstSubGroup.AddEntry();
			firstSubEntry.AddTag(32, 21);

			var secondSubGroup = firstSubEntry.AddRepeatingGroup(252);
			var secondSubEntry = secondSubGroup.AddEntry();
			secondSubEntry.AddTag(42, 31);

			var thirdSubGroup = secondSubEntry.AddRepeatingGroup(253);
			var thirdSubEntry = thirdSubGroup.AddEntry();
			thirdSubEntry.AddTag(52, 41);
			thirdSubEntry.AddTag(53, 42);

			secondSubEntry.AddTag(43, 32);

			firstSubEntry.AddTag(33, 22);

			entry.AddTag(23, 12);
			ClassicAssert.AreEqual(
				"250=1 | 22=11 | 251=1 | 32=21 | 252=1 | 42=31 | 253=1 | 52=41 | 53=42 | 43=32 | 33=22 | 23=12 | ",
				group.ToPrintableString());

			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "250=1\u000122=11\u0001251=1\u000123=12\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "251=1\u000132=21\u0001252=1\u000133=22\u0001",
				Msg.GetRepeatingGroup(250).GetEntry(0).GetRepeatingGroup(251));
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "252=1\u000142=31\u0001253=1\u000143=32\u0001",
				Msg.GetRepeatingGroup(250).GetEntry(0).GetRepeatingGroup(251).GetEntry(0).GetRepeatingGroup(252));
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "253=1\u000152=41\u000153=42\u0001",
				Msg.GetRepeatingGroup(250).GetEntry(0).GetRepeatingGroup(251).GetEntry(0).GetRepeatingGroup(252)
					.GetEntry(0).GetRepeatingGroup(253));
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "454=1\u0001455=5\u0001456=6\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg,
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "518=1\u0001519=10\u0001520=11\u0001", null);

			var expectedMessage =
				"8=FIX.4.3 | 9=94 | 35=8 | 49=target | 56=sender | 115=onBehalf | 34=1 | 50=senderSub | 52=20080212-04:15:18.308 | " +
				"250=1 | 22=11 | 251=1 | 32=21 | 252=1 | 42=31 | 253=1 | 52=41 | 53=42 | 43=32 | 33=22 | 23=12 | " +
				"454=1 | 455=5 | 456=6 | " + "232=2 | 233=7 | 234=8 | 233=9 | 234=9 | " + "518=1 | 519=10 | 520=11 | " +
				"10=124 | ";
			ClassicAssert.AreEqual(expectedMessage, Msg.ToPrintableString());
		}

		[Test]
		public virtual void ThirdNestedLevel2()
		{
			var group = Msg.AddRepeatingGroupAtIndex(9, 250, false);
			var entry = group.AddEntry();
			entry.AddTag(22, 11);
			var firstSubGroup = entry.AddRepeatingGroup(251);
			entry.AddTag(23, 12);

			var firstSubEntry = firstSubGroup.AddEntry();
			firstSubEntry.AddTag(32, 21);
			var secondSubGroup = firstSubEntry.AddRepeatingGroup(252);
			firstSubEntry.AddTag(33, 22);
			firstSubEntry.AddTag(34, 23);
			firstSubEntry.AddTag(35, 24);
			firstSubEntry.AddTag(36, 25);

			var secondSubEntry = secondSubGroup.AddEntry();
			secondSubEntry.AddTag(42, 31);
			var thirdSubGroup = secondSubEntry.AddRepeatingGroup(253);
			secondSubEntry.AddTag(43, 32);
			var thirdSubEntry = thirdSubGroup.AddEntry();
			thirdSubEntry.AddTag(52, 41);
			thirdSubEntry.AddTag(53, 42);

			firstSubEntry.AddTag(37, 26);

			ClassicAssert.AreEqual(
				"250=1 | 22=11 | " + "251=1 | 32=21 | " + "252=1 | 42=31 | " + "253=1 | 52=41 | 53=42 | " + "43=32 | " +
				"33=22 | 34=23 | 35=24 | 36=25 | 37=26 | " + "23=12 | ", group.ToPrintableString());

			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "250=1\u000122=11\u0001251=1\u000123=12\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "251=1\u000132=21\u0001252=1\u000133=22\u0001",
				Msg.GetRepeatingGroup(250).GetEntry(0).GetRepeatingGroup(251));
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "252=1\u000142=31\u0001253=1\u000143=32\u0001",
				Msg.GetRepeatingGroup(250).GetEntry(0).GetRepeatingGroup(251).GetEntry(0).GetRepeatingGroup(252));
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "253=1\u000152=41\u000153=42\u0001",
				Msg.GetRepeatingGroup(250).GetEntry(0).GetRepeatingGroup(251).GetEntry(0).GetRepeatingGroup(252)
					.GetEntry(0).GetRepeatingGroup(253));
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "454=1\u0001455=5\u0001456=6\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg,
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(Msg, "518=1\u0001519=10\u0001520=11\u0001", null);

			var expectedMessage =
				"8=FIX.4.3 | 9=94 | 35=8 | 49=target | 56=sender | 115=onBehalf | 34=1 | 50=senderSub | 52=20080212-04:15:18.308 | " +
				"250=1 | 22=11 | 251=1 | 32=21 | 252=1 | 42=31 | 253=1 | 52=41 | 53=42 | 43=32 | 33=22 | 34=23 | 35=24 | 36=25 | 37=26 | 23=12 | " +
				"454=1 | 455=5 | 456=6 | " + "232=2 | 233=7 | 234=8 | 233=9 | 234=9 | " + "518=1 | 519=10 | 520=11 | " +
				"10=124 | ";
			ClassicAssert.AreEqual(expectedMessage, Msg.ToPrintableString());
		}

		[Test]
		public virtual void UpdateEntry()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u0001" +
				"34=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" + "454=1\u0001455=5\u0001456=6\u0001" +
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001" + "518=1\u0001519=10\u0001520=11\u0001" +
				"555=1\u0001600=12\u0001564=13\u000110=124\u0001";
			var msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());

			var group = RepeatingGroupPool.RepeatingGroup;
			msg.GetRepeatingGroup(555, group);
			ClassicAssert.AreEqual("555=1 | 600=12 | 564=13 | ", group.ToPrintableString());

			var entry = group.GetEntry(0);
			entry.AddTag(565, 111);
			entry.UpdateValue(600, 112, IndexedStorage.MissingTagHandling.DontAddIfNotExists);
			entry.RemoveTag(564);
			ClassicAssert.AreEqual("555=1 | 600=112 | 565=111 | ", group.ToPrintableString());
			group.Release();


			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg, "454=1\u0001455=5\u0001456=6\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg,
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001", null);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg, "555=1\u0001600=112\u0001565=111\u0001", null);

			var fixMsgWithNewGroup =
				"8=FIX.4.3 | 9=94 | 35=8 | 49=target | 56=sender | 115=onBehalf | 34=1 | 50=senderSub | 52=20080212-04:15:18.308 | " +
				"454=1 | 455=5 | 456=6 | " + "232=2 | 233=7 | 234=8 | 233=9 | 234=9 | " + "518=1 | 519=10 | 520=11 | " +
				"555=1 | 600=112 | 565=111 | " + "10=124 | ";
			ClassicAssert.AreEqual(fixMsgWithNewGroup, msg.ToPrintableString());
		}
	}
}
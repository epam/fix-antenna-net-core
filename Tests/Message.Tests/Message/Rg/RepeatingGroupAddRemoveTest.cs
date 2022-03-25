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

using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Message.Rg;
using NUnit.Framework;

namespace Epam.FixAntenna.Message.Tests.Rg
{
	[TestFixture]
	internal class RepeatingGroupAddRemoveTest
	{
		private void RemoveGroup(FixMessage msg, RepeatingGroup rootGroup)
		{
			msg.RemoveRepeatingGroup(555);
		}

		[Test]
		public virtual void TestAddRemoveFromMessageFull()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			var rootGroup = msg.AddRepeatingGroup(555);

			var rootEntry1 = rootGroup.AddEntry();
			rootEntry1.AddTag(600, "ZNZ5");
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 555=1 | 600=ZNZ5 | ", msg.ToPrintableString());

			rootGroup.RemoveEntry(0);
			RemoveGroup(msg, rootGroup);
			Assert.IsFalse(msg.IsRepeatingGroupExists(555));
			Assert.IsNull(msg.GetRepeatingGroup(555));
			Assert.IsFalse(msg.IsTagExists(555));

			Assert.AreEqual("8=FIX.4.4 | 35=8 | ", msg.ToPrintableString());

			rootGroup = msg.AddRepeatingGroup(555);
			Assert.IsTrue(msg.IsRepeatingGroupExists(555));
			Assert.IsNotNull(msg.GetRepeatingGroup(555));
			Assert.IsFalse(msg.IsTagExists(555));
		}

		[Test]
		public virtual void TestAddRemoveFromMessageShort()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			var rootGroup = msg.AddRepeatingGroup(555);

			var rootEntry1 = rootGroup.AddEntry();
			rootEntry1.AddTag(600, "ZNZ5");
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 555=1 | 600=ZNZ5 | ", msg.ToPrintableString());

			RemoveGroup(msg, rootGroup);
			Assert.IsFalse(msg.IsRepeatingGroupExists(555));
			Assert.IsNull(msg.GetRepeatingGroup(555));
			Assert.IsFalse(msg.IsTagExists(555));

			Assert.AreEqual("8=FIX.4.4 | 35=8 | ", msg.ToPrintableString());

			rootGroup = msg.AddRepeatingGroup(555);
			Assert.IsTrue(msg.IsRepeatingGroupExists(555));
			Assert.IsNotNull(msg.GetRepeatingGroup(555));
			Assert.IsFalse(msg.IsTagExists(555));
		}

		[Test]
		public virtual void TestAddRemoveNestedRg()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			var parentGroup = msg.AddRepeatingGroup(111);
			var entry = parentGroup.AddEntry();
			entry.AddTag(222, 222);

			var rootGroup = entry.AddRepeatingGroup(555);

			var rootEntry1 = rootGroup.AddEntry();
			rootEntry1.AddTag(600, "ZNZ5");
			var nestedGroup = rootEntry1.AddRepeatingGroup(604);

			var nestedEntry1 = nestedGroup.AddEntry();
			nestedEntry1.UpdateValue(605, "TYZ5", IndexedStorage.MissingTagHandling.AddIfNotExists);
			nestedEntry1.UpdateValue(606, "5", IndexedStorage.MissingTagHandling.AddIfNotExists);

			var nestedEntry2 = nestedGroup.AddEntry();
			nestedEntry2.UpdateValue(605, "TYZ5 Comdty", IndexedStorage.MissingTagHandling.AddIfNotExists);
			nestedEntry2.UpdateValue(606, "A", IndexedStorage.MissingTagHandling.AddIfNotExists);

			Assert.AreEqual(
				"8=FIX.4.4 | 35=8 | 111=1 | 222=222 | 555=1 | 600=ZNZ5 | 604=2 | 605=TYZ5 | 606=5 | 605=TYZ5 Comdty | 606=A | ",
				msg.ToPrintableString());

			nestedEntry1.Remove();
			nestedEntry2.Remove();

			rootEntry1.RemoveRepeatingGroup(604);

			Assert.IsFalse(rootEntry1.IsRepeatingGroupExists(604));
			Assert.IsNull(rootEntry1.GetRepeatingGroup(604));
			Assert.IsFalse(msg.IsTagExists(604));

			Assert.AreEqual("8=FIX.4.4 | 35=8 | 111=1 | 222=222 | 555=1 | 600=ZNZ5 | ", msg.ToPrintableString());

			nestedGroup = rootEntry1.AddRepeatingGroup(604);
			Assert.IsTrue(rootEntry1.IsRepeatingGroupExists(604));
			Assert.IsNotNull(rootEntry1.GetRepeatingGroup(604));
			Assert.IsFalse(msg.IsTagExists(604));
		}

		[Test]
		public virtual void TestAddRemoveNestedRgFull()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			var parentGroup = msg.AddRepeatingGroup(111);
			var entry = parentGroup.AddEntry();
			entry.AddTag(222, 222);

			var rootGroup = entry.AddRepeatingGroup(555);

			var rootEntry1 = rootGroup.AddEntry();
			rootEntry1.AddTag(600, "ZNZ5");
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 111=1 | 222=222 | 555=1 | 600=ZNZ5 | ", msg.ToPrintableString());

			rootGroup.RemoveEntry(0);
			RemoveGroup(msg, rootGroup);
			Assert.IsFalse(msg.IsRepeatingGroupExists(555));
			Assert.IsNull(msg.GetRepeatingGroup(555));
			Assert.IsFalse(msg.IsTagExists(555));

			Assert.AreEqual("8=FIX.4.4 | 35=8 | 111=1 | 222=222 | ", msg.ToPrintableString());

			rootGroup = msg.AddRepeatingGroup(555);
			Assert.IsTrue(msg.IsRepeatingGroupExists(555));
			Assert.IsNotNull(msg.GetRepeatingGroup(555));
			Assert.IsFalse(msg.IsTagExists(555));
		}

		[Test]
		public virtual void TestAddRemoveNestedRgShort()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			var parentGroup = msg.AddRepeatingGroup(111);
			var entry = parentGroup.AddEntry();
			entry.AddTag(222, 222);

			var rootGroup = entry.AddRepeatingGroup(555);

			var rootEntry1 = rootGroup.AddEntry();
			rootEntry1.AddTag(600, "ZNZ5");
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 111=1 | 222=222 | 555=1 | 600=ZNZ5 | ", msg.ToPrintableString());

			RemoveGroup(msg, rootGroup);
			Assert.IsFalse(msg.IsRepeatingGroupExists(555));
			Assert.IsNull(msg.GetRepeatingGroup(555));
			Assert.IsFalse(msg.IsTagExists(555));

			Assert.AreEqual("8=FIX.4.4 | 35=8 | 111=1 | 222=222 | ", msg.ToPrintableString());

			rootGroup = msg.AddRepeatingGroup(555);
			Assert.IsTrue(msg.IsRepeatingGroupExists(555));
			Assert.IsNotNull(msg.GetRepeatingGroup(555));
			Assert.IsFalse(msg.IsTagExists(555));
		}

		[Test]
		public virtual void TestAddRemoveNestedRgWithNestedFull()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			var parentGroup = msg.AddRepeatingGroup(111);
			var entry = parentGroup.AddEntry();
			entry.AddTag(222, 222);

			var rootGroup = entry.AddRepeatingGroup(555);

			var rootEntry1 = rootGroup.AddEntry();
			rootEntry1.AddTag(600, "ZNZ5");
			var nestedGroup = rootEntry1.AddRepeatingGroup(604);

			var nestedEntry1 = nestedGroup.AddEntry();
			nestedEntry1.UpdateValue(605, "TYZ5", IndexedStorage.MissingTagHandling.AddIfNotExists);
			nestedEntry1.UpdateValue(606, "5", IndexedStorage.MissingTagHandling.AddIfNotExists);

			var nestedEntry2 = nestedGroup.AddEntry();
			nestedEntry2.UpdateValue(605, "TYZ5 Comdty", IndexedStorage.MissingTagHandling.AddIfNotExists);
			nestedEntry2.UpdateValue(606, "A", IndexedStorage.MissingTagHandling.AddIfNotExists);

			Assert.AreEqual(
				"8=FIX.4.4 | 35=8 | 111=1 | 222=222 | 555=1 | 600=ZNZ5 | 604=2 | 605=TYZ5 | 606=5 | 605=TYZ5 Comdty | 606=A | ",
				msg.ToPrintableString());
			rootGroup.RemoveEntry(0);
			RemoveGroup(msg, rootGroup);
			Assert.IsFalse(msg.IsRepeatingGroupExists(555));
			Assert.IsNull(msg.GetRepeatingGroup(555));
			Assert.IsFalse(msg.IsTagExists(555));

			Assert.AreEqual("8=FIX.4.4 | 35=8 | 111=1 | 222=222 | ", msg.ToPrintableString());

			rootGroup = msg.AddRepeatingGroup(555);
			Assert.IsTrue(msg.IsRepeatingGroupExists(555));
			Assert.IsNotNull(msg.GetRepeatingGroup(555));
			Assert.IsFalse(msg.IsTagExists(555));
		}

		[Test]
		public virtual void TestAddRemoveNestedRgWithNestedShort()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			var parentGroup = msg.AddRepeatingGroup(111);
			var entry = parentGroup.AddEntry();
			entry.AddTag(222, 222);

			var rootGroup = entry.AddRepeatingGroup(555);

			var rootEntry1 = rootGroup.AddEntry();
			rootEntry1.AddTag(600, "ZNZ5");
			var nestedGroup = rootEntry1.AddRepeatingGroup(604);

			var nestedEntry1 = nestedGroup.AddEntry();
			nestedEntry1.UpdateValue(605, "TYZ5", IndexedStorage.MissingTagHandling.AddIfNotExists);
			nestedEntry1.UpdateValue(606, "5", IndexedStorage.MissingTagHandling.AddIfNotExists);

			var nestedEntry2 = nestedGroup.AddEntry();
			nestedEntry2.UpdateValue(605, "TYZ5 Comdty", IndexedStorage.MissingTagHandling.AddIfNotExists);
			nestedEntry2.UpdateValue(606, "A", IndexedStorage.MissingTagHandling.AddIfNotExists);

			Assert.AreEqual(
				"8=FIX.4.4 | 35=8 | 111=1 | 222=222 | 555=1 | 600=ZNZ5 | 604=2 | 605=TYZ5 | 606=5 | 605=TYZ5 Comdty | 606=A | ",
				msg.ToPrintableString());

			RemoveGroup(msg, rootGroup);
			Assert.IsFalse(msg.IsRepeatingGroupExists(555));
			Assert.IsNull(msg.GetRepeatingGroup(555));
			Assert.IsFalse(msg.IsTagExists(555));

			Assert.AreEqual("8=FIX.4.4 | 35=8 | 111=1 | 222=222 | ", msg.ToPrintableString());

			rootGroup = msg.AddRepeatingGroup(555);
			Assert.IsTrue(msg.IsRepeatingGroupExists(555));
			Assert.IsNotNull(msg.GetRepeatingGroup(555));
			Assert.IsFalse(msg.IsTagExists(555));
		}

		[Test]
		public virtual void TestAddRemoveWithNestedFull()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			var rootGroup = msg.AddRepeatingGroup(555);

			var rootEntry1 = rootGroup.AddEntry();
			rootEntry1.AddTag(600, "ZNZ5");
			var nestedGroup = rootEntry1.AddRepeatingGroup(604);

			var nestedEntry1 = nestedGroup.AddEntry();
			nestedEntry1.UpdateValue(605, "TYZ5", IndexedStorage.MissingTagHandling.AddIfNotExists);
			nestedEntry1.UpdateValue(606, "5", IndexedStorage.MissingTagHandling.AddIfNotExists);

			var nestedEntry2 = nestedGroup.AddEntry();
			nestedEntry2.UpdateValue(605, "TYZ5 Comdty", IndexedStorage.MissingTagHandling.AddIfNotExists);
			nestedEntry2.UpdateValue(606, "A", IndexedStorage.MissingTagHandling.AddIfNotExists);

			Assert.AreEqual(
				"8=FIX.4.4 | 35=8 | 555=1 | 600=ZNZ5 | 604=2 | 605=TYZ5 | 606=5 | 605=TYZ5 Comdty | 606=A | ",
				msg.ToPrintableString());

			rootGroup.RemoveEntry(0);
			RemoveGroup(msg, rootGroup);
			Assert.IsFalse(msg.IsRepeatingGroupExists(555));
			Assert.IsNull(msg.GetRepeatingGroup(555));
			Assert.IsFalse(msg.IsTagExists(555));

			Assert.AreEqual("8=FIX.4.4 | 35=8 | ", msg.ToPrintableString());

			rootGroup = msg.AddRepeatingGroup(555);
			Assert.IsTrue(msg.IsRepeatingGroupExists(555));
			Assert.IsNotNull(msg.GetRepeatingGroup(555));
			Assert.IsFalse(msg.IsTagExists(555));
		}

		[Test]
		public virtual void TestAddRemoveWithNestedShort()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			var rootGroup = msg.AddRepeatingGroup(555);

			var rootEntry1 = rootGroup.AddEntry();
			rootEntry1.AddTag(600, "ZNZ5");
			var nestedGroup = rootEntry1.AddRepeatingGroup(604);

			var nestedEntry1 = nestedGroup.AddEntry();
			nestedEntry1.UpdateValue(605, "TYZ5", IndexedStorage.MissingTagHandling.AddIfNotExists);
			nestedEntry1.UpdateValue(606, "5", IndexedStorage.MissingTagHandling.AddIfNotExists);

			var nestedEntry2 = nestedGroup.AddEntry();
			nestedEntry2.UpdateValue(605, "TYZ5 Comdty", IndexedStorage.MissingTagHandling.AddIfNotExists);
			nestedEntry2.UpdateValue(606, "A", IndexedStorage.MissingTagHandling.AddIfNotExists);

			Assert.AreEqual(
				"8=FIX.4.4 | 35=8 | 555=1 | 600=ZNZ5 | 604=2 | 605=TYZ5 | 606=5 | 605=TYZ5 Comdty | 606=A | ",
				msg.ToPrintableString());

			RemoveGroup(msg, rootGroup);
			Assert.IsFalse(msg.IsRepeatingGroupExists(555));
			Assert.IsNull(msg.GetRepeatingGroup(555));
			Assert.IsFalse(msg.IsTagExists(555));

			Assert.AreEqual("8=FIX.4.4 | 35=8 | ", msg.ToPrintableString());

			rootGroup = msg.AddRepeatingGroup(555);
			Assert.IsTrue(msg.IsRepeatingGroupExists(555));
			Assert.IsNotNull(msg.GetRepeatingGroup(555));
			Assert.IsFalse(msg.IsTagExists(555));
		}

		[Test]
		public virtual void testNestedRepeatingGroupManipulation_2()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			//Add Party group
			var partyGroup = msg.AddRepeatingGroup(453);
			var party1 = partyGroup.AddEntry();
			party1.UpdateValue(448, "RBS", IndexedStorage.MissingTagHandling.AddIfNotExists);
			party1.UpdateValue(447, "D", IndexedStorage.MissingTagHandling.AddIfNotExists);
			party1.UpdateValue(452, "1", IndexedStorage.MissingTagHandling.AddIfNotExists);


			//Remove party1
			party1.Remove();
			Assert.IsFalse(msg.IsTagExists(453), "Msg should not have party group(453)");

			var randomTag = msg.GetTagValueAsString(4711); //passes, returns null as expected
			Assert.IsNull(randomTag, "Non existent tag should return null");
			var noParty = msg.GetTagValueAsString(453); //passes, returns null as expected
			Assert.IsNull(noParty, "Removed party group tag(453) should return null");


			var legGroup = msg.AddRepeatingGroup(555);

			var leg1 = legGroup.AddEntry();
			leg1.UpdateValue(600, "ZNZ5", IndexedStorage.MissingTagHandling.AddIfNotExists);
			var legSecAltIdGroup = leg1.AddRepeatingGroup(604);

			var secAltId1 = legSecAltIdGroup.AddEntry();
			secAltId1.UpdateValue(605, "TYZ5", IndexedStorage.MissingTagHandling.AddIfNotExists);
			secAltId1.UpdateValue(606, "5", IndexedStorage.MissingTagHandling.AddIfNotExists);

			var secAltId2 = legSecAltIdGroup.AddEntry();
			secAltId2.UpdateValue(605, "TYZ5 Comdty", IndexedStorage.MissingTagHandling.AddIfNotExists);
			secAltId2.UpdateValue(606, "A", IndexedStorage.MissingTagHandling.AddIfNotExists);


			var entry = msg.GetRepeatingGroup(555).GetEntry(0);
			Assert.AreEqual(entry.GetTagValueAsString(600), "ZNZ5", "Incorrect leg1 symbol");
			var secEntry = entry.GetRepeatingGroup(604).GetEntry(0);
			Assert.AreEqual(secEntry.GetTagValueAsString(605), "TYZ5", "Incorrect leg1 secalt1 symbol");
			secEntry = entry.GetRepeatingGroup(604).GetEntry(1);
			Assert.AreEqual("TYZ5 Comdty", secEntry.GetTagValueAsString(605), "Incorrect leg1 secalt2 symbol");

			//remove both secAltAID
			secAltId1.Remove();
			Assert.IsTrue(leg1.IsTagExists(604), "leg should still have secalt2 group(604)");
			secAltId2.Remove();
			Assert.IsFalse(leg1.IsTagExists(604), "leg should not have any secalt groups(604)");


			randomTag = leg1.GetTagValueAsString(4711); //passes, returns null as expected
			var noSecAltId = leg1.GetTagValueAsString(604); //passes, returns null as expected
			Assert.IsNull(randomTag, "Non existent tag should return null");
			Assert.IsNull(noSecAltId, "Removed sec alt group tag(604) should return null");
			Assert.AreEqual(0, legSecAltIdGroup.Count, "Incorrect #nested entries");

			secAltId1 = legSecAltIdGroup.AddEntry();
			secAltId1.UpdateValue(605, "TYZ5", IndexedStorage.MissingTagHandling.AddIfNotExists);
			secAltId1.UpdateValue(606, "5", IndexedStorage.MissingTagHandling.AddIfNotExists);

			secAltId2 = legSecAltIdGroup.AddEntry();
			secAltId2.UpdateValue(605, "TYZ5 Comdty", IndexedStorage.MissingTagHandling.AddIfNotExists);
			secAltId2.UpdateValue(606, "A", IndexedStorage.MissingTagHandling.AddIfNotExists);


			secAltId1.Remove();
			Assert.IsTrue(leg1.IsTagExists(604), "leg should still have secalt2 group(604)");
			secAltId2.Remove();
			Assert.IsFalse(leg1.IsTagExists(604), "leg should not have any secalt groups(604)");


			randomTag = leg1.GetTagValueAsString(4711); //passes, returns null as expected
			noSecAltId = leg1.GetTagValueAsString(604); //passes, returns null as expected
			Assert.IsNull(randomTag, "Non existent tag should return null");
			Assert.IsNull(noSecAltId, "Removed sec alt group tag(604) should return null");
			Assert.AreEqual(0, legSecAltIdGroup.Count, "Incorrect #nested entries");

			//legSecAltIDGroup.remove(); // NEEDED
			leg1.RemoveRepeatingGroup(604);
			legSecAltIdGroup = leg1.AddRepeatingGroup(604);
			//legSecAltIDGroup.remove(); // NEEDED
			leg1.RemoveRepeatingGroup(604);
			leg1.AddRepeatingGroup(604);

			leg1.Remove(); // NEEDED
			Assert.AreEqual(0, legGroup.Count, "Incorrect #nested entries");

			leg1 = legGroup.AddEntry();
			//now re-add the nested group
			legSecAltIdGroup = leg1.AddRepeatingGroup(604);

			secAltId1 = legSecAltIdGroup.AddEntry();
			secAltId1.UpdateValue(605, "TYZ5", IndexedStorage.MissingTagHandling.AddIfNotExists);
			secAltId1.UpdateValue(606, "5", IndexedStorage.MissingTagHandling.AddIfNotExists);

			secAltId2 = legSecAltIdGroup.AddEntry();
			secAltId2.UpdateValue(605, "TYZ5 Comdty", IndexedStorage.MissingTagHandling.AddIfNotExists);
			secAltId2.UpdateValue(606, "A", IndexedStorage.MissingTagHandling.AddIfNotExists);


			var noSecAltIdLong = leg1.GetTagValueAsLong(604);
			Assert.AreEqual(2L, noSecAltIdLong, "Incorrect noSecAlt value");
		}

		[Test]
		public virtual void TestRemoveAndCleanEntry()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			var parentGroup = msg.AddRepeatingGroup(111);
			var entry = parentGroup.AddEntry();
			entry.AddTag(222, 222);

			var rootGroup = entry.AddRepeatingGroup(555);

			var rootEntry1 = rootGroup.AddEntry();
			rootEntry1.AddTag(600, "ZNZ5");
			var nestedGroup = rootEntry1.AddRepeatingGroup(604);

			var nestedEntry1 = nestedGroup.AddEntry();
			nestedEntry1.UpdateValue(605, "TYZ5", IndexedStorage.MissingTagHandling.AddIfNotExists);
			nestedEntry1.UpdateValue(606, "5", IndexedStorage.MissingTagHandling.AddIfNotExists);

			var nestedEntry2 = nestedGroup.AddEntry();
			nestedEntry2.UpdateValue(605, "TYZ5 Comdty", IndexedStorage.MissingTagHandling.AddIfNotExists);
			nestedEntry2.UpdateValue(606, "A", IndexedStorage.MissingTagHandling.AddIfNotExists);

			Assert.AreEqual(
				"8=FIX.4.4 | 35=8 | 111=1 | 222=222 | 555=1 | 600=ZNZ5 | 604=2 | 605=TYZ5 | 606=5 | 605=TYZ5 Comdty | 606=A | ",
				msg.ToPrintableString());

			nestedEntry1.RemoveAndClean();
			nestedEntry2.RemoveAndClean();

			Assert.IsFalse(rootEntry1.IsRepeatingGroupExists(604));
			Assert.IsNull(rootEntry1.GetRepeatingGroup(604));
			Assert.IsFalse(msg.IsTagExists(604));

			Assert.AreEqual("8=FIX.4.4 | 35=8 | 111=1 | 222=222 | 555=1 | 600=ZNZ5 | ", msg.ToPrintableString());

			nestedGroup = rootEntry1.AddRepeatingGroup(604);
			Assert.IsTrue(rootEntry1.IsRepeatingGroupExists(604));
			Assert.IsNotNull(rootEntry1.GetRepeatingGroup(604));
			Assert.IsFalse(msg.IsTagExists(604));
		}

		[Test]
		public virtual void TestRemoveAndCleanEntryByIndex()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			var parentGroup = msg.AddRepeatingGroup(111);
			var entry = parentGroup.AddEntry();
			entry.AddTag(222, 222);

			var rootGroup = entry.AddRepeatingGroup(555);

			var rootEntry1 = rootGroup.AddEntry();
			rootEntry1.AddTag(600, "ZNZ5");
			var nestedGroup = rootEntry1.AddRepeatingGroup(604);

			var nestedEntry1 = nestedGroup.AddEntry();
			nestedEntry1.UpdateValue(605, "TYZ5", IndexedStorage.MissingTagHandling.AddIfNotExists);
			nestedEntry1.UpdateValue(606, "5", IndexedStorage.MissingTagHandling.AddIfNotExists);

			var nestedEntry2 = nestedGroup.AddEntry();
			nestedEntry2.UpdateValue(605, "TYZ5 Comdty", IndexedStorage.MissingTagHandling.AddIfNotExists);
			nestedEntry2.UpdateValue(606, "A", IndexedStorage.MissingTagHandling.AddIfNotExists);

			Assert.AreEqual(
				"8=FIX.4.4 | 35=8 | 111=1 | 222=222 | 555=1 | 600=ZNZ5 | 604=2 | 605=TYZ5 | 606=5 | 605=TYZ5 Comdty | 606=A | ",
				msg.ToPrintableString());

			nestedGroup.RemoveEntryAndClean(0);
			nestedGroup.RemoveEntryAndClean(0);

			Assert.IsFalse(rootEntry1.IsRepeatingGroupExists(604));
			Assert.IsNull(rootEntry1.GetRepeatingGroup(604));
			Assert.IsFalse(msg.IsTagExists(604));

			Assert.AreEqual("8=FIX.4.4 | 35=8 | 111=1 | 222=222 | 555=1 | 600=ZNZ5 | ", msg.ToPrintableString());

			nestedGroup = rootEntry1.AddRepeatingGroup(604);
			Assert.IsTrue(rootEntry1.IsRepeatingGroupExists(604));
			Assert.IsNotNull(rootEntry1.GetRepeatingGroup(604));
			Assert.IsFalse(msg.IsTagExists(604));
		}

		[Test]
		public virtual void TestRemoveAndCleanEntryByObj()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			var parentGroup = msg.AddRepeatingGroup(111);
			var entry = parentGroup.AddEntry();
			entry.AddTag(222, 222);

			var rootGroup = entry.AddRepeatingGroup(555);

			var rootEntry1 = rootGroup.AddEntry();
			rootEntry1.AddTag(600, "ZNZ5");
			var nestedGroup = rootEntry1.AddRepeatingGroup(604);

			var nestedEntry1 = nestedGroup.AddEntry();
			nestedEntry1.UpdateValue(605, "TYZ5", IndexedStorage.MissingTagHandling.AddIfNotExists);
			nestedEntry1.UpdateValue(606, "5", IndexedStorage.MissingTagHandling.AddIfNotExists);

			var nestedEntry2 = nestedGroup.AddEntry();
			nestedEntry2.UpdateValue(605, "TYZ5 Comdty", IndexedStorage.MissingTagHandling.AddIfNotExists);
			nestedEntry2.UpdateValue(606, "A", IndexedStorage.MissingTagHandling.AddIfNotExists);

			Assert.AreEqual(
				"8=FIX.4.4 | 35=8 | 111=1 | 222=222 | 555=1 | 600=ZNZ5 | 604=2 | 605=TYZ5 | 606=5 | 605=TYZ5 Comdty | 606=A | ",
				msg.ToPrintableString());

			nestedGroup.RemoveEntryAndClean(nestedEntry1);
			nestedGroup.RemoveEntryAndClean(nestedEntry2);

			Assert.IsFalse(rootEntry1.IsRepeatingGroupExists(604));
			Assert.IsNull(rootEntry1.GetRepeatingGroup(604));
			Assert.IsFalse(msg.IsTagExists(604));

			Assert.AreEqual("8=FIX.4.4 | 35=8 | 111=1 | 222=222 | 555=1 | 600=ZNZ5 | ", msg.ToPrintableString());

			nestedGroup = rootEntry1.AddRepeatingGroup(604);
			Assert.IsTrue(rootEntry1.IsRepeatingGroupExists(604));
			Assert.IsNotNull(rootEntry1.GetRepeatingGroup(604));
			Assert.IsFalse(msg.IsTagExists(604));
		}
	}
}
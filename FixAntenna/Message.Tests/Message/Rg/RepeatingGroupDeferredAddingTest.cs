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
	internal class RepeatingGroupDeferredAddingTest
	{
		private RepeatingGroup _group456;
		private RepeatingGroup _group123;
		private RepeatingGroup _group789;

		private void FillGroup123()
		{
			var entry1231 = _group123.AddEntry();
			var entry1232 = _group123.AddEntry();

			entry1231.AddTag(2222, 3);
			entry1232.AddTag(2222, 4);
		}

		private void FillGroup456()
		{
			var entry4561 = _group456.AddEntry();
			var entry4562 = _group456.AddEntry();

			entry4561.AddTag(1111, 1);
			entry4562.AddTag(1111, 2);
		}

		private void FillGroup789()
		{
			var entry7891 = _group789.AddEntry();
			var entry7892 = _group789.AddEntry();

			entry7891.AddTag(3333, 1);
			entry7892.AddTag(3333, 2);
		}

		[Test]
		public virtual void AddEntryWhenPrevEntryEmpty()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			_group123 = msg.AddRepeatingGroup(123);
			FillGroup123();

			var entry = _group123.GetEntry(0);
			_group456 = _group123.AddEntry().AddRepeatingGroup(456);
			_group123.AddEntry().AddTag(2222, 6);
			_group789 = _group123.AddEntry().AddRepeatingGroup(789);

			FillGroup789();
			FillGroup456();
			Assert.AreEqual(
				"8=FIX.4.4 | 35=8 | 123=5 | 2222=3 | 2222=4 | 456=2 | 1111=1 | 1111=2 | 2222=6 | 789=2 | 3333=1 | 3333=2 | ",
				msg.ToPrintableString());
		}

		[Test]
		public virtual void AddFewNestedGroupsInSamePlace1()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			_group123 = msg.AddRepeatingGroup(123);
			FillGroup123();

			var entry = _group123.GetEntry(0);
			_group456 = entry.AddRepeatingGroup(456);
			_group789 = entry.AddRepeatingGroup(789);

			FillGroup789();
			FillGroup456();
			Assert.AreEqual(
				"8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 456=2 | 1111=1 | 1111=2 | 789=2 | 3333=1 | 3333=2 | 2222=4 | ",
				msg.ToPrintableString());

			_group456.Remove();
			_group789.Remove();

			_group456 = entry.AddRepeatingGroup(456);
			_group789 = entry.AddRepeatingGroup(789);

			FillGroup456();
			FillGroup789();
			Assert.AreEqual(
				"8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 456=2 | 1111=1 | 1111=2 | 789=2 | 3333=1 | 3333=2 | 2222=4 | ",
				msg.ToPrintableString());
		}

		[Test]
		public virtual void AddFewNestedGroupsInSamePlace2()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			_group123 = msg.AddRepeatingGroup(123);
			FillGroup123();
			_group456 = _group123.AddEntry().AddRepeatingGroup(456);
			_group789 = _group123.AddEntry().AddRepeatingGroup(789);

			FillGroup789();
			FillGroup456();
			Assert.AreEqual(
				"8=FIX.4.4 | 35=8 | 123=4 | 2222=3 | 2222=4 | 456=2 | 1111=1 | 1111=2 | 789=2 | 3333=1 | 3333=2 | ",
				msg.ToPrintableString());

			_group456.Remove();
			_group789.Remove();

			_group456 = _group123.AddEntry().AddRepeatingGroup(456);
			_group789 = _group123.AddEntry().AddRepeatingGroup(789);
			FillGroup456();
			FillGroup789();
			Assert.AreEqual(
				"8=FIX.4.4 | 35=8 | 123=4 | 2222=3 | 2222=4 | 456=2 | 1111=1 | 1111=2 | 789=2 | 3333=1 | 3333=2 | ",
				msg.ToPrintableString());
		}

		[Test]
		public virtual void AddThreeGroups()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			_group123 = msg.AddRepeatingGroup(123); //1
			_group456 = msg.AddRepeatingGroup(456); //2
			_group789 = msg.AddRepeatingGroup(789); //3

			FillGroup123();
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 2222=4 | ", msg.ToPrintableString());
			Assert.AreEqual("123=2 | 2222=3 | 2222=4 | ", _group123.ToPrintableString());
			Assert.AreEqual("", _group456.ToPrintableString());
			Assert.AreEqual("", _group789.ToPrintableString());

			FillGroup456();
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 2222=4 | 456=2 | 1111=1 | 1111=2 | ",
				msg.ToPrintableString());
			Assert.AreEqual("123=2 | 2222=3 | 2222=4 | ", _group123.ToPrintableString());
			Assert.AreEqual("456=2 | 1111=1 | 1111=2 | ", _group456.ToPrintableString());
			Assert.AreEqual("", _group789.ToPrintableString());

			FillGroup789();
			Assert.AreEqual(
				"8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 2222=4 | 456=2 | 1111=1 | 1111=2 | 789=2 | 3333=1 | 3333=2 | ",
				msg.ToPrintableString());
			Assert.AreEqual("123=2 | 2222=3 | 2222=4 | ", _group123.ToPrintableString());
			Assert.AreEqual("456=2 | 1111=1 | 1111=2 | ", _group456.ToPrintableString());
			Assert.AreEqual("789=2 | 3333=1 | 3333=2 | ", _group789.ToPrintableString());

			_group123.Remove();
			_group456.Remove();
			_group789.Remove();

			_group123 = msg.AddRepeatingGroup(123); //3
			_group456 = msg.AddRepeatingGroup(456); //2
			_group789 = msg.AddRepeatingGroup(789); //1

			FillGroup789();
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 789=2 | 3333=1 | 3333=2 | ", msg.ToPrintableString());
			Assert.AreEqual("", _group123.ToPrintableString());
			Assert.AreEqual("", _group456.ToPrintableString());
			Assert.AreEqual("789=2 | 3333=1 | 3333=2 | ", _group789.ToPrintableString());
			FillGroup456();
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 456=2 | 1111=1 | 1111=2 | 789=2 | 3333=1 | 3333=2 | ",
				msg.ToPrintableString());
			Assert.AreEqual("", _group123.ToPrintableString());
			Assert.AreEqual("456=2 | 1111=1 | 1111=2 | ", _group456.ToPrintableString());
			Assert.AreEqual("789=2 | 3333=1 | 3333=2 | ", _group789.ToPrintableString());
			FillGroup123();
			Assert.AreEqual(
				"8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 2222=4 | 456=2 | 1111=1 | 1111=2 | 789=2 | 3333=1 | 3333=2 | ",
				msg.ToPrintableString());
			Assert.AreEqual("123=2 | 2222=3 | 2222=4 | ", _group123.ToPrintableString());
			Assert.AreEqual("456=2 | 1111=1 | 1111=2 | ", _group456.ToPrintableString());
			Assert.AreEqual("789=2 | 3333=1 | 3333=2 | ", _group789.ToPrintableString());

			_group123.Remove();
			_group456.Remove();
			_group789.Remove();

			_group123 = msg.AddRepeatingGroup(123); //3
			_group456 = msg.AddRepeatingGroup(456); //2
			_group789 = msg.AddRepeatingGroup(789); //1

			FillGroup456();
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 456=2 | 1111=1 | 1111=2 | ", msg.ToPrintableString());
			Assert.AreEqual("", _group123.ToPrintableString());
			Assert.AreEqual("456=2 | 1111=1 | 1111=2 | ", _group456.ToPrintableString());
			Assert.AreEqual("", _group789.ToPrintableString());
			FillGroup789();
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 456=2 | 1111=1 | 1111=2 | 789=2 | 3333=1 | 3333=2 | ",
				msg.ToPrintableString());
			Assert.AreEqual("", _group123.ToPrintableString());
			Assert.AreEqual("456=2 | 1111=1 | 1111=2 | ", _group456.ToPrintableString());
			Assert.AreEqual("789=2 | 3333=1 | 3333=2 | ", _group789.ToPrintableString());
			FillGroup123();
			Assert.AreEqual(
				"8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 2222=4 | 456=2 | 1111=1 | 1111=2 | 789=2 | 3333=1 | 3333=2 | ",
				msg.ToPrintableString());
			Assert.AreEqual("123=2 | 2222=3 | 2222=4 | ", _group123.ToPrintableString());
			Assert.AreEqual("456=2 | 1111=1 | 1111=2 | ", _group456.ToPrintableString());
			Assert.AreEqual("789=2 | 3333=1 | 3333=2 | ", _group789.ToPrintableString());
		}

		[Test]
		public virtual void AddTwoGroupsAndThenTags()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			_group123 = msg.AddRepeatingGroup(123);
			_group456 = msg.AddRepeatingGroup(456);

			var entry1231 = _group123.AddEntry();
			entry1231.AddTag(111, 1);

			var entry4561 = _group456.AddEntry();
			entry4561.AddTag(444, 4);

			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=1 | 111=1 | 456=1 | 444=4 | ", msg.ToPrintableString());
		}

		[Test]
		public virtual void AddTwoGroupsWithTags()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			_group123 = msg.AddRepeatingGroup(123);
			var entry1231 = _group123.AddEntry();
			entry1231.AddTag(111, 1);


			_group456 = msg.AddRepeatingGroup(456);
			var entry4561 = _group456.AddEntry();
			entry4561.AddTag(444, 4);

			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=1 | 111=1 | 456=1 | 444=4 | ", msg.ToPrintableString());
		}

		[Test]
		public virtual void DeferredGroupAddedFirst()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			_group123 = msg.AddRepeatingGroup(123);
			_group456 = msg.AddRepeatingGroup(456);

			FillGroup456();

			Assert.AreEqual("8=FIX.4.4 | 35=8 | 456=2 | 1111=1 | 1111=2 | ", msg.ToPrintableString());
			Assert.AreEqual("456=2 | 1111=1 | 1111=2 | ", _group456.ToPrintableString());
			Assert.AreEqual("", _group123.ToPrintableString());

			FillGroup123();

			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 2222=4 | 456=2 | 1111=1 | 1111=2 | ",
				msg.ToPrintableString());
			Assert.AreEqual("123=2 | 2222=3 | 2222=4 | ", _group123.ToPrintableString());
			Assert.AreEqual("456=2 | 1111=1 | 1111=2 | ", _group456.ToPrintableString());

			_group123.Remove();
			_group456.Remove();

			Assert.AreEqual("8=FIX.4.4 | 35=8 | ", msg.ToPrintableString());

			_group123 = msg.AddRepeatingGroup(123);
			_group456 = msg.AddRepeatingGroup(456);

			FillGroup123();
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 2222=4 | ", msg.ToPrintableString());
			Assert.AreEqual("123=2 | 2222=3 | 2222=4 | ", _group123.ToPrintableString());
			Assert.AreEqual("", _group456.ToPrintableString());

			FillGroup456();
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 2222=4 | 456=2 | 1111=1 | 1111=2 | ",
				msg.ToPrintableString());
			Assert.AreEqual("456=2 | 1111=1 | 1111=2 | ", _group456.ToPrintableString());
			Assert.AreEqual("123=2 | 2222=3 | 2222=4 | ", _group123.ToPrintableString());
		}

		[Test]
		public virtual void DeferredGroupAddedSecond()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			_group456 = msg.AddRepeatingGroup(456);
			_group123 = msg.AddRepeatingGroup(123);

			FillGroup123();

			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 2222=4 | ", msg.ToPrintableString());
			Assert.AreEqual("123=2 | 2222=3 | 2222=4 | ", _group123.ToPrintableString());
			Assert.AreEqual("", _group456.ToPrintableString());

			FillGroup456();

			Assert.AreEqual("8=FIX.4.4 | 35=8 | 456=2 | 1111=1 | 1111=2 | 123=2 | 2222=3 | 2222=4 | ",
				msg.ToPrintableString());
			Assert.AreEqual("456=2 | 1111=1 | 1111=2 | ", _group456.ToPrintableString());
			Assert.AreEqual("123=2 | 2222=3 | 2222=4 | ", _group123.ToPrintableString());

			_group123.Remove();
			_group456.Remove();
			Assert.AreEqual("8=FIX.4.4 | 35=8 | ", msg.ToPrintableString());

			_group456 = msg.AddRepeatingGroup(456);
			_group123 = msg.AddRepeatingGroup(123);

			FillGroup456();
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 456=2 | 1111=1 | 1111=2 | ", msg.ToPrintableString());
			Assert.AreEqual("456=2 | 1111=1 | 1111=2 | ", _group456.ToPrintableString());
			Assert.AreEqual("", _group123.ToPrintableString());

			FillGroup123();

			Assert.AreEqual("8=FIX.4.4 | 35=8 | 456=2 | 1111=1 | 1111=2 | 123=2 | 2222=3 | 2222=4 | ",
				msg.ToPrintableString());
			Assert.AreEqual("123=2 | 2222=3 | 2222=4 | ", _group123.ToPrintableString());
			Assert.AreEqual("456=2 | 1111=1 | 1111=2 | ", _group456.ToPrintableString());
		}

		[Test]
		public virtual void NestedDeferredGroupAfterTagRemove()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			_group123 = msg.AddRepeatingGroup(123);
			var entry1231 = _group123.AddEntry();

			entry1231.AddTag(1234, 1);
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=1 | 1234=1 | ", msg.ToPrintableString());
			_group456 = entry1231.AddRepeatingGroup(456);
			entry1231.RemoveTag(1234);
			entry1231.AddTag(1235, 2);

			_group456.AddEntry().AddTag(4567, 1);
			entry1231.AddTag(1236, 3);

			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=1 | 456=1 | 4567=1 | 1235=2 | 1236=3 | ", msg.ToPrintableString());
			Assert.AreEqual("456=1 | 4567=1 | ", _group456.ToPrintableString());
			Assert.AreEqual("123=1 | 456=1 | 4567=1 | 1235=2 | 1236=3 | ", _group123.ToPrintableString());
		}

		[Test]
		public virtual void printLeadingTagOnlyWhenTagsExists()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			var group = msg.AddRepeatingGroup(123);
			var entry = group.AddEntry();

			Assert.AreEqual("8=FIX.4.4 | 35=8 | ", msg.ToPrintableString());
			Assert.AreEqual("", group.ToPrintableString());

			entry.AddTag(222, 2);

			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=1 | 222=2 | ", msg.ToPrintableString());
			Assert.AreEqual("123=1 | 222=2 | ", group.ToPrintableString());
		}

		[Test]
		public virtual void RemoveDeferredNestedGroupByIndex()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			_group123 = msg.AddRepeatingGroup(123);
			var entry = _group123.AddEntry();
			entry.AddTag(1111, 2);
			_group456 = entry.AddRepeatingGroup(456);
			entry.AddTag(1112, 3);

			_group456.AddEntry().AddTag(2222, 1);

			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=1 | 1111=2 | 456=1 | 2222=1 | 1112=3 | ", msg.ToPrintableString());
			Assert.IsTrue(entry.RemoveTagAtIndex(1));

			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=1 | 1111=2 | 1112=3 | ", msg.ToPrintableString());
		}

		[Test]
		public virtual void SaveDeferredGroupInCenterOfMsg()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");
			msg.Set(10, "123");

			_group123 = msg.AddRepeatingGroupAtIndex(2, 123); //3
			_group456 = msg.AddRepeatingGroupAtIndex(2, 456); //2
			_group789 = msg.AddRepeatingGroupAtIndex(2, 789); //1

			FillGroup123();
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 2222=4 | 10=123 | ", msg.ToPrintableString());
			Assert.AreEqual("123=2 | 2222=3 | 2222=4 | ", _group123.ToPrintableString());
			Assert.AreEqual("", _group456.ToPrintableString());
			Assert.AreEqual("", _group789.ToPrintableString());

			FillGroup456();
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 456=2 | 1111=1 | 1111=2 | 123=2 | 2222=3 | 2222=4 | 10=123 | ",
				msg.ToPrintableString());
			Assert.AreEqual("123=2 | 2222=3 | 2222=4 | ", _group123.ToPrintableString());
			Assert.AreEqual("456=2 | 1111=1 | 1111=2 | ", _group456.ToPrintableString());
			Assert.AreEqual("", _group789.ToPrintableString());

			FillGroup789();
			Assert.AreEqual(
				"8=FIX.4.4 | 35=8 | 789=2 | 3333=1 | 3333=2 | 456=2 | 1111=1 | 1111=2 | 123=2 | 2222=3 | 2222=4 | 10=123 | ",
				msg.ToPrintableString());
			Assert.AreEqual("123=2 | 2222=3 | 2222=4 | ", _group123.ToPrintableString());
			Assert.AreEqual("456=2 | 1111=1 | 1111=2 | ", _group456.ToPrintableString());
			Assert.AreEqual("789=2 | 3333=1 | 3333=2 | ", _group789.ToPrintableString());
		}

		[Test]
		public virtual void TestRemoveInPrevEntry()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			_group123 = msg.AddRepeatingGroup(123);
			FillGroup123();
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 2222=4 | ", msg.ToPrintableString());
			Assert.AreEqual("123=2 | 2222=3 | 2222=4 | ", _group123.ToPrintableString());
			_group456 = _group123.GetEntry(0).AddRepeatingGroup(456);
			FillGroup456();
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 456=2 | 1111=1 | 1111=2 | 2222=4 | ",
				msg.ToPrintableString());
			Assert.AreEqual("123=2 | 2222=3 | 456=2 | 1111=1 | 1111=2 | 2222=4 | ", _group123.ToPrintableString());
			Assert.AreEqual("456=2 | 1111=1 | 1111=2 | ", _group456.ToPrintableString());
			_group789 = _group123.GetEntry(0).AddRepeatingGroup(789);
			_group456.GetEntry(0).Remove();
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 456=1 | 1111=2 | 2222=4 | ", msg.ToPrintableString());
			Assert.AreEqual("123=2 | 2222=3 | 456=1 | 1111=2 | 2222=4 | ", _group123.ToPrintableString());
			Assert.AreEqual("456=1 | 1111=2 | ", _group456.ToPrintableString());
			FillGroup789();
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 456=1 | 1111=2 | 789=2 | 3333=1 | 3333=2 | 2222=4 | ",
				msg.ToPrintableString());
			Assert.AreEqual("123=2 | 2222=3 | 456=1 | 1111=2 | 789=2 | 3333=1 | 3333=2 | 2222=4 | ",
				_group123.ToPrintableString());
			Assert.AreEqual("456=1 | 1111=2 | ", _group456.ToPrintableString());
			Assert.AreEqual("789=2 | 3333=1 | 3333=2 | ", _group789.ToPrintableString());
		}

		[Test]
		public virtual void TestRemoveInPrevEntry1()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			_group123 = msg.AddRepeatingGroup(123);
			FillGroup123();
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 2222=4 | ", msg.ToPrintableString());
			Assert.AreEqual("123=2 | 2222=3 | 2222=4 | ", _group123.ToPrintableString());

			var entry1 = _group123.GetEntry(0);
			var entry2 = _group123.GetEntry(1);
			entry1.AddTag(2223, 5);
			_group456 = entry2.AddRepeatingGroup(456);
			entry1.RemoveTagAtIndex(1);
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 2222=4 | ", msg.ToPrintableString());
			FillGroup456();
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 2222=4 | 456=2 | 1111=1 | 1111=2 | ",
				msg.ToPrintableString());
			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg, "123=2\u00012222=3\u00012222=4\u0001456=2\u0001",
				_group123);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg, "456=2\u00011111=1\u00011111=2\u0001", _group456);
		}

		[Test]
		public virtual void TestRemoveInPrevEntry2()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			_group123 = msg.AddRepeatingGroup(123);
			FillGroup123();
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 2222=4 | ", msg.ToPrintableString());
			Assert.AreEqual("123=2 | 2222=3 | 2222=4 | ", _group123.ToPrintableString());

			var entry1 = _group123.GetEntry(0);
			var entry2 = _group123.GetEntry(1);
			entry1.AddTag(2223, 5);
			_group456 = entry2.AddRepeatingGroup(456);
			entry2.AddTag(2224, 6);
			entry1.RemoveTagAtIndex(1);
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 2222=4 | 2224=6 | ", msg.ToPrintableString());
			FillGroup456();
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 2222=4 | 456=2 | 1111=1 | 1111=2 | 2224=6 | ",
				msg.ToPrintableString());
			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg,
				"123=2\u00012222=3\u00012222=4\u0001456=2\u00012224=6\u0001", _group123);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg, "456=2\u00011111=1\u00011111=2\u0001", _group456);
		}

		[Test]
		public virtual void TestRemoveInPrevEntryThirdNestedLevel()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			_group123 = msg.AddRepeatingGroup(123);
			FillGroup123();
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 2222=4 | ", msg.ToPrintableString());
			Assert.AreEqual("123=2 | 2222=3 | 2222=4 | ", _group123.ToPrintableString());

			var entry1 = _group123.GetEntry(0);
			var entry2 = _group123.GetEntry(1);
			entry1.AddTag(2223, 5);
			_group456 = entry2.AddRepeatingGroup(456);
			_group789 = _group456.AddEntry().AddRepeatingGroup(789);
			entry1.AddTag(2224, 6);
			entry1.RemoveTagAtIndex(1);
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 2224=6 | 2222=4 | ", msg.ToPrintableString());

			FillGroup456();
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 2224=6 | 2222=4 | 456=2 | 1111=1 | 1111=2 | ",
				msg.ToPrintableString());
			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg,
				"123=2\u00012222=3\u00012224=6\u00012222=4\u0001456=2\u0001", _group123);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg, "456=2\u00011111=1\u00011111=2\u0001", _group456);

			FillGroup789();
			Assert.AreEqual(
				"8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 2224=6 | 2222=4 | 456=3 | 789=2 | 3333=1 | 3333=2 | 1111=1 | 1111=2 | ",
				msg.ToPrintableString());
			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg,
				"123=2\u00012222=3\u00012224=6\u00012222=4\u0001456=3\u0001", _group123);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg, "789=2\u00013333=1\u00013333=2\u0001", _group789);
		}

		[Test]
		public virtual void TestRemoveInSameEntry1()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			_group123 = msg.AddRepeatingGroup(123);
			FillGroup123();
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 2222=4 | ", msg.ToPrintableString());
			Assert.AreEqual("123=2 | 2222=3 | 2222=4 | ", _group123.ToPrintableString());

			var entry1 = _group123.GetEntry(0);
			entry1.AddTag(2223, 5);
			_group456 = entry1.AddRepeatingGroup(456);
			entry1.RemoveTagAtIndex(1);
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 2222=4 | ", msg.ToPrintableString());
			FillGroup456();
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 456=2 | 1111=1 | 1111=2 | 2222=4 | ",
				msg.ToPrintableString());

			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg, "123=2\u00012222=3\u0001456=2\u00012222=4\u0001",
				_group123);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg, "456=2\u00011111=1\u00011111=2\u0001", _group456);
		}

		[Test]
		public virtual void TestRemoveInSameEntry2()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			_group123 = msg.AddRepeatingGroup(123);
			FillGroup123();
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 2222=4 | ", msg.ToPrintableString());
			Assert.AreEqual("123=2 | 2222=3 | 2222=4 | ", _group123.ToPrintableString());

			var entry1 = _group123.GetEntry(0);
			_group456 = entry1.AddRepeatingGroup(456);
			entry1.AddTag(2223, 5);
			entry1.AddTag(2224, 6);
			entry1.RemoveTagAtIndex(1);
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 2224=6 | 2222=4 | ", msg.ToPrintableString());
			FillGroup456();
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 456=2 | 1111=1 | 1111=2 | 2224=6 | 2222=4 | ",
				msg.ToPrintableString());

			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg,
				"123=2\u00012222=3\u0001456=2\u00012224=6\u00012222=4\u0001", _group123);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg, "456=2\u00011111=1\u00011111=2\u0001", _group456);
		}

		[Test]
		public virtual void TestRemoveInSameEntryThirdNestedLevel()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			_group123 = msg.AddRepeatingGroup(123);
			FillGroup123();
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 2222=4 | ", msg.ToPrintableString());
			Assert.AreEqual("123=2 | 2222=3 | 2222=4 | ", _group123.ToPrintableString());

			var entry1 = _group123.GetEntry(0);
			entry1.AddTag(2223, 5);
			_group456 = entry1.AddRepeatingGroup(456);
			_group789 = _group456.AddEntry().AddRepeatingGroup(789);
			entry1.AddTag(2224, 6);
			entry1.RemoveTagAtIndex(1);
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 2224=6 | 2222=4 | ", msg.ToPrintableString());

			FillGroup456();
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 456=2 | 1111=1 | 1111=2 | 2224=6 | 2222=4 | ",
				msg.ToPrintableString());
			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg,
				"123=2\u00012222=3\u0001456=2\u00012224=6\u00012222=4\u0001", _group123);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg, "456=2\u00011111=1\u00011111=2\u0001", _group456);

			FillGroup789();
			Assert.AreEqual(
				"8=FIX.4.4 | 35=8 | 123=2 | 2222=3 | 456=3 | 789=2 | 3333=1 | 3333=2 | 1111=1 | 1111=2 | 2224=6 | 2222=4 | ",
				msg.ToPrintableString());
			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg,
				"123=2\u00012222=3\u0001456=3\u00012224=6\u00012222=4\u0001", _group123);
			RepeatingGroupTestUtil.ValidateRepeatingGroup(msg, "789=2\u00013333=1\u00013333=2\u0001", _group789);
		}

		[Test]
		public virtual void TestUpdateParentLeadingTagWhenRemovedSubEntry()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");


			_group123 = msg.AddRepeatingGroup(123);

			_group456 = _group123.AddEntry().AddRepeatingGroup(456);

			_group789 = _group456.AddEntry().AddRepeatingGroup(789);
			_group789.AddEntry().AddTag(1234, 4321);
			FillGroup123();
			FillGroup456();

			Assert.AreEqual(
				"8=FIX.4.4 | 35=8 | 123=3 | 456=3 | 789=1 | 1234=4321 | 1111=1 | 1111=2 | 2222=3 | 2222=4 | ",
				msg.ToPrintableString());

			_group789.RemoveEntry(0);
			Assert.AreEqual("8=FIX.4.4 | 35=8 | 123=3 | 456=2 | 1111=1 | 1111=2 | 2222=3 | 2222=4 | ",
				msg.ToPrintableString());
		}
	}
}
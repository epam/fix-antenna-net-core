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
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Message.Rg;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Message.Tests.Rg
{
	[TestFixture]
	internal class RepeatingGroupUatTest
	{
		private static readonly string[][] TestSampleSecurityAltIdGroup =
		{
			new[] { "605=TYZ5", "606=5" },
			new[] { "605=TYZ5 Comdty", "606=A" }
		};

		private static readonly string[][] TestSamplePartyGroup =
		{
			new[] { "448=Daniel", "452=3", "447=D" },
			new[] { "448=Mikey Mouse", "452=11", "447=D" }
		};

		private object GetGroup(FixMessage msg, int noTag)
		{
			if (msg.IsTagExists(noTag))
			{
				return msg.GetRepeatingGroup(noTag);
			}

			{
				return null;
			}
		}

		private byte[] GetMessageBytes(string msg)
		{
			return msg.Replace(" | ", "\u0001").AsByteArray();
		}

		private void CheckRepeatingGroup(RepeatingGroup.Entry entry, int noTag, string[][] sampleGroupData)
		{
			//1) check overall group structure
			ClassicAssert.AreEqual((long)sampleGroupData.Length, entry.GetTagValueAsLong(noTag),
				"Wrong value of group No tag.");

			var group = entry.GetRepeatingGroup(noTag);
			ClassicAssert.IsNotNull(group, "No group found for NoTag=" + noTag);

			ClassicAssert.AreEqual(sampleGroupData.Length, @group.Count, "Wrong number of entries in group.");
			ClassicAssert.AreEqual((long)@group.Count, entry.GetTagValueAsLong(noTag),
				"Repeating group No-tag value doesn't match number of entries in group");

			//Check group's entries against the sample data

			for (var i = 0; i < sampleGroupData.Length; i++)
			{
				var entryData = sampleGroupData[i];
				var nestedEntry = group.GetEntry(i);

				for (var e = 0; e < entryData.Length; e++)
				{
					var tagValue = entryData[e].Split("=", true);
					if (tagValue.Length == 2)
					{
						ClassicAssert.AreEqual(tagValue[1], nestedEntry.GetTagValueAsString(int.Parse(tagValue[0])),
							"Wrong tag/value pair in entry[" + i + "].");
					}
					else
					{
						throw new ArgumentException("Invalid input data: '" + entryData[e] +
													". Expected format: tag=value, e.g. 123=ABC");
					}
				}
			}
		}

		private void CheckRepeatingGroup(ITagList msg, int noTag, string[][] sampleGroupData)
		{
			//1) check overall group structure
			ClassicAssert.AreEqual((long)sampleGroupData.Length, msg.GetTagValueAsLong(noTag), "Wrong value of group No tag.");

			var group = msg.GetRepeatingGroup(noTag);
			ClassicAssert.IsNotNull(group, "No group found for NoTag=" + noTag);

			var groupEntries = GetAllEntries(group);
			ClassicAssert.AreEqual((long)sampleGroupData.Length, groupEntries.Length, "Wrong number of entries in group.");
			ClassicAssert.AreEqual((long)groupEntries.Length, msg.GetTagValueAsLong(noTag),
				"Repeating group No-tag value doesn't match number of entries in group");

			//Check group's entries against the sample data

			for (var i = 0; i < sampleGroupData.Length; i++)
			{
				var entryData = sampleGroupData[i];
				var entry = groupEntries[i];

				for (var e = 0; e < entryData.Length; e++)
				{
					var tagValue = entryData[e].Split("=", true);
					if (tagValue.Length == 2)
					{
						ClassicAssert.AreEqual(tagValue[1], entry.GetTagValueAsString(int.Parse(tagValue[0])),
							"Wrong tag/value pair in entry[" + i + "].");
					}
					else
					{
						throw new ArgumentException("Invalid input data: '" + entryData[e] +
													". Expected format: tag=value, e.g. 123=ABC");
					}
				}
			}
		}

		private RepeatingGroup.Entry[] GetAllEntries(RepeatingGroup group)
		{
			var entries = new RepeatingGroup.Entry[@group.Count];
			for (var i = 0; i < @group.Count; i++)
			{
				entries[i] = group.GetEntry(i);
			}

			return entries;
		}

		private RepeatingGroup AddRepeatingGroup(ITagList tagValues, int noTag, string[][] sampleGroupData)
		{
			var group = tagValues.AddRepeatingGroup(noTag);

			for (var i = 0; i < sampleGroupData.Length; i++)
			{
				var entryData = sampleGroupData[i];

				var entry = group.AddEntry();

				for (var e = 0; e < entryData.Length; e++)
				{
					var tagValue = entryData[e].Split("=", true);
					if (tagValue.Length == 2)
					{
						entry.UpdateValue(int.Parse(tagValue[0]), tagValue[1],
							IndexedStorage.MissingTagHandling.AddIfNotExists);
					}
					else
					{
						throw new ArgumentException("Invalid input data: '" + entryData[e] +
													". Expected format: tag=value, e.g. 123=ABC");
					}
				}
			}

			return group;
		}

		[Test]
		public virtual void TestNestedRepeatingGroupManipulation()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

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

			ClassicAssert.AreEqual(
				"8=FIX.4.4 | 35=8 | 555=1 | 600=ZNZ5 | 604=2 | 605=TYZ5 | 606=5 | 605=TYZ5 Comdty | 606=A | ",
				msg.ToPrintableString());
			ClassicAssert.AreEqual("600=ZNZ5 | 604=2 | 605=TYZ5 | 606=5 | 605=TYZ5 Comdty | 606=A | ",
				leg1.ToPrintableString());

			legSecAltIdGroup = leg1.GetRepeatingGroup(604);

			var tagValueAsLong = leg1.GetTagValueAsLong(604);
			ClassicAssert.AreEqual(2L, tagValueAsLong, "Wrong value for msg.leg.NoLegSecurityAltID");
			ClassicAssert.AreEqual(2, legSecAltIdGroup.Count, "Wrong number of entries");

			secAltId2 = legSecAltIdGroup.GetEntry(1);
			secAltId2.Remove();

			ClassicAssert.AreEqual(1L, leg1.GetTagValueAsLong(604), "Wrong value for msg.leg.NoLegSecurityAltID");
			ClassicAssert.AreEqual(1, legSecAltIdGroup.Count, "Wrong number of entries");

			ClassicAssert.AreEqual("8=FIX.4.4 | 35=8 | 555=1 | 600=ZNZ5 | 604=1 | 605=TYZ5 | 606=5 | ",
				msg.ToPrintableString());
			ClassicAssert.AreEqual("600=ZNZ5 | 604=1 | 605=TYZ5 | 606=5 | ", leg1.ToPrintableString());
			ClassicAssert.AreEqual("604=1 | 605=TYZ5 | 606=5 | ", legSecAltIdGroup.ToPrintableString());

			secAltId1 = legSecAltIdGroup.GetEntry(0);
			secAltId1.Remove();

			//entry was removed, thus message doesn't contains empty(0) leading tag too
			//        ClassicAssertEquals("Wrong value for msg.leg.NoLegSecurityAltID", 0l, leg1.getTagValueAsLong(604)); //fails
			ClassicAssert.AreEqual(0, legSecAltIdGroup.Count, "Wrong number of entries"); //passes

			ClassicAssert.AreEqual("8=FIX.4.4 | 35=8 | 555=1 | 600=ZNZ5 | ", msg.ToPrintableString());
			ClassicAssert.AreEqual("600=ZNZ5 | ", leg1.ToPrintableString());
			ClassicAssert.AreEqual("", legSecAltIdGroup.ToPrintableString());
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

			ClassicAssert.AreEqual("8=FIX.4.4 | 35=8 | 453=1 | 448=RBS | 447=D | 452=1 | ", msg.ToPrintableString());
			ClassicAssert.AreEqual("448=RBS | 447=D | 452=1 | ", party1.ToPrintableString());

			//Remove party1
			party1.Remove();
			ClassicAssert.AreEqual("8=FIX.4.4 | 35=8 | ", msg.ToPrintableString());

			ClassicAssert.IsNull(msg.GetTagValueAsString(4711));
			ClassicAssert.IsNull(msg.GetTagValueAsString(453));
			partyGroup.Remove();

			//add another group
			var legGroup = msg.AddRepeatingGroup(555);
			var leg1 = legGroup.AddEntry();
			leg1.UpdateValue(600, "ZNZ5", IndexedStorage.MissingTagHandling.AddIfNotExists);

			//add internal group
			var legSecAltIdGroup = leg1.AddRepeatingGroup(604);

			var secAltId1 = legSecAltIdGroup.AddEntry();
			secAltId1.UpdateValue(605, "TYZ5", IndexedStorage.MissingTagHandling.AddIfNotExists);
			secAltId1.UpdateValue(606, "5", IndexedStorage.MissingTagHandling.AddIfNotExists);

			var secAltId2 = legSecAltIdGroup.AddEntry();
			secAltId2.UpdateValue(605, "TYZ5 Comdty", IndexedStorage.MissingTagHandling.AddIfNotExists);
			secAltId2.UpdateValue(606, "A", IndexedStorage.MissingTagHandling.AddIfNotExists);

			ClassicAssert.AreEqual(
				"8=FIX.4.4 | 35=8 | 555=1 | 600=ZNZ5 | 604=2 | 605=TYZ5 | 606=5 | 605=TYZ5 Comdty | 606=A | ",
				msg.ToPrintableString());
			ClassicAssert.AreEqual("600=ZNZ5 | 604=2 | 605=TYZ5 | 606=5 | 605=TYZ5 Comdty | 606=A | ",
				leg1.ToPrintableString());

			//remove both secAltAID
			secAltId1.Remove();
			secAltId2.Remove();

			ClassicAssert.AreEqual("8=FIX.4.4 | 35=8 | 555=1 | 600=ZNZ5 | ", msg.ToPrintableString());
			ClassicAssert.AreEqual("600=ZNZ5 | ", leg1.ToPrintableString());

			ClassicAssert.IsNull(leg1.GetTagValueAsString(4711));
			ClassicAssert.IsNull(leg1.GetTagValueAsString(604));
			ClassicAssert.IsFalse(leg1.IsTagExists(604));
			ClassicAssert.IsFalse(leg1.IsTagExists(5555));

			//now re-add the nested group
			//        legSecAltIDGroup = leg1.addRepeatingGroup(604);

			secAltId1 = legSecAltIdGroup.AddEntry();
			secAltId1.UpdateValue(605, "TYZ5", IndexedStorage.MissingTagHandling.AddIfNotExists);
			secAltId1.UpdateValue(606, "5", IndexedStorage.MissingTagHandling.AddIfNotExists);

			secAltId2 = legSecAltIdGroup.AddEntry();
			secAltId2.UpdateValue(605, "TYZ5 Comdty", IndexedStorage.MissingTagHandling.AddIfNotExists);
			secAltId2.UpdateValue(606, "A", IndexedStorage.MissingTagHandling.AddIfNotExists);

			ClassicAssert.AreEqual(
				"8=FIX.4.4 | 35=8 | 555=1 | 600=ZNZ5 | 604=2 | 605=TYZ5 | 606=5 | 605=TYZ5 Comdty | 606=A | ",
				msg.ToPrintableString());
			ClassicAssert.AreEqual("600=ZNZ5 | 604=2 | 605=TYZ5 | 606=5 | 605=TYZ5 Comdty | 606=A | ",
				leg1.ToPrintableString());

			ClassicAssert.AreEqual(2, leg1.GetTagValueAsLong(604));

			//add another internal group
			var newRepeatingGroup = secAltId1.AddRepeatingGroup(1234);
			var newEntry1 = newRepeatingGroup.AddEntry();
			var newEntry2 = newRepeatingGroup.AddEntry();
			newEntry2.AddTag(234, "2");
			newEntry1.AddTag(234, "1");

			ClassicAssert.AreEqual(
				"8=FIX.4.4 | 35=8 | 555=1 | 600=ZNZ5 | 604=2 | 605=TYZ5 | 606=5 | 1234=2 | 234=1 | 234=2 | 605=TYZ5 Comdty | 606=A | ",
				msg.ToPrintableString());
			ClassicAssert.AreEqual("1234=2 | 234=1 | 234=2 | ", newRepeatingGroup.ToPrintableString());
			ClassicAssert.AreEqual("600=ZNZ5 | 604=2 | 605=TYZ5 | 606=5 | 1234=2 | 234=1 | 234=2 | 605=TYZ5 Comdty | 606=A | ",
				leg1.ToPrintableString());

			newEntry1.Remove();
			secAltId1.AddTag(222, "222");

			ClassicAssert.AreEqual(
				"600=ZNZ5 | 604=2 | 605=TYZ5 | 606=5 | 1234=1 | 234=2 | 222=222 | 605=TYZ5 Comdty | 606=A | ",
				leg1.ToPrintableString());

			newEntry2.AddTag(235, "235");

			ClassicAssert.AreEqual(
				"600=ZNZ5 | 604=2 | 605=TYZ5 | 606=5 | 1234=1 | 234=2 | 235=235 | 222=222 | 605=TYZ5 Comdty | 606=A | ",
				leg1.ToPrintableString());
		}

		[Test]
		public virtual void testNestedRepeatingGroupManipulation_3()
		{
			var msg = new FixMessage();
			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

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

			ClassicAssert.AreEqual(
				"8=FIX.4.4 | 35=8 | 555=1 | 600=ZNZ5 | 604=2 | 605=TYZ5 | 606=5 | 605=TYZ5 Comdty | 606=A | ",
				msg.ToPrintableString());
			ClassicAssert.AreEqual("600=ZNZ5 | 604=2 | 605=TYZ5 | 606=5 | 605=TYZ5 Comdty | 606=A | ",
				leg1.ToPrintableString());

			ClassicAssert.AreEqual(2L, leg1.GetTagValueAsLong(604), "Wrong value of group No tag.");

			CheckRepeatingGroup(leg1, 604, TestSampleSecurityAltIdGroup);

			//remove both secAltAID
			secAltId1.Remove();
			secAltId2.Remove();

			ClassicAssert.AreEqual("8=FIX.4.4 | 35=8 | 555=1 | 600=ZNZ5 | ", msg.ToPrintableString());
			ClassicAssert.AreEqual("600=ZNZ5 | ", leg1.ToPrintableString());

			ClassicAssert.IsNull(leg1.GetTagValueAsString(4711)); //passes, returns null as expected
			ClassicAssert.IsNull(leg1.GetTagValueAsString(604)); //passes, returns null as expected

			//now re-add the nested group
			//        legSecAltIDGroup = leg1.addRepeatingGroup(604);

			secAltId1 = legSecAltIdGroup.AddEntry();
			secAltId1.UpdateValue(605, "TYZ5", IndexedStorage.MissingTagHandling.AddIfNotExists);
			secAltId1.UpdateValue(606, "5", IndexedStorage.MissingTagHandling.AddIfNotExists);

			secAltId2 = legSecAltIdGroup.AddEntry();
			secAltId2.UpdateValue(605, "TYZ5 Comdty", IndexedStorage.MissingTagHandling.AddIfNotExists);
			secAltId2.UpdateValue(606, "A", IndexedStorage.MissingTagHandling.AddIfNotExists);

			ClassicAssert.AreEqual(
				"8=FIX.4.4 | 35=8 | 555=1 | 600=ZNZ5 | 604=2 | 605=TYZ5 | 606=5 | 605=TYZ5 Comdty | 606=A | ",
				msg.ToPrintableString());
			ClassicAssert.AreEqual("600=ZNZ5 | 604=2 | 605=TYZ5 | 606=5 | 605=TYZ5 Comdty | 606=A | ",
				leg1.ToPrintableString());

			ClassicAssert.AreEqual(2, leg1.GetTagValueAsLong(604));
			ClassicAssert.AreEqual("TYZ5", secAltId1.GetTagValueAsString(605));
			ClassicAssert.AreEqual("5", secAltId1.GetTagValueAsString(606));
			ClassicAssert.AreEqual("TYZ5 Comdty", secAltId2.GetTagValueAsString(605));
			ClassicAssert.AreEqual("A", secAltId2.GetTagValueAsString(606));

			CheckRepeatingGroup(leg1, 604, TestSampleSecurityAltIdGroup);
			ClassicAssert.AreEqual(2, leg1.GetTagValueAsLong(604));
		}

		[Test]
		public virtual void TestRepeatingGroupManipulation()
		{
			var msg = RawFixUtil.GetFixMessage(GetMessageBytes(
				"8=FIX.4.4 | 9=999 | 35=8 | 34=18195 | 49=RBS_UK_PRD_TFC | 56=RBS_UK_PRD_TES | 43=N | 52=20151207-11:50:46.001 | 57=TM | 200=201603 | 1=86687 | 6=158.100000000000 | 11=EURGOV-u3lEU54td0@1#EUREX#FGBLH6 | 12=0.000000 | 13=3 | 14=25 | 15=EUR | 17=00000636720TRLO1.1.1 | 22=8 | 30=XEUR | 31=158.100000000000 | 32=25 | 37=00000331447ORLO1 | 38=25 | 39=2 | 40=2 | 44=158.100000000000 | 528=P | 48=FGBL | 54=1 | 55=FGBL | 59=0 | 60=20151207-11:50:45.979 | 64=20151210 | 63=6 | 75=20151207 | 76=EUXCCP | 99=0.000000 | 126=20151207-21:30:00.000 | 119=0.000000 | 120=EUR | 150=F | 151=0.000000 | 207=XEUR | 7562=00000434931PELO1 | 526=00000333089POLO1 | 198=1449469807193368590 | 442=1 | 461=FXXXS | 527=10617800_1449469807193368590 | 107=German 10Y Govt. Euro Bond (EUREX) | 583=2|90200703 | 8105=FQCXxksTlx | 18991=FID-FIX | 18999=EURGOV | 1028=Y | 6002=_X:BBGUuid=11121944;_X:BBGBook=SFI-EUBM;_X:BBGAcc=CPFAUTO | 18994=PRD | 18992=UK | 18989={[_|_|_],[FIX_OPAUDIT|negallu|_],[RBS_UK_PRD_TFC|_|_]} | 454=2 | 455=RXH6 Comdty | 456=A | 455=FGBLH6 | 456=5 | 453=4 | 448=negallu | 447=D | 452=11 | 448=RBS | 447=D | 452=3 | 448=FIX.DMA | 447=D | 452=12 | 448=86687 | 447=D | 452=24 | 10=024 | "));

			/*
			 * Update and remove entry
			 */
			CheckRepeatingGroup(msg, 453, new[]
			{
				new[] { "448=negallu", "447=D", "452=11" },
				new[] { "448=RBS", "447=D", "452=3" },
				new[] { "448=FIX.DMA", "447=D", "452=12" },
				new[] { "448=86687", "447=D", "452=24" }
			});

			//update the repeating group with 448=RBS and change it to 448=rbs
			var partyGroup = msg.GetRepeatingGroup(453);
			var entry = partyGroup.GetEntry(1);
			entry.UpdateValue(448, "rbs", IndexedStorage.MissingTagHandling.AddIfNotExists);

			CheckRepeatingGroup(msg, 453, new[]
			{
				new[] { "448=negallu", "447=D", "452=11" },
				new[] { "448=rbs", "447=D", "452=3" },
				new[] { "448=FIX.DMA", "447=D", "452=12" },
				new[] { "448=86687", "447=D", "452=24" }
			});

			//remove the repeating group entry with 448=FIX.DMA
			entry = partyGroup.GetEntry(2);
			entry.Remove();

			CheckRepeatingGroup(msg, 453, new[]
			{
				new[] { "448=negallu", "447=D", "452=11" },
				new[] { "448=rbs", "447=D", "452=3" },
				new[] { "448=86687", "447=D", "452=24" }
			});


			/*
			 * Successive removing RG, entry by entry
			 */
			CheckRepeatingGroup(msg, 454, new[]
			{
				new[] { "455=RXH6 Comdty", "456=A" },
				new[] { "455=FGBLH6", "456=5" }
			});

			//remove the repeating group entry with 456=A
			var altSecIdGroup = msg.GetRepeatingGroup(454);
			entry = altSecIdGroup.GetEntry(0);
			entry.Remove();

			CheckRepeatingGroup(msg, 454, new[]
			{
				new[] { "455=FGBLH6", "456=5" }
			});

			//remove the repeating group entry with 456=5
			entry = altSecIdGroup.GetEntry(0);
			entry.Remove();
			altSecIdGroup.Remove();

			ClassicAssert.IsNull(GetGroup(msg, 454), "SecurityAltID group not removed from message.");


			/*
			 * Add group and then remove it again
			 */
			AddRepeatingGroup(msg, 454, new[]
			{
				new[] { "455=RXH6 Comdty", "456=A" },
				new[] { "455=FGBLH6", "456=5" }
			});


			CheckRepeatingGroup(msg, 454, new[]
			{
				new[] { "455=RXH6 Comdty", "456=A" },
				new[] { "455=FGBLH6", "456=5" }
			});

			msg.RemoveRepeatingGroup(454);
			ClassicAssert.IsNull(GetGroup(msg, 454), "SecurityAltID group not removed from message.");


			/*
			 * Run x-check via the message's byte[]
			 */
			var xcheckMsg = RawFixUtil.GetFixMessage(msg.AsByteArray());
			CheckRepeatingGroup(xcheckMsg, 453, new[]
			{
				new[] { "448=negallu", "447=D", "452=11" },
				new[] { "448=rbs", "447=D", "452=3" },
				new[] { "448=86687", "447=D", "452=24" }
			});

			ClassicAssert.IsNull(GetGroup(xcheckMsg, 454), "SecurityAltID group shouldn't be on message.");
		}

		[Test]
		public virtual void ultra263_testToString()
		{
			var msg = new FixMessage();

			msg.Set(8, "FIX.4.4");
			msg.Set(35, "8");

			AddRepeatingGroup(msg, 453, TestSamplePartyGroup);

			var legGroup = msg.AddRepeatingGroup(555);
			var leg1 = legGroup.AddEntry();
			leg1.UpdateValue(600, "ZNZ5", IndexedStorage.MissingTagHandling.AddIfNotExists);
			AddRepeatingGroup(leg1, 454, TestSampleSecurityAltIdGroup);

			ClassicAssert.AreEqual(StringHelper.NewString(msg.AsByteArray()).Replace("\u0001", " | "), msg.ToPrintableString(),
				"IFIXMessage.toString() returns invalid message string");
		}
	}
}
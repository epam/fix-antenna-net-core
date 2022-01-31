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

using System.Text;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation.FixMessage.Tree;
using Epam.FixAntenna.NetCore.Validation.Utils;
using NUnit.Framework;

namespace Epam.FixAntenna.Validation.Tests.Engine.FIXMessage.Tree
{
	[TestFixture]
	internal class FixMessageTreeUtilTest
	{
		[SetUp]
		public void SetUp()
		{
			_fixUtil = FixUtilFactory.Instance
				.GetFixUtil(FixVersionContainer.GetFixVersionContainer(FixVersion.Fix44));
		}

		private const string Message =
			"8=FIX.4.2\u00019=244\u000135=i\u000149=TESTI\u000156=TESTA\u000134=4\u000152=20030204-0x8:56:41\u0001117=1116226\u0001296=2\u0001302=0001\u0001311=TESTA\u0001304=5\u0001295=2\u0001299=11\u000155=TESTB\u0001132=11\u0001299=12\u000155=TESTC\u0001133=12\u0001302=0002\u0001311=TESTB\u0001304=5\u0001295=3\u0001299=21\u000155=TESTD\u0001132=13\u0001299=22\u000155=TESTE\u0001133=14\u0001299=23\u000155=TESTF\u0001133=15\u000110=0x1D\u0001";

		private const string MsgRgInRg =
			"8=FIX.4.4\u00019=397\u000135=8\u000149=RECO\u000156=NKPI\u000134=15\u000150=DBS\u000157=PUBLIC\u000143=Y\u000152=20050214-21:0x2:0x6\u0001122=20050214-16:13:54\u000137=11-20050214-11:14:39\u0001527=0000FB66-F9C0A794\u000111=1108397679775\u0001453=3\u0001448=GINS\u0001447=C\u0001452=1\u0001802=2\u0001523=0x54\u0001803=17\u0001523=Tom\u0001803=9\u0001448=CHAS\u0001452=17\u0001802=1\u0001523=0x74\u0001803=17\u0001448=LLOYD\u0001447=C\u0001452=22\u000117=11-20050214-11:14:39\u0001150=F\u000132=5\u000131=97.5\u000139=2\u000155=[N/A]\u000148=700690AS9\u000122=1\u000154=1\u000138=5\u000144=97.5\u0001151=0\u000114=5\u00016=97.5\u000160=20050214-11:14:39\u000110=0x88\u0001";

		private FixUtil _fixUtil;

		private const string EntrySpace = "--";
		private const string NewLine = "\n";

		public int GetFieldsNum(FixEntry entry)
		{
			var size = entry.Fields.Count;
			foreach (var rg in entry.RepeatingGroups)
			{
				size++;
				foreach (var rgEntry in rg.Entries)
				{
					size += GetFieldsNum(rgEntry);
				}
			}

			return size;
		}

		private StringBuilder Append(FixEntry block, StringBuilder builder, int entryNum)
		{
			var space = BuildTab(entryNum);
			foreach (var filed in block.Fields)
			{
				Append(filed, builder, space);
			}

			var nextEntryNum = entryNum + 1;
			foreach (var rg in block.RepeatingGroups)
			{
				Append(rg, builder, nextEntryNum);
			}

			return builder;
		}

		private StringBuilder Append(FixRepeatingGroup rg, StringBuilder builder, int entryNum)
		{
			var space = BuildTab(entryNum);
			builder.Append(space).Append('+').Append(rg.TagId).Append('=').Append(rg.Entries.Count)
				.Append(NewLine);
			var nextEntryNum = entryNum + 1;
			foreach (var block in rg.Entries)
			{
				builder.Append(space).Append("[").Append(NewLine);
				Append(block, builder, nextEntryNum);
				builder.Append(space).Append("]").Append(NewLine);
			}

			return builder;
		}

		private StringBuilder Append(TagValue filed, StringBuilder builder, string space)
		{
			return builder.Append(space).Append(filed.TagId).Append('=')
				.Append(filed.StringValue).Append(NewLine);
		}

		private string BuildTab(int entryNum)
		{
			var filedEntrySpace = "";
			for (var i = 0; i < entryNum; i++)
			{
				filedEntrySpace += EntrySpace;
			}

			return filedEntrySpace;
		}

		[Test]
		public void TestTreeMessageBlock()
		{
			var list = RawFixUtil.GetFixMessage(MsgRgInRg.AsByteArray());
			var entry = FixMessageTreeUtil.BuildMessageTree(list, _fixUtil);

			Assert.AreEqual(list.Length, GetFieldsNum(entry));
		}
	}
}
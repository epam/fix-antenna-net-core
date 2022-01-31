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

using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework;

namespace Epam.FixAntenna.Message.Tests.Rg
{
	[TestFixture]
	internal class RepeatingGroupCopyApi
	{
		[SetUp]
		public virtual void CreateMsg()
		{
			_srcMsg = RawFixUtil.GetFixMessage(_executionReportForCopy.AsByteArray());
			_dstMsg = RawFixUtil.GetFixMessage(_targetExecutionReport.AsByteArray());
		}

		private readonly string _executionReportForCopy =
			"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
			"454=1\u0001455=5\u0001456=abc\u0001" + "232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001" +
			"555=2\u0001600=12\u0001603=13\u0001" + "251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
			"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001" +
			"539=2\u0001524=23\u0001524=24\u0001525=25\u000110=124\u0001";

		private readonly string _targetExecutionReport =
			"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
			"454=1\u0001455=5\u0001456=abc\u0001" + "555=2\u0001600=12\u0001603=13\u0001251=15\u0001" +
			"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001" +
			"539=2\u0001524=23\u0001524=24\u0001525=25\u000110=124\u0001";

		private FixMessage _srcMsg;
		private FixMessage _dstMsg;

		[Test]
		public virtual void CopyGroupEntry()
		{
			var groupForCopy = _srcMsg.GetRepeatingGroup(454);
			var entryForCopy = groupForCopy.GetEntry(0);
			var targetGroup = _dstMsg.GetRepeatingGroup(454);

			var copiedEntry =
				targetGroup.CopyEntry(entryForCopy, 1); //entry for copy and index where entry will be inserted

			Assert.AreEqual(copiedEntry.ToString(), entryForCopy.ToString());

			copiedEntry.RemoveTag(456);

			var expectedTargetMsg =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"454=2\u0001455=5\u0001456=abc\u0001455=5\u0001" + "555=2\u0001600=12\u0001603=13\u0001251=15\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001" +
				"539=2\u0001524=23\u0001524=24\u0001525=25\u000110=124\u0001";
			var expectedMsgForCopy = _executionReportForCopy;

			Assert.AreEqual(expectedTargetMsg, _dstMsg.ToString());
			Assert.AreEqual(expectedMsgForCopy, _srcMsg.ToString());
			Assert.IsFalse(entryForCopy.ToString().Equals(copiedEntry.ToString()));
		}

		[Test]
		public virtual void CopyNestedGroup()
		{
			var groupForCopy = _srcMsg.GetRepeatingGroup(555).GetEntry(0).GetRepeatingGroup(539);
			var targetEntry = _dstMsg.GetRepeatingGroup(555).GetEntry(0);

			var copiedGroup =
				targetEntry.CopyRepeatingGroup(
					groupForCopy); //Actually you can insert nested group in any entry or even in root of message without limitation

			Assert.AreEqual(copiedGroup.ToString(), groupForCopy.ToString());

			copiedGroup.RemoveEntry(1);

			var expectedTargetMsg =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"454=1\u0001455=5\u0001456=abc\u0001" + "555=2\u0001600=12\u0001603=13\u0001251=15\u0001" +
				"539=1\u0001524=16\u0001525=17\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001" +
				"539=2\u0001524=23\u0001524=24\u0001525=25\u000110=124\u0001";
			var expectedMsgForCopy = _executionReportForCopy;
			Assert.AreEqual(expectedTargetMsg, _dstMsg.ToString());
			Assert.AreEqual(expectedMsgForCopy, _srcMsg.ToString());
			Assert.IsFalse(copiedGroup.ToString().Equals(groupForCopy.ToString()));
		}

		[Test]
		public virtual void CopyRepeatingGroup()
		{
			var srcGroup = _srcMsg.GetRepeatingGroup(232);
			var copiedGroup =
				_dstMsg.CopyRepeatingGroup(srcGroup, 9); // group for copy and index where group will be inserted

			Assert.AreEqual(copiedGroup.ToString(), srcGroup.ToString());

			var newEntry = copiedGroup.AddEntry();
			newEntry.AddTag(233, 123);

			var expectedMsgWithGroup = _executionReportForCopy;
			var expectedMsgWithoutGroup =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"232=3\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001233=123\u0001" +
				"454=1\u0001455=5\u0001456=abc\u0001" + "555=2\u0001600=12\u0001603=13\u0001251=15\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001" +
				"539=2\u0001524=23\u0001524=24\u0001525=25\u000110=124\u0001";
			Assert.AreEqual(expectedMsgWithGroup, _srcMsg.ToString());
			Assert.AreEqual(expectedMsgWithoutGroup, _dstMsg.ToString());
			Assert.IsFalse(srcGroup.ToString().Equals(copiedGroup.ToString()));
		}
	}
}
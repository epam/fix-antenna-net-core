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
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Message.Rg.Exceptions;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Message.Tests.Rg
{
	[TestFixture]
	internal class RepeatingGroupParseValidationTest
	{
		private readonly FixVersionContainer _version = FixVersionContainer.GetFixVersionContainer(FixVersion.Fix43);
		private readonly string _msgType = "8";

		[Test]
		public virtual void DelimiterTagNotFollowLeadingTagAtInnerGroup()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"382=2\u0001375=1\u0001337=2\u0001375=3\u0001437=4\u0001" + "454=1\u0001455=5\u0001456=6\u0001" +
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001" + "518=1\u0001519=10\u0001520=11\u0001" +
				"555=2\u0001600=12\u0001603=13\u0001" +
				"604=1\u0001605=14\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001606=210\u0001605=21\u0001605=32\u0001251=22\u0001" +
				"539=2\u0001524=23\u0001524=24\u0001525=25\u000110=124\u0001";
			var msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());

			ClassicAssert.Throws<InvalidDelimiterTagException>(() => RawFixUtil.IndexRepeatingGroup(msg, true),
				new InvalidDelimiterTagException(605, 606, _version, _msgType).Message);
		}

		[Test]
		public virtual void DelimiterTagNotFollowLeadingTagAtOuterGroup()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"382=2\u0001375=1\u0001337=2\u0001375=3\u0001437=4\u0001" + "454=1\u0001456=6\u0001455=5\u0001" +
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001" + "518=1\u0001519=10\u0001520=11\u0001" +
				"555=2\u0001600=12\u0001603=13\u0001" +
				"604=1\u0001605=14\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001" +
				"539=2\u0001524=23\u0001524=24\u0001525=25\u000110=124\u0001";
			var msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());

			ClassicAssert.Throws<InvalidDelimiterTagException>(() => RawFixUtil.IndexRepeatingGroup(msg, true),
				new InvalidDelimiterTagException(455, 456, _version, _msgType).Message);
		}

		[Test]
		public virtual void DuplicateTagAtInnerGroup()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"382=2\u0001375=1\u0001337=2\u0001375=3\u0001437=4\u0001" + "454=1\u0001455=5\u0001456=6\u0001" +
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001" + "518=1\u0001519=10\u0001520=11\u0001" +
				"555=2\u0001600=12\u0001603=13\u0001" +
				"604=1\u0001605=14\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001606=123\u0001606=124\u0001605=32\u0001251=22\u0001" +
				"539=2\u0001524=23\u0001524=24\u0001525=25\u000110=124\u0001";
			var msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());

			ClassicAssert.Throws<DuplicateTagException>(() => RawFixUtil.IndexRepeatingGroup(msg, true),
				new DuplicateTagException(604, 606, _version, _msgType).Message);
		}

		[Test]
		public virtual void DuplicateTagAtOuterGroup()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"382=2\u0001375=1\u0001337=2\u0001337=3\u0001375=3\u0001437=4\u0001" +
				"454=1\u0001455=5\u0001456=6\u0001" + "232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001" +
				"518=1\u0001519=10\u0001520=11\u0001" + "555=2\u0001600=12\u0001603=13\u0001" +
				"604=1\u0001605=14\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001" +
				"539=2\u0001524=23\u0001524=24\u0001525=25\u000110=124\u0001";
			var msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());

			ClassicAssert.Throws<DuplicateTagException>(() => RawFixUtil.IndexRepeatingGroup(msg, true),
				new DuplicateTagException(382, 337, _version, _msgType).Message);
		}

		[Test]
		public virtual void LeadingTagValueGreaterThanEntryCountInNestedGroup()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"382=2\u0001375=1\u0001337=2\u0001375=3\u0001437=4\u0001" + "454=1\u0001455=5\u0001456=6\u0001" +
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001" + "518=1\u0001519=10\u0001520=11\u0001" +
				"555=2\u0001600=12\u0001603=13\u0001" +
				"604=1\u0001605=14\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
				"600=19\u0001603=20\u0001604=3\u0001605=21\u0001605=32\u0001251=22\u0001" +
				"539=2\u0001524=23\u0001524=24\u0001525=25\u000110=124\u0001";
			var msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());

			ClassicAssert.Throws<InvalidLeadingTagValueException>(() => RawFixUtil.IndexRepeatingGroup(msg, true),
				new InvalidLeadingTagValueException(604, true, _version, _msgType).Message);
		}

		[Test]
		public virtual void LeadingTagValueGreaterThanEntryCountInOuterGroup()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"382=3\u0001375=1\u0001337=2\u0001375=3\u0001437=4\u0001" + "454=1\u0001455=5\u0001456=6\u0001" +
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001" + "518=1\u0001519=10\u0001520=11\u0001" +
				"555=2\u0001600=12\u0001603=13\u0001" +
				"604=1\u0001605=14\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001" +
				"539=2\u0001524=23\u0001524=24\u0001525=25\u000110=124\u0001";
			var msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());

			ClassicAssert.Throws<InvalidLeadingTagValueException>(() => RawFixUtil.IndexRepeatingGroup(msg, true),
				new InvalidLeadingTagValueException(382, true, _version, _msgType).Message);
		}

		[Test]
		public virtual void LeadingTagValueLessThanEntryCountInNestedGroup()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"382=2\u0001375=1\u0001337=2\u0001375=3\u0001437=4\u0001" + "454=1\u0001455=5\u0001456=6\u0001" +
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001" + "518=1\u0001519=10\u0001520=11\u0001" +
				"555=2\u0001600=12\u0001603=13\u0001" +
				"604=1\u0001605=14\u0001605=140\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001" +
				"539=2\u0001524=23\u0001524=24\u0001525=25\u000110=124\u0001";
			var msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());

			ClassicAssert.Throws<InvalidLeadingTagValueException>(() => RawFixUtil.IndexRepeatingGroup(msg, true),
				new InvalidLeadingTagValueException(604, false, _version, _msgType).Message);
		}

		[Test]
		public virtual void LeadingTagValueLessThanEntryCountInOuterGroup()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"382=2\u0001375=1\u0001337=2\u0001375=3\u0001437=4\u0001" + "454=1\u0001455=5\u0001456=6\u0001" +
				"232=1\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001" + "518=1\u0001519=10\u0001520=11\u0001" +
				"555=2\u0001600=12\u0001603=13\u0001" +
				"604=1\u0001605=14\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001" +
				"539=2\u0001524=23\u0001524=24\u0001525=25\u000110=124\u0001";
			var msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());

			ClassicAssert.Throws<InvalidLeadingTagValueException>(() => RawFixUtil.IndexRepeatingGroup(msg, true),
				new InvalidLeadingTagValueException(232, false, _version, _msgType).Message);
		}

		[Test]
		public virtual void NotGroupTagAtCenterOfInnerGroup()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"382=2\u0001375=1\u0001337=2\u0001375=3\u0001437=4\u0001" + "454=1\u0001455=5\u0001456=6\u0001" +
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001" + "518=1\u0001519=10\u0001520=11\u0001" +
				"555=2\u0001600=12\u0001603=13\u0001" +
				"604=1\u0001605=14\u0001123=123\u0001606=123\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001" +
				"539=2\u0001524=23\u0001524=24\u0001525=25\u000110=124\u0001";
			var msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());

			ClassicAssert.Throws<InvalidLeadingTagValueException>(() => RawFixUtil.IndexRepeatingGroup(msg, true),
				new InvalidLeadingTagValueException(555, true, _version, _msgType).Message);
		}

		[Test]
		public virtual void NotGroupTagAtCenterOfOuterGroup()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"382=2\u0001375=1\u0001337=2\u0001375=3\u0001437=4\u0001" +
				"454=1\u0001455=5\u0001123=123\u0001456=6\u0001" +
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001" + "518=1\u0001519=10\u0001520=11\u0001" +
				"555=2\u0001600=12\u0001603=13\u0001" +
				"604=1\u0001605=14\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001" +
				"539=2\u0001524=23\u0001524=24\u0001525=25\u000110=124\u0001";
			var msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());

			ClassicAssert.Throws<UnexpectedGroupTagException>(() => RawFixUtil.IndexRepeatingGroup(msg, true),
				new UnexpectedGroupTagException(456, _version, _msgType).Message);
		}

		[Test]
		public virtual void NotGroupTagAtEndOfEntryOfInnerGroup()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"382=2\u0001375=1\u0001337=2\u0001375=3\u0001437=4\u0001" + "454=1\u0001455=5\u0001456=6\u0001" +
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001" + "518=1\u0001519=10\u0001520=11\u0001" +
				"555=2\u0001600=12\u0001603=13\u0001" +
				"604=1\u0001605=14\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001123=123\u0001605=32\u0001251=22\u0001" +
				"539=2\u0001524=23\u0001524=24\u0001525=25\u000110=124\u0001";
			var msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());

			ClassicAssert.Throws<InvalidLeadingTagValueException>(() => RawFixUtil.IndexRepeatingGroup(msg, true),
				new InvalidLeadingTagValueException(604, true, _version, _msgType).Message);
		}

		[Test]
		public virtual void NotGroupTagAtEndOfEntryOfOuterGroup()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"382=2\u0001375=1\u0001337=2\u0001123=123\u0001375=3\u0001437=4\u0001" +
				"454=1\u0001455=5\u0001456=6\u0001" + "232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001" +
				"518=1\u0001519=10\u0001520=11\u0001" + "555=2\u0001600=12\u0001603=13\u0001" +
				"604=1\u0001605=14\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001" +
				"539=2\u0001524=23\u0001524=24\u0001525=25\u000110=124\u0001";
			var msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());

			ClassicAssert.Throws<InvalidLeadingTagValueException>(() => RawFixUtil.IndexRepeatingGroup(msg, true),
				new InvalidLeadingTagValueException(382, true, _version, _msgType).Message);
		}

		[Test]
		public virtual void NotGroupTagAtEndOfInnerGroup()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"382=2\u0001375=1\u0001337=2\u0001375=3\u0001437=4\u0001" + "454=1\u0001455=5\u0001456=6\u0001" +
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001" + "518=1\u0001519=10\u0001520=11\u0001" +
				"555=2\u0001600=12\u0001603=13\u0001" +
				"604=1\u0001605=14\u0001123=123\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001" +
				"539=2\u0001524=23\u0001524=24\u0001525=25\u000110=124\u0001";
			var msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());

			ClassicAssert.Throws<InvalidLeadingTagValueException>(() => RawFixUtil.IndexRepeatingGroup(msg, true),
				new InvalidLeadingTagValueException(555, true, _version, _msgType).Message);
		}

		[Test]
		public virtual void SelfLeadingTagAtCenterOfInnerGroup()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"382=2\u0001375=1\u0001337=2\u0001375=3\u0001437=4\u0001" + "454=1\u0001455=5\u0001456=6\u0001" +
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001" + "518=1\u0001519=10\u0001520=11\u0001" +
				"555=2\u0001600=12\u0001603=13\u0001" +
				"604=1\u0001605=14\u0001604=123\u0001606=123\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001" +
				"539=2\u0001524=23\u0001524=24\u0001525=25\u000110=124\u0001";
			var msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());

			ClassicAssert.Throws<DuplicateTagException>(() => RawFixUtil.IndexRepeatingGroup(msg, true),
				new DuplicateTagException(555, 604, _version, _msgType).Message);
		}

		[Test]
		public virtual void SelfLeadingTagAtCenterOfOuterGroup()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"382=2\u0001375=1\u0001337=2\u0001375=3\u0001437=4\u0001" +
				"454=1\u0001455=5\u0001454=2\u0001456=6\u0001" +
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001" + "518=1\u0001519=10\u0001520=11\u0001" +
				"555=2\u0001600=12\u0001603=13\u0001" +
				"604=1\u0001605=14\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001" +
				"539=2\u0001524=23\u0001524=24\u0001525=25\u000110=124\u0001";
			var msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());

			ClassicAssert.Throws<DuplicateGroupException>(() => RawFixUtil.IndexRepeatingGroup(msg, true),
				new DuplicateGroupException(454, _version, _msgType).Message);
		}

		[Test]
		public virtual void SelfLeadingTagAtEndOfEntryOfInnerGroup()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"382=2\u0001375=1\u0001337=2\u0001375=3\u0001437=4\u0001" +
				"454=2\u0001455=5\u0001456=6\u0001455=50\u0001456=60\u0001" +
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001" + "518=1\u0001519=10\u0001520=11\u0001" +
				"555=2\u0001600=12\u0001603=13\u0001" +
				"604=1\u0001605=14\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001606=123\u0001604=124\u0001605=32\u0001251=22\u0001" +
				"539=2\u0001524=23\u0001524=24\u0001525=25\u000110=124\u0001";
			var msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());

			ClassicAssert.Throws<InvalidLeadingTagValueException>(() => RawFixUtil.IndexRepeatingGroup(msg, true),
				new InvalidLeadingTagValueException(604, true, _version, _msgType).Message);
		}

		[Test]
		public virtual void SelfLeadingTagAtEndOfEntryOfOuterGroup()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"382=2\u0001375=1\u0001337=2\u0001375=3\u0001437=4\u0001" +
				"454=2\u0001455=5\u0001456=6\u0001454=2\u0001455=50\u0001456=60\u0001" +
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001" + "518=1\u0001519=10\u0001520=11\u0001" +
				"555=2\u0001600=12\u0001603=13\u0001" +
				"604=1\u0001605=14\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001" +
				"539=2\u0001524=23\u0001524=24\u0001525=25\u000110=124\u0001";
			var msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());

			ClassicAssert.Throws<InvalidLeadingTagValueException>(() => RawFixUtil.IndexRepeatingGroup(msg, true),
				new InvalidLeadingTagValueException(454, true, _version, _msgType).Message);
		}

		[Test]
		public virtual void SelfLeadingTagAtEndOfInnerGroup()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"382=2\u0001375=1\u0001337=2\u0001375=3\u0001437=4\u0001" + "454=1\u0001455=5\u0001456=6\u0001" +
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001" + "518=1\u0001519=10\u0001520=11\u0001" +
				"555=2\u0001600=12\u0001603=13\u0001" +
				"604=1\u0001605=14\u0001604=123\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001" +
				"539=2\u0001524=23\u0001524=24\u0001525=25\u000110=124\u0001";
			var msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());

			ClassicAssert.Throws<DuplicateTagException>(() => RawFixUtil.IndexRepeatingGroup(msg, true),
				new DuplicateTagException(555, 604, _version, _msgType).Message);
		}

		[Test]
		public virtual void SelfLeadingTagAtEndOfOuterGroup()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"382=2\u0001375=1\u0001337=2\u0001375=3\u0001437=4\u0001" +
				"454=1\u0001455=5\u0001456=6\u0001454=2\u0001" +
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001" + "518=1\u0001519=10\u0001520=11\u0001" +
				"555=2\u0001600=12\u0001603=13\u0001" +
				"604=1\u0001605=14\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001" +
				"539=2\u0001524=23\u0001524=24\u0001525=25\u000110=124\u0001";
			var msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());

			ClassicAssert.Throws<DuplicateGroupException>(() => RawFixUtil.IndexRepeatingGroup(msg, true),
				new DuplicateGroupException(454, _version, _msgType).Message);
		}

		[Test]
		public virtual void TagFromOtherGroupAtCenterOfInnerGroup()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"382=2\u0001375=1\u0001337=2\u0001375=3\u0001437=4\u0001" + "454=1\u0001455=5\u0001456=6\u0001" +
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001" + "518=1\u0001519=10\u0001520=11\u0001" +
				"555=2\u0001600=12\u0001603=13\u0001" +
				"604=1\u0001605=14\u0001455=5\u0001606=120\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001" +
				"539=2\u0001524=23\u0001524=24\u0001525=25\u000110=124\u0001";
			var msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());

			ClassicAssert.Throws<InvalidLeadingTagValueException>(() => RawFixUtil.IndexRepeatingGroup(msg, true),
				new InvalidLeadingTagValueException(555, true, _version, _msgType).Message);
		}

		[Test]
		public virtual void TagFromOtherGroupAtCenterOfOuterGroup()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"382=2\u0001375=1\u0001337=2\u0001375=3\u0001437=4\u0001" +
				"454=1\u0001455=5\u0001603=32\u0001456=6\u0001" +
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001" + "518=1\u0001519=10\u0001520=11\u0001" +
				"555=2\u0001600=12\u0001603=13\u0001" +
				"604=1\u0001605=14\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001" +
				"539=2\u0001524=23\u0001524=24\u0001525=25\u000110=124\u0001";
			var msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());

			ClassicAssert.Throws<UnexpectedGroupTagException>(() => RawFixUtil.IndexRepeatingGroup(msg, true),
				new UnexpectedGroupTagException(603, _version, _msgType).Message);
		}

		[Test]
		public virtual void TagFromOtherGroupAtEndOfEntryOfInnerGroup()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"382=2\u0001375=1\u0001337=2\u0001375=3\u0001437=4\u0001" + "454=1\u0001455=5\u0001456=6\u0001" +
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001" + "518=1\u0001519=10\u0001520=11\u0001" +
				"555=2\u0001600=12\u0001603=13\u0001" +
				"604=1\u0001605=14\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001455=123\u0001605=32\u0001251=22\u0001" +
				"539=2\u0001524=23\u0001524=24\u0001525=25\u000110=124\u0001";
			var msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());

			ClassicAssert.Throws<InvalidLeadingTagValueException>(() => RawFixUtil.IndexRepeatingGroup(msg, true),
				new InvalidLeadingTagValueException(604, true, _version, _msgType).Message);
		}

		[Test]
		public virtual void TagFromOtherGroupAtEndOfEntryOfOuterGroup()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"382=2\u0001375=1\u0001337=2\u0001603=32\u0001375=3\u0001437=4\u0001" +
				"454=1\u0001455=5\u0001456=6\u0001" + "232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001" +
				"518=1\u0001519=10\u0001520=11\u0001" + "555=2\u0001600=12\u0001603=13\u0001" +
				"604=1\u0001605=14\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001" +
				"539=2\u0001524=23\u0001524=24\u0001525=25\u000110=124\u0001";
			var msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());

			ClassicAssert.Throws<UnexpectedGroupTagException>(() => RawFixUtil.IndexRepeatingGroup(msg, true),
				new UnexpectedGroupTagException(603, _version, _msgType).Message);
		}

		[Test]
		public virtual void TagFromOtherGroupAtEndOfInnerGroup()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"382=2\u0001375=1\u0001337=2\u0001375=3\u0001437=4\u0001" + "454=1\u0001455=5\u0001456=6\u0001" +
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001" + "518=1\u0001519=10\u0001520=11\u0001" +
				"555=2\u0001600=12\u0001603=13\u0001" +
				"604=1\u0001605=14\u0001455=5\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001" +
				"539=2\u0001524=23\u0001524=24\u0001525=25\u000110=124\u0001";
			var msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());

			ClassicAssert.Throws<InvalidLeadingTagValueException>(() => RawFixUtil.IndexRepeatingGroup(msg, true),
				new InvalidLeadingTagValueException(555, true, _version, _msgType).Message);
		}

		[Test]
		public virtual void TagFromOtherGroupAtEndOfOuterGroup()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u0001" +
				"382=2\u0001375=1\u0001337=2\u0001375=3\u0001437=4\u0001" +
				"454=1\u0001455=5\u0001456=6\u0001603=32\u0001" +
				"232=2\u0001233=7\u0001234=8\u0001233=9\u0001234=9\u0001" + "518=1\u0001519=10\u0001520=11\u0001" +
				"555=2\u0001600=12\u0001603=13\u0001" +
				"604=1\u0001605=14\u0001251=15\u0001539=2\u0001524=16\u0001525=17\u0001524=18\u0001" +
				"600=19\u0001603=20\u0001604=2\u0001605=21\u0001605=32\u0001251=22\u0001" +
				"539=2\u0001524=23\u0001524=24\u0001525=25\u000110=124\u0001";
			var msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());

			ClassicAssert.Throws<UnexpectedGroupTagException>(() => RawFixUtil.IndexRepeatingGroup(msg, true),
				new UnexpectedGroupTagException(603, _version, _msgType).Message);
		}
	}
}
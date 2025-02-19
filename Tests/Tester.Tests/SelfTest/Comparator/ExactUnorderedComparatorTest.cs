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

using Epam.FixAntenna.Tester.Comparator;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Tester.SelfTest
{
	[TestFixture]
	public class ExactUnorderedComparatorTest
	{
		internal ExactUnorderedComparator GenericUnorderedComparator;

		[SetUp]
		public virtual void SetUp()
		{
			GenericUnorderedComparator = new ExactUnorderedComparator();
			GenericUnorderedComparator.SetMessageSeparator("#");
		}

		[Test]
		public virtual void TestIsEquals()
		{
			string message = "8=FIX.4.2\x00019=115\x000135=A\x000149=ADAPTOR\x000156=123\x000134=1\x000150=SENDERSUBID\x0001142=SENDERLOCATIONID\x000157=TARGETSUBID\x000152=20040219-11:02:43.334\x000198=0\x0001108=0\x000110=207\x0001";
			string message2 = "8=FIX.4.2#9=115#35=A#49=ADAPTOR#56=123#34=1#50=SENDERSUBID#142=SENDERLOCATIONID#57=TARGETSUBID#52=20040219-11:02:43.334#98=0#108=0#10=207#";
			GenericUnorderedComparator.SetEtalonMessage(message2);
			ClassicAssert.AreEqual(true, GenericUnorderedComparator.IsMessageOk(message));
		}

		[Test]
		public virtual void TestIsEqualsTagSwitched()
		{
			string message = "8=FIX.4.2\x00019=115\x000135=A\x000149=ADAPTOR\x000156=123\x000134=1\x000150=SENDERSUBID\x0001142=SENDERLOCATIONID\x000157=TARGETSUBID\x000152=20040219-11:02:43.334\x000198=0\x0001108=0\x000110=207\x0001";
			string message2 = "8=FIX.4.2#9=115#35=A#56=123#49=ADAPTOR#34=1#50=SENDERSUBID#142=SENDERLOCATIONID#57=TARGETSUBID#52=20040219-11:02:43.334#98=0#108=0#10=207#";
			GenericUnorderedComparator.SetEtalonMessage(message2);
			ClassicAssert.AreEqual(true, GenericUnorderedComparator.IsMessageOk(message));
		}

		[Test]
		public virtual void TestIsNotEqualsByTagCount()
		{
			string message = "8=FIX.4.2\x00019=115\x000135=A\x000149=ADAPTOR\x000156=123\x000134=1\x000150=SENDERSUBID\x0001142=SENDERLOCATIONID\x000157=TARGETSUBID\x000152=20040219-11:02:43.334\x000198=0\x0001108=0\x000110=207\x0001";
			string message2 = "8=FIX.4.2#35=A#49=ADAPTOR#56=123#34=1#50=SENDERSUBID#142=SENDERLOCATIONID#57=TARGETSUBID#52=20040219-11:02:43.334#98=0#108=0#10=207#";
			GenericUnorderedComparator.SetEtalonMessage(message2);
			ClassicAssert.AreEqual(false, GenericUnorderedComparator.IsMessageOk(message));
		}

		[Test]
		public virtual void TestIsNotEqualsByValues()
		{
			string message = "8=FIX.4.2\x00019=115\x000135=A\x000149=ADAPTOR\x000156=123\x000134=1\x000150=SENDERSUBID\x0001142=SENDERLOCATIONID\x000157=TARGETSUBID\x000152=20040219-11:02:43.334\x000198=0\x0001108=0\x000110=207\x0001";
			string message2 = "8=FIX.4.2#19=116#35=A#49=ADAPTOR#56=123#34=1#50=SENDERSUBID#142=SENDERLOCATIONID#57=TARGETSUBID#52=20040219-11:02:43.334#98=0#108=0#10=207#";
			GenericUnorderedComparator.SetEtalonMessage(message2);
			ClassicAssert.AreEqual(false, GenericUnorderedComparator.IsMessageOk(message));
		}
	}
}
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
using Epam.FixAntenna.NetCore.Validation.Validators.Condition;
using Epam.FixAntenna.NetCore.Validation.Validators.Condition.Operators;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Validation.Tests.Engine.Validators.Condition
{
	[TestFixture]
	public class ConditionValidateParserTest
	{
		[Test]
		public virtual void ParseExistCondition()
		{
			var parser = new ConditionValidateParser("existtags(T$91)");
			var rootCondition = parser.GetCondition();
			ClassicAssert.IsTrue(rootCondition is ExistTagsValidateOperator,
				"Condition should have type ExistTagsOperator, but has " + rootCondition.GetType().FullName);
			var existTagsOperator = (ExistTagsValidateOperator)rootCondition;
			ClassicAssert.AreEqual(existTagsOperator.GetTags().Count, 1, "Only 1 tag required");
			ClassicAssert.AreEqual(existTagsOperator.GetTags()[0], 91, "Required tag 91");
		}

		[Test]
		public virtual void ParseExistCondition2()
		{
			var parser = new ConditionValidateParser("existtags(T$91, T$92,T$93)");
			var rootCondition = parser.GetCondition();
			ClassicAssert.IsTrue(rootCondition is ExistTagsValidateOperator,
				"Condition should have type ExistTagsOperator, but has " + rootCondition.GetType().FullName);
			var existTagsOperator = (ExistTagsValidateOperator)rootCondition;
			ClassicAssert.AreEqual(existTagsOperator.GetTags().Count, 3, "3 tags required");
			ClassicAssert.AreEqual(existTagsOperator.GetTags()[0], 91, "Required tag 91");
			ClassicAssert.AreEqual(existTagsOperator.GetTags()[1], 92, "Required tag 92");
			ClassicAssert.AreEqual(existTagsOperator.GetTags()[2], 93, "Required tag 93");
		}

		[Test]
		public virtual void ParseExistCondition3()
		{
			var parser = new ConditionValidateParser("T$91 ");

			var rootCondition = parser.GetCondition();
			ClassicAssert.IsTrue(rootCondition is ExistTagsValidateOperator,
				"Condition should have type ExistTagsOperator, but has " + rootCondition.GetType().FullName);
			var existTagsOperator = (ExistTagsValidateOperator)rootCondition;
			ClassicAssert.AreEqual(existTagsOperator.GetTags().Count, 1, "Only 1 tag required");
			ClassicAssert.AreEqual(existTagsOperator.GetTags()[0], 91, "Required tag 91");
		}

		[Test]
		public virtual void ParseExistGroupCondition()
		{
			var parser = new ConditionValidateParser("existtags(G$199(T$104))");

			var rootCondition = parser.GetCondition();
			ClassicAssert.IsTrue(rootCondition is ExistTagsValidateOperator,
				"Condition should have type ExistTagsOperator, but has " + rootCondition.GetType().FullName);
			var existTagsOperator = (ExistTagsValidateOperator)rootCondition;
			ClassicAssert.AreEqual(existTagsOperator.GetTags().Count, 2, "Only 1 tag required");
			ClassicAssert.AreEqual(existTagsOperator.GetTags()[0], 199, "Required tag 199");
			ClassicAssert.AreEqual(existTagsOperator.GetTags()[1], 104, "Required tag 104");
		}

		[Test]
		public virtual void ParseExistGroupCondition1And()
		{
			var parser = new ConditionValidateParser("existtags(G$199(T$104)) and T$91");

			var rootCondition = parser.GetCondition();
			ClassicAssert.IsTrue(rootCondition is AndValidateOperator,
				"Condition should have type ExistTagsOperator, but has " + rootCondition.GetType().FullName);

			var andValidateOperator = (AndValidateOperator)rootCondition;

			//Operator 1 from AND condition
			ClassicAssert.IsTrue(andValidateOperator.GetOperand1() is ExistTagsValidateOperator,
				"Condition should have type ExistTagsOperator, but has " +
				andValidateOperator.GetOperand1().GetType().FullName);

			var existTagsOperator1 = (ExistTagsValidateOperator)andValidateOperator.GetOperand1();
			ClassicAssert.AreEqual(existTagsOperator1.GetTags().Count, 2, "Only 2 tags required");
			ClassicAssert.AreEqual(existTagsOperator1.GetTags()[0], 199, "Required tag 199");
			ClassicAssert.AreEqual(existTagsOperator1.GetTags()[1], 104, "Required tag 104");

			//Operator 2 from AND condition
			ClassicAssert.IsTrue(andValidateOperator.GetOperand2() is ExistTagsValidateOperator,
				"Condition should have type ExistTagsOperator, but has " +
				andValidateOperator.GetOperand1().GetType().FullName);

			var existTagsOperator2 = (ExistTagsValidateOperator)andValidateOperator.GetOperand2();
			ClassicAssert.AreEqual(existTagsOperator2.GetTags().Count, 1, "Only 1 tag required");
			ClassicAssert.AreEqual(existTagsOperator2.GetTags()[0], 91, "Required tag 91");
		}

		[Test]
		public virtual void ParseExistGroupCondition2And()
		{
			var parser = new ConditionValidateParser("existtags(G$199(T$104)) and existtags(T$91)");

			var rootCondition = parser.GetCondition();
			ClassicAssert.IsTrue(rootCondition is AndValidateOperator,
				"Condition should have type ExistTagsOperator, but has " + rootCondition.GetType().FullName);

			var andValidateOperator = (AndValidateOperator)rootCondition;

			//Operator 1 from AND condition
			ClassicAssert.IsTrue(andValidateOperator.GetOperand1() is ExistTagsValidateOperator,
				"Condition should have type ExistTagsOperator, but has " +
				andValidateOperator.GetOperand1().GetType().FullName);

			var existTagsOperator1 = (ExistTagsValidateOperator)andValidateOperator.GetOperand1();
			ClassicAssert.AreEqual(existTagsOperator1.GetTags().Count, 2, "Only 2 tags required");
			ClassicAssert.AreEqual(existTagsOperator1.GetTags()[0], 199, "Required tag 199");
			ClassicAssert.AreEqual(existTagsOperator1.GetTags()[1], 104, "Required tag 104");

			//Operator 2 from AND condition
			ClassicAssert.IsTrue(andValidateOperator.GetOperand2() is ExistTagsValidateOperator,
				"Condition should have type ExistTagsOperator, but has " +
				andValidateOperator.GetOperand1().GetType().FullName);

			var existTagsOperator2 = (ExistTagsValidateOperator)andValidateOperator.GetOperand2();
			ClassicAssert.AreEqual(existTagsOperator2.GetTags().Count, 1, "Only 1 tag required");
			ClassicAssert.AreEqual(existTagsOperator2.GetTags()[0], 91, "Required tag 91");
		}

		[Test]
		public virtual void ParseExistGroupCondition2Or()
		{
			var parser =
				new ConditionValidateParser(
					"existtags(G$136(T$137)) or existtags(G$136(T$138)) or existtags(G$136(T$139))");

			var rootCondition = parser.GetCondition();
			ClassicAssert.IsTrue(rootCondition is OrValidateOperator,
				"Condition should have type ExistTagsOperator, but has " + rootCondition.GetType().FullName);
			var orOperator = (OrValidateOperator)rootCondition;

			ClassicAssert.IsTrue(orOperator.GetOperand1() is OrValidateOperator,
				"Condition should have type OrValidateOperator, but has " +
				orOperator.GetOperand1().GetType().FullName);

			var orOperator1 = (OrValidateOperator)orOperator.GetOperand1();

			ClassicAssert.IsTrue(orOperator1.GetOperand1() is ExistTagsValidateOperator,
				"Condition should have type ExistTagsOperator, but has " +
				orOperator1.GetOperand1().GetType().FullName);

			var existTagsOperator1 = (ExistTagsValidateOperator)orOperator1.GetOperand1();
			ClassicAssert.AreEqual(existTagsOperator1.GetTags().Count, 2, "Only 2 tags required");
			ClassicAssert.AreEqual(existTagsOperator1.GetTags()[0], 136, "Required tag 136");
			ClassicAssert.AreEqual(existTagsOperator1.GetTags()[1], 137, "Required tag 137");

			var existTagsOperator2 = (ExistTagsValidateOperator)orOperator1.GetOperand2();
			ClassicAssert.AreEqual(existTagsOperator2.GetTags().Count, 2, "Only 2 tags required");
			ClassicAssert.AreEqual(existTagsOperator2.GetTags()[0], 136, "Required tag 136");
			ClassicAssert.AreEqual(existTagsOperator2.GetTags()[1], 138, "Required tag 138");

			//Operator 2 from or condition
			var orOperator2 = (ExistTagsValidateOperator)orOperator.GetOperand2();

			ClassicAssert.AreEqual(orOperator2.GetTags().Count, 2, "Only 2 tags required");
			ClassicAssert.AreEqual(orOperator2.GetTags()[0], 136, "Required tag 136");
			ClassicAssert.AreEqual(orOperator2.GetTags()[1], 139, "Required tag 139");
		}

		[Test]
		public virtual void TestCascadeOrOperator()
		{
			var parser = new ConditionValidateParser("T$146>0 or T$167='FUT' or T$167='OPT'");

			var rootCondition = parser.GetCondition();

			ClassicAssert.IsTrue(rootCondition is OrValidateOperator,
				"Condition should have type OrValidateOperator, but has " + rootCondition.GetType().FullName);
			var orOperator = (OrValidateOperator)rootCondition;

			ClassicAssert.IsTrue(orOperator.GetOperand1() is OrValidateOperator,
				"Condition should have type OrValidateOperator, but has " +
				orOperator.GetOperand1().GetType().FullName);

			var greatThanValidateOperator =
				(GreatThanValidateOperator)((OrValidateOperator)orOperator.GetOperand1()).GetOperand1();
			ClassicAssert.AreEqual(0, greatThanValidateOperator.GetValue());
			ClassicAssert.AreEqual(146, greatThanValidateOperator.GetTag());

			var eqValidateOperator = (EqValidateOperator)((OrValidateOperator)orOperator.GetOperand1()).GetOperand2();
			ClassicAssert.AreEqual(eqValidateOperator.GetValue(), "FUT");
			ClassicAssert.AreEqual(167, eqValidateOperator.GetTag());

			eqValidateOperator = (EqValidateOperator)orOperator.GetOperand2();
			ClassicAssert.AreEqual(eqValidateOperator.GetValue(), "OPT");
			ClassicAssert.AreEqual(167, eqValidateOperator.GetTag());
		}

		[Test]
		public virtual void TestParseAndNotExistCondition()
		{
			var parser = new ConditionValidateParser("T$59='6' and (not existtags(T$126))");

			var rootCondition = parser.GetCondition();

			ClassicAssert.IsTrue(rootCondition is AndValidateOperator,
				"Condition should have type NOTOperator, but has " + rootCondition.GetType().FullName);
			var andOperator = (AndValidateOperator)rootCondition;

			ClassicAssert.IsTrue(andOperator.GetOperand1() is EqValidateOperator,
				"Condition should have type EQOperator, but has " + andOperator.GetOperand1().GetType().FullName);

			var eqOperator = (NotValidateOperator)andOperator.GetOperand2();
			ClassicAssert.AreEqual(eqOperator.GetTags()[0], 126, "Required tag 126");
		}

		[Test]
		public virtual void TestParseNotEqualsCondition()
		{
			var parser = new ConditionValidateParser("T$20 != '3'");

			var rootCondition = parser.GetCondition();

			ClassicAssert.IsTrue(rootCondition is NotValidateOperator,
				"Condition should have type NOTOperator, but has " + rootCondition.GetType().FullName);
			var notOperator = (NotValidateOperator)rootCondition;

			ClassicAssert.IsTrue(notOperator.GetOperand() is EqValidateOperator,
				"Condition should have type EQOperator, but has " + notOperator.GetOperand().GetType().FullName);

			var eqOperator = (EqValidateOperator)notOperator.GetOperand();
			ClassicAssert.AreEqual(eqOperator.GetTag(), 20, "Required tag 91");
			ClassicAssert.AreEqual(eqOperator.GetValue(), "3", "Required value '3'");
		}

		[Test]
		public virtual void TestParseNotEqualsCondition2()
		{
			var parser = new ConditionValidateParser("T$150!='8'");

			var rootCondition = parser.GetCondition();

			var msg = new FixMessage();
			msg.AddTag(150, "8");
			ClassicAssert.IsFalse(rootCondition.IsRequired(msg));

			msg.Set(150, "7");
			ClassicAssert.IsTrue(rootCondition.IsRequired(msg));
		}
	}
}
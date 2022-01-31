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

namespace Epam.FixAntenna.Message.Tests
{
	[TestFixture]
	internal class TagValueTest
	{
		private TagValue _value1;
		private TagValue _value1Same;
		private TagValue _value2;

		[SetUp]
		public virtual void Before()
		{
			var valueStr1 = "abc123".AsByteArray();
			var valueStr1Same = "_abc123".AsByteArray();
			var valueStr2 = "321cba".AsByteArray();
			_value1 = new TagValue(35, valueStr1, 1, 2);
			_value1Same = new TagValue(35, valueStr1Same, 2, 2);
			_value2 = new TagValue(35, valueStr2, 1, 2);
		}

		[Test]
		public virtual void TestEquals()
		{
			Assert.AreEqual(_value1, _value1Same);
		}

		[Test]
		public virtual void TestHashCodeEquals()
		{
			Assert.AreEqual(_value1.GetHashCode(), _value1Same.GetHashCode());
		}

		[Test]
		public virtual void TestHashCodeNotEquals()
		{
			Assert.IsFalse(_value1.GetHashCode() == _value2.GetHashCode());
		}

		[Test]
		public virtual void TestNotEquals()
		{
			Assert.IsFalse(_value1.Equals((object)_value2), "Two object is different. Equals method must return false");
		}

		#region Migrated from FixFieldTest
		[Test]
		public virtual void TestDouble()
		{
			var f = new TagValue(51, 33.123456, 3);
			Assert.That(f.ToString(), Is.EqualTo("51=33.123"));
			Assert.AreEqual(33.123, f.DoubleValue, 0.0001);
		}

		[Test]
		public virtual void TestEqualsEx()
		{
			Assert.That(new TagValue(0, ""), Is.EqualTo(new TagValue(0, "")));
			Assert.That(new TagValue(1, ""), Is.EqualTo(new TagValue(1, "")));
			Assert.That(new TagValue(10, ""), Is.EqualTo(new TagValue(10, "")));
			Assert.That(new TagValue(9999, ""), Is.EqualTo(new TagValue(9999, "")));
		}

		[Test]
		public virtual void TestExactDouble()
		{
			var f = new TagValue(51, "33.123");
			Assert.That(f.ToString(), Is.EqualTo("51=33.123"));
			Assert.AreEqual(33.123, f.DoubleValue, 0);
		}

		[Test]
		public virtual void TestHasCode()
		{
			Assert.AreEqual(new TagValue(0, "").GetHashCode(), new TagValue(0, "").GetHashCode());
			Assert.AreEqual(new TagValue(1, "").GetHashCode(), new TagValue(1, "").GetHashCode());
			Assert.AreEqual(new TagValue(10, "001").GetHashCode(), new TagValue(10, "001").GetHashCode());
			Assert.AreEqual(new TagValue(9999, "999").GetHashCode(), new TagValue(9999, "999").GetHashCode());
			Assert.AreNotEqual(new TagValue(1, "").GetHashCode(), new TagValue(0, "").GetHashCode());
		}

		[Test]
		public virtual void TestSize()
		{
			Assert.That(new TagValue(0, "").FullSize, Is.EqualTo(2));
			Assert.That(new TagValue(1, "").FullSize, Is.EqualTo(2));
			Assert.That(new TagValue(10, "").FullSize, Is.EqualTo(3));
			Assert.That(new TagValue(9999, "9999").FullSize, Is.EqualTo(9));
		}

		[Test]
		public virtual void TestToString()
		{
			Assert.That(new TagValue(0, "").ToString(), Is.EqualTo("0="));
			Assert.That(new TagValue(1, "").ToString(), Is.EqualTo("1="));
			Assert.That(new TagValue(10, "").ToString(), Is.EqualTo("10="));
			Assert.That(new TagValue(9999, "9999").ToString(), Is.EqualTo("9999=9999"));
		}
		#endregion
	}
}
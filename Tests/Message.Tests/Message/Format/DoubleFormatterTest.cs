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
using Epam.FixAntenna.NetCore.Message.Format;
using NUnit.Framework;

namespace Epam.FixAntenna.Message.Tests.Format
{
	[TestFixture]
	internal class DoubleFormatterTest
	{
		internal const string DecimalPattern = "0.################";
		public const int Precision = 16;
		internal DoubleFormatter FixFormatter = new DoubleFormatter();

		private string FixFormat(double v)
		{
			var b = new byte[100];
			var len = DoubleFormatter.Format(v, 16, b, 0);
			return StringHelper.NewString(b, 0, len);
		}

		private int FixLength(double v)
		{
			return DoubleFormatter.GetFormatLength(v, 16);
		}

		private string FixFormatWithPadding(double v)
		{
			var b = new byte[16];
			DoubleFormatter.FormatWithPadding(v, 16, 16, b, 0);
			return StringHelper.NewString(b, 0, 16);
		}

		private void AssertFormattedLength(double v, int expectedLength)
		{
			var actualLength = DoubleFormatter.GetFormatLength(v, 16);
			Assert.AreEqual(expectedLength, actualLength, "Wrong length for " + v);
		}

		private static string ConvertDoubleToString(double v)
		{
			return v.ToString(DecimalPattern, FixTypes.UsFormatSymbols);
		}

		[Test]
		public virtual void TestCleanFormatted()
		{
			byte[] buff = { (byte)'1', (byte)'0', (byte)'.', (byte)'9', (byte)'0', (byte)'0' };
			var resLen = DoubleFormatter.CleanFormatted(buff, 0, 6);
			Assert.AreEqual(4, resLen);
			Assert.That(new[] { (byte)'1', (byte)'0', (byte)'.', (byte)'9', (byte)' ', (byte)' ' },
				Is.EquivalentTo(buff));
		}

		[Test]
		public virtual void TestCleanFormattedToInt()
		{
			byte[] buff = { (byte)'1', (byte)'0', (byte)'.', (byte)'0', (byte)'0', (byte)'0' };
			var resLen = DoubleFormatter.CleanFormatted(buff, 0, 6);
			Assert.AreEqual(2, resLen);
			Assert.That(new[] { (byte)'1', (byte)'0', (byte)' ', (byte)' ', (byte)' ', (byte)' ' },
				Is.EquivalentTo(buff));
		}

		[Test]
		public virtual void TestFormatAlmostZero()
		{
			var v = 0.00000000000000001d;
			var str1 = ConvertDoubleToString(v);
			var str2 = FixFormat(v);
			Assert.AreEqual(str1, str2);
			AssertFormattedLength(v, 18);
		}

		[Test]
		public virtual void TestFormatBigestLong()
		{
			var v = 999999999999999d;
			var str1 = ConvertDoubleToString(v);
			var str2 = FixFormat(v);
			Assert.AreEqual(str1, str2);
			AssertFormattedLength(v, 15);
		}

		[Test]
		public virtual void TestFormatDouble()
		{
			var v = -2.34d;
			var str1 = ConvertDoubleToString(v);
			var str2 = FixFormat(v);
			Assert.AreEqual(str1, str2);
		}

		[Test]
		public virtual void TestFormatSmall()
		{
			var v = 0.0000000000000001d;
			var str1 = ConvertDoubleToString(v);
			var str2 = FixFormat(v);
			Assert.AreEqual(str1, str2);
			AssertFormattedLength(v, 18);
		}

		[Test]
		public virtual void TestFormatSmallestLong()
		{
			var v = -999999999999999d;
			var str1 = ConvertDoubleToString(v);
			var str2 = FixFormat(v);
			Assert.AreEqual(str1, str2);
			AssertFormattedLength(v, 16);
		}

		[Test]
		public virtual void TestFormatWithPadding()
		{
			var v = 12.345d;
			//System.out.printf("%.16f", v);
			var str1 = "000000000012.345";
			var str2 = FixFormatWithPadding(v);
			Assert.AreEqual(str1, str2);
		}

		[Test]
		public virtual void TestFormatWithRound()
		{
			var v = 0.00000000000000015d;
			//System.out.printf("%.16f", v);

			Assert.AreEqual("0.0000000000000002", FixFormat(v));
			AssertFormattedLength(v, 18);
		}

		[Test]
		public virtual void TestFormatWithRoundAndTrunk()
		{
			var v = 0.00000000000000999d;
			//System.out.printf("%.16f", v);
			var str1 = ConvertDoubleToString(v);
			var str2 = FixFormat(v);
			Assert.AreEqual(str1, str2);
			//TODO: here should be 16 in general
			AssertFormattedLength(v, 18);
		}

		[Test]
		public virtual void TestFormatZero()
		{
			double v = 0;
			var str1 = ConvertDoubleToString(v);
			var str2 = FixFormat(v);
			Assert.AreEqual(str1, str2);
			AssertFormattedLength(v, 1);
		}

		[Test]
		public virtual void TestLengthBigestLong()
		{
			var v = 999999999999999d;
			var length1 = ConvertDoubleToString(v).Length;
			var length2 = FixLength(v);
			Assert.AreEqual(length1, length2);
		}

		[Test]
		public virtual void TestLengthDouble()
		{
			var v = -2.34d;
			var length1 = ConvertDoubleToString(v).Length;
			var length2 = FixLength(v);
			Assert.AreEqual(length1, length2);
		}

		[Test]
		public virtual void TestLengthSmall()
		{
			var v = 0.0000000000000001d;
			var length1 = ConvertDoubleToString(v).Length;
			var length2 = FixLength(v);
			Assert.AreEqual(length1, length2);
		}

		[Test]
		public virtual void TestLengthSmallestLong()
		{
			var v = -999999999999999d;
			var length1 = ConvertDoubleToString(v).Length;
			var length2 = FixLength(v);
			Assert.AreEqual(length1, length2);
		}

		[Test]
		public virtual void TestLengthWithRound()
		{
			var v = 0.00000000000000015d;
			var length1 = ConvertDoubleToString(v).Length;
			var length2 = FixLength(v);
			Assert.AreEqual(length1, length2);
		}

		[Test]
		public virtual void TestLengthZero()
		{
			double v = 0;
			var length1 = ConvertDoubleToString(v).Length;
			var length2 = FixLength(v);
			Assert.AreEqual(length1, length2);
		}

		[Test]
		public virtual void TestRoundUpFormatted()
		{
			byte[] buff = { (byte)'1', (byte)'0', (byte)'.', (byte)'1', (byte)'2' };
			var resLen = DoubleFormatter.RoundUpFormatted(buff, 0, 5);
			Assert.AreEqual(5, resLen);
			Assert.That(new[] { (byte)'1', (byte)'0', (byte)'.', (byte)'1', (byte)'3' }, Is.EquivalentTo(buff));
		}

		[Test]
		public virtual void TestRoundUpFormattedWithOverflow()
		{
			byte[] buff = { (byte)'9', (byte)'9', (byte)'.', (byte)'9', (byte)'9' };
			var resLen = DoubleFormatter.RoundUpFormatted(buff, 0, 5);
			Assert.AreEqual(3, resLen);
			Assert.That(new[] { (byte)'1', (byte)'0', (byte)'0', (byte)' ', (byte)' ' }, Is.EquivalentTo(buff));
		}

		[Test]
		public virtual void TestRoundUpFormattedWithTrunc()
		{
			byte[] buff = { (byte)'1', (byte)'0', (byte)'.', (byte)'1', (byte)'9' };
			var resLen = DoubleFormatter.RoundUpFormatted(buff, 0, 5);
			Assert.AreEqual(4, resLen);
			Assert.That(new[] { (byte)'1', (byte)'0', (byte)'.', (byte)'2', (byte)' ' }, Is.EquivalentTo(buff));
		}

		[Test]
		public virtual void TestRoundUpFormattedWithTruncToInt()
		{
			byte[] buff = { (byte)'1', (byte)'9', (byte)'.', (byte)'9', (byte)'9' };
			var resLen = DoubleFormatter.RoundUpFormatted(buff, 0, 5);
			Assert.AreEqual(2, resLen);
			Assert.That(new[] { (byte)'2', (byte)'0', (byte)' ', (byte)' ', (byte)' ' }, Is.EquivalentTo(buff));
		}
	}
}
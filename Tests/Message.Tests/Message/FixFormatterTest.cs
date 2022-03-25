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

namespace Epam.FixAntenna.Message.Tests.Message
{
	[TestFixture]
	internal class FixFormatterTest
	{

		[Test]
		public void FormatIntTest()
		{
			var value = 12345L;
			var formatter = new FixFormatter();
			var formatted = StringHelper.NewString(formatter.FormatInt(value));

			Assert.That(formatted, Is.EqualTo("12345"));
		}

		[Test]
		public void FormatIntNegativeTest()
		{
			var value = -12345L;
			var formatter = new FixFormatter();
			var formatted = StringHelper.NewString(formatter.FormatInt(value));

			Assert.That(formatted, Is.EqualTo("-12345"));
		}

		[Test]
		public void FormatIntMinLengthTest()
		{
			var value = 12345L;
			var minLen = 10;
			var formatter = new FixFormatter();
			var formatted = StringHelper.NewString(formatter.FormatInt(value, 0, minLen));

			Assert.That(formatted, Is.EqualTo("0000012345"));
		}

		[Test]
		public void FormatIntNegativeMinLengthTest()
		{
			var value = -12345L;
			var minLen = 10;
			var formatter = new FixFormatter();
			var formatted = StringHelper.NewString(formatter.FormatInt(value, minLength:minLen));

			Assert.That(formatted, Is.EqualTo("-000012345"));
		}

		[Test]
		public void FormatIntNegativeMinLengthTest2()
		{
			var value = -123456L;
			var minLen = 7;
			var formatter = new FixFormatter();
			var formatted = StringHelper.NewString(formatter.FormatInt(value, minLength: minLen));

			Assert.That(formatted, Is.EqualTo("-123456"));
		}
	}
}

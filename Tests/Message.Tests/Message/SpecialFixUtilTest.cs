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
using Epam.FixAntenna.NetCore.Message.SpecialTags;
using NUnit.Framework;

namespace Epam.FixAntenna.Message.Tests.Message
{
	[TestFixture, Property("Story", "https://jira.epam.com/jira/browse/BBP-17118")]
	internal class SpecialFixUtilTest
	{
		private static string Original = "8=FIX.4.2\u000134=1\u000158=Hello World!!!\u0001554=pwd\u0001925=foo\u0001";
		private static string Expected = "8=FIX.4.2\u000134=1\u000158=Hello World!!!\u0001554=***\u0001925=***\u0001";
		private static string ExCustom = "8=FIX.4.2\u000134=1\u000158=**************\u0001554=***\u0001925=***\u0001";

		private static string OriginalWithRaw = "8=FIX.4.2\u000134=1\u000158=Hello World!!!\u0001554=pwd\u0001" +
																						"95=7\u000196=1234567\u0001925=foo\u0001";
		private static string ExpectedWithRaw = "8=FIX.4.2\u000134=1\u000158=Hello World!!!\u0001554=***\u0001" +
																						"95=7\u000196=*******\u0001925=***\u0001";

		[Test]
		public void GetMaskedStringTest()
		{
			var buffer = Original.AsByteArray();
			var message = SpecialFixUtil.GetMaskedString(buffer, 0, buffer.Length, null, null);
			Assert.That(message, Is.EqualTo(Expected));
		}

		[Test]
		public void GetCustomMaskedStringTest()
		{
			var maskedTags = CustomMaskedTags.Create("58");
			var buffer = Original.AsByteArray();
			var message = SpecialFixUtil.GetMaskedString(buffer, 0, buffer.Length, null, maskedTags);
			Assert.That(message, Is.EqualTo(ExCustom));
		}

		[Test]
		public void GetMaskedStringOffsetTest()
		{
			var buffer = (Original+Original).AsByteArray();
			var message = SpecialFixUtil.GetMaskedString(buffer, buffer.Length / 2, buffer.Length / 2, null, null);
			Assert.That(message, Is.EqualTo(Expected));
		}

		[Test]
		public void GetMaskedRawTagTest()
		{
			var maskedTags = CustomMaskedTags.Create("96");
			var buffer = OriginalWithRaw.AsByteArray();
			var message = SpecialFixUtil.GetMaskedString(buffer, 0, buffer.Length, null, maskedTags);
			Assert.That(message, Is.EqualTo(ExpectedWithRaw));
		}

		[Test, Category("Bug"), Property("JIRA", "https://jira.epam.com/jira/browse/BBP-23939")]
		public void RawDataLengthMaskedTest()
		{
			var maskedTags = CustomMaskedTags.Create("95");
			var buffer = OriginalWithRaw.AsByteArray();
			var message = SpecialFixUtil.GetMaskedString(buffer, 0, buffer.Length, null, maskedTags);
			Assert.That(message, Is.Not.Null);
		}
	}
}

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
using Epam.FixAntenna.NetCore.Validation.Utils;
using NUnit.Framework;

namespace Epam.FixAntenna.Fix.Validation.Engine.Utils
{
	[TestFixture]
	public class FixUtilDescriptionsTest
	{
		[SetUp]
		public virtual void SetUp()
		{
			FixUtilFactory.Instance.ClearResources();
			FixUtilFactory.SetLoadDescriptions(true);
		}

		[TearDown]
		public virtual void TearDown()
		{
			FixUtilFactory.Instance.ClearResources();
		}

		private const string HtmlTemplate =
			@"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<descr>
  <p>
          Identifies<msgref msgtype=""7"">Advertisement</msgref>message transaction type
        </p>
</descr>";

		private const string HtmlTemplate2 =
			@"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<descr>
  <p>
          Identifies <msgref msgtype=""7"">Advertisement</msgref> message transaction type
        </p>
</descr>";

		private void TestTagDescription(FixVersion fixVersion, int tagId, string html)
		{
			var util = BuildFixUtil(fixVersion);
			var field = util.GetFieldDefByTag(tagId);
			Assert.IsNotNull(field.Descr);
			Assert.AreEqual(html, FixUtil.DescrToHtmlStr(field.Descr));
		}

		private FixUtil BuildFixUtil(FixVersion fixVersion)
		{
			var fixVersionContainer = FixVersionContainer.GetFixVersionContainer(fixVersion);
			return FixUtilFactory.Instance.GetFixUtil(fixVersionContainer);
		}

		private void TestAllDescriptions(FixVersion fixVersion)
		{
			var util = BuildFixUtil(fixVersion);
			var fieldDefs = util.GetFieldDef();
			foreach (var fielddef in fieldDefs)
			{
				FixUtil.DescrToHtmlStr(fielddef.Descr);
			}
		}

		[Test]
		public virtual void TestAllDescriptions40()
		{
			TestAllDescriptions(FixVersion.Fix40);
		}

		[Test]
		public virtual void TestAllDescriptions41()
		{
			TestAllDescriptions(FixVersion.Fix41);
		}

		[Test]
		public virtual void TestAllDescriptions42()
		{
			TestAllDescriptions(FixVersion.Fix42);
		}

		[Test]
		public virtual void TestAllDescriptions43()
		{
			TestAllDescriptions(FixVersion.Fix43);
		}

		[Test]
		public virtual void TestAllDescriptions44()
		{
			TestAllDescriptions(FixVersion.Fix44);
		}

		[Test]
		public virtual void TestAllDescriptions50()
		{
			TestAllDescriptions(FixVersion.Fix50);
		}

		[Test]
		public virtual void TestAllDescriptions50Sp1()
		{
			TestAllDescriptions(FixVersion.Fix50Sp1);
		}

		[Test]
		public virtual void TestAllDescriptions50Sp2()
		{
			TestAllDescriptions(FixVersion.Fix50Sp2);
		}

		[Test]
		public virtual void TestTagDescription41()
		{
			TestTagDescription(FixVersion.Fix41, 5, HtmlTemplate);
		}

		[Test]
		public virtual void TestTagDescription42()
		{
			TestTagDescription(FixVersion.Fix42, 5, HtmlTemplate);
		}

		[Test]
		public virtual void TestTagDescription43()
		{
			TestTagDescription(FixVersion.Fix43, 5, HtmlTemplate);
		}

		[Test]
		public virtual void TestTagDescription44()
		{
			TestTagDescription(FixVersion.Fix44, 5, HtmlTemplate2);
		}

		[Test]
		public virtual void TestTagDescription50()
		{
			TestTagDescription(FixVersion.Fix50, 5, HtmlTemplate2);
		}

		[Test]
		public virtual void TestTagDescription50Sp1()
		{
			TestTagDescription(FixVersion.Fix50Sp1, 5, HtmlTemplate2);
		}

		[Test]
		public virtual void TestTagDescription50Sp2()
		{
			TestTagDescription(FixVersion.Fix50Sp2, 5, HtmlTemplate2);
		}
	}
}
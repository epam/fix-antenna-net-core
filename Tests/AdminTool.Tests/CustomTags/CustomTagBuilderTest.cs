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

using Epam.FixAntenna.AdminTool.Builder;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.AdminTool.Tests.CustomTags
{
	internal class CustomTagBuilderTest
	{
		[Test]
		public void TestCustomTagWithoutSenderAndTarget()
		{
			var sessionParameters = new SessionParameters();
			var fieldList = RawFixUtil.GetFixMessage("8=FIX.4.2\u00019=65\u000135=A\u000149=BLUEFIX\u000156=GNITEST\u000157=BLUEFIX\u000134=1\u000152=99990909-17:17:17\u000198=0\u0001383=10000\u00011004=H\u0001108=30\u0001553=bluefix\u0001554=bluefix\u000110=140\u0001"
				.AsByteArray());

			var fieldListCustom = CustomTagsBuilder
				.GetInstance()
				.GetCustomTags(sessionParameters, fieldList);

			sessionParameters.OutgoingLoginMessage.AddAll(fieldListCustom);

			ClassicAssert.IsNotNull(sessionParameters.SenderCompId, "sender must exist");
			ClassicAssert.IsNotNull(sessionParameters.TargetCompId, "target must exist");
			ClassicAssert.IsNotNull(sessionParameters.TargetSubId, "target must exist");

			// check custom tags
			ClassicAssert.IsNotNull(sessionParameters.OutgoingLoginMessage.GetTag(553), "user tag must exist");
			ClassicAssert.IsNotNull(sessionParameters.OutgoingLoginMessage.GetTag(554), "password tag must exist");
			ClassicAssert.IsNotNull(sessionParameters.OutgoingLoginMessage.GetTag(1004), "tag 1000 must exist");
			ClassicAssert.IsNotNull(sessionParameters.OutgoingLoginMessage.GetTag(98), "tag 98 must exist");
		}

		[Test]
		public void TestCustomTagWithSenderAndTarget()
		{
			var sessionParameters = new SessionParameters();
			sessionParameters.SenderCompId = "sender";
			sessionParameters.TargetCompId = "target";
			sessionParameters.TargetSubId = "subid";

			var fieldList = RawFixUtil.GetFixMessage("8=FIX.4.2\u00019=65\u000135=A\u000149=BLUEFIX\u000156=GNITEST\u000157=BLUEFIX\u000134=1\u000152=99990909-17:17:17\u000198=0\u0001383=10000\u00011004=H\u0001108=30\u0001553=bluefix\u0001554=bluefix\u000110=140\u0001"
				.AsByteArray());

			var fieldListCustom = CustomTagsBuilder
				.GetInstance()
				.GetCustomTags(sessionParameters, fieldList);

			sessionParameters.OutgoingLoginMessage
				.AddAll(fieldListCustom);

			ClassicAssert.AreEqual("sender", sessionParameters.SenderCompId);
			ClassicAssert.AreEqual("target", sessionParameters.TargetCompId);
			ClassicAssert.AreEqual("subid", sessionParameters.TargetSubId);

			// check custom tags
			ClassicAssert.IsNotNull(sessionParameters.OutgoingLoginMessage.GetTag(553), "user tag must exist");
			ClassicAssert.IsNotNull(sessionParameters.OutgoingLoginMessage.GetTag(554), "password tag must exist");
			ClassicAssert.IsNotNull(sessionParameters.OutgoingLoginMessage.GetTag(1004), "tag 1000 must exist");
			ClassicAssert.IsNotNull(sessionParameters.OutgoingLoginMessage.GetTag(98), "tag 98 must exist");
		}
	}
}
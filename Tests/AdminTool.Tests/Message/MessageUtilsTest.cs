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

using Epam.FixAntenna.AdminTool.Tests.Util;
using Epam.FixAntenna.Fixicc.Message;
using Epam.FixAntenna.NetCore.Helpers;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.AdminTool.Tests.Message
{
	internal class MessageUtilsTest
	{
		[SetUp]
		public void Before()
		{
			LogAppender.Clear();
		}

		[TearDown]
		public void After()
		{
			LogAppender.ClassicAssertIfErrorExist();
		}

		[Test]
		public void TestXMLMarshaling()
		{
			var deleteAllBean = new DeleteAll();
			var messageStr = MessageUtils.ToXml(deleteAllBean);
			MessageUtils.FromXml(messageStr);
		}

		[Test]
		public void TestXMLMarshalingWithInvalidChars()
		{
			var etalonMessage = "233=45\u000120=23";
			var sendMessage = new SendMessage();
			sendMessage.Message = etalonMessage.ReplaceAll("\u0001", "&#01;").AsByteArray();
			var messageStr = MessageUtils.ToXml(sendMessage);
			var receiveMessage = (SendMessage) MessageUtils.FromXml(messageStr);

			var transformMessage = StringHelper
				.NewString(receiveMessage.Message)
				.ReplaceAll("&#01;", "\u0001");

			ClassicAssert.AreEqual(etalonMessage, transformMessage);
		}
	}
}
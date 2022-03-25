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

using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Storage.File;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;

using NUnit.Framework;

using System;
using System.IO;

namespace Epam.FixAntenna.Core.Tests.FixEngine.Storage.File
{
	[Property("Story", "https://jira.epam.com/jira/browse/BBP-17110")]
	internal class MaskedFieldsStorageTest : AbstractMessageStorageTest
	{
		private static string original = "8=FIX.4.2\u000134=1\u000158=Hello World!!!\u0001554=pwd\u0001925=foo\u0001";
		private static string expected = "8=FIX.4.2\u000134=1\u000158=**************\u0001554=***\u0001925=***\u0001";

		private static string originalRaw = "8=FIX.4.2\u000134=1\u000158=Hello World!!!\u0001" +
																				"554=pwd\u000195=7\u000196=1234567\u0001925=foo\u0001";
		private static string expectedRaw = "8=FIX.4.2\u000134=1\u000158=**************\u0001" +
																				"554=***\u000195=7\u000196=*******\u0001925=***\u0001";

		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			Config.GlobalConfiguration.SetProperty(Config.MaskedTags, "58,96");
		}

		public override AbstractFileMessageStorage GetInstanceMessageStorage()
		{
			return new FlatFileMessageStorage(ConfigurationAdapter.Configuration);
		}

		[Test, Property("Implements", "https://jira.epam.com/jira/browse/BBP-17110")]
		public void WriteMaskedFieldTest()
		{
			UpMessageStorage();
			var message = RawFixUtil.GetFixMessage(original.AsByteArray());
			MessageStorage.AppendMessage(message.AsByteArray());
			var strMsg = RetrieveMessage(0);
			strMsg = strMsg.Substring(strMsg.IndexOf("8=", StringComparison.InvariantCulture)); // trim timestamp
			Assert.That(strMsg, Is.EqualTo(expected));
		}

		[Test, Property("Implements", "https://jira.epam.com/jira/browse/BBP-17110")]
		public void WriteMaskedRawFieldTest()
		{
			UpMessageStorage();
			var message = RawFixUtil.GetFixMessage(originalRaw.AsByteArray());
			MessageStorage.AppendMessage(message.AsByteArray());
			var strMsg = RetrieveMessage(0);
			strMsg = strMsg.Substring(strMsg.IndexOf("8=", StringComparison.InvariantCulture)); // trim timestamp
			Assert.That(strMsg, Is.EqualTo(expectedRaw));
		}

		[Test, Property("Implements", "https://jira.epam.com/jira/browse/BBP-17110")]
		public void WriteMaskedSecondMessageTest()
		{
			UpMessageStorage();
			WriteDummyMessageToStorage();
			var message = RawFixUtil.GetFixMessage(original.AsByteArray());
			MessageStorage.AppendMessage(message.AsByteArray());
			var strMsg = RetrieveMessage(1);
			strMsg = strMsg.Substring(strMsg.IndexOf("8=", StringComparison.InvariantCulture)); // trim timestamp
			Assert.That(strMsg, Is.EqualTo(expected));
		}

		private string RetrieveMessage(int msgNum)
		{
			var num = 0;
			var storageFileName = FileNameHelper.GetStorageFileName(ConfigurationAdapter.StorageDirectory);
			using (var stream = new FileStream(storageFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				using (var br = new StreamReader(stream))
				{
					do
					{
						var actualMsg = br.ReadLine();

						if (actualMsg == null)
							break;

						if (num == msgNum)
						{
							return actualMsg;
						}

						num++;
					} while (num <= msgNum);
				}
			}

			return string.Empty;
		}
	}
}

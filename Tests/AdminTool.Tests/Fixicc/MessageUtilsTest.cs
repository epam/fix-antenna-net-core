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

using System;
using System.IO;
using System.Reflection;
using Epam.FixAntenna.Fixicc.Message;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.AdminTool.Tests.Fixicc
{
	internal class MessageUtilsTest
	{
		[Test]
		public void TestImportConfig()
		{
			var config = Array.Empty<byte>();
			var assembly = Assembly.GetExecutingAssembly();
			var res = assembly.GetManifestResourceStream("Epam.FixAntenna.AdminTool.Tests.engine.properties");
			using (var ms = new MemoryStream())
			{
				res.CopyTo(ms);
				config = ms.ToArray();
			}

			var importConfig = new ImportConfig
			{
				Config = config
			};
			var messageStr = MessageUtils.ToXml(importConfig);
			importConfig = (ImportConfig)MessageUtils.FromXml(messageStr);
			ClassicAssert.That(importConfig.Config, Is.EqualTo(config), "Config file was changed during roundtrip conversion");
		}

		[Test]
		public virtual void TestDTDReadFiles()
		{
			var data =
				"<?xml version=\"1.0\" encoding=\"UTF- 8\"?>\n" +
				"<!DOCTYPE foo [<!ELEMENT foo ANY><!ENTITY xxx SYSTEM \"file:///tmp/asdf\">]>\n" +
				"<ServerStatus RequestID=\"1\"><ToAgent>true</ToAgent><SubscriptionRequestType>&xxx;</SubscriptionRequestType></ServerStatus>";
			ClassicAssert.Throws<InvalidOperationException>(() => MessageUtils.FromXml(data));
		}
	}
}
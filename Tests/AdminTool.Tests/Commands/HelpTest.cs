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
using Epam.FixAntenna.NetCore.Configuration;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.AdminTool.Tests.Commands
{
	internal class HelpTest : AdminToolHelper
	{
		[Test]
		public void ExecuteHelpCommand()
		{
			FixSession = FindAdminSession();

			var help = new Help();

			RequestID = GetNextRequest();
			help.RequestID = RequestID;

			var response = GetReponse(help);
			ClassicAssert.AreEqual(RequestID, response.RequestID);
			ClassicAssert.AreEqual(ResultCode.OperationSuccess.Code, response.ResultCode);
			var helpData = response.HelpData;
			ClassicAssert.IsNotNull(helpData);
			ClassicAssert.IsNotNull(helpData.SupportedRequest);
			ClassicAssert.IsTrue(helpData.SupportedRequest.Count > 0);
			ClassicAssert.IsTrue(helpData.SupportedRequest[0].ToString().Length > 1);
		}

		[Test]
		public void ExecuteHelpCommandWithoutRequestId()
		{
			FixSession = FindAdminSession();

			var help = new Help();

			RequestID = GetNextRequest();
			help.RequestID = RequestID;

			var response = GetReponse(help);
			ClassicAssert.AreEqual(RequestID, response.RequestID);
			ClassicAssert.AreEqual(ResultCode.OperationSuccess.Code, response.ResultCode);
			var helpData = response.HelpData;
			ClassicAssert.IsNotNull(helpData);
			ClassicAssert.IsTrue(helpData.SupportedRequest.Count > 1);
		}

		[Test]
		public void ExecuteHelpCommandWithInvalidProperty()
		{
			FixSession = FindAdminSession();
			FixSession.Parameters.Configuration
				.SetProperty(Config.AutostartAcceptorCommandPackage, "invalid.package");

			var help = new Help();

			RequestID = GetNextRequest();
			help.RequestID = RequestID;

			var response = GetReponse(help);

			// restore data
			FixSession.Parameters.Configuration
				.SetProperty(Config.AutostartAcceptorCommandPackage, "");

			ClassicAssert.IsNotNull(response, "no response");

			LogAppender.Clear();
		}

		[Test]
		public void ExecuteHelpCommandWithValidProperty()
		{
			FixSession = FindAdminSession();
			FixSession.Parameters.Configuration.SetProperty(Config.AutostartAcceptorCommandPackage, "Epam.FixAntenna.AdminTool.Commands,Epam.FixAntenna.AdminTool");
			var help = new Help();

			RequestID = GetNextRequest();
			help.RequestID = RequestID;

			var response = GetReponse(help);
			ClassicAssert.IsNotNull(response, "no response");
			ClassicAssert.AreEqual(RequestID, response.RequestID);
			ClassicAssert.AreEqual(ResultCode.OperationSuccess.Code, response.ResultCode);

			var helpData = response.HelpData;
			ClassicAssert.IsNotNull(helpData);
			ClassicAssert.IsTrue(helpData.SupportedRequest.Count > 1);
		}
	}
}
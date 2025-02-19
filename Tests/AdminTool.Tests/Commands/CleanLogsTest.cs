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
using Epam.FixAntenna.AdminTool.Commands;
using Epam.FixAntenna.Fixicc.Message;
using Epam.FixAntenna.NetCore.Configuration;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.AdminTool.Tests.Commands
{
	internal class CleanLogsTest : AdminToolHelper
	{
		[Test]
		public void ExecuteUnsupportedCommand()
		{
			FixSession = FindAdminSession();

			var command = new FixAntenna.Fixicc.Message.CleanLogs();

			RequestID = GetNextRequest();
			command.RequestID = RequestID;

			var response = GetReponse(command);
			ClassicAssert.AreEqual(RequestID, response.RequestID);
			ClassicAssert.AreEqual(ResultCode.OperationNotImplemented.Code, response.ResultCode);
		}

		[Test]
		public void ExecuteUsingInvalidExternalPackage()
		{
			FixSession = FindAdminSession();
			FixSession.Parameters.Configuration
				.SetProperty(Config.AutostartAcceptorCommandPackage, "invalid.package");

			// have to call this to set external package name
			AdminTool.NewFixSession(AdminSession);

			var command = new FixAntenna.Fixicc.Message.CleanLogs();

			RequestID = GetNextRequest();
			command.RequestID = RequestID;

			var response = GetReponse(command);
			ClassicAssert.AreEqual(RequestID, response.RequestID);
			ClassicAssert.AreEqual(ResultCode.OperationNotImplemented.Code, response.ResultCode);
		}

		[Test]
		public void ExecuteUsingValidExternalPackage()
		{
			FixSession = FindAdminSession();
			FixSession.Parameters.Configuration.SetProperty(Config.AutostartAcceptorCommandPackage, "Epam.FixAntenna.AdminTool.Tests.Commands,Epam.FixAntenna.AdminTool.Tests");

			// have to call this to set external package name
			AdminTool.NewFixSession(AdminSession);

			var command = new FixAntenna.Fixicc.Message.CleanLogs();

			RequestID = GetNextRequest();
			command.RequestID = RequestID;

			var response = GetReponse(command);
			ClassicAssert.IsNotNull(response, "no response");
			ClassicAssert.AreEqual(RequestID, response.RequestID);
			ClassicAssert.AreEqual(ResultCode.OperationSuccess.Code, response.ResultCode);
		}
	}

	internal class CleanLogs : Command
	{
		public override void Execute()
		{
			Log.Debug("[TEST] Execute CleanLog Command");
			try
			{
				var response = new Response
				{
					Description = "[TEST] CleanLog command from external test package completed."
				};
				SendResponseSuccess(response);
			}
			catch (Exception e)
			{
				Log.Error(e);
				SendError(e.Message);
			}
		}
	}
}
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
using Epam.FixAntenna.Fixicc.Message;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.AdminTool.Commands.Generic
{
	/// <summary>
	/// The TestRequest command.
	/// Send the test request to specified session.
	/// </summary>
	internal class TestRequest : Command
	{
		public override void Execute()
		{
			Log.Debug("Execute TestRequest Command");
			try
			{
				var testRequest = (FixAntenna.Fixicc.Message.TestRequest) Request;
				if (string.IsNullOrWhiteSpace(testRequest.SenderCompID))
				{
					SendInvalidArgument("Parameter SenderCompID is required");
					return;
				}
				if (string.IsNullOrWhiteSpace(testRequest.TargetCompID))
				{
					SendInvalidArgument("Parameter TargetCompID is required");
					return;
				}
				var id = new SessionId(testRequest.SenderCompID, testRequest.TargetCompID, testRequest.SessionQualifier);
				var session = GetFixSession(id);
				if (session == null)
				{
					SendUnknownSession(id);
					return;
				}
				if (testRequest.TestReqID == null)
				{
					SendInvalidArgument("Parameter TestReqID is required");
					return;
				}
				var fixMessage = new FixMessage();
				fixMessage.AddTag(112, testRequest.TestReqID);
				session.SendMessage("1", fixMessage);
				SendResponseSuccess(new Response());
			}
			catch (Exception e)
			{
				Log.Error(e);
				SendError(e.Message);
			}
		}
	}
}
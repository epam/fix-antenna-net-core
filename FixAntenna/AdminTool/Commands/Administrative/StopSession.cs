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

namespace Epam.FixAntenna.AdminTool.Commands.Administrative
{
	internal class StopSession : Command
	{
		public override void Execute()
		{
			Log.Debug("Execute StopSession Command");
			try
			{
				var stop = (FixAntenna.Fixicc.Message.StopSession) Request;
				if (string.IsNullOrWhiteSpace(stop.SenderCompID))
				{
					SendInvalidArgument("Parameter SenderCompID is required");
					return;
				}
				if (string.IsNullOrWhiteSpace(stop.TargetCompID))
				{
					SendInvalidArgument("Parameter TargetCompID is required");
					return;
				}
				var id = new SessionId(stop.SenderCompID, stop.TargetCompID, stop.SessionQualifier);
				var session = GetFixSession(id);
				if (session == null)
				{
					SendUnknownSession(id);
					return;
				}
				bool? sendLogout = stop.SendLogout;
				if (sendLogout != null && sendLogout.Value)
				{
					// [-] Fixed 15201: Logout was sent without a text for tag 58
					session.Disconnect(
						!string.IsNullOrWhiteSpace(stop.LogoutReason)
							? stop.LogoutReason
							: "Closed by Remote Admin Interface");
				}
				session.Dispose();
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
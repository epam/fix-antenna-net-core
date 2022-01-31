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
	/// <summary>
	/// The Delete command.
	/// Deletes the specified session.
	/// </summary>
	internal class Delete : Command
	{
		public override void Execute()
		{
			Log.Debug("Execute Delete Command");
			try
			{
				var delete = (FixAntenna.Fixicc.Message.Delete) Request;
				if (string.IsNullOrWhiteSpace(delete.SenderCompID))
				{
					SendInvalidArgument("Parameter SenderCompID is required");
					return;
				}
				if (string.IsNullOrWhiteSpace(delete.TargetCompID))
				{
					SendInvalidArgument("Parameter TargetCompID is required");
					return;
				}
				var id = new SessionId(delete.SenderCompID, delete.TargetCompID, delete.SessionQualifier);
				var session = GetFixSession(id);
				var configuredParams = GetConfiguredSession(id);
				if (session == null && configuredParams == null)
				{
					SendUnknownSession(id);
					return;
				}
				if (session != null)
				{
					Log.Debug("Delete active session");
					if (delete.SendLogout)
					{
						// [-] Fixed 15201: Logout was sent without a text for tag 58
						session.Disconnect(
							!string.IsNullOrWhiteSpace(delete.LogoutReason)
								? delete.LogoutReason
								: "Closed by Remote Admin Interface"
							);
					}
					session.Dispose();
				}
				if (configuredParams != null)
				{
					Log.Debug("Delete configured session");
					ConfiguredSessionRegister.UnregisterSession(configuredParams);
				}
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
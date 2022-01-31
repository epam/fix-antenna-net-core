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
using Epam.FixAntenna.AdminTool.Commands.Util;
using Epam.FixAntenna.Fixicc.Message;
using Epam.FixAntenna.NetCore.FixEngine;

namespace Epam.FixAntenna.AdminTool.Commands.Monitoring
{
	/// <summary>
	/// The SessionParams command.
	/// Returns the list of sessions with parameters.
	/// </summary>
	internal class SessionParams : Command
	{
		public override void Execute()
		{
			Log.Debug("Execute SessionParams Command");
			try
			{
				if (RequestId == null)
				{
					SendInvalidArgument("Parameter RequestID is required");
					return;
				}

				var request = (FixAntenna.Fixicc.Message.SessionParams) Request;
				if (string.IsNullOrWhiteSpace(request.SenderCompID))
				{
					SendInvalidArgument("Parameter SenderCompID is required");
					return;
				}
				if (string.IsNullOrWhiteSpace(request.TargetCompID))
				{
					SendInvalidArgument("Parameter TargetCompID is required");
					return;
				}
				var sessionsListData = new SessionParamsData();
				SessionParameters parameters;
				var id = new SessionId(request.SenderCompID, request.TargetCompID, request.SessionQualifier);
				var session = GetFixSession(id);
				if (session == null)
				{
					parameters = GetConfiguredSession(id);
					if (parameters == null)
					{
						SendUnknownSession(id);
						return;
					}
					sessionsListData.Role = SessionRole.ACCEPTOR; // configured session can be only acceptor sessions
					sessionsListData.ExtraSessionParams = CommandUtil.GetExtraSessionParams(parameters, null);
				}
				else
				{
					parameters = session.Parameters;
					var runtimeState = session.RuntimeState;
					sessionsListData.Role = CommandUtil.GetRole(session);
					sessionsListData.ExtraSessionParams = CommandUtil.GetExtraSessionParams(parameters, runtimeState);
					sessionsListData.RemoteHost = parameters.Host;
					sessionsListData.RemotePort = parameters.Port;
				}

				sessionsListData.SenderCompID = request.SenderCompID;
				sessionsListData.TargetCompID = request.TargetCompID;
				sessionsListData.SessionQualifier = request.SessionQualifier;

				if (parameters.Destinations.Count > 0)
				{
					var destination = parameters.Destinations[0];
					if (!string.IsNullOrWhiteSpace(destination.Host))
					{
						var backup = new Backup { RemoteHost = destination.Host, RemotePort = destination.Port };
						sessionsListData.Backup = backup;
					}
				}

				sessionsListData.Version = CommandUtil.GetVersion(parameters.FixVersion, parameters.AppVersion);

				var response = new Response();
				response.SessionParamsData = sessionsListData;
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
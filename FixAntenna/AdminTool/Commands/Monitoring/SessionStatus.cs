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
	/// The SessionStatus command.
	/// Returns the list of sessions.
	/// </summary>
	internal class SessionStatus : Command
	{
		public override void Execute()
		{
			Log.Debug("Execute SessionStatus Command");
			try
			{
				var sessionStatusData = new SessionStatusData();
				var sessionStatus = (FixAntenna.Fixicc.Message.SessionStatus) Request;
				if (string.IsNullOrWhiteSpace(sessionStatus.SenderCompID))
				{
					SendInvalidArgument("Parameter SenderCompID is required");
					return;
				}
				if (string.IsNullOrWhiteSpace(sessionStatus.TargetCompID))
				{
					SendInvalidArgument("Parameter TargetCompID is required");
					return;
				}
				SessionParameters parameters;
				var id = new SessionId(sessionStatus.SenderCompID, sessionStatus.TargetCompID, sessionStatus.SessionQualifier);
				var session = GetFixSession(id);
				if (session == null)
				{
					parameters = GetConfiguredSession(id);
					if (parameters == null)
					{
						SendUnknownSession(id);
						return;
					}
					// configured session
					sessionStatusData.Status = CommandUtil.ConfiguredSessionStatus;
					sessionStatusData.StatusGroup = CommandUtil.ConfiguredSessionStatusGroup;
				}
				else
				{
					// active session
					parameters = session.Parameters;
					sessionStatusData.Status = session.SessionState.ToString();
					sessionStatusData.StatusGroup = CommandUtil.GetStatusGroup(session);
					sessionStatusData.BackupState = CommandUtil.IsBackupHost(session);
				}

				sessionStatusData.SenderCompID = parameters.SenderCompId;
				sessionStatusData.TargetCompID = parameters.TargetCompId;
				sessionStatusData.SessionQualifier = parameters.SessionQualifier;

				var runtimeState = session?.RuntimeState;
				sessionStatusData.InSeqNum = CommandUtil.GetInSeqNum(parameters, runtimeState);
				sessionStatusData.OutSeqNum = CommandUtil.GetOutSeqNum(parameters, runtimeState);

				var response = new Response();
				response.SessionStatusData = sessionStatusData;
				SendResponseSuccess(response);
			}
			catch (Exception e)
			{
				Log.Error(e);
				SendReject(e.Message);
			}
		}
	}
}
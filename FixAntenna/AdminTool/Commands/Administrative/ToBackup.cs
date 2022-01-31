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
using Epam.FixAntenna.NetCore.FixEngine.Session;

namespace Epam.FixAntenna.AdminTool.Commands.Administrative
{
	/// <summary>
	/// The ToBackup command.
	/// Switches a session connection to primary host.
	/// </summary>
	internal class ToBackup : Command
	{
		public override void Execute()
		{
			Log.Debug("Execute ToBackup Command");
			try
			{
				var toBackup = (FixAntenna.Fixicc.Message.ToBackup) Request;
				if (string.IsNullOrWhiteSpace(toBackup.SenderCompID))
				{
					SendInvalidArgument("Parameter SenderCompID is required");
					return;
				}
				if (string.IsNullOrWhiteSpace(toBackup.TargetCompID))
				{
					SendInvalidArgument("Parameter TargetCompID is required");
					return;
				}
				var id = new SessionId(toBackup.SenderCompID, toBackup.TargetCompID, toBackup.SessionQualifier);
				var session = GetFixSession(id);
				if (session == null)
				{
					SendUnknownSession(id);
					return;
				}
				if (!(session is IBackupFixSession))
				{
					SendInvalidArgument("Not a backup fix session instance");
					return;
				}
				var backupFIXSession = (IBackupFixSession) session;
				if (session.Parameters.Destinations.Count == 1 && backupFIXSession.IsRunningOnBackup)
				{
					SendReject("Session on backup state");
					return;
				}

				backupFIXSession.SwitchToBackUp();
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
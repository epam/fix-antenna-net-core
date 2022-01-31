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
	/// The ToPrimary command.
	/// Switches the session connection to backup host.
	/// </summary>
	internal class ToPrimary : Command
	{
		public override void Execute()
		{
			Log.Debug("Execute ToPrimary Command");
			try
			{
				var toPrimary = (FixAntenna.Fixicc.Message.ToPrimary) Request;
				if (string.IsNullOrWhiteSpace(toPrimary.SenderCompID))
				{
					SendInvalidArgument("Parameter SenderCompID is required");
					return;
				}
				if (string.IsNullOrWhiteSpace(toPrimary.TargetCompID))
				{
					SendInvalidArgument("Parameter TargetCompID is required");
					return;
				}
				var id = new SessionId(toPrimary.SenderCompID, toPrimary.TargetCompID, toPrimary.SessionQualifier);
				var session = GetFixSession(id);
				if (session == null)
				{
					SendUnknownSession(id);
					return;
				}
				if (!(session is IBackupFixSession))
				{
					SendReject("Not a backup fix session instance");
					return;
				}
				var backupFIXSession = (IBackupFixSession) session;
				if (!backupFIXSession.IsRunningOnBackup)
				{
					SendReject("Session on primary state");
					return;
				}
				backupFIXSession.SwitchToPrimary();
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
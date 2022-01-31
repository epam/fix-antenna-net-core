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
	/// The DeleteAll command.
	/// Deletes the sessions.
	/// </summary>
	internal class DeleteAll : Command
	{
		public override void Execute()
		{
			Log.Debug("Execute DeleteAll Command");
			try
			{
				var deleteAll = (Fixicc.Message.DeleteAll) Request;
				IFixSession ourSession = null;
				foreach (var session in GetFixSessions())
				{
					if (session == AdminFixSession)
					{
						ourSession = session;
						continue; // our session
					}
					CloseSession(deleteAll, session);
				}
				var configuredSessionRegister = ConfiguredSessionRegister;
				foreach (var sParams in GetConfiguredSessionParameters())
				{
					if (!IsOurAdminSession(sParams))
					{
						configuredSessionRegister.UnregisterSession(sParams);
					}
				}
				// Bug BBP-749: never kill own session
				// TODO: Exclude all admin session for Exclude.ALL_ADMIN_SESSIONS
	//            if (ourSession != null && !Exclude.CURRENT_ADMIN_SESSION.equals(deleteAll.getExclude())) {
	//                closeSession(deleteAll, ourSession);
	//            }
				SendResponseSuccess(new Response());
			}
			catch (Exception e)
			{
				Log.Error("delete session(s)", e);
				SendError(e.Message);
			}
		}

		private bool IsOurAdminSession(SessionParameters @params)
		{
			var sp = AdminFixSession.Parameters;
			return sp.SessionId.Equals(@params.SessionId);
		}

		private void CloseSession(Fixicc.Message.DeleteAll deleteAll, IFixSession ourSession)
		{
			if (deleteAll.SendLogout)
			{
				ourSession.Disconnect(
					!string.IsNullOrWhiteSpace(deleteAll.LogoutReason)
						? deleteAll.LogoutReason
						: "Closed by Remote Admin Interface");
			}
			ourSession.Dispose();
		}
	}
}
// Copyright (c) 2022 EPAM Systems
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

using Epam.FixAntenna.NetCore.FixEngine.Session;

using Quartz;

using System;
using System.Threading.Tasks;

namespace Epam.FixAntenna.NetCore.FixEngine.Scheduler.Tasks
{
	/// <summary>
	/// Thi task disconnects specific session.
	/// </summary>
	internal class SessionStopTask : AbstractSessionTask
	{
		protected override async Task RunForSession(IJobExecutionContext context, IExtendedFixSession session)
		{
			if (SessionState.IsDisposed(session.SessionState))
			{
				if (Log.IsWarnEnabled)
				{
					Log.Warn("Session could not be disconnected because it was disposed already: " + session);
				}
				return;
			}

			if (Log.IsDebugEnabled)
			{
				Log.Debug("Disconnect session: " + session);
			}
			try
			{
				await session.DisconnectAsync("Scheduled disconnect");
			}
			catch (Exception e)
			{
				Log.Error("Error on disconnect", e);
			}
		}
	}
}
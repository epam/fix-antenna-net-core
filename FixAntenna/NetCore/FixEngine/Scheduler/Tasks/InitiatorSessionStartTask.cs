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

using System.IO;
using System.Threading.Tasks;

namespace Epam.FixAntenna.NetCore.FixEngine.Scheduler.Tasks
{
	/// <summary>
	/// Start initiator session. If it not exists than create it first.
	/// </summary>
	internal class InitiatorSessionStartTask : AbstractSessionTask
	{
		protected override async Task RunForSession(IJobExecutionContext context, IExtendedFixSession session)
		{
			var sessionParameters = session.Parameters;
			if (SessionState.IsDisposed(session.SessionState))
			{
				if (Log.IsWarnEnabled)
				{
					Log.Warn("Session could not be started because it was disposed: " + sessionParameters);
				}
				return;
			}

			if (SessionState.IsDisconnected(session.SessionState))
			{
				if (Log.IsDebugEnabled)
				{
					Log.Debug("Start initiator session: " + sessionParameters);
				}
				try
				{
					await session.ConnectAsync();
				}
				catch (IOException e)
				{
					Log.Error("Session " + sessionParameters.SessionId + " could not be started", e);
				}
			}
			else
			{
				Log.Warn("Session " + sessionParameters.SessionId + " is already active");
			}
		}
	}
}
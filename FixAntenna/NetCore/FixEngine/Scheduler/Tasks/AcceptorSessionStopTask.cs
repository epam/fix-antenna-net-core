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

using System.Threading.Tasks;
using Epam.FixAntenna.NetCore.FixEngine.Manager;

namespace Epam.FixAntenna.NetCore.FixEngine.Scheduler.Tasks
{
	/// <summary>
	/// This task shutdown acceptor session and deny connection for it.
	/// </summary>
	internal class AcceptorSessionStopTask : SessionStopTask
	{
		internal AllowedSessionRegistry Registry { get; }

		public AcceptorSessionStopTask()
		{
			//TODO: take Registry instance from FixSessionManager or FixServer, depending on Registry location 
			//Registry = FixSessionManager.Instance.AllowedSessionsRegistry;
		}

		protected override async Task RunForSession(IJobExecutionContext context, IExtendedFixSession session)
		{
			if (Log.IsDebugEnabled)
			{
				Log.Debug("Deny to connect acceptor session: " + session);
			}
			Registry.DenySessionForConnect(session.Parameters);
			await base.RunForSession(context, session);
		}
	}
}
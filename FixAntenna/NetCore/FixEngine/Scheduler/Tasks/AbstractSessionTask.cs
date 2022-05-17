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

using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.FixEngine.Session;

using Quartz;

using System.Threading.Tasks;

namespace Epam.FixAntenna.NetCore.FixEngine.Scheduler.Tasks
{
	/// <summary>
	/// Abstract task for session processing.
	/// </summary>
	internal abstract class AbstractSessionTask : IJob
	{
		protected readonly ILog Log;

		public AbstractSessionTask()
		{
			Log = LogFactory.GetLog(GetType());
		}

		public async Task Execute(IJobExecutionContext context)
		{
			var sessionId = context.JobDetail.JobDataMap.GetString("SessionId");
			var session = FixSessionManager.Instance.Locate(sessionId);

			if (session == null)
			{
				Log.Debug($"Session {sessionId} isn`t active. Task cannot be run.");
				return;
			}

			await RunForSession(context, session);
		}

		protected abstract Task RunForSession(IJobExecutionContext context, IExtendedFixSession session);
	}
}
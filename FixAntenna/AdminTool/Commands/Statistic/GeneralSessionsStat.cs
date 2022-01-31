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
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.FixEngine;

namespace Epam.FixAntenna.AdminTool.Commands.Statistic
{
	/// <summary>
	/// The GeneralSessionsStat command.
	/// Returns a list of statistics on sessions.
	/// </summary>
	internal class GeneralSessionsStat : Command
	{
		public override void Execute()
		{
			Log.Debug("Execute GeneralSessionsStat Command");
			try
			{
				var generalSessionsStatData = new GeneralSessionsStatData();

				var activeSessions = 0;
				var terminatedNormalSessions = 0;
				var reconnectingSessions = 0;
				var terminatedAbnormalSessions = 0;
				var awaitingSessions = 0;
				long numOfPrecessedMessages = 0;
				long minLifeTime = long.MaxValue, maxLifeTime = 0;
				var currTime = DateTimeHelper.CurrentMilliseconds;
				long lastSessionCreation = 0;
				var statisticEnabled = false;
				foreach (var session in GetFixSessions())
				{
					switch (session.SessionState.EnumValue)
					{
						case SessionState.InnerEnum.Connected:
							activeSessions++;
							break;
						case SessionState.InnerEnum.Disconnected:
							terminatedNormalSessions++;
							break;
						case SessionState.InnerEnum.Reconnecting:
							reconnectingSessions++;
							terminatedAbnormalSessions++;
							break;
						case SessionState.InnerEnum.WaitingForLogon:
							activeSessions++;
							break;
					}
					if (session.IsStatisticEnabled)
					{
						// statistic is enable for all sessions or disable for all sessions
						statisticEnabled = true;
						numOfPrecessedMessages += session.NoOfInMessages + session.NoOfOutMessages;
					}

					var diffTime = Math.Abs(currTime - session.IsEstablished);
					if (diffTime > maxLifeTime)
					{
						maxLifeTime = diffTime;
					}
					if (diffTime < minLifeTime)
					{
						minLifeTime = diffTime;
						lastSessionCreation = session.IsEstablished;
					}
				}
				generalSessionsStatData.ActiveSessions = activeSessions;
				generalSessionsStatData.AwaitingSessions = awaitingSessions;
				generalSessionsStatData.TerminatedAbnormalSessions = terminatedAbnormalSessions;
				generalSessionsStatData.ReconnectingSessions = reconnectingSessions;
				generalSessionsStatData.TerminatedNormalSessions = terminatedNormalSessions;
				if (statisticEnabled)
				{
					generalSessionsStatData.NumOfProcessedMessages = numOfPrecessedMessages;
					generalSessionsStatData.NumOfProcessedMessages = numOfPrecessedMessages;
				}
				generalSessionsStatData.MaxSessionLifetime = maxLifeTime;
				generalSessionsStatData.MinSessionLifetime = minLifeTime;
				generalSessionsStatData.LastSessionCreation = DateTimeHelper.FromMilliseconds(lastSessionCreation);

				var response = new Response();
				response.GeneralSessionsStatData = generalSessionsStatData;
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
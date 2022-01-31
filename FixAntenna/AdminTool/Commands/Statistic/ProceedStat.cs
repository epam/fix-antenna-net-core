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

namespace Epam.FixAntenna.AdminTool.Commands.Statistic
{
	/// <summary>
	/// The ProceedStat command.
	/// Returns the total amount of received and send messages.
	/// </summary>
	internal class ProceedStat : Command
	{
		public override void Execute()
		{
			Log.Debug("Execute ProceedStat Command");
			try
			{
				var proceedStatData = new ProceedStatData();
				long result = 0;
				foreach (var session in GetFixSessions())
				{
					if (!session.IsStatisticEnabled)
					{
						throw new InvalidOperationException("Message statistic is disabled");
					}
					result += session.NoOfOutMessages;
					result += session.NoOfInMessages;
				}
				proceedStatData.ProceedMessages = (int) result;
				var response = new Response { ProceedStatData = proceedStatData };
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
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
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;

namespace Epam.FixAntenna.AdminTool.Commands.Statistic
{
	/// <summary>
	/// The SessionStat command.
	/// Returns the statistic of session.
	/// </summary>
	internal class SessionStat : Command
	{
		public override void Execute()
		{
			Log.Debug("Execute SessionStat Command");
			try
			{
				var sessionStatData = new SessionStatData();
				var request = (FixAntenna.Fixicc.Message.SessionStat) Request;
				if (string.IsNullOrWhiteSpace(request.SenderCompID))
				{
					SendInvalidArgument("Parameter SenderCompID is required");
					return;
				}
				if (string.IsNullOrWhiteSpace(request.TargetCompID))
				{
					SendInvalidArgument("Parameter TargetCompID is required");
					return;
				}
				var id = new SessionId(request.SenderCompID, request.TargetCompID, request.SessionQualifier);
				var session = GetFixSession(id);
				if (session == null)
				{
					SendUnknownSession(id);
					return;
				}

				var parameters = session.Parameters;
				sessionStatData.SenderCompID = parameters.SenderCompId;
				sessionStatData.TargetCompID = parameters.TargetCompId;
				sessionStatData.SessionQualifier = parameters.SessionQualifier;
				sessionStatData.Established = DateTimeHelper.FromMilliseconds(session.IsEstablished);
				sessionStatData.LastReceivedMessage = DateTimeHelper.FromMilliseconds(session.LastInMessageTimestamp);
				sessionStatData.LastSentMessage = DateTimeHelper.FromMilliseconds(session.LastOutMessageTimestamp);
				if (session.IsStatisticEnabled)
				{
					sessionStatData.NumOfProcessedMessages = session.NoOfInMessages + session.NoOfOutMessages;
					sessionStatData.ReceivedBytes = (int) session.BytesRead;
					sessionStatData.SentBytes = (int) session.BytesSent;
					sessionStatData.ReceivedMessages = (int) session.NoOfInMessages;
					sessionStatData.SentMessages = (int) session.NoOfOutMessages;
				}
				sessionStatData.MaxQueueSize = parameters.Configuration.GetPropertyAsBytesLength(Config.MaxMessageSize);
				var response = new Response();
				response.SessionStatData = sessionStatData;
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
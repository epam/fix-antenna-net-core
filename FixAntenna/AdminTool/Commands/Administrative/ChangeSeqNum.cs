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
	/// The ChangeSeqNum command.
	/// Changes the sequency number for specified session.
	/// </summary>
	internal class ChangeSeqNum : Command
	{
		public override void Execute()
		{
			Log.Debug("Execute ChangeSeqNum Command");
			try
			{
				var changeSeqNum = (FixAntenna.Fixicc.Message.ChangeSeqNum) Request;
				var senderCompId = changeSeqNum.SenderCompID;
				if (string.IsNullOrWhiteSpace(senderCompId))
				{
					SendInvalidArgument("Parameter SenderCompID is required");
					return;
				}
				var targetCompId = changeSeqNum.TargetCompID;
				if (string.IsNullOrWhiteSpace(targetCompId))
				{
					SendInvalidArgument("Parameter TargetCompID is required");
					return;
				}
				var id = new SessionId(senderCompId, targetCompId, changeSeqNum.SessionQualifier);
				SessionParameters parameters;
				var session = GetFixSession(id);
				if (session == null)
				{
					// use configured session if absent active session
					parameters = GetConfiguredSession(id);
					if (parameters == null)
					{
						SendUnknownSession(id);
						return;
					}
				}
				else
				{
					parameters = session.Parameters;
				}

				var outSeqNumObj = changeSeqNum.OutSeqNum;
				var inSeqNumObj = changeSeqNum.InSeqNum;

				var outSeqNum = outSeqNumObj == null ? 0 : outSeqNumObj.Value;
				var inSeqNum = inSeqNumObj == null ? 0 : inSeqNumObj.Value;


				if (outSeqNum < 0 || inSeqNum < 0)
				{
					SendInvalidArgument("Parameter OutSeqNum or InSeqNum must be not negative");
					return;
				}

				if (outSeqNum >= 0 && outSeqNumObj != null)
				{
					if (session == null || SessionState.IsDisconnected(session.SessionState))
					{
						parameters.OutgoingSequenceNumber = outSeqNum;
					}
					else
					{
						session.RuntimeState.OutSeqNum = outSeqNum; // In the settings stored next number rather than the current one.
					}
					if (Log.IsInfoEnabled)
					{
						Log.Info($"ChangeSeqNum to: OutSeqNum={outSeqNum} for session {id}");
					}
				}
				if (inSeqNum >= 0 && inSeqNumObj != null)
				{
					if (session == null || SessionState.IsDisconnected(session.SessionState))
					{
						parameters.IncomingSequenceNumber = inSeqNum;
					}
					else
					{
						session.RuntimeState.InSeqNum = inSeqNum; // In the settings stored next number rather than the current one.
					}
					if (Log.IsInfoEnabled)
					{
						Log.Info($"ChangeSeqNum to: InSeqNum={inSeqNum} for session {id}");
					}
				}
				SendResponseSuccess(new Response());
			}
			catch (Exception e)
			{
				Log.Error(e.Message, e);
				SendError(e.Message);
			}
		}
	}
}
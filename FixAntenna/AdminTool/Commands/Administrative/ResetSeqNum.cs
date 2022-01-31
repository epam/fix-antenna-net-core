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
	/// The ResetSeqNum command.
	/// Reset the sequence number for specified session.
	/// </summary>
	internal class ResetSeqNum : Command
	{
		public override void Execute()
		{
			Log.Debug("Execute ResetSeqNum Command");
			try
			{
				var resetSeqNum = (FixAntenna.Fixicc.Message.ResetSeqNum) Request;
				var senderCompId = resetSeqNum.SenderCompID;
				if (string.IsNullOrWhiteSpace(senderCompId))
				{
					SendInvalidArgument("Parameter SenderCompID is required");
					return;
				}
				var targetCompId = resetSeqNum.TargetCompID;
				if (string.IsNullOrWhiteSpace(targetCompId))
				{
					SendInvalidArgument("Parameter TargetCompID is required");
					return;
				}
				var id = new SessionId(senderCompId, targetCompId, resetSeqNum.SessionQualifier);
				var session = GetFixSession(id);
				if (session != null)
				{
					session.ResetSequenceNumbers();
				}
				else
				{
					var parameters = GetConfiguredSession(id);
					if (parameters != null)
					{
						parameters.IncomingSequenceNumber = 1;
						parameters.OutgoingSequenceNumber = 1;
					}
					else
					{
						SendUnknownSession(id);
						return;
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
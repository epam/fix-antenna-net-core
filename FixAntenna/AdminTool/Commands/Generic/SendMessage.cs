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
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.AdminTool.Commands.Generic
{
	/// <summary>
	/// The SendMessage command.
	/// Send the message to specified session.
	/// </summary>
	internal class SendMessage : Command
	{
		public override void Execute()
		{
			Log.Debug("Execute SendMessage Command");
			try
			{
				var sendMessage = (FixAntenna.Fixicc.Message.SendMessage) Request;
				if (string.IsNullOrWhiteSpace(sendMessage.SenderCompID))
				{
					SendInvalidArgument("Parameter SenderCompID is required");
					return;
				}
				if (string.IsNullOrWhiteSpace(sendMessage.TargetCompID))
				{
					SendInvalidArgument("Parameter TargetCompID is required");
					return;
				}
				var id = new SessionId(sendMessage.SenderCompID, sendMessage.TargetCompID, sendMessage.SessionQualifier);
				var session = GetFixSession(id);
				if (session == null)
				{
					SendUnknownSession(id);
					return;
				}
				var message = StringHelper.NewString(sendMessage.Message);
				if (string.IsNullOrWhiteSpace(message))
				{
					SendInvalidArgument("Parameter Message is required");
					return;
				}

				if (message.IndexOf("&amp;", StringComparison.Ordinal) > 0)
				{
					message = message.ReplaceAll("&amp;", "&");
				}

	//            if (message.indexOf(AdminConstants.SEND_MESSAGE_DELIMITER) > 0) {
	//                message = message.replaceAll(AdminConstants.SEND_MESSAGE_DELIMITER, "\u0001");
	//            }

				message = XmlHelper.RestoreSpecialCharacters(message);

				var msg = RawFixUtil.GetFixMessage(message.AsByteArray());
				session.SendMessage(msg);
				SendResponseSuccess(new Response());
			}
			catch (Exception e)
			{
				Log.Error(e);
				SendError(e.Message);
			}
		}
	}
}
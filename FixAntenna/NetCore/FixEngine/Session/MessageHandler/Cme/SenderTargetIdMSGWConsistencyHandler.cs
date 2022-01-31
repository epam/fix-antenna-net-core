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
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Cme
{
	internal class SenderTargetIdMSGWConsistencyHandler : AbstractGlobalMessageHandler
	{

		private static readonly string[] _faultTolIds = new string[] { "B", "P", "N", "U" };

		public override void OnNewMessage(FixMessage message)
		{
			HandleMessage(message);
		}

		private void HandleMessage(FixMessage message)
		{
			var messageTargetCompId = message.GetTag(Tags.TargetCompID);
			var messageSenderCompId = message.GetTag(Tags.SenderCompID);
			if (messageTargetCompId == null || messageSenderCompId == null)
			{
	//            getFIXSession().shutdown(false);
	//            return;
			}
			else
			{
				var sessionParameters = Session.Parameters;
				var messageTargetCompIdStr = messageTargetCompId.StringValue;
				var sessionSenderCompId = sessionParameters.SenderCompId;
				var senderCompIdCorrect = ValidateTargetCompID(messageTargetCompIdStr, sessionSenderCompId);
				var targetCompIdCorrect = messageSenderCompId.StringValue.Equals(sessionParameters.TargetCompId);

				if (!senderCompIdCorrect || !targetCompIdCorrect)
				{
					SendRejectAndDisconnectSession(message, messageTargetCompId, messageSenderCompId, senderCompIdCorrect);
					return;
				}
			}
			CallNextHandler(message);
		}

		public virtual bool ValidateTargetCompID(string incomingTarget, string mySenderComId)
		{
			if (incomingTarget.Length < 3)
			{
				return false;
			}
			for (var i = 0; i < 3; i++)
			{
				if (mySenderComId[i] != incomingTarget[i])
				{
					return false;
				}
			}
			foreach (var faultCode in _faultTolIds)
			{
				if (incomingTarget.EndsWith(faultCode, StringComparison.Ordinal))
				{
					return true;
				}
			}
			return false;
		}

		private void SendRejectAndDisconnectSession(FixMessage message, TagValue messageTargetCompId, TagValue messageSenderCompId, bool senderCompIdCorrect)
		{
			var msg = "Invalid SenderCompID or TargetCompID";
			var list = Session.MessageFactory.GetRejectForMessageTag(message, senderCompIdCorrect ? (int)messageSenderCompId.LongValue : (int)messageTargetCompId.LongValue, 9, msg);
			if (FixMessageUtil.IsLogon(message))
			{
				Session.ClearQueue();
			}
			Session.ErrorHandler.OnWarn(msg, new InvalidMessageException(message, msg));
			Session.SendMessageOutOfTurn(MsgType.Reject, list);
			Session.Disconnect(DisconnectReason.InvalidMessage, msg);
		}
	}
}
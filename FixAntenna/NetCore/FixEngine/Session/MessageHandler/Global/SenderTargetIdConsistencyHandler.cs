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
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation.Error;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global
{
	/// <summary>
	/// Global sender target consistency handler.
	/// </summary>
	internal class SenderTargetIdConsistencyHandler : AbstractGlobalMessageHandler
	{
		private const string ErrorMissedSenderOrTarget = "Missed SenderCompID or TargetCompID";
		private const string ErrorInvalidSenderOrTarget = "Invalid SenderCompID or TargetCompID";
		private TagValue _problemTv = new TagValue();
		private bool _consistencyCheck;

		/// <inheritdoc />
		public override IExtendedFixSession Session
		{
			set
			{
				base.Session = value;
				_consistencyCheck = value.Parameters.Configuration.GetPropertyAsBoolean(Config.SenderTargetIdConsistencyCheck);
			}
		}

		/// <inheritdoc />
		public override void OnNewMessage(FixMessage message)
		{
			HandleMessage(message);
		}


		private void HandleMessage(FixMessage message)
		{
			if (message.IsTagExists(Tags.TargetCompID) && message.IsTagExists(Tags.SenderCompID))
			{
				if (_consistencyCheck)
				{
					var sessionParameters = Session.Parameters;
					var targetCompIdCorrect = FixMessageUtil.IsTagValueEquals(message, Tags.TargetCompID, sessionParameters.SenderCompId);
					var senderCompIdCorrect = FixMessageUtil.IsTagValueEquals(message, Tags.SenderCompID, sessionParameters.TargetCompId);
					if (!senderCompIdCorrect || !targetCompIdCorrect)
					{
						var problemTagId = !senderCompIdCorrect ? Tags.SenderCompID : Tags.TargetCompID;
						message.LoadTagValue(problemTagId, _problemTv);
						SendRejectAndDisconnectSession(message, problemTagId, FixErrorCode.CompidProblem, ErrorInvalidSenderOrTarget);
						throw new MessageValidationException(message, _problemTv, FixErrorCode.CompidProblem, ErrorInvalidSenderOrTarget, true);
					}
				}
			}
			else
			{
				_problemTv.TagId = (!message.IsTagExists(Tags.SenderCompID)) ? Tags.SenderCompID : Tags.TargetCompID;
				_problemTv.Value = Array.Empty<byte>();

				SendReject(message, _problemTv.TagId, FixErrorCode.RequiredTagMissing, ErrorMissedSenderOrTarget);
				throw new MessageValidationException(message, _problemTv, FixErrorCode.RequiredTagMissing, ErrorMissedSenderOrTarget, false);
			}
			CallNextHandler(message);
		}

		private void SendRejectAndDisconnectSession(FixMessage message, int problemTagId, FixErrorCode errorCode, string errorDescr)
		{
			SendReject(message, problemTagId, errorCode, errorDescr);
			Session.Disconnect(DisconnectReason.InvalidMessage, errorDescr);
		}

		private void SendReject(FixMessage message, int problemTagId, FixErrorCode errorCode, string errorDescr)
		{
			var list = Session.MessageFactory.GetRejectForMessageTag(message, problemTagId, errorCode.Code, errorDescr);
			Session.SendMessageOutOfTurn(MsgType.Reject, list);
		}
	}
}
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
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation.Error;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global
{
	/// <summary>
	/// The global sending time accuracy handler.
	/// </summary>
	internal class SendingTimeAccuracyHandler : AbstractGlobalMessageHandler
	{

		internal const int DefaultReasonableDelay = 2 * 60 * 1000; // 2 minutes
		internal const int DefaultAccuracy = 1;
		public const string ErrorInvalidSendingTime = "Invalid sending time";
		public const string ErrorMissedSendingTime = "Missed sending time";
		public const string ErrorSendingTimeAccuracy = "SendingTime accuracy problem";

		private int _reasonableDelay = DefaultReasonableDelay;
		private TagValue _sendingTimeTv = new TagValue(Tags.SendingTime);

		private bool _enableAccuracyCheck = false;

		private int _accuracyInMs = DefaultAccuracy;

		/// <inheritdoc />
		public override IExtendedFixSession Session
		{
			set
			{
				base.Session = value;

				var configuration = new ConfigurationAdapter(value.Parameters.Configuration);
				_enableAccuracyCheck = configuration.IsSendingTimeAccuracyCheckEnabled;
				_reasonableDelay = configuration.GetReasonableDelay(DefaultReasonableDelay);
				_accuracyInMs = configuration.GetAccuracy(DefaultAccuracy);
			}
		}

		/// <inheritdoc />
		public override void OnNewMessage(FixMessage message)
		{
			HandleMessage(message);
		}

		private bool IsMessageFromBuffer()
		{
			return ((AbstractFixSession)Session).SequenceManager.SeqResendManager.IsMessageProcessingFromBufferStarted;
		}

		private void HandleMessage(FixMessage message)
		{
			if (_enableAccuracyCheck && !IsMessageFromBuffer())
			{
				var sendingTime = ParseMessageTimestamp(message);
				if (!ValidateSendingTime(sendingTime))
				{
					message.LoadTagValue(Tags.SendingTime, _sendingTimeTv);
					SendRejectAndDisconnectSession(message, Tags.SendingTime, FixErrorCode.SendingTimeAccuracyProblem.Code, ErrorSendingTimeAccuracy);
					throw new MessageValidationException(message, _sendingTimeTv, FixErrorCode.SendingTimeAccuracyProblem, ErrorSendingTimeAccuracy, true);
				}
			}
			CallNextHandler(message);
		}

		/// <summary>
		/// Validates the sending time field(52 tag).
		/// </summary>
		/// <returns> true if need to call next handler </returns>
		public virtual bool ValidateSendingTime(DateTime sendingDate)
		{
			var currentTimeInMills = DateTimeHelper.CurrentMilliseconds;
			var messageMs = sendingDate.TotalMilliseconds();
			return !IsTimeOutOfBorder(messageMs, currentTimeInMills);
		}

		/// <summary>
		/// Parse message timestamp to local field
		/// </summary>
		/// <param name="message"> the incoming message </param>
		/// <returns> true if parse was successful </returns>
		private DateTime ParseMessageTimestamp(FixMessage message)
		{
			try
			{
				return message.GetTagValueAsTimestamp(Tags.SendingTime);
			}
			catch (Exception)
			{
				if (message.IsTagExists(Tags.SendingTime))
				{
					message.LoadTagValue(Tags.SendingTime, _sendingTimeTv);
					SendRejectAndDisconnectSession(message, Tags.SendingTime, FixErrorCode.IncorrectDataFormatForValue.Code, ErrorInvalidSendingTime);

					throw new MessageValidationException(message, _sendingTimeTv, FixErrorCode.IncorrectDataFormatForValue, ErrorInvalidSendingTime, true);
				}
				else
				{
					_sendingTimeTv.Value = Array.Empty<byte>();
					SendRejectAndDisconnectSession(message, Tags.SendingTime, FixErrorCode.RequiredTagMissing.Code, ErrorMissedSendingTime);

					throw new MessageValidationException(message, _sendingTimeTv, FixErrorCode.RequiredTagMissing, ErrorMissedSendingTime, true);
				}
			}
		}

		private void ClearQueue(FixMessage message)
		{
			if (FixMessageUtil.IsLogon(message))
			{
				// remove outgoing logon since we are going to close session
				Session.ClearQueue();
			}
		}

		public virtual bool IsTimeInBorder(long msgTimeMs, long srvTimeMs)
		{
			if (srvTimeMs > msgTimeMs)
			{
				return (srvTimeMs - msgTimeMs) < (_reasonableDelay + _accuracyInMs);
			}
			else
			{
				return (msgTimeMs - srvTimeMs) < (_reasonableDelay + _accuracyInMs);
			}
		}

		public virtual bool IsTimeOutOfBorder(long msgTimeMs, long srvTimeMs)
		{
			return !IsTimeInBorder(msgTimeMs, srvTimeMs);
		}

		private void SendRejectAndDisconnectSession(FixMessage message, int refTagId, int rejectReason, string problemDescription)
		{
			ClearQueue(message);
			//getFIXSession().GetErrorHandler().OnWarn(problemDescription, new InvalidMessageException(message, problemDescription));

			var rejectForMessageTag = Session.MessageFactory.GetRejectForMessageTag(message, refTagId, rejectReason, problemDescription);
			Session.SendMessageOutOfTurn(MsgType.Reject, rejectForMessageTag);
			Session.Disconnect(DisconnectReason.InvalidMessage, problemDescription);
		}
	}
}
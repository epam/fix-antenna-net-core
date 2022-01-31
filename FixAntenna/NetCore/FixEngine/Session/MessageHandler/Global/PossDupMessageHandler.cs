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
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation.Error;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global
{
	/// <summary>
	/// The global PossDup message handler.
	/// </summary>
	internal class PossDupMessageHandler : AbstractGlobalMessageHandler
	{
		private bool _enableOrigSendingTimeCheck = false;
		private bool _ignorePossDupForGapFill = true;

		private TagValue _origSendingTimeTv = new TagValue();
		private TagValue _sendingTimeTv = new TagValue();

		public PossDupMessageHandler()
		{
		}

		/// <inheritdoc />
		public override IExtendedFixSession Session
		{
			set
			{
				base.Session = value;

				var configuration = new ConfigurationAdapter(value.Parameters.Configuration);
				_enableOrigSendingTimeCheck = configuration.IsOrigSendingTimeCheckingEnabled;
				_ignorePossDupForGapFill = configuration.IsIgnorePossDupForGapFill;
			}
		}

		/// <summary>
		/// The method validates the message only if PossDupFlag is
		/// set to true, otherwise the next handler is invoked.
		/// The content of message should includes Smh.OrigSendingTime, Smh.SendingTime, if this
		/// fields are incorrect the reject message will be send.
		/// </summary>
		/// <seealso cref="IFixMessageListener.OnNewMessage"> </seealso>
		public override void OnNewMessage(FixMessage message)
		{
			HandleMessage(message);
		}

		private void HandleMessage(FixMessage message)
		{
			if (_enableOrigSendingTimeCheck && FixMessageUtil.IsPosDup(message) && !IgnoreMessageForOrigTimeValidation(message))
			{
				//FIXField sendingTime = message.GetTag(Smh.SendingTime);
				var sendingTimeIndex = message.GetTagIndex(Tags.SendingTime);
				var origSendingTimeIndex = message.GetTagIndex(Tags.OrigSendingTime);

				if (origSendingTimeIndex == IndexedStorage.NotFound)
				{
					DecrementIncomingSeqNumber(message);
					SendReject(message, Tags.OrigSendingTime, FixErrorCode.RequiredTagMissing.Code, PossDupRejectCode.MissingOrgSendTime.Value);
					throw new MessageValidationException(message, new TagValue(Tags.OrigSendingTime), FixErrorCode.RequiredTagMissing, PossDupRejectCode.MissingOrgSendTime.Value, false);
				}
				else if (sendingTimeIndex != IndexedStorage.NotFound)
				{
					message.LoadTagValueByIndex(sendingTimeIndex, _sendingTimeTv);
					message.LoadTagValueByIndex(origSendingTimeIndex, _origSendingTimeTv);
					try
					{
						var origSending = FixTypes.ParseTimestamp(_origSendingTimeTv.Buffer, _origSendingTimeTv.Offset, _origSendingTimeTv.Length);
						var sending = FixTypes.ParseTimestamp(_sendingTimeTv.Buffer, _sendingTimeTv.Offset, _sendingTimeTv.Length);
						if (origSending > sending)
						{
							DecrementIncomingSeqNumber(message);
							SendReject(message, Tags.OrigSendingTime, FixErrorCode.SendingTimeAccuracyProblem.Code, PossDupRejectCode.OrgSendTimeAfterSendTime.Value);
							throw new MessageValidationException(message, _origSendingTimeTv, FixErrorCode.SendingTimeAccuracyProblem, PossDupRejectCode.OrgSendTimeAfterSendTime.Value, false);
						}
					}
					catch (MessageValidationException)
					{
						throw;
					}
					catch (Exception)
					{
						DecrementIncomingSeqNumber(message);
						SendReject(message, Tags.OrigSendingTime, FixErrorCode.SendingTimeAccuracyProblem.Code, PossDupRejectCode.InvalidSendingTime.Value);
						throw new MessageValidationException(message, _origSendingTimeTv, FixErrorCode.SendingTimeAccuracyProblem, PossDupRejectCode.InvalidSendingTime.Value, false);
					}
				}
			}
			CallNextHandler(message);
		}

		private bool IgnoreMessageForOrigTimeValidation(FixMessage message)
		{
			return _ignorePossDupForGapFill && IsGapFill(message);
		}

		private bool IsGapFill(FixMessage message)
		{
			return message.IsTagExists(123) && message.GetTagValueAsString(123).Equals("Y");
		}

		private void DecrementIncomingSeqNumber(FixMessage message)
		{
			if (HasMessageBeenReceivedAlready(message))
			{
				var sessionSequenceManager = GetSequenceManager();
				sessionSequenceManager.DecrementIncomingSeqNumber();

	//            SessionParameters sessionParameters = getFIXSession().GetSessionParametersInstance();
	//            sessionParameters.decrementIncomingSequenceNumber();
	//
	//            sessionParameters.setProcessedIncomingSequenceNumber(incoming > 0 ? incoming - 1 : 0);
	//            long incoming = sessionParameters.GetIncomingSequenceNumber();
			}
		}

		private void SendReject(FixMessage message, int originalSendingTime, int rejectReason, string rejectMessage)
		{
			var list = Session.MessageFactory.GetRejectForMessageTag(message, originalSendingTime, rejectReason, rejectMessage);
			//getFIXSession().GetErrorHandler().OnWarn(rejectMessage, new InvalidMessageException(message, rejectMessage));
			Session.SendMessageOutOfTurn(MsgType.Reject, list);
		}

		private bool HasMessageBeenReceivedAlready(FixMessage message)
		{
			return message.MsgSeqNumber < Session.RuntimeState.InSeqNum;
		}

		private ISessionSequenceManager GetSequenceManager()
		{
			return ((AbstractFixSession) Session).SequenceManager;
		}
	}
}
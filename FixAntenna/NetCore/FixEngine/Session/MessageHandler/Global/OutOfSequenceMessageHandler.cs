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

using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global
{
	/// <summary>
	/// The out of sequence global message handler.
	/// </summary>
	internal class OutOfSequenceMessageHandler : AbstractGlobalMessageHandler
	{
		private bool _posDupSmartDelivery;
		private bool _advancedResendRequestProcessing;
		private bool _allowToSendMultipleRr = true;
		private bool _ignoreSeqNumTooLowAtLogon = true;
		private int _allowedCountOfSimilarRr = 3;
		protected internal bool IncludeNextExpectedMsgSeqNum;
		private bool _resetQueueOnLowSeqNum = true;
		private bool _resetSeqNumFromFirstLogon = false;

		/// <summary>
		/// If incoming sequence is equals to expected the next handler will be calls.
		/// <p/>
		/// If incoming sequence > expected the '2' message will be send, otherwise
		/// the session is shutdown.
		/// </summary>
		/// <param name="message"> the received message. </param>
		public override void OnNewMessage(FixMessage message)
		{
			HandleMessage(message);
		}

		public override IExtendedFixSession Session
		{
			set
			{
				var configuration = value.Parameters.Configuration;
				var cfg = new ConfigurationAdapter(configuration);
				_posDupSmartDelivery = cfg.PossDupSmartDelivery;
				_advancedResendRequestProcessing = cfg.AdvancedResendRequestProcessing;
				_allowToSendMultipleRr = !cfg.SwitchOffSendingMultipleResendRequests;
				_ignoreSeqNumTooLowAtLogon = cfg.IgnoreSeqNumTooLowAtLogon;
				_allowedCountOfSimilarRr = cfg.AllowedCountOfSimilarRr;
				var fixVersion = value.Parameters.FixVersion;
				// include only if current session FIXVersion is 4.4 or above
				if (fixVersion.CompareTo(FixVersion.Fix44) >= 0)
				{
					IncludeNextExpectedMsgSeqNum = cfg.HandleSeqNumAtLogon;
				}

				_resetQueueOnLowSeqNum = configuration.GetPropertyAsBoolean(Config.ResetQueueOnLowSequenceNum, true);

				if (value is AcceptorFixSession)
				{
					_resetSeqNumFromFirstLogon = cfg.ResetSeqNumFromFirstLogonMode() == ResetSeqNumFromFirstLogonMode.Schedule;
				}

				base.Session = value;
			}
		}

		private void HandleMessage(FixMessage message)
		{
			long msgInSeqNum = 0;
			try
			{
				msgInSeqNum = message.GetTagAsInt(Tags.MsgSeqNum);
			}
			catch (FieldNotFoundException)
			{
			}

			var sequenceManager = GetSequenceManager();
			var expectedSeqNum = sequenceManager.GetExpectedIncomingSeqNumber();

			if (msgInSeqNum == expectedSeqNum)
			{
				ProcessMessageWithExpectedSeqNum(message, msgInSeqNum);
			}
			else if (IsMsgShouldBeProcessedAnyway(message))
			{
				//if message has higher sequence but have to be processed as usually it needs to decrement in seq because
				// it will be incremented later in @IncrementIncomingMessageHandler
				if (FixMessageUtil.IsPosDup(message) || msgInSeqNum > expectedSeqNum || IsSeqResetAndNewSeqNoIsLessThanExpected(message, expectedSeqNum))
				{
					sequenceManager.DecrementIncomingSeqNumber();
					Session.SetAttribute(ExtendedFixSessionAttribute.SequenceWasDecremented.Name, expectedSeqNum);
				}

				ProcessMessageWithExpectedSeqNum(message, msgInSeqNum);
			}
			else if (msgInSeqNum > expectedSeqNum)
			{
				ProcessMessageWithHighSeqNum(message, msgInSeqNum, expectedSeqNum);
			}
			else
			{
				ProcessMessageWithLowSeqNum(message, msgInSeqNum, expectedSeqNum);
			}
		}

		/// <summary>
		/// Returns true if message is sequence reset and its NewSeqNo is less than expected.
		/// </summary>
		/// <param name="message"> the message </param>
		/// <param name="expectedSeqNum">the expected sequence number</param>
		public static bool IsSeqResetAndNewSeqNoIsLessThanExpected(FixMessage message, long expectedSeqNum)
		{
			return FixMessageUtil.IsSeqReset(message) && message.GetTagValueAsLong(Tags.NewSeqNo) < expectedSeqNum;
		}

		public virtual ISessionSequenceManager GetSequenceManager()
		{
			return ((AbstractFixSession) Session).SequenceManager;
		}

		public virtual void ProcessMessageWithExpectedSeqNum(FixMessage message, long incomingSeqNum)
		{
			if (LogIsTraceEnabled)
			{
				Log.Trace("Store to session incoming seq num: " + incomingSeqNum);
			}
			//getFIXSession().SetAttribute(ExtendedFIXSessionAttribute.IncomingSeqNum, incomingSeqNum);
			ResetLastRrRangeCounter(incomingSeqNum);

			CallNextHandler(message);
		}

		private void ResetLastRrRangeCounter(long incomingSeqNum)
		{
			var endOfRrRange = Session.GetAttributeAsLong(ExtendedFixSessionAttribute.EndOfRrRange);
			if (incomingSeqNum == endOfRrRange)
			{
				var lastRrRange = Session.GetAttribute(ExtendedFixSessionAttribute.LastRrRange);
				if (Log.IsDebugEnabled && lastRrRange != null)
				{
					Log.Debug("Remove Last RR Range");
				}
				Session.RemoveAttribute(ExtendedFixSessionAttribute.LastRrRange.Name);
			}
		}

		/// <summary>
		/// Returns true if message should be processed anyway.
		/// <p/>
		/// The methods returns true only for next message types: logon, logout, SR, RR.
		/// <ul>
		/// <li>logon - will be processed anyway if logon has 34=1 and 141=Y.</li>
		/// <li>logout - will be processed anyway if session has <c>SessionState.WAITING_FOR_LOGOFF</c>.</li>
		/// <li>SR - will be processed if 123=N or 123 does not exist.</li>
		/// <li>RR - will be processed if 43=Y.</li>
		/// </ul>
		/// </summary>
		/// <param name="message"> the message </param>
		/// <returns> boolean </returns>
		public virtual bool IsMsgShouldBeProcessedAnyway(FixMessage message)
		{
			if (FixMessageUtil.IsLogout(message))
			{
				if (Session.SessionState == SessionState.WaitingForLogoff || Session.SessionState == SessionState.WaitingForForcedLogoff)
				{
					return true;
				}
			}

			var anyFlagToProcessLogonAnyway = _ignoreSeqNumTooLowAtLogon || _resetSeqNumFromFirstLogon;

			return FixMessageUtil.IsIgnorableMsg(message) ||
				(anyFlagToProcessLogonAnyway && FixMessageUtil.IsMessageType(message, new[]{ (byte)'A' }));
		}

		/// <summary>
		/// Process message with high seq num.
		/// <p/>
		/// The methods request missing messages.
		/// Note: The incoming seq num will be decremented.
		/// </summary>
		/// <param name="message">        the message </param>
		/// <param name="incomingSeqNum"> the incoming seq num </param>
		/// <param name="expectedSeqNum"> the expected seq num </param>
		public virtual void ProcessMessageWithHighSeqNum(FixMessage message, long incomingSeqNum, long expectedSeqNum)
		{
			if (Log.IsInfoEnabled)
			{
				Log.Info("Incoming seq number: " + incomingSeqNum + " is greater than expected " + expectedSeqNum);
			}
			var isPosDupResendReq = false;
			var sessionSequenceManager = GetSequenceManager();
			if (sessionSequenceManager.SeqResendManager.IsRrRangeActive)
			{
				if (_advancedResendRequestProcessing)
				{
					// The last MsgSeqNum value received and processed
					var tagIndex = message.GetTagIndex(369);
					var outSeqNum = Session.RuntimeState.OutSeqNum;
					if (tagIndex != FixMessage.NotFound && message.GetTagValueAsLongAtIndex(tagIndex) != outSeqNum - 1)
					{
						isPosDupResendReq = true;
					}
				}
			}

			var lastRrSeqNum = sessionSequenceManager.GetRrSequenceFromSession();
			var waitingForRrAnswer = lastRrSeqNum != -1;

			if (lastRrSeqNum < incomingSeqNum)
			{
				Session.FixSessionOutOfSyncListener.OnGapDetected(expectedSeqNum, incomingSeqNum);
			}

			var isLogon = FixMessageUtil.IsLogon(message);
			if (_allowToSendMultipleRr || !waitingForRrAnswer)
			{
				var countOfSentRequests = sessionSequenceManager.GetCountOfSentRequests(expectedSeqNum, incomingSeqNum);
				if (countOfSentRequests < _allowedCountOfSimilarRr)
				{
					//sessionSequenceManager.getSeqResendManager().putMessageIntoBuffer(message.clone());
					if (IncludeNextExpectedMsgSeqNum && isLogon)
					{
						Log.Info("Enabled handleSeqNumAtLogon option - don't send ResendRequest for Logon but set 789 tag");
					}
					else
					{
						sessionSequenceManager.RequestLostMessages(expectedSeqNum, incomingSeqNum, isPosDupResendReq);
					}
				}
				else
				{
					var errorMessage = "Detected possible infinite resend loop for range from " + incomingSeqNum + " to " + expectedSeqNum + " (" + _allowedCountOfSimilarRr + " times)";
					Session.ForcedDisconnect(DisconnectReason.PossibleRrLoop, errorMessage, false);
					throw new RrLoopException(errorMessage, message.ToPrintableString());
				}
			}
			else
			{
				if (!isLogon)
				{
					var endOfRr = sessionSequenceManager.GetEndRangeOfRrSequence();
					if (incomingSeqNum < endOfRr)
					{
						sessionSequenceManager.RequestLostMessages(expectedSeqNum, incomingSeqNum, isPosDupResendReq);
					}
					else
					{
						var resendManager = sessionSequenceManager.SeqResendManager;
						if (!resendManager.IsMessageProcessingFromBufferStarted)
						{
							resendManager.PutMessageIntoBuffer((FixMessage)message.Clone());
						}

					}
				}
			}

			if (isLogon)
			{
				CallNextHandler(message);
			}
			// reduce seq number because current message was ignored
			sessionSequenceManager.DecrementIncomingSeqNumber();
		}

		/// <summary>
		/// Process message with low seq num.
		/// <p/>
		/// Note:
		/// The incoming message will be ignored only if message has 43=Y and incoming seq num is low than expected.
		/// </summary>
		/// <param name="message">        the message </param>
		/// <param name="incomingSeqNum"> the incoming seq num </param>
		/// <param name="expectedSeqNum"> the expected seq num </param>
		public virtual void ProcessMessageWithLowSeqNum(FixMessage message, long incomingSeqNum, long expectedSeqNum)
		{
			if (HasMessageBeenReceivedAlready(message))
			{
				if (!_posDupSmartDelivery)
				{
					GetSequenceManager().DecrementIncomingSeqNumber();
					throw new InvalidMessageException(message, "Message was received with SeqNum that was proceed earlier. " + "Message will be skipped");
				}

				CallNextHandler(message);
			}
			else
			{
				var errorMessage = "Incoming seq number " + incomingSeqNum + " is less then expected " + expectedSeqNum;
				var session = Session;
				if (_resetQueueOnLowSeqNum && FixMessageUtil.IsLogon(message))
				{
					session.ClearQueue();
				}
				session.ForcedDisconnect(DisconnectReason.GotSequenceTooLow, errorMessage, false);
				throw new SequenceToLowException(errorMessage, message.ToPrintableString());
			}
		}

		private bool HasMessageBeenReceivedAlready(FixMessage message)
		{
			return FixMessageUtil.IsPosDup(message) && message.MsgSeqNumber < Session.RuntimeState.InSeqNum;
		}

	//    private void decrementIncomingSeqNumber() {
	//        IExtendedFixSession session = getFIXSession();
	//        FIXSessionRuntimeState runtimeState = session.GetRuntimeState();
	//
	//        runtimeState.DecrementInSeqNum();
	////        long incoming = runtimeState.GetInSeqNum();
	////        runtimeState.SetLastProcessedSeqNum(incoming > 0 ? incoming - 1 : 0);
	//
	//        //SessionParameters parameters = session.GetSessionParametersInstance();
	//        //parameters.decrementIncomingSequenceNumber();
	//        //long incoming = parameters.GetIncomingSequenceNumber();
	//        //parameters.setProcessedIncomingSequenceNumber(incoming > 0 ? incoming - 1 : 0);
	//
	//        session.SetAttribute(ExtendedFIXSessionAttribute.IncomingSeqNum,
	//                session.GetSessionParametersInstance().GetIncomingSequenceNumber());
	//    }
	}

}
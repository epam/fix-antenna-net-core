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
using System.IO;
using System.Threading.Tasks;
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Scheduler;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.PerType.Util;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation.Error;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.PerType
{
	/// <summary>
	/// Logon message handler.
	/// </summary>
	internal class LogonMessageHandler : AbstractSessionMessageHandler
	{
		private const string ErrorMsgSeqNumMustBe1 = "MsgSeqNum must be equal to 1 while resetting the sequence number";
		private const string ErrorIncorrectMsgSeqNum = "Incorrect or undefined MsgSeqNum";
		private const string ErrorNegativeHrtbInterval = "Negative heartbeat interval";
		private const string ErrorIncorrectHrtbInterval = "Incorrect or undefined heartbeat interval";
		private const string ErrorHrtbIntervalMismatch = "Logon HeartBtInt(108) does not match value configured for session";

		private bool _ignoreSeqNumTooLowAtLogon = true;
		private bool _resetSeqNumFromFirstLogon = false;
		private bool _isDisconnectOnHbtMismatchEnabled;

		private readonly TagValue _tempTagValue = new TagValue();

		/// <inheritdoc />
		public override IExtendedFixSession Session
		{
			set
			{
				var configuration = value.Parameters.Configuration;
				var cfg = new ConfigurationAdapter(configuration);
				_ignoreSeqNumTooLowAtLogon = cfg.IgnoreSeqNumTooLowAtLogon;
				_isDisconnectOnHbtMismatchEnabled = configuration.GetPropertyAsBoolean(Config.DisconnectOnLogonHbtMismatch, true);
				if (value is AcceptorFixSession)
				{
					_resetSeqNumFromFirstLogon = cfg.ResetSeqNumFromFirstLogonMode() == ResetSeqNumFromFirstLogonMode.Schedule;
				}

				base.Session = value;
			}
		}

		/// <summary>
		/// If session received the valid logon message and the session has
		/// <c>WAITING_FOR_LOGON</c> state, the handler changed the session state to <c>CONNECTED</c>
		/// <p/>
		/// Error occurred if:<para>
		/// </para>
		/// 1. 108 tag has negative value.<para>
		/// </para>
		/// 2. 141 tag is set and 34 tag has value not equals to 1.<para>
		///
		/// </para>
		/// </summary>
		/// <seealso cref="IFixMessageListener.OnNewMessage(FixMessage)"> </seealso>
		public override void OnNewMessage(FixMessage message)
		{
			Log.Debug("Logon message handler");
			var session = Session;
			session.Parameters.IncomingLoginMessage = (FixMessage)message.Clone();
			var hbtIndex = message.GetTagIndex(Tags.HeartBtInt);
			if (hbtIndex == FixMessage.NotFound)
			{
				DisconnectWithReason(message, session, Tags.HeartBtInt, ErrorIncorrectHrtbInterval);
				_tempTagValue.TagId = Tags.HeartBtInt;
				_tempTagValue.Value = Array.Empty<byte>();
				throw new MessageValidationException(message, _tempTagValue, FixErrorCode.RequiredTagMissing, ErrorIncorrectHrtbInterval, true);
			}

			try
			{
				if (message.GetTagAsInt(Tags.HeartBtInt) < 0)
				{
					DisconnectWithReason(message, session, Tags.HeartBtInt, ErrorNegativeHrtbInterval);
					message.LoadTagValue(Tags.HeartBtInt, _tempTagValue);
					throw new MessageValidationException(message, _tempTagValue, FixErrorCode.ValueIncorrectOutOfRangeForTag, ErrorNegativeHrtbInterval, true);
				}
			}
			catch (MessageValidationException)
			{
				throw;
			}
			catch (System.ArgumentException)
			{
				DisconnectWithReason(message, session, Tags.HeartBtInt, ErrorIncorrectHrtbInterval);
				message.LoadTagValue(Tags.HeartBtInt, _tempTagValue);
				throw new MessageValidationException(message, _tempTagValue, FixErrorCode.IncorrectDataFormatForValue, ErrorIncorrectHrtbInterval, true);
			}

			if (_isDisconnectOnHbtMismatchEnabled && message.GetTagValueAsLongAtIndex(hbtIndex) != session.Parameters.HeartbeatInterval)
			{
				DisconnectWithReason(message, session, Tags.HeartBtInt, ErrorHrtbIntervalMismatch);
				_tempTagValue.TagId = Tags.HeartBtInt;
				_tempTagValue.Value = Array.Empty<byte>();
				throw new MessageValidationException(message, _tempTagValue, FixErrorCode.ValueIncorrectOutOfRangeForTag, ErrorHrtbIntervalMismatch, true);
			}

			var resetSeqNumFlagIndex = message.GetTagIndex(Tags.ResetSeqNumFlag);
			var resetSeqNumFlag = '\u0000';
			if (resetSeqNumFlagIndex != FixMessage.NotFound){
				resetSeqNumFlag = (char) message.GetTagValueAsByteAtIndex(resetSeqNumFlagIndex, 0);
			}

			if (resetSeqNumFlagIndex != FixMessage.NotFound && resetSeqNumFlag != 'n' && resetSeqNumFlag != 'N')
			{
				if (message.GetTagValueLengthAtIndex(resetSeqNumFlagIndex) == 1 && (resetSeqNumFlag == 'y' || resetSeqNumFlag == 'Y'))
				{
					if (!message.IsTagExists(Tags.MsgSeqNum))
					{
						DisconnectWithReason(message, session, Tags.MsgSeqNum, ErrorIncorrectMsgSeqNum);
						message.LoadTagValue(Tags.MsgSeqNum, _tempTagValue);
						throw new MessageValidationException(message, _tempTagValue, FixErrorCode.RequiredTagMissing, ErrorIncorrectMsgSeqNum, true);
					}

					try
					{
						if (message.GetTagAsInt(Tags.MsgSeqNum) != 1)
						{
							DisconnectWithReason(message, session, Tags.MsgSeqNum, ErrorMsgSeqNumMustBe1);
							message.LoadTagValue(Tags.MsgSeqNum, _tempTagValue);
							throw new MessageValidationException(message, _tempTagValue, FixErrorCode.ValueIncorrectOutOfRangeForTag, ErrorMsgSeqNumMustBe1, true);
						}
					}
					catch (MessageValidationException)
					{
						throw;
					}
					catch (System.ArgumentException)
					{
						DisconnectWithReason(message, session, Tags.MsgSeqNum, ErrorIncorrectMsgSeqNum);
						message.LoadTagValue(Tags.MsgSeqNum, _tempTagValue);
						throw new MessageValidationException(message, _tempTagValue, FixErrorCode.ValueIncorrectOutOfRangeForTag, ErrorIncorrectMsgSeqNum, true);
					}

					try
					{
						ResetSeqNumAndSendLogonResponse(message, session);
					}
					catch (IOException e)
					{
						if (Log.IsDebugEnabled)
						{
							Log.Warn($"Storage for session {session.Parameters.SessionId} can't be reset due to error", e);
						}
						else
						{
							Log.Warn($"Storage for session {session.Parameters.SessionId} can't be reset due to error. " + e.Message);
						}
					}
				}
			}
			else
			{
				var resetFromFirstLogonOccurred = false;
				if (_resetSeqNumFromFirstLogon) {
					resetFromFirstLogonOccurred = HandleResetSeqNumFromFirstLogon(message, session);
				}

				var nextExpectedSeqnumIndex = message.GetTagIndex(Tags.NextExpectedMsgSeqNum);
				if (nextExpectedSeqnumIndex != FixMessage.NotFound)
				{
					var nextExpectedSeqNum = message.GetTagValueAsLongAtIndex(nextExpectedSeqnumIndex);
					var nextActualSeqNum = session.RuntimeState.OutSeqNum;

					if (!resetFromFirstLogonOccurred)
					{
						if (nextActualSeqNum > nextExpectedSeqNum)
						{
							//need to resend messages
							session.SetAttribute(ExtendedFixSessionAttribute.IsResendRequestProcessed.Name,
								ExtendedFixSessionAttribute.YesValue);
							try
							{
								session.SetOutOfTurnMode(true);
								//check if queue contains Logon answer (acceptor case) - increase outgoing seq num
								var messageQueue = session.MessageQueue;
								var msg = messageQueue.Poll();
								if (messageQueue.Size > 0 && ("A".Equals(msg.MessageType) || IsLogon(msg)))
								{
									nextActualSeqNum++;
								}

								var gapStart = nextExpectedSeqNum;
								//nextActualSeqNum shows the next (non exist) message
								var gapEnd = nextActualSeqNum - 1;
								session.FixSessionOutOfSyncListener.OnResendRequestReceived(gapStart, gapEnd);

								ExtractAllMessagesAndResend(message, session, gapStart, gapEnd);
							}
							finally
							{
								session.RemoveAttribute(ExtendedFixSessionAttribute.IsResendRequestProcessed.Name);
								session.SetOutOfTurnMode(false);

								session.FixSessionOutOfSyncListener.OnResendRequestProcessed(nextActualSeqNum);
							}
						}
						else if (nextActualSeqNum < nextExpectedSeqNum)
						{
							//remove Logon answer
							session.ClearQueue();
							var errorMessage = "NextExpectedMsgSeqNum(789) request sequence " + nextExpectedSeqNum + " which is higher then actual " + nextActualSeqNum;
							session.ForcedDisconnect(DisconnectReason.InitConnectionProblem, errorMessage, false);
							throw new InvalidMessageException(message, errorMessage);
						}
					}
				}
				else if (_ignoreSeqNumTooLowAtLogon)
				{
					GetSequenceManager().ApplyInSeqNum(message.MsgSeqNumber);
				}
			}

			if (session.SessionState == SessionState.WaitingForLogon)
			{
				session.SessionState = SessionState.Connected; // connected if was waiting for logon
			}

			// It is recommended to wait a short period of time following the Logon or to send a TestRequest and
			// wait for a response to it before sending queued or new messages in order to Allow both sides to
			// handle resend request processing.
			DoDelay(session);
		}

		private bool HandleResetSeqNumFromFirstLogon(FixMessage message, IExtendedFixSession session)
		{
			if (Log.IsTraceEnabled) {
				Log.Trace("Check if the reset from first Logon is required for session " + Session.Parameters.SessionId);
			}

			var acceptorFixSession = (AcceptorFixSession) session;
			var expectedInMsgSeqNum = message.MsgSeqNumber;
			var nextExpectedSeqNumIndex = message.GetTagIndex(Tags.NextExpectedMsgSeqNum);
			var expectedOutMsgSeqNum = nextExpectedSeqNumIndex != FixMessage.NotFound ?
				message.GetTagValueAsLongAtIndex(nextExpectedSeqNumIndex) : 1;

			var currentInSeqNum = GetSequenceManager().GetExpectedIncomingSeqNumber();
			var currentOutSeqNum = session.RuntimeState.OutSeqNum;

			var result = false;
			if (currentInSeqNum != expectedInMsgSeqNum || currentOutSeqNum != expectedOutMsgSeqNum) {
				result = HandleTradingSessionParameters(message,
					acceptorFixSession,
					expectedInMsgSeqNum, expectedOutMsgSeqNum);
			}

			if (!result && expectedInMsgSeqNum < currentInSeqNum) {
				var errorMessage = $"Incoming seq number {expectedInMsgSeqNum} is less then expected {currentInSeqNum}";
				session.ForcedDisconnect(DisconnectReason.GotSequenceTooLow, errorMessage, false);
				throw new SequenceToLowException(errorMessage, message.ToPrintableString());
			}

			return result;
		}

		private bool HandleTradingSessionParameters(
			FixMessage message, AcceptorFixSession acceptorFixSession,
			long expectedInMsgSeqNum, long expectedOutMsgSeqNum)
		{
			var restoredInSeqNum = (long?) acceptorFixSession.GetAttribute(ExtendedFixSessionAttribute.SequenceWasDecremented.Name);
			var currentInSeqNum = restoredInSeqNum ?? acceptorFixSession.RuntimeState.InSeqNum;
			var currentOutSeqNum = acceptorFixSession.RuntimeState.OutSeqNum;
			var firstSessionConnection = currentInSeqNum == 1 && currentOutSeqNum == 1;

			var config = acceptorFixSession.ConfigAdapter;
			var schedule = new Schedule(config.TradePeriodBegin, config.TradePeriodEnd, config.TradePeriodTimeZone);

			if (schedule.IsTradingPeriodDefined())
			{
				if (IsLastResetOccurredAfterPeriodEnd(acceptorFixSession, schedule))
				{
					var reason = "Illegal state on handle of reset sequence numbers from first logon: last reset is after scheduled end of trading period";
					OnFailedSeqNumResetFromFirstLogon(message, acceptorFixSession, reason, null);
				}
				else if (!IsLastResetOccurredInTradingPeriod(acceptorFixSession, schedule) || firstSessionConnection)
				{
					if (Log.IsTraceEnabled)
					{
						Log.Trace("Trade period is defined for session " + Session.Parameters.SessionId +
							" and needs sequence reset (first connect in this period)");
					}

					return ResetSequencesFromFirstLogon(message, acceptorFixSession, expectedInMsgSeqNum, expectedOutMsgSeqNum);
				}
				else
				{
					if (Log.IsTraceEnabled)
					{
						Log.Trace("No needs reset for session " + Session.Parameters.SessionId +
							" in this trading period (it was done already)");
					}
				}
			}
			else if (schedule.IsOnlyPeriodBeginDefined())
			{
				if (!IsLastResetOccurredAfterPeriodBegin(acceptorFixSession, schedule) || firstSessionConnection)
				{
					if (Log.IsTraceEnabled)
					{
						Log.Trace("Trade period begins for session " + Session.Parameters.SessionId +
							" and needs sequence reset (first connect in this period)");
					}

					return ResetSequencesFromFirstLogon(message, acceptorFixSession, expectedInMsgSeqNum, expectedOutMsgSeqNum);
				}
				else
				{
					if (Log.IsTraceEnabled)
					{
						Log.Trace("Trade period begins for session " + Session.Parameters.SessionId +
							" but reset was done before");
					}
				}
			}
			else
			{
				var reason = "Trading period is not defined for the session with enabled reset sequence from first logon. Session: "
						+ acceptorFixSession.Parameters.SessionId;
				Log.Warn(reason);
				return true;
			}

			return false;
		}

		private bool ResetSequencesFromFirstLogon(
			FixMessage message, AcceptorFixSession acceptorFixSession, long expectedInMsgSeqNum, long expectedOutMsgSeqNum)
		{
			try {
				if (Log.IsTraceEnabled) {
					Log.Trace("Do reset in/out sequences from first logon in session {} to {}:{}" + acceptorFixSession.Parameters.SessionId
						+ " to " + expectedInMsgSeqNum + ":" + expectedOutMsgSeqNum);
				}

				GetSequenceManager().ConfigureStateBeforeReset();
				acceptorFixSession.SetSequenceNumbers(expectedInMsgSeqNum, expectedOutMsgSeqNum);
				acceptorFixSession.Parameters.LastSeqNumResetTimestamp = DateTimeHelper.CurrentMilliseconds;
				Log.Info("In/out sequences were reset from first logon in session " + acceptorFixSession.Parameters.SessionId
					+ " to " + expectedInMsgSeqNum + ":" + expectedOutMsgSeqNum);
				return true;
			} catch (IOException e) {
				var reason = "Error on handle of reset sequence numbers from first logon.";
				OnFailedSeqNumResetFromFirstLogon(message, acceptorFixSession, reason + " Cause: " + e.Message, e);
			}

			return false;
		}

		private void OnFailedSeqNumResetFromFirstLogon(FixMessage message, AcceptorFixSession acceptorFixSession, string reason, Exception e)
		{
			if (e != null) {
				Log.Error(reason, e);
			} else {
				Log.Error(reason);
			}

			acceptorFixSession.ClearQueue();
			acceptorFixSession.Disconnect(DisconnectReason.InvalidMessage, reason);
			throw new InvalidMessageException(message, reason, true);
		}

		private bool IsLastResetOccurredInTradingPeriod(AcceptorFixSession acceptorFixSession, Schedule schedule)
		{
			return schedule.IsTimestampInTradingPeriod(acceptorFixSession.Parameters.LastSeqNumResetTimestamp);
		}

		private bool IsLastResetOccurredAfterPeriodBegin(AcceptorFixSession acceptorFixSession, Schedule schedule)
		{
			return schedule.IsTimestampAfterTradingPeriodBegin(acceptorFixSession.Parameters.LastSeqNumResetTimestamp);
		}

		private bool IsLastResetOccurredAfterPeriodEnd(AcceptorFixSession acceptorFixSession, Schedule schedule) {
			return schedule.IsTimestampAfterTradingPeriodEnd(acceptorFixSession.Parameters.LastSeqNumResetTimestamp);
		}

		private bool IsLogon(FixMessageWithType msgWithType)
		{
			var message = msgWithType.FixMessage;
			if (message == null)
			{
				return false;
			}
			var msgType = message.MsgType;
			return msgType != null && RawFixUtil.IsLogon(msgType);
		}

		private void DisconnectWithReason(FixMessage message, IExtendedFixSession session, int refTagId, string reason)
		{
			var list = session.MessageFactory.GetRejectForMessageTag(message, refTagId, 5, reason);
			session.ClearQueue();
			session.SendMessageOutOfTurn(MsgType.Reject, list);
			session.Disconnect(DisconnectReason.InvalidMessage, reason);
			//throw new InvalidMessageException(message, reason);
		}

		private void ResetSeqNumAndSendLogonResponse(FixMessage message, IExtendedFixSession session)
		{
			//it's indicate that counterparty reset their sequences - let's reset our incoming too!
			GetSequenceManager().ResetSequencesOnLogon();
			var isReset = false;
			var currentIncSeqNum = session.RuntimeState.InSeqNum;
			if (currentIncSeqNum > 1)
			{
				session.Parameters.IncomingSequenceNumber = 1;
				isReset = true;
			}
			GetSequenceManager().RemoveRangeOfRrSequence();

			if (session.GetAttribute(ExtendedFixSessionAttribute.IntradayLogonIsSent.Name) != null)
			{
				// response received, we were initiator of intraday logon
				session.RemoveAttribute(ExtendedFixSessionAttribute.IntradayLogonIsSent.Name);
			}
			else
			{
				//we are acceptor of Logon with seqnum reset
				if (isReset)
				{
					if (SessionState.IsConnected(session.SessionState))
					{
						try
						{
							session.ResetSequenceNumbers(false);
							session.RemoveAttribute(ExtendedFixSessionAttribute.IntradayLogonIsSent.Name);
						}
						catch (IOException e)
						{
							var reason = "Error on process intraday logon.";
							Log.Error(reason + " Cause: " + e.Message, e);
							session.Disconnect(DisconnectReason.InvalidMessage, reason);
							throw new InvalidMessageException(message, reason, true);
						}
					}
					else
					{
						((AbstractFixSession) session).BackupStorages();
					}
				}
			}
		}

		private void DoDelay(IExtendedFixSession session)
		{
			var sleep = session.Parameters.Configuration.GetPropertyAsInt(Config.MaxDelayToSendAfterLogon, 50);
			if (sleep < 0)
				sleep = 50;

			Task.Delay(sleep).ContinueWith((t) =>
			{
				if (session.GetAttribute(ExtendedFixSessionAttribute.IsResendRequestProcessed.Name) == null)
				{
					session.SetOutOfTurnMode(false);
					Log.Debug("Login timeout finished, turn of mode");
				}
			}).ConfigureAwait(false);
		}

		private ISessionSequenceManager GetSequenceManager()
		{
			return ((AbstractFixSession) Session).SequenceManager;
		}

		/// <summary>
		/// Handle the resend request.
		/// </summary>
		/// <param name="resendRequestMessage"> original ResendRequest </param>
		/// <param name="fixSession">           current FIXSession object </param>
		/// <param name="beginSeqNum">          start of requested interval </param>
		/// <param name="endSeqNum">            end of requested interval </param>
		public virtual void ExtractAllMessagesAndResend(FixMessage resendRequestMessage, IExtendedFixSession fixSession, long beginSeqNum, long endSeqNum)
		{
			var storageExtractor = new StorageExtractor(fixSession, resendRequestMessage.GetTagValueAsBytes(Tags.SendingTime), beginSeqNum, endSeqNum);
			storageExtractor.ExtractAllMessagesAndResend();
		}
	}
}
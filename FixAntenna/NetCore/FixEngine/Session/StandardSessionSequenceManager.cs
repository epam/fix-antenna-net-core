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
using System.Text;
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Collections;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.FixEngine.Storage;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	internal sealed class StandardSessionSequenceManager : ISessionSequenceManager
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(StandardSessionSequenceManager));
		private const long MillisInDay = 24 * 60 * 60 * 1000;
		private readonly bool _logIsTraceEnabled = Log.IsTraceEnabled;

		private AbstractFixSession _session;
		private SessionParameters _sessionParameters;
		private FixSessionRuntimeState _runtimeState;
		private IStorageFactory _storageFactory;
		private ConfigurationAdapter _configAdapter;
		private bool _ignoreResetSeqNumFlagOnReset;

		public StandardSessionSequenceManager(AbstractFixSession session)
		{
			Init(session);
		}

		/// <inheritdoc />
		public void Reinit(AbstractFixSession session)
		{
			Init(session);
		}

		private void Init(AbstractFixSession session)
		{
			_session = session;
			_storageFactory = session.StorageFactory;
			_sessionParameters = session.Parameters;
			_runtimeState = session.RuntimeState;
			_configAdapter = session.ConfigAdapter;
			SeqResendManager = CreateResendManagerForSession();
			_ignoreResetSeqNumFlagOnReset = _configAdapter.Configuration.GetPropertyAsBoolean(Config.IgnoreResetSeqNumFlagOnReset, false);
		}

		/// <summary>
		/// Gets sequence resend manager.
		/// </summary>
		/// <value> SequenceResendManager </value>
		public ISequenceResendManager SeqResendManager { get; private set; }

		private ISequenceResendManager CreateResendManagerForSession()
		{
			long maxRequestResend = _configAdapter.Configuration.GetPropertyAsInt(Config.MaxRequestResendInBlock, 0);
			if (maxRequestResend > 0)
			{
				return new BlockSequenceResendManager(this, maxRequestResend);
			}

			return new FreeRangeSequenceResendManager(this);
		}

		/// <summary>
		/// Gets expected sequence number.
		/// <para>
		/// If in the session <see cref="ExtendedFixSessionAttribute.LastRrSeqNum"/> attribute is set the seq number takes from it;
		/// Otherwise if processed seq number is set in the session parameters the expected seq num takes from it;
		/// Otherwise the seq number takes from incoming seq num.</para>
		/// </summary>
		public long GetExpectedIncomingSeqNumber()
		{
			var expectedSeqNum = _runtimeState.InSeqNum;
			var processedSeqNum = _runtimeState.LastProcessedSeqNum;

			if (processedSeqNum != 0)
			{
				if (_logIsTraceEnabled)
				{
					Log.Trace("Get expected seq num from processed seq num: " + (processedSeqNum + 1));
				}
				return processedSeqNum + 1; // get next seq num
			}

			if (_logIsTraceEnabled)
			{
				Log.Trace("Get expected seq num from incoming seq num: " + expectedSeqNum);
			}
			return expectedSeqNum;
		}

		private bool IsExpectedSeqNum(long? lastSeqId, long? seqNum)
		{
			return seqNum != null && lastSeqId.Equals(seqNum);
		}

		/// <inheritdoc />
		public void DecrementIncomingSeqNumber()
		{
			var runtimeState = _session.RuntimeState;

			runtimeState.DecrementInSeqNum();
		}

		/// <summary>
		/// Store the last processed seq number.
		/// <para>The method work only when last processed and last incoming seq nums are not equals.</para>
		/// </summary>
		public void SaveProcessedSeqNumberOnShutdown()
		{
			if (IsProcessedSequenceInvalid())
			{
				// ideal situation when last precessed seq num eq last incoming seq num,
				// otherwise engine should save the last processed value.
				SaveCurrentProcessedSequence();
			}
		}

		private bool IsProcessedSequenceInvalid()
		{
			var processSeqNum = _runtimeState.LastProcessedSeqNum;
			if (processSeqNum == 0)
			{
				return false;
			}
			var lastIncomingSeqNum = _runtimeState.InSeqNum;
			return !(processSeqNum == lastIncomingSeqNum - 1);
		}

		/// <summary>
		/// Saves the processed seq ID.
		/// </summary>
		/// <exception cref="IOException"> if error occurred </exception>
		private void SaveCurrentProcessedSequence()
		{
			try
			{
				SaveRestoredSequences();

				_session.SetAttribute(ExtendedFixSessionAttribute.DeleteLastProcessedSeqNumFromFile, true);
			}
			catch (Exception e)
			{
				_session.ErrorHandler.OnWarn("Error on save processed seq id.", e);
			}
		}

		/// <summary>
		/// Save session parameters in file.
		/// If session attribute <see cref="ExtendedFixSessionAttribute.LastRrSeqNum"/> is set the incoming seq num will be saved;
		/// And if <c>sessionParameters.GetProcessedIncomingSequenceNumber()</c> is set the last processed seq num will be saved;
		/// </summary>
		/// <exception cref="IOException"> if error occurred </exception>
		public void SaveSessionParameters()
		{
			var sessionParameters = GetSessionParametersForSaving(_session.Parameters);

			var stateToStore = new FixSessionRuntimeState();
			//stateToStore.SetOutSeqNum(0);
			if (!Common.Constants.IsNull(_session.GetAttributeAsLong(ExtendedFixSessionAttribute.LastRrSeqNum)))
			{
				stateToStore.InSeqNum = _runtimeState.InSeqNum;
			}

			stateToStore.LastProcessedSeqNum = _runtimeState.LastProcessedSeqNum;

			_storageFactory.SaveSessionParameters(sessionParameters, stateToStore);
		}

		/// <summary>
		/// Restore session parameters file.
		/// </summary>
		/// <exception cref="IOException"> if error occurred </exception>
		public void SaveRestoredSequences()
		{
			var stateToStore = new FixSessionRuntimeState();
			stateToStore.InSeqNum = 0;
			stateToStore.OutSeqNum = 0;
			stateToStore.LastProcessedSeqNum = _runtimeState.LastProcessedSeqNum;
			_storageFactory.SaveSessionParameters(_sessionParameters, stateToStore);
		}

		public SessionParameters GetSessionParametersForSaving(SessionParameters sessionParameters)
		{
			var clonedParameters = new SessionParameters();
			clonedParameters.SenderCompId = sessionParameters.SenderCompId;
			clonedParameters.TargetCompId = sessionParameters.TargetCompId;
			clonedParameters.SessionQualifier = sessionParameters.SessionQualifier;
			clonedParameters.Port = sessionParameters.Port;
			clonedParameters.Host = sessionParameters.Host;
			clonedParameters.FixVersionContainer = sessionParameters.FixVersionContainer;
			clonedParameters.AppVersionContainer = sessionParameters.AppVersionContainer;
			clonedParameters.LastSeqNumResetTimestamp = sessionParameters.LastSeqNumResetTimestamp;
			if (sessionParameters.IsCustomSessionId)
			{
				clonedParameters.SetSessionId(sessionParameters.SessionId.ToString());
			}

			clonedParameters.IncomingSequenceNumber = sessionParameters.IncomingSequenceNumber;
			clonedParameters.OutgoingSequenceNumber = sessionParameters.OutgoingSequenceNumber;
			return clonedParameters;
		}

		/// <inheritdoc />
		public void RestoreSessionParameters()
		{
			SaveRestoredSequences();
		}

		private void BackupSessionStorage()
		{
			_session.BackupStorages();
			_sessionParameters.LastSeqNumResetTimestamp = DateTimeHelper.CurrentMilliseconds;
			RestoreSessionParameters();
		}

		/// <summary>
		/// Loads sequences from file.
		/// </summary>
		public void LoadStoredParameters()
		{

			var parameters = CreateSessionParameters();
			SetBasicSessionParameters(parameters);

			var storedState = new FixSessionRuntimeState();
			//loading persisted properties
			if (_session.StorageFactory.LoadSessionParameters(parameters, storedState))
			{

				ProcessInSeqNum(storedState.InSeqNum);
				SetSessionParametersOverrides(_session.Parameters, parameters);
			}
		}


		private void SetBasicSessionParameters(SessionParameters parameters)
		{
			var sessionParametersInstance = _session.Parameters;
			parameters.SenderCompId = sessionParametersInstance.SenderCompId;
			parameters.TargetCompId = sessionParametersInstance.TargetCompId;
			if (sessionParametersInstance.IsCustomSessionId)
			{
				parameters.SetSessionId(sessionParametersInstance.SessionId.ToString());
			}
		}

		private void ProcessInSeqNum(long storedInSeqNum)
		{
			if (storedInSeqNum > 0)
			{
				_session.SetAttribute(ExtendedFixSessionAttribute.LastRrSeqNum, _runtimeState.InSeqNum);
				if (Log.IsDebugEnabled)
				{
					Log.Debug("Attribute '" + ExtendedFixSessionAttribute.LastRrSeqNum.Name + "' is set, value - " + storedInSeqNum);
				}
				_runtimeState.InSeqNum = storedInSeqNum;
			}
		}

		public void SetSessionParametersOverrides(SessionParameters sessionParameters, SessionParameters inParameters)
		{
			sessionParameters.LastSeqNumResetTimestamp = inParameters.LastSeqNumResetTimestamp;
			if (!sessionParameters.IsSetInSeqNumsOnNextConnect)
			{
				sessionParameters.IncomingSequenceNumber = inParameters.IncomingSequenceNumber;
			}
			if (!sessionParameters.SetOutSeqNumsOnNextConnect)
			{
				sessionParameters.OutgoingSequenceNumber = inParameters.OutgoingSequenceNumber;
			}
		}

		public SessionParameters CreateSessionParameters()
		{
			return new SessionParameters();
		}

		/// <summary>
		/// Gets RR sequence from session attribute.
		/// </summary>
		/// <returns> RR num or null. </returns>
		public long GetRrSequenceFromSession()
		{
			return _session.GetAttributeAsLong(ExtendedFixSessionAttribute.LastRrSeqNum);
		}

		/// <summary>
		/// Saves the RR seq.
		/// </summary>
		/// <param name="lastRrSeq"> the resend request sequence </param>
		/// <exception cref="IOException"> if error occurred </exception>
		public void SaveRrSequence(long lastRrSeq)
		{
			try
			{
				_session.SetAttribute(ExtendedFixSessionAttribute.LastRrSeqNum, lastRrSeq);

				var stateToStore = new FixSessionRuntimeState();
				stateToStore.OutSeqNum = 0;
				stateToStore.InSeqNum = lastRrSeq;
				stateToStore.LastProcessedSeqNum = _runtimeState.LastProcessedSeqNum;

				_storageFactory.SaveSessionParameters(_sessionParameters, stateToStore);

				if (Log.IsDebugEnabled)
				{
					Log.Debug("Attribute '" + ExtendedFixSessionAttribute.LastRrSeqNum.Name + "' was stored, value: " + lastRrSeq);
				}
			}
			catch (Exception e)
			{
				_session.ErrorHandler.OnWarn("Error on remove attribute '" + ExtendedFixSessionAttribute.LastRrSeqNum.Name + "'.", e);
			}
		}

		/// <summary>
		/// Remove RR seq from session.
		/// </summary>
		public void RemoveRrSequenceFromSession(long? lastSeqId)
		{
			_session.RemoveLongAttribute(ExtendedFixSessionAttribute.LastRrSeqNum);
			if (Log.IsDebugEnabled)
			{
				Log.Debug("Attribute '" + ExtendedFixSessionAttribute.LastRrSeqNum.Name + "' was removed, value: " + lastSeqId.Value);
			}
			try
			{
				SaveRestoredSequences();
			}
			catch (IOException e)
			{
				_session.ErrorHandler.OnWarn("Error on restore session parameters.", e);
			}
		}


		private void SaveRequestSequenceRange(long startOfRrRange, long endOfRrRange)
		{
			if (Log.IsDebugEnabled)
			{
				Log.Debug("Save RR range " + ExtendedFixSessionAttribute.StartOfRrRange.Name + ": " + startOfRrRange + ", " + ExtendedFixSessionAttribute.EndOfRrRange.Name + ": " + endOfRrRange);
			}
			_session.SetAttribute(ExtendedFixSessionAttribute.StartOfRrRange, startOfRrRange);
			_session.SetAttribute(ExtendedFixSessionAttribute.EndOfRrRange, endOfRrRange);
		}

		/// <inheritdoc />
		public void RemoveRangeOfRrSequence()
		{
			var end = _session.GetAttributeAsLong(ExtendedFixSessionAttribute.EndOfRrRange);
			if (Log.IsDebugEnabled)
			{
				var start = _session.GetAttributeAsLong(ExtendedFixSessionAttribute.StartOfRrRange);
				var sb = new StringBuilder("Remove RR range ");
				if (Common.Constants.IsNull(end) || Common.Constants.IsNull(start))
				{
					sb.Append("session attributes");
				}
				else
				{
					sb.Append(ExtendedFixSessionAttribute.StartOfRrRange.Name).Append(": ").Append(start);
					sb.Append(", ").Append(ExtendedFixSessionAttribute.EndOfRrRange.Name).Append(": ").Append(end);
				}
				Log.Debug(sb.ToString());
			}
			_session.RemoveLongAttribute(ExtendedFixSessionAttribute.StartOfRrRange);
			_session.RemoveLongAttribute(ExtendedFixSessionAttribute.EndOfRrRange);
			_session.RemoveLongAttribute(ExtendedFixSessionAttribute.RequestedEndRrRange);
			_session.RemoveLongAttribute(ExtendedFixSessionAttribute.LastRrRange);
			//_session.RemoveLongAttribute(ExtendedFixSessionAttribute.SimilarRrCounter); - this property we can't remove - it is requred for the case of repetitive RR (autoBreakInfiniteRRLoop.xml)
			_session.RemoveLongAttribute(ExtendedFixSessionAttribute.LastRrSeqNum);
			_session.FixSessionOutOfSyncListener.OnGapClosed(end);
		}

		/// <summary>
		/// Gets start of RR range.
		/// </summary>
		/// <returns> start of range or -1 </returns>
		public long GetStartRangeOfRrSequence()
		{
			var startOfRange = _session.GetAttributeAsLong(ExtendedFixSessionAttribute.StartOfRrRange);
			if (Common.Constants.IsNull(startOfRange))
			{
				return -1;
			}
			return startOfRange;
		}

		/// <summary>
		/// Gets end of RR range.
		/// </summary>
		/// <returns> end of range or -1 </returns>
		public long GetEndRangeOfRrSequence()
		{
			var endOfRange = _session.GetAttributeAsLong(ExtendedFixSessionAttribute.EndOfRrRange);
			if (Common.Constants.IsNull(endOfRange))
			{
				return -1;
			}
			return endOfRange;
		}

		/// <summary>
		/// Returns true if <c>seqNum</c> in range.
		/// </summary>
		public bool IsSequenceInRange(long seqNum)
		{
			var start = GetStartRangeOfRrSequence();
			var end = GetEndRangeOfRrSequence();
			return seqNum >= start && (seqNum <= end || end == 0);
		}

		/// <summary>
		/// Returns true if range exists.
		/// </summary>
		public bool IsRRangeExists()
		{
			return !Common.Constants.IsNull(_session.GetAttributeAsLong(ExtendedFixSessionAttribute.EndOfRrRange)) && !Common.Constants.IsNull(_session.GetAttributeAsLong(ExtendedFixSessionAttribute.StartOfRrRange));
		}

		/// <inheritdoc />
		//TODO: improve method name
		public bool IsRrSequenceActive()
		{
			var lastRrSeq = GetRrSequenceFromSession();
			if (Common.Constants.IsNull(lastRrSeq))
			{
				return false;
			}
			return true;
		}

		/// <inheritdoc />
		public void UpdateEndOfRrRange(long incomingSeqNum)
		{
			var endRange = GetEndRangeOfRrSequence();
			if (endRange < incomingSeqNum)
			{
				SaveRequestSequenceRange(GetStartRangeOfRrSequence(), incomingSeqNum);
			}
		}

		/// <inheritdoc />
		public void UpdateLastRrSequence(long? msgSeqNum)
		{
			var lastRrSeq = GetRrSequenceFromSession();
			var endOfRr = GetEndRangeOfRrSequence();
			if (Log.IsTraceEnabled)
			{
				_session.SetAttribute(ExtendedFixSessionAttribute.LastRrSeqNum, msgSeqNum);
				Log.Trace("Update RR seq num: " + msgSeqNum);
			}

			var enhancedResendLogic = _session.Parameters.Configuration
				.GetPropertyAsBoolean(Config.EnhancedCmeResendLogic);

			bool canRemoveRrSequence;

			if (enhancedResendLogic)
			{
				canRemoveRrSequence = IsExpectedSeqNum(lastRrSeq, msgSeqNum) && msgSeqNum == endOfRr;
			}
			else
			{
				canRemoveRrSequence = IsExpectedSeqNum(lastRrSeq, msgSeqNum);
			}

			if (canRemoveRrSequence)
			{
				RemoveRrSequenceFromSession(lastRrSeq);
			}
		}

		/// <summary>
		/// Reset sequences in file.
		/// </summary>
		public void ResetSequencesOnRequest(long msgSeqNum)
		{
			ResetSequences();

			var endOfSeqNumRange = GetEndRangeOfRrSequence();
			if (msgSeqNum >= endOfSeqNumRange)
			{
				RemoveRangeOfRrSequence();
				_session.SetOutOfTurnMode(false);
			}

			var inSeqNum = msgSeqNum - 1; // -1 because number will be incremented after this message
			_runtimeState.InSeqNum = inSeqNum;

			if (Log.IsTraceEnabled)
			{
				Log.Trace("Save processed seq num to session:" + inSeqNum);
			}
			_runtimeState.LastProcessedSeqNum = inSeqNum > 0 ? inSeqNum - 1 : 0;

			SeqResendManager.SkipMessagesFromBufferTillSeqNum(_runtimeState.InSeqNum + 1);
		}

		/// <inheritdoc />
		public void ResetSequencesOnLogon()
		{
			//it's indicate that counterparty reset their sequences - let's reset our incoming too!
			_runtimeState.InSeqNum = 1;
			_runtimeState.LastProcessedSeqNum = 0;
			if (_session.GetAttribute(ExtendedFixSessionAttribute.IntradayLogonIsSent.Name) != null)
			{
				// response received, we were initiator of intraday logon
				_session.RemoveAttribute(ExtendedFixSessionAttribute.IntradayLogonIsSent.Name);
			}
			else
			{
				//we are acceptor of Logon with seqnum reset
				if (SessionState.IsConnected(_session.SessionState))
				{
					_session.ResetSequenceNumbers(false);
					_session.RemoveAttribute(ExtendedFixSessionAttribute.IntradayLogonIsSent.Name);

				}
			}
		}

		private void ResetSequences()
		{
			try
			{
				_session.RemoveAttribute(ExtendedFixSessionAttribute.DeleteLastProcessedSeqNumFromFile);
				if (_logIsTraceEnabled)
				{
					var processedSeqNum = _session.RuntimeState.LastProcessedSeqNum;
					Log.Trace("Attribute '" + ExtendedFixSessionAttribute.DeleteLastProcessedSeqNumFromFile + "' was removed, value: " + processedSeqNum);
				}
				var lastRrSeqNum = _session.GetAttributeAsLong(ExtendedFixSessionAttribute.LastRrSeqNum);
				_session.RemoveAttribute(ExtendedFixSessionAttribute.LastRrSeqNum);
				if (Log.IsDebugEnabled)
				{
					Log.Debug("Attribute '" + ExtendedFixSessionAttribute.LastRrSeqNum.Name + "' " + (lastRrSeqNum == 0 ? "was removed from session" : ("was removed from session, value: " + lastRrSeqNum)));
				}
				SaveRestoredSequences();
			}
			catch (IOException e)
			{
				_session.ErrorHandler.OnWarn("Error on restore session parameters.", e);
			}
		}

		/// <inheritdoc />
		public bool DoAfterMessageProcessActions()
		{
			// commented, these code invalid - seq num from attribute invalid, because: ignorable message Allow to set invalid seq num
			// Long incomingSeqNum = getSequenceManager().getIncomingSequenceFromSessionAttribute();
			var incomingSeqNum = _runtimeState.InSeqNum;
			var lastProcessedSeqNum = _runtimeState.LastProcessedSeqNum;
			if (incomingSeqNum != 0 && lastProcessedSeqNum != 0)
			{
				if (incomingSeqNum != (lastProcessedSeqNum + 1))
				{
					// don't increment last processed seq num
					SaveCurrentProcessedSequence();
					return false; // do not increment incoming sequence
				}

				IncrementProcessedSequence();
				return true;
			}

			IncrementProcessedSequence();
			return true;
		}

		/// <summary>
		/// Increment processed sequence.
		/// </summary>
		private void IncrementProcessedSequence()
		{
			//removeIncomingSequenceFromSession();
			var inSeqNum = _runtimeState.InSeqNum;
			if (_logIsTraceEnabled)
			{
				Log.Trace("Save processed seq num to session: " + inSeqNum);
			}
			_runtimeState.LastProcessedSeqNum = inSeqNum;

			CleanDeleteLastProcessedSeqNumFromFileFlag();
		}

		private void CleanDeleteLastProcessedSeqNumFromFileFlag()
		{
			if (_session.GetAttributeAsBool(ExtendedFixSessionAttribute.DeleteLastProcessedSeqNumFromFile))
			{
				try
				{
					if (_logIsTraceEnabled)
					{
						Log.Trace("Remove processed seq num from file: " + _runtimeState.LastProcessedSeqNum);
					}
					SaveRestoredSequences();
					_session.RemoveAttribute(ExtendedFixSessionAttribute.DeleteLastProcessedSeqNumFromFile);
				}
				catch (Exception)
				{
				}
			}
		}


		private void RequestNextMessagePart(long startRange, long end, bool posDup)
		{
			SaveRrSequence(startRange);

			_session.SetAttribute(ExtendedFixSessionAttribute.RequestedEndRrRange, end);
			if (Log.IsDebugEnabled)
			{
				Log.Debug("Storing RequestedEndRrRange attribute:" + end);
			}

			if (!_session.GetAttributeAsBool(ExtendedFixSessionAttribute.RejectSession))
			{
				SendRequestNextMessagePart(startRange, end, posDup);
			}
		}

		private void SendRequestNextMessagePart(long startRange, long end, bool posDup)
		{
			var msg = new FixMessage();
			var msgFactory = _session.MessageFactory;

			if (posDup)
			{
				msg.AddTag(Tags.PossDupFlag, true);
				msg.AddTag(Tags.OrigSendingTime, msgFactory.GetCurrentSendingTime());
			}

			msg.SetPaddedLongTag(Tags.BeginSeqNo, startRange, msgFactory.MinSeqNumFieldsLength);
			msg.SetPaddedLongTag(Tags.EndSeqNo, end, msgFactory.MinSeqNumFieldsLength);

			_session.SendMessageOutOfTurn("2", msg);
			_session.FixSessionOutOfSyncListener.OnResendRequestSent(startRange, end);
		}

		/// <inheritdoc />
		public long GetCountOfSentRequests(long startRange, long endRange)
		{
			var currentRange = BuildLastRrRangeVal(startRange, endRange);
			var val = (string)_session.GetAttribute(ExtendedFixSessionAttribute.LastRrRange);
			if (val  != null && currentRange.Equals(val))
			{
				return _session.GetAttributeAsLong(ExtendedFixSessionAttribute.SimilarRrCounter);
			}
			return 0;
		}

		private void IncreaseRrCounter(long startRange, long endRange)
		{
			var currentRange = BuildLastRrRangeVal(startRange, endRange);
			var val = (string)_session.GetAttribute(ExtendedFixSessionAttribute.LastRrRange);
			if (ReferenceEquals(val, null) || !currentRange.Equals(val))
			{
				_session.SetAttribute(ExtendedFixSessionAttribute.LastRrRange, currentRange);
				_session.SetAttribute(ExtendedFixSessionAttribute.SimilarRrCounter, 1);
			}
			else
			{
				var rrCounter = _session.GetAttributeAsLong(ExtendedFixSessionAttribute.SimilarRrCounter);
				_session.SetAttribute(ExtendedFixSessionAttribute.SimilarRrCounter, rrCounter + 1);
			}
		}

		private string BuildLastRrRangeVal(long startRange, long end)
		{
			var b = new StringBuilder();
			b.Append(startRange).Append('-').Append(end);
			return b.ToString();
		}

		/// <inheritdoc />
		public void RequestLostMessages(long expectedSeqNum, long incomingSeqNum, bool posDup)
		{
			IncreaseRrCounter(expectedSeqNum, incomingSeqNum);
			SeqResendManager.RequestLostMessages(expectedSeqNum, incomingSeqNum, posDup);
		}

		/// <inheritdoc />
		public void InitSeqNums(long inStorageSeqNum, long nextOutStorageSeqNum)
		{
			if (_session is InitiatorFixSession)
			{
				if (ApplyForceSeqNumReset())
				{
					return;
				}
			}
			else
			{
				//acceptor
				if (InLogonHasResetFlag())
				{
					ResetSeqNumAndOptionallySetResetSeqNumFlag();
					if (Log.IsTraceEnabled)
					{
						Log.Trace("Reset sequences by incoming LOGON ResetSeqNum(141)");
					}
					return;
				}
			}

			if (OutLogonHasResetFlag())
			{
				ResetSeqNumAndSetResetSeqNumFlag();
				if (Log.IsTraceEnabled)
				{
					Log.Trace("Reset sequences by outgoing LOGON ResetSeqNum(141)");
				}
				return;
			}

			if (ApplyIntraDayReset())
			{
				if (Log.IsTraceEnabled)
				{
					Log.Trace("Apply intra day reset");
				}
				return;
			}

			if (ApplyResetByTime())
			{
				if (Log.IsTraceEnabled)
				{
					Log.Trace("Apply daily reset");
				}
				return;
			}

			if (ApplyResetOnNextConnect())
			{
				if (Log.IsTraceEnabled)
				{
					Log.Trace("Apply reset sequences for next connect (by session parameters)");
				}
				return;
			}

			InitInSeqNumFromProperties(inStorageSeqNum);
			InitOutSeqNumFromProperties(nextOutStorageSeqNum);
			//FIXME_NOW: may be we need more clear save method
			SaveSessionParameters();
		}

		private bool ApplyForceSeqNumReset()
		{
			switch (_sessionParameters.ForceSeqNumReset)
			{
				case ForceSeqNumReset.Always:
					ResetSeqNumAndOptionallySetResetSeqNumFlag();
					if (Log.IsTraceEnabled)
					{
						Log.Trace("Apply force seq num reset - ALWAYS");
					}
					return true;
				case ForceSeqNumReset.OneTime:
					if (!IsSendLogonWithSeqResetNum())
					{
						ResetSeqNumAndOptionallySetResetSeqNumFlag();
						_session.SetAttribute(ExtendedFixSessionAttribute.IsSendResetSeqNum.Name, "Y");
						if (Log.IsTraceEnabled)
						{
							Log.Trace("Apply force seq num reset - ONETIME");
						}
						return true;
					}
					else
					{
						_runtimeState.OutgoingLogon = _sessionParameters.OutgoingLoginMessage;
					}
					break;
				case ForceSeqNumReset.Never:
					break;
			}

			return false;
		}

		private void ResetSeqNumAndSetResetSeqNumFlag()
		{
			ResetRuntimeSequences();
			SetResetSeqNumFlagIntoOutgoingLogon();
		}

		private void ResetSeqNumAndOptionallySetResetSeqNumFlag()
		{
			ResetRuntimeSequences();
			if (!_ignoreResetSeqNumFlagOnReset)
			{
				SetResetSeqNumFlagIntoOutgoingLogon();
			}
		}

		/// <inheritdoc />
		public void SetResetSeqNumFlagIntoOutgoingLogon()
		{
			_runtimeState.OutgoingLogon.UpdateValue(Tags.ResetSeqNumFlag, ResetSeqNumFlagValue, IndexedStorage.MissingTagHandling.AddIfNotExists);
		}

		private void ResetRuntimeSequences()
		{
			PrepareStateForReset();

			ApplyOutSeqnum(1);
			ApplyInSeqNum(1);

			ResetSequencesForNextConnect();
			SaveSessionParameters();
		}

		private void PrepareStateForReset()
		{
			_session.BackupStorages();
			_sessionParameters.LastSeqNumResetTimestamp = DateTimeHelper.CurrentMilliseconds;
			RemoveRangeOfRrSequence();
			SaveRestoredSequences();
		}

		private bool InLogonHasResetFlag()
		{
			return IsPresetResetFlag(_sessionParameters.IncomingLoginMessage);
		}

		private bool OutLogonHasResetFlag()
		{
			return IsPresetResetFlag(_runtimeState.OutgoingLogon);
		}

		private bool IsPresetResetFlag(FixMessage msg)
		{
			var rsnfTagIndex = msg.GetTagIndex(Tags.ResetSeqNumFlag);
			return rsnfTagIndex != FixMessage.NotFound && msg.GetTagValueAsBoolAtIndex(rsnfTagIndex);
		}


		private bool ApplyIntraDayReset()
		{
			if (_configAdapter.IsIntraDeySeqNumResetEnabled)
			{
				ResetRuntimeSequences();
				return true;
			}
			return false;
		}

		private bool ApplyResetByTime()
		{
			if (_configAdapter.IsResetSeqNumTimeEnabled)
			{
				var lastSeqNumResetTimestamp = _sessionParameters.LastSeqNumResetTimestamp;
				if (IsResetTimeMissed(lastSeqNumResetTimestamp))
				{
					ResetSeqNumAndOptionallySetResetSeqNumFlag();
					return true;
				}
			}

			return false;
		}

		private bool ApplyResetOnNextConnect()
		{
			var inSeqNumsForNextConnect = _sessionParameters.IncomingSequenceNumber;
			var outSeqNumsForNextConnect = _sessionParameters.OutgoingSequenceNumber;
			if (inSeqNumsForNextConnect == 1 && outSeqNumsForNextConnect == 1)
			{
				ResetSeqNumAndOptionallySetResetSeqNumFlag();
				//FIXME_NOW: may be we need more clear save method
				SaveSessionParameters();
				return true;
			}
			return false;
		}

		private void InitInSeqNumFromProperties(long inStorageSeqNum)
		{
			if (_sessionParameters.IsSetInSeqNumsOnNextConnect)
			{
				if (Log.IsTraceEnabled)
				{
					Log.Trace("Init in sequence with configured value: " + _sessionParameters.IncomingSequenceNumber);
				}
				_runtimeState.InSeqNum = _sessionParameters.IncomingSequenceNumber;
				//reset last processed seq num when reset in seq num
				_runtimeState.LastProcessedSeqNum = 0;
				_sessionParameters.DisableInSeqNumsOnNextConnect();
				//todo - save session parameters
			}
			else if (_runtimeState.InSeqNum == FixSessionRuntimeState.InitSeqNum)
			{
				if (Log.IsTraceEnabled)
				{
					Log.Trace("Init in sequence from storage sequence: " + inStorageSeqNum);
				}
				_runtimeState.InSeqNum = inStorageSeqNum;
			}
		}

		private void InitOutSeqNumFromProperties(long nextOutStorageSeqNum)
		{
			if (_sessionParameters.SetOutSeqNumsOnNextConnect)
			{
				if (Log.IsTraceEnabled)
				{
					Log.Trace("Init out sequence with configured value: " + _sessionParameters.OutgoingSequenceNumber);
				}
				_runtimeState.OutSeqNum = _sessionParameters.OutgoingSequenceNumber;
				_sessionParameters.DisableOutSeqNumsOnNextConnect();
				//todo - save session parameters
			}
			else if (_runtimeState.OutSeqNum == FixSessionRuntimeState.InitSeqNum)
			{
				if (Log.IsTraceEnabled)
				{
					Log.Trace("Init out sequence from storage sequence: " + nextOutStorageSeqNum);
				}
				_runtimeState.OutSeqNum = nextOutStorageSeqNum;
			}
		}

		private void ResetSequencesForNextConnect()
		{
			_sessionParameters.DisableInSeqNumsOnNextConnect();
			_sessionParameters.DisableOutSeqNumsOnNextConnect();
		}

		private static readonly byte[] ResetSeqNumFlagValue = { (byte)'Y' };


		private bool IsSendLogonWithSeqResetNum()
		{
			return _session.GetAttribute(ExtendedFixSessionAttribute.IsSendResetSeqNum.Name) != null;
		}

		/// <inheritdoc />
		public void ConfigureStateBeforeReset()
		{
			RestoreSessionParameters();
			// backup session
			BackupSessionStorage();
			// sets new seq nums
			//reset also RR flags
			RemoveRangeOfRrSequence();
			_sessionParameters.LastSeqNumResetTimestamp = DateTimeHelper.CurrentMilliseconds;
		}

		/// <inheritdoc />
		public bool IsResetTimeMissed(long lastResetTime)
		{
			if (lastResetTime != 0)
			{
				var resetSequenceTimeInUserTimestamp = _configAdapter.ResetSequenceTimeInUserTimestamp;
				var nextStart = AdjustTimestamp(resetSequenceTimeInUserTimestamp);
				return nextStart - lastResetTime >= MillisInDay;
			}

			return false;
		}

		private long AdjustTimestamp(long timestamp)
		{
			if (DateTimeHelper.CurrentMilliseconds > timestamp)
			{
				timestamp += MillisInDay;
			}

			return timestamp;
		}

		/// <inheritdoc />
		public void InitLastSeqNumResetTimestampOnNewSession()
		{
			if (_configAdapter.IsResetSeqNumTimeEnabled)
			{
				var lastSeqNumResetTimestamp = _sessionParameters.LastSeqNumResetTimestamp;
				if (lastSeqNumResetTimestamp == 0 && _sessionParameters.IncomingSequenceNumber <= 1 && _sessionParameters.OutgoingSequenceNumber <= 1)
				{
					_sessionParameters.LastSeqNumResetTimestamp = DateTimeHelper.CurrentMilliseconds;
					SaveSessionParameters();
				}
			}
		}

		/// <inheritdoc />
		public void ResetSeqNumForNextConnect()
		{
			ConfigureStateBeforeReset();
			_session.RemoveAttribute(ExtendedFixSessionAttribute.IsSendResetSeqNum.Name);
			_sessionParameters.IncomingSequenceNumber = 1;
			_sessionParameters.OutgoingSequenceNumber = 1;
			ApplyResetOnNextConnect();
		}

		/// <inheritdoc />
		public void ApplyOutSeqnum(long outSeqNum)
		{
			_runtimeState.OutSeqNum = outSeqNum;
		}

		/// <inheritdoc />
		public void ApplyInSeqNum(long inSeqNum)
		{
			_runtimeState.InSeqNum = inSeqNum;
			_runtimeState.LastProcessedSeqNum = inSeqNum - 1;
		}

		/// <inheritdoc />
		public void IncrementOutSeqNum()
		{
			_runtimeState.IncrementOutSeqNum();
			if (_sessionParameters.SetOutSeqNumsOnNextConnect)
			{
				_sessionParameters.DisableOutSeqNumsOnNextConnect();
				SaveRestoredSequences();
			}
		}

		internal abstract class AbstractSequenceResendManager : ISequenceResendManager
		{
			/// <inheritdoc />
			public abstract void SendRequestForResend(long seqNum, bool posDup);

			/// <inheritdoc />
			public abstract bool IsBlockResendSupported(long seqNum);

			/// <inheritdoc />
			public abstract void RequestLostMessages(long expectedSeqNum, long incomingSeqNum, bool posDup);
			private readonly StandardSessionSequenceManager _sequenceManager;


			internal IBoundedQueue<FixMessage> BufferedMessages;

			public AbstractSequenceResendManager(StandardSessionSequenceManager sequenceManager)
			{
				_sequenceManager = sequenceManager;
				var bufferSize = _sequenceManager._configAdapter.Configuration.GetPropertyAsInt(Config.SequenceResendManagerMessageBufferSize, 32);
				BufferedMessages = new SimpleBoundedQueue<FixMessage>(bufferSize);
			}

			/// <inheritdoc />
			public virtual bool IsBufferEmpty
			{
				get { return BufferedMessages.IsEmpty; }
			}

			/// <inheritdoc />
			public virtual bool PutMessageIntoBuffer(FixMessage message)
			{
				if (BufferedMessages.Offer(message))
				{
					if (Log.IsInfoEnabled)
					{
						Log.Info("Message with sequence number \"" + message.GetTagAsInt(Tags.MsgSeqNum) + "\" put into buffer. Buffer size is " + BufferedMessages.Size);
					}
					return true;
				}

				if (Log.IsInfoEnabled)
				{
					Log.Info("Buffer is full. Buffer size is " + BufferedMessages.Size);
				}
				return false;
			}

			/// <inheritdoc />
			public virtual FixMessage TakeMessageFromBuffer()
			{
				if (Log.IsDebugEnabled)
				{
					Log.Debug("SequenceResendBuffer size is " + BufferedMessages.Size);
				}
				var message = BufferedMessages.Poll();
				if (null != message)
				{
					if (Log.IsInfoEnabled)
					{
						Log.Info("Message " + message.ToPrintableString() + " taken from buffer.");
					}
				}
				else
				{
					if (Log.IsInfoEnabled)
					{
						Log.Info("SequenceResendBuffer is empty");
					}
				}
				return message;
			}

			/// <inheritdoc />
			public bool IsMessageProcessingFromBufferStarted { get; set; }

			/// <inheritdoc />
			public virtual bool IsRrRangeActive
			{
				get
				{
					return !Common.Constants.IsNull(
									_sequenceManager._session.GetAttributeAsLong(ExtendedFixSessionAttribute.StartOfRrRange)) &&
								!Common.Constants.IsNull(
									_sequenceManager._session.GetAttributeAsLong(ExtendedFixSessionAttribute.EndOfRrRange));
				}
			}

			/// <inheritdoc />
			public virtual void SkipMessagesFromBufferTillSeqNum(long seqNum)
			{
				while ((!BufferedMessages.IsEmpty) && BufferedMessages.Peek().MsgSeqNumber < seqNum)
				{
					var message = BufferedMessages.Remove();
					if (Log.IsDebugEnabled)
					{
						Log.Debug("Skipped from buffer: " + message);
					}
				}
			}

		}

		internal class BlockSequenceResendManager : AbstractSequenceResendManager
		{
			private readonly StandardSessionSequenceManager _sequenceManager;

			internal long MaxRequestResendBlock;

			public BlockSequenceResendManager(StandardSessionSequenceManager sequenceManager, long maxRequestResendBlock) : base(sequenceManager)
			{
				_sequenceManager = sequenceManager;
				MaxRequestResendBlock = maxRequestResendBlock;
			}

			public virtual bool IsNeedPartResendForMessage()
			{
				var start = _sequenceManager.GetStartRangeOfRrSequence();
				var end = _sequenceManager.GetEndRangeOfRrSequence();
				return (end - start) > MaxRequestResendBlock && MaxRequestResendBlock != 0;
			}

			public override bool IsBlockResendSupported(long seqNum)
			{
				if (MaxRequestResendBlock == 0)
				{
					return false;
				}
				var startRange = _sequenceManager.GetStartRangeOfRrSequence();
				var endRange = _sequenceManager.GetEndRangeOfRrSequence();
				return seqNum < endRange && (seqNum - startRange + 1) % MaxRequestResendBlock == 0;
			}

			public override void SendRequestForResend(long startRange, bool posDup)
			{
				_sequenceManager.SaveRrSequence(startRange);
				var endRange = GetEndOfRange(startRange);
				_sequenceManager.RequestNextMessagePart(startRange, endRange, posDup);
			}

			public virtual long GetEndOfRange(long startRange)
			{
				var endRange = _sequenceManager.GetEndRangeOfRrSequence();
				if (endRange - startRange >= MaxRequestResendBlock && MaxRequestResendBlock != 0)
				{
					var endOfFrame = endRange;
					if (startRange + MaxRequestResendBlock - 1 < endOfFrame)
					{
						endOfFrame = startRange + MaxRequestResendBlock - 1;
					}

					endRange = endOfFrame;
				}
				return endRange;
			}

			public override void RequestLostMessages(long expectedSeqNum, long incomingSeqNum, bool posDup)
			{
				var fixVersion = _sequenceManager._session.Parameters.FixVersion;
				long endOfSequence = 0;
				if (fixVersion == FixVersion.Fix40 || fixVersion == FixVersion.Fix41)
				{
					endOfSequence = _sequenceManager._session.MessageFactory.GetEndSequenceNumber();
				}
				_sequenceManager.SaveRequestSequenceRange(expectedSeqNum, incomingSeqNum);
				if (IsNeedPartResendForMessage())
				{
					SendRequestForResend(expectedSeqNum, posDup);
				}
				else
				{
					_sequenceManager.RequestNextMessagePart(expectedSeqNum, endOfSequence, posDup);
				}
			}
		}

		internal class FreeRangeSequenceResendManager : AbstractSequenceResendManager
		{
			private readonly StandardSessionSequenceManager _sequenceManager;


			public FreeRangeSequenceResendManager(StandardSessionSequenceManager sequenceManager) : base(sequenceManager)
			{
				_sequenceManager = sequenceManager;
			}

			public override bool IsBlockResendSupported(long seqNum)
			{
				return false;
			}

			public override void SendRequestForResend(long seqNum, bool posDup)
			{
				throw new NotSupportedException();
			}

			public override void RequestLostMessages(long expectedSeqNum, long incomingSeqNum, bool posDup)
			{
				var fixVersion = _sequenceManager._session.Parameters.FixVersion;
				long endOfSequence = 0;
				if (fixVersion == FixVersion.Fix40 || fixVersion == FixVersion.Fix41)
				{
					endOfSequence = _sequenceManager._session.MessageFactory.GetEndSequenceNumber();
				}
				_sequenceManager.SaveRequestSequenceRange(expectedSeqNum, incomingSeqNum);
				_sequenceManager.RequestNextMessagePart(expectedSeqNum, endOfSequence, posDup);
			}
		}
	}
}
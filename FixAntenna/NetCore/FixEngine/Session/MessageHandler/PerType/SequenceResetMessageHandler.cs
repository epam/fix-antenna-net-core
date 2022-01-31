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
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.PerType
{
	/// <summary>
	/// The sequence reset handler.
	/// </summary>
	internal class SequenceResetMessageHandler : AbstractSessionMessageHandler
	{
		/// <inheritdoc />
		public override void OnNewMessage(FixMessage message)
		{
			Log.Debug("SequenceReset message handler");

			var newSeqNo = message.GetTagValueAsLong(Tags.NewSeqNo);
			var msgSeqNum = message.GetTagValueAsLong(Tags.MsgSeqNum);
			var session = Session;
			var runtimeState = session.RuntimeState;
			var currentIncomingSeqNum = (long?) session.GetAndRemoveAttribute(ExtendedFixSessionAttribute.SequenceWasDecremented.Name);
			if (currentIncomingSeqNum == null || FixMessageUtil.IsPosDup(message))
			{
				currentIncomingSeqNum = runtimeState.InSeqNum;
				// decrement for Sequence Reset (Reset) if it wasn't decremented before (MsgNum(43) is expected)
				// if NewSeqNo>=expected - reset will be applied and new sequences will be set
				// if NewSeqNo<expected - message should be rejected without incrementing session' sequences
				if (!FixMessageUtil.IsGapFill(message))
				{
					//OutOfSequenceMessageHandler decrements sequence if it is not equal to expected, otherwise it has to be decremented here.
					runtimeState.DecrementInSeqNum();
				}
			}

			if (FixMessageUtil.IsGapFill(message))
			{
				if (msgSeqNum < currentIncomingSeqNum.Value && FixMessageUtil.IsPosDup(message))
				{
					runtimeState.DecrementInSeqNum();
					return;
				}
				session.FixSessionOutOfSyncListener.OnGapFillReceived(message);
				if (newSeqNo <= currentIncomingSeqNum.Value)
				{
					var problemDescription = "Attempt to lower sequence number, invalid value NewSeqNum=" + newSeqNo;
					var list = session.MessageFactory.GetRejectForMessageTag(message, Tags.NewSeqNo, 5, problemDescription);
					session.ErrorHandler.OnWarn(problemDescription, new InvalidMessageException(message, problemDescription));
					session.SendMessageOutOfTurn(MsgType.Reject, list);
					return;
				}
			}
			else
			{
				if (newSeqNo < currentIncomingSeqNum.Value)
				{
					var problemDescription = "Value " + newSeqNo + " is incorrect (out of range) for this tag " + Tags.NewSeqNo;
					var list = session.MessageFactory.GetRejectForMessageTag(message, Tags.NewSeqNo, 5, problemDescription);
					session.ErrorHandler.OnWarn(problemDescription, new InvalidMessageException(message, problemDescription));
					session.SendMessageOutOfTurn(MsgType.Reject, list);
					return;
				}
				else if (newSeqNo == currentIncomingSeqNum.Value)
				{
					Log.Warn("Your counterparty sent sequence reset with the new sequence number equal to the currently expected sequence number for session " + session + ". It may indicate possible error");
				}
			}

			var endOfRrRange = session.GetAttributeAsLong(ExtendedFixSessionAttribute.EndOfRrRange);
			var requestedEndOfRr = session.GetAttributeAsLong(ExtendedFixSessionAttribute.RequestedEndRrRange);
			var sequenceManager = GetSequenceManager();
			var enhancedResendLogic = session.Parameters.Configuration
				.GetPropertyAsBoolean(Config.EnhancedCmeResendLogic);

			if (enhancedResendLogic && requestedEndOfRr > 0 && endOfRrRange > requestedEndOfRr && newSeqNo > requestedEndOfRr)
			{
				sequenceManager.SeqResendManager.SendRequestForResend(requestedEndOfRr + 1, false);
				runtimeState.LastProcessedSeqNum = requestedEndOfRr;
				return;
			}

			if (Log.IsInfoEnabled)
			{
				Log.Info("Seq reset to: " + newSeqNo);
			}

			sequenceManager.ResetSequencesOnRequest(newSeqNo);
		}

		private ISessionSequenceManager GetSequenceManager()
		{
			return ((AbstractFixSession) Session).SequenceManager;
		}
	}
}
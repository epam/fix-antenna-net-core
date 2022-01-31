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

using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global
{
	internal class RrSequenceRangeResponseHandler : AbstractGlobalMessageHandler
	{
		private bool _ignorePossDupForGapFill = true;

		/// <inheritdoc />
		public override IExtendedFixSession Session
		{
			set
			{
				base.Session = value;
				var configuration = new ConfigurationAdapter(value.Parameters.Configuration);
				_ignorePossDupForGapFill = configuration.IsIgnorePossDupForGapFill;
			}
		}

		/// <inheritdoc />
		public override void OnNewMessage(FixMessage message)
		{
			HandleMessage(message);
		}

		private void HandleMessage(FixMessage message)
		{
			var sequenceManager = GetSequenceManager();

			var msgSeqNum = message.MsgSeqNumber;

			var messageInRange = sequenceManager.IsSequenceInRange(msgSeqNum);
			var blockResendSupportedForMessage = sequenceManager.SeqResendManager.IsBlockResendSupported(msgSeqNum);

			if (messageInRange && blockResendSupportedForMessage)
			{
				sequenceManager.SeqResendManager.SendRequestForResend(msgSeqNum + 1, false);
			}

			if (!IsIgnorableMsg(message))
			{

				if (messageInRange)
				{
					if (CheckForCorrectResend(message))
					{
						if (sequenceManager.GetEndRangeOfRrSequence() == msgSeqNum)
						{
							sequenceManager.RemoveRangeOfRrSequence();
							Session.SetOutOfTurnMode(false);
						}
					}
					else
					{
						StartDisconnectProcess(message, sequenceManager.GetEndRangeOfRrSequence());
					}
				}
			}
			CallNextHandler(message);
		}

		private bool CheckForCorrectResend(FixMessage message)
		{
			//should have PosDup flag
			return FixMessageUtil.IsPosDup(message) || (IsSeqReset(message) && (!IsGapFill(message) || _ignorePossDupForGapFill));
		}

		private bool IsSeqReset(FixMessage message)
		{
			return message.GetTagValueAsString(35).Equals("4");
		}

		private bool IsGapFill(FixMessage message)
		{
			return message.IsTagExists(123) && message.GetTagValueAsString(123).Equals("Y");
		}

		private bool IsIgnorableMsg(FixMessage message)
		{
			if (FixMessageUtil.IsLogout(message))
			{
				var state = Session.SessionState;
				if (state == SessionState.WaitingForLogoff || state == SessionState.WaitingForForcedLogoff)
				{
					return true;
				}
			}

			if (FixMessageUtil.IsLogon(message))
			{
				return true; // skip message
			}

			//Sequence Reset - Reset
			if (FixMessageUtil.IsSeqReset(message) && FixMessageUtil.IsPosDup(message))
			{
				return true; // skip its too
			}

			return false;
		}

		private ISessionSequenceManager GetSequenceManager()
		{
			return ((AbstractFixSession) Session).SequenceManager;
		}

		private void StartDisconnectProcess(FixMessage message, long endRangeOfRrSequence)
		{
			var errorMessage = "Incoming seq number " + message.MsgSeqNumber + " is less then expected " + (endRangeOfRrSequence + 1) + " or need PossDup flag";
			//log.Warn(errorMessage);
			if (FixMessageUtil.IsLogon(message))
			{
				Session.ClearQueue();
			}
			Session.ForcedDisconnect(DisconnectReason.GotSequenceTooLow, errorMessage, false);

			throw new SequenceToLowException(errorMessage, message.ToPrintableString());
		}
	}
}
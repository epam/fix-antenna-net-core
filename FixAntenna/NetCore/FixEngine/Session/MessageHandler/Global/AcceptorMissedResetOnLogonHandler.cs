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
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global
{
	/// Missed reset handler: disconnect sessions with a special <seealso cref="DisconnectReason.PossibleMissedReset"/>
	/// if incoming LOGON message has a sequence number higher than expected by <seealso cref="Config.ResetThreshold"/>
	internal class AcceptorMissedResetOnLogonHandler : AbstractGlobalMessageHandler
	{
		protected internal new static readonly ILog Log = LogFactory.GetLog(typeof(AcceptorMissedResetOnLogonHandler));

		private bool _isEnabled = false;
		private int _resetThreshold;

		/// <inheritdoc />
		public override IExtendedFixSession Session
		{
			set
			{
				base.Session = value;
				_resetThreshold = Session.Parameters.Configuration.GetPropertyAsInt(Config.ResetThreshold);
				_isEnabled = value is AcceptorFixSession && _resetThreshold > 0;
			}
		}

		/// <inheritdoc />
		public override void OnNewMessage(FixMessage message)
		{
			// if reset & logon & logon without reset flag
			if (_isEnabled && IsReset() && FixMessageUtil.IsLogon(message) && !IsLogonWithReset(message))
			{
				// check logon message
				CheckMissedReset(message);
			}
			else
			{
				CallNextHandler(message);
			}
		}

		private bool IsLogonWithReset(FixMessage logonMessage)
		{
			var resetSeqNumFlagIndex = logonMessage.GetTagIndex(Tags.ResetSeqNumFlag);
			if (resetSeqNumFlagIndex != FixMessage.NotFound)
			{
				var resetSeqNumFlag = (char) logonMessage.GetTagValueAsByteAtIndex(resetSeqNumFlagIndex, 0);
				return logonMessage.GetTagValueLengthAtIndex(resetSeqNumFlagIndex) == 1 && (IsResetFlagExist(resetSeqNumFlag));
			}
			return false;
		}

		private bool IsResetFlagExist(char resetSeqNumFlag)
		{
			return resetSeqNumFlag == 'y' || resetSeqNumFlag == 'Y';
		}

		private bool IsReset()
		{
			return ((AbstractFixSession) Session).SequenceManager.GetExpectedIncomingSeqNumber() == 1;
		}

		private void CheckMissedReset(FixMessage message)
		{
			long msgInSeqNum = 0;
			try
			{
				msgInSeqNum = message.GetTagAsInt(Tags.MsgSeqNum);
			}
			catch (FieldNotFoundException)
			{
			}

			if (msgInSeqNum > 1)
			{
				if (msgInSeqNum > _resetThreshold)
				{
					if (Log.IsInfoEnabled)
					{
						Log.Info("Disconnected due to reset threshold with threshold set to: " + _resetThreshold + "; and " + "logon message: " + message);
					}
					Session.ForcedDisconnect(DisconnectReason.PossibleMissedReset, "Incoming sequence number higher than expected (missed reset) with threshold set to: " + _resetThreshold, false);
					return;
				}
			}

			CallNextHandler(message);
		}
	}
}
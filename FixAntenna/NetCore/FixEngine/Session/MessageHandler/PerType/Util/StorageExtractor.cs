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

using System.IO;
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.FixEngine.Storage;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.PerType.Util
{
	internal class StorageExtractor : IMessageStorageListener
	{
		protected internal static readonly ILog Log = LogFactory.GetLog(typeof(StorageExtractor));

		private readonly SentRetrievedMessageCounter _messageCounter;
		private readonly IExtendedFixSession _session;
		private byte[] _lastSendingTime;
		private RrMessageCarrierSender _messageCarrierSender;
		private int _maxDifference = 0;

		public StorageExtractor(IExtendedFixSession session, byte[] sendingTime, long beginSeqNum, long endSeqNum)
		{
			_lastSendingTime = sendingTime;
			_session = session;
			_messageCounter = new SentRetrievedMessageCounter(this, beginSeqNum, endSeqNum);
			_messageCarrierSender = new RrMessageCarrierSender(session);
			var configuration = new ConfigurationAdapter(session.Parameters.Configuration);
			_maxDifference = configuration.MaxDifference;
		}

		public virtual void ExtractAllMessagesAndResend()
		{
			try
			{
				// calculate
				if (_maxDifference > 0)
				{
					var gapSize = _messageCounter.GetGapSize();
					if (gapSize > _maxDifference)
					{
						SendGapFieldAndCalculateBeginGapAgain(_maxDifference);
					}
				}

				if (Log.IsDebugEnabled)
				{
					Log.Debug("Resent from: " + _messageCounter.GetLastSentSeqNum() + " to: " + _messageCounter.GetEndSeqNum());
				}

				_session.OutgoingStorage.RetrieveMessages(_messageCounter.GetLastSentSeqNum(), _messageCounter.GetEndSeqNum(), this, true);
				SendSeqResetWhenGapDetectedAgain();
			}
			catch (StorageClosedException e)
			{
				if (Log.IsDebugEnabled)
				{
					Log.Warn("The storage was closed, processing was interrupted. Cause: " + e.Message, e);
				}
				else
				{
					Log.Warn("The storage was closed, processing was interrupted. Cause: " + e.Message);
				}
			}
			catch (IOException e)
			{
				Log.Warn("Sequence reset was sent in response to resend request because resent process " + "encountered invalid message in message store. Cause: " + e.ToString());
				if (Log.IsDebugEnabled)
				{
					Log.Debug(e, e);
				}
				_messageCarrierSender.SendGapFill(_messageCounter.GetLastSentSeqNum() + 1, _messageCounter.GetEndSeqNum() + 1, _lastSendingTime);
			}
		}

		private void SendGapFieldAndCalculateBeginGapAgain(int maxDifference)
		{
			var endSeqNum = _messageCounter.GetEndSeqNum() - maxDifference + 1;
			//long endSeqNum = messageCounter.GetEndSeqNum() - maxDifference;
			_messageCarrierSender.SendGapFill(_messageCounter.GetLastSentSeqNum() + 1, endSeqNum, _lastSendingTime);
			_messageCounter.AdjustBeginGap(endSeqNum);
		}

		/// <inheritdoc />
		public virtual void OnMessage(byte[] message)
		{
			_messageCounter.IncrementCurrentRetrievedSeqNum();
			if (message != null && message.Length != 0)
			{
				if (Log.IsDebugEnabled)
				{
					Log.Debug("Retrieved from logs: " + StringHelper.NewString(message));
				}

				if (RawFixUtil.IsSessionLevelMessage(message))
				{
					// session level message
					_lastSendingTime = RawFixUtil.GetFixMessage(message).GetTagValueAsBytes(Tags.SendingTime);
					return;
				}

				SendRetrievedMessage(message);
			}
		}

		private void SendRetrievedMessage(byte[] message)
		{
			var messageDelivered = false;
			try
			{
				_messageCounter.IncrementLastSent();
				SendSeqResetWhenGapDetected();
				_messageCarrierSender.SendRetrievedMessage(message);
				messageDelivered = true;
			}
			finally
			{
				// notice:
				// message was not delivered, we should decremented last send seq num,
				//
				if (!messageDelivered)
				{
					_messageCounter.DecrementLastSent();
				}
			}
		}

		private void SendSeqResetWhenGapDetected()
		{
			if (_messageCounter.IsGapDetected())
			{
				// send gap fill on this application message or null (end) message
				_messageCounter.SetLastSentSeqNum(_messageCarrierSender.SendGapFill(_messageCounter.GetLastSentSeqNum(), _messageCounter.GetCurrentRetrievedSeqNum(), _lastSendingTime));
			}
		}

		private void SendSeqResetWhenGapDetectedAgain()
		{
			if (_messageCounter.IsGapDetected() || _messageCounter.GetLastSentSeqNum() < _messageCounter.GetEndSeqNum())
			{
				var gapEnd = _messageCounter.GetCurrentRetrievedSeqNum();
				if (gapEnd < _messageCounter.GetEndSeqNum())
				{
					gapEnd = _messageCounter.GetEndSeqNum();
				}
				_messageCounter.SetLastSentSeqNum(_messageCarrierSender.SendGapFill(_messageCounter.GetLastSentSeqNum() + 1, gapEnd + 1, _lastSendingTime));
			}
		}


		internal class SentRetrievedMessageCounter
		{
			private readonly StorageExtractor _outerInstance;

			internal long LastSentSeqNum, EndSeqNum, CurrentRetrievedSeqNum;

			public SentRetrievedMessageCounter(StorageExtractor outerInstance, long lastSentSeqNum, long endSeqNum)
			{
				_outerInstance = outerInstance;
				CurrentRetrievedSeqNum = lastSentSeqNum - 1;
				LastSentSeqNum = lastSentSeqNum;
				EndSeqNum = endSeqNum;
			}

			public virtual long GetLastSentSeqNum()
			{
				return LastSentSeqNum;
			}

			public virtual void SetLastSentSeqNum(long lastSentSeqNum)
			{
				LastSentSeqNum = lastSentSeqNum;
			}

			public virtual void IncrementLastSent()
			{
				LastSentSeqNum++;
			}

			public virtual void DecrementLastSent()
			{
				LastSentSeqNum--;
			}

			public virtual long GetEndSeqNum()
			{
				return EndSeqNum;
			}

			public virtual long GetCurrentRetrievedSeqNum()
			{
				return CurrentRetrievedSeqNum;
			}

			public virtual void IncrementCurrentRetrievedSeqNum()
			{
				CurrentRetrievedSeqNum++;
			}

			public virtual bool IsGapDetected()
			{
				return LastSentSeqNum != CurrentRetrievedSeqNum + 1;
			}

			public virtual long GetGapSize()
			{
				return EndSeqNum - LastSentSeqNum;
			}

			public virtual void AdjustBeginGap(long beginGap)
			{
				LastSentSeqNum = beginGap;
				CurrentRetrievedSeqNum = beginGap - 1;
			}
		}
	}
}
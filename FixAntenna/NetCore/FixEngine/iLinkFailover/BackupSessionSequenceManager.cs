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

using Epam.FixAntenna.NetCore.FixEngine.Session;

namespace Epam.FixAntenna.NetCore.FixEngine.iLinkFailover
{
	internal class BackupSessionSequenceManager : ISessionSequenceManager
	{
		private ISessionSequenceManager _sequenceManager;

		public BackupSessionSequenceManager(AbstractFixSession session)
		{
			_sequenceManager = new StandardSessionSequenceManager(session);
		}

		public virtual void Reinit(AbstractFixSession session)
		{
			_sequenceManager = new StandardSessionSequenceManager(session);
		}

		public virtual ISequenceResendManager SeqResendManager => _sequenceManager.SeqResendManager;

		public virtual long GetExpectedIncomingSeqNumber()
		{
			return 0;
		}

		public virtual void DecrementIncomingSeqNumber()
		{
		}

		public virtual void SaveProcessedSeqNumberOnShutdown()
		{
			_sequenceManager.SaveProcessedSeqNumberOnShutdown();
		}

		public virtual void SaveSessionParameters()
		{
			_sequenceManager.SaveSessionParameters();
		}

		public virtual void SaveRestoredSequences()
		{
			_sequenceManager.SaveRestoredSequences();
		}

		public virtual void RestoreSessionParameters()
		{
			_sequenceManager.RestoreSessionParameters();
		}

		public virtual void LoadStoredParameters()
		{
			_sequenceManager.LoadStoredParameters();
		}

		public virtual long GetRrSequenceFromSession()
		{
			return 0;
		}

		public virtual void SaveRrSequence(long lastRRSeq)
		{
			_sequenceManager.SaveRrSequence(lastRRSeq);
		}

		public virtual void RemoveRrSequenceFromSession(long? lastSeqId)
		{
			_sequenceManager.RemoveRrSequenceFromSession(lastSeqId);
		}

		public virtual void RemoveRangeOfRrSequence()
		{
			_sequenceManager.RemoveRangeOfRrSequence();
		}

		public virtual long GetStartRangeOfRrSequence()
		{
			return 0;
		}

		public virtual long GetEndRangeOfRrSequence()
		{
			return 0;
		}

		public virtual bool IsSequenceInRange(long seqNum)
		{
			return _sequenceManager.IsSequenceInRange(seqNum);
		}

		public virtual bool IsRRangeExists()
		{
			return _sequenceManager.IsRRangeExists();
		}

		public virtual bool IsRrSequenceActive()
		{
			return _sequenceManager.IsRrSequenceActive();
		}

		public virtual void UpdateEndOfRrRange(long incomingSeqNum)
		{
			_sequenceManager.UpdateEndOfRrRange(incomingSeqNum);
		}

		public virtual void UpdateLastRrSequence(long? msgSeqNum)
		{
			_sequenceManager.UpdateLastRrSequence(msgSeqNum);
		}

		public virtual void ResetSequencesOnRequest(long msgSeqNum)
		{
			_sequenceManager.ResetSequencesOnRequest(msgSeqNum);
		}

		public virtual void ResetSequencesOnLogon()
		{
			_sequenceManager.ResetSequencesOnLogon();
		}

		public virtual bool DoAfterMessageProcessActions()
		{
			return _sequenceManager.DoAfterMessageProcessActions();
		}

		public virtual long GetCountOfSentRequests(long startRange, long endRange)
		{
			return 0;
		}

		public virtual void RequestLostMessages(long expectedSeqNum, long incomingSeqNum, bool posDup)
		{
			_sequenceManager.RequestLostMessages(expectedSeqNum, incomingSeqNum, posDup);
		}

		public virtual void InitSeqNums(long inStorageSeqNum, long nextOutStorageSeqNum)
		{
			_sequenceManager.InitSeqNums(inStorageSeqNum, nextOutStorageSeqNum);
		}

		public virtual void SetResetSeqNumFlagIntoOutgoingLogon()
		{
			_sequenceManager.SetResetSeqNumFlagIntoOutgoingLogon();
		}

		public virtual void ConfigureStateBeforeReset()
		{
			_sequenceManager.ConfigureStateBeforeReset();
		}

		public virtual bool IsResetTimeMissed(long lastResetTime)
		{
			return _sequenceManager.IsResetTimeMissed(lastResetTime);
		}

		public virtual void InitLastSeqNumResetTimestampOnNewSession()
		{
			_sequenceManager.InitLastSeqNumResetTimestampOnNewSession();
		}

		public virtual void ResetSeqNumForNextConnect()
		{
			_sequenceManager.ResetSeqNumForNextConnect();
		}

		public virtual void ApplyOutSeqnum(long outSeqNum)
		{
			_sequenceManager.ApplyOutSeqnum(0);
		}

		public virtual void ApplyInSeqNum(long inSeqNum)
		{
			_sequenceManager.ApplyInSeqNum(0);
		}

		public virtual void IncrementOutSeqNum()
		{
		}
	}
}
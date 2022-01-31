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

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	internal interface ISessionSequenceManager
	{
		void Reinit(AbstractFixSession session);

		ISequenceResendManager SeqResendManager { get; }

		long GetExpectedIncomingSeqNumber();

		void DecrementIncomingSeqNumber();

		void SaveProcessedSeqNumberOnShutdown();

		void SaveSessionParameters();

		void SaveRestoredSequences();

		void RestoreSessionParameters();

		void LoadStoredParameters();

		long GetRrSequenceFromSession();

		void SaveRrSequence(long lastRrSeq);

		void RemoveRrSequenceFromSession(long? lastSeqId);

		void RemoveRangeOfRrSequence();

		long GetStartRangeOfRrSequence();

		long GetEndRangeOfRrSequence();

		bool IsSequenceInRange(long seqNum);

		bool IsRRangeExists();

		bool IsRrSequenceActive();

		void UpdateEndOfRrRange(long incomingSeqNum);

		void UpdateLastRrSequence(long? msgSeqNum);

		void ResetSequencesOnRequest(long msgSeqNum);

		void ResetSequencesOnLogon();

		bool DoAfterMessageProcessActions();

		long GetCountOfSentRequests(long startRange, long endRange);

		void RequestLostMessages(long expectedSeqNum, long incomingSeqNum, bool posDup);

		void InitSeqNums(long inStorageSeqNum, long nextOutStorageSeqNum);

		void SetResetSeqNumFlagIntoOutgoingLogon();

		void ConfigureStateBeforeReset();

		bool IsResetTimeMissed(long lastResetTime);

		void InitLastSeqNumResetTimestampOnNewSession();

		void ResetSeqNumForNextConnect();

		void ApplyOutSeqnum(long outSeqNum);

		void ApplyInSeqNum(long inSeqNum);

		void IncrementOutSeqNum();
	}
}
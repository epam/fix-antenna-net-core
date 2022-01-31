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

using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Post
{
	/// <summary>
	/// Handler process the sequence numbers of incoming messages.
	/// </summary>
	internal class LastProcessedSequenceMessageHandler : AbstractGlobalPostProcessSessionMessageHandler
	{

		/// <inheritdoc />
		public override bool HandleMessage(MsgBuf message)
		{
			return GetSequenceManager().DoAfterMessageProcessActions();

	//        // commented, these code invalid - seq num from attribute invalid, because: ignorable message Allow to set invalid seq num
	//        // Long incomingSeqNum = getSequenceManager().getIncomingSequenceFromSessionAttribute();
	//        long incomingSeqNum = getFIXSession().GetSessionParametersInstance().GetIncomingSequenceNumber();
	//        long lastProcessedSeqNum = getFIXSession().GetSessionParametersInstance().getProcessedIncomingSequenceNumber();
	//        if (incomingSeqNum != 0 && lastProcessedSeqNum != 0) {
	//            if (incomingSeqNum != (lastProcessedSeqNum + 1)) {
	//                // don't increment last processed seq num
	//                getSequenceManager().saveProcessedSequence();
	//                return false; // do not increment incoming sequence
	//            } else {
	//                getSequenceManager().incrementProcessedSequence();
	//                return true;
	//            }
	//        } else {
	//            getSequenceManager().incrementProcessedSequence();
	//            return true;
	//        }
		}

		private ISessionSequenceManager GetSequenceManager()
		{
			return ((AbstractFixSession) Session).SequenceManager;
		}
	}
}
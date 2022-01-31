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

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Cme
{
	internal class AdjustSequencesHandler : AbstractGlobalMessageHandler
	{
		private const string AdjustSequencesFromLogoffMessage = "adjustSequencesFromLogoffMessage";
		private const int NextExpectedMsgSeqNumTag = 789;

		public override void OnNewMessage(FixMessage message)
		{
			HandleMessage(message);
			CallNextHandler(message);
		}

		protected void HandleMessage(FixMessage message)
		{
			var fixSession = Session;
			var adjustSequences = fixSession.Parameters.Configuration.GetPropertyAsBoolean(AdjustSequencesFromLogoffMessage);

			if (FixMessageUtil.IsLogout(message) && adjustSequences && message.IsTagExists(NextExpectedMsgSeqNumTag))
			{
				var sessionParameters = fixSession.Parameters;
				var runtimeState = fixSession.RuntimeState;

				var outSeqNum = message.GetTagValueAsLong(NextExpectedMsgSeqNumTag);
				Log.Info("Adjust out sequence: change from " + runtimeState.OutSeqNum + " to " + outSeqNum);
				sessionParameters.OutgoingSequenceNumber = outSeqNum;
				runtimeState.OutSeqNum = outSeqNum;
			}
		}
	}
}
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
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.User
{
	internal class LastProcessedSlowConsumerCheckerMessageHandler : AbstractUserGlobalMessageHandler
	{
		private int _gapSize;

		public LastProcessedSlowConsumerCheckerMessageHandler(int gapSize)
		{
			_gapSize = gapSize;
		}

		/// <inheritdoc />
		public override bool ProcessMessage(FixMessage message)
		{
			if (message.HasTagValue(Tags.LastMsgSeqNumProcessed))
			{
				var slowConsumerListener = GetSlowConsumerListener();
				if (slowConsumerListener != null)
				{
					var current = Session.OutSeqNum;
					var processed = message.GetTagValueAsLong(Tags.LastMsgSeqNumProcessed);
					if (current - processed > _gapSize)
					{
						slowConsumerListener.OnSlowConsumerDetected(SlowConsumerReason.LastPrecessedSeqReceived, current, processed);
					}
				}
			}

			return true;
		}

		private IFixSessionSlowConsumerListener GetSlowConsumerListener()
		{
			return ((AbstractFixSession) Session).SlowConsumerListener;
		}
	}
}
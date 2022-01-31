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
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Post
{
	internal class RestoreSequenceAfterResendRequestHandler : AbstractGlobalPostProcessSessionMessageHandler
	{
		/// <inheritdoc />
		public override bool HandleMessage(MsgBuf message)
		{
			UpdateSessionParameters(message);
			return true;
		}

		private void UpdateSessionParameters(MsgBuf message)
		{
			if (GetSequenceManager().IsRrSequenceActive())
			{
				long? msgSeqNum = null;
				try
				{
					msgSeqNum = FixTypes.ParseInt(RawFixUtil.GetRawValue(message.Buffer, message.Offset, message.Length, Tags.MsgSeqNum));
				}
				catch (Exception)
				{
					// do nothing
				}
				GetSequenceManager().UpdateLastRrSequence(msgSeqNum);
			}
		}


		private ISessionSequenceManager GetSequenceManager()
		{
			return ((AbstractFixSession) Session).SequenceManager;
		}
	}
}
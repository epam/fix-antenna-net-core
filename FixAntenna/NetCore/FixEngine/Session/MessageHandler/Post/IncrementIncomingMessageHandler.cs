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
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Post
{
	/// <summary>
	/// Handler increment incoming sequence number.
	/// </summary>
	internal class IncrementIncomingMessageHandler : AbstractGlobalPostProcessSessionMessageHandler
	{
		/// <inheritdoc />
		public override bool HandleMessage(MsgBuf message)
		{
			var sessionParameters = Session.Parameters;
			if (sessionParameters.IsSetInSeqNumsOnNextConnect)
			{
				//handled first message after reset - disable flag
				if (Log.IsDebugEnabled)
				{
					Log.Debug("Disable resetInSeqNumsOnNextConnect flag - handled first incoming message after reset");
				}

				sessionParameters.DisableInSeqNumsOnNextConnect();
				try
				{
					Session.SaveSessionParameters();
				}
				catch (IOException e)
				{
					if (Log.IsDebugEnabled)
					{
						Log.Warn("Can't reset flag for sequences reset session parameters", e);
					}
					else
					{
						Log.Warn("Can't reset flag for sequences reset session parameters. Reason: " + e.Message);
					}
				}
			}

			var runtimeState = Session.RuntimeState;
			runtimeState.IncrementInSeqNum();
			if (Log.IsTraceEnabled)
			{
				Log.Trace("Increment incoming seq num: " + runtimeState.InSeqNum);
			}

			return true;
		}
	}
}
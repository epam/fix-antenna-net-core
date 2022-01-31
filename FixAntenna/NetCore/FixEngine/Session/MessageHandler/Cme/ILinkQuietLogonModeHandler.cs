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

using System.Threading;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Cme
{
	/// <summary>
	/// This class shutdown session with only warning notification if session get other then Logon(A) message in answer
	/// to Logon(A) request.
	///
	/// Default FIX Antenna behaviour in such case - send Logout(5) message with warning and forceble shutdown session
	/// with error.
	/// </summary>
	internal class ILinkQuietLogonModeHandler : QuietLogonModeHandler
	{
		public override void OnNewMessage(FixMessage message)
		{
			HandleMessage(message);
		}

		private void HandleMessage(FixMessage message)
		{
			var fixSession = Session;
			if (SessionState.WaitingForLogon == fixSession.SessionState)
			{ // only if WAITING_FOR_LOGON
				if (FixMessageUtil.IsLogout(message))
				{
					Log.Debug("Detected first message as Logout");
					fixSession.ErrorHandler.OnWarn("Received Logout right after connect for session " + fixSession.ToString() + ". Reason: " + message.GetTagValueAsString(58), new InvalidMessageException(message, "Received Logout right after connect", false));
					fixSession.MarkShutdownAsGraceful();
					fixSession.SessionState = SessionState.WaitingForForcedLogoff;
					Log.Info("Session is shutting down");
					var thread = new Thread(() => fixSession.Shutdown(DisconnectReason.ClosedByCounterparty, false))
					{
						Name = fixSession.Parameters.SessionId + "-stopper"
					};
					thread.Start();
				}
			}
			CallNextHandler(message);
		}
	}
}
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
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.PerType
{
	/// <summary>
	/// The Logoff message handler.
	/// </summary>
	internal class LogoffMessageHandler : AbstractSessionMessageHandler
	{
		private readonly FixMessage _list = new FixMessage();

		/// <summary>
		/// Creates the <c>LogoffMessageHandler</c>.
		/// </summary>
		public LogoffMessageHandler()
		{
			_list.AddTag(58, "Confirming logout".AsByteArray());
		}

		/// <summary>
		/// The method is shutdown the fix session.
		/// If session is not in <c>WAITING_FOR_LOGOFF</c> state, the logoff message will be sent.
		/// </summary>
		/// <seealso cref="IFixMessageListener.OnNewMessage(FixMessage)"> </seealso>
		public override void OnNewMessage(FixMessage message)
		{
			var fixSession = Session;
			if (fixSession.SessionState != SessionState.WaitingForLogoff
					&& fixSession.SessionState != SessionState.WaitingForForcedLogoff)
			{
				Log.Debug("Logoff message handler");
				fixSession.MarkShutdownAsGraceful();

				if (fixSession.TryStartSendingLogout())
				{
					fixSession.SendMessageOutOfTurn("5", _list);
					Log.Info("Confirming logoff sent, session is shutting down");
				}
				else
				{
					Log.Info("Confirming logoff skipped, session is shutting down");
				}
			}
			else
			{
				Log.Info("Confirming logoff received session is shutting down");
			}

			var threadName = fixSession.Parameters.SessionId + "-stopper";
			var thread = new Thread(() =>
			{
				DisconnectReason reason = null;
				if (fixSession.SessionState != SessionState.WaitingForLogoff
					&& fixSession.SessionState != SessionState.WaitingForForcedLogoff)
				{
					reason = DisconnectReason.ClosedByCounterparty;
				}

				fixSession.Shutdown(reason, false);
			});
			thread.Name = threadName;
			thread.Start();
		}
	}
}
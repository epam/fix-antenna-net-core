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

using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global
{
	/// <summary>
	/// The global logon message handler.
	/// </summary>
	internal class InvalidIncomingLogonMessageHandler : AbstractGlobalMessageHandler
	{
		protected internal new static readonly ILog Log = LogFactory.GetLog(typeof(InvalidIncomingLogonMessageHandler));
		public const string ErrorNotLogon = "First message is not logon";

		/// <summary>
		/// The handler calls the next handler only if the session state is <c>WaitingForLogon</c>
		/// and the first message is 'A', otherwise the session is shutdown.
		/// </summary>
		/// <seealso cref="IFixMessageListener.OnNewMessage"></seealso>
		public override void OnNewMessage(FixMessage message)
		{
			HandleMessage(message);
		}

		private void HandleMessage(FixMessage message)
		{
			if (SessionState.WaitingForLogon == Session.SessionState)
			{ // only if WAITING_FOR_LOGON
				if (!FixMessageUtil.IsLogon(message))
				{
					Session.ForcedDisconnect(DisconnectReason.InvalidMessage, ErrorNotLogon, false);
					throw new InvalidMessageException(message, ErrorNotLogon, true);
				}
			}
			CallNextHandler(message);
		}
	}
}
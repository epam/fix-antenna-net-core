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
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global
{
	internal class QuietLogonModeHandler : AbstractGlobalMessageHandler
	{
		private bool _instanceFieldsInitialized = false;

		public QuietLogonModeHandler()
		{
			if (!_instanceFieldsInitialized)
			{
				InitializeInstanceFields();
				_instanceFieldsInitialized = true;
			}
		}

		private void InitializeInstanceFields()
		{
			_list.AddTag(58, "Confirming logout");
		}

		protected internal new static readonly ILog Log = LogFactory.GetLog(typeof(QuietLogonModeHandler));
		private bool _quietLogonMode = false;
		private FixMessage _list = new FixMessage();

		public override IExtendedFixSession Session
		{
			set
			{
				base.Session = value;
				var configuration = new ConfigurationAdapter(value.Parameters.Configuration);
				_quietLogonMode = configuration.IsQuietLogonModeEnabled;
			}
		}

		/// <summary>
		/// The handler calls the next handler only if the session state is <c>WAITING_FOR_LOGON</c>
		/// and the first message is 'A',
		/// otherwise the session is shutdown.
		/// </summary>
		/// <seealso cref="IFixMessageListener.OnNewMessage"> </seealso>
		public override void OnNewMessage(FixMessage message)
		{
			HandleMessage(message);
		}

		private void HandleMessage(FixMessage message)
		{
			var fixSession = Session;
			if (_quietLogonMode && SessionState.WaitingForLogon == fixSession.SessionState)
			{ // only if WAITING_FOR_LOGON
				if (FixMessageUtil.IsLogout(message))
				{
					Log.Debug("Detected first message as Logout");
					fixSession.ErrorHandler.OnWarn("Received Logout right after connect for session " + fixSession.ToString() + ". Reason: " + message.GetTagValueAsString(58), new InvalidMessageException(message, "Received Logout right after connect", false));
					fixSession.SendMessageOutOfTurn("5", _list);
					fixSession.MarkShutdownAsGraceful();
					fixSession.SessionState = SessionState.WaitingForForcedLogoff;
					Log.Info("Confirming Logout sent, session is shutting down");
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
			CallNextHandler(message);
		}
	}
}
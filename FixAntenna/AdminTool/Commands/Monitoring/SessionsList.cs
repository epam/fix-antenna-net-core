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

using Epam.FixAntenna.AdminTool.Commands.Monitoring.Manager;
using Epam.FixAntenna.Fixicc.Message;

namespace Epam.FixAntenna.AdminTool.Commands.Monitoring
{
	/// <summary>
	/// The SessionsList command.
	/// </summary>
	internal class SessionsList : Command
	{
		private SessionListManager _sessionListManager;

		public SessionsList()
		{
			_sessionListManager = new SessionListManager(this);
		}

		public override void Execute()
		{
			Log.Debug("Execute SessionsList Command");

			var sessionsListRequest = (Fixicc.Message.SessionsList) Request;
			var subscriptionType = sessionsListRequest.SubscriptionRequestType;

			switch (subscriptionType)
			{
				case SubscriptionRequestType.Item0: // Snapshot only
					_sessionListManager.DoSnapshot();
					break;
				case SubscriptionRequestType.Item1: // Subscribe with snapshot
					_sessionListManager.DoSubscribeWithSnapshot();
					break;
				case SubscriptionRequestType.Item2: // Unsubscribe
					_sessionListManager.DoUnsubscribe();
					break;
				case SubscriptionRequestType.Item3: // Subscribe only
					_sessionListManager.DoSubscribe();
					break;
				default:
					SendInvalidArgument("Parameter SubscriptionRequestType must be one of 0|1|2|3");
					return;
			}
		}
	}
}
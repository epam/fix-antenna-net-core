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

using Epam.FixAntenna.NetCore.FixEngine.Session;

namespace Epam.FixAntenna.NetCore.FixEngine.Manager.Tasks
{
	/// <summary>
	/// The TestRequest task.
	/// </summary>
	/// <seealso cref="FixSessionManager"> </seealso>
	internal class TestRequestTask : ISessionManagerTask
	{
		/// <summary>
		/// Check if test request is send and replay received.
		/// If test request is send and no replay received during heartbeat+20%
		/// seconds, session wil be disconnected.
		/// </summary>
		/// <param name="session"> </param>
		public virtual void RunForSession(IExtendedFixSession session)
		{
			if (!SessionState.IsConnected(session.SessionState))
			{
				return; // no need to run for sessions that aren't connected
			}

			if (session.Parameters.HeartbeatInterval == 0)
			{
				return;
			}

			var abstractFixSession = (AbstractFixSession) session;
			var value = abstractFixSession.GetAttribute(ExtendedFixSessionAttribute.IsResendRequestProcessed);
			//Test request should not been sent if engine is answering on resend request
			if (!ExtendedFixSessionAttribute.YesValue.Equals(value))
			{
				abstractFixSession.CheckHasSessionSendOrReceivedTestRequest();
			}
		}
	}
}
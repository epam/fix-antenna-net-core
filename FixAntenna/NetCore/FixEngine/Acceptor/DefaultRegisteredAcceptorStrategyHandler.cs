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
using Epam.FixAntenna.NetCore.FixEngine.Transport;

namespace Epam.FixAntenna.NetCore.FixEngine.Acceptor
{
	internal class DefaultRegisteredAcceptorStrategyHandler : SessionAcceptorStrategyHandler
	{
		public override void HandleIncomingConnection(SessionParameters sessionParameters, IFixTransport fixTransport)
		{
			if (AutostartAcceptorSessions.IsAdminSession(sessionParameters))
			{
				ProcessIncomingAdminSession(fixTransport, sessionParameters.SessionId, sessionParameters);

			}
			else
			{
				var session = (IExtendedFixSession) StandardFixSessionFactory.GetFactory(sessionParameters).CreateAcceptorSession(sessionParameters, fixTransport);
				if (!string.IsNullOrEmpty(((ParsedSessionParameters) sessionParameters).LogonError))
				{
					session.Reject(((ParsedSessionParameters) sessionParameters).LogonError);
				}
				else
				{
					if (Listener != null)
					{
						Listener.NewFixSession(session);
					}
					else
					{
						CloseDeniedSession(session);
					}
				}
			}
		}

		public override void CloseAllRegisteredSessions()
		{

		}
	}
}
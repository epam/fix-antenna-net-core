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

using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Acceptor;
using Epam.FixAntenna.NetCore.FixEngine.Transport;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	/// <summary>
	/// Defines environment and helper functions to test FIX session
	/// </summary>
	internal class AcceptorFixSessionHelper
	{
		public readonly TestTransport Transport;
		public readonly IFixSession Session;

		/// <summary>
		/// Initializes test environment and creates acceptor session inside from given details with custom transport
		/// </summary>
		/// <param name="details"></param>
		public AcceptorFixSessionHelper(SessionParameters details)
		{
			Transport = new TestTransport();
			var fixTransport = new DefaultSessionTransportFactory().CreateAcceptorTransport(Transport, Config.GlobalConfiguration);
			Session = StandardFixSessionFactory.GetFactory(details).CreateAcceptorSession(details, fixTransport);
		}

		public void SetIncomingLogon(FixMessage logon)
		{
			var logonMessageParser = new LogonMessageParser();
			var parseResult = logonMessageParser.ParseLogon(logon, Transport.RemoteEndPoint.Address.AsString(), Transport.LocalEndPoint.Port);

			var strategy = new AllowNonRegisteredAcceptorStrategyHandler();
			strategy.CheckSessionParameters(parseResult.SessionParameters, Session.Parameters);
			strategy.MergeSessionParameters(parseResult.SessionParameters, Session.Parameters);
		}
	}
}
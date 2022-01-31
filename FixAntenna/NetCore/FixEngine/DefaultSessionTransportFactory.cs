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
using Epam.FixAntenna.NetCore.FixEngine.Transport;
using Epam.FixAntenna.NetCore.FixEngine.Transport.Client.Tcp;

namespace Epam.FixAntenna.NetCore.FixEngine
{
	internal sealed class DefaultSessionTransportFactory : ISessionTransportFactory
	{
		/// <inheritdoc />
		public IFixTransport CreateInitiatorTransport(string host, int port, SessionParameters parameters)
		{
			return new InitiatorFixTransport(host, port, parameters);
		}

		/// <inheritdoc />
		public IFixTransport CreateAcceptorTransport(ITransport transport, Config configuration)
		{
			return new AcceptorFixTransport(transport ?? new TcpTransport(), configuration);
		}
	}
}
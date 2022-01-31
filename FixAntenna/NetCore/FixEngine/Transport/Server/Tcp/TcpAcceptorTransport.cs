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
using System.Net.Sockets;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;

namespace Epam.FixAntenna.NetCore.FixEngine.Transport.Server.Tcp
{
	/// <summary>
	/// TCP acceptor transport implementation.
	/// </summary>
	internal class TcpAcceptorTransport : SocketTransport
	{
		private bool EnableTcpNoDelay { get; }
		private readonly ConfigurationAdapter _configAdapter;
		private readonly ConnectionAuthenticator _authenticator;

		/// <summary>
		/// Creates the <c>TCPAcceptorTransport</c>.
		/// </summary>
		/// <param name="socket">The socket.</param>
		/// <param name="configAdapter">Configuration adapter.</param>
		internal TcpAcceptorTransport(Socket socket, ConfigurationAdapter configAdapter)
		{
			_configAdapter = configAdapter;
			_authenticator = new ConnectionAuthenticator(_configAdapter);

			Socket = socket;

			SendBufferSize = configAdapter.TcpSendBufferSize;
			ReceiveBufferSize = configAdapter.TcpReceiveBufferSize;
			EnableTcpNoDelay = !configAdapter.IsNagleEnabled;
		}

		/// <inheritdoc />
		public override void Open()
		{
			try
			{
				if (Log.IsTraceEnabled)
				{
					Log.Trace(
						$"Acceptor {Socket.LocalEndPoint.AsString()} was configured with TCP_NO_DELAY = {EnableTcpNoDelay}");
				}

				Socket.NoDelay = EnableTcpNoDelay;
				if (SendBufferSize > 0)
				{
					Socket.SendBufferSize = SendBufferSize;
				}

				if (ReceiveBufferSize > 0)
				{
					Socket.ReceiveBufferSize = ReceiveBufferSize;
				}

#pragma warning disable CA2000
				Stream netStream = new NetworkStream(Socket);
#pragma warning restore CA2000

				var connectionSecured = _configAdapter.IsSslPort(LocalEndPoint.Port);
				Os = Is = connectionSecured ? _authenticator.AuthenticateAcceptor(netStream) : netStream;
			}
			catch (SocketException ex)
			{
				throw new IOException(ex.Message, ex);
			}
		}
	}
}

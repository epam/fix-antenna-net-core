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

using System;
using System.Threading.Tasks;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Transport.Client.Tcp;

namespace Epam.FixAntenna.NetCore.FixEngine.Transport
{
	/// <summary>
	/// Initiator fix transport.
	/// </summary>
	internal class InitiatorFixTransport : AbstractFixTransport, IOutgoingFixTransport
	{
		private static readonly object LockObj = new object();

		/// <summary>
		/// Creates <c>InitiatorFIXTransport</c>.
		/// </summary>
		/// <param name="host"> the host </param>
		/// <param name="port"> the port </param>
		internal InitiatorFixTransport(string host, int port) : base(new TcpTransport(host, port), Config.GlobalConfiguration)
		{
			InitConnection(ConfigAdapter.ConnectAddress);
		}

		public InitiatorFixTransport(string host, int port, SessionParameters parameters)
			: base(new TcpTransport(host, port, parameters), parameters)
		{
			InitConnection(ConfigAdapter.ConnectAddress);
		}

		private void InitConnection(string connectAddress)
		{
			if (!string.IsNullOrEmpty(connectAddress))
			{
				throw new NotImplementedException(Config.ConnectAddress);
				//TODO: check this
				//((IClientTransport) Transport).setSocketFactory(new ConnectAddressSocketFactory(connectAddress));
			}

			((TcpTransport)Transport).AuthenticateStream = new ConnectionAuthenticator(ConfigAdapter).AuthenticateInitiator;
		}

		/// <inheritdoc />
		public virtual void Open()
		{
			lock (LockObj)
			{
				base.Reset();
				Transport.Open();
				Log.Debug("Transport opened");
			}
		}

		/// <inheritdoc />
		public async Task OpenAsync()
		{
			base.Reset();
			await ((TcpTransport)Transport).OpenAsync().ConfigureAwait(false);
			Log.Debug("Transport opened");
		}
	}
}
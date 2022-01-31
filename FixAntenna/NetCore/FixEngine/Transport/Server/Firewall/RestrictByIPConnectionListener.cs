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

using System.Collections.Generic;

namespace Epam.FixAntenna.NetCore.FixEngine.Transport.Server.Firewall
{
	internal class RestrictByIpConnectionListener : PassthroughConnectionListener
	{
		private readonly ISet<string> _allowedIPs;

		public RestrictByIpConnectionListener(IConnectionListener listener) : this(listener, new HashSet<string>())
		{
		}

		public RestrictByIpConnectionListener(IConnectionListener listener, ICollection<string> allowedIPs) :
			base(listener)
		{
			_allowedIPs = new HashSet<string>(allowedIPs);
		}

		public virtual void Allow(string ip)
		{
			_allowedIPs.Add(ip);
		}

		public virtual void Restrict(string ip)
		{
			_allowedIPs.Remove(ip);
		}

		/// <inheritdoc />
		public override void OnConnect(ITransport transport)
		{
			if (_allowedIPs.Contains(transport.RemoteEndPoint.Address.AsString()))
			{
				base.OnConnect(transport);
			}
		}
	}
}
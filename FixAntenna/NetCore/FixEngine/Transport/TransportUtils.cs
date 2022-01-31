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

using System.Net;

namespace Epam.FixAntenna.NetCore.FixEngine.Transport
{
	internal static class TransportUtils
	{
		/// <summary>
		/// Returns string representation of <see cref="EndPoint"/> in format [hostname|IP]:port.
		/// Correctly handles IPv4 mapped to IPv6 <see cref="IPAddress"/> in case of <see cref="IPEndPoint"/>.
		/// Returns <see cref="DnsEndPoint.Host"/> property in case of <see cref="DnsEndPoint"/>.
		/// </summary>
		/// <param name="ep">EndPoint</param>
		/// <returns>String representation of <see cref="IPEndPoint.Address"/>
		/// for <see cref="IPEndPoint"/> or <see cref="DnsEndPoint.Host"/> for <see cref="DnsEndPoint"/>.</returns>
		public static string AsString(this EndPoint ep)
		{
			if (ep == null) return null;

			switch (ep)
			{
				case IPEndPoint ipEp:
					return ipEp.Address.AsString() + ":" + ipEp.Port.ToString();
				case DnsEndPoint dnsEp:
					return dnsEp.Host + ":" + dnsEp.Port.ToString(); ;
				default:
					return ep.ToString();
			}
		}

		/// <summary>
		/// Returns string representation of <see cref="IPAddress"/>.
		/// If address is mapped to IPv6 (has form like ::ffff:127.0.0.1) maps it back to IPv4.
		/// </summary>
		/// <param name="address">Address</param>
		/// <returns>String representation of <see cref="IPAddress"/>.</returns>
		public static string AsString(this IPAddress address)
		{
			if (address == null) return null;

			return address.IsIPv4MappedToIPv6 ? address.MapToIPv4().ToString() : address.ToString();
		}

		/// <summary>
		/// Parses input string as IP address and returns IPEndPoint if succeed.
		/// Otherwise, returns DnsEndPoint.
		/// </summary>
		/// <param name="address">IP address or host name.</param>
		/// <param name="port">Port to use in EndPoint ctor.</param>
		/// <returns>Returns <see cref="EndPoint"/> initialized with provided address and port.</returns>
		public static EndPoint ToEndPoint(this string address, int port)
		{
			if (IPAddress.TryParse(address, out var ip))
			{
				return new IPEndPoint(ip, port);
			}

			return new DnsEndPoint(address, port);
		}
	}
}

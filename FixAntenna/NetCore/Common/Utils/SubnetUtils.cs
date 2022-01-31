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
using System.Globalization;
using System.Net;
using Epam.FixAntenna.NetCore.Helpers;

namespace Epam.FixAntenna.NetCore.Common.Utils
{
	/// <summary>
	/// This class supposed to be the partial replacement of the Apache SubnetUtils class
	/// for one use case: check if the host is in IP range.
	/// </summary>
	internal class SubnetUtils
	{
		private readonly long _low;
		private readonly long _high;

		public SubnetUtils(string subnet, bool inclusive = false)
		{
			if (string.IsNullOrWhiteSpace(subnet))
				throw new ArgumentException("Network subnet cannot be null or empty.", nameof(subnet));

			var subnetParts = subnet.Split('/');

			if (subnetParts.Length <= 1 || subnetParts.Length > 2)
				throw new ArgumentException("Network subnet should contain mask '/xx'.", nameof(subnet));

			long addrCidr = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(IPAddress.Parse(subnetParts[0]).GetAddressBytes(), 0));
			long maskCidr = -1 << (32 - int.Parse(subnetParts[1], CultureInfo.InvariantCulture));

			var network = addrCidr & maskCidr;
			var broadcast = network | ~maskCidr;

			if (inclusive)
			{
				_low = network;
				_high = broadcast;
			}
			else if (broadcast - network <= 1)
			{
				_low = 0;
				_high = 0;
			}
			else
			{
				_low = network + 1;
				_high = broadcast - 1;
			}
		}

		public bool IsInRange(string host)
		{
			if (string.IsNullOrWhiteSpace(host))
				throw new ArgumentException("Host cannot be null or empty.", nameof(host));

			var addr = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(IPAddress.Parse(host).GetAddressBytes(), 0));
			var diff = addr - _low;
			return diff >= 0 && diff <= _high - _low;
		}

		public static SubnetUtils[] ParseIpMasks(string ipStr)
		{
			if ("*".Equals(ipStr))
			{
				return new SubnetUtils[] { };
			}
			else
			{
				var maskStr = ipStr.Split(",", true);
				var masks = new SubnetUtils[maskStr.Length];
				for (var i = 0; i < maskStr.Length; i++)
				{
					var mask = maskStr[i].Trim();
					if (mask.IndexOf("/", StringComparison.Ordinal) != -1)
					{
						try
						{
							masks[i] = new SubnetUtils(mask);
						}
						catch (Exception e)
						{
							throw new ArgumentException("Invalid subnet definition - " + mask, e);
						}
					}
					else
					{
						try
						{
							masks[i] = new SubnetUtils(mask + "/32", true);
						}
						catch (Exception)
						{
							throw new ArgumentException("Invalid IP address - " + mask);
						}
					}
				}
				return masks;
			}
		}
	}
}

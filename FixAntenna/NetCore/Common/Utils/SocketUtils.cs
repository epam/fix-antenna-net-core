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
using System.Net.Sockets;

namespace Epam.FixAntenna.NetCore.Common.Utils
{
	internal class SocketUtils
	{
		public static bool IsLocalPortAvailableForBinding(int port)
		{
			try
			{
				using (var ss = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
				{
					ss.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
					ss.Bind(new IPEndPoint(IPAddress.Loopback, port));
					ss.Close();
					return true;
				}
			}
			catch (SocketException)
			{
			}
			return false;
		}
	}
}
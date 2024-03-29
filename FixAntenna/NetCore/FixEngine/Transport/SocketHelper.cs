﻿// Copyright (c) 2021 EPAM Systems
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
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Epam.FixAntenna.NetCore.FixEngine.Transport
{
	internal static class SocketHelper
	{
		/// <summary>
		/// Disables sends and receives for a socket only under Linux
		/// </summary>
		/// <param name="socket">Socket</param>
		public static void ShutdownAndClose(this Socket socket)
		{
			if (socket == null)
				return;

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				try
				{
					socket.Shutdown(SocketShutdown.Both);
				}
				catch (ObjectDisposedException)
				{
					// it's ok
				}
			}

			socket.Close();
		}
	}
}

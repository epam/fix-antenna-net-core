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
using System.IO;
using System.Net.Sockets;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.FixEngine.Transport;
using Epam.FixAntenna.NetCore.Helpers;

namespace Epam.FixAntenna.TestUtils.Transport
{
	internal abstract class BasicSocketTransport : ITransport
	{
		protected internal FixMessageReader Input;
		protected internal Stream Output;
		protected internal Socket Socket;

		public abstract void Init(int port, TimeSpan timeout);

		public abstract void Open();

		public virtual string ReceiveMessage()
		{
			if (Input != null)
			{
				return StringHelper.NewString(Input.ReadMessage());
			}

			throw new IOException("Transport is not opened.");
		}

		public virtual void SendMessage(string message)
		{
			if (Output != null)
			{
				var bytesToWrite = message.AsByteArray();
				Output.Write(bytesToWrite, 0, bytesToWrite.Length);
				Output.Flush();
			}
			else
			{
				throw new IOException("Transport is not opened.");
			}
		}

		public virtual void Close()
		{
			try
			{
				Input?.Close();
				Output?.Close();
				Socket.ShutdownAndClose();
			}
			catch (IOException e)
			{
				LogFactory.GetLog(GetType()).Error(e, e);
			}
		}
	}
}
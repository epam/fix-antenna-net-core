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
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Helpers;

namespace Epam.FixAntenna.Tester.Transport
{
	public abstract class BasicSocketTransport : ITransport
	{

#pragma warning disable CA2213
		protected internal Stream Out;
#pragma warning restore CA2213
		protected internal FIXMessageReader Input;
		protected internal Socket Socket;
		private bool disposedValue;

		public abstract void Init(IDictionary<string, string> parameters);

		public abstract void Open();

		public virtual string ReceiveMessage()
		{
			if (Input != null)
			{
				return StringHelper.NewString(Input.ReadMessage());
			}
			else
			{
				throw new IOException("Transport is not opened.");
			}
		}

		public virtual void SendMessage(string message)
		{
			//log.debug("Sending:" + message);
			if (Out != null)
			{
				var buf = message.AsByteArray(Encoding.ASCII);
				Out.Write(buf, 0, buf.Length);
				Out.Flush();
			}
			else
			{
				throw new IOException("Transport is not opened.");
			}
		}
		public virtual void SendMessage(string message, IDictionary<string, string> @params)
		{
			SendMessage(message);
		}

		public virtual string ReceiveMessage(IDictionary<string, string> @params)
		{
			return ReceiveMessage();
		}

		public virtual void SendMessage(byte[] message)
		{
			//log.debug("Sending:" + message);
			if (Out != null)
			{
				Out.Write(message, 0, message.Length);
				Out.Flush();
			}
			else
			{
				throw new IOException("Transport is not opened.");
			}
		}

		private void Close()
		{
			try
			{
				if (Input != null)
				{
					Input.close();
					Input = null;
				}
				if (Out != null)
				{
					Out.Close();
					Out = null;
				}
			}
			catch (Exception e)
			{
				LogFactory.GetLog(this.GetType()).Error(e, e);
			}
			finally
			{
				try
				{
					if (Socket != null)
					{
						Socket.Close();
						Socket = null;
					}
				}
				catch (Exception e)
				{
					LogFactory.GetLog(this.GetType()).Error(e, e);
				}
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					Close();
				}
				disposedValue = true;
			}
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			System.GC.SuppressFinalize(this);
		}
	}

}
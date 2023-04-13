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
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Common.Utils;

namespace Epam.FixAntenna.NetCore.FixEngine.Transport
{
	/// <summary>
	/// Abstract socket implementation of transport.
	/// </summary>
	internal abstract class SocketTransport : ITransport
	{
		private readonly object _sync = new object();

		protected readonly ILog Log;
		protected internal Stream Is;
		protected internal Stream Os;
		protected internal int ReceiveBufferSize;
		protected internal int SendBufferSize;

		protected internal Socket Socket;

		public const int SocketReadSize = 32768;

		protected SocketTransport()
		{
			Log = LogFactory.GetLog(GetType());
		}

		/// <inheritdoc />
		public virtual bool IsBlockingSocket => true;

		/// <inheritdoc />
		public virtual bool IsSecured => Is is SslStream sslStream && sslStream.IsAuthenticated;

		/// <inheritdoc />
		public virtual int Read(byte[] buffer, int offset, int length)
		{
			try
			{
				if (!IsOpen)
				{
					throw new IOException("transport is closed");
				}

				var read = Is.Read(buffer, offset, length);
				return read;
			}
			catch (SocketException ex)
			{
				throw new IOException(ex.Message, ex);
			}
		}

		/// <inheritdoc />
		public virtual int Read(byte[] buffer)
		{
			return Read(buffer, 0, buffer.Length);
		}

		/// <inheritdoc />
		public virtual void Write(byte[] message)
		{
			try
			{
				if (!IsOpen)
				{
					throw new IOException("transport is closed");
				}

				Os.Write(message, 0, message.Length);
			}
			catch (SocketException ex)
			{
				throw new IOException(ex.Message, ex);
			}
		}

		/// <inheritdoc />
		public virtual int Write(byte[] message, int offset, int length)
		{
			try
			{
				if (!IsOpen)
				{
					throw new IOException("transport is closed");
				}

				Os.Write(message, offset, length);
				return length;
			}
			catch (SocketException ex)
			{
				throw new IOException(ex.Message, ex);
			}
		}

		/// <inheritdoc />
		public virtual int Write(ByteBuffer buffer)
		{
			throw new NotSupportedException();
		}

		/// <inheritdoc />
		public virtual int Write(ByteBuffer buffer, int offset, int length)
		{
			Write(buffer.GetByteArray(), offset, length);
			return length;
		}

		/// <inheritdoc />
		public virtual int Read(ByteBuffer buffer, int offset, int length)
		{
			return Read(buffer.GetByteArray(), offset, length);
		}

		/// <inheritdoc />
		public virtual int Read(ByteBuffer buffer)
		{
			throw new NotSupportedException();
		}

		/// <inheritdoc />
		public virtual void WaitUntilReadyToWrite()
		{
		}

		/// <inheritdoc />
		public IPEndPoint LocalEndPoint => Socket?.LocalEndPoint as IPEndPoint;

		/// <inheritdoc />
		public IPEndPoint RemoteEndPoint => Socket?.RemoteEndPoint as IPEndPoint;

		/// <inheritdoc />
		public virtual void Close()
		{
			try
			{
				lock (_sync)
				{
					if (Is == Os)
					{
						if (Os != null)
						{
							try
							{
								Os.Flush();
								Os.Close();
							}
							catch (Exception e)
							{
								Log.Debug("Ignored exception while flushing/closing Stream", e);
							}
						}
					}
					else
					{
						if (Is != null)
						{
							try
							{
								Is.Close();
							}
							catch (Exception e)
							{
								Log.Debug("Ignored exception while closing InputStream", e);
							}
						}
					}

					Socket.ShutdownAndClose();

					Is = null;
					Os = null;
					Socket = null;
					Log.Debug("Transport was closed");
				}
			}
			catch (SocketException ex)
			{
				throw new IOException(ex.Message, ex);
			}
		}

		/// <inheritdoc />
		public abstract void Open();

		/// <inheritdoc />
		public virtual bool IsOpen => Socket != null && Socket.Connected;
	}
}
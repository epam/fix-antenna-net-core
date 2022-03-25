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
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.FixEngine.Transport;

namespace Epam.FixAntenna.NetCore.FixEngine.Transport
{
	internal class ReadStreamTransport : ITransport
	{
		private Stream _inputStream;
		internal int[] PartSize;
		internal int Pointer = 0;

		/// <inheritdoc />
		public virtual bool IsBlockingSocket => true;

		/// <inheritdoc />
		public virtual bool IsSecured => _inputStream is SslStream sslStream && sslStream.IsAuthenticated;

		public ReadStreamTransport(Stream inputStream, int[] partSize)
		{
			_inputStream = inputStream;
			PartSize = partSize;
		}

		public virtual int Write(byte[] message, int offset, int length)
		{
			throw new System.NotSupportedException();
		}

		public virtual void Write(byte[] message)
		{
			throw new System.NotSupportedException();
		}

		public virtual int Read(byte[] buffer, int offset, int length)
		{
			var size = PartSize[Pointer++ % PartSize.Length];
			return _inputStream.Read(buffer, offset, Math.Min(size, length));
		}

		public virtual int Read(byte[] buffer)
		{
			return Read(buffer, 0, buffer.Length);
		}

		public virtual int Write(ByteBuffer buffer)
		{
			throw new System.NotSupportedException();
		}
		public virtual int Write(ByteBuffer buffer, int offset, int length)
		{
			throw new System.NotSupportedException();
		}
		public virtual int Read(ByteBuffer buffer, int offset, int length)
		{
			return Read(buffer.GetByteArray(), offset, length);
		}
		public virtual int Read(ByteBuffer buffer)
		{
			return Read(buffer.GetByteArray());
		}

		public virtual void WaitUntilReadyToWrite() {}

		public IPEndPoint LocalEndPoint => throw new System.NotSupportedException();

		public IPEndPoint RemoteEndPoint => throw new System.NotSupportedException();

		public virtual void Open()
		{
			throw new System.NotSupportedException();
		}

		public virtual void Close()
		{
			throw new System.NotSupportedException();
		}

		public virtual bool IsOpen => throw new System.NotSupportedException();
	}
}
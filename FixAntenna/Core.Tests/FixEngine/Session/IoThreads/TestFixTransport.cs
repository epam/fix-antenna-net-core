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
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.FixEngine.Transport;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.IOThreads
{
	internal class TestFixTransport : IFixTransport
	{
		private static readonly object _lock = new object();

		private IList<byte[]> _chunks = new List<byte[]>();
		private IList<byte[]> _messages = new List<byte[]>();
		private FixMessage _messageToRead;

		public TestFixTransport()
		{
		}

		public TestFixTransport(FixMessage messageToRead)
		{
			_messageToRead = messageToRead;
		}

		public virtual bool IsBlockingSocket => true;

		public virtual void ReadMessage(MsgBuf buf)
		{
			throw new Exception("not implemented");
		}

		public virtual void Write(byte[] message)
		{
			lock (_lock)
			{
				_messages.Add(message);
			}
		}

		public virtual void WaitUntilReadyToWrite() {}

		public virtual int Write(ByteBuffer b, int o, int s)
		{
			var chunk = b.GetSubArray(o, s);
			_chunks.Add(chunk);
			var chopper = new FixMessageChopper(new MemoryStream(chunk, 0, s), 10000, 10000);
			try
			{
				var buf = new MsgBuf();
				while (true)
				{
					chopper.ReadMessage(buf);
					_messages.Add(Copy(buf.Buffer, buf.Offset, buf.Length));
				}
			}
			catch (Exception)
			{
				// catch end of file exception
			}

			return s;
		}

		public virtual int Write(byte[] message, int offset, int length)
		{
			lock (_lock)
			{
				var chunk = Copy(message, offset, length);
				_chunks.Add(chunk);
				var chopper = new FixMessageChopper(new MemoryStream(message, offset, length), 10000, 10000);
				try
				{
					var buf = new MsgBuf();
					while (true)
					{
						chopper.ReadMessage(buf);
						_messages.Add(Copy(buf.Buffer, buf.Offset, buf.Length));
					}
				}
				catch (Exception)
				{
					// catch end of file exception
				}

				return length;
			}
		}

		private byte[] Copy(byte[] message, int offset, int length)
		{
			var copy = new byte[length];
			Array.Copy(message, offset, copy, 0, length);
			return copy;
		}

		public virtual IList<byte[]> GetMessages()
		{
			lock (_lock)
			{
				return new List<byte[]>(_messages);
			}
		}

		public virtual int GetMessagesCount()
		{
			lock (_lock)
			{
				return _messages.Count;
			}
		}

		public virtual IList<byte[]> GetChunks()
		{
			lock (_lock)
			{
				return new List<byte[]>(_chunks);
			}
		}

		public virtual void Close() {}

		public virtual string RemoteHost => "test";

		public virtual int OptimalBufferSize => 200_000;
	}
}
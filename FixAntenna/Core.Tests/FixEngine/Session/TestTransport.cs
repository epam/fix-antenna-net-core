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

using System.Threading;
using System.Net;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.FixEngine.Transport;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	internal class TestTransport : ITransport
	{
		private object _sync = new object();
		private bool _toSessionStreamDone = false;
		private int _toSessionWaitersCount = 0;
		private readonly Queue<ByteBuffer> _toSession = new Queue<ByteBuffer>();
		private readonly Queue<ByteBuffer> _fromSession = new Queue<ByteBuffer>();

		public FixMessage ReadMessageFromSession(int timeoutMs = 100000)
		{
			var buf = ReadDataFromSession(timeoutMs);
			return RawFixUtil.GetFixMessage(buf);
		}

		public byte[] ReadDataFromSession(int timeoutMs = 100000)
		{
			if (TryReadDataFromSession(out var bytes, timeoutMs))
				return bytes;
			throw new Exception($"No data written by session in {timeoutMs} ms");
		}

		public bool TryReadDataFromSession(out byte[] bytes, int timeoutMs = 100000)
		{
			var sw = Stopwatch.StartNew();
			lock (_sync)
			{
				while (_fromSession.Count == 0)
				{
					if (timeoutMs == -1)
						Monitor.Wait(_sync);
					else
					{
						var elapsed = sw.ElapsedMilliseconds;
						if (elapsed >= timeoutMs)
						{
							bytes = null;
							return false;
						}
						Monitor.Wait(_sync, timeoutMs - (int)elapsed);
					}
				}
				bytes = _fromSession.Dequeue().GetByteArray();
				return true;
			}
		}

		public void CloseSessionInputStream()
		{
			lock (_sync)
			{
				_toSessionStreamDone = true;
				Monitor.PulseAll(_sync);
			}
		}

		public void SendDataToSession(byte[] bytes, int timeoutMs = 100000)
		{
			SendDataToSessionNoWait(bytes);
			if (!WaitUntilReaderIsReady(timeoutMs))
				throw new Exception($"Sent data was not processed by session in {timeoutMs} ms");
		}

		public void SendDataToSessionNoWait(byte[] bytes)
		{
			lock (_sync)
			{
				var buf = new ByteBuffer(bytes.Length);
				buf.Add(bytes);
				_toSession.Enqueue(buf);
				Monitor.PulseAll(_sync);
			}
		}

		public bool WaitUntilReaderIsReady(int timeoutMs)
		{
			var sw = Stopwatch.StartNew();
			lock (_sync)
			{
				while (!(_toSession.Count == 0 && _toSessionWaitersCount > 0))
				{
					if (timeoutMs == -1)
						Monitor.Wait(_sync);
					else
					{
						var elapsed = sw.ElapsedMilliseconds;
						if (elapsed >= timeoutMs)
							return false;
						Monitor.Wait(_sync, timeoutMs - (int)elapsed);
					}
				}
				return true;
			}
		}

		// ITransport interface

		public void Close()
		{
		}

		public bool IsOpen => true;

		public bool IsSecured => false;

		public bool IsBlockingSocket => true;

		private static IPEndPoint _remote = new IPEndPoint(IPAddress.Loopback, 4321);
		public IPEndPoint RemoteEndPoint => _remote;

		private static IPEndPoint _local = new IPEndPoint(IPAddress.Loopback, 1234);
		public IPEndPoint LocalEndPoint => _local;

		public void Open()
		{
		}

		public int Read(byte[] buffer, int offset, int length)
		{
			lock (_sync)
			{
				while (_toSession.Count == 0)
				{
					if (_toSessionStreamDone)
						return 0;

					_toSessionWaitersCount++;
					Monitor.PulseAll(_sync);
					Monitor.Wait(_sync);
					_toSessionWaitersCount--;
				}

				var buf = _toSession.Peek();
				var srcBytes = buf.GetByteArray();

				var available = buf.Length - buf.Offset;
				var toWrite = Math.Min(available, length);

				for (var i = 0; i < toWrite; i++)
				{
					buffer[offset + i] = srcBytes[buf.Offset + i];
				}

				if (toWrite < available)
					buf.Offset = buf.Offset + toWrite; // keep the rest in queue
				else
					_toSession.Dequeue();

				Monitor.PulseAll(_sync); // notify state has changed

				return toWrite;
			}
		}

		public int Read(ByteBuffer buffer, int offset, int length)
		{
			return Read(buffer.GetByteArray(), offset, length);
		}

		public int Read(byte[] buffer)
		{
			return Read(buffer, 0, buffer.Length);
		}

		public int Read(ByteBuffer buffer)
		{
			return Read(buffer.GetByteArray());
		}

		public void WaitUntilReadyToWrite()
		{
		}

		public void Write(byte[] message)
		{
			Write(message, 0, message.Length);
		}

		public int Write(ByteBuffer message)
		{
			return Write(message.GetByteArray(), 0, message.Offset + 1);
		}

		public int Write(byte[] message, int offset, int length)
		{
			lock (_sync)
			{
				var buf = new ByteBuffer(length);
				buf.Add(message, offset, length);
				_fromSession.Enqueue(buf);
				Monitor.PulseAll(_sync);
				return length;
			}
		}

		public int Write(ByteBuffer message, int offset, int length)
		{
			return Write(message.GetByteArray(), offset, length);
		}
	}
}
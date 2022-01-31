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

using System.IO;
using System.Threading;
using Epam.FixAntenna.TestUtils.Hooks;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.FixEngine.Transport;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.IOThreads.Helper
{
	internal class FixTransportHelper : IFixTransport
	{
		private volatile FixMessage _message;
		private volatile bool _close = false;
		private readonly EventHook _eventHook = new EventHook("sendMessage", 20000);

		public virtual void SetMessage(FixMessage message)
		{
			_message = message.DeepClone(true, true);
			_eventHook.RaiseEvent();
		}

		public virtual bool IsBlockingSocket => true;

		public virtual byte[] ReadMessage()
		{
			if (_close)
			{
				throw new IOException();
			}

			try
			{
				_eventHook.IsEventRaised();
			}
			catch (ThreadInterruptedException e)
			{
				throw new IOException(e.Message);
			}

			return _message.AsByteArray();
		}

		public virtual void WaitUntilReadyToWrite()
		{
		}

		public virtual int Write(ByteBuffer message, int offset, int length)
		{
			_message = RawFixUtil.GetFixMessage(message.GetSubArray(offset, length));
			return length;
		}

		public virtual void ReadMessage(MsgBuf buf)
		{
			var buffer = ReadMessage();
			buf.Buffer = buffer;
			buf.Offset = 0;
			buf.Length = buffer.Length;
			buf.FixMessage = _message;
		}

		public virtual void Write(byte[] message)
		{
			_message = RawFixUtil.GetFixMessage(message);
		}

		public virtual int Write(byte[] message, int offset, int length)
		{
			_message = RawFixUtil.GetFixMessage(message, offset, length);
			return length;
		}

		public virtual void Close()
		{
			_message = null;
			_close = true;
			_eventHook.RaiseEvent();
			_eventHook.ResetEvent();
		}

		public virtual string RemoteHost => "test";

		public virtual int OptimalBufferSize => _message?.RawLength ?? 0;
	}
}
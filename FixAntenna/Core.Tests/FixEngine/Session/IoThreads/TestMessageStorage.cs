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
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Storage;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.IOThreads
{
	internal class TestMessageStorage : IMessageStorage
	{
		private IList<object> _messages = new List<object>();
		private volatile bool _disposed;

		public virtual void AppendMessage(byte[] message, int offset, int length)
		{
			lock (this)
			{
				var destMessage = new byte[length];
				Array.Copy(message, 0, destMessage, 0, length);
				_messages.Add(destMessage);
			}
		}

		public virtual void AppendMessage(byte[] message)
		{
			lock (this)
			{
				_messages.Add(message);
			}
		}

		public virtual byte[] RetrieveMessage(long seqNumber)
		{
			return new byte[0];
		}

		public virtual void RetrieveMessages(long fromSeqNum, long toSeqNun, IMessageStorageListener listener, bool blocking)
		{
		}

		public virtual long Initialize()
		{
			return 0;
		}

		public virtual void Close()
		{

		}

		public virtual IList<object> GetMessages()
		{
			lock (this)
			{
				return new List<object>(_messages);
			}
		}

		public virtual void BackupStorage(SessionParameters sessionParameters)
		{
			//To change body of implemented methods use File | Settings | File Templates.
		}

		public void AppendMessage(byte[] timestampFormatted, byte[] message, int offset, int length)
		{
			throw new NotImplementedException();
		}

		public void AppendMessage(byte[] timestampFormatted, byte[] message)
		{
			throw new NotImplementedException();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					Close();
				}

				_disposed = true;
			}
		}
	}
}
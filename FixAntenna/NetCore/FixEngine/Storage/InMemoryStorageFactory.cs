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
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.FixEngine.Storage.Queue;

namespace Epam.FixAntenna.NetCore.FixEngine.Storage
{
	/// <summary>
	/// In memory storage factory implementation.
	/// </summary>
	internal class InMemoryStorageFactory : IStorageFactory
	{
		public InMemoryStorageFactory(Config configuration)
		{
		}

		/// <inheritdoc />
		public virtual IMessageStorage CreateIncomingMessageStorage(SessionParameters details)
		{
			return new DefaultMemoryMessageStorage();
		}

		/// <inheritdoc />
		public virtual IMessageStorage CreateOutgoingMessageStorage(SessionParameters details)
		{
			return new DefaultMemoryMessageStorage();
		}

		/// <inheritdoc />
		public virtual void SaveSessionParameters(SessionParameters details, FixSessionRuntimeState state)
		{
		}

		/// <inheritdoc />
		public virtual bool LoadSessionParameters(SessionParameters sessionParameters, FixSessionRuntimeState state)
		{
			return false;
		}

		/// <summary>
		/// Create and return a InMemoryQueue instance.
		/// </summary>
		/// <param name="sessionParameters"> the session parameters </param>
		public virtual IQueue<FixMessageWithType> CreateQueue(SessionParameters sessionParameters)
		{
			return new InMemoryQueue<FixMessageWithType>();
		}

		public virtual void RestoreSessionParameters(SessionParameters sessionParameters)
		{
		}

		private class DefaultMemoryMessageStorage : IMessageStorage
		{
			public virtual void AppendMessage(byte[] message)
			{
			}

			public void AppendMessage(byte[] timestampFormatted, byte[] message, int offset, int length)
			{
				AppendMessage(message, offset, length); // todo: or better to throw exception here?
			}

			public void AppendMessage(byte[] timestampFormatted, byte[] message)
			{
				AppendMessage(message); // todo: or better to throw exception here?
			}

			public virtual void AppendMessage(byte[] message, int offset, int length)
			{
			}

			public virtual byte[] RetrieveMessage(long seqNumber)
			{
				throw new IOException("Not supported");
			}

			public virtual void RetrieveMessages(long from, long to, IMessageStorageListener listener, bool blocking)
			{
				throw new IOException("Not supported");
			}

			public virtual long Initialize()
			{
				return 1;
			}

			public virtual void Close()
			{
			}

			public virtual void BackupStorage(SessionParameters sessionParameters)
			{
			}

			public void Dispose()
			{
			}
		}
	}
}
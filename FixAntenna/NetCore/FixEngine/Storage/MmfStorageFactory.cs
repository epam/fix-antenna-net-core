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

using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.FixEngine.Storage.File;
using Epam.FixAntenna.NetCore.FixEngine.Storage.Queue;

namespace Epam.FixAntenna.NetCore.FixEngine.Storage
{
	/// <summary>
	/// Provides ability to store messages in a file using the memory mapped files technology.
	/// </summary>
	internal class MmfStorageFactory : FilesystemStorageFactory
	{
		public MmfStorageFactory(Config configuration) : base(configuration)
		{
		}

		/// <summary>
		/// Gets incoming message storage.
		/// If parameter <c>incomingStorageIndexed</c> configured, the message storage will be <see cref="MmfIndexedMessageStorage"/>,
		/// otherwise <see cref="MmfMessageStorage"/>
		/// </summary>
		/// <param name="sessionParameters"> the session parameters </param>
		/// <seealso cref="IStorageFactory"> </seealso>
		public override IMessageStorage CreateIncomingMessageStorage(SessionParameters sessionParameters)
		{
			AbstractFileMessageStorage storage;
			if (ConfigAdapter.IsIncomingStorageIndexed)
			{
				storage = new MmfIndexedMessageStorage(sessionParameters.Configuration);
			}
			else
			{
				storage = new MmfMessageStorage(sessionParameters.Configuration);
			}

			storage.FileName = IncomingLogFileLocator.GetFileName(sessionParameters);
			storage.SetBackupFileLocator(BackupIncomingLogFileLocator);
			return storage;
		}

		/// <summary>
		/// Gets outgoing message storage.
		/// If parameter <c>outgoingStorageIndexed</c> configured, the message storage will be <see cref="MmfIndexedMessageStorage"/>,
		/// otherwise <see cref="MmfMessageStorage"/>
		/// </summary>
		/// <param name="sessionParameters"> the session parameters </param>
		public override IMessageStorage CreateOutgoingMessageStorage(SessionParameters sessionParameters)
		{
			AbstractFileMessageStorage storage;
			if (ConfigAdapter.IsOutgoingStorageIndexed)
			{
				storage = new MmfIndexedMessageStorage(sessionParameters.Configuration);
			}
			else
			{
				storage = new MmfMessageStorage(sessionParameters.Configuration);
			}

			storage.FileName = OutgoingLogFileLocator.GetFileName(sessionParameters);
			storage.SetBackupFileLocator(BackupOutgoingLogFileLocator);
			return storage;
		}

		/// <summary>
		/// Gets queue for session.
		/// If parameter <c>inMemoryQueue</c> configured, the queue will be <see cref="InMemoryQueue{T}"/>,
		/// otherwise <see cref="PersistentInMemoryQueue{T}"/>
		/// </summary>
		/// <param name="sessionParameters"> the parameter for session </param>
		public override IQueue<FixMessageWithType> CreateQueue(SessionParameters sessionParameters)
		{
			if (ConfigAdapter.PreferredSendingMode == SendingMode.SyncNoqueue || ConfigAdapter.IsInMemoryQueueEnabled)
			{
				return new InMemoryQueue<FixMessageWithType>();
			}

			if (ConfigAdapter.IsMemoryMappedQueueEnabled)
			{
				return new MmfPersistentInMemoryQueue<FixMessageWithType>(
					QueueFileLocator.GetFileName(sessionParameters), new FixMessageWithTypeFactory());
			}

			return new PersistentInMemoryQueue<FixMessageWithType>(QueueFileLocator.GetFileName(sessionParameters),
				new FixMessageWithTypeFactory());
		}
	}
}
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
using Epam.FixAntenna.NetCore.FixEngine.Storage.File;

namespace Epam.FixAntenna.NetCore.FixEngine.Storage
{
	internal class SlicedFileStorageFactory : FilesystemStorageFactory
	{
		/// <summary>
		/// Creates the <c>SlicedFileStorageFactory</c> storage.
		/// </summary>
		public SlicedFileStorageFactory(Config configuration) : base(configuration)
		{
		}

		/// <summary>
		/// Gets incoming message storage.
		/// If parameter <c>incomingStorageIndexed</c> configured,
		/// the message storage will be <c>SlicedIndexedMessageStorage</c>,
		/// otherwise <c>SlicedFileMessageStorage</c>
		/// </summary>
		/// <param name="sessionParameters"> the session parameters </param>
		/// <seealso cref="IStorageFactory"> </seealso>
		public override IMessageStorage CreateIncomingMessageStorage(SessionParameters sessionParameters)
		{
			AbstractFileMessageStorage storage = ConfigAdapter.IsIncomingStorageIndexed
				? new SlicedIndexedMessageStorage(sessionParameters.Configuration)
				: new SlicedFileMessageStorage(sessionParameters.Configuration);

			storage.FileName = IncomingLogFileLocator.GetFileName(sessionParameters);
			storage.SetBackupFileLocator(BackupIncomingLogFileLocator);
			return storage;
		}

		/// <summary>
		/// Gets outgoing message storage.
		/// If parameter <c>outgoingStorageIndexed</c> configured,
		/// the message storage will be <c>SlicedIndexedMessageStorage</c>,
		/// otherwise <c>SlicedFileMessageStorage</c>
		/// </summary>
		/// <param name="sessionParameters"> the session parameters </param>
		public override IMessageStorage CreateOutgoingMessageStorage(SessionParameters sessionParameters)
		{
			AbstractFileMessageStorage storage = ConfigAdapter.IsOutgoingStorageIndexed
				? new SlicedIndexedMessageStorage(sessionParameters.Configuration)
				: new SlicedFileMessageStorage(sessionParameters.Configuration);

			storage.FileName = OutgoingLogFileLocator.GetFileName(sessionParameters);
			storage.SetBackupFileLocator(BackupOutgoingLogFileLocator);
			return storage;
		}
	}
}
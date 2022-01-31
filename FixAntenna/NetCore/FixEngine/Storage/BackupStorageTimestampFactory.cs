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

using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.FixEngine.Storage.Timestamp;

namespace Epam.FixAntenna.NetCore.FixEngine.Storage
{
	internal class BackupStorageTimestampFactory
	{
		/// <summary>
		/// Returns an appropriate StorageTimestamp implementation
		/// </summary>
		/// <returns> StorageTimestamp implementation </returns>
		public static IStorageTimestamp GetStorageTimestamp(Config configuration)
		{
			var precision = new ConfigurationAdapter(configuration).BackupTimestampsPrecision;
			switch (precision)
			{
				case TimestampPrecision.Micro:
					return new StorageTimestampMicro();
				case TimestampPrecision.Nano:
					return new StorageTimestampNano();
				default:
					return new StorageTimestampMilli();
			}
		}
	}
}
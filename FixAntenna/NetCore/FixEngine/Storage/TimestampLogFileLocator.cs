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
using System.Text;
using Epam.FixAntenna.NetCore.Helpers;

namespace Epam.FixAntenna.NetCore.FixEngine.Storage
{
	internal class TimestampLogFileLocator : DefaultLogFileLocator
	{
		private const int MaxAttemptsToGetUniqueFile = 10;

		public TimestampLogFileLocator(string dir, string nameTemplate) : base(dir, nameTemplate)
		{
		}

		/// <inheritdoc />
		public override string GetFileName(SessionParameters details)
		{
			var directory = GetDirectory();

			var storageTimestamp = BackupStorageTimestampFactory.GetStorageTimestamp(details.Configuration);
			var tmstmpPart = StringHelper.NewString(storageTimestamp.FormatBackup());

			for (var i = 0; i < MaxAttemptsToGetUniqueFile; i++)
			{
				var fileName = string.Format(GetNameTemplate(), details.SessionId.ToString(),
					details.SenderCompId,
					details.TargetCompId, i == 0 ? tmstmpPart : tmstmpPart + "(" + i + ")",
					details.SessionQualifier == null ? "" : details.SessionQualifier);
				var sb = new StringBuilder(directory);
				sb.Append(Path.DirectorySeparatorChar).Append(fileName);

				var fileNameWithDirectory = sb.ToString();
				var file = new FileInfo(fileNameWithDirectory);
				if (!file.Exists)
				{
					return fileNameWithDirectory;
				}
			}

			throw new InvalidOperationException("Failed to create unique filename in " +
												MaxAttemptsToGetUniqueFile + " attempts");
		}
	}
}
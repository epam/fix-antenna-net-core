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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Epam.FixAntenna.NetCore.Common.Logging;

namespace Epam.FixAntenna.TestUtils
{
	internal class LogsCleaner
	{
		private readonly ILog _log;

		public LogsCleaner()
		{
			_log = LogFactory.GetLog(GetType());
		}

		public virtual void Clean()
		{
			var logDir = new DirectoryInfo("./logs");
			if (!logDir.Exists)
			{
				return;
			}

			var logFiles = logDir.GetFileSystemInfos().Where(x => ShouldBeCleaned(x.Name)).ToList();
			if (!logFiles.Any())
			{
				return;
			}

			var deletedFiles = 0;
			var skipped = new StringBuilder("Skipped: ");
			var skipFilesNames = false;
			foreach (var logFile in logFiles)
			{
				try
				{
					logFile.Delete();
					deletedFiles++;
				}
				catch (Exception)
				{
					if (skipFilesNames)
					{
						skipped.Append(", ");
					}

					skipFilesNames = true;
					skipped.Append(logFile.Name);
				}
			}

			_log.Debug("Successfully deleted " + (deletedFiles + 1) + " of " + logFiles.Count + " files");
		}

		public virtual bool Clean(string dir)
		{
			var logDir = new DirectoryInfo(dir);
			if (!logDir.Exists)
			{
				return true;
			}

			var logFiles = logDir.GetFileSystemInfos().Where(x => ShouldBeCleaned(x.Name)).ToList();
			if (logFiles.Any())
			{
				var deletedFilesList = new List<FileSystemInfo>();
				var deletedFiles = 0;
				foreach (var logFile in logFiles)
				{
					try
					{
						logFile.Delete();
						deletedFiles++;
						deletedFilesList.Add(logFile);
					}
					catch (Exception)
					{
						// ignored
					}
				}

				_log.Debug("Successfully deleted " + deletedFiles + " of " + logFiles.Count + " files in " + dir);

				if (deletedFiles != logFiles.Count)
				{
					var sb = new StringBuilder();
					foreach (var f in deletedFilesList)
					{
						sb.Append(f.Name).Append("\t");
					}

					_log.Info("Skipped: " + sb.ToString() + " in " + dir);
					return false;
				}
			}
			else
			{
				_log.Debug("No filed to delete in " + dir);
			}

			return true;
		}

		private bool ShouldBeCleaned(string name)
		{
			return Regex.IsMatch(name, ".*\\.que.*")
					|| Regex.IsMatch(name, ".*\\.seq.*")
					|| Regex.IsMatch(name, ".*\\.in$")
					|| Regex.IsMatch(name, ".*\\.out$")
					|| Regex.IsMatch(name, ".*\\.log$")
					|| Regex.IsMatch(name, ".*\\.outq$")
					|| Regex.IsMatch(name, ".*\\.idx$")
					|| Regex.IsMatch(name, ".*\\.[0-9]{1,5}$")
					|| Regex.IsMatch(name, ".*\\.properties$");
		}
	}
}
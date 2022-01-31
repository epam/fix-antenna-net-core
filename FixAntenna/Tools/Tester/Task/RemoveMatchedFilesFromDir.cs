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
using System.Text.RegularExpressions;
using Epam.FixAntenna.NetCore.Common.Logging;

namespace Epam.FixAntenna.Tester.Task
{
	public sealed class RemoveMatchedFilesFromDir : ITask
	{
		private static readonly ILog _log = LogFactory.GetLog(typeof(RemoveMatchedFilesFromDir));

		private string _logs;
		private string _match;

		public void Init(IDictionary<string, string> @params, CustomConcurrentDictionary<string, object> session)
		{
			_logs = @params[TesterParams.LOGS_PARAM];
			_match = @params[TesterParams.MATCH_PARAM];
		}

		public void DoTask()
		{
			if (_log.IsDebugEnabled)
			{
				_log.Debug("Cleaning logs in: " + _logs);
			}

			bool success = false;

			if (Directory.Exists(_logs))
			{
				var files = Directory.GetFiles(_logs);

				foreach (var fn in files)
				{
					if (Regex.IsMatch(fn, _match))
					{
						try
						{
							File.Delete(fn);
							_log.Trace($"File deleted: {fn}");
							success = true;
						}
						catch (Exception ex)
						{
							_log.Warn($"File not deleted: {fn} - {ex.Message}");
						}

					}
				}
			}

			if (!success)
			{
				_log.Debug("No files were deleted");
			}
		}

		public void Dispose()
		{
		}
	}
}
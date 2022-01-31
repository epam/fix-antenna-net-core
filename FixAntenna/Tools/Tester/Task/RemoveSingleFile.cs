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
using Epam.FixAntenna.NetCore.Common.Logging;

namespace Epam.FixAntenna.Tester.Task
{
	public sealed class RemoveSingleFile : ITask
	{
		private bool InstanceFieldsInitialized = false;

		public RemoveSingleFile()
		{
			if (!InstanceFieldsInitialized)
			{
				InitializeInstanceFields();
				InstanceFieldsInitialized = true;
			}
		}

		private void InitializeInstanceFields()
		{
			_log = LogFactory.GetLog(this.GetType());
		}

		private ILog _log;
		private string _logs;

		public void Init(IDictionary<string, string> @params, CustomConcurrentDictionary<string, object> session)
		{
			_logs = @params[TesterParams.FILE];
		}

		public void DoTask()
		{
			if (_log.IsDebugEnabled)
			{
				_log.Debug("Cleaning " + TesterParams.FILE + ": " + _logs);
			}
			try
			{
				File.Delete(_logs);
				_log.Trace($"File deleted: {_logs}");
			}
			catch (Exception ex)
			{
				_log.Warn($"File not deleted: {_logs} - {ex.Message}");
			}
		}

		public void Dispose()
		{
		}
	}

}
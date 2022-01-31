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
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Configuration;

namespace Epam.FixAntenna.Tester.Task.FixEngine
{
	public sealed class SetGlobalConfig : ITask
	{
		private static readonly ILog _log = LogFactory.GetLog(typeof(SetGlobalConfig));
		private const string SOURCE_FILE_PARAM = "sourceFile";

		private string _sourceFile;
		private IDictionary<string, string> _params;

		public void Init(IDictionary<string, string> @params, CustomConcurrentDictionary<string, object> session)
		{
			_params = @params;
			_sourceFile = @params[SOURCE_FILE_PARAM];
		}

		public void DoTask()
		{
			if (_sourceFile != null)
			{
				_log.Info("Load global config from file "+ _sourceFile);
				var config = new Config(_sourceFile);
				Config.GlobalConfiguration.SetAllProperties(config.Properties);
			}

			foreach (var keyName in _params.Keys)
			{
				if (keyName.StartsWith("global_", StringComparison.Ordinal))
				{
					string paramName = keyName.Substring("global_".Length);
					string paramValue = _params[keyName];
					Config.GlobalConfiguration.SetProperty(paramName, paramValue);
				}
			}
		}

		public void Dispose()
		{
		}
	}
}
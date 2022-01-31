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
using Epam.FixAntenna.NetCore.Common.Logging;

namespace Epam.FixAntenna.Tester.Logger
{
	public class LoggingCaseLogger : ICaseLogger
	{
		private static readonly ILog _log = LogFactory.GetLog("TEST_LOGGER");

		static LoggingCaseLogger()
		{
			_log.Info("============================ Starting at " + DateTime.Now + " =============================");
		}

		public LoggingCaseLogger()
		{
			_log.Info("----------------------------------------------------------------------------------------");
		}

		public virtual void LogOk(string name)
		{
			_log.Info(name + " - OK");
		}

		public virtual void LogError(string name, string expected, string actual)
		{
			_log.Info(name + " - ERROR");
			_log.Debug("Expected:" + expected);
			_log.Debug("  Actual:" + actual);
		}

		public virtual void LogError(string name)
		{
			_log.Info(name + " - ERROR");
		}


		public virtual void LogError(string name, string reason)
		{
			_log.Info(name + " - ERROR{" + reason + '}');
		}

		public virtual void LogExpected(string expected)
		{
			_log.Debug("Expected:" + expected);
		}

		public virtual void LogActual(string actual)
		{
			_log.Debug("  Actual:" + actual);
		}
	}

}
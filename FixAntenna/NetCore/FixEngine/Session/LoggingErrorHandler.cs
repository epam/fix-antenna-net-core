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

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	internal class LoggingErrorHandler : IErrorHandler
	{
		private readonly ILog _log = LogFactory.GetLog(typeof(LoggingErrorHandler));

		/// <inheritdoc />
		public virtual void OnWarn(string description, Exception st)
		{
			if (_log.IsDebugEnabled)
			{
				_log.Warn(description, st);
			}
			else
			{
				_log.Warn(description + ". Reason: " + st.Message);
			}
		}

		/// <inheritdoc />
		public virtual void OnError(string description, Exception st)
		{
			if (_log.IsDebugEnabled)
			{
				_log.Error(description, st);
			}
			else
			{
				_log.Error(description + ". Reason: " + st.Message);
			}
		}

		/// <inheritdoc />
		public virtual void OnFatalError(string description, Exception st)
		{
			if (_log.IsDebugEnabled)
			{
				_log.Fatal(description, st);
			}
			else
			{
				_log.Fatal(description);
			}
		}
	}
}
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
using NLog;

namespace Epam.FixAntenna.NetCore.Common.Logging
{
	internal class NLogWrapper : BaseLog, ILog
	{
		private static readonly string FatalMarker = "FATAL:";
		private readonly ILogger _log;

		public NLogWrapper(ILogger logger)
		{
			_log = logger;
		}

		/// <inheritdoc />
		public void Debug(object message)
		{
			var msg = ToMessage(message);
			_log.Debug(msg);
		}

		/// <inheritdoc />
		public void Debug(object message, Exception throwable)
		{
			var msg = ToMessage(message);
			_log.Debug(throwable, msg);
		}

		/// <inheritdoc />
		public void Error(object message)
		{
			var msg = ToMessage(message);
			_log.Error(msg);
		}

		/// <inheritdoc />
		public void Error(object message, Exception throwable)
		{
			var msg = ToMessage(message);
			_log.Error(throwable, msg);
		}

		/// <inheritdoc />
		public void Fatal(object message)
		{
			var msg = ToMessage(message);
			_log.Fatal(FatalMarker + msg);
		}

		/// <inheritdoc />
		public void Fatal(object message, Exception throwable)
		{
			var msg = ToMessage(message);
			_log.Fatal(throwable, FatalMarker + msg);
		}

		/// <inheritdoc />
		public void Info(object message)
		{
			var msg = ToMessage(message);
			_log.Info(msg);
		}

		/// <inheritdoc />
		public void Info(object message, Exception throwable)
		{
			var msg = ToMessage(message);
			_log.Info(throwable, msg);
		}

		/// <inheritdoc />
		public bool IsDebugEnabled => _log.IsDebugEnabled;

		/// <inheritdoc />
		public bool IsErrorEnabled => _log.IsErrorEnabled;

		/// <inheritdoc />
		public bool IsFatalEnabled => _log.IsFatalEnabled;

		/// <inheritdoc />
		public bool IsInfoEnabled => _log.IsInfoEnabled;

		/// <inheritdoc />
		public bool IsTraceEnabled => _log.IsTraceEnabled;

		/// <inheritdoc />
		public bool IsWarnEnabled => _log.IsWarnEnabled;

		/// <inheritdoc />
		public void Trace(object message)
		{
			var msg = ToMessage(message);
			_log.Trace(msg);
		}

		/// <inheritdoc />
		public void Trace(object message, Exception throwable)
		{
			var msg = ToMessage(message);
			_log.Trace(throwable, msg);
		}

		/// <inheritdoc />
		public void Warn(object message)
		{
			var msg = ToMessage(message);
			_log.Warn(msg);
		}

		/// <inheritdoc />
		public void Warn(object message, Exception throwable)
		{
			var msg = ToMessage(message);
			_log.Warn(throwable, msg);
		}
	}
}

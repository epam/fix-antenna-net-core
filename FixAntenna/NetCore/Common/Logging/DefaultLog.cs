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

namespace Epam.FixAntenna.NetCore.Common.Logging
{
	/// <summary>
	/// Default log implementation.
	/// This log supports only <c>error, fatal and warn</c> levels.
	/// </summary>
	internal class DefaultLog : BaseLog, ILog
	{
		/// <summary>
		/// Always returns false. </summary>
		/// <seealso cref="ILog.IsDebugEnabled"></seealso>
		public bool IsDebugEnabled => false;

		/// <inheritdoc />
		public void Debug(object message)
		{
		}

		/// <inheritdoc />
		public void Debug(object message, Exception throwable)
		{
		}

		/// <summary>
		/// Always returns true. </summary>
		/// <seealso cref="ILog.IsErrorEnabled"></seealso>
		public bool IsErrorEnabled => true;

		/// <summary>
		/// Writes error message to err output stream. </summary>
		/// <seealso cref="ILog.Error(object)"></seealso>
		public void Error(object message)
		{
			Console.Error.WriteLine(ToMessage(message));
		}

		/// <summary>
		/// Writes error message to err output stream. </summary>
		/// <seealso cref="ILog.Error(object, Exception)"></seealso>
		public void Error(object message, Exception throwable)
		{
			Console.Error.WriteLine(ToMessage(message) + "\n" + throwable.ToString());
		}

		/// <summary>
		/// Always returns true. </summary>
		/// <seealso cref="ILog.IsFatalEnabled"></seealso>
		public bool IsFatalEnabled => true;

		/// <summary>
		/// Writes error message to err output stream. </summary>
		/// <seealso cref="ILog.Fatal(object)"></seealso>
		public void Fatal(object message)
		{
			Console.WriteLine(ToMessage(message));
		}

		/// <summary>
		/// Writes error message to err output stream. </summary>
		/// <seealso cref="ILog.Fatal(object, Exception)"></seealso>
		public void Fatal(object message, Exception throwable)
		{
			Console.Error.WriteLine(ToMessage(message) + "\n" + throwable.ToString());
		}

		/// <inheritdoc />
		public bool IsInfoEnabled => false;

		/// <inheritdoc />
		public void Info(object message)
		{
		}

		/// <inheritdoc />
		public void Info(object message, Exception throwable)
		{
		}

		/// <summary>
		/// Always returns false. </summary>
		/// <seealso cref="ILog.IsTraceEnabled"></seealso>
		public bool IsTraceEnabled => false;

		/// <inheritdoc />
		public void Trace(object message)
		{
		}

		/// <inheritdoc />
		public void Trace(object message, Exception throwable)
		{
		}

		/// <summary>
		/// Always returns true. </summary>
		/// <seealso cref="ILog.IsWarnEnabled"></seealso>
		public bool IsWarnEnabled => true;

		/// <summary>
		/// Writes error message to output stream. </summary>
		/// <seealso cref="ILog.Warn(object)"></seealso>
		public void Warn(object message)
		{
			Console.WriteLine(ToMessage(message));
		}

		/// <summary>
		/// Writes error message to output stream. </summary>
		/// <seealso cref="ILog.Warn(object, Exception)"></seealso>
		public void Warn(object message, Exception throwable)
		{
			Console.WriteLine(ToMessage(message) + "\n" + throwable.ToString());
		}
	}
}
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
	/// The base logging interface.
	/// </summary>
	public interface ILog
	{
		/// <summary>
		/// Log a message object with the DEBUG level. </summary>
		/// <param name="message"> the message
		///  </param>
		void Debug(object message);

		/// <summary>
		/// Log a message object with the DEBUG level including the stack trace of the Throwable. </summary>
		/// <param name="message"> the message </param>
		/// <param name="throwable"> the error
		///  </param>
		void Debug(object message, Exception throwable);

		/// <summary>
		/// Log a message object with the ERROR level. </summary>
		/// <param name="message"> the message
		///  </param>
		void Error(object message);

		/// <summary>
		/// Log a message object with the DEBUG level including the stack trace of the Throwable. </summary>
		/// <param name="message"> the message </param>
		/// <param name="throwable"> the error
		///  </param>
		void Error(object message, Exception throwable);

		/// <summary>
		/// Log a message object with the FATAL level. </summary>
		/// <param name="message"> the message
		///  </param>
		void Fatal(object message);

		/// <summary>
		/// Log a message object with the FATAL level including the stack trace of the Throwable. </summary>
		/// <param name="message"> the message </param>
		/// <param name="throwable"> the error
		///  </param>
		void Fatal(object message, Exception throwable);

		/// <summary>
		/// Log a message object with the INFO level. </summary>
		/// <param name="message"> the message
		///  </param>
		void Info(object message);

		/// <summary>
		/// Log a message object with the INFO level including the stack trace of the Throwable. </summary>
		/// <param name="message"> the message </param>
		/// <param name="throwable"> the error
		///  </param>
		void Info(object message, Exception throwable);

		/// <summary>
		/// Check whether this category is enabled for the DEBUG Level.
		/// </summary>
		bool IsDebugEnabled { get; }

		/// <summary>
		/// Check whether this category is enabled for the ERROR Level.
		/// </summary>
		bool IsErrorEnabled { get; }

		/// <summary>
		/// Check whether this category is enabled for the FATAL Level.
		/// </summary>
		bool IsFatalEnabled { get; }

		/// <summary>
		/// Check whether this category is enabled for the INFO Level.
		/// </summary>
		bool IsInfoEnabled { get; }

		/// <summary>
		/// Check whether this category is enabled for the TRACE Level.
		/// </summary>
		bool IsTraceEnabled { get; }

		/// <summary>
		/// Check whether this category is enabled for the WARN Level.
		/// </summary>
		bool IsWarnEnabled { get; }

		/// <summary>
		/// Log a message object with the TRACE level. </summary>
		/// <param name="message"> the message </param>
		void Trace(object message);

		/// <summary>
		/// Log a message object with the trace level including the stack trace of the Throwable. </summary>
		/// <param name="message"> the message </param>
		/// <param name="throwable"> the error
		///  </param>
		void Trace(object message, Exception throwable);

		/// <summary>
		/// Log a message object with the WARN level. </summary>
		/// <param name="message"> the message
		///  </param>
		void Warn(object message);

		/// <summary>
		/// Log a message object with the WARN level including the stack trace of the Throwable. </summary>
		/// <param name="message"> the message </param>
		/// <param name="throwable"> the error
		///  </param>
		void Warn(object message, Exception throwable);
	}
}
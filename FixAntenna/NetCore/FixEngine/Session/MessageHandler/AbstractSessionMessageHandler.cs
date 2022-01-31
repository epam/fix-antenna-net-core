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
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler
{
	/// <summary>
	/// The abstract session message handler
	/// </summary>
	internal abstract class AbstractSessionMessageHandler : ISessionMessageHandler
	{
		protected internal readonly ILog Log;
		protected readonly bool LogIsTraceEnabled;

		public AbstractSessionMessageHandler()
		{
			Log = LogFactory.GetLog(GetType());
			LogIsTraceEnabled = Log.IsTraceEnabled;
		}

		/// <inheritdoc />
		public virtual IExtendedFixSession Session { get; set; }

		public virtual void LogWarnToSession(string description, Exception throwable)
		{
			Session.ErrorHandler.OnWarn(description, throwable);
		}

		public virtual void LogErrorToSession(string description, Exception throwable)
		{
			Session.ErrorHandler.OnError(description, throwable);
		}

		/// <inheritdoc />
		public abstract void OnNewMessage(FixMessage message);
	}
}
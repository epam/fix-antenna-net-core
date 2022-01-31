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

using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global
{
	internal class MsgLoggingHandler : AbstractGlobalMessageHandler
	{
		private bool _instanceFieldsInitialized = false;

		public MsgLoggingHandler()
		{
			if (!_instanceFieldsInitialized)
			{
				InitializeInstanceFields();
				_instanceFieldsInitialized = true;
			}
		}

		private void InitializeInstanceFields()
		{
			Log = LogFactory.GetLog(this.GetType());
		}

		internal const string LoggerPrefix = "Epam.FixAntenna.NetCore.FixEngine.inmsg";
		protected internal new ILog Log;
		protected internal bool IsDebugEnabled = false;
		private string _sessionId;

		/// <inheritdoc />
		public override IExtendedFixSession Session
		{
			set
			{
				_sessionId = value.Parameters.SessionId.ToString();
				Log = LogFactory.GetLog(LoggerPrefix + "." + _sessionId);
				IsDebugEnabled = Log.IsDebugEnabled;

				base.Session = value;
			}
		}

		/// <inheritdoc />
		public override void OnNewMessage(FixMessage message)
		{
			if (IsDebugEnabled)
			{
				Log.Debug(message.ToPrintableString());
			}
			CallNextHandler(message);
		}

		public static bool IsLoggingEnabled()
		{
			var log = LogFactory.GetLog(LoggerPrefix);
			return log.IsDebugEnabled;
		}
	}
}
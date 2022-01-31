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
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.PerType;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global
{
	internal class EnhancedRrMessageHandler : AbstractGlobalMessageHandler
	{
		private bool _instanceFieldsInitialized = false;
		protected internal new ILog Log;
		public const string SystemMessagehandler2 = "system.messagehandler.2";
		private ISessionMessageHandler _origRrHandler;


		public EnhancedRrMessageHandler()
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

		/// <inheritdoc />
		public override IExtendedFixSession Session
		{
			set
			{
				base.Session = value;
				_origRrHandler = GetResendRequestHandler(value);
				_origRrHandler.Session = value;
			}
		}

		private ISessionMessageHandler GetResendRequestHandler(IExtendedFixSession fixSession)
		{
			var configuration = fixSession.Parameters.Configuration;
			var className = configuration.GetProperty(SystemMessagehandler2, typeof(ResendRequestMessageHandler).FullName);

			try
			{
				return (ISessionMessageHandler)Activator.CreateInstance(Type.GetType(className));
			}
			catch (Exception e)
			{
				Log.Fatal("Unable to load session level message handler", e);
				return new ResendRequestMessageHandler();
			}
		}

		/// <inheritdoc />
		public override void OnNewMessage(FixMessage message)
		{
			var msgType = message.MsgType;
			if (msgType.Length == 1 && msgType[0] == (byte)'2')
			{
				ProcessRr(message);
			}
			NextHandler.OnNewMessage(message);
		}

		private void ProcessRr(FixMessage message)
		{
			_origRrHandler.OnNewMessage(message);
		}
	}
}
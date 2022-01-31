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

using System.IO;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.FixEngine.Transport;
using Epam.FixAntenna.NetCore.FixEngine.Transport.Server;
using Epam.FixAntenna.NetCore.FixEngine.Transport.Server.Firewall;

namespace Epam.FixAntenna.NetCore.FixEngine
{
	internal class ConnectionValidatorListener : PassthroughConnectionListener
	{
		private bool _instanceFieldsInitialized = false;

		private void InitializeInstanceFields()
		{
			_log = LogFactory.GetLog(this.GetType());
		}

		private ILog _log;
		private IConnectionValidator _validator;

		public ConnectionValidatorListener(IConnectionListener listener, IConnectionValidator validator) : base(listener)
		{
			if (!_instanceFieldsInitialized)
			{
				InitializeInstanceFields();
				_instanceFieldsInitialized = true;
			}
			this._validator = validator;
		}

		/// <inheritdoc />
		public override void OnConnect(ITransport transport)
		{
			if (_validator.Allow(transport.RemoteEndPoint.Address))
			{
				base.OnConnect(transport);
			}
			else
			{
				if (_log.IsDebugEnabled)
				{
					_log.Debug($"New incoming connection from {transport.RemoteEndPoint.AsString()} will be rejected");
				}
				try
				{
					transport.Close();
				}
				catch (IOException e)
				{
					if (_log.IsDebugEnabled)
					{
						_log.Warn("Can't close acceptor transport", e);
					}
					else
					{
						_log.Warn($"Can't close acceptor transport. {e.Message}");
					}
				}
			}
		}
	}
}
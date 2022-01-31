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

using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global
{
	/// <summary>
	/// The version consistency handler.
	/// </summary>
	internal class VersionConsistencyHandler : AbstractGlobalMessageHandler
	{
		protected internal new static readonly ILog Log = LogFactory.GetLog(typeof(VersionConsistencyHandler));
		private const string Reason = "FIX version changed suddenly";
		private TagValue _versionValue = new TagValue();
		private string _sessionVersionStr = "";
		private byte[] _sessionVersionBytes;

		/// <summary>
		/// This handler checks the 8 tag in all incoming message.
		/// </summary>
		/// <seealso cref="IFixMessageListener.OnNewMessage"> </seealso>
		public override void OnNewMessage(FixMessage message)
		{
			HandleMessage(message);
		}

		private void HandleMessage(FixMessage message)
		{
			if (message.IsTagExists(Tags.BeginString))
			{
				message.LoadTagValue(Tags.BeginString, _versionValue);
				var sessionVersion = Session.Parameters.FixVersion.MessageVersion;
				if (!_sessionVersionStr.Equals(sessionVersion))
				{
					_sessionVersionStr = sessionVersion;
					_sessionVersionBytes = sessionVersion.AsByteArray();
				}
				if (_sessionVersionBytes.Length != _versionValue.Length)
				{
					Disconnect();
					throw new GarbledMessageException(Reason, message.ToPrintableString(), true);
				}
				for (var i = 0; i < _versionValue.Length; i++)
				{
					if (_sessionVersionBytes[i] != _versionValue.Buffer[i + _versionValue.Offset])
					{
						Disconnect();
						throw new GarbledMessageException(Reason, message.ToPrintableString(), true);
					}
				}
			}
			else
			{
				Disconnect();
				throw new GarbledMessageException(Reason, message.ToPrintableString(), true);
			}
			CallNextHandler(message);
		}

		private void Disconnect()
		{
			Session.Disconnect(DisconnectReason.InvalidMessage, Reason);
		}
	}
}
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
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Storage.Timestamp;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Post
{
	/// <summary>
	/// Handler appends message to incoming storage.
	/// </summary>
	internal class AppendIncomingMessageHandler : AbstractGlobalPostProcessSessionMessageHandler
	{
		private const bool DefaultTsInLogs = true;
		private const bool DefaultTsInMsg = false;

		private bool _timestampInLogs;
		private bool _recTimeInLogs;
		private byte[] _tsBuffer;
		private IStorageTimestamp _storageTimestamp;

		/// <inheritdoc />
		public override bool HandleMessage(MsgBuf message)
		{
			try
			{
				if (_timestampInLogs)
				{
					if (_recTimeInLogs)
					{
						AddWithReceivingTimestamp(message, message.MessageReadTimeInTicks);
					}
					else
					{
						Session.IncomingStorage.AppendMessage(message.Buffer, message.Offset, message.Length);
					}
				}
				try
				{
					Session.ExtendedFixSessionListener.OnMessageReceived(message);
				}
				catch (Exception e)
				{
					if (Log.IsDebugEnabled)
					{
						Log.Warn("IExtendedFIXSessionListener::OnMessageReceived thrown exception. Cause: " + e.Message, e);
					}
					else
					{
						Log.Warn("IExtendedFIXSessionListener::OnMessageReceived thrown exception. Cause: " + e.Message);
					}
				}
				return true;
			}
			catch (Exception e)
			{
				throw new SystemException(e?.Message, e);
			}
		}

		/// <inheritdoc />
		public override IExtendedFixSession Session
		{
			set
			{
				base.Session = value;
				InitFromSession(value);
			}
		}

		private void InitFromSession(IExtendedFixSession fixSession)
		{
			var configuration = fixSession.Parameters.Configuration;
			_timestampInLogs = configuration.GetPropertyAsBoolean(Config.TimestampsInLogs, DefaultTsInLogs);
			_recTimeInLogs = configuration.GetPropertyAsBoolean(Config.MarkIncomingMessageTime, DefaultTsInMsg);
			if (_recTimeInLogs)
			{
				_storageTimestamp = new StorageTimestampNano();
				_tsBuffer = new byte[_storageTimestamp.GetFormatLength()];
			}
		}

		private void AddWithReceivingTimestamp(MsgBuf message, long ticks)
		{
			_storageTimestamp.Format(ticks, _tsBuffer);
			Session.IncomingStorage.AppendMessage(_tsBuffer, message.Buffer, message.Offset, message.Length);
		}
	}
}
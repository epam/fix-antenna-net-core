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
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Post;
using Epam.FixAntenna.NetCore.FixEngine.Storage;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Post.Helper
{
	internal class AppendIncomingMessageHandlerChainHelper : AbstractGlobalMessageHandler
	{
		private AppendIncomingMessageHandler _messageHandler;
		private TestFixSession _session;
		private FixMessage _message;
		private byte[] _lastLoggedMsgTimestamp;

		public AppendIncomingMessageHandlerChainHelper()
		{
			_session = new FixSessionWithTimestampsInLog(this);
			_session.Parameters.Configuration.SetProperty(Config.MarkIncomingMessageTime, true.ToString());
			_messageHandler = new AppendIncomingMessageHandler();
			_messageHandler.Session = _session;
			_messageHandler.NextHandler = this;
			_session.SessionState = SessionState.Connected;
		}

		public virtual void ProcessMessage(MsgBuf msgBuf)
		{
			_messageHandler.OnPostProcessMessage(msgBuf);
		}

		public override void OnNewMessage(FixMessage message)
		{
			_message = message;
		}

		public virtual FixMessage GetMessage()
		{
			return _message;
		}

		public virtual void FreeResources()
		{
			_session.Dispose();
		}

		public virtual byte[] GetLastLoggedMsgTimestamp()
		{
			return _lastLoggedMsgTimestamp;
		}

		private class IncomingMessageStorageWithTimestamps : IMessageStorage
		{
			private readonly AppendIncomingMessageHandlerChainHelper _chainHelper;

			public IncomingMessageStorageWithTimestamps(AppendIncomingMessageHandlerChainHelper chainHelper)
			{
				_chainHelper = chainHelper;
			}

			public virtual long Initialize()
			{
				return 0;
			}

			public virtual void AppendMessage(byte[] message, int offset, int length)
			{

			}

			public virtual void AppendMessage(byte[] message)
			{

			}

			public virtual void AppendMessage(byte[] timestampFormatted, byte[] message, int offset, int length)
			{
				_chainHelper._lastLoggedMsgTimestamp = timestampFormatted;
			}

			public virtual void AppendMessage(byte[] timestampFormatted, byte[] message)
			{
				_chainHelper._lastLoggedMsgTimestamp = timestampFormatted;
			}

			public virtual byte[] RetrieveMessage(long seqNumber)
			{
				return Array.Empty<byte>();
			}

			public virtual void RetrieveMessages(long fromSeqNum, long toSeqNun, IMessageStorageListener listener, bool blocking)
			{

			}

			public virtual void Close()
			{

			}

			public virtual void BackupStorage(SessionParameters sessionParameters)
			{

			}

			public void Dispose()
			{
			}
		}

		private class FixSessionWithTimestampsInLog : TestFixSession
		{
			private readonly AppendIncomingMessageHandlerChainHelper _outerInstance;

			public FixSessionWithTimestampsInLog(AppendIncomingMessageHandlerChainHelper outerInstance)
			{
				_outerInstance = outerInstance;
			}

			public override IMessageStorage IncomingStorage => new IncomingMessageStorageWithTimestamps(_outerInstance);
		}
	}
}
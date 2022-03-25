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
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.TestUtils.Transport;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Session.Impl;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.ResetLogon.Util
{
	internal abstract class FixSessionEmulator
	{
		protected internal SessionParameters SessionParameters;
		protected internal FixSessionRuntimeState RuntimeState;
		protected internal BasicSocketTransport Transport;
		protected internal StandardMessageFactory MessageFactory;
		protected internal string TestRequestId;
		protected internal FixMessage LastReceivedMessage;
		protected internal SerializationContext SerializationContext;


		public FixSessionEmulator(SessionParameters sessionParameters, BasicSocketTransport transport, StandardMessageFactory messageFactory)
		{
			SessionParameters = sessionParameters;
			RuntimeState = new FixSessionRuntimeState();
			Transport = transport ?? throw new ArgumentNullException(nameof(transport));
			Transport.Init(sessionParameters.Port.Value, TimeSpan.FromMilliseconds(sessionParameters.HeartbeatInterval * 1200));
			MessageFactory = messageFactory ?? throw new ArgumentNullException(nameof(transport));
			MessageFactory.SetSessionParameters(sessionParameters);
			//FIXME_NOW: we don't need such hack with setting sequences
			RuntimeState.InSeqNum = 1;
			RuntimeState.OutSeqNum = 1;
			MessageFactory.SetRuntimeState(RuntimeState);
			SerializationContext = new SerializationContext(MessageFactory);
		}

		public virtual FixMessage GetLastReceivedMessage()
		{
			return LastReceivedMessage;
		}

		public virtual void Open()
		{
			Transport.Open();
		}

		public virtual void Close()
		{
			Transport.Close();
		}

		public virtual void SendLogon()
		{
			var logon = MessageFactory.CompleteMessage("A", new FixMessage());
			var byteBuffer = SerializeMessage("", logon);
			SendMessage(byteBuffer);
		}

		public virtual void SendLogout(string reason)
		{
			var logout = new FixMessage();
			logout.AddTag(Tags.Text, reason);
			var byteBuffer = SerializeMessage("5", logout);
			SendMessage(byteBuffer);
		}

		public virtual void ReceiveLogon()
		{
			ValidateAndParseMsg("A");
		}

		public virtual void ReceiveResetLogon()
		{
			ValidateAndParseMsg("A");
		}

		private void ValidateAndParseMsg(string msgType)
		{
			var message = ReceiveMessage();
			ValidateMsgType(message, msgType);
			LastReceivedMessage = RawFixUtil.GetFixMessage(message.AsByteArray());
		}

		private string ReceiveMessage()
		{
			RuntimeState.InSeqNum++;
			return Transport.ReceiveMessage();
		}

		private void ValidateMsgType(string message, string msgType)
		{
			if (string.IsNullOrEmpty(message))
			{
				throw new InvalidOperationException("invalid logon received");
			}

			LastReceivedMessage = RawFixUtil.GetFixMessage(message.AsByteArray());
			ValidateMsgType(LastReceivedMessage, msgType);
		}

		private void ValidateMsgType(FixMessage fixMessage, string msgType)
		{
			if (!msgType.Equals(fixMessage.GetTagValueAsString(35)))
			{
				throw new InvalidOperationException("expected logon but received " + fixMessage);
			}
		}

		public virtual void ReceiveTestRequest()
		{
			var message = ReceiveMessage();
			ValidateMsgType(message, "1");
			TestRequestId = LastReceivedMessage.GetTagValueAsString(112);
		}

		public virtual void SendResponseToTestRequest()
		{
			var list = new FixMessage();
			if (!string.IsNullOrEmpty(TestRequestId))
			{
				list.AddTag(112, TestRequestId);
				TestRequestId = null;
			}
			var byteBuffer = SerializeMessage("0", list);
			SendMessage(byteBuffer);
		}

		private void SendMessage(string byteBuffer)
		{
			Transport.SendMessage(byteBuffer);
			RuntimeState.IncrementOutSeqNum();
		}

		public virtual void ForcedResetSequences()
		{
			RuntimeState.InSeqNum = 1;
			RuntimeState.OutSeqNum = 1;
		}

		private string SerializeMessage(FixMessage fixMessage)
		{
			var byteBuffer = new ByteBuffer();
			MessageFactory.Serialize(fixMessage.GetTagValueAsString(35), fixMessage, byteBuffer, SerializationContext);
			return StringHelper.NewString(byteBuffer.GetByteArray(), 0, byteBuffer.Offset);
		}

		private string SerializeMessage(string msgType, FixMessage content)
		{
			var byteBuffer = new ByteBuffer();
			MessageFactory.Serialize(msgType, content, byteBuffer, SerializationContext);
			return StringHelper.NewString(byteBuffer.GetByteArray(), 0, byteBuffer.Offset);
		}

		public virtual void SendResetLogon()
		{
			var list = MessageFactory.CompleteMessage("A", new FixMessage());
			list.Set(141, "Y".AsByteArray());
			RuntimeState.OutSeqNum = 1;
			RuntimeState.InSeqNum = 1;
			var byteBuffer = SerializeMessage("", list);
			SendMessage(byteBuffer);
		}

		public virtual void ReceiveAnyMessage()
		{
			var message = ReceiveMessage();
			LastReceivedMessage = RawFixUtil.GetFixMessage(message.AsByteArray());
		}

		public virtual void SendNewsMessage()
		{
			SendMessage(SerializeMessage("B", CreateSampleMessage()));
		}

		private static readonly byte[] NewsMsgHeadline = "Hello there:".AsByteArray();
		private static readonly byte[] NewsMsgText = "Line1".AsByteArray();

		public virtual FixMessage CreateSampleMessage()
		{
			// create FIX 4.2 News
			var messageContent = new FixMessage();

			messageContent.AddTag(148, NewsMsgHeadline); // Add Subject
			messageContent.AddTag(33, (long)1); // Add Repeating group
			messageContent.AddTag(58, NewsMsgText);

			return messageContent;
		}
	}
}
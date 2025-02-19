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

using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global.Helper;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Post.Helper;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Post
{
	[TestFixture]
	internal class AppendIncomingMessageHandlerTest
	{
		private AppendIncomingMessageHandlerChainHelper _messageHandler;

		[SetUp]
		public virtual void SetUp()
		{
			_messageHandler = new AppendIncomingMessageHandlerChainHelper();
		}

		[TearDown]
		public virtual void TearDown()
		{
			_messageHandler.FreeResources();
		}

		[Test]
		public virtual void TestReceivingTimeInIncomingLog()
		{
			var msgBuf = new MsgBuf
			{
				MessageReadTimeInTicks = DateTimeHelper.CurrentTicks,
				FixMessage = MessageHelper.GetHbMessage()
			};

			ClassicAssert.IsNull(_messageHandler.GetLastLoggedMsgTimestamp());
			_messageHandler.ProcessMessage(msgBuf);
			ClassicAssert.IsNotNull(_messageHandler.GetLastLoggedMsgTimestamp());
		}
	}
}
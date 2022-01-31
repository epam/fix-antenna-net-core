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

using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.PerType;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.PerType
{
	[TestFixture]
	internal class ResendRequestMessageHandlerTest
	{
		private FixMessage _message;
		private ResendRequestMessageHandler _resendRequestMessageHandler;
		private TestFixSession _session;

		[SetUp]
		public virtual void SetUp()
		{
			_message = RawFixUtil.GetFixMessage("8=FIX.4.4\u00019=109\u000135=2\u000149=TRGT\u000156=SNDR\u000134=2\u000152=20100723-11:03:44.995\u00017=2000\u000116=0\u000110=190\u0001".AsByteArray());
			_resendRequestMessageHandler = new ResendRequestMessageHandler();
			_resendRequestMessageHandler.Session = _session = new TestFixSession();
			_session.RuntimeState.OutSeqNum = 5000;
		}

		[Test]
		public virtual void RequestMoreMessages()
		{
			_resendRequestMessageHandler.OnNewMessage(_message);

			Assert.IsTrue(_session.Messages.Count > 0);
			var message = _session.Messages[0];
			Assert.AreEqual(4, message.GetTagAsInt(35));
		}

		[Test]
		public virtual void RequestOverMessageLimitMessages()
		{
			_session.Parameters.Configuration.SetProperty(Config.ResendRequestNumberOfMessagesLimit, "1000");

			_resendRequestMessageHandler.Session = _session;
			_resendRequestMessageHandler.OnNewMessage(_message);

			Assert.IsTrue(_session.Messages.Count > 0);
			var message = _session.Messages[0];
			// the first message should be seq reset
			Assert.AreEqual(4, message.GetTagAsInt(35)); // seq reset
			Assert.AreEqual(4000, message.GetTagAsInt(36));
		}
	}
}
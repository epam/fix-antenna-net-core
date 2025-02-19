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
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.PerType.Util;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.PerType.Util
{
	[TestFixture]
	internal class MessageRrCarrierSenderTest
	{

		private FixMessage _message;

		private RrMessageCarrierSender _messageRrCarrierSender;

		[SetUp]
		public virtual void SetUp()
		{
			_message = RawFixUtil.GetFixMessage("8=FIX.4.4\u00019=109\u000135=6\u000149=TRGT\u000156=SNDR\u000134=2\u000152=20100723-11:03:44.995\u0001369=2\u0001122=20100101-14:12:53\u000123=24\u000128=N\u000155=TESTB\u000154=1\u000127=S\u000110=190\u0001".AsByteArray());
			_messageRrCarrierSender = new RrMessageCarrierSender(new TestFixSession());
		}

		[Test]
		public virtual void CheckPrepareForResendRequest()
		{
			var preparedMessage = _messageRrCarrierSender.PrepareForResendRequest(_message);

			ClassicAssert.IsTrue(preparedMessage.GetTag(Tags.OrigSendingTime) != null);
			ClassicAssert.IsTrue(preparedMessage.GetTag(Tags.PossDupFlag) != null);
			ClassicAssert.AreEqual(preparedMessage.CalculateBodyLength(), preparedMessage.GetTagAsInt(Tags.BodyLength));
		}
	}
}
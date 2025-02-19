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
using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global;
using Epam.FixAntenna.NetCore.Message;
using Moq;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global
{
	[TestFixture]
	internal class OutOfSequenceMessageHandlerMockTest
	{
		private FixMessage _message;

		private OutOfSequenceMessageHandler _handler;
		private Mock<ISessionMessageHandler> _nextHandler;
		private Mock<AbstractFixSessionForTests> _session;

		[SetUp]
		public virtual void SetUp()
		{
			_nextHandler = new Mock<ISessionMessageHandler>();
			_session = new Mock<AbstractFixSessionForTests>();

			_message = new FixMessage();
			_message.AddTag(8, "FIX.4.4");
			_message.AddTag(35, "A");
			_message.AddTag(34, "1");
			_message.AddTag(10, "20");
		}

		[Test]
		public virtual void ToLowMessageSequenceDisconnectWithQueueClean()
		{
			var conf = (Config)Config.GlobalConfiguration.Clone();

			var sessionParameters = new SessionParameters(conf);
			_session.Setup(x => x.Parameters).Returns(sessionParameters);
			_session.Setup(x => x.SequenceManager.GetExpectedIncomingSeqNumber()).Returns(20);

			ClassicAssert.Throws<SequenceToLowException>(() =>
				{
					//init handler
					_handler = new OutOfSequenceMessageHandler();
					_handler.NextHandler = _nextHandler.Object;
					_handler.Session = _session.Object;
					_handler.OnNewMessage(_message);
				});

			_session.Verify(x => x.ForcedDisconnect(
					It.Is<DisconnectReason>(y => y == DisconnectReason.GotSequenceTooLow),
					It.Is<string>(y => y.Equals("Incoming seq number 1 is less then expected 20", StringComparison.InvariantCulture)),
					false),
				Times.Once);
		}

		[Test]
		public virtual void ToLowMessageSequenceDisconnectWithoutQueueClean()
		{
			var conf = (Config)Config.GlobalConfiguration.Clone();
			conf.SetProperty(Config.ResetQueueOnLowSequenceNum, "false");
			var sessionParameters = new SessionParameters(conf);
			_session.Setup(x => x.Parameters).Returns(sessionParameters);
			_session.Setup(x => x.SequenceManager.GetExpectedIncomingSeqNumber()).Returns(20);

			ClassicAssert.Throws<SequenceToLowException>(() =>
			{
				//init handler
				_handler = new OutOfSequenceMessageHandler();
				_handler.NextHandler = _nextHandler.Object;
				_handler.Session = _session.Object;
				_handler.OnNewMessage(_message);
			});

			_session.Verify(x => x.ForcedDisconnect(
					It.Is<DisconnectReason>(y => y == DisconnectReason.GotSequenceTooLow),
					It.Is<string>(y => y.Equals("Incoming seq number 1 is less then expected 20", StringComparison.InvariantCulture)),
					false),
				Times.Once);

			//check that queue isn't cleaned due to configuration
			_session.Verify(x => x.ClearQueue(), Times.Never);
		}

		// ReSharper disable once MemberCanBePrivate.Global: needed for moq
		internal abstract class AbstractFixSessionForTests : AbstractFixSession
		{

		}
	}
}
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

using Epam.FixAntenna.TestUtils;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	[TestFixture]
	internal class AcceptorSessionCreateTest : AbstractAcceptorSessionTest
	{
		[Test]
		public virtual void TestConnect()
		{
			var config = Config.GlobalConfiguration;
			var sessionParameters = new SessionParameters(config);
			sessionParameters.SenderCompId = "sender";
			sessionParameters.TargetCompId = "target";
			sessionParameters.Port = Port;
			var acceptorSession = (AbstractFixSession) sessionParameters.CreateAcceptorSession();

			var parameters = new SessionParameters();
			parameters.SenderCompId = "target";
			parameters.TargetCompId = "sender";
			parameters.Port = Port;
			parameters.Host = "localhost";
			var initiatorSession = StandardFixSessionFactory.GetFactory(parameters).CreateInitiatorSession(parameters);
			initiatorSession.Connect();

			CheckingUtils.CheckWithinTimeout(() =>
				initiatorSession.SessionState.Equals(SessionState.Connected), TimeSpan.FromMilliseconds(20000));
			acceptorSession.Disconnect("test");
			CheckingUtils.CheckWithinTimeout(() =>
				initiatorSession.SessionState.Equals(SessionState.Disconnected), TimeSpan.FromMilliseconds(2000));
			acceptorSession.Dispose();
			initiatorSession.Dispose();
		}

		[Test]
		public async Task TestConnectAsync()
		{
			var config = Config.GlobalConfiguration;
			var sessionParameters = new SessionParameters(config);
			sessionParameters.SenderCompId = "sender";
			sessionParameters.TargetCompId = "target";
			sessionParameters.Port = Port;
			var acceptorSession = (AbstractFixSession)sessionParameters.CreateAcceptorSession();

			var parameters = new SessionParameters();
			parameters.SenderCompId = "target";
			parameters.TargetCompId = "sender";
			parameters.Port = Port;
			parameters.Host = "localhost";
			var initiatorSession = StandardFixSessionFactory.GetFactory(parameters).CreateInitiatorSession(parameters);
			await initiatorSession.ConnectAsync().ConfigureAwait(false);

			CheckingUtils.CheckWithinTimeout(() =>
				initiatorSession.SessionState.Equals(SessionState.Connected), TimeSpan.FromMilliseconds(20000));
			await acceptorSession.DisconnectAsync("test").ConfigureAwait(false);
			CheckingUtils.CheckWithinTimeout(() =>
				initiatorSession.SessionState.Equals(SessionState.Disconnected), TimeSpan.FromMilliseconds(2000));
			acceptorSession.Dispose();
			initiatorSession.Dispose();
		}

		[Test]
		public virtual void TestWithQueuedMessage()
		{
			var listener = new Mock<IFixSessionListener>(MockBehavior.Strict);

			var config = Config.GlobalConfiguration;
			var sessionParameters = new SessionParameters(config);
			sessionParameters.SenderCompId = "sender";
			sessionParameters.TargetCompId = "target";
			sessionParameters.Port = Port;
			var acceptorSession = (AbstractFixSession) sessionParameters.CreateAcceptorSession();
			//sending message in disconnected newly created acceptor
			var messageGenerator = new MessageGenerator(sessionParameters.SenderCompId, sessionParameters.TargetCompId);
			var newsMessage = messageGenerator.GetNewsMessage(2);
			acceptorSession.SendMessage(newsMessage);
			//setting expectations for initiator sessions
			listener.Setup(x => x.OnNewMessage(It.Is<FixMessage>(
				y => y.GetTagValueAsString(35).Equals(newsMessage.GetTagValueAsString(35), StringComparison.InvariantCulture))));

			var parameters = new SessionParameters();
			parameters.SenderCompId = "target";
			parameters.TargetCompId = "sender";
			parameters.Port = Port;
			parameters.Host = "localhost";
			var initiatorSession = StandardFixSessionFactory.GetFactory(parameters).CreateInitiatorSession(parameters);
			//settings mocked listener with expectations
			initiatorSession.SetFixSessionListener(listener.Object);
			initiatorSession.Connect();

			CheckingUtils.CheckWithinTimeout(() =>
				initiatorSession.SessionState.Equals(SessionState.Connected), TimeSpan.FromMilliseconds(2000));
			CheckingUtils.CheckWithinTimeout(() => initiatorSession.InSeqNum == 3, TimeSpan.FromSeconds(5));
			acceptorSession.Disconnect("test");
			CheckingUtils.CheckWithinTimeout(() =>
				initiatorSession.SessionState.Equals(SessionState.Disconnected), TimeSpan.FromMilliseconds(2000));
			listener.Verify(x => x.OnNewMessage(It.IsAny<FixMessage>()), Times.Once);

			acceptorSession.Dispose();
			initiatorSession.Dispose();
		}

		public override bool ClearLogs()
		{
			return new LogsCleaner().Clean("./logs") && (new LogsCleaner()).Clean("./logs/backup");
		}
	}
}
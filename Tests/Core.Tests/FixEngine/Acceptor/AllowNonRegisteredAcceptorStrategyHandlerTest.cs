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
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Acceptor;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.FixEngine.Transport;
using Moq;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Acceptor
{
	internal class AllowNonRegisteredAcceptorStrategyHandlerTest
	{
		private readonly Config _conf = Config.GlobalConfiguration;

		private AllowNonRegisteredAcceptorStrategyHandler _acceptorHandler;

		private Mock<IConfiguredSessionRegister> _sessionRegister;
		private Mock<IFixServerListener> _sessionListener;

		[SetUp]
		public void SetUp()
		{
			_acceptorHandler = new AllowNonRegisteredAcceptorStrategyHandler();
			_sessionRegister = new Mock<IConfiguredSessionRegister>();
			_sessionListener = new Mock<IFixServerListener>();
			_acceptorHandler.Init(_conf, _sessionRegister.Object);
			_acceptorHandler.SetSessionListener(_sessionListener.Object);
		}

		[Test]
		public virtual void TestRegisteredSession()
		{
			var fixTransport = new Mock<IFixTransport>();
			var regParams = new SessionParameters();
			regParams.SetSessionId("registered");

			var inParams = new ParsedSessionParameters();
			_sessionRegister.Setup(x => x.IsSessionRegistered(inParams.SessionId)).Returns(true);
			_sessionRegister.Setup(x => x.GetSessionParams(inParams.SessionId)).Returns(regParams);

			_acceptorHandler.HandleIncomingConnection(inParams, fixTransport.Object);

			_sessionListener.Verify(x => x.NewFixSession(It.IsAny<IFixSession>()), Times.Once);
		}
	}
}
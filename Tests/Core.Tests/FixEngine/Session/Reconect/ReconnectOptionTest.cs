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
using Epam.FixAntenna.TestUtils;
using NUnit.Framework;
using System.Threading;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Message;
using System.Collections.Generic;
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Configuration;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.Reconect
{
    [TestFixture]
    internal class ReconnectOptionTest
    {
        private IFixSession _initiatorSession;
        private IFixSession _acceptorSession;
        private const int Port = 3000;
        private FixServer _server;
        private CountdownEvent _disconnectedAbnormally;
        private CountdownEvent _newSessionConnected;

        [SetUp]
        public void Before()
        {
            LogsCleaner.ClearDefaultLogs();
            _disconnectedAbnormally = new CountdownEvent(1);
            _newSessionConnected = new CountdownEvent(1);
        }

        [Test]
        public virtual void TestAutoreconnectSessionWhenForceSeqNumResetIsOneTime()
        {
            var props = new Dictionary<string, string>();
            props.Add("autoreconnectAttempts", "0");
            props.Add("autoreconnectDelayInMs", "100");
            props.Add("forceSeqNumReset", "OneTime");
            InitClient(new Config(props));

            _initiatorSession.Connect();
            _disconnectedAbnormally.Wait(TimeSpan.FromSeconds(5));

            var aacceptorConfig = GetAcceptorConfiguration();
            InitServer(aacceptorConfig);

            _newSessionConnected.Wait(TimeSpan.FromSeconds(5));

            Assert.IsTrue(!string.IsNullOrEmpty(_acceptorSession.Parameters.IncomingLoginMessage.GetTagValueAsString(Tags.ResetSeqNumFlag)));

            _newSessionConnected.Reset();
            _disconnectedAbnormally.Reset();

            _initiatorSession.Disconnect("tests");
            _disconnectedAbnormally.Wait(TimeSpan.FromSeconds(5));
            _initiatorSession.Connect();
            _newSessionConnected.Wait(TimeSpan.FromSeconds(5));

            Assert.IsTrue(string.IsNullOrEmpty(_acceptorSession.Parameters.IncomingLoginMessage.GetTagValueAsString(Tags.ResetSeqNumFlag)));
        }

        private void InitServer(Config config)
        {
            _server = new FixServer(config);
            _server.SetPort(Port);
            _server.SetListener(new FixServerListener(this));
            _server.Start();
        }

        private void InitClient(Config config)
        {
            var parameters = new SessionParameters
            {
                FixVersion = FixVersion.Fix44,
                Host = "localhost",
                Port = Port,
                SenderCompId = "initiator",
                TargetCompId = "acceptor"  ,
                Configuration = config
            };

            _initiatorSession = parameters.CreateInitiatorSession();
            _initiatorSession.SetFixSessionListener(new InitiatorFixSessionListener(this));
        }

        private Config GetAcceptorConfiguration()
        {
            var props = new Dictionary<string, string>();
            props.Add("host", "localhost");
            props.Add("senderCompID", "acceptor");
            props.Add("targetCompID", "initiator");
            props.Add("sessionType", "acceptor");
            props.Add("fixVersion", "FIX.4.4");

            return new Config(props);
        }

        private class FixServerListener : IFixServerListener
        {
            private readonly ReconnectOptionTest _outerScope;

            public FixServerListener(ReconnectOptionTest outerScope)
            {
                _outerScope = outerScope;
            }

            public void NewFixSession(IFixSession session)
            {
                _outerScope._acceptorSession = (AcceptorFixSession)session;
                session.Connect();
                _outerScope._newSessionConnected.Signal();
            }
        }

        private class InitiatorFixSessionListener : IFixSessionListener
        {
            private readonly ReconnectOptionTest _outerInstance;

            public InitiatorFixSessionListener(ReconnectOptionTest outerInstance)
            {
                _outerInstance = outerInstance;
            }

            public void OnNewMessage(FixMessage message)
            {
            }

            public void OnSessionStateChange(SessionState sessionState)
            {
                if (sessionState.Equals(SessionState.DisconnectedAbnormally))
                {
                    _outerInstance._disconnectedAbnormally.Signal();
                }
            }
        }
    }
}
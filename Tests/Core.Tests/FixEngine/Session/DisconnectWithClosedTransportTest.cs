// Copyright (c) 2026 EPAM Systems
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

using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.FixEngine.Session.IoThreads;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler;
using Epam.FixAntenna.NetCore.Message;

using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	[TestFixture]
	internal class DisconnectWithClosedTransportTest
	{
		[Test]
		public void DisconnectDoesNotThrowWhenLogoutCannotBeSent()
		{
			var session = new LogoutSendFailureSession();

			// the logout message cannot be delivered (simulates the transport being closed
			// by the reader thread after a broken connection); the failure must not
			// propagate to the Disconnect caller
			Assert.DoesNotThrow(() => session.Disconnect("test disconnect"));
			Assert.That(session.SessionState, Is.EqualTo(SessionState.WaitingForLogoff));
		}

		private class LogoutSendFailureSession : InitiatorFixSession
		{
			private SessionState _sessionState = SessionState.Connected;

			public LogoutSendFailureSession() : base(null, CreateParameters(),
#pragma warning disable CA2000 // Dispose objects before losing scope: disposed in AbstractFIXSession
				new HandlerChain())
#pragma warning restore CA2000 // Dispose objects before losing scope
			{
				// normally reset when the session is started; required for Disconnect to send the logout
				AlreadySendingLogout = false;
			}

			private static SessionParameters CreateParameters()
			{
				return new SessionParameters
				{
					FixVersion = FixVersion.Fix44,
					TargetCompId = "T",
					SenderCompId = "S",
					HeartbeatInterval = 30
				};
			}

			public override SessionState SessionState
			{
				get => _sessionState;
				set => _sessionState = value;
			}

			public override bool SendMessageOutOfTurn(string type, FixMessage list)
			{
				throw new MessageNotSentException("Message wasn't sent due to error");
			}
		}
	}
}

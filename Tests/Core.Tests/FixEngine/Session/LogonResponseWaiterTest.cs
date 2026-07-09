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

using System;
using System.Threading;

using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Session.IoThreads;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler;

using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	[TestFixture]
	internal class LogonResponseWaiterTest
	{
		[Test]
		public void ExceptionFromDisconnectOnLogonTimeoutIsDeliveredToErrorHandlerInsteadOfEscapingThread()
		{
			var session = new ThrowingDisconnectInitiatorSession();

			// entering WaitingForLogon starts the LogonResponseWaiter thread;
			// on timeout it disconnects the session and the disconnect throws
			// (simulates the transport being closed by the reader thread just before the timeout)
			session.SessionState = SessionState.WaitingForLogon;

			// before the fix the MessageNotSentException escaped the waiter thread
			// as an unhandled exception and terminated the test process
			Assert.That(session.RecordingHandler.WaitForError(TimeSpan.FromSeconds(10)), Is.True,
				"Exception thrown from Disconnect was not delivered to the session error handler");
			Assert.That(session.RecordingHandler.Error, Is.InstanceOf<MessageNotSentException>());
		}

		private class ThrowingDisconnectInitiatorSession : InitiatorFixSession
		{
			public RecordingErrorHandler RecordingHandler { get; } = new RecordingErrorHandler();

			public ThrowingDisconnectInitiatorSession() : base(null, CreateParameters(),
#pragma warning disable CA2000 // Dispose objects before losing scope: disposed in AbstractFIXSession
				new HandlerChain())
#pragma warning restore CA2000 // Dispose objects before losing scope
			{
			}

			private static SessionParameters CreateParameters()
			{
				var parameters = new SessionParameters
				{
					FixVersion = FixVersion.Fix44,
					TargetCompId = "T",
					SenderCompId = "S",
					HeartbeatInterval = 30
				};
				parameters.Configuration.SetProperty(Config.LoginWaitTimeout, "50");
				return parameters;
			}

			public override IErrorHandler ErrorHandler
			{
				get => RecordingHandler;
				set { }
			}

			public override void Disconnect(DisconnectReason type, string reason, bool isGracefull, bool isForced, bool continueReading)
			{
				throw new MessageNotSentException("Message wasn't sent due to error");
			}
		}

		private class RecordingErrorHandler : IErrorHandler
		{
			private readonly ManualResetEventSlim _errorReceived = new ManualResetEventSlim(false);

			public Exception Error { get; private set; }

			public bool WaitForError(TimeSpan timeout)
			{
				return _errorReceived.Wait(timeout);
			}

			public void OnWarn(string description, Exception throwable)
			{
			}

			public void OnError(string description, Exception throwable)
			{
				Error = throwable;
				_errorReceived.Set();
			}

			public void OnFatalError(string description, Exception throwable)
			{
				Error = throwable;
				_errorReceived.Set();
			}
		}
	}
}

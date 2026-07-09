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

using Epam.FixAntenna.NetCore.FixEngine.Session.IoThreads;

using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.Util
{
	[TestFixture]
	internal class SessionStatusCheckerThreadTest
	{
		[Test]
		public void ExceptionFromShutdownIsDeliveredToErrorHandlerInsteadOfEscapingThread()
		{
			var errorHandler = new RecordingErrorHandler();
			var session = new ThrowingShutdownFixSession(errorHandler)
			{
				SessionState = SessionState.WaitingForLogoff
			};

			var checker = new SessionStatusCheckerThread(session, 0, SessionState.WaitingForLogoff, "test reason");
			checker.Start();

			// before the fix the MessageNotSentException escaped the checker thread
			// as an unhandled exception and terminated the test process
			Assert.That(errorHandler.WaitForError(TimeSpan.FromSeconds(10)), Is.True,
				"Exception thrown from Shutdown was not delivered to the session error handler");
			Assert.That(errorHandler.Error, Is.InstanceOf<MessageNotSentException>());
		}

		private class ThrowingShutdownFixSession : TestFixSession
		{
			private readonly IErrorHandler _errorHandler;

			public ThrowingShutdownFixSession(IErrorHandler errorHandler)
			{
				_errorHandler = errorHandler;
			}

			public override IErrorHandler ErrorHandler
			{
				get => _errorHandler;
				set { }
			}

			public override void Shutdown(DisconnectReason reason, bool blocking)
			{
				throw new MessageNotSentException("Message wasn't sent due to error");
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

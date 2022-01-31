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
using System.Diagnostics;
using System.Threading;
using Epam.FixAntenna.NetCore.Common.Logging;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.Util
{
	/// <summary>
	/// Checks the session status after specific timeout, if the session state is
	/// the same disconnect the session with specific reason.
	/// </summary>
	internal class SessionStatusCheckerThread/* : WorkerThread*/
	{
		private const int Second = 1000;

		private static readonly ILog Log = LogFactory.GetLog(typeof(SessionStatusCheckerThread));

		private readonly IExtendedFixSession _session;
		private readonly int _timeout;
		private readonly SessionState _state;
		private readonly string _reason;
		private readonly bool _deliverError;
		private readonly Thread _thread; //TODO: implement logic
		private readonly Stopwatch _stopWatch;

		/// <summary>
		/// Creates the <c>SessionStatusCheckerThread</c>.
		/// </summary>
		/// <param name="session">            the monitored session </param>
		/// <param name="timeout">            the time to wait before close the session </param>
		/// <param name="state">              the initial session state </param>
		/// <param name="reason">             the closed reason if session will be closed </param>
		/// <param name="notifyErrorHandler"> if sets to true the session error handler
		///                           will be notify with error level and reason message. </param>
		public SessionStatusCheckerThread(IExtendedFixSession session, int timeout, SessionState state, string reason, bool notifyErrorHandler = true)
		{
			_session = session;
			_timeout = timeout;
			_state = state;
			_stopWatch = new Stopwatch();
			_reason = reason;
			_deliverError = notifyErrorHandler;
			_thread = new Thread(Run) { Name = "SessionStatusChecker" };
		}

		public void Start()
		{
			_stopWatch.Start();
			_thread.Start();
		}

		private void Run()
		{
			if (Log.IsTraceEnabled)
			{
				Log.Trace("Start checker thread for session: " + _session + ". wait for state " + _state + " during " + _timeout);
			}

			while (_stopWatch.ElapsedMilliseconds / Second <= _timeout && _session.SessionState == _state)
			{
				lock (this)
				{
					try
					{
						Monitor.Wait(this, TimeSpan.FromMilliseconds(Second));
					}
					catch (ThreadInterruptedException)
					{
						// intentionally blank
					}
				}
			}

			_stopWatch.Stop();

			if (_session.SessionState == _state)
			{
				if (Log.IsInfoEnabled)
				{
					Log.Info("Session state is still " + _state + ". Session will be disconnected");
				}

				if (_session.SessionState == SessionState.Connected)
				{
					_session.Disconnect(null, _reason);
				}
				else
				{
					//session.markShutdownAsGraceful();
					_session.Shutdown(null, false);
				}

				if (_deliverError)
				{
					_session.ErrorHandler.OnWarn(_reason, new InvalidOperationException(_reason));
				}
			}

			if (Log.IsTraceEnabled)
			{
				Log.Trace("Stop checker thread for session: " + _session);
			}
		}
	}
}
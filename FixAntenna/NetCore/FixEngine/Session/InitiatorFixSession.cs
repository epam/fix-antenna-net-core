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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.FixEngine.Scheduler;
using Epam.FixAntenna.NetCore.FixEngine.Scheduler.Tasks;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler;
using Epam.FixAntenna.NetCore.FixEngine.Transport;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	internal class InitiatorFixSession : AbstractFixSession, IScheduledFixSession
	{
		private ISessionTransportFactory TransportFactory { get; }

		/// <summary>
		/// Creates the <c>InitiatorFIXSession</c>.
		/// </summary>
		public InitiatorFixSession(IFixMessageFactory factory, SessionParameters sessionParameters, HandlerChain fixSessionListener)
			: base(factory, sessionParameters, fixSessionListener)
		{
			TransportFactory = new DefaultSessionTransportFactory();
		}

		/// <summary>
		/// Gets transport.
		/// </summary>
		/// <param name="host">              the host </param>
		/// <param name="port">              the port </param>
		/// <param name="sessionParameters"> the session parameters </param>
		protected virtual IFixTransport GetTransport(string host, int port, SessionParameters sessionParameters)
		{
			if (Log.IsDebugEnabled)
			{
				Log.Debug($"Getting transport for: {{host - {host}, port - {port}}}");
			}

			return TransportFactory.CreateInitiatorTransport(host, port, sessionParameters);
		}

		/// <summary>
		/// Returns true, if session is in active state.
		/// </summary>
		protected bool IsActive { get; private set; }

		/// <inheritdoc />
		public override void Init()
		{
			lock (SessionLock)
			{
				InitTransport();
				base.Init();
			}
		}

		/// <inheritdoc />
		public override void Connect()
		{
			CheckForDisposed();

			lock (SessionLock)
			{
				CheckForActiveAndInitTransport();
				ConnectInternal();
			}
		}

		public override async Task ConnectAsync()
		{
			CheckForDisposed();
			CheckForActiveAndInitTransport();
			await ConnectInternalAsync().ConfigureAwait(false);
		}

		private void CheckForDisposed()
		{
			if (SessionState.IsDisposed(SessionState))
			{
				//re-register FIX session again
				FixSessionManager.Instance.RegisterFixSession(this);
				Init();
			}
		}

		private void CheckForActiveAndInitTransport()
		{
			var currentState = SessionState;
			if (!(SessionState.IsDisposed(currentState) || SessionState.IsDisconnected(currentState)))
			{
				throw new InvalidOperationException($"Session is alive. Current session state: {currentState}");
			}

			InitTransport();
			SessionState = SessionState.Connecting;
		}

		/// <inheritdoc />
		public override SessionState SessionState
		{
			set
			{
				base.SessionState = value;
				if (value == SessionState.WaitingForLogon)
				{
					StartLogonResponseWaiter();
				}
				else if (value == SessionState.Connected)
				{
					CancelLogonResponseWaiter();
				}
			}
		}

		private LogonResponseWaiter _logonResponseWaiter;

		private void StartLogonResponseWaiter()
		{
			CancelLogonResponseWaiter();
			var logonWaitTimeout = ConfigAdapter.LogonWaitTimeout;
			_logonResponseWaiter = new LogonResponseWaiter(this, (int)logonWaitTimeout);
			_logonResponseWaiter.Start();
		}

		private void CancelLogonResponseWaiter()
		{
			if (_logonResponseWaiter != null)
			{
				_logonResponseWaiter.Cancel();
				_logonResponseWaiter.Dispose();
				_logonResponseWaiter = null;
			}
		}


		/// <summary>
		/// Unsupported for InitiatorFIXSession. Always throws IllegalStateException.
		/// </summary>
		/// <exception cref="InvalidOperationException"> </exception>
		/// <seealso cref="IFixSession.Reject(string)"></seealso>
		public override void Reject(string reason)
		{
			throw new InvalidOperationException("Initiator session cannot be rejected. It should be disconnected instead of.");
		}

		protected virtual void ConnectInternal()
		{
			IsActive = true;
			try
			{
				((IOutgoingFixTransport)Transport).Open();
				SessionState = SessionState.WaitingForLogon;
				PrepareForConnect();
				StartSession();
			}
			catch (IOException e)
			{
				LastDisconnectReason = DisconnectReason.InitConnectionProblem;
				SessionState = SessionState.DisconnectedAbnormally;
				if (Log.IsDebugEnabled)
				{
					Log.Debug("Connect failed", e);
				}
				else
				{
					Log.Warn($"Connect failed: {e.Message}");
				}

				throw;
			}
			catch (Exception e)
			{
				LastDisconnectReason = DisconnectReason.InitConnectionProblem;
				SessionState = SessionState.DisconnectedAbnormally;
				if (Log.IsDebugEnabled)
				{
					Log.Debug("Session startup failed", e);
				}
				else
				{
					Log.Warn($"Session startup failed: {e.Message}");
				}

				throw new Exception("Session startup failed", e);
			}
		}

		protected virtual async Task ConnectInternalAsync()
		{
			IsActive = true;
			try
			{
				await ((IOutgoingFixTransport)Transport).OpenAsync().ConfigureAwait(false);
				SessionState = SessionState.WaitingForLogon;
				PrepareForConnect();
				StartSession();
			}
			catch (IOException e)
			{
				LastDisconnectReason = DisconnectReason.InitConnectionProblem;
				SessionState = SessionState.DisconnectedAbnormally;
				if (Log.IsDebugEnabled)
				{
					Log.Debug("Connect failed", e);
				}
				else
				{
					Log.Warn($"Connect failed: {e.Message}");
				}

				throw;
			}
			catch (Exception e)
			{
				LastDisconnectReason = DisconnectReason.InitConnectionProblem;
				SessionState = SessionState.DisconnectedAbnormally;
				if (Log.IsDebugEnabled)
				{
					Log.Debug("Session startup failed", e);
				}
				else
				{
					Log.Warn($"Session startup failed: {e.Message}");
				}

				throw new Exception("Session startup failed", e);
			}
		}

		public virtual void InitTransport()
		{
			if (Transport == null)
			{
				Transport = GetTransport(Parameters.Host, Parameters.Port.Value, Parameters);
			}
		}

		/// <inheritdoc />
		public override void Disconnect(string reason)
		{
			IsActive = false;
			base.Disconnect(reason);
		}

		public override void Dispose()
		{
			IsActive = false;
			base.Dispose();
		}

		private sealed class LogonResponseWaiter : IDisposable
		{
			private readonly InitiatorFixSession _session;
			private readonly int _timeout;
			private readonly CountdownEvent _countDownLatch;
			private readonly Thread _worker;
			private bool _canceled;

			public LogonResponseWaiter(InitiatorFixSession session, int logonWaitTimeout)
			{
				_session = session;
				_timeout = logonWaitTimeout;
				_countDownLatch = new CountdownEvent(1);
				_worker = new Thread(Run) { Name = nameof(LogonResponseWaiter) };
			}

			public void Start()
			{
				_worker.Start();
			}

			private void Run()
			{
				if (_session.Log.IsTraceEnabled)
				{
					_session.Log.Trace("Starting...");
				}
				try
				{
					_countDownLatch.Wait(_timeout);
					if (!_canceled && _session.SessionState == SessionState.WaitingForLogon)
					{
						//non gracefully disconnect session
						_session.Disconnect(DisconnectReason.NoAnswer, $"Logon response wasn't received during {_timeout} ms", false, false);
					}
				}
				catch (ThreadInterruptedException)
				{
					_session.Log.Warn($"Logon response waiter was interrupted. Session {GetSessionName()}");
				}
			}

			public string GetSessionName()
			{
				var @params = _session.Parameters;
				return @params.SessionId.ToString();
			}

			public void Cancel()
			{
				_canceled = true;
				_countDownLatch.Signal();
			}

			private void Dispose(bool disposing)
			{
				if (disposing)
				{
					_countDownLatch.Dispose();
				}
			}

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}
		}

		#region IScheduledFixSession implementation
		public void Schedule()
		{
			VerifySessionState();

			Log.Debug($"Init session scheduler: {Parameters.SessionId}");

			var startTimeExpr = ConfigAdapter.TradePeriodBegin;
			var stopTimeExpr = ConfigAdapter.TradePeriodEnd;
			var timeZone = ConfigAdapter.TradePeriodTimeZone;

			ValidateParameters(startTimeExpr, stopTimeExpr);

			if (startTimeExpr != null)
			{
				Scheduler.ScheduleCronTask<InitiatorSessionStartTask>(startTimeExpr, timeZone);
			}

			if (stopTimeExpr != null)
			{
				Scheduler.ScheduleCronTask<InitiatorSessionStopTask>(stopTimeExpr, timeZone);
			}

			if (startTimeExpr == null || stopTimeExpr == null) return;

			if (!CanStartScheduledSession(startTimeExpr, stopTimeExpr, timeZone)) return;

			lock (SessionLock)
			{
				if (CanStartScheduledSession(startTimeExpr, stopTimeExpr, timeZone))
				{
					Connect();
				}
			}
		}

		private bool CanStartScheduledSession(string startTimeExpr, string stopTimeExpr, TimeZoneInfo timeZone)
		{
			var isDisconnected = SessionState.IsDisconnected(SessionState);
			var isInsideInterval = SessionTaskScheduler.IsInsideInterval(DateTimeOffset.UtcNow, startTimeExpr, stopTimeExpr, timeZone);
			return isDisconnected && isInsideInterval;
		}

		public void Deschedule()
		{
			Log.Debug($"Cancel session scheduler: {Parameters.SessionId}");

			if (Scheduler == null) return;

			if (Scheduler.IsTaskScheduled<InitiatorSessionStartTask>())
			{
				Log.Trace($"Cancel start session task: {Parameters.SessionId}");
				Scheduler.DescheduleTask<InitiatorSessionStartTask>();
			}

			if (Scheduler.IsTaskScheduled<InitiatorSessionStopTask>())
			{
				Log.Trace($"Cancel stop session task: {Parameters.SessionId}");
				Scheduler.DescheduleTask<InitiatorSessionStopTask>();
			}
		}
		
		private void ValidateParameters(string startTimeExpr, string stopTimeExpr)
		{
			ValidateStartTime(startTimeExpr);
			ValidateStopTime(stopTimeExpr);
		}

		private void ValidateStopTime(string stopTimeExpr)
		{
			if (stopTimeExpr == null) return;
			if (SessionTaskScheduler.IsValidCronExpression(stopTimeExpr)) return;

			Log.Error("Session stop time expression is invalid: " + stopTimeExpr);
			throw new ArgumentException("Session stop time expression is invalid: " + stopTimeExpr);
		}

		private void ValidateStartTime(string startTimeExpr)
		{
			if (startTimeExpr == null) return;
			if (SessionTaskScheduler.IsValidCronExpression(startTimeExpr)) return;

			Log.Error("Session start time expression is invalid: " + startTimeExpr);
			throw new ArgumentException("Session start time expression is invalid: " + startTimeExpr);
		}

		#endregion
	}
}
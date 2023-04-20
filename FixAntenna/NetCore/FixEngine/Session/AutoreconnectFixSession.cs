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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	internal class AutoreconnectFixSession : InitiatorFixSession, IBackupFixSession
	{
		private static readonly int NoAutoreconnect = int.Parse(Config.NoAutoreconnect);
		private readonly List<DnsEndPoint> _destinations;

		private volatile Thread _connectThread;

		private int _destinationIndex;
		private int _attempt;
		private readonly bool _enableAutoSwitchToBackupConnection;
		private readonly bool _cyclicSwitchBackupConnection;
		private readonly bool _resetBackup;
		private readonly bool _resetPrimary;
		private bool _requestedSwitch;

		private readonly int _maxAttempts;
		private readonly int _delay;

		private readonly object _sync = new object();

		public AutoreconnectFixSession(IFixMessageFactory factory, SessionParameters sessionParameters, HandlerChain fixSessionListener)
			: base(factory, sessionParameters, fixSessionListener)
		{
			_destinations = new List<DnsEndPoint>();
			if (sessionParameters.Host != null && sessionParameters.Port > 0)
			{
				_destinations.Add(new DnsEndPoint(sessionParameters.Host, sessionParameters.Port.Value));
			}

			_destinations.AddRange(sessionParameters.Destinations);
			_maxAttempts = ConfigAdapter.GetAutoReconnectAttempts(0);
			_delay = ConfigAdapter.AutoReconnectDelay;
			_enableAutoSwitchToBackupConnection = ConfigAdapter.IsAutoSwitchToBackupConnectionEnabled;
			_cyclicSwitchBackupConnection = ConfigAdapter.IsCyclicSwitchBackupConnectionEnabled;
			_resetBackup = ConfigAdapter.IsResetOnSwitchToBackupEnabled;
			_resetPrimary = ConfigAdapter.IsResetOnSwitchToPrimaryEnabled;
		}

		/// <inheritdoc />
		public override void InitTransport()
		{
			if (Transport == null)
			{
				if (_destinations.Count == 0)
				{
					throw new InvalidOperationException(
						"Endpoint configuration is wrong or missing. Check Port and Host settings.");
				}

				var initDestination = _destinations[0];
				Transport = GetTransport(initDestination.Host, initDestination.Port, Parameters);
			}

			base.InitTransport();
		}

		/// <inheritdoc />
		public override void SetFixSessionListener(IFixSessionListener listener)
		{
			var clientExtendedListener = listener is IExtendedFixSessionListener sessionListener ? sessionListener : null;
			var sessionHandler = new ExtendedListenerImpl(this, listener, clientExtendedListener);
			base.SetFixSessionListener(sessionHandler);
		}

		private sealed class ExtendedListenerImpl : IExtendedFixSessionListener
		{
			private readonly AutoreconnectFixSession _session;
			private readonly IFixSessionListener _listener;
			private readonly IExtendedFixSessionListener _clientExtendedListener;

			private readonly object _sync = new object();

			public ExtendedListenerImpl(AutoreconnectFixSession session, IFixSessionListener listener,
				IExtendedFixSessionListener clientExtendedListener)
			{
				_session = session;
				_listener = listener;
				_clientExtendedListener = clientExtendedListener;
			}

			/// <inheritdoc />
			public void OnMessageReceived(MsgBuf msgBuf)
			{
				_clientExtendedListener?.OnMessageReceived(msgBuf);
			}

			/// <inheritdoc />
			public void OnMessageSent(byte[] bytes, int offset, int length)
			{
				_clientExtendedListener?.OnMessageSent(bytes, offset, length);
			}

			public void OnSessionStateChange(SessionState sessionState)
			{
				_listener.OnSessionStateChange(sessionState);
				if (_session.IsActive && sessionState == SessionState.DisconnectedAbnormally)
				{
					if (_session._attempt < _session._maxAttempts || _session._maxAttempts == 0)
					{
						var previousSessionState = _session.SessionState;
						if (previousSessionState != SessionState.Dead)
						{
							//if user disposed session in previous callback
							lock (_sync)
							{
								_session.SessionState = SessionState.Reconnecting;
								_session.AutoChangeDestination();
								_session._attempt++;
								_session.StartConnecting();
							}
						}
					}
					else
					{
						if (_session._maxAttempts != NoAutoreconnect)
						{
							_session.Log.Warn(
								$"Unable to connect with attempt: {_session._attempt} while maxAttempts: {_session._maxAttempts}");
						}
					}
				}
				else
				{
					if (sessionState == SessionState.Connected)
					{
						_session._attempt = 0;
					}
				}
			}

			/// <inheritdoc />
			public void OnNewMessage(FixMessage message)
			{
				_listener.OnNewMessage(message);
			}
		}

		/// <inheritdoc />
		public override void Disconnect(DisconnectReason reasonType, string reasonDescription, bool isGracefull, bool isForced, bool continueReading)
		{
			base.Disconnect(reasonType, reasonDescription, isGracefull, isForced, continueReading);
			_connectThread?.Interrupt();
			if (SessionState == SessionState.Reconnecting)
			{
				//we need to reset this state
				SessionState = SessionState.Disconnected;
			}
		}

		/// <inheritdoc />
		public override void StartSession()
		{
			if (_requestedSwitch)
			{
				if (_destinationIndex == 0)
				{
					if (_resetPrimary)
					{
						if (Log.IsDebugEnabled)
						{
							Log.Debug("Resetting sequences for primary connection");
						}

						try
						{
							SequenceManager.ResetSeqNumForNextConnect();
						}
						catch (IOException e)
						{
							Log.Error("Can't reset sequence numbers before connect to primary host", e);
						}
					}
				}
				else if (_destinationIndex > 0 && _resetBackup)
				{
					if (Log.IsDebugEnabled)
					{
						Log.Debug("Resetting sequences for backup connection");
					}

					try
					{
						SequenceManager.ResetSeqNumForNextConnect();
					}
					catch (IOException e)
					{
						Log.Error("Can't reset sequence numbers before connect to primary host", e);
					}
				}

				_requestedSwitch = false;
			}

			base.StartSession();
		}

		/// <inheritdoc />
		protected override void ConnectInternal()
		{
			try
			{
				base.ConnectInternal();
			}
			catch (IOException e)
			{
				Log.Warn(e.Message); //not rethrowing exception as connection might still be in progress on other destinations
			}
		}

		protected override async Task ConnectInternalAsync()
		{
			try
			{
				await base.ConnectInternalAsync().ConfigureAwait(false);
			}
			catch (IOException e)
			{
				Log.Warn(e.Message); //not rethrowing exception as connection might still be in progress on other destinations
			}
		}

		private void StartConnecting()
		{
			_connectThread = new Thread(() =>
			{
				Sleep();
				if (IsActive)
				{
					ConnectInternal();
				}
			}) { Name = "AutoreconnectThread" };

			_connectThread.Start();
		}

		private void Sleep()
		{
			if (_delay <= 0)
			{
				return;
			}

			try
			{
				Thread.Sleep(_delay);
			}
			catch (Exception)
			{
				// An exception can be thrown as the result of the invoking "interrupt" from another thread.
				// Ignore.
			}
		}

		private void AutoChangeDestination()
		{
			lock (_sync)
			{
				if (_destinations.Count == 1)
				{
					// return if single destination
					return;
				}

				if (_destinationIndex == 0 && !_enableAutoSwitchToBackupConnection)
				{
					//enabled primary transport but disabled automatic switch to backup
					return;
				}

				var newIndex = _destinationIndex + 1;
				if (newIndex > _destinations.Count - 1)
				{
					newIndex = 0;
				}

				if (newIndex == 0 && !_cyclicSwitchBackupConnection)
				{
					//next transport is again primary but disabled automatic switch to primary
					return;
				}

				ChangeDestination(newIndex);
			}
		}

		private void ChangeDestination(int newIndex)
		{
			lock (_sync)
			{
				if (_destinationIndex == newIndex)
				{
					Log.Warn("Can't change transport destination cause there is only one destination");
					return;
				}

				if (newIndex >= _destinations.Count || newIndex < 0)
				{
					Log.Warn("Invalid destination index");
					return;
				}

				_destinationIndex = newIndex;

				var currentDestination = _destinations[_destinationIndex];
				Transport = GetTransport(currentDestination.Host, currentDestination.Port, Parameters);
				Log.Info($"Transport destination has changed to host {currentDestination.Host}");
				_requestedSwitch = true;
			}
		}

		/// <inheritdoc />
		public void SwitchToBackUp()
		{
			if (IsRunningOnSingleBackup)
			{
				return;
			}

			if (SessionState.IsNotDisconnected(SessionState))
			{
				// close session and all session object (pumper,queue ect)
				// after this call, in our session listener,
				//  we must handled received state(DISCONNECTED_ABNORMALLY)
				Disconnect(DisconnectReason.UserRequest, "Manual switch to backup connection");
				Shutdown(DisconnectReason.UserRequest, true);
			}
			else
			{
				//reset initialization in case if session was disconnected
				ResetInitialization();
			}

			if (_delay > 0)
			{
				if (Log.IsDebugEnabled)
				{
					Log.Debug($"Wait {_delay} ms before establish backup connection");
				}

				try
				{
					Thread.Sleep(_delay);
				}
				catch (Exception)
				{
					// do nothing
				}
			}

			Log.Debug("Manual switch to backup");
			_attempt = 0;

			var newIndex = _destinationIndex + 1;
			if (newIndex > _destinations.Count - 1)
			{
				newIndex = 1;
			}

			ChangeDestination(newIndex);
			ConnectInternal();
			//FIXME: if session is disposed on disconnect - session will be removed from manager.
			RegisterSessionIfAbsent();
		}

		private void RegisterSessionIfAbsent()
		{
			var fixSessionManager = FixSessionManager.Instance;
			if (fixSessionManager.Locate(Parameters.SessionId) == null)
			{
				fixSessionManager.RegisterFixSession(this);
			}
		}

		/// <inheritdoc />
		public void SwitchToPrimary()
		{
			if (!IsRunningOnBackup)
			{
				return;
			}

			// close session and all session object (pumper,queue ect)
			// after this call, in our session listener,
			//  we must handled received state(DISCONNECTED_ABNORMALLY)
			Disconnect(DisconnectReason.UserRequest, "Manual switch to primary connection");
			Shutdown(DisconnectReason.UserRequest, true);

			if (_delay > 0)
			{
				if (Log.IsDebugEnabled)
				{
					Log.Debug($"Wait {_delay} ms before establish primary connection");
				}

				try
				{
					Thread.Sleep(_delay);
				}
				catch (Exception)
				{
					// do nothing
				}
			}

			Log.Debug("Manual switch to primary");
			_attempt = 0;
			ChangeDestination(0);
			ConnectInternal();
			//FIXME: if session is disposed on disconnect - session will be removed from manager.
			RegisterSessionIfAbsent();
		}

		/// <inheritdoc />
		public bool IsRunningOnBackup
		{
			get
			{
				lock (_sync)
				{
					return _destinationIndex != 0;
				}
			}
		}

		private bool IsRunningOnSingleBackup
		{
			get
			{
				lock (_sync)
				{
					return (_destinationIndex != 0) && (_destinations.Count == 2);
				}
			}
		}
	}
}
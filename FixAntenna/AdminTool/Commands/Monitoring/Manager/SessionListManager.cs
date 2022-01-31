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
using System.Threading;
using System.Threading.Tasks;
using Epam.FixAntenna.AdminTool.Resources;
using Epam.FixAntenna.AdminTool.Commands.Util;
using Epam.FixAntenna.Fixicc.Message;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using Action = Epam.FixAntenna.Fixicc.Message.Action;

namespace Epam.FixAntenna.AdminTool.Commands.Monitoring.Manager
{
	internal class SessionListManager
	{
		private bool _instanceFieldsInitialized = false;

		private void InitializeInstanceFields()
		{
			Log = LogFactory.GetLog(GetType());
		}

		protected internal ILog Log;

		private readonly SessionStatePoller _sessionStatePoller;

		private Command _parentCommand;
		private ConfiguredSessionListListener _configuredSessionListListener;
		private ActiveSessionListListener _activeSessionListListener;
		private IFixSessionStateListener _sessionStateListener;

		public SessionListManager(Command parentCommand)
		{
			if (!_instanceFieldsInitialized)
			{
				InitializeInstanceFields();
				_instanceFieldsInitialized = true;
			}
			_parentCommand = parentCommand;
			_sessionStatePoller = new SessionStatePoller(this);
			_configuredSessionListListener = new ConfiguredSessionListListener(this);
			_activeSessionListListener = new ActiveSessionListListener(this);
			_sessionStateListener = new SessionStateListener(this);
		}


		public virtual void DoSnapshot()
		{
			var constructor = GetSnapshotConstructor();
			try
			{
				constructor.Response.Description = Strings.OperationSessionListSubscribeSuccess;
				_parentCommand.SendResponseSuccess(constructor.Response);
			}
			catch (Exception e)
			{
				Log.Error("Error on get snapshot.", e);
				_parentCommand.SendError(e.Message);
			}
		}

		public virtual void DoUnsubscribe()
		{
			try
			{
				_sessionStatePoller.Stop();

				Unsubscribe();
				var response = new Response
				{
					Description = Strings.OperationSessionListUnsubscribeSnapshotSuccess
				};
				_parentCommand.SendResponseSuccess(response);
			}
			catch (Exception e)
			{
				Log.Error("Error on unsubscribe.", e);
				_parentCommand.SendError(e.Message);
			}
		}

		public virtual void DoSubscribeWithSnapshot()
		{
			Subscribe();

			_sessionStatePoller.Start();

			var constructor = GetSnapshotConstructor();
			try
			{
				constructor.Response.Description = Strings.OperationSessionListWithSnapshotSuccess;
				_parentCommand.SendResponseSuccess(constructor.Response);
			}
			catch (Exception e)
			{
				Log.Error("Error on subscribe with snapshot.", e);
				_parentCommand.SendError(e.Message);
			}
		}

		public virtual void DoSubscribe()
		{
			try
			{
				Subscribe();
				var response = new Response
				{
					Description = Strings.OperationSessionListSubscribeSuccess
				};
				_parentCommand.SendResponseSuccess(response);
			}
			catch (Exception e)
			{
				Log.Error("Error on subscribe.", e);
				_parentCommand.SendError(e.Message);
			}
		}

		private void Subscribe()
		{
			FixSessionManager.Instance
				.RegisterSessionManagerListener(_activeSessionListListener);

			_parentCommand
				.ConfiguredSessionRegister
				.AddSessionManagerListener(_configuredSessionListListener);
		}

		private void Unsubscribe()
		{
			FixSessionManager.Instance
				.UnregisterSessionManagerListener(_activeSessionListListener);

			_parentCommand
				.ConfiguredSessionRegister
				.RemoveSessionManagerListener(_configuredSessionListListener);
		}

		private SessionsListDataConstructor GetSnapshotConstructor()
		{
			var constructor = new SessionsListDataConstructor();
			var sessionIDs = new HashSet<SessionId>();
			foreach (var fixSession in GetFixSessions())
			{
				constructor.AddFixSession(fixSession, Action.NEW);
				sessionIDs.Add(fixSession.Parameters.SessionId);
			}

			foreach (var @params in GetFixSessionsConfigured())
			{
				var id = @params.SessionId;
				if (!sessionIDs.Contains(id))
				{
					constructor.AddFixSessionConfigured(@params, Action.NEW);
					sessionIDs.Add(id);
				}
			}
			return constructor;
		}

		/// <summary>
		/// Gets fix session list.
		/// </summary>
		public virtual IList<IExtendedFixSession> GetFixSessions()
		{
			return FixSessionManager.Instance.SessionListCopy;
		}

		/// <summary>
		/// Gets configured fix session list.
		/// </summary>
		public virtual IList<SessionParameters> GetFixSessionsConfigured()
		{
			return _parentCommand.ConfiguredSessionRegister.RegisteredSessions;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "<Pending>")]
		private sealed class SessionStatePoller
		{
			private readonly SessionListManager _manager;
			private readonly Queue<SessionsListDataSession> _queue = new Queue<SessionsListDataSession>();
			private CancellationTokenSource _cts;

			public SessionStatePoller(SessionListManager manager)
			{
				_manager = manager;
			}

			public void Start()
			{
				_cts = new CancellationTokenSource();
				_ = Run(Payload, TimeSpan.FromSeconds(1), _cts.Token);
			}

			private void Payload()
			{
				lock (_queue)
				{
					if (_queue.Count == 0)
					{
						return;
					}

					var constructor = new SessionsListDataConstructor();
					while (_queue.Count > 0)
					{
						var session = _queue.Dequeue();
						constructor.AddFixSession(session);
					}
					_manager._parentCommand.SendResponseSuccess(constructor.Response);
				}
			}

			public void Stop()
			{
				_cts.Cancel();
				_cts.Dispose();
			}

			private async Task Run(System.Action action, TimeSpan period, CancellationToken token)
			{
				try
				{
					while (!token.IsCancellationRequested)
					{
						await Task.Delay(period, token).ConfigureAwait(false);

						if (!token.IsCancellationRequested)
							action();
					}
				}
				catch (TaskCanceledException)
				{
					// it's ok
					_manager.Log.Trace("SessionStatePoller cancelled.");
				}
			}

			public void Add(SessionsListDataSession session)
			{
				lock (_queue)
				{
					_queue.Enqueue(session);
				}
			}

			public void AddAll(IEnumerable<SessionsListDataSession> sessions)
			{
				lock (_queue)
				{
					foreach (var session in sessions)
					{
						_queue.Enqueue(session);
					}
				}
			}
		}

		private class SessionsListDataConstructor
		{
			public Response Response { get; }

			public SessionsListDataConstructor()
			{
				Response = new Response { SessionsListData = new SessionsListData() };
			}

			public void AddFixSession(SessionsListDataSession session)
			{
				Response.SessionsListData.Session.Add(session);
			}

			public void AddFixSession(IExtendedFixSession session, Action action)
			{
				Response.SessionsListData.Session.Add(CreateJaxbSession(session, action));
			}

			public void AddFixSessionConfigured(SessionParameters @params, Action action)
			{
				Response.SessionsListData.Session.Add(CreateJaxbConfiguredSession(@params, action));
			}

			private SessionsListDataSession CreateJaxbSession(IExtendedFixSession fixSession, Action action)
			{
				var sessionParameters = fixSession.Parameters;
				var runtimeState = fixSession.RuntimeState;

				var session = new SessionsListDataSession
				{
					SenderCompID = fixSession.Parameters.SenderCompId,
					TargetCompID = fixSession.Parameters.TargetCompId,
					SessionQualifier = fixSession.Parameters.SessionQualifier,
					Status = fixSession.SessionState.ToString(),
					Timestamp = DateTime.UtcNow,
					StatusGroup = CommandUtil.GetStatusGroup(fixSession),
					Action = action,
					InSeqNum = CommandUtil.GetInSeqNum(sessionParameters, runtimeState),
					OutSeqNum = CommandUtil.GetOutSeqNum(sessionParameters, runtimeState),
					BackupState = CommandUtil.IsBackupHost(fixSession)
				};

				return session;
			}

			private SessionsListDataSession CreateJaxbConfiguredSession(SessionParameters @params, Action action)
			{
				var session = new SessionsListDataSession
				{
					SenderCompID = @params.SenderCompId,
					TargetCompID = @params.TargetCompId,
					SessionQualifier = @params.SessionQualifier,
					Timestamp = DateTime.UtcNow,
					Status = CommandUtil.ConfiguredSessionStatus,
					StatusGroup = CommandUtil.ConfiguredSessionStatusGroup,
					Action = action
				};

				//FIXME_NOW
				//session.SetInSeqNum(CommandUtil.getInSeqNum(params));
				//session.SetOutSeqNum(CommandUtil.getOutSeqNum(params));

				return session;
			}
		}

		private class ConfiguredSessionListListener : IConfiguredSessionListener
		{
			private readonly SessionListManager _manager;

			public ConfiguredSessionListListener(SessionListManager manager)
			{
				_manager = manager;
			}

			public void OnAddSession(SessionParameters @params)
			{
				var constructor = new SessionsListDataConstructor();
				constructor.AddFixSessionConfigured(@params, Action.NEW);
				try
				{
					_manager._parentCommand.SendResponseSuccess(constructor.Response);
				}
				catch (Exception e)
				{
					_manager.Log.Error("Error on notify event about session", e);
				}
			}

			public void OnRemoveSession(SessionParameters @params)
			{
				var constructor = new SessionsListDataConstructor();
				constructor.AddFixSessionConfigured(@params, Action.DELETED);
				try
				{
					_manager._parentCommand.SendResponseSuccess(constructor.Response);
				}
				catch (Exception e)
				{
					_manager.Log.Error("Error on notify event about session", e);
				}
			}
		}

		private class ActiveSessionListListener : IFixSessionListListener
		{
			private readonly SessionListManager _manager;

			public ActiveSessionListListener(SessionListManager manager)
			{
				_manager = manager;
			}

			public void OnAddSession(IExtendedFixSession fixSession)
			{
				if (fixSession is ISessionStateListenSupport sessionStateListenSupport)
				{
					sessionStateListenSupport.AddSessionStateListener(_manager._sessionStateListener);
				}

				var constructor = new SessionsListDataConstructor();
				var activeParams = fixSession.Parameters;

				var action = _manager._parentCommand.ConfiguredSessionRegister.IsSessionRegistered(activeParams.SessionId)
					? Action.UPDATED : Action.NEW;

				constructor.AddFixSession(fixSession, action);
				try
				{
					_manager._parentCommand.SendResponseSuccess(constructor.Response);
				}
				catch (Exception e)
				{
					_manager.Log.Error("Error on notify event about session", e);
				}
			}

			public void OnRemoveSession(IExtendedFixSession fixSession)
			{
				if (fixSession is ISessionStateListenSupport sessionStateListenSupport)
				{
					sessionStateListenSupport.RemoveSessionStateListener(_manager._sessionStateListener);
				}

				var constructor = new SessionsListDataConstructor();
				var activeParams = fixSession.Parameters;
				var configuredParams = _manager._parentCommand.ConfiguredSessionRegister.GetSessionParams(activeParams.SessionId);
				if (configuredParams != null)
				{
					constructor.AddFixSessionConfigured(configuredParams, Action.UPDATED);
				}
				else
				{
					constructor.AddFixSession(fixSession, Action.DELETED);
				}

				try
				{
					if (fixSession != _manager._parentCommand.AdminFixSession)
					{
						_manager._parentCommand.SendResponseSuccess(constructor.Response);
					}
					else
					{
						Task.Run(() => _manager.Unsubscribe()).ConfigureAwait(false);
					}
				}
				catch (Exception e)
				{
					_manager.Log.Error("Error on notify event about session", e);
				}
			}
		}

		private class SessionStateListener : IFixSessionStateListener
		{
			private readonly SessionListManager _manager;

			public SessionStateListener(SessionListManager manager)
			{
				_manager = manager;
			}

			public void OnSessionStateChange(StateEvent stateEvent)
			{
				var constructor = new SessionsListDataConstructor();
				constructor.AddFixSession((IExtendedFixSession) stateEvent.GetSource(), Action.UPDATED);
				_manager._sessionStatePoller.AddAll(constructor.Response.SessionsListData.Session);
			}
		}
	}
}
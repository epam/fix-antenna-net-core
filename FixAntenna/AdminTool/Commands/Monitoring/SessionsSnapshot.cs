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
using Epam.FixAntenna.AdminTool.Commands.Util;
using Epam.FixAntenna.Fixicc.Message;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Session;

namespace Epam.FixAntenna.AdminTool.Commands.Monitoring
{
	/// <summary>
	/// The SessionsSnapshot command.
	/// Returns the list of sessions with enxtra parameters.
	/// </summary>
	internal class SessionsSnapshot : Command
	{
		public override void Execute()
		{
			// StatusData - STATUS,
			// ParamsData - STATUS_PARAMS,
			// StatData   - STATUS_PARAMS_STAT,
			Log.Debug("Execute SessionsSnapshot Command");
			try
			{
				var snapshot = (Fixicc.Message.SessionsSnapshot) Request;
				var snapshotData = new SessionsSnapshotData();

				if (!ValidateSessionsSnapshot(snapshot))
				{
					return;
				}

				// active sessions
				var sessions = GetFixSessions();
				foreach (var fixSession in sessions)
				{
					var session = new SessionsSnapshotDataSession();
					var id = fixSession.Parameters.SessionId;
					session.SenderCompID = id.Sender;
					session.TargetCompID = id.Target;
					session.SessionQualifier = id.Qualifier;

					SetDataForSession(snapshot.View, session, fixSession);
					// added extended parameter if need
					if (IsNeedExtendedParameters(snapshot.SessionView, id))
					{
						var views = GetExtendedParameters(snapshot.SessionView, id);
						using (var it = views.GetEnumerator())
						{
							while (it.MoveNext())
							{
								var view = it.Current;
								SetDataForSession(view, session, fixSession);
							}
						}
					}
					snapshotData.Session.Add(session);
				}
				var activeSessionIDs = GetSessionsIDs(sessions);
				// preconfigured sessions
				var preconfigSessions = GetConfiguredSessionParameters();
				foreach (var sParam in preconfigSessions)
				{
					var id = sParam.SessionId;
					if (activeSessionIDs.Contains(id))
					{
						continue;
					}
					var session = new SessionsSnapshotDataSession();
					session.SenderCompID = sParam.SenderCompId;
					session.TargetCompID = sParam.TargetCompId;
					session.SessionQualifier = sParam.SessionQualifier;

					SetDataForConfiguredSession(snapshot.View, session, sParam);
					// added extended parameter if need
					if (IsNeedExtendedParameters(snapshot.SessionView, id))
					{
						var views = GetExtendedParameters(snapshot.SessionView, id);
						using (var it = views.GetEnumerator())
						{
							while (it.MoveNext())
							{
								var view = it.Current;
								SetDataForConfiguredSession(view, session, sParam);
							}
						}
					}
					snapshotData.Session.Add(session);
				}

				var response = new Response();
				response.SessionsSnapshotData = snapshotData;
				SendResponseSuccess(response);
			}
			catch (Exception e)
			{
				Log.Error(e.Message, e);
				SendError(e.Message);
			}
		}

		private ISet<SessionId> GetSessionsIDs(IList<IExtendedFixSession> sessions)
		{
			ISet<SessionId> result = new HashSet<SessionId>();
			foreach (var s in sessions)
			{
				result.Add(s.Parameters.SessionId);
			}
			return result;
		}

		private bool ValidateSessionsSnapshot(FixAntenna.Fixicc.Message.SessionsSnapshot sessionsSnapshot)
		{
			if (sessionsSnapshot.SessionView == null)
			{
				return true;
			}

			foreach (var sessionView in sessionsSnapshot.SessionView)
			{
				if (string.IsNullOrWhiteSpace(sessionView.SenderCompID))
				{
					SendInvalidArgument("Parameter SenderCompID in SessionView is required");
					return false;
				}
				if (string.IsNullOrWhiteSpace(sessionView.TargetCompID))
				{
					SendInvalidArgument("Parameter TargetCompID in SessionView is required");
					return false;
				}
			}
			return true;
		}

		private void SetDataForSession(View view, SessionsSnapshotDataSession session, IExtendedFixSession fixSession)
		{
			if (View.STATUS_PARAMS_STAT == view)
			{
				session.StatData = GenerateSessionParamsStat(fixSession);
			}
			else if (View.STATUS_PARAMS == view)
			{
				session.ParamsData = GenerateSessionStatusParams(fixSession);
			}
			else if (View.STATUS == view)
			{
				session.StatusData = GenerateSessionStatus(fixSession);
			}
		}

		private void SetDataForConfiguredSession(View view, SessionsSnapshotDataSession session, SessionParameters sessionParam)
		{
			if (View.STATUS_PARAMS_STAT == view)
			{
				// no statistic for configured session
				// session.setStatData(generateSessionParamsStat(sessionParam));
			}
			else if (View.STATUS_PARAMS == view)
			{
				session.ParamsData = GenerateSessionStatusParamsForConfigured(sessionParam);
			}
			else if (View.STATUS == view)
			{
				session.StatusData = GenerateSessionStatus(sessionParam);
			}
		}

		private StatData GenerateSessionParamsStat(IExtendedFixSession fixSession)
		{
			var statData = new StatData();
			statData.Established = DateTimeHelper.FromMilliseconds(fixSession.IsEstablished);
			statData.LastReceivedMessage = DateTimeHelper.FromMilliseconds(fixSession.LastInMessageTimestamp);
			statData.LastSentMessage = DateTimeHelper.FromMilliseconds(fixSession.LastOutMessageTimestamp);
			if (fixSession.IsStatisticEnabled)
			{
				statData.NumOfProcessedMessages = fixSession.NoOfInMessages + fixSession.NoOfOutMessages;
				statData.ReceivedBytes = (int) fixSession.BytesRead;
				statData.ReceivedMessages = (int) fixSession.NoOfInMessages;
				statData.SentBytes = (int) fixSession.BytesSent;
				statData.SentMessages = (int) fixSession.NoOfOutMessages;
			}
			return statData;
		}

		private ParamsData GenerateSessionStatusParams(IExtendedFixSession fixSession)
		{
			var @params = fixSession.Parameters;
			var runtimeState = fixSession.RuntimeState;
			var paramsData = new ParamsData();

			paramsData.Role = CommandUtil.GetRole(fixSession);

			paramsData.ExtraSessionParams = CommandUtil.GetExtraSessionParams(fixSession);
			paramsData.RemoteHost = @params.Host;
			paramsData.RemotePort = @params.Port;
			paramsData.Version = CommandUtil.GetVersion(@params.FixVersion, @params.AppVersion);

			var statusData = new StatusData();
			statusData.BackupState = CommandUtil.IsBackupHost(fixSession);
			statusData.InSeqNum = CommandUtil.GetInSeqNum(@params, runtimeState);
			statusData.OutSeqNum = CommandUtil.GetOutSeqNum(@params, runtimeState);
			statusData.Status = fixSession.SessionState.ToString();
			statusData.StatusGroup = CommandUtil.GetStatusGroup(fixSession);

			return paramsData;
		}

		private ParamsData GenerateSessionStatusParamsForConfigured(SessionParameters @params)
		{
			var paramsData = new ParamsData();

			// preconfigured can be only acceptor sessions
			paramsData.Role = SessionRole.ACCEPTOR;

			paramsData.ExtraSessionParams = CommandUtil.GetExtraSessionParams(@params, null);
			paramsData.Version = CommandUtil.GetVersion(@params.FixVersion, @params.AppVersion);

			var statusData = new StatusData();
			//FIXME_NOW
	//        statusData.SetInSeqNum(CommandUtil.getInSeqNum(params));
	//        statusData.SetOutSeqNum(CommandUtil.getOutSeqNum(params));
			statusData.Status = CommandUtil.ConfiguredSessionStatus;
			statusData.StatusGroup = CommandUtil.ConfiguredSessionStatusGroup;

			return paramsData;
		}

		private StatusData GenerateSessionStatus(IExtendedFixSession fixSession)
		{
			var @params = fixSession.Parameters;
			var runtimeState = fixSession.RuntimeState;
			// TODO: why we set sender and target for active session
	//        params.setSenderCompId(fixSession.GetSessionParameters().getSenderCompId());
	//        params.setTargetCompId(fixSession.GetSessionParameters().getTargetCompId());

			var statusData = new StatusData();
			statusData.InSeqNum = CommandUtil.GetInSeqNum(@params, runtimeState);
			statusData.OutSeqNum = CommandUtil.GetOutSeqNum(@params, runtimeState);
			statusData.StatusGroup = CommandUtil.GetStatusGroup(fixSession);
			statusData.Status = fixSession.SessionState.ToString();
			statusData.BackupState = CommandUtil.IsBackupHost(fixSession);
			return statusData;
		}

		private StatusData GenerateSessionStatus(SessionParameters @params)
		{
			var statusData = new StatusData();

			//FIXME_NOW
	//        statusData.SetInSeqNum(CommandUtil.getInSeqNum(params));
	//        statusData.SetOutSeqNum(CommandUtil.getOutSeqNum(params));
			statusData.Status = CommandUtil.ConfiguredSessionStatus;
			statusData.StatusGroup = CommandUtil.ConfiguredSessionStatusGroup;
			return statusData;
		}

		private bool IsNeedExtendedParameters(IList<SessionsSnapshotSessionView> sessionViews, SessionId id)
		{
			if (sessionViews == null || sessionViews.Count == 0)
			{
				return false;
			}
			foreach (var sessionView in sessionViews)
			{
				if (IsSessionIdEquals(id, sessionView.SenderCompID, sessionView.TargetCompID, sessionView.SessionQualifier))
				{
					return true;
				}
			}
			return false;
		}

		private bool IsSessionIdEquals(SessionId id, string sender, string target, string qualifier)
		{
			if (id.Sender.Equals(sender) && id.Target.Equals(target))
			{
				if (string.IsNullOrWhiteSpace(qualifier) || string.IsNullOrWhiteSpace(id.Qualifier))
				{
					return string.IsNullOrWhiteSpace(qualifier) && string.IsNullOrWhiteSpace(id.Qualifier);
				}
				return id.Qualifier.Equals(qualifier);
			}
			return false;
		}

		private ISet<View> GetExtendedParameters(IList<SessionsSnapshotSessionView> sessionViews, SessionId id)
		{
			ISet<View> views = new SortedSet<View>();
			foreach (var sessionView in sessionViews)
			{
				if (IsSessionIdEquals(id, sessionView.SenderCompID, sessionView.TargetCompID, sessionView.SessionQualifier))
				{
					views.Add(sessionView.View);
				}
			}
			return views;
		}
	}
}
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
using System.Net.Sockets;
using Epam.FixAntenna.Fixicc.Message;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.AdminTool.Commands.Administrative
{
	/// <summary>
	/// The CreateInitiator command.
	/// Creates a new initiator session.
	/// </summary>
	internal class CreateInitiator : CreateSession
	{
		public override void Execute()
		{
			Log.Debug("Execute CreateInitiator Command");
			try
			{
				if (RequestId == null)
				{
					SendInvalidArgument("Parameter RequestID is required");
					return;
				}

				var createInitiatorRequest = (Fixicc.Message.CreateInitiator) Request;

				var details = new SessionParameters();
				if (!FillSenderTargetIds(createInitiatorRequest, details))
				{
					return;
				}

				if (!FillConnectionParameters(createInitiatorRequest, details))
				{
					return;
				}

				if (!FillFixVersion(createInitiatorRequest, details))
				{
					return;
				}
				if (!FillBackupConnection(createInitiatorRequest, details))
				{
					return;
				}

				var extraSessionParams = createInitiatorRequest.ExtraSessionParams;
				if (extraSessionParams != null)
				{ // fixed bug 15170 Session using the RAI was not created
					if (!FillExtraSessionParams(details, extraSessionParams))
					{
						return;
					}
					FillCyclicSwitchOption(createInitiatorRequest, details);
					FillAutoSwitchToBackupOption(createInitiatorRequest, details);
					FillForcedReconnect(createInitiatorRequest, details);
				}
				if (ConfiguredSessionRegister.IsSessionRegistered(details.SessionId))
				{
					SendError($"Can't create initiator session. Session {details.SessionId} already exists");
					return;
				}
				var session = details.CreateNewFixSession();
				if (session == null)
				{
					SendError("Can't create session");
					return;
				}

				session.SetFixSessionListener(new FixSessionListenerAnonymousInnerClass(this));

				if (createInitiatorRequest.Backup != null
						&& createInitiatorRequest.Backup.ActiveConnection == ActiveConnection.BACKUP)
				{
					var backupFIXSession = (IBackupFixSession) session;
					//switch host and connect
					backupFIXSession.SwitchToBackUp();
				}
				else
				{
					session.Connect();
				}

				SendResponseSuccess(new Response());
			}
			catch (IOException e)
			{
				Log.Error(e);
				SendError($"Unknown host: {e.Message}");
			}
			catch (SocketException e)
			{
				Log.Error(e);
				SendError($"Unknown host: {e.Message}");
			}
			catch (Exception e)
			{
				Log.Error(e);
				SendError(e.Message);
			}
		}

		internal class FixSessionListenerAnonymousInnerClass : IFixSessionListener
		{
			private readonly CreateInitiator _command;

			public FixSessionListenerAnonymousInnerClass(CreateInitiator command)
			{
				_command = command;
			}

			public void OnNewMessage(FixMessage message)
			{
				_command.Log.Debug($"Received new message: {message}");
			}

			public void OnSessionStateChange(SessionState sessionState)
			{
				// handle state
		//                    if (SessionState.IsDisconnected(sessionState)) {
		//                        session.dispose();
		//                    }
			}
		}

		private void FillForcedReconnect(FixAntenna.Fixicc.Message.CreateInitiator createInitiatorRequest, SessionParameters details)
		{
			var forcedReconnect = createInitiatorRequest.ExtraSessionParams.ForcedReconnect;
			if (forcedReconnect != null)
			{
				if (forcedReconnect.Value)
				{
					details.Configuration.SetProperty(Config.AutoreconnectAttempts, Config.InfinityAutoreconnect);
				}
				else
				{
					details.Configuration.SetProperty(Config.AutoreconnectAttempts, Config.NoAutoreconnect);
				}
			}
		}

		private void FillAutoSwitchToBackupOption(FixAntenna.Fixicc.Message.CreateInitiator createInitiatorRequest, SessionParameters details)
		{
			// enableAutoSwitchToBackupConnection
			var enableAutoSwitchToBackupConnection = createInitiatorRequest.ExtraSessionParams.EnableAutoSwitchToBackupConnection;
			if (enableAutoSwitchToBackupConnection != null)
			{
				details.Configuration.SetProperty(Config.EnableAutoSwitchToBackupConnection, enableAutoSwitchToBackupConnection.ToString());
			}
		}

		private void FillCyclicSwitchOption(FixAntenna.Fixicc.Message.CreateInitiator createInitiatorRequest, SessionParameters details)
		{
			// cyclicSwitchBackupConnection
			var cyclicSwitchBackupConnection = createInitiatorRequest.ExtraSessionParams.CyclicSwitchBackupConnection;
			if (cyclicSwitchBackupConnection != null)
			{
				details.Configuration.SetProperty(Config.CyclicSwitchBackupConnection, cyclicSwitchBackupConnection.ToString());
			}
		}

		private bool FillBackupConnection(FixAntenna.Fixicc.Message.CreateInitiator createInitiatorRequest, SessionParameters details)
		{
			// back up
			if (createInitiatorRequest.Backup != null)
			{
				if (createInitiatorRequest.Backup.RemoteHost == null)
				{
					SendInvalidArgument("Parameter RemoteHost is required");
					return Fail;
				}
				if (createInitiatorRequest.Backup.RemotePort <= 0)
				{
					SendInvalidArgument("Parameter RemotePort must be > 0");
					return Fail;
				}
				details.AddDestination(createInitiatorRequest.Backup.RemoteHost, createInitiatorRequest.Backup.RemotePort);

				if (createInitiatorRequest.Backup.ExtraSessionParams != null)
				{
					Log.Warn("There is Extra Session Params in Backup Tag");
				}
			}
			return Success;
		}

		private bool FillConnectionParameters(FixAntenna.Fixicc.Message.CreateInitiator createInitiatorRequest, SessionParameters details)
		{
			if (string.IsNullOrWhiteSpace(createInitiatorRequest.RemoteHost))
			{
				SendInvalidArgument("Parameter RemoteHost is required");
				return Fail;
			}
			if (createInitiatorRequest.RemotePort <= 0)
			{
				SendInvalidArgument("Parameter RemotePort is required");
				return Fail;
			}
			details.Host = createInitiatorRequest.RemoteHost;
			details.Port = createInitiatorRequest.RemotePort;
			return Success;
		}
	}
}
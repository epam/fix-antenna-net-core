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

using Epam.FixAntenna.Fixicc.Message;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.NetCore.FixEngine.Storage;

using ForceSeqNumReset = Epam.FixAntenna.NetCore.FixEngine.ForceSeqNumReset;

namespace Epam.FixAntenna.AdminTool.Commands.Util
{
	internal class CommandUtil
	{
		public static readonly StatusGroup ConfiguredSessionStatusGroup = StatusGroup.DISCONNECTED;
		public const string ConfiguredSessionStatus = "CONFIGURED";
		public const string FixversionDelimiter = ":";
		public static readonly Dictionary<string, FixVersion> FixVersionMapping = new Dictionary<string, FixVersion>();

		static CommandUtil()
		{
			var list = FixVersion.FixVersionEnum;
			while (list.MoveNext())
			{
				var version = list.Current;
				FixVersionMapping[ToAdminProtocol(version)] = version;
			}
		}

		/// <summary>
		/// Gets extra session params from fix session.
		/// </summary>
		/// <param name="fixSession"> the fix session </param>
		public static ExtraSessionParams GetExtraSessionParams(IExtendedFixSession fixSession)
		{
			var sessionParametersInstance = fixSession.Parameters;
			var runtimeState = fixSession.RuntimeState;
			var extraSessionParams = GetExtraSessionParams(sessionParametersInstance, runtimeState);

			if (fixSession is IBackupFixSession)
			{
				extraSessionParams.EnableAutoSwitchToBackupConnection = GetPropertyAsBollean(fixSession, Config.EnableAutoSwitchToBackupConnection, false);
				extraSessionParams.CyclicSwitchBackupConnection = GetPropertyAsBollean(fixSession, Config.CyclicSwitchBackupConnection, false);
			}
			else
			{
				extraSessionParams.EnableAutoSwitchToBackupConnection = false;
				extraSessionParams.CyclicSwitchBackupConnection = false;
				extraSessionParams.KeepConnectionState = false;
			}
			return extraSessionParams;
		}

		/// <summary>
		/// Gets extra session params from FIX session parameters.
		/// Use this method for preconfigured sessions. For active session use method <code>CommandUtil.GetExtraSessionParams(FixAntenna.NetCore.FixEngine.Session.IExtendedFixSession)</code>
		/// </summary>
		/// <param name="parameters"> the FIX session parameters </param>
		/// <seealso cref="CommandUtil.GetExtraSessionParams(IExtendedFixSession)"/>
		public static ExtraSessionParams GetExtraSessionParams(SessionParameters parameters, FixSessionRuntimeState runtimeState)
		{
			var extraSessionParams = new ExtraSessionParams();
			var configuration = parameters.Configuration;

			// Extended
			extraSessionParams.TargetLocationID = parameters.TargetLocationId;
			extraSessionParams.TargetSubID = parameters.TargetSubId;
			extraSessionParams.SenderLocationID = parameters.SenderLocationId;
			extraSessionParams.SenderSubID = parameters.SenderSubId;
			extraSessionParams.HBI = parameters.HeartbeatInterval;

			if (!string.IsNullOrWhiteSpace(parameters.SessionQualifier))
			{
				var qualifierTag = configuration.GetProperty(Config.LogonMessageSessionQualifierTag);
				extraSessionParams.LogonMessageSessionQualifierTag = qualifierTag;
			}

			// Enabled for initiator only
			var storage = configuration.GetProperty(Config.StorageFactory);
			if (storage != null)
			{
				if (storage.Equals(typeof(InMemoryStorageFactory).FullName))
				{
					extraSessionParams.StorageType = StorageType.TRANSIENT;
				}
				else if (storage.Equals(typeof(MmfStorageFactory).FullName))
				{
					extraSessionParams.StorageType = StorageType.PERSISTENTMM;
				}
				else if (storage.Equals(typeof(SlicedFileStorageFactory).FullName))
				{
					extraSessionParams.StorageType = StorageType.SPLITPERSISTENT;
				}
				else
				{
					extraSessionParams.StorageType = StorageType.PERSISTENT;
				}
			}
			else
			{
				extraSessionParams.StorageType = StorageType.PERSISTENT;
			}
			extraSessionParams.EnableMessageRejecting = configuration.GetPropertyAsBoolean(Config.EnableMessageRejecting, false);
			var maxMessagesAmountInBunch = configuration.GetPropertyAsInt(Config.MaxMessagesToSendInBatch, 1, int.MaxValue, false);
			if (maxMessagesAmountInBunch > 0)
			{
				extraSessionParams.MaxMessagesAmountInBunch = maxMessagesAmountInBunch;
			}

			extraSessionParams.EncryptMethod = EncryptMethod.NONE;
			extraSessionParams.ClientType = ClientType.GENERIC;

			var autoReconnectAttempts = configuration.GetPropertyAsInt(Config.AutoreconnectAttempts);
			extraSessionParams.ReconnectMaxTries = autoReconnectAttempts;
			extraSessionParams.ForcedReconnect = autoReconnectAttempts >= 0;
			extraSessionParams.DisableTCPBuffer = !configuration.GetPropertyAsBoolean(Config.EnableNagle);

			// Sequence Numbers
			extraSessionParams.InSeqNum = GetInSeqNum(parameters, runtimeState);
			extraSessionParams.OutSeqNum = GetOutSeqNum(parameters, runtimeState);

			if (parameters.ForceSeqNumReset == ForceSeqNumReset.Always)
			{
				extraSessionParams.ForceSeqNumReset = Fixicc.Message.ForceSeqNumReset.ALWAYS;
			}
			else if (parameters.ForceSeqNumReset == ForceSeqNumReset.OneTime)
			{
				extraSessionParams.ForceSeqNumReset = Fixicc.Message.ForceSeqNumReset.ON;
			}
			else
			{
				extraSessionParams.ForceSeqNumReset = Fixicc.Message.ForceSeqNumReset.OFF;
			}

			// Security
			var username = parameters.OutgoingLoginMessage.GetTagValueAsString(553);
			extraSessionParams.Username = username;
			var password = parameters.OutgoingLoginMessage.GetTagValueAsString(554);
			if (password != null)
			{
				extraSessionParams.Password = password;
			}
			return extraSessionParams;
		}

		public static SessionRole GetRole(IExtendedFixSession session)
		{
			if (session is AcceptorFixSession)
			{
				return SessionRole.ACCEPTOR;
			}
			else
			{
				return SessionRole.INITIATOR;
			}
		}

		public static long GetInSeqNum(SessionParameters parameters, FixSessionRuntimeState runtimeState)
		{
			long seqNum = -1;
			if (runtimeState != null)
			{
				//live session
				seqNum = runtimeState.InSeqNum;
			}
			else
			{
				seqNum = parameters.IncomingSequenceNumber;
			}
			return seqNum > 0 ? seqNum : 0;
		}

		public static long GetOutSeqNum(SessionParameters parameters, FixSessionRuntimeState runtimeState)
		{
			long seqNum = -1;
			if (runtimeState != null)
			{
				//live session
				seqNum = runtimeState.OutSeqNum;
			}
			else
			{
				seqNum = parameters.OutgoingSequenceNumber;
			}

			// Session parameters does not contain processed outgoing SeqNum. OutgoingSequenceNumber is next SeqNum, so we do - 1
			seqNum--;
			return seqNum > 0 ? seqNum : 0;
		}

		public static IList<string> GetSupportedVersions()
		{
			IList<string> list = new List<string>();
			// simple versions first
			var fixVersionEnum = FixVersion.FixVersionEnum;
			while (fixVersionEnum.MoveNext())
			{
				var version = fixVersionEnum.Current;
				if (!version.IsFixt && 2 <= version.FixtVersion && version.FixtVersion <= 6)
				{
					list.Add(GetVersion(version, null));
				}
			}

			// versions via transport protocol
			fixVersionEnum = FixVersion.FixVersionEnum;
			while (fixVersionEnum.MoveNext())
			{
				var version = fixVersionEnum.Current;
				if (version.IsFixt)
				{
					var fixEnumSubList = FixVersion.FixVersionEnum;
					while (fixEnumSubList.MoveNext())
					{
						var nextVersion = fixEnumSubList.Current;
						if (!nextVersion.IsFixt)
						{
							list.Add(GetVersion(version, nextVersion));
						}
					}
				}
			}

			// TODO: add custom versions
			return list;
		}

		public static string GetVersion(FixVersion fixVersion, FixVersion appVersion)
		{
			if (fixVersion.IsFixt)
			{
				return ToAdminProtocol(fixVersion) + FixversionDelimiter + ToAdminProtocol(appVersion);
			}
			else
			{
				return ToAdminProtocol(fixVersion);
			}
		}

		private static string ToAdminProtocol(FixVersion version)
		{
			return version.ToString().Replace(".", "");
		}

		public static FixVersion GetFixVersion(string version)
		{
			if (version.Contains(FixversionDelimiter))
			{
				version = version.Substring(0, version.IndexOf(FixversionDelimiter, StringComparison.Ordinal));
			}
			var fixVersion = FixVersionMapping[version];
			if (fixVersion == null)
			{
				throw new ArgumentException($"Illegal version: {version}");
			}
			return fixVersion;
		}

		public static FixVersion GetAppVersion(string version)
		{
			if (version.Contains(FixversionDelimiter))
			{
				version = version.Substring(version.IndexOf(FixversionDelimiter, StringComparison.Ordinal) + 1);
			}
			else
			{
				return null;
			}
			var fixVersion = FixVersionMapping[version];
			if (fixVersion == null)
			{
				throw new ArgumentException($"Illegal ApplVer: {version}");
			}
			return fixVersion;
		}

		public static StatusGroup GetStatusGroup(IExtendedFixSession session)
		{
			if (session.SessionState == SessionState.Connecting
					|| session.SessionState == SessionState.WaitingForLogon)
			{
				return StatusGroup.CONNECTING;
			}
			else if (session.SessionState == SessionState.Connected)
			{
				return StatusGroup.ESTABLISHED;
			}
			else if (session.SessionState == SessionState.Reconnecting)
			{
				return StatusGroup.RECONNECTING;
			}
			else
			{
				return StatusGroup.DISCONNECTED;
			}
		}

		public static BackupState IsBackupHost(IExtendedFixSession session)
		{
			if (session is IBackupFixSession)
			{
				var backupFIXSession = (IBackupFixSession) session;
				return backupFIXSession.IsRunningOnBackup ? BackupState.BACKUP : BackupState.PRIMARY;
			}
			return BackupState.PRIMARY;
		}

		protected internal static bool GetPropertyAsBollean(IExtendedFixSession session, string name, bool defaultVal)
		{
			return session.Parameters.Configuration.GetPropertyAsBoolean(name, defaultVal);
		}
	}
}
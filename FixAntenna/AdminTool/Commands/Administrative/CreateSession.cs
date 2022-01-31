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

using Epam.FixAntenna.AdminTool.Builder;
using Epam.FixAntenna.AdminTool.Commands.Util;
using Epam.FixAntenna.Fixicc.Message;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Storage;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;

using ForceSeqNumReset = Epam.FixAntenna.NetCore.FixEngine.ForceSeqNumReset;

namespace Epam.FixAntenna.AdminTool.Commands.Administrative
{
	internal abstract class CreateSession : Command
	{
		protected internal const bool Fail = false;
		protected internal const bool Success = true;

		public virtual bool FillExtraSessionParams(SessionParameters details, ExtraSessionParams extraSessionParams)
		{
			FillHbi(details, extraSessionParams);
			FillLocationIds(details, extraSessionParams);
			FillSubIds(details, extraSessionParams);
			FillStorageType(details, extraSessionParams);
			FillCreadentials(details, extraSessionParams);
			if (!FillCustomLogon(details, extraSessionParams))
			{
				return Fail;
			}

			FillSequences(details, extraSessionParams);
			FillRejectiong(details, extraSessionParams);
			FillForceSeqNumReset(details, extraSessionParams);
			FillMaxMessagesToSendInBatch(details, extraSessionParams);
			FillDisableTcpBuffer(details, extraSessionParams);
			FillSessionQualifierTag(details, extraSessionParams);

			if (!FillEncryptedMethodData(details, extraSessionParams))
			{
				return Fail;
			}
			return Success;
		}

		public virtual void FillSessionQualifierTag(SessionParameters details, ExtraSessionParams extraSessionParams)
		{
			var configuration = details.Configuration;
			var qualifierTagVal = details.SessionQualifier;
			var paramsLmqTag = extraSessionParams.LogonMessageSessionQualifierTag;
			if (!string.IsNullOrWhiteSpace(qualifierTagVal) && !string.IsNullOrWhiteSpace(paramsLmqTag))
			{
				var outgoingLoginFixMessage = details.OutgoingLoginMessage;
				//remove old qualifier tag from outgoing Logon
				var configuredQualifierTag = configuration.GetPropertyAsInt(Config.LogonMessageSessionQualifierTag);
				outgoingLoginFixMessage.RemoveTag(configuredQualifierTag);

				//set new qualifier tag and its value
				configuration.SetProperty(Config.LogonMessageSessionQualifierTag, paramsLmqTag);
				outgoingLoginFixMessage.Set(Convert.ToInt32(paramsLmqTag), qualifierTagVal);
			}
		}

		public virtual void FillMaxMessagesToSendInBatch(SessionParameters details, ExtraSessionParams extraSessionParams)
		{
			if (extraSessionParams.MaxMessagesAmountInBunch != null)
			{
				details.Configuration.SetProperty(Config.MaxMessagesToSendInBatch, extraSessionParams.MaxMessagesAmountInBunch.ToString());
			}
		}

		public virtual void FillForceSeqNumReset(SessionParameters details, ExtraSessionParams extraSessionParams)
		{
			// [Bug 15751] New: Session was not created using command "CreateInitiator"   with parameter HBI
			if (extraSessionParams.ForceSeqNumReset != null)
			{
				switch (extraSessionParams.ForceSeqNumReset)
				{
					case Fixicc.Message.ForceSeqNumReset.ALWAYS:
						details.ForceSeqNumReset = ForceSeqNumReset.Always;
						break;
					case Fixicc.Message.ForceSeqNumReset.ON:
						details.ForceSeqNumReset = ForceSeqNumReset.OneTime;
						break;
					case Fixicc.Message.ForceSeqNumReset.OFF:
						details.ForceSeqNumReset = ForceSeqNumReset.Never;
						break;
				}
			}
		}

		public virtual void FillRejectiong(SessionParameters details, ExtraSessionParams extraSessionParams)
		{
			// enable message rejecting
			if (extraSessionParams.EnableMessageRejecting != null)
			{
				details.Configuration.SetProperty(Config.EnableMessageRejecting, extraSessionParams.EnableMessageRejecting.ToString());
			}
		}

		public virtual void FillSequences(SessionParameters details, ExtraSessionParams extraSessionParams)
		{
			if (extraSessionParams.InSeqNum.HasValue)
			{
				// in details contains next seqNum
				details.IncomingSequenceNumber = Math.Max(0, extraSessionParams.InSeqNum.Value);
			}
			if (extraSessionParams.OutSeqNum.HasValue)
			{
				// in details contains next seqNum
				details.OutgoingSequenceNumber = Math.Max(0, extraSessionParams.OutSeqNum.Value);
			}
		}

		public virtual bool FillCustomLogon(SessionParameters details, ExtraSessionParams extraSessionParams)
		{
			// set custom tags
			if (IsNotEmpty(extraSessionParams.CustomLogon))
			{
				try
				{
					var customMessage = GetCustomMessage(extraSessionParams);
					if (!FixMessageUtil.IsLogon(customMessage))
					{
						Log.Error($"Not logon message, msg. type-{StringHelper.NewString(customMessage.MsgType)}");
						SendInvalidArgument("Message type must be 'A'");
						return Fail;
					}
					var customFields = CustomTagsBuilder.GetInstance().GetCustomTags(details, customMessage);
					details.OutgoingLoginMessage.AddAll(customFields);
				}
				catch (Exception e)
				{
					Log.Error($"error on parse custom logon, [{StringHelper.NewString(extraSessionParams.CustomLogon)}]", e);
					SendError($"Error on parse /{StringHelper.NewString(extraSessionParams.CustomLogon)}/");
					return Fail;
				}
			}
			return Success;
		}

		public virtual void FillCreadentials(SessionParameters details, ExtraSessionParams extraSessionParams)
		{
			// set user and password
			if (!string.IsNullOrWhiteSpace(extraSessionParams.Username))
			{
				details.OutgoingLoginMessage.AddTag(553, extraSessionParams.Username);
			}
			if (!string.IsNullOrWhiteSpace(extraSessionParams.Password))
			{
				details.OutgoingLoginMessage.AddTag(554, extraSessionParams.Password);
			}
		}

		public virtual void FillStorageType(SessionParameters details, ExtraSessionParams extraSessionParams)
		{
			// storage type
			var configuration = details.Configuration;
			var storageFactory = typeof(InMemoryStorageFactory).FullName;
			if (extraSessionParams.StorageType.HasValue)
			{
				switch (extraSessionParams.StorageType)
				{
					case StorageType.PERSISTENT:
						storageFactory = typeof(FilesystemStorageFactory).FullName;
						break;
					case StorageType.PERSISTENTMM:
						storageFactory = typeof(MmfStorageFactory).FullName;
						break;
					case StorageType.SPLITPERSISTENT:
						storageFactory = typeof(SlicedFileStorageFactory).FullName;
						break;
				}
			}
			configuration.SetProperty(Config.StorageFactory, storageFactory);
		}


		public virtual void FillSubIds(SessionParameters details, ExtraSessionParams extraSessionParams)
		{
			if (!string.IsNullOrWhiteSpace(extraSessionParams.SenderSubID))
			{
				details.SenderSubId = extraSessionParams.SenderSubID;
			}
			if (!string.IsNullOrWhiteSpace(extraSessionParams.TargetSubID))
			{
				details.TargetSubId = extraSessionParams.TargetSubID;
			}
		}

		public virtual void FillLocationIds(SessionParameters details, ExtraSessionParams extraSessionParams)
		{
			if (!string.IsNullOrWhiteSpace(extraSessionParams.SenderLocationID))
			{
				details.SenderLocationId = extraSessionParams.SenderLocationID;
			}
			if (!string.IsNullOrWhiteSpace(extraSessionParams.TargetLocationID))
			{
				details.TargetLocationId = extraSessionParams.TargetLocationID;
			}
		}

		public virtual void FillHbi(SessionParameters details, ExtraSessionParams extraSessionParams)
		{
			if (extraSessionParams.HBI.HasValue)
			{
				details.HeartbeatInterval = extraSessionParams.HBI.Value;
			}
		}

		public virtual bool FillFixVersion(Fixicc.Message.CreateInitiator createInitiatorRequest, SessionParameters details)
		{
			return FillFixVersion(createInitiatorRequest.Version, details);
		}

		public virtual bool FillFixVersion(Fixicc.Message.CreateAcceptor createAcceptorRequest, SessionParameters details)
		{
			return FillFixVersion(createAcceptorRequest.Version, details);
		}

		public virtual void FillDisableTcpBuffer(SessionParameters details, ExtraSessionParams extraSessionParams)
		{
			if (extraSessionParams.DisableTCPBuffer != null)
			{
				// DisableTCPBuffer == !enableNagle
				details.Configuration.SetProperty(Config.EnableNagle, Convert.ToString(!extraSessionParams.DisableTCPBuffer));
			}
		}

		public virtual bool FillFixVersion(string version, SessionParameters details)
		{
			if (version == null)
			{
				SendInvalidArgument("Parameter Version is required");
				return Fail;
			}

			var appFixVersion = CommandUtil.GetAppVersion(version);
			if (appFixVersion != null)
			{
				details.AppVersion = appFixVersion;
			}

			var fixVersion = CommandUtil.GetFixVersion(version);
			details.FixVersion = fixVersion;
			return Success;
		}

		public virtual bool FillSenderTargetIds(Fixicc.Message.CreateInitiator createInitiatorRequest, SessionParameters details)
		{
			return FillSenderTargetIds(createInitiatorRequest.SenderCompID, createInitiatorRequest.TargetCompID, createInitiatorRequest.SessionQualifier, details);
		}

		public virtual bool FillSenderTargetIds(Fixicc.Message.CreateAcceptor createAcceptorRequest, SessionParameters details)
		{
			return FillSenderTargetIds(createAcceptorRequest.SenderCompID, createAcceptorRequest.TargetCompID, createAcceptorRequest.SessionQualifier, details);
		}

		public virtual bool FillSenderTargetIds(string senderCompId, string targetCompId, string sessionQualifier, SessionParameters details)
		{

			if (string.IsNullOrWhiteSpace(senderCompId))
			{
				SendInvalidArgument("Parameter SenderCompID is required");
				return Fail;
			}
			if (string.IsNullOrWhiteSpace(targetCompId))
			{
				SendInvalidArgument("Parameter TargetCompID is required");
				return Fail;
			}

			details.SenderCompId = senderCompId;
			details.TargetCompId = targetCompId;
			details.SessionQualifier = sessionQualifier;
			return Success;
		}

		public virtual bool FillEncryptedMethodData(SessionParameters details, ExtraSessionParams extraSessionParams)
		{
			if (extraSessionParams.EncryptMethod != null)
			{
				switch (extraSessionParams.EncryptMethod)
				{
					case EncryptMethod.NONE:
						break;
					case EncryptMethod.DES:
					case EncryptMethod.PGP_DES_MD5:
					case EncryptMethod.PKCS_DES:
					case EncryptMethod.PGP_DES:
					case EncryptMethod.PEM_DES_MD5:
					case EncryptMethod.PKCS:
						Log.Warn(
							$"Can't create required session: defined unsupported encryption method {extraSessionParams.EncryptMethod}");
						SendInvalidArgument(
							$"Unsupported encryption method for session: {extraSessionParams.EncryptMethod}. Please choose NONE");
						return Fail;
				}
			}
			return Success;
		}

		/// <summary>
		/// construct custom login message
		/// </summary>
		public virtual FixMessage GetCustomMessage(ExtraSessionParams extraSessionParams)
		{
			var msgCustom = StringHelper.NewString(extraSessionParams.CustomLogon).Replace('?', '\u0001');
			msgCustom = msgCustom.Replace('#', '\u0001');
			return RawFixUtil.GetFixMessage(msgCustom.AsByteArray());
		}

		private bool IsNotEmpty(byte[] value)
		{
			return value != null && value.Length > 0;
		}
	}
}
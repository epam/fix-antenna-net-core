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
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine
{
	internal class FixSessionBuilder
	{
		protected internal const string FixSessionPrefix = "fixengine.session";

		protected internal const string FixSessionFixVersionSuffix = ".fixVersion";
		protected internal const string FixSessionHostSuffix = ".host";
		protected internal const string FixSessionPortSuffix = ".port";
		protected internal const string FixSessionSenderCompIdSuffix = ".SenderCompID";
		protected internal const string FixSessionTargetCompIdSuffix = ".TargetCompID";
		// sub id
		protected internal const string FixSessionSenderSubIdSuffix = ".SenderSubId";
		protected internal const string FixSessionTargetSubIdSuffix = ".TargetSubId";
		// location id
		protected internal const string FixSessionSenderLocationIdSuffix = ".SenderLocationId";
		protected internal const string FixSessionTargetLocationIdSuffix = ".TargetLocationId";

		protected internal const string FixSessionIncomingSequenceNumberSuffix = ".IncomingSequenceNumber";
		protected internal const string FixSessionOutgoingSequenceNumberSuffix = ".OutgoingSequenceNumber";

		protected internal const string FixSessionHeartbeatIntervalSuffix = ".HeartbeatInterval";

		protected internal const string FixSessionOutgoingLoginFieldsSuffix = ".OutgoingLoginFields";


		public virtual IFixSession CreateFixSession(string sessionPropertiesId, Properties fixJmsProp)
		{
			var details = new SessionParameters();
			var fixVersionPValue = fixJmsProp.GetNotEmptyProperty(FixSessionPrefix + "." + sessionPropertiesId + FixSessionFixVersionSuffix);
			var fixVersionUtils = new FixVersionUtils(fixVersionPValue);
			var fixtVer = fixVersionUtils.GetFixtVersion();
			if (fixtVer != null)
			{
				details.FixVersion = fixtVer;
				details.AppVersion = fixVersionUtils.GetFixVersion();
			}
			else
			{
				details.FixVersion = fixVersionUtils.GetFixVersion();
			}
			details.Host = fixJmsProp.GetNotEmptyProperty(FixSessionPrefix + "." + sessionPropertiesId + FixSessionHostSuffix);
			details.Port = fixJmsProp.GetNotEmptyIntegerProperty(FixSessionPrefix + "." + sessionPropertiesId + FixSessionPortSuffix);
			details.SenderCompId = fixJmsProp.GetNotEmptyProperty(FixSessionPrefix + "." + sessionPropertiesId + FixSessionSenderCompIdSuffix);
			details.TargetCompId = fixJmsProp.GetNotEmptyProperty(FixSessionPrefix + "." + sessionPropertiesId + FixSessionTargetCompIdSuffix);
			// sub id
			details.SenderSubId = fixJmsProp.GetProperty(FixSessionPrefix + "." + sessionPropertiesId + FixSessionSenderSubIdSuffix);
			details.TargetSubId = fixJmsProp.GetProperty(FixSessionPrefix + "." + sessionPropertiesId + FixSessionTargetSubIdSuffix);
			// location id
			details.SenderLocationId = fixJmsProp.GetProperty(FixSessionPrefix + "." + sessionPropertiesId + FixSessionSenderLocationIdSuffix);
			details.TargetLocationId = fixJmsProp.GetProperty(FixSessionPrefix + "." + sessionPropertiesId + FixSessionTargetLocationIdSuffix);

			// incoming and outgoing sequence number
			var inSeq = fixJmsProp.GetProperty(FixSessionPrefix + "." + sessionPropertiesId + FixSessionIncomingSequenceNumberSuffix);
			var outSeq = fixJmsProp.GetProperty(FixSessionPrefix + "." + sessionPropertiesId + FixSessionOutgoingSequenceNumberSuffix);
			if (!string.IsNullOrWhiteSpace(inSeq))
			{
				try
				{
					details.IncomingSequenceNumber = int.Parse(inSeq);
				}
				catch (Exception e)
				{
					throw new System.ArgumentException("invalid value: " + inSeq + ", for: " + FixSessionPrefix + "." + sessionPropertiesId + FixSessionIncomingSequenceNumberSuffix, e);
				}
			}
			if (!string.IsNullOrWhiteSpace(outSeq))
			{
				try
				{
					details.OutgoingSequenceNumber = int.Parse(outSeq);
				}
				catch (Exception e)
				{
					throw new System.ArgumentException("invalid value: " + outSeq + ", for: " + FixSessionPrefix + "." + sessionPropertiesId + FixSessionOutgoingSequenceNumberSuffix, e);
				}
			}

			// heartbeat interval
			var interval = fixJmsProp.GetProperty(FixSessionPrefix + "." + sessionPropertiesId + FixSessionHeartbeatIntervalSuffix);
			if (!string.IsNullOrWhiteSpace(interval))
			{
				try
				{
					details.HeartbeatInterval = int.Parse(interval);
				}
				catch (Exception e)
				{
					throw new System.ArgumentException("invalid value: " + interval + ", for: " + FixSessionPrefix + "." + sessionPropertiesId + FixSessionHeartbeatIntervalSuffix, e);
				}
			}

			//additional outgoing login fields
			var outgoingFields = fixJmsProp.GetProperty(FixSessionPrefix + "." + sessionPropertiesId + FixSessionOutgoingLoginFieldsSuffix);
			if (!string.IsNullOrWhiteSpace(outgoingFields))
			{
				try
				{
					var outMessage = RawFixUtil.GetFixMessage(outgoingFields.AsByteArray());
					details.OutgoingLoginMessage = outMessage;
				}
				catch (Exception e)
				{
					throw new System.ArgumentException("invalid value: " + outgoingFields + ", for: " + FixSessionPrefix + "." + sessionPropertiesId + FixSessionOutgoingLoginFieldsSuffix, e);
				}
			}

			return details.CreateNewFixSession();
		}
	}
}
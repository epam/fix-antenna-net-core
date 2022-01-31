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

using Epam.FixAntenna.AdminTool.Builder.Util;
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.AdminTool.Builder
{
	/// <summary>
	/// Provides ability to build logon message
	///  from custom message.
	/// </summary>
	internal class CustomTagsBuilder
	{
		private static CustomTagsBuilder _instance = new CustomTagsBuilder();

		private CustomTagsBuilder()
		{
		}

		public static CustomTagsBuilder GetInstance()
		{
			return _instance;
		}

		/// <summary>
		/// get custom tags
		/// </summary>
		public virtual FixMessage GetCustomTags(SessionParameters sessionParameters, FixMessage customMessage)
		{
			// user
			if (!string.IsNullOrWhiteSpace(customMessage.GetTagValueAsString(553)))
			{
				TagUtil.RemoveTag(553, sessionParameters.OutgoingLoginMessage);
				sessionParameters.OutgoingLoginMessage.AddTag(553, customMessage.GetTagValueAsString(553));
			}
			// password
			if (!string.IsNullOrWhiteSpace(customMessage.GetTagValueAsString(554)))
			{
				TagUtil.RemoveTag(554, sessionParameters.OutgoingLoginMessage);
				sessionParameters.OutgoingLoginMessage.AddTag(554, customMessage.GetTagValueAsString(554));
			}
			// sender sub id
			if (string.IsNullOrWhiteSpace(sessionParameters.SenderSubId))
			{
				sessionParameters.SenderSubId = customMessage.GetTagValueAsString(Tags.SenderSubID);
			}
			// target sub id
			if (string.IsNullOrWhiteSpace(sessionParameters.TargetSubId))
			{
				sessionParameters.TargetSubId = customMessage.GetTagValueAsString(Tags.TargetSubID);
			}
			// sender location id
			if (string.IsNullOrWhiteSpace(sessionParameters.SenderLocationId))
			{
				sessionParameters.SenderLocationId = customMessage.GetTagValueAsString(Tags.SenderLocationID);
			}
			// target location id
			if (string.IsNullOrWhiteSpace(sessionParameters.TargetLocationId))
			{
				sessionParameters.TargetLocationId = customMessage.GetTagValueAsString(Tags.TargetLocationID);
			}
			// sender comp id
			if (string.IsNullOrWhiteSpace(sessionParameters.SenderCompId))
			{
				sessionParameters.SenderCompId = customMessage.GetTagValueAsString(Tags.SenderCompID);
			}
			// target comp id
			if (string.IsNullOrWhiteSpace(sessionParameters.TargetCompId))
			{
				sessionParameters.TargetCompId = customMessage.GetTagValueAsString(Tags.TargetCompID);
			}
			// remove tags
			TagUtil.RemoveTags(
				new int[]
				{
					Tags.SenderCompID,
					Tags.Username,
					Tags.Password,
					Tags.HeartBtInt,
					Tags.TargetCompID,
					Tags.SenderLocationID,
					Tags.TargetLocationID,
					Tags.TargetSubID,
					Tags.SenderSubID,
					Tags.BeginString,
					Tags.BodyLength,
					Tags.MsgType,
					Tags.MsgSeqNum,
					Tags.SendingTime,
					Tags.CheckSum

				}, customMessage);

			return customMessage;
		}
	}
}
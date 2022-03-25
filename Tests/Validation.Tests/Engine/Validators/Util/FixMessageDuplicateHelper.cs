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

using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation.Utils;

namespace Epam.FixAntenna.Validation.Tests.Engine.Validators.Util
{
	internal sealed class FixMessageDuplicateHelper
	{
		public static FixMessage GetMessage(FixVersion fixVersion, string msgType)
		{
			var message = new FixMessage();
			var fixVersionContainer = FixVersionContainer.GetFixVersionContainer(fixVersion);
			var fixUtil = FixUtilFactory.Instance.GetFixUtil(fixVersionContainer);

			var fielddefs = fixUtil.GetFieldsByMessageType(msgType);
			foreach (var fielddef in fielddefs)
			{
				message.AddTag(fielddef.Tag, fielddef.Tag);
			}

			UpdateMsgTypeAndFixVersionFields(fixVersion, msgType, message);
			return message;
		}

		private static void UpdateMsgTypeAndFixVersionFields(FixVersion fixVersion, string msgType,
			FixMessage message)
		{
			if (message.IsTagExists(8))
			{
				message.Set(8, fixVersion.MessageVersion);
			}

			if (message.IsTagExists(35))
			{
				message.Set(35, msgType);
			}

			message.Set(9, message.CalculateBodyLength());
			message.Set(10, FixTypes.FormatCheckSum(message.CalculateChecksum()));
		}

		public static FixMessage GetMessageWithDuplicateFields(FixVersion fixVersion, string msgType,
			params int[] dupTags)
		{
			return GetMessageWithDuplicateFields(GetMessage(fixVersion, msgType), dupTags);
		}

		public static FixMessage GetMessageWithDuplicateFields(FixMessage message, params int[] dupTags)
		{
			for (var i = 0; i < message.Count; i++)
			{
				foreach (var tag in dupTags)
				{
					if (message[i].TagId == tag)
					{
						var field = message[i];
						message.AddAtIndex(i, field);
						++i;
					}
				}
			}

			return message;
		}
	}
}
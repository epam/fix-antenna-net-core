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
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global
{
	/// <summary>
	/// The garbled message handler.
	/// </summary>
	internal class GarbledMessageHandler : AbstractGlobalMessageHandler
	{
		protected internal new static readonly ILog Log = LogFactory.GetLog(typeof(GarbledMessageHandler));
		private static readonly string Reason = "MsgType (tag " + Tags.MsgType + ") is missing";
		private const string FirstThreeTagsInvalid = "First three tags are invalid or in a wrong order";

		private static readonly int[] PrefixMessageTags = new int[] { Tags.BeginString, Tags.BodyLength, Tags.MsgType };

		/// <summary>
		/// The next handler calls only:
		/// 1. if message starts with 8 tag; <br/>
		/// 2. if the second tag is 9. <br/>
		/// 3. if the third tag is 35 <br/>
		/// </summary>
		/// <seealso cref="IFixMessageListener.OnNewMessage"> </seealso>
		public override void OnNewMessage(FixMessage message)
		{
			HandleMessage(message);
		}

		private void HandleMessage(FixMessage message)
		{
			if (message.Length < 3 || IsInvalidPrefixOfMessage(message))
			{
				//logWarnToSession(FIRST_THREE_TAGS_INVALID, garbledMessageException);
				throw new GarbledMessageException(FirstThreeTagsInvalid, message.ToPrintableString());
			}

			// If the MsgSeqNum(tag #34) is missing a logout message should be sent terminating the FIX Connection, as this
			// indicates a serious application error that is likely only circumvented by software modification.
			if (!HasValidMsgSeqNum(message))
			{
				// garbled message detected
				Session.Disconnect("MsgSeqNum is missing or empty");
				throw new GarbledMessageException("MsgSeqNum(tag #34) is missing or empty", message.ToPrintableString(), true);
			}
			CallNextHandler(message);
		}

		private static bool HasValidMsgSeqNum(FixMessage message)
		{
			try
			{
				return message.IsTagExists(Tags.MsgSeqNum) && message.GetTagValueAsLong(Tags.MsgSeqNum) >= 0;
			}
			catch (Exception)
			{
				return false;
			}
		}

		private static bool IsInvalidPrefixOfMessage(FixMessage message)
		{
			for (var i = 0; i < PrefixMessageTags.Length; i++)
			{
				if (!message.IsTagExists(PrefixMessageTags[i]) || message.GetTagIndex(PrefixMessageTags[i]) != i)
				{
					return true;
				}
			}
			return false;
		}
	}
}
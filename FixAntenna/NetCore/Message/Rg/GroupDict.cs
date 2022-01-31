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

using System.Collections.Generic;
using Epam.FixAntenna.NetCore.Validation.Entities;

namespace Epam.FixAntenna.NetCore.Message.Rg
{
	internal class GroupDict
	{
		private readonly Dictionary<string, MessageWithGroupDict> _msgTypeToMessageDict;

		public GroupDict(Fixdic fixdic)
		{
			IList<Msgdef> messages = fixdic.Msgdic.Msgdef;
			IList<Blockdef> blocks = fixdic.Msgdic.Blockdef;
			var initialSize = messages.Count;
			_msgTypeToMessageDict = new Dictionary<string, MessageWithGroupDict>(initialSize);

			var blockToTags = new Dictionary<string, IList<object>>(blocks.Count);

			foreach (var block in blocks)
			{
				blockToTags[block.Id] = block.FieldOrDescrOrGroup;
			}

			foreach (var message in messages)
			{
				IList<object> messageContent = message.FieldOrDescrOrAlias;
				var messageWithGroupDict = new MessageWithGroupDict(blockToTags);
				messageWithGroupDict.ParseMessage(messageContent);

				if (messageWithGroupDict.GetOuterLeadingTags().Count > 0)
				{
					messageWithGroupDict.CreateArraysData();
					_msgTypeToMessageDict[message.Msgtype] = messageWithGroupDict;
				}
			}
		}

		public virtual MessageWithGroupDict GetMessageDict(string msgType)
		{
			_msgTypeToMessageDict.TryGetValue(msgType, out var msgWithGroupDict);
			return msgWithGroupDict;
		}
	}
}
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
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation.Utils;
using Epam.FixAntenna.NetCore.Validation.Utils.Cache;

namespace Epam.FixAntenna.NetCore.Validation.FixMessage.Tree
{
	internal class FixMessageTreeUtil
	{
		public static FixEntry BuildMessageTree(Message.FixMessage message, FixUtil fixUtil)
		{
			var treeMsg = new FixEntry();
			TagValue field;
			int tagId;
			var groupsCache = fixUtil.GetGroupsCache(StringHelper.NewString(message.MsgType));
			var groups = groupsCache.GetGroupsCaches();
			var index = 0;
			var toIndex = message.Length;
			while (index < toIndex)
			{
				field = message[index];
				tagId = field.TagId;
				if (groups.ContainsKey(tagId))
				{
					var rg = new FixRepeatingGroup(tagId);
					index = FillRg(rg, index, toIndex, message, groupsCache);
					treeMsg.RepeatingGroups.Add(rg);
				}
				else
				{
					treeMsg.Fields.Add(field);
					index++;
				}
			}

			return treeMsg;
		}

		public static FixRepeatingGroup BuildRg(int rgTagId, Message.FixMessage message, FixUtil fixUtil)
		{
			return BuildRg(rgTagId, 0, message.Length, message, fixUtil);
		}

		public static FixRepeatingGroup BuildRg(int rgTagId, int fromIndex, int toIndex, Message.FixMessage message,
			FixUtil fixUtil)
		{
			var rg = new FixRepeatingGroup(rgTagId);
			FillRg(rg, fromIndex, toIndex, message,
				fixUtil.GetGroupsCache(StringHelper.NewString(message.MsgType)));
			return rg;
		}

		private static int FillRgEntry(FixEntry entry, int leadingTagId, int fromIndex, int toIndex,
			Message.FixMessage message, ICollection<int> entryFields, GroupsCache groupsCache)
		{
			var groups = groupsCache.GetGroupsCaches();
			TagValue field;
			int tagId;
			var index = fromIndex;
			var leadTagWasRead = false;
			while (index < toIndex)
			{
				field = message[index];
				tagId = field.TagId;
				if (tagId == leadingTagId)
				{
					if (!leadTagWasRead)
					{
						leadTagWasRead = true;
					}
					else
					{
						// one RG entry must contain only one leadingTagId
						break;
					}
				}

				if (entryFields.Contains(tagId))
				{
					if (groups.ContainsKey(tagId))
					{
						var rg = new FixRepeatingGroup(tagId);
						index = FillRg(rg, index, toIndex, message, groupsCache);
						entry.RepeatingGroups.Add(rg);
					}
					else
					{
						entry.Fields.Add(field);
						index++;
					}
				}
				else
				{
					// end of RG
					break;
				}
			}

			return index;
		}

		private static int FillRg(FixRepeatingGroup rg, int fromIndex, int toIndex, Message.FixMessage message,
			GroupsCache groupsCache)
		{
			var entryFields = new HashSet<int>(groupsCache.GetGroupsCaches()[rg.TagId].GetCache().Keys);
			foreach (var block in groupsCache.GetGroupBlocksCache(rg.TagId))
			{
				entryFields.AddRange(block.GetCache().Keys);
			}

			var leadingTagId = groupsCache.GetGroupCache(rg.TagId).GetStartTagId();
			TagValue field;
			int tagId;
			var index = fromIndex;
			// start index not always is start RG. We read message to end of RG or end of message.
			var isFound = false;
			while (index < toIndex)
			{
				field = message[index];
				tagId = field.TagId;
				if (entryFields.Contains(tagId))
				{
					if (!isFound)
					{
						isFound = true;
					}

					var rgEntry = new FixEntry();
					index = FillRgEntry(rgEntry, leadingTagId, index, toIndex, message, entryFields, groupsCache);
					rg.Entries.Add(rgEntry);
				}
				else
				{
					if (isFound)
					{
						break;
					}

					index++;
				}
			}

			return index;
		}
	}
}
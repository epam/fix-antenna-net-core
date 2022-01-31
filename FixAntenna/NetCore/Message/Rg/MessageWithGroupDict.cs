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
using Epam.FixAntenna.NetCore.Validation.Entities;

namespace Epam.FixAntenna.NetCore.Message.Rg
{
	internal class MessageWithGroupDict
	{
		private readonly HashSet<int> _allGroupTags;
		private readonly Dictionary<string, IList<object>> _blockToTags;
		private readonly Dictionary<int, int> _leadingToDelimiter;
		private readonly Dictionary<int, HashSet<int>> _leadingToTags;
		private readonly HashSet<int> _nestedLeadingTags;
		private readonly HashSet<int> _outerLeadingTags;
		private int[] _nestedLeadingTagsArray;
		private int[] _outerLeadingTagsArray;

		public MessageWithGroupDict(Dictionary<string, IList<object>> blockToTags)
		{
			_blockToTags = blockToTags;
			_outerLeadingTags = new HashSet<int>();
			_nestedLeadingTags = new HashSet<int>();
			_leadingToDelimiter = new Dictionary<int, int>();
			_leadingToTags = new Dictionary<int, HashSet<int>>();
			_allGroupTags = new HashSet<int>();
		}

		internal virtual void ParseMessage(IList<object> messageContent)
		{
			foreach (var obj in messageContent)
			{
				if (obj is Group)
				{
					ParseGroup((Group)obj, false);
				}
				else if (obj is Block)
				{
					var block = (Block)obj;
					ParseMessage(_blockToTags[block.Idref]);
				}
			}
		}

		internal virtual void ParseGroup(Group group, bool isNested)
		{
			var leadingTag = @group.Nofield;

			_leadingToDelimiter.Add(leadingTag, @group.Startfield);
			if (isNested)
			{
				_nestedLeadingTags.Add(leadingTag);
			}
			else
			{
				_outerLeadingTags.Add(leadingTag);
			}

			var tags = new HashSet<int>();

			foreach (var content in @group.Content)
			{
				switch (content)
				{
					case Field field:
						tags.Add(field.Tag);
						break;
					case Block block:
						ParseBlock(tags, block);
						break;
					case Group group1:
						ParseGroup(group1, true);
						break;
				}
			}

			_allGroupTags.UnionWith(tags);
			_leadingToTags.Add(@group.Nofield, tags);
		}

		private void ParseBlock(HashSet<int> tags, Block content)
		{
			var blockContent = _blockToTags[content.Idref];
			foreach (var o in blockContent)
			{
				switch (o)
				{
					case Field field:
					{
						tags.Add(field.Tag);
						break;
					}
					case Group groupInBlock:
					{
						tags.Add(groupInBlock.Nofield);
						ParseGroup(groupInBlock, true);
						break;
					}
					case Block block:
						ParseBlock(tags, block);
						break;
					default:
						throw new Exception(
							"Expects only field or group during parse block in group but got " + o.GetType());
				}
			}
		}

		public virtual HashSet<int> GetNestedLeadingTags()
		{
			return _nestedLeadingTags;
		}

		public virtual HashSet<int> GetOuterLeadingTags()
		{
			return _outerLeadingTags;
		}

		public virtual HashSet<int> GetGroupTags(int leadingTag)
		{
			_leadingToTags.TryGetValue(leadingTag, out var groupTag);
			return groupTag;
		}

		public virtual int GetDelimiterTag(int leadingTag)
		{
			return _leadingToDelimiter[leadingTag];
		}

		public virtual HashSet<int> GetAllGroupTags()
		{
			return _allGroupTags;
		}

		public virtual void CreateArraysData()
		{
			_outerLeadingTagsArray = new int[_outerLeadingTags.Count];
			_outerLeadingTags.CopyTo(_outerLeadingTagsArray);
			_nestedLeadingTagsArray = new int[_nestedLeadingTags.Count];
			_nestedLeadingTags.CopyTo(_nestedLeadingTagsArray);
		}

		public virtual int[] GetOuterLeadingTagsArray()
		{
			return _outerLeadingTagsArray;
		}

		public virtual int[] GetNestedLeadingTagsArray()
		{
			return _nestedLeadingTagsArray;
		}
	}
}
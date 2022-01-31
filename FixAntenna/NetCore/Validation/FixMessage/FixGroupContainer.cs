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
using System.Linq;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation.Utils;
using Epam.FixAntenna.NetCore.Validation.Validators.Condition;
using Epam.FixAntenna.NetCore.Validation.Validators.Condition.Container;

namespace Epam.FixAntenna.NetCore.Validation.FixMessage
{
	internal class FixGroupContainer
	{
		private IDictionary<int, ICondition> _conditionMap;
		private IList<Message.FixMessage> _fixMessages;
		private ValidationFixMessage _fixMessage;
		private ISet<int> _groupConditionalTags;
		private int _groupsCount;
		private int _groupsCountTag;
		private int _groupStartTag;
		private readonly FixUtil _util;

		private FixGroupContainer(IValidationFixMessage message, FixUtil util)
		{
			_fixMessage = (ValidationFixMessage)message;
			_util = util;
		}

		public virtual IDictionary<int, ICondition> GetConditionMap()
		{
			return _conditionMap;
		}

		public virtual ISet<int> GetGroupConditionalTags()
		{
			return _groupConditionalTags;
		}

		public virtual int GetGroupsCountTag()
		{
			return _groupsCountTag;
		}

		public virtual IList<Message.FixMessage> GetFixMessages()
		{
			return _fixMessages;
		}

		public virtual int GetGroupsCount()
		{
			return _groupsCount;
		}

		public virtual int GetGroupStartTag()
		{
			return _groupStartTag;
		}

		public virtual FixGroupContainer CreateContainer(ConditionalGroup group)
		{
			_conditionMap = group.GetConditionMap();
			_groupConditionalTags = BuildSetOfGroupConditionalTags(group, _conditionMap);
			_groupsCountTag = group.GetRootTag();
			return CreateContainer(_groupsCountTag);
		}

		public virtual FixGroupContainer CreateContainer(int groupsCountTag)
		{
			_groupsCountTag = groupsCountTag;
			var tag = _fixMessage.FullFixMessage.GetTag(groupsCountTag);
			var maps = _fixMessage.GetFixFieldByGroupIndt(groupsCountTag);
			_fixMessages = new List<Message.FixMessage>();
			if (tag == null)
			{
				foreach (var fixMessageMap in maps)
				{
					_fixMessages.Add(fixMessageMap.FixMessage);
				}
			}
			else
			{
				CreateListOfSeparatedGroups(maps);
			}

			return this;
		}

		private void CreateListOfSeparatedGroups(IList<FixMessageMap> fixMessageMaps)
		{
			foreach (var fixMessageMap in fixMessageMaps)
			{
				var message = fixMessageMap.FixMessage;
				CreateSeparateList(_fixMessage, message, _groupsCountTag);
			}
		}

		public virtual FixGroupContainer CreateSeparateList(ValidationFixMessage validationMessage, Message.FixMessage fixMessage,
			int groupsCountTag)
		{
			long groupsCountTemp;
			_fixMessage = validationMessage;
			_groupsCountTag = groupsCountTag;
			if (_fixMessages == null)
			{
				_fixMessages = new List<Message.FixMessage>();
			}

			groupsCountTemp = fixMessage.GetTagValueAsLong(groupsCountTag);
			_groupsCount += (int)groupsCountTemp;

			var fields = new TagValue[fixMessage.Length];
			for (var i = 0; i < fixMessage.Length; i++)
			{
				fields[i] = fixMessage[i];
			}

			_groupStartTag = _util.GetStartTagForGroup(validationMessage.GetMsgType(), groupsCountTag);

			if (groupsCountTemp == 0 && !fixMessage.IsEmpty)
			{
				_fixMessages.Add(fixMessage);
			}

			var message = new Message.FixMessage();
			for (int index = 0, fixFieldsLength = fields.Length; index < fixFieldsLength; index++)
			{
				var field = fields[index];
				if (field.TagId == groupsCountTag)
				{
					if (index == fields.Length - 1)
					{
						var writeUtil = new WriteUtil(this, fixMessage.GetTag(groupsCountTag), groupsCountTemp,
							message).Write();
						groupsCountTemp = writeUtil.GroupsCountTemp;
					}

					continue;
				}

				// TBD: check if we can use "field.equals(fixMessage.GetTag(field.TagId)"
				if (field.TagId == _groupStartTag && !message.IsEmpty ||
					field.Equals(message.GetTag(field.TagId)))
				{
					var writeUtil = new WriteUtil(this, fixMessage.GetTag(groupsCountTag), groupsCountTemp, message)
						.Write();
					groupsCountTemp = writeUtil.GroupsCountTemp;
					message = writeUtil.FixMessage;
					// added last group to the list.
					if (groupsCountTemp - 1 == 0)
					{
						CreateLastGroup(fixMessage.GetTag(groupsCountTag), groupsCountTemp, fields, index,
							fixFieldsLength);
						break;
					}
				}

				message.Add(field);
				if (index == fields.Length - 1 && !message.IsEmpty)
				{
					var writeUtil = new WriteUtil(this, fixMessage.GetTag(groupsCountTag), groupsCountTemp, message)
						.Write();
					groupsCountTemp = writeUtil.GroupsCountTemp;
					message = writeUtil.FixMessage;
				}
			}

			return this;
		}

		private long CreateLastGroup(TagValue tagValue, long groupsCountTemp, TagValue[] fixFields, int index,
			int fixFieldsLength)
		{
			Message.FixMessage fixMessage;
			WriteUtil writeUtil;
			var lastGroupLength = fixFieldsLength - index;
			var lastGroup = new TagValue[lastGroupLength];
			Array.Copy(fixFields, index, lastGroup, 0, lastGroupLength);
			fixMessage = new Message.FixMessage();
			fixMessage.AddAll(lastGroup.ToList());
			writeUtil = new WriteUtil(this, tagValue, groupsCountTemp, fixMessage).Write();
			groupsCountTemp = writeUtil.GroupsCountTemp;
			return groupsCountTemp;
		}

		private ISet<int> BuildSetOfGroupConditionalTags(ConditionalGroup group, IDictionary<int, ICondition> conditionMap)
		{
			var groupConditionalTags = new HashSet<int>();
			groupConditionalTags.UnionWith(conditionMap.Keys);
			groupConditionalTags.Add(group.GetRootTag());
			return groupConditionalTags;
		}

		public static FixGroupContainer CreateFixGroupContainer(IValidationFixMessage message, FixUtil util)
		{
			return new FixGroupContainer(message, util);
		}

		private class WriteUtil
		{
			private readonly FixGroupContainer _container;
			private readonly TagValue _tagValue;

			public WriteUtil(FixGroupContainer container, TagValue tagValue, long groupsCountTemp, Message.FixMessage fixMessage)
			{
				_container = container;
				_tagValue = tagValue;
				GroupsCountTemp = groupsCountTemp;
				FixMessage = fixMessage;
			}

			public long GroupsCountTemp { get; private set; }

			public Message.FixMessage FixMessage { get; private set; }

			public WriteUtil Write()
			{
				WriteData(_container._fixMessages, FixMessage, _tagValue);
				--GroupsCountTemp;

				// create new instance of FixMessage.
				FixMessage = new Message.FixMessage();
				return this;
			}

			private void WriteData(IList<Message.FixMessage> messages, Message.FixMessage fixMessage, TagValue tagValue)
			{
				// added group length
				fixMessage.AddTagAtIndex(0, tagValue);
				messages.Add(fixMessage);
			}
		}
	}
}
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
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation.Error;
using Epam.FixAntenna.NetCore.Validation.FixMessage;
using Epam.FixAntenna.NetCore.Validation.Utils;
using Epam.FixAntenna.NetCore.Validation.Validators.Condition;
using Epam.FixAntenna.NetCore.Validation.Validators.Condition.Container;
using Epam.FixAntenna.NetCore.Validation.Validators.Condition.Operators;

namespace Epam.FixAntenna.NetCore.Validation.Validators
{
	/// <summary>
	/// Implementation of IValidator that supports conditional validation.
	/// </summary>
	internal class ConditionalValidator : AbstractValidator
	{
		private ConditionalGroup _currentGroup;

		/// <summary>
		/// Creates the <c>ConditionalValidator</c>.
		/// </summary>
		/// <param name="util"> the fixutil </param>
		public ConditionalValidator(FixUtil util) : base(util)
		{
		}

		/// <inheritdoc />
		public override FixErrorContainer Validate(string msgType, IValidationFixMessage message,
			bool isContentValidation)
		{
			return ValidateConditionForTags((ValidationFixMessage)message, msgType);
		}

		/// <summary>
		/// Validates of conditions of message .
		/// </summary>
		/// <param name="message">  FIXMessage. </param>
		/// <param name="messageType"> type of message. </param>
		/// <returns> if message has errors, returns list with errors in otherwise empty list. </returns>
		private FixErrorContainer ValidateConditionForTags(ValidationFixMessage message, string messageType)
		{
			var errors = new FixErrorContainer();
			if (!IsMessageTypeExist(messageType))
			{
				errors.Add(FixErrorBuilder.BuildError(FixErrorCode.InvalidMsgtype, message.GetMsgSeqNumber(),
					messageType, Tags.MsgType));
				return errors;
			}

			Util.GetConditionalCache().TryGetValue(messageType, out var conditionalMessage);
			ValidateConditionalTags(conditionalMessage.GetConditionMap(), message, message.FixMessage,
				false, errors);
			ValidateConditionalBlock(conditionalMessage.GetConditionalBlocks(), message,
				message.FixMessage, false, errors);
			ValidateConditionalGroup(conditionalMessage.GetConditionalGroups(), message, errors);
			return errors;
		}

		private void ValidateConditionalBlock(IList<ConditionalBlock> conditionalBlocks,
			ValidationFixMessage validationMessage, Message.FixMessage fixMessage, bool fromGroup, FixErrorContainer errors)
		{
			foreach (var conditionalBlock in conditionalBlocks)
			{
				var conditionMap = conditionalBlock.GetConditionMap();
				var iCondition = conditionalBlock.GetCondition();
				if (iCondition != null && iCondition.IsRequired(fixMessage) || conditionalBlock.IsRequired() ||
					HasConditionalTagInMessage(fixMessage,
						BuildSetWithAllConditionalTags(conditionalBlock.GetConditionMap())))
				{
					ValidateConditionalTags(conditionMap, validationMessage, fixMessage, fromGroup, errors);
				}

				// validate internal Blocks.
				var internalBlocks = conditionalBlock.GetConditionalBlocks();
				if (internalBlocks != null && internalBlocks.Count > 0)
				{
					ValidateConditionalBlock(internalBlocks, validationMessage, fixMessage, fromGroup, errors);
				}

				// validate child block's groups.
				var internalGroups = conditionalBlock.GetConditionalGroups();
				if (internalGroups != null && internalGroups.Count > 0)
				{
					ValidateConditionalGroup(internalGroups, validationMessage, errors);
				}
			}
		}

		private void ValidateConditionalGroup(IList<ConditionalGroup> conditionalGroups,
			ValidationFixMessage message, FixErrorContainer errors)
		{
			foreach (var group in conditionalGroups)
			{
				_currentGroup = group;
				FixGroupContainer container;
				try
				{
					container = FixGroupContainer.CreateFixGroupContainer(message, Util).CreateContainer(group);
				}
				catch (FormatException e)
				{
					errors.Add(FixErrorBuilder.BuildError(FixErrorCode.Other, (TagValue)null,
						"The message: " + message.GetMsgType() +
						" is not valid because : throw NumberFormatException " + e.Message));
					return;
				}
				catch (ArgumentException e)
				{
					errors.Add(FixErrorBuilder.BuildError(FixErrorCode.Other, (TagValue)null,
						"The message: " + message.GetMsgType() +
						" is not valid because : throw IllegalArgumentException " + e.Message));
					return;
				}

				var conditionMap = container.GetConditionMap();
				var groupsCountTag = container.GetGroupsCountTag();
				var fixMessages = container.GetFixMessages();
				var groupConditionalTags = container.GetGroupConditionalTags();
				if (fixMessages.Count < container.GetGroupsCount())
				{
					errors.Add(FixErrorBuilder.BuildError(FixErrorCode.RequiredTagMissing,
						message.GetTag(container.GetGroupStartTag()), message.GetMsgSeqNumber(),
						message.GetMsgType(), container.GetGroupStartTag()));
				}

				// Check condition rule for the root tag for the group.
				if (conditionMap.TryGetValue(groupsCountTag, out var iCondition)
					&& fixMessages.Count == 0 && iCondition != null)
				{
					foreach (var tag in iCondition.GetTags())
					{
						if (message.ContainsTagInAllFields(tag))
						{
							if (!iCondition.ValidateCondition(groupsCountTag, message.FullFixMessage,
								message.GetMsgType(), conditionMap, false))
							{
								errors.Add(FixErrorBuilder.BuildError(FixErrorCode.CondrequiredTagMissing,
									message.GetTag(groupsCountTag), message.GetMsgSeqNumber(),
									StringHelper.NewString(message.FullFixMessage.MsgType),
									groupsCountTag));
							}
						}
					}
				}

				// check other condition rule for the group.
				foreach (var fixMessage in fixMessages)
				{
					if (CheckValidateRules(message, conditionMap, groupConditionalTags, iCondition, fixMessage))
					{
						ValidateConditionalTags(conditionMap, message, fixMessage, true, errors);
						// validate internal Blocks.
						var internalBlocks = group.GetConditionalBlocks();
						if (internalBlocks != null && internalBlocks.Count > 0)
						{
							ValidateConditionalBlock(internalBlocks, message, fixMessage, true, errors);
						}
					}
				}

				// validate internal Groups.
				var internalConditionalGroups = group.GetConditionalGroups();
				if (internalConditionalGroups != null && internalConditionalGroups.Count > 0)
				{
					ValidateConditionalGroup(internalConditionalGroups, message, errors);
				}
			}
		}

		private bool CheckValidateRules(ValidationFixMessage message, IDictionary<int, ICondition> conditionMap,
			ISet<int> groupConditionalTags, ICondition iCondition, Message.FixMessage fixMessage)
		{
			return HasConditionalTagInMessage(fixMessage, groupConditionalTags) ||
					HasCyclicDependency(conditionMap) ||
					HasGroupRootTagConReq(iCondition, message.FullFixMessage);
		}

		public virtual void ValidateConditionalTags(IDictionary<int, ICondition> conditionMap,
			ValidationFixMessage message, Message.FixMessage fixMessage, bool fromGroup, FixErrorContainer errors)
		{
			var list = fixMessage.DeepCopyTo(new Message.FixMessage());
			if (list.IsEmpty)
			{
				list.AddAll(message.FullFixMessage);
			}
			else if (!message.FixMessage.Equals(fixMessage))
			{
				// ???
				list.AddAll(message.FixMessage);
			}

			foreach (var condTag in conditionMap.Keys)
			{
				var condition = conditionMap[condTag];
				if (condition.IsRequired(message.FullFixMessage) ||
					Util.IsRequiredTag(message.GetMsgType(), condTag) || HasCyclicDependency(conditionMap))
				{
					if (condition.IsGroupTags() && !fromGroup)
					{
						list = message.FullFixMessage;
					}
					//TODO: if cond. rule related to internal root tags
					//else
					//{
					//	AddsRequiredTags(message, list, condition.GetTags());
					//}

					if (!condition.ValidateCondition(condTag, list, message.GetMsgType(), conditionMap, false))
					{
						errors.Add(FixErrorBuilder.BuildError(FixErrorCode.CondrequiredTagMissing,
							message.GetTag(condTag), message.GetMsgSeqNumber(),
							StringHelper.NewString(message.FullFixMessage.MsgType), condTag));
					}
				}
			}
		}

		private void AddsRequiredTags(ValidationFixMessage message, Message.FixMessage list, IList<int> tags)
		{
			if (_currentGroup != null && _currentGroup.GetConditionalGroups() != null)
			{
				var conditionalGroups = _currentGroup.GetConditionalGroups();
				var tagValue = ReturnsTagValue(message, tags, _currentGroup);
				if (tagValue == null)
				{
					tagValue = ReturnsTagsFromInternalGroups(conditionalGroups, message, tags);
				}

				AddFixField(list, tagValue);
			}
		}

		private void AddFixField(Message.FixMessage list, TagValue tagValue)
		{
			if (tagValue != null)
			{
				list.Add(tagValue);
			}
		}

		private TagValue ReturnsTagValue(ValidationFixMessage message, IList<int> tags, ConditionalGroup conditionalGroup)
		{
			var container = FixGroupContainer.CreateFixGroupContainer(message, Util).CreateContainer(conditionalGroup);

			foreach (var fixMessage in container.GetFixMessages())
			{
				foreach (var tag in tags)
				{
					var tagValue = fixMessage.GetTag(tag);
					if (tagValue != null)
					{
						return tagValue;
					}
				}
			}

			return null;
		}

		private TagValue ReturnsTagsFromInternalGroups(IList<ConditionalGroup> conditionalGroups, ValidationFixMessage message, IList<int> tags)
		{
			var tagValue = new TagValue();
			foreach (var conditionalGroup in conditionalGroups)
			{
				tagValue = ReturnsTagValue(message, tags, conditionalGroup);
				if (tagValue != null)
				{
					break;
				}

				tagValue = ReturnsTagsFromInternalGroups(conditionalGroup.GetConditionalGroups(), message, tags);
			}

			return tagValue;
		}

		private ISet<int> BuildSetWithAllConditionalTags(IDictionary<int, ICondition> conditionMap)
		{
			ISet<int> set = new HashSet<int>();
			set.AddRange(conditionMap.Keys);
			foreach (var tag in conditionMap.Keys)
			{
				var condition = conditionMap[tag];
				set.AddRange(condition.GetTags());
			}

			return set;
		}

		private bool HasGroupRootTagConReq(ICondition iCondition, Message.FixMessage fixMessage)
		{
			if (iCondition != null)
			{
				var condTags = iCondition.GetTags();
				foreach (var condTag in condTags)
				{
					if (fixMessage.GetTag(condTag) != null)
					{
						return true;
					}
				}
			}

			return false;
		}

		private bool HasCyclicDependency(IDictionary<int, ICondition> conditionMap)
		{
			foreach (var key in conditionMap.Keys)
			{
				conditionMap.TryGetValue(key, out var condition);
				if (condition is NotValidateOperator)
				{
					foreach (var tag in condition.GetTags())
					{
						if (conditionMap.TryGetValue(tag, out var internalCondition))
						{
							var internalTags = internalCondition.GetTags();
							if (internalTags.Contains(key))
							{
								return true;
							}
						}
					}
				}
			}

			return false;
		}

		private bool HasConditionalTagInMessage(Message.FixMessage message, ISet<int> conditionalTags)
		{
			if (conditionalTags != null && conditionalTags.Count > 0 && message != null && !message.IsEmpty)
			{
				foreach (var tagId in conditionalTags)
				{
					if (message.IsTagExists(tagId))
					{
						return true;
					}
				}
			}

			return false;
		}
	}
}
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
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Validation.Entities;
using Epam.FixAntenna.NetCore.Validation.FixMessage;
using Epam.FixAntenna.NetCore.Validation.Utils;

namespace Epam.FixAntenna.NetCore.Validation
{
	internal class ValidationFixGroupBuilder
	{
		private static ValidationFixGroupBuilder _validationFixGroupBuilder;

		private ValidationFixGroupBuilder()
		{
		}

		public static ValidationFixGroupBuilder CreateBuilder()
		{
			if (_validationFixGroupBuilder == null)
			{
				_validationFixGroupBuilder = new ValidationFixGroupBuilder();
			}

			return _validationFixGroupBuilder;
		}

		public virtual ValidationFixGroupContainer BuildGroup(Message.FixMessage fixMessage, int groupIndex,
			IDictionary<int, Field> groupTags, int parentGroupTag, FixUtil fixUtil)
		{
			var fixGroupContainer = new ValidationFixGroupContainer();
			var fixFieldCount = fixMessage.Length;
			var internalFixList = new Message.FixMessage();
			var messageType = StringHelper.NewString(fixMessage.MsgType);
			var groupLength = 0;
			var startField = -1;
			// starts from the index of group
			for (var fixFieldIndex = groupIndex; fixFieldIndex < fixFieldCount; fixFieldIndex++)
			{
				var tagValue = fixMessage[fixFieldIndex];
				// only first tag check.
				if (fixFieldIndex == groupIndex)
				{
					startField = fixUtil.GetStartTagForGroup(messageType, tagValue.TagId);
					// calculates end of group
					var keys = new HashSet<int>(groupTags.Keys);
					groupLength = fixUtil.CountLengthForGroupUnit(fixMessage, groupIndex, startField, tagValue, keys,
						messageType, parentGroupTag);
					// adds group length to the list
					internalFixList.Add(tagValue);
					// increment group in order to add first group's tag.
					groupLength += 1;
				}

				// if we move to the end of group return the container with FIXGroup
				// and shiftIndex;
				var theLastCycleOfLoop = fixFieldIndex == fixFieldCount - 1;
				if (groupLength == 0 || theLastCycleOfLoop)
				{
					if (tagValue.TagId != Tags.CheckSum && theLastCycleOfLoop)
					{
						// adds fix field into the group's FixMessage
						internalFixList.Add(tagValue);
					}

					// sets shift index for the goup's tags.
					fixGroupContainer.SetFixFieldIndex(fixFieldIndex - 1);
					// create new FIXGroup.
					fixGroupContainer.UpdateValidationFixGroup(new ValidationFixGroup(internalFixList,
						new List<ValidationFixGroup>()));
					break;
				}

				// checks if group Contains internal goups.
				if (fixFieldIndex > groupIndex)
				{
					var internalGroupTags = fixUtil.GetGroupTagsWithInternalGroups(
						StringHelper.NewString(fixMessage.MsgType), tagValue.TagId, fixMessage);
					if (internalGroupTags.Count > 0)
					{
						// creates internal group
						var internalContainer = BuildGroup(fixMessage, fixFieldIndex, internalGroupTags, startField,
							fixUtil);
						// calculate shift index for internal goup length
						var internalGroupShiftIndex = internalContainer.GetFixFieldIndex() - fixFieldIndex;
						// move group length to the internalGroupShiftIndex
						groupLength -= internalGroupShiftIndex;
						// moves index over internal group tags.
						fixFieldIndex = internalContainer.GetFixFieldIndex();

						// adds internal group into parrent goup.
						fixGroupContainer.GetValidationFixGroup()
							.AddFixGroup(internalContainer.GetValidationFixGroup());
					}
					else
					{
						// adds fix field into the group's FixMessage
						internalFixList.Add(tagValue);
					}
				}

				// decrement group length
				--groupLength;
			}

			return fixGroupContainer;
		}
	}
}
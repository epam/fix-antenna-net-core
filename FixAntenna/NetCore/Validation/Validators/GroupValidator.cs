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
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation.Entities;
using Epam.FixAntenna.NetCore.Validation.Error;
using Epam.FixAntenna.NetCore.Validation.Error.Resource;
using Epam.FixAntenna.NetCore.Validation.FixMessage;
using Epam.FixAntenna.NetCore.Validation.Utils;

namespace Epam.FixAntenna.NetCore.Validation.Validators
{
	/// <summary>
	/// Implementation of IValidator that supports group validation.
	/// </summary>
	internal class GroupValidator : AbstractValidator
	{
		public GroupValidator(FixUtil util) : base(util)
		{
		}

		/// <inheritdoc />
		public override FixErrorContainer Validate(string msgType, IValidationFixMessage message,
			bool isContentValidation)
		{
			var errors = new FixErrorContainer();
			var validationFixMessage = (ValidationFixMessage)message;

			if (!IsMessageTypeExist(msgType))
			{
				errors.Add(FixErrorBuilder.BuildError(FixErrorCode.InvalidMsgtype, validationFixMessage.GetMsgSeqNumber(),
					msgType, Tags.MsgType));
				return errors;
			}

			ValidatePresentRgTagIdAndRgStartTagId(msgType, validationFixMessage, errors);
			// isRequired all groups.
			var validationFixGroups = validationFixMessage.ValidationFixGroups;
			ValidateGroup(validationFixGroups, validationFixMessage, msgType, errors);
			return errors;
		}

		private void ValidatePresentRgTagIdAndRgStartTagId(string msgType, ValidationFixMessage fixMessage,
			FixErrorContainer errors)
		{
			var message = fixMessage.FullFixMessage;
			var gc = Util.GetGroupsCache(msgType);
			// set of RG ID. If message contain at least one tag of RG we make a check if in message present RG Id and lead tag.
			var unCheckRg = new HashSet<int>(gc.GetGroupsCaches().Keys);
			var missRgIDs = new List<int>();
			int? rgIdFoundIn = null;

			var missStartRgIDs = new List<int>();
			int? startRgIdFoundIn = null;

			for (var i = 0; i < message.Length; i++)
			{
				if (unCheckRg.Count == 0)
				{
					break;
				}

				var tag = message.GetTagIdAtIndex(i);
				rgIdFoundIn = null;
				startRgIdFoundIn = null;
				missRgIDs.Clear();
				missStartRgIDs.Clear();

				foreach (var rgId in unCheckRg)
				{
					if (gc.GetGroupsTagIDsWithBlocksTags(rgId).Contains(tag))
					{
						// check
						var rg = gc.GetGroupCache(rgId);
						if (message.IsTagExists(rg.GetGroupId()))
						{
							rgIdFoundIn = rgId;
						}
						else
						{
							missRgIDs.Add(rgId);
						}

						if (message.IsTagExists(rg.GetStartTagId()))
						{
							startRgIdFoundIn = rgId;
						}
						else
						{
							missStartRgIDs.Add(rgId);
						}

						if (rgIdFoundIn != null && startRgIdFoundIn != null)
						{
							break;
						}
					}
				}

				if (rgIdFoundIn == null)
				{
					// notFound
					if (missRgIDs.Count > 0)
					{
						unCheckRg.RemoveAll(missRgIDs);
						foreach (var rgId in missRgIDs)
						{
							var rg = gc.GetGroupCache(rgId);
							var tagValue = message.GetTag(tag);
							errors.Add(FixErrorBuilder.BuildError(FixErrorCode.RequiredTagMissing, tagValue,
								fixMessage.GetMsgSeqNumber(), msgType, rg.GetGroupId()));
						}
					}
				}
				else
				{
					unCheckRg.Remove(rgIdFoundIn.Value);
				}

				if (startRgIdFoundIn == null)
				{
					if (missStartRgIDs.Count > 0)
					{
						unCheckRg.RemoveAll(missStartRgIDs);
						foreach (var rgId in missStartRgIDs)
						{
							var rg = gc.GetGroupCache(rgId);
							var tagValue = message.GetTag(tag);
							errors.Add(FixErrorBuilder.BuildError(FixErrorCode.RequiredTagMissing, tagValue,
								fixMessage.GetMsgSeqNumber(), msgType, rg.GetStartTagId()));
						}
					}
				}
				else
				{
					unCheckRg.Remove(startRgIdFoundIn.Value);
				}
			}
		}

		private void ValidateGroup(IList<ValidationFixGroup> validationFixGroups, ValidationFixMessage fixMessage,
			string msgType, FixErrorContainer errors)
		{
			// // gets all tags of group
			// final Map<Integer, Field> groupTags = util.getGroupTagsWithInternalGroups(msgType, tag, fixMessage);

			foreach (var validationFixGroup in validationFixGroups)
			{
				// gets internal groups for the parent group
				var internalFixGroups = validationFixGroup.ValidationFixGroups;
				// check to the parent group Contains the child groups.
				if (internalFixGroups != null && internalFixGroups.Count > 0)
				{
					ValidateGroup(internalFixGroups, fixMessage, msgType, errors);
				}

				var noField = validationFixGroup.GetNoField();
				FixGroupContainer container;
				try
				{
					container = FixGroupContainer.CreateFixGroupContainer(fixMessage, Util)
						.CreateSeparateList(fixMessage, validationFixGroup.FixMessage, noField);
				}
				catch (FormatException e)
				{
					errors.Add(FixErrorBuilder.BuildError(FixErrorCode.Other, (TagValue)null,
						"The message: " + fixMessage.GetMsgType() +
						" is not valid because : throw NumberFormatException " + e.Message));
					return;
				}
				catch (ArgumentException e)
				{
					errors.Add(FixErrorBuilder.BuildError(FixErrorCode.Other, (TagValue)null,
						"The message: " + fixMessage.GetMsgType() +
						" is not valid because : throw IllegalArgumentException " + e.Message));
					return;
				}

				// isRequired group.
				var messages = container.GetFixMessages();
				var startField = Util.GetStartTagForGroup(msgType, noField);
				if (messages.Count != container.GetGroupsCount())
				{
					errors.Add(FixErrorBuilder.BuildError(
						FixErrorCode.IncorrectNumingroupCountForRepeatingGroup, fixMessage.GetTag(startField),
						fixMessage.GetMsgSeqNumber(), msgType, startField));
				}

				foreach (var contentOfGroup in messages)
				{
					// check start field of group.
					var fieldsCount = contentOfGroup.Length;
					for (var fieldIndex = 0; fieldIndex < fieldsCount; fieldIndex++)
					{
						var field = contentOfGroup[fieldIndex];
						if (field.TagId == noField)
						{
							var nextFieldIndex = fieldIndex + 1;
							if (nextFieldIndex < fieldsCount)
							{
								if (contentOfGroup[nextFieldIndex].TagId != startField)
								{
									errors.Add(FixErrorBuilder.BuildError(
										FixErrorCode.RepeatingGroupFieldsOutOfOrder, fixMessage.GetTag(startField),
										fixMessage.GetMsgSeqNumber(), msgType, startField));
								}
							}

							break;
						}
					}

					// checks order of groups tag.
					CheckOrderOfGroupsTag(validationFixGroup.FixMessage, fixMessage.FullFixMessage,
						noField, errors, msgType, startField);

					var countOfRepeatingGroups = 0;
					try
					{
						// gets max of frequency of tags
						countOfRepeatingGroups = (int)contentOfGroup.GetTagValueAsLong(noField);
					}
					catch (FormatException e)
					{
						errors.Add(FixErrorBuilder.BuildError(FixErrorCode.Other, (TagValue)null,
							"The message: " + fixMessage.GetMsgType() +
							" is not valid because : throw NumberFormatException " + e.Message));
						return;
					}
					catch (ArgumentException e)
					{
						errors.Add(FixErrorBuilder.BuildError(FixErrorCode.Other, (TagValue)null,
							"The message: " + fixMessage.GetMsgType() +
							" is not valid because : throw IllegalArgumentException " + e.Message));
						return;
					}
					catch (FieldNotFoundException e)
					{
						errors.Add(FixErrorBuilder.BuildError(FixErrorCode.Other, (TagValue)null,
							"The message: " + fixMessage.GetMsgType() +
							" is not valid because : throw FieldNotFoundException " + e.Message));
						return;
					}

					IList<Field> requiredFieldsOfGroup = new List<Field>();
					if (fieldsCount > 0)
					{
						requiredFieldsOfGroup =
							GetRequiredFields(Util.GetGroupsTags(msgType, noField, contentOfGroup, false));
					}

					var groupTags =
						Util.GetGroupTagsWithInternalGroups(msgType, noField, fixMessage.FullFixMessage);
					for (var index = 0; index < fieldsCount; index++)
					{
						var fixField = contentOfGroup[index];
						groupTags.TryGetValue(fixField.TagId, out var groupTag);
						if (groupTag != null)
						{
							requiredFieldsOfGroup.Remove(groupTag);
							var groupTagValue = groupTag.Tag;
							var frequency = CountFrequency(contentOfGroup, groupTagValue);
							if (frequency == 0)
							{
								continue;
							}

							// checks first tag of group this tag must be always
							if (index == 0)
							{
								if ("Y".Equals(groupTag.Req))
								{
									if (contentOfGroup[1].TagId != groupTag.Tag)
									{
										errors.Add(FixErrorBuilder.BuildError(
											FixErrorCode.RepeatingGroupFieldsOutOfOrder,
											fixMessage.GetTag(groupTagValue), fixMessage.GetMsgSeqNumber(), msgType,
											groupTagValue));
									}
								}
								else
								{
									if (IsConditionalValidareFailed(errors, groupTag.Condreq))
									{
										errors.Add(FixErrorBuilder.BuildError(
											FixErrorCode.RepeatingGroupFieldsOutOfOrder,
											fixMessage.GetTag(groupTagValue), fixMessage.GetMsgSeqNumber(), msgType,
											groupTagValue));
									}
								}

								frequency = CountFrequency(validationFixGroup.FixMessage, groupTagValue);
								// checks first required tag
								CheckTag(msgType, errors, countOfRepeatingGroups, frequency,
									fixMessage.GetMsgSeqNumber(), fixMessage.GetTag(groupTagValue));
							}
							else
							{
								// checks required tag
								if ("Y".Equals(groupTag.Req))
								{
									frequency = CountFrequency(validationFixGroup.FixMessage, groupTagValue);
									CheckTag(msgType, errors, countOfRepeatingGroups, frequency,
										fixMessage.GetMsgSeqNumber(), fixMessage.GetTag(groupTagValue));
								}
								// check other tags of group
								else
								{
									// check if tag is present in group and counts of present(check only more than size of
									// repeating gorup because these not required tags. )
									frequency = CountFrequency(contentOfGroup, groupTagValue);
									if (frequency > 1)
									{
										errors.Add(FixErrorBuilder.BuildError(
											FixErrorCode.IncorrectNumingroupCountForRepeatingGroup,
											fixMessage.GetTag(groupTagValue), fixMessage.GetMsgSeqNumber(), msgType,
											groupTagValue));
									}
								}
							}
						}
					}

					if (requiredFieldsOfGroup.Count > 0)
					{
						foreach (var field in requiredFieldsOfGroup)
						{
							errors.Add(FixErrorBuilder.BuildError(FixErrorCode.RequiredTagMissing,
								new TagValue(field.Tag, ""), fixMessage.GetMsgSeqNumber(), msgType,
								field.Tag));
						}
					}
				}
			}
		}

		private IList<Field> GetRequiredFields(IDictionary<int, Field> groupTags)
		{
			IList<Field> fields = new List<Field>();
			foreach (var field in groupTags.Values)
			{
				if ("Y".Equals(field.Req))
				{
					fields.Add(field);
				}
			}

			return fields;
		}

		// checkOrderOfGroupsTag

		private void CheckOrderOfGroupsTag(Message.FixMessage contentOfGroup, Message.FixMessage fixFields, int groupTag,
			FixErrorContainer errors, string messageType, int startField)
		{
			// If full array of fix fields contain the tag and groups tags contain this tag ,
			// but array in part of fields do not contain tag .
			CheckOutsideGroupTags(messageType, fixFields, contentOfGroup, groupTag, startField, errors);
			// if List of group tags is not contain the tag from a fix message group.
			CheckInsideGroupTags(contentOfGroup, fixFields, groupTag, errors, messageType);
		}

		private void CheckInsideGroupTags(Message.FixMessage contentOfGroup, Message.FixMessage fixFields, int groupTag,
			FixErrorContainer errors, string messageType)
		{
			var groupFields = Util.GetGroupTagsWithOutInternalGroups(messageType, groupTag, fixFields);
			ISet<int> tags = new HashSet<int>(groupFields.Keys);
			foreach (var tagValue in contentOfGroup)
			{
				var tag = tagValue.TagId;
				// skip group length.
				if (tag == groupTag)
				{
					continue;
				}

				if (!tags.Contains(tag))
				{
					errors.Add(FixErrorBuilder.BuildError(FixErrorCode.UndefinedTag, tagValue,
						ResourceHelper.GetStringMessage("INVALID_MESSAGE_TAG_NOT_DEFINED_REPEATING_GROUP",
							fixFields.MsgSeqNumber, messageType, tag)));
				}
			}
		}

		/// <summary>
		/// Checks outside group tags
		/// </summary>
		/// <param name="messageFields">      the message type </param>
		/// <param name="groupContentFields"> the group fields from messageFields </param>
		/// <param name="groupTag">           the group tag </param>
		/// <param name="startField">         the start tag of group </param>
		/// <param name="errors">             the error container </param>
		private void CheckOutsideGroupTags(string messageType, Message.FixMessage messageFields,
			Message.FixMessage groupContentFields, int groupTag, int startField, FixErrorContainer errors)
		{
			var insideGroup = false;
			ISet<int> allGroupTags =
				new HashSet<int>(Util.GetGroupTagsWithOutInternalGroups(messageType, groupTag, messageFields).Keys);
			ISet<int> groupWithInnerGroupTags =
				new HashSet<int>(Util.GetGroupTagsWithInternalGroups(messageType, groupTag, messageFields).Keys);
			// creates loop by all tags for the FIX Message
			foreach (var tagValue in messageFields)
			{
				var tag = tagValue.TagId;
				// if group starts - sets the flag
				if (startField == tag)
				{
					insideGroup = true;
				}
				else
				{
					// if prev state of insideGroup is true, check if group field Contains the tag,
					// if no - we outside group
					insideGroup = insideGroup ? groupWithInnerGroupTags.Contains(tag) : false;
				}

				var error = false;
				if (!insideGroup)
				{
					// we outside group
					// checks if group content Contains this tag.
					error = allGroupTags.Contains(tag);
				}

				// if message Contains this tag, ignore error.
				if (Util.IsMessageContainField(messageType, tag))
				{
					// message body contain tag too, ignore it
					error = false;
				}

				if (error)
				{
					errors.Add(FixErrorBuilder.BuildError(FixErrorCode.Other, tagValue,
						ResourceHelper.GetStringMessage("INVALID_MESSAGE_TAG_IS_OUTSIDE_REPEATING_GROUP",
							messageFields.MsgSeqNumber, messageType, tag)));
				}
			}
		}

		private int CountFrequency(Message.FixMessage fields, int groupTag)
		{
			var frequency = 0;
			foreach (var tag in fields)
			{
				if (tag.TagId == groupTag)
				{
					++frequency;
				}
			}

			return frequency;
		}

		private bool IsConditionalValidareFailed(FixErrorContainer errors, string tag)
		{
			foreach (var error in errors.Errors)
			{
				if (FixErrorCode.CondrequiredTagMissing.Equals(error.FixErrorCode) && tag != null &&
					error.Description.Contains(tag))
				{
					return true;
				}
			}

			return false;
		}

		private void CheckTag(string msgType, FixErrorContainer errors, int countOfRepeatingGroups, int frequency,
			long msgSeqNumber, TagValue tagValue)
		{
			if (frequency != countOfRepeatingGroups)
			{
				errors.Add(FixErrorBuilder.BuildError(FixErrorCode.IncorrectNumingroupCountForRepeatingGroup,
					tagValue, msgSeqNumber, msgType, tagValue.TagId));
			}
		}
	}
}
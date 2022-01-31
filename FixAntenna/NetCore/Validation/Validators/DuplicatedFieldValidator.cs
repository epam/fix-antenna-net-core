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
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation.Error;
using Epam.FixAntenna.NetCore.Validation.FixMessage;
using Epam.FixAntenna.NetCore.Validation.Utils;

namespace Epam.FixAntenna.NetCore.Validation.Validators
{
	/// <summary>
	/// Implementation of IValidator that supports duplicated field validation.
	/// </summary>
	internal class DuplicatedFieldValidator : AbstractValidator
	{
		/// <summary>
		/// Cerates <c>DuplicatedFieldValidator</c>.
		/// </summary>
		/// <param name="util"> of type FixUtils </param>
		public DuplicatedFieldValidator(FixUtil util) : base(util)
		{
		}

		/// <inheritdoc />
		public override FixErrorContainer Validate(string msgType, IValidationFixMessage message,
			bool isContentValidation)
		{
			return Validate2(msgType, (ValidationFixMessage)message, isContentValidation);
		}

		public virtual FixErrorContainer Validate2(string msgType, ValidationFixMessage fixMessage,
			bool isContentValidation)
		{
			var validationErrors = new FixErrorContainer();
			if (!IsMessageTypeExist(msgType))
			{
				validationErrors.Add(FixErrorBuilder.BuildError(FixErrorCode.InvalidMsgtype,
					fixMessage.GetMsgSeqNumber(), msgType, Tags.MsgType));
				return validationErrors;
			}

			ISet<int> validatedFieldList = new HashSet<int>();
			var fieldList = fixMessage.FullFixMessage;
			var fieldsLength = fieldList.Length;

			for (var fieldCount = 0; fieldCount < fieldsLength; fieldCount++)
			{
				var tag = fieldList.GetTagIdAtIndex(fieldCount);
				if (isContentValidation && IsHeaderOrTrailer(tag))
				{
					continue;
				}

				if (Util.IsGroupTag(msgType, tag))
				{
					fieldCount = ValidateRg(msgType, fixMessage, fieldList, validationErrors, tag, ++fieldCount);
					continue;
				}

				if (!validatedFieldList.Add(tag))
				{
					// Duplicate
					AddDuplicateError(fieldCount, msgType, fixMessage, validationErrors, tag);
				}
			}

			return validationErrors;
		}

		private int ValidateRg(string msgType, ValidationFixMessage validationMessage, Message.FixMessage fixMessage,
			FixErrorContainer validationErrors, int startGroup, int fieldCount)
		{
			ISet<int> validatedFieldList = new HashSet<int>();
			var groupsCache = Util.GetGroupsCache(msgType);
			var groupTags = groupsCache.GetGroupsTagIDsWithBlocksTags(startGroup);
			var gCache = groupsCache.GetGroupCache(startGroup);
			var fieldsLength = fixMessage.Length;
			var startTagId = gCache.GetStartTagId();
			for (; fieldCount < fieldsLength; fieldCount++)
			{
				var tag = fixMessage.GetTagIdAtIndex(fieldCount);
				if (!groupTags.Contains(tag))
				{
					// End of RG
					--fieldCount;
					break;
				}

				if (Util.IsGroupTag(msgType, tag))
				{
					fieldCount = ValidateRg(msgType, validationMessage, fixMessage, validationErrors, tag, ++fieldCount);
				}

				if (startTagId == tag && validatedFieldList.Count > 0)
				{
					// New RG entry
					validatedFieldList.Clear();
				}

				if (!validatedFieldList.Add(tag))
				{
					// Duplicate
					AddDuplicateError(fieldCount, msgType, validationMessage, validationErrors, tag);
				}
			}

			return fieldCount;
		}

		private void AddDuplicateError(int tagCounter, string msgType, ValidationFixMessage fixMessage,
			FixErrorContainer validationErrors, int tag)
		{
			var tagValue = new TagValue();
			fixMessage.FullFixMessage.LoadTagValueByIndex(tagCounter, tagValue);
			validationErrors.Add(FixErrorBuilder.BuildError(FixErrorCode.TagAppearsMoreThanOnce, tagValue,
				fixMessage.GetMsgSeqNumber(), msgType, tag));
		}
	}
}
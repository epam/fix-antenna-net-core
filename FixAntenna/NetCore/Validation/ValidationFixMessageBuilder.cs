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
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation.FixMessage;
using Epam.FixAntenna.NetCore.Validation.Utils;

namespace Epam.FixAntenna.NetCore.Validation
{
	/// <summary>
	/// Provides ability to create <see cref="ValidationFixMessage"/>.
	/// </summary>
	internal class ValidationFixMessageBuilder
	{
		private static ValidationFixMessageBuilder _validationFixMessageBuilder;

		private FixUtil FixUtil { get; set; }
		private ValidationFixGroupBuilder _groupBuilder;

		private ValidationFixMessageBuilder()
		{
		}

		public static ValidationFixMessageBuilder CreateBuilder(FixUtil fixUtil)
		{
			if (_validationFixMessageBuilder == null)
			{
				_validationFixMessageBuilder = new ValidationFixMessageBuilder();
			}

			_validationFixMessageBuilder.FixUtil = fixUtil;
			return _validationFixMessageBuilder;
		}

		/// <summary>
		/// Creates the <see cref="ValidationFixMessage"/>.
		/// </summary>
		/// <param name="fixMessage"> the validated message </param>
		public virtual ValidationFixMessage BuildValidationFixMessage(Message.FixMessage fixMessage)
		{
			var validationFixMessage = new ValidationFixMessage(null, new List<ValidationFixGroup>(), fixMessage);

			var msgType = fixMessage.GetTagValueAsString(35);
			var resultMessage = new Message.FixMessage();
			var tagValue = new TagValue();
			for (var i = 0; i < fixMessage.Length; i++)
			{
				fixMessage.LoadTagValueByIndex(i, tagValue);

				if (FixUtil.IsGroupTag(msgType, tagValue.TagId))
				{
					var groupTags = FixUtil.GetGroupTagsWithInternalGroups(msgType, tagValue.TagId, fixMessage);
					_groupBuilder = CreateBuilder();
					var validationFixGroupContainer =
						_groupBuilder.BuildGroup(fixMessage, i, groupTags, -1, FixUtil);
					validationFixMessage.AddFixGroup(validationFixGroupContainer.GetValidationFixGroup());
					i = validationFixGroupContainer.GetFixFieldIndex();
				}
				else
				{
					resultMessage.Add(tagValue);
				}
			}

			validationFixMessage.FixMessage = resultMessage;
			return validationFixMessage;
		}

		private ValidationFixGroupBuilder CreateBuilder()
		{
			return _groupBuilder ?? (_groupBuilder = ValidationFixGroupBuilder.CreateBuilder());
		}
	}
}
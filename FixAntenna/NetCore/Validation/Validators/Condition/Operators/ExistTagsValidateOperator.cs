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
using System.Linq;

namespace Epam.FixAntenna.NetCore.Validation.Validators.Condition.Operators
{
	/// <summary>
	/// The <c>existstag</c> operator implementation.
	/// </summary>
	internal class ExistTagsValidateOperator : AbstractCondition
	{
		private readonly int[] _tags;

		/// <summary>
		/// Creates the <c>ExistTagsValidateOperator</c>.
		/// </summary>
		/// <param name="tags"> the array of tags </param>
		/// <param name="isGroup"> the group flags
		///  </param>
		public ExistTagsValidateOperator(int[] tags, bool isGroup) : base(isGroup)
		{
			_tags = tags;
		}

		/// <summary>
		/// Checks if all tags exists in <c>msg</c>.
		/// The example of use: existtags(T$1).
		/// </summary>
		/// <param name="validateTag"> the validate tag </param>
		/// <param name="msgFieldList"> the message </param>
		/// <param name="msgType"> the message type </param>
		/// <param name="tagsMap">    All tags with conditional for current part of FIXMessage. </param>
		/// <param name="inversion">  Inversion: if true all validations are doing in invert order.
		///  </param>
		public override bool ValidateCondition(int validateTag, Message.FixMessage msg, string msgType,
			IDictionary<int, ICondition> tagsMap, bool inversion)
		{
			foreach (var tag in _tags)
			{
				var field = msg.GetTag(tag);
				if (inversion)
				{
					if (field == null)
					{
						if (CheckCondition(validateTag, msg))
						{
							return false;
						}
					}
				}
				else
				{
					if (field != null)
					{
						if (CheckCondition(validateTag, msg))
						{
							return false;
						}
					}
				}
			}

			return true;
		}

		private bool CheckCondition(int validateTag, Message.FixMessage msg)
		{
			var validateField = msg.GetTag(validateTag);
			return validateField == null;
		}

		/// <inheritdoc />
		public override bool IsRequired(Message.FixMessage msg)
		{
			foreach (var tag in _tags)
			{
				if (msg.IsTagExists(tag))
				{
					return true;
				}
			}

			return false;
		}

		/// <inheritdoc />
		public override bool IsGroupTags()
		{
			return IsGroupTag;
		}

		/// <inheritdoc />
		public override void SetGroupTags(bool isGroup)
		{
			IsGroupTag = isGroup;
		}

		/// <inheritdoc />
		public override IList<int> GetTags()
		{
			return _tags.ToList();
		}
	}
}
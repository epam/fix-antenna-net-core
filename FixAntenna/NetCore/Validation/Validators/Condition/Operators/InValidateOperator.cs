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

namespace Epam.FixAntenna.NetCore.Validation.Validators.Condition.Operators
{
	/// <summary>
	/// The <c>IN</c> operator implementation.
	/// </summary>
	internal class InValidateOperator : AbstractCondition
	{
		private readonly int _tag;
		private readonly string[] _values;

		/// <summary>
		/// Creates the <c>INValidateOperator</c>.
		/// </summary>
		/// <param name="tag"> the tag </param>
		/// <param name="values"> the array of value </param>
		/// <param name="isGroup"> the group tag
		///  </param>
		public InValidateOperator(int tag, string[] values, bool isGroup) : base(isGroup)
		{
			_tag = tag;
			_values = values;
		}

		/// <summary>
		/// Returns true, if the tag value equals one of the array of <c>values</c>.
		/// The example of use: T$150 in ('G','H').
		/// </summary>
		/// <param name="validateTag"> the validate tag </param>
		/// <param name="msg"> the message </param>
		/// <param name="msgType"> the message type </param>
		/// <param name="tagsMap">    All tags with conditional for current part of FIXMessage. </param>
		/// <param name="inversion">  Inversion: if true all validations are doing in invert order.
		///  </param>
		public override bool ValidateCondition(int validateTag, Message.FixMessage msg, string msgType,
			IDictionary<int, ICondition> tagsMap, bool inversion)
		{
			var tagValue = msg.GetTag(_tag);
			if (tagValue != null)
			{
				var isInvertValid = true;
				var valueOfTag = tagValue.StringValue;
				foreach (var value in _values)
				{
					if (inversion && valueOfTag.Equals(value))
					{
						isInvertValid = false;
						break;
					}

					if (valueOfTag.Equals(value))
					{
						if (CheckCondition(validateTag, msg))
						{
							return false;
						}
					}
				}

				if (inversion && isInvertValid)
				{
					if (CheckCondition(validateTag, msg))
					{
						return false;
					}
				}
			}

			return true;
		}

		private bool CheckCondition(int validateTag, Message.FixMessage fixMessage)
		{
			return fixMessage.GetTag(validateTag) == null;
		}

		/// <summary>
		/// Returns true, if the tag value equals one of the array of <c>values</c> values.
		/// </summary>
		/// <param name="msg"> the message</param>
		public override bool IsRequired(Message.FixMessage msg)
		{
			var field = msg.GetTag(_tag);
			if (field != null)
			{
				var valueOfTag = field.StringValue;
				foreach (var value in _values)
				{
					if (valueOfTag.Equals(value))
					{
						return true;
					}
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
			return new List<int> { _tag };
		}

		/// <summary>
		/// Gets tag.
		/// </summary>
		public virtual int GetTag()
		{
			return _tag;
		}

		/// <summary>
		/// Gets the values.
		/// </summary>
		public virtual string[] GetValues()
		{
			return _values;
		}
	}
}
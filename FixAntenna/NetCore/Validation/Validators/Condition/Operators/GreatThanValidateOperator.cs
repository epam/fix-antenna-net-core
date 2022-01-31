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
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.Validation.Validators.Condition.Operators
{
	/// <summary>
	/// The <c>></c> operator implementation.
	/// </summary>
	internal class GreatThanValidateOperator : AbstractCondition
	{
		private readonly int _tag;
		private readonly int _value;

		/// <summary>
		/// Creates the <c>GreatThanValidateOperator</c>.
		/// </summary>
		/// <param name="tag"> the tag </param>
		/// <param name="value"> the value </param>
		/// <param name="isGroup"> the group flag
		///  </param>
		public GreatThanValidateOperator(int tag, int value, bool isGroup) : base(isGroup)
		{
			_tag = tag;
			_value = value;
		}

		/// <summary>
		/// Returns true, if the <c>tag</c> value in <c>msg</c> is greater than <c>value</c>,
		/// otherwise false. If <c>tag</c> not exists return true.
		///
		/// The example of use: existtags(T$1) > 10.0.
		/// </summary>
		/// <param name="validateTag"> the validate tag </param>
		/// <param name="msg"> the message </param>
		/// <param name="msgType"> the message type </param>
		/// <param name="tagsMap">    All tags with conditional for current part of FIXMessage. </param>
		/// <param name="inversion">  Inversion: if true all validations are doing in invert order.
		///
		///  </param>
		public override bool ValidateCondition(int validateTag, Message.FixMessage msg, string msgType,
			IDictionary<int, ICondition> tagsMap, bool inversion)
		{
			var field = msg.GetTag(_tag);
			if (field != null)
			{
				double fieldValue;
				try
				{
					fieldValue = FixTypes.ParseInt(field);
				}
				catch (ArgumentException)
				{
					try
					{
						fieldValue = FixTypes.ParseFloat(field);
					}
					catch (ArgumentException)
					{
						return false;
					}
				}

				if (inversion)
				{
					if (!(fieldValue > _value))
					{
						if (CheckCondition(validateTag, msg))
						{
							return false;
						}
					}
				}
				else
				{
					if (fieldValue > _value)
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
			var fixField = msg.GetTag(validateTag);
			return fixField == null;
		}

		/// <summary>
		/// Returns true, if the field value in <c>msg</c> is greater than that tag value,
		/// otherwise false.
		/// </summary>
		/// <param name="msgFieldList"> the message
		///  </param>
		public override bool IsRequired(Message.FixMessage msg)
		{
			var field = msg.GetTag(_tag);
			if (field != null)
			{
				double fieldValue;
				try
				{
					fieldValue = FixTypes.ParseInt(field);
				}
				catch (ArgumentException)
				{
					try
					{
						fieldValue = FixTypes.ParseFloat(field);
					}
					catch (ArgumentException)
					{
						return false;
					}
				}

				return fieldValue > _value;
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
		/// Gets the tag.
		/// </summary>
		public virtual int GetTag()
		{
			return _tag;
		}

		/// <summary>
		/// Gets the value.
		/// </summary>
		public virtual int GetValue()
		{
			return _value;
		}
	}
}
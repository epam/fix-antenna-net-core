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
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.Validation.Validators.Condition.Operators
{
	/// <summary>
	/// The <c>=</c> operator implementation.
	/// </summary>
	internal class EqValidateOperator : AbstractCondition
	{
		private readonly int _tag;
		private readonly string _value;
		private readonly byte[] _valueBytes;

		/// <summary>
		/// Creates the <c>EqValidateOperator</c>.
		/// </summary>
		/// <param name="tag"> the tag </param>
		/// <param name="value"> the value </param>
		/// <param name="isGroup"> the group flag
		///  </param>
		public EqValidateOperator(int tag, string value, bool isGroup) : base(isGroup)
		{
			_tag = tag;
			_value = value;
			_valueBytes = value.AsByteArray();
		}

		/// <summary>
		/// Returns true, if the values are equal.
		/// The example of use: T$167='OPT'.
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
			//        final FixField field = msg.GetTag(tag);

			if (msg.IsTagExists(_tag))
			{
				if (inversion)
				{
					if (!FixMessageUtil.IsTagValueEquals(msg, _tag, _valueBytes))
					{
						if (CheckCondition(validateTag, msg))
						{
							return false;
						}
					}
				}
				else
				{
					if (FixMessageUtil.IsTagValueEquals(msg, _tag, _valueBytes))
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
			return !msg.IsTagExists(validateTag);
		}

		/// <inheritdoc />
		public override bool IsRequired(Message.FixMessage msg)
		{
			return FixMessageUtil.IsTagValueEquals(msg, _tag, _valueBytes);
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
		/// Gets the value.
		/// </summary>
		public virtual string GetValue()
		{
			return _value;
		}
	}
}
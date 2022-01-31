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
	/// The NOT operator implementation.
	/// </summary>
	internal class NotValidateOperator : AbstractCondition
	{
		private readonly ICondition _operand;

		/// <summary>
		/// Creates the <c>NOTValidateOperator</c>.
		/// </summary>
		/// <param name="operand"> the operand </param>
		/// <param name="isGroup"> the group tag
		///  </param>
		public NotValidateOperator(ICondition operand, bool isGroup) : base(isGroup)
		{
			_operand = operand;
		}

		/// <summary>
		/// Returns true, if <c>operand</c> is true.
		/// The example of use: NOT existtags(T$2).
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
			var internalTags = _operand.GetTags();
			// loop dependecy
			foreach (var tag in internalTags)
			{
				if (tagsMap.TryGetValue(tag, out var condition) && condition != null)
				{
					var conTags = condition.GetTags();
					if (condition is NotValidateOperator && conTags.Contains(validateTag))
					{
						// first cycle of loop
						if (!inversion)
						{
							return _operand.ValidateCondition(validateTag, msg, msgType, tagsMap, true) &&
									condition.ValidateCondition(tag, msg, msgType, tagsMap, true);
						}
					}
				}
			}

			return _operand.ValidateCondition(validateTag, msg, msgType, tagsMap, true);
		}

		/// <summary>
		/// Returns true if <c>operand</c> is not required. </summary>
		/// <param name="msgFieldList"> the message
		///  </param>
		public override bool IsRequired(Message.FixMessage msg)
		{
			return !_operand.IsRequired(msg);
		}

		/// <inheritdoc />
		public override bool IsGroupTags()
		{
			return _operand.IsGroupTags();
		}

		/// <inheritdoc />
		public override void SetGroupTags(bool isGroup)
		{
			_operand.SetGroupTags(isGroup);
		}

		/// <inheritdoc />
		public override IList<int> GetTags()
		{
			return _operand.GetTags();
		}

		/// <summary>
		/// Gets the operand
		/// </summary>
		public virtual ICondition GetOperand()
		{
			return _operand;
		}
	}
}
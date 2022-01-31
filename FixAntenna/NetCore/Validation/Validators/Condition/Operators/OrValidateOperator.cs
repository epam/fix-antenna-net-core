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
	/// The logical OR operator.
	/// </summary>
	internal class OrValidateOperator : AbstractCondition
	{
		private readonly ICondition _operand1;
		private readonly ICondition _operand2;

		/// <summary>
		/// Creates the <c>OrValidateOperator</c>.
		/// </summary>
		/// <param name="operand1"> the first operand </param>
		/// <param name="operand2"> the second operand </param>
		/// <param name="isGroup"> the group tag
		///  </param>
		public OrValidateOperator(ICondition operand1, ICondition operand2, bool isGroup) : base(isGroup)
		{
			_operand1 = operand1;
			_operand2 = operand2;
		}

		/// <summary>
		/// Returns true, if one of the operators returns true.
		///
		/// The example of use: existtags(T$1) OR existtags(T$2).
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
			var validateOper1 = _operand1.ValidateCondition(validateTag, msg, msgType, tagsMap, inversion);
			var validateOper2 = _operand2.ValidateCondition(validateTag, msg, msgType, tagsMap, inversion);
			return !(!validateOper1 || !validateOper2);
		}

		/// <summary>
		/// Returns true, if both operands are required. </summary>
		/// <param name="msgFieldList"> the message
		///  </param>
		public override bool IsRequired(Message.FixMessage msg)
		{
			return _operand1.IsRequired(msg) || _operand2.IsRequired(msg);
		}

		/// <summary>
		/// Returns true, if one of all operand is group.
		/// </summary>
		public override bool IsGroupTags()
		{
			return _operand1.IsGroupTags() || _operand2.IsGroupTags();
		}

		/// <summary>
		/// Sets group flag. </summary>
		/// <param name="isGroup"> the group flag
		///  </param>
		public override void SetGroupTags(bool isGroup)
		{
			_operand1.SetGroupTags(isGroup);
			_operand2.SetGroupTags(isGroup);
		}

		/// <summary>
		/// Gets tags from operands.
		/// </summary>
		public override IList<int> GetTags()
		{
			var integers = new List<int>();
			integers.AddRange(_operand1.GetTags());
			integers.AddRange(_operand2.GetTags());
			return integers;
		}

		/// <summary>
		/// Gets the first operand.
		/// </summary>
		public virtual ICondition GetOperand1()
		{
			return _operand1;
		}

		/// <summary>
		/// Gets the second operand.
		/// </summary>
		public virtual ICondition GetOperand2()
		{
			return _operand2;
		}
	}
}
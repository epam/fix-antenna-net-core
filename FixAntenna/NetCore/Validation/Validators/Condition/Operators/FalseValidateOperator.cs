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
	/// The FALSE operator.
	/// </summary>
	internal class FalseValidateOperator : AbstractCondition
	{
		public FalseValidateOperator(bool isGroup) : base(isGroup)
		{
		}

		/// <summary>
		/// Always returns false.
		/// The example of use: existtags(T$1) OR FALSE.
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
			return false;
		}

		/// <inheritdoc />
		public override bool IsRequired(Message.FixMessage msg)
		{
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
			return new List<int>();
		}
	}
}
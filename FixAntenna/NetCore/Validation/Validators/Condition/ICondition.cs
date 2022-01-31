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

namespace Epam.FixAntenna.NetCore.Validation.Validators.Condition
{
	internal interface ICondition
	{
		/// <summary>
		/// This method proposes for the validate condition rules.
		/// </summary>
		/// <param name="validateTag">  Tag with conditional rule. </param>
		/// <param name="msgFieldList"> Part of FIX message where doing validation process. </param>
		/// <param name="msgType">      Type of FIXMessage. </param>
		/// <param name="tagsMap">      All tags with conditional for current part of FIXMessage. </param>
		/// <param name="inversion">    Inversion: if true all validations are doing in invert order. </param>
		/// <returns> true if condition valid in other case false. </returns>
		bool ValidateCondition(int validateTag, Message.FixMessage msg, string msgType,
			IDictionary<int, ICondition> tagsMap, bool inversion);

		/// <summary>
		/// Returns true, if msg has a required tag.
		/// </summary>
		/// <param name="msgFieldList"> the message
		///  </param>
		bool IsRequired(Message.FixMessage msg);

		/// <summary>
		/// Returns true, if this group tag.
		/// </summary>
		bool IsGroupTags();

		/// <summary>
		/// Setter for group.
		/// </summary>
		/// <param name="isGroup"> the group flag
		///  </param>
		void SetGroupTags(bool isGroup);

		/// <summary>
		/// Gets the tags list
		/// </summary>
		IList<int> GetTags();
	}
}
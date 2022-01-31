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
	internal abstract class AbstractCondition : ICondition
	{
		protected internal bool IsGroupTag;

		public AbstractCondition(bool isGroup)
		{
			IsGroupTag = isGroup;
		}

		/// <inheritdoc />
		public abstract IList<int> GetTags();

		/// <inheritdoc />
		public abstract void SetGroupTags(bool isGroup);

		/// <inheritdoc />
		public abstract bool IsGroupTags();

		/// <inheritdoc />
		public abstract bool IsRequired(Message.FixMessage msg);

		/// <inheritdoc />
		public abstract bool ValidateCondition(int validateTag, Message.FixMessage msg, string msgType,
			IDictionary<int, ICondition> tagsMap, bool inversion);
	}
}
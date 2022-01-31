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
using Epam.FixAntenna.NetCore.Common;

namespace Epam.FixAntenna.NetCore.Validation.Validators.Condition.Container
{
	/// <summary>
	/// Provides the condition group.
	/// </summary>
	internal class ConditionalGroup : IConditionalMessage
	{
		private readonly IList<ConditionalBlock> _conditionalBlocks = new List<ConditionalBlock>();
		private readonly IList<ConditionalGroup> _conditionalGroups = new List<ConditionalGroup>();

		private IDictionary<int, ICondition> _conditionMap = new Dictionary<int, ICondition>();

		private bool _isRequiredConflict;
		private int _rootTag;

		public ConditionalGroup(int rootTag, IDictionary<int, ICondition> conditionMap)
		{
			_rootTag = rootTag;
			_conditionMap = conditionMap;
			_isRequiredConflict = false;
		}

		public virtual int GetRootTag()
		{
			return _rootTag;
		}

		public virtual void SetRootTag(int rootTag)
		{
			_rootTag = rootTag;
		}

		public virtual IDictionary<int, ICondition> GetConditionMap()
		{
			_conditionMap = Resort(_conditionMap);
			return _conditionMap;
		}

		public virtual void PutConditionMap(IDictionary<int, ICondition> conditionMap)
		{
			_conditionMap.PutAll(conditionMap);
		}

		public virtual IList<ConditionalGroup> GetConditionalGroups()
		{
			return _conditionalGroups;
		}

		public virtual void AddConditionalGroup(ConditionalGroup conditionalGroup)
		{
			_conditionalGroups.Add(conditionalGroup);
		}

		public virtual void AddConditionalGroups(IList<ConditionalGroup> conditionalGroups)
		{
			((List<ConditionalGroup>)conditionalGroups).AddRange(conditionalGroups);
		}

		public virtual IList<ConditionalBlock> GetConditionalBlocks()
		{
			return _conditionalBlocks;
		}

		public virtual void AddConditionalBlock(ConditionalBlock conditionalBlock)
		{
			_conditionalBlocks.Add(conditionalBlock);
		}

		public virtual void AddConditionalBlock(IList<ConditionalBlock> conditionalBlocks)
		{
			((List<ConditionalBlock>)_conditionalBlocks).AddRange(conditionalBlocks);
		}

		public virtual bool IsRequired()
		{
			return _isRequiredConflict;
		}

		public virtual void SetRequired(bool required)
		{
			_isRequiredConflict = required;
		}

		private IDictionary<int, ICondition> Resort(IDictionary<int, ICondition> conditionMap)
		{
			return conditionMap;
		}
	}
}
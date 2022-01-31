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

namespace Epam.FixAntenna.NetCore.Validation.FixMessage
{
	internal class ValidationFixGroupContainer
	{
		private int _fixFieldIndex;
		private ValidationFixGroup _validationFixGroup;

		public ValidationFixGroupContainer()
		{
			_fixFieldIndex = 0;
			_validationFixGroup = new ValidationFixGroup(null, new List<ValidationFixGroup>());
		}

		public ValidationFixGroupContainer(ValidationFixGroup validationFixGroup, int fixFieldIndex)
		{
			_validationFixGroup = validationFixGroup;
			_fixFieldIndex = fixFieldIndex;
		}

		public virtual int GetFixFieldIndex()
		{
			return _fixFieldIndex;
		}

		public virtual ValidationFixGroup GetValidationFixGroup()
		{
			return _validationFixGroup;
		}

		public virtual void SetFixFieldIndex(int fixFieldIndex)
		{
			_fixFieldIndex = fixFieldIndex;
		}

		public virtual void SetValidationFixGroup(ValidationFixGroup validationFixGroup)
		{
			_validationFixGroup = validationFixGroup;
		}

		public virtual void UpdateValidationFixGroup(ValidationFixGroup validationFixGroup)
		{
			_validationFixGroup.FixMessage = validationFixGroup.FixMessage;
			var validationFixGroups = validationFixGroup.ValidationFixGroups;
			if (validationFixGroups != null && validationFixGroups.Count > 0)
			{
				foreach (var fixGroup in validationFixGroups)
				{
					_validationFixGroup.AddFixGroup(fixGroup);
				}
			}
		}
	}
}
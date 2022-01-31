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
	internal class LessThanValidateOperator : AbstractCondition
	{
		private readonly int _tag;
		private readonly int _value;

		public LessThanValidateOperator(int tag, int value, bool isGroup) : base(isGroup)
		{
			_tag = tag;
			_value = value;
		}

		/// <inheritdoc />
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
					if (!(fieldValue < _value))
					{
						if (CheckCondition(validateTag, msg))
						{
							return false;
						}
					}
				}
				else
				{
					if (fieldValue < _value)
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

		/// <inheritdoc />
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

				return fieldValue < _value;
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

		public virtual int GetTag()
		{
			return _tag;
		}

		public virtual int GetValue()
		{
			return _value;
		}
	}
}
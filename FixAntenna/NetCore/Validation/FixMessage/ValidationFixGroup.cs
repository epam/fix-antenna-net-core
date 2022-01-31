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
	/// <summary>
	/// This class provide functionality for recursion create FIX messages with hierarchic of groups.
	/// </summary>
	internal class ValidationFixGroup : IValidationFixMessage
	{
		private int _noField = -1;
		private Message.FixMessage _fixMessage;

		public IList<ValidationFixGroup> ValidationFixGroups { get; private set; }

		public ValidationFixGroup(Message.FixMessage fixMessage, IList<ValidationFixGroup> validationFixGroups)
		{
			_fixMessage = fixMessage;
			ValidationFixGroups = validationFixGroups;
			if (fixMessage != null && fixMessage.Length > 0)
			{
				_noField = fixMessage[0].TagId;
			}
		}

		public virtual int GetMessageSize()
		{
			var size = _fixMessage.Length;
			foreach (var validationFixGroup in ValidationFixGroups)
			{
				size += validationFixGroup.GetMessageSize();
			}

			return size;
		}

		public virtual Message.FixMessage FixMessage
		{
			set
			{
				_fixMessage = value;
				if (value != null && value.Length > 0)
				{
					_noField = value[0].TagId;
				}
			}
			get => _fixMessage;
		}

		public virtual void AddFixGroup(ValidationFixGroup validationFixGroup)
		{
			if (ValidationFixGroups == null)
			{
				ValidationFixGroups = new List<ValidationFixGroup>();
			}

			ValidationFixGroups.Add(validationFixGroup);
		}

		public virtual int GetNoField()
		{
			return _noField;
		}

		public virtual IList<FixMessageMap> GetFixFieldByGroupIndt(int tag)
		{
			if (_noField == tag)
			{
				return CreateMap(tag, _fixMessage);
			}

			return GetFixFieldByGroupIndt(ValidationFixGroups, tag);
		}

		private IList<FixMessageMap> GetFixFieldByGroupIndt(IList<ValidationFixGroup> validationFixGroups, int tag)
		{
			IList<FixMessageMap> fieldListMap = new List<FixMessageMap>();
			foreach (var validationFixGroup in validationFixGroups)
			{
				var internalValidationFixGroups = validationFixGroup.ValidationFixGroups;
				if (internalValidationFixGroups != null && internalValidationFixGroups.Count > 0)
				{
					((List<FixMessageMap>)fieldListMap).AddRange(GetFixFieldByGroupIndt(internalValidationFixGroups,
						tag));
				}

				if (validationFixGroup.GetNoField() == tag)
				{
					((List<FixMessageMap>)fieldListMap).AddRange(CreateMap(tag, validationFixGroup.FixMessage));
				}
			}

			return fieldListMap;
		}

		private IList<FixMessageMap> CreateMap(int tag, Message.FixMessage fixMessage)
		{
			IList<FixMessageMap> fixMessageMaps = new List<FixMessageMap>();
			fixMessageMaps.Add(new FixMessageMap(tag, fixMessage));
			return fixMessageMaps;
		}

		public override string ToString()
		{
			return "ValidationFIXGroup{"
						+ "FixMessage=" + _fixMessage
						+ ", validationFIXGroups=" + ValidationFixGroups + '}';
		}
	}
}
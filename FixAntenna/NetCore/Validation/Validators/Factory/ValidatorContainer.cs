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

namespace Epam.FixAntenna.NetCore.Validation.Validators.Factory
{
	/// <summary>
	/// Contains the list of validators.
	/// </summary>
	/// <seealso cref="Validation.ValidatorType" />
	internal class ValidatorContainer : IValidatorContainer
	{
		private readonly Dictionary<ValidatorType, IValidator>
			_validators = new Dictionary<ValidatorType, IValidator>();

		private MessageTypeValidator _messageTypeValidator;

		/// <inheritdoc />
		public virtual void PutNewValidator(ValidatorType type, IValidator validator)
		{
			if (type == Validation.ValidatorType.MessageType)
			{
				_messageTypeValidator = (MessageTypeValidator)validator;
			}
			else
			{
				_validators[type] = validator;
			}
		}

		/// <inheritdoc />
		public virtual IValidator GetValidator(ValidatorType type)
		{
			return _validators[type];
		}

		/// <inheritdoc />
		public virtual IList<IValidator> GetValidatorsWithOutInputs(IList<ValidatorType> types)
		{
			var keySet = _validators.Keys;
			var validatorList = new List<IValidator>();
			foreach (var validatorType in keySet)
			{
				if (!types.Contains(validatorType))
				{
					validatorList.Add(_validators[validatorType]);
				}
			}

			return validatorList;
		}

		/// <inheritdoc />
		public virtual IList<IValidator> ValidatorsWithOutMessageType
		{
			get
			{
				var validatorCollection = _validators.Values;
				return new List<IValidator>(validatorCollection);
			}
		}

		/// <inheritdoc />
		public virtual IList<IValidator> AllValidators
		{
			get
			{
				var validatorCollection = _validators.Values;
				IList<IValidator> validatorList = new List<IValidator>(validatorCollection);
				validatorList.Add(_messageTypeValidator);
				return validatorList;
			}
		}

		/// <inheritdoc />
		public virtual IValidator MessageTypeValidator
		{
			get { return _messageTypeValidator; }
		}

		/// <inheritdoc />
		public virtual void PutOtherValidators(IValidatorContainer container)
		{
			var types = container.ValidatorType;
			foreach (var type in types)
			{
				PutNewValidator(type, container.GetValidator(type));
			}
		}

		/// <inheritdoc />
		public virtual ISet<ValidatorType> ValidatorType
		{
			get
			{
				ISet<ValidatorType> types = new HashSet<ValidatorType>(_validators.Keys);
				if (_messageTypeValidator != null)
				{
					types.Add(Validation.ValidatorType.MessageType);
				}

				return types;
			}
		}
	}
}
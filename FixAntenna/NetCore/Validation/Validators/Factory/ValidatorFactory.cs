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

using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Validation.Utils;

namespace Epam.FixAntenna.NetCore.Validation.Validators.Factory
{
	/// <summary>
	/// Provides ability to create validators.
	/// </summary>
	/// <seealso cref="ValidatorType"> </seealso>
	public sealed class ValidatorFactory : IValidatorFactory
	{
		private readonly FixUtil _fixUtil;

		private ValidatorFactory(FixUtil util)
		{
			_fixUtil = util;
		}

		/// <inheritdoc />
		public IValidator CreateValidator(ValidatorType type)
		{
			IValidator validator;
			switch (type)
			{
				case ValidatorType.Conditional:
					validator = new ConditionalValidator(_fixUtil);
					break;
				case ValidatorType.Duplicate:
					validator = new DuplicatedFieldValidator(_fixUtil);
					break;
				case ValidatorType.FieldAllowed:
					validator = new FieldAllowedInMessageValidator(_fixUtil);
					break;
				case ValidatorType.FieldDefinition:
					validator = new FieldsDefinitionsTypeValidator(_fixUtil);
					break;
				case ValidatorType.FieldOrder:
					validator = new FieldOrderValidator(_fixUtil);
					break;
				case ValidatorType.Group:
					validator = new GroupValidator(_fixUtil);
					break;
				case ValidatorType.MessageType:
					validator = new MessageTypeValidator(_fixUtil);
					break;
				case ValidatorType.MessageWelformed:
					validator = new MessageWelformedValidator(_fixUtil);
					break;
				case ValidatorType.RequiredFields:
					validator = new RequiredFieldValidator(_fixUtil);
					break;
				default:
				{
					validator = null;
					break;
				}
			}

			return validator;
		}

		/// <inheritdoc />
		public IValidatorContainer CreateAllValidators()
		{
			IValidatorContainer validatorContainer = new ValidatorContainer();
			validatorContainer.PutNewValidator(ValidatorType.Conditional, new ConditionalValidator(_fixUtil));
			validatorContainer.PutNewValidator(ValidatorType.Duplicate, new DuplicatedFieldValidator(_fixUtil));
			validatorContainer.PutNewValidator(ValidatorType.FieldAllowed,
				new FieldAllowedInMessageValidator(_fixUtil));
			validatorContainer.PutNewValidator(ValidatorType.FieldDefinition,
				new FieldsDefinitionsTypeValidator(_fixUtil));
			validatorContainer.PutNewValidator(ValidatorType.FieldOrder, new FieldOrderValidator(_fixUtil));
			validatorContainer.PutNewValidator(ValidatorType.Group, new GroupValidator(_fixUtil));
			validatorContainer.PutNewValidator(ValidatorType.MessageType, new MessageTypeValidator(_fixUtil));
			validatorContainer.PutNewValidator(ValidatorType.MessageWelformed, new MessageWelformedValidator(_fixUtil));
			validatorContainer.PutNewValidator(ValidatorType.RequiredFields, new RequiredFieldValidator(_fixUtil));
			return validatorContainer;
		}

		/// <inheritdoc />
		public IValidatorContainer CreateRequiredValidator()
		{
			IValidatorContainer validatorContainer = new ValidatorContainer();
			validatorContainer.PutNewValidator(ValidatorType.MessageType, new MessageTypeValidator(_fixUtil));
			return validatorContainer;
		}

		/// <summary>
		/// Creates validation factory for build any validators.
		/// </summary>
		/// <param name="fixVersion"> FIX version of dictionary. </param>
		/// <param name="appVersion"> Application FIX version of dictionary. </param>
		/// <returns> Instance of <c>ValidationFactory</c>. </returns>
		public static IValidatorFactory CreateFactory(FixVersionContainer fixVersion, FixVersionContainer appVersion)
		{
			return new ValidatorFactory(FixUtilFactory.Instance.GetFixUtil(fixVersion, appVersion));
		}
	}
}
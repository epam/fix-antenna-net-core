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

namespace Epam.FixAntenna.NetCore.Validation
{
	public interface IValidatorContainer
	{
		void PutNewValidator(ValidatorType type, IValidator validator);

		/// <summary>
		/// Gets the validator by validator type.
		/// </summary>
		/// <param name="type"> the type of validator
		///  </param>
		IValidator GetValidator(ValidatorType type);

		/// <summary>
		/// Gets the validators with specific types.
		/// </summary>
		/// <param name="validatorTypes"> the types of validator
		///  </param>
		IList<IValidator> GetValidatorsWithOutInputs(IList<ValidatorType> validatorTypes);

		/// <summary>
		/// Gets the validators. The result does not contain <c>MessageType</c> validator.
		///
		/// </summary>
		IList<IValidator> ValidatorsWithOutMessageType { get; }

		/// <summary>
		/// Gets validators.
		/// </summary>
		/// <value>
		///   the list of validator
		/// </value>
		IList<IValidator> AllValidators { get; }

		/// <summary>
		/// Gets validators.
		/// </summary>
		/// <value>
		///   the list of validator
		/// </value>
		IValidator MessageTypeValidator { get; }

		/// <summary>
		/// Gets the list of type validator.
		/// </summary>
		/// <value>
		///   the list of validator
		/// </value>
		ISet<ValidatorType> ValidatorType { get; }

		/// <summary>
		/// Appends the validators from <c>container</c> container into current instance.
		/// </summary>
		/// <returns> the list of validator
		///  </returns>
		void PutOtherValidators(IValidatorContainer container);
	}
}
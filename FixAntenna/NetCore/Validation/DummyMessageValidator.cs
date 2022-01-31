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
using Epam.FixAntenna.NetCore.Validation.Error;

namespace Epam.FixAntenna.NetCore.Validation
{
	/// <summary>
	/// The dummy message validator implementation.
	/// This validator used instead of real validator.
	/// </summary>
	internal sealed class DummyMessageValidator : IMessageValidator
	{
		public static readonly IMessageValidator Instance = new DummyMessageValidator();
		private static readonly DummyValidationResult ValidationResult = new DummyValidationResult();

		/// <inheritdoc />
		public IValidationResult ValidateContent(string msgType, Message.FixMessage content)
		{
			return ValidationResult;
		}

		/// <inheritdoc />
		public bool ContentValidation { get; set; }

		/// <inheritdoc />
		public IValidationResult Validate(Message.FixMessage message)
		{
			return ValidationResult;
		}

		/// <inheritdoc />
		public void Validate(Message.FixMessage message, IValidationResult result)
		{
			result.Errors.Add(ValidationResult.Errors);
		}

		private sealed class DummyValidationResult : IValidationResult
		{
			private static readonly FixErrorContainer Container = new FixErrorContainer(new List<FixError>());

			public bool IsMessageValid => true;

			public FixErrorContainer Errors => Container;

			public void Reset()
			{
			}
		}
	}
}
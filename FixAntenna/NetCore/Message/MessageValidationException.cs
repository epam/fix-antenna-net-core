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

using System.Text;
using Epam.FixAntenna.NetCore.Validation;
using Epam.FixAntenna.NetCore.Validation.Error;

namespace Epam.FixAntenna.NetCore.Message
{
	internal class MessageValidationException : InvalidMessageException
	{
		private readonly FixErrorCode _fixErrorCode;
		private readonly TagValue _problemField;
		private readonly IValidationResult _validationResult;

		public MessageValidationException(FixMessage invalidMessage, IValidationResult result, string description) :
			this(invalidMessage, result, description, false)
		{
		}

		public MessageValidationException(FixMessage invalidMessage, IValidationResult result, string description,
			bool critical) : base(invalidMessage, description, critical)
		{
			_validationResult = result;
			var priorityFixError = result.Errors.IsPriorityError;
			_problemField = priorityFixError.TagValue;
			_fixErrorCode = priorityFixError.FixErrorCode;
		}

		public MessageValidationException(FixMessage invalidMessage, TagValue tagValue, FixErrorCode errorCode,
			string description, bool critical) : base(invalidMessage, description, critical)
		{
			var error = new FixError(errorCode, description, tagValue);
			var errorContainer = new FixErrorContainer();
			errorContainer.Add(error);
			_validationResult = new ValidationResult(errorContainer);

			_problemField = tagValue;
			_fixErrorCode = errorCode;
		}

		public override string Message
		{
			get
			{
				var builder =
					new StringBuilder("We have some errors while validate message: ").Append(GetInvalidMessage());
				foreach (var error in _validationResult.Errors.Errors)
				{
					builder.Append("\n\terror ").Append(error.Description);
					builder.Append(" on ").Append(error.TagValue);
					builder.Append(" error code: ").Append(error.FixErrorCode);
				}

				return base.Message;
			}
		}

		public virtual TagValue GetProblemField()
		{
			return _problemField;
		}

		public virtual FixErrorCode GetFixErrorCode()
		{
			return _fixErrorCode;
		}

		public virtual IValidationResult GetValidationResult()
		{
			return _validationResult;
		}

		private class ValidationResult : IValidationResult
		{
			private readonly FixErrorContainer _errorContainer;

			public ValidationResult(FixErrorContainer errorContainer)
			{
				_errorContainer = errorContainer;
			}

			public bool IsMessageValid => false;

			public FixErrorContainer Errors => _errorContainer;

			public void Reset()
			{
			}
		}
	}
}
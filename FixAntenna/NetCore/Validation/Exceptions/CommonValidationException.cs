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
using Epam.FixAntenna.NetCore.Validation.Error;

namespace Epam.FixAntenna.NetCore.Validation.Exceptions
{
	internal class CommonValidationException : Exception
	{
		/// <summary>
		/// Construct a <c>CommonValidatorException</c> with the specified detail message
		/// and nested exception.
		/// </summary>
		/// <param name="validationError"> instance that Contains all problems </param>
		/// <param name="cause">           the nested exception </param>
		public CommonValidationException(FixError validationError, Exception cause) 
			: base(validationError.Description, cause)
		{
			ValidationError = validationError;
		}

		/// <summary>
		/// Retrieve the innermost cause of this exception, if any.
		/// </summary>
		/// <value> the innermost exception, or <c>null</c> if none </value>
		public Exception RootCause
		{
			get
			{
				Exception rootCause = null;
				Exception cause = this;
				while (cause != null && cause != rootCause)
				{
					rootCause = cause;
					cause = cause.InnerException;
				}

				return rootCause;
			}
		}

		/// <summary>
		/// Gets the validationError of this <c>CommonValidatorException</c> object.
		/// </summary>
		/// <value> the FIXError. </value>
		public FixError ValidationError { get; }
	}
}
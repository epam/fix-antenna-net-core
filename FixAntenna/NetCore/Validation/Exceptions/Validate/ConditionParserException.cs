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

namespace Epam.FixAntenna.NetCore.Validation.Exceptions.Validate
{
	internal class ConditionParserException : CommonValidationException
	{
		/// <summary>
		/// Construct a <c>ConditionValidateException</c> with the specified detail message
		/// and nested exception.
		/// </summary>
		/// <param name="validationError"> instance that Contains all problems </param>
		/// <param name="cause">           the nested exception </param>
		public ConditionParserException(FixError validationError, Exception cause) : base(validationError, cause)
		{
		}

		/// <summary>
		/// Construct a <c>ConditionValidateException</c> with the specified detail message
		/// and nested exception.
		/// </summary>
		/// <param name="str"> description of problem </param>
		public ConditionParserException(string str) : base(
			FixErrorBuilder.CreateBuilder().BuildError(FixErrorCode.CondrequiredTagMissing, str), null)
		{
		}
	}
}
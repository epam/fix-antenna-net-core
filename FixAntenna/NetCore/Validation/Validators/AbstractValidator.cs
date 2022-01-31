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

using Epam.FixAntenna.NetCore.Validation.Error;
using Epam.FixAntenna.NetCore.Validation.Utils;

namespace Epam.FixAntenna.NetCore.Validation.Validators
{
	/// <summary>
	/// General interface to provide creating of validators for an application.
	/// </summary>
	internal abstract class AbstractValidator : IValidator
	{
		protected internal static FixErrorBuilder FixErrorBuilder = FixErrorBuilder.CreateBuilder();

		public AbstractValidator(FixUtil util)
		{
			Util = util;
		}

		/// <summary>
		/// Method "validate" validates of FIX message and returns list of errors of validation process.
		/// </summary>
		/// <param name="msgType">             Type of Message. </param>
		/// <param name="message">          FIX Message. </param>
		/// <param name="isContentValidation"> If true that is mean that will be validation only content of FIX message, without header or trailer. </param>
		/// <returns> List of errors of validation, if process of validationg is done successeful method returns empty list of errors. </returns>
		public abstract FixErrorContainer Validate(string msgType, IValidationFixMessage message,
			bool isContentValidation);

		public virtual bool IsHeader(int tag)
		{
			return Util.IsHeader(tag);
		}

		public virtual bool IsTrailer(int tag)
		{
			return Util.IsTrailer(tag);
		}

		public virtual bool IsHeaderOrTrailer(int tag)
		{
			return Util.IsHeader(tag) || Util.IsTrailer(tag);
		}

		public virtual bool IsMessageTypeExist(string msgType)
		{
			return Util.GetMessageDefUtils().IsMessageTypeExist(msgType);
		}

		/// <summary>
		/// Gets fix util.
		/// </summary>
		/// <value> FixUtil </value>
		public FixUtil Util { get; }
	}
}
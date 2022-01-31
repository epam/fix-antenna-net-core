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

using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation;
using Epam.FixAntenna.NetCore.Validation.Error;
using Epam.FixAntenna.NetCore.Validation.FixMessage;
using Epam.FixAntenna.NetCore.Validation.Utils;

namespace Epam.FixAntenna.Fix.Validation.Engine.Validators
{
	internal abstract class AbstractValidatorTst
	{
		protected internal static FixErrorBuilder FixErrorBuilder = FixErrorBuilder.CreateBuilder();

		protected internal FixUtil FixUtil;
		protected internal IValidator Validator;

		/// <summary>
		/// Gets validator.
		/// User should implement this method for specific validation of fix messages.
		/// </summary>
		/// <param name="fixVersion"> the fix version </param>
		public abstract IValidator GetValidator(FixVersion fixVersion);

		/// <summary>
		/// Creates message for validation
		/// </summary>
		/// <param name="message"> the message </param>
		public virtual ValidationFixMessage CreateValidationMessage(FixMessage message)
		{
			var fixMessageBuilder = ValidationFixMessageBuilder.CreateBuilder(FixUtil);
			var fixMessage = fixMessageBuilder.BuildValidationFixMessage(message);
			return fixMessage;
		}

		/// <summary>
		/// Checks if exist error code.
		/// </summary>
		/// <param name="errorCode">      the error code </param>
		/// <param name="errorContainer"> the error container </param>
		public virtual bool IsExistErrorCode(FixErrorCode errorCode, FixErrorContainer errorContainer)
		{
			using (var it = errorContainer.Errors.GetEnumerator())
			{
				while (it.MoveNext())
				{
					var error = it.Current;
					if (error.FixErrorCode.Equals(errorCode))
					{
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Gets error code.
		/// </summary>
		/// <param name="errorCode">      the error code </param>
		/// <param name="errorContainer"> the error container </param>
		public virtual string GetErrorCodeDescription(FixErrorCode errorCode, FixErrorContainer errorContainer)
		{
			var it = errorContainer.Errors.GetEnumerator();
			while (it.MoveNext())
			{
				var error = it.Current;
				if (error.FixErrorCode.Equals(errorCode))
				{
					return error.Description;
				}
			}

			return "";
		}

		public FixError GetError(FixErrorCode errorCode, int seqNum, string messageType, int tag)
		{
			return FixErrorBuilder.BuildError(errorCode, seqNum, messageType, tag);
		}

		/// <summary>
		/// Gets error message.
		/// </summary>
		/// <param name="errorCode">   the error code </param>
		/// <param name="seqNum">      the sequency number </param>
		/// <param name="messageType"> the message type </param>
		/// <param name="tagValue">    the FIX field </param>
		public FixError GetError(FixErrorCode errorCode, int seqNum, string messageType, TagValue tagValue)
		{
			return FixErrorBuilder.BuildError(errorCode, tagValue, seqNum, messageType, tagValue.TagId);
		}

		/// <summary>
		/// Gets error message.
		/// </summary>
		/// <param name="errorCode">   the error code </param>
		/// <param name="seqNum">      the sequency number </param>
		/// <param name="messageType"> the message type </param>
		/// <param name="tagValue">    the FIX field </param>
		/// <param name="text">        the error text </param>
		public FixError GetError(FixErrorCode errorCode, int seqNum, string messageType, TagValue tagValue, string text)
		{
			return FixErrorBuilder.BuildError(errorCode, tagValue, text, seqNum, messageType, tagValue.TagId);
		}
	}
}
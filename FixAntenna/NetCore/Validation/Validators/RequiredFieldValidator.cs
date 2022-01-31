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

using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Validation.Error;
using Epam.FixAntenna.NetCore.Validation.FixMessage;
using Epam.FixAntenna.NetCore.Validation.Utils;

namespace Epam.FixAntenna.NetCore.Validation.Validators
{
	internal class RequiredFieldValidator : AbstractValidator
	{
		private static readonly int[] CriticalHeaderFields = { 49, 56, 52 };

		/// <summary>
		/// Constructor RequiredFieldValidator creates a new RequiredFieldValidator instance.
		/// </summary>
		/// <param name="util"> of type FixUtils </param>
		public RequiredFieldValidator(FixUtil util) : base(util)
		{
		}

		/// <inheritdoc />
		public override FixErrorContainer Validate(string msgType, IValidationFixMessage message,
			bool isContentValidation)
		{
			var fixMessage = ((ValidationFixMessage)message).FullFixMessage;

			var errors = new FixErrorContainer();

			if (!IsMessageTypeExist(msgType))
			{
				errors.Add(FixErrorBuilder.BuildError(FixErrorCode.InvalidMsgtype, fixMessage.MsgSeqNumber,
					msgType, Tags.MsgType));
				return errors;
			}

			if (isContentValidation)
			{
				// validate 49, 56, 52
				//#bug 15284: Required tags for Header of message (49, 56, 52) must be necessarily validated
				ValidateHeaderContent(msgType, fixMessage, CriticalHeaderFields, errors);
			}

			var reqTags = Util.GetRequiredTagsForMessage(msgType);
			foreach (var reqTag in reqTags)
			{
				if (isContentValidation && IsHeaderOrTrailer(reqTag))
				{
					continue; // skip header fields
				}

				if (!fixMessage.IsTagExists(reqTag))
				{
					errors.Add(FixErrorBuilder.BuildError(FixErrorCode.RequiredTagMissing,
						fixMessage.MsgSeqNumber, msgType, reqTag));
				}
			}

			return errors;
		}

		private void ValidateHeaderContent(string msgType, Message.FixMessage fixList, int[] validatedTags,
			FixErrorContainer errors)
		{
			for (var tagIndex = 0; tagIndex < validatedTags.Length; tagIndex++)
			{
				var validatedTag = validatedTags[tagIndex];
				if (!fixList.IsTagExists(validatedTag))
				{
					var value = fixList.GetTag(validatedTag);
					errors.Add(FixErrorBuilder.BuildError(FixErrorCode.RequiredTagMissing, value,
						fixList.MsgSeqNumber, msgType, validatedTag));
				}
			}
		}
	}
}
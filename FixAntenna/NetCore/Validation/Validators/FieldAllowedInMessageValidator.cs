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
	/// <summary>
	/// Implementation of IValidator that supports field allowed in message validation.
	/// </summary>
	internal class FieldAllowedInMessageValidator : AbstractValidator
	{
		/// <summary>
		/// Creates the <c>FieldAllowedInMessageValidation</c>.
		/// </summary>
		/// <param name="util"> of type FixUtils </param>
		public FieldAllowedInMessageValidator(FixUtil util) : base(util)
		{
		}

		/// <inheritdoc />
		public override FixErrorContainer Validate(string msgType, IValidationFixMessage message,
			bool isContentValidation)
		{
			var fixMessage = (ValidationFixMessage)message;
			var fieldList = fixMessage.FullFixMessage;
			var errors = new FixErrorContainer();

			if (!IsMessageTypeExist(msgType))
			{
				errors.Add(FixErrorBuilder.BuildError(FixErrorCode.InvalidMsgtype, fixMessage.GetMsgSeqNumber(),
					msgType, Tags.MsgType));
				return errors;
			}

			var fieldsLength = fieldList.Length;
			for (var fieldCount = 0; fieldCount < fieldsLength; fieldCount++)
			{
				var tagValue = fieldList[fieldCount];
				var tag = tagValue.TagId;
				if (isContentValidation && IsHeaderOrTrailer(tag))
				{
					continue;
				}

				if (tag <= 0)
				{
					errors.Add(FixErrorBuilder.BuildError(FixErrorCode.InvalidTagNumber, tagValue,
						fixMessage.GetMsgSeqNumber(), msgType, tag));
				}

				if (tag < Utils.Constants.BeginCustomTag && !Util.IsTagDefinedForMessage(msgType, tag))
				{
					if (Util.IsKnownTag(tag))
					{
						errors.Add(FixErrorBuilder.BuildError(FixErrorCode.TagNotDefinedForThisMessageType,
							tagValue, fixMessage.GetMsgSeqNumber(), msgType, tag));
					}
					else
					{
						errors.Add(FixErrorBuilder.BuildError(FixErrorCode.UndefinedTag, tagValue,
							fixMessage.GetMsgSeqNumber(), msgType, tag));
					}
				}
			}

			return errors;
		}
	}
}
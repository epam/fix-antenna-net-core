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
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Validation.Error;
using Epam.FixAntenna.NetCore.Validation.FixMessage;
using Epam.FixAntenna.NetCore.Validation.Utils;

namespace Epam.FixAntenna.NetCore.Validation.Validators
{
	/// <summary>
	/// Implementation of IValidator that supports field order validation.
	/// </summary>
	internal class FieldOrderValidator : AbstractValidator
	{
		/// <summary>
		/// Creates the <c>FieldOrderValidator</c>.
		/// </summary>
		/// <param name="util"> instance of  FixUtils </param>
		public FieldOrderValidator(FixUtil util) : base(util)
		{
		}

		/// <inheritdoc />
		public override FixErrorContainer Validate(string msgType, IValidationFixMessage message,
			bool isContentValidation)
		{
			var fixMessage = (ValidationFixMessage)message;
			var msg = fixMessage.FullFixMessage;

			var errors = new FixErrorContainer();
			if (!IsMessageTypeExist(msgType))
			{
				errors.Add(FixErrorBuilder.BuildError(FixErrorCode.InvalidMsgtype, fixMessage.GetMsgSeqNumber(),
					msgType, Tags.MsgType));
				return errors;
			}

			var fieldsLength = msg.Length;
			var state = 0;
			for (var fieldCount = 0; fieldCount < fieldsLength; fieldCount++)
			{
				var tagValue = msg[fieldCount];
				var tag = tagValue.TagId;
				if (tag >= Utils.Constants.BeginCustomTag)
				{
					continue;
				}

				switch (state)
				{
					case 0:
						if (isContentValidation && IsHeader(tag))
						{
							state = 1;
							break;
						}

						if (Util.IsTagDefinedForMessageOrBlock("SMH", tag))
						{
							state = 1;
							break;
						}

						errors.Add(FixErrorBuilder.BuildError(FixErrorCode.TagSpecifiedOutOfRequiredOrder,
							tagValue, fixMessage.GetMsgSeqNumber(), msgType, tag));
						goto case 1;
					case 1:
						if (isContentValidation && IsHeader(tag) || Util.IsTagDefinedForMessageOrBlock("SMH", tag))
						{
							break;
						}
						else if (Util.IsTagDefinedForMessageOrBlock(msgType, tag) ||
								msgType.StartsWith("U", StringComparison.Ordinal))
						{
							state = 2;
							break;
						}
						else if (isContentValidation && IsTrailer(tag) ||
								Util.IsTagDefinedForMessageOrBlock("SMT", tag))
						{
							state = 3;
							break;
						}

						errors.Add(FixErrorBuilder.BuildError(FixErrorCode.TagSpecifiedOutOfRequiredOrder,
							tagValue, fixMessage.GetMsgSeqNumber(), msgType, tag));
						goto case 2;
					case 2:
						if (Util.IsTagDefinedForMessageOrBlock(msgType, tag) ||
							msgType.StartsWith("U", StringComparison.Ordinal))
						{
							state = 2;
							break;
						}
						else if (isContentValidation && IsTrailer(tag) ||
								Util.IsTagDefinedForMessageOrBlock("SMT", tag))
						{
							state = 3;
							break;
						}

						errors.Add(FixErrorBuilder.BuildError(FixErrorCode.TagSpecifiedOutOfRequiredOrder,
							tagValue, fixMessage.GetMsgSeqNumber(), msgType, tag));
						goto case 3;
					case 3:
						if (isContentValidation && IsTrailer(tag) || Util.IsTagDefinedForMessageOrBlock("SMT", tag))
						{
							break;
						}

						errors.Add(FixErrorBuilder.BuildError(FixErrorCode.TagSpecifiedOutOfRequiredOrder,
							tagValue, fixMessage.GetMsgSeqNumber(), msgType, tag));
						break;
				}
			}

			return errors;
		}
	}
}
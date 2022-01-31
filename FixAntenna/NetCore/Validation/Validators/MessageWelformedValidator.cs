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
using System.Text;
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation.Entities;
using Epam.FixAntenna.NetCore.Validation.Error;
using Epam.FixAntenna.NetCore.Validation.FixMessage;
using Epam.FixAntenna.NetCore.Validation.Utils;

namespace Epam.FixAntenna.NetCore.Validation.Validators
{
	/// <summary>
	/// Implementation of IValidator that supports message welformed validate.
	/// Returns <c>non-null</c> on the first (if any) match.
	/// </summary>
	internal class MessageWelformedValidator : AbstractValidator
	{
		private readonly IDictionary<string, string> _strings = new Dictionary<string, string>(16);

		public MessageWelformedValidator(FixUtil util) : base(util)
		{
		}

		/// <inheritdoc />
		public override FixErrorContainer Validate(string msgType, IValidationFixMessage message,
			bool isContentValidation)
		{
			var errors = new FixErrorContainer();
			var fixMessage = (ValidationFixMessage)message;
			if (!isContentValidation)
			{
				// 8, 9, 25 - header
				HeaderValidating(fixMessage, errors);
				// 10 - trailer
				TrailerValidating(fixMessage, errors);
				// checksum validator
				ChecksumValidating(fixMessage, errors);
				// body length validator
				BodyLengthValidating(fixMessage, errors);
				// required field length isRequired
				FieldLengthValidating(fixMessage, errors);
				//  version isRequired
				VersionValidating(fixMessage, errors);
			}

			return errors;
		}

		private void VersionValidating(ValidationFixMessage fields, FixErrorContainer errors)
		{
			if (fields.GetTag(8).StringValue.Equals(FixVersion.Fixt11.MessageVersion))
			{
				var isValid = false;
				var fixField = fields.GetTag(1128);
				if (fixField == null)
				{
					return;
				}

				var fielddef = Util.GetFieldDefByTag(1128);
				var tagValue = fixField.StringValue;
				var rootVersion = PrepareString(Util.GetVersion().MessageVersion);
				var childVersion = "";
				if (fielddef != null)
				{
					IList<Item> items = fielddef.Item;
					var size = items.Count;
					for (var index = 0; index < size; index++)
					{
						var item = items[index];
						if (item.Val.Equals(tagValue))
						{
							IList<object> content = item.Content;
							foreach (var o in content)
							{
								var str = o.ToString().Trim();
								if (str.Length > 0)
								{
									childVersion = str;
								}

								if (str.Equals(rootVersion))
								{
									isValid = true;
									break;
								}
							}
						}
					}
				}

				if (!isValid)
				{
					errors.Add(FixErrorBuilder.BuildError(FixErrorCode.Other, fixField,
						"The version " + childVersion + " of FIX message does not correct. Correct version is " +
						rootVersion));
				}
			}
		}

		private string PrepareString(string messageVersion)
		{
			var exists = _strings.TryGetValue(messageVersion, out var returnString);
			if (returnString == null)
			{
				var builder = new StringBuilder(messageVersion);
				var index = builder.ToString().IndexOf(".");
				while (index != -1)
				{
					builder.Remove(index, 1);
					index = builder.ToString().IndexOf(".");
				}

				returnString = builder.ToString();
				if (exists)
				{
					_strings[messageVersion] = returnString;
				}
				else
				{
					_strings.Add(messageVersion, returnString);
				}
			}

			return returnString;
		}

		private void FieldLengthValidating(ValidationFixMessage fields, FixErrorContainer errors)
		{
			var size = fields.FullFixMessage.Length;
			for (var i = 0; i < size; i++)
			{
				var field = fields.FullFixMessage[i];
				var fielddef = Util.GetFieldDefByTag(field.TagId);
				if (fielddef != null)
				{
					int? lenField = fielddef.Lenfield;
					if (lenField != null && lenField != 0)
					{
						ValidateFieldLength(fields, errors, fielddef);
					}
				}
			}
		}

		private void ValidateFieldLength(ValidationFixMessage fields, FixErrorContainer errors, Fielddef fielddef)
		{
			int? lenField = fielddef.Lenfield;
			// if method will be used in other context
			if (lenField == null)
			{
				return;
			}

			var tag = lenField.Value;
			var tagValue = fields.GetTag(tag);
			if (tagValue == null)
			{
				errors.Add(FixErrorBuilder.BuildError(FixErrorCode.RequiredTagMissing, tagValue,
					fields.GetMsgSeqNumber(), fields.GetMsgType(), tag));
			}
			else
			{
				if (FixTypes.IsInvalidInt(tagValue))
				{
					errors.Add(FixErrorBuilder.BuildError(FixErrorCode.IncorrectDataFormatForValue,
						fields.GetMsgSeqNumber(), fields.GetMsgType(), tagValue.TagId));
					return;
				}

				var length = FixTypes.ParseInt(tagValue);
				var field = fields.GetTag(fielddef.Tag);
				if (field.StringValue.Length != length)
				{
					errors.Add(FixErrorBuilder.BuildError(FixErrorCode.Other, tagValue,
						"The length  " + length + " of encoded field " + field.TagId +
						" is invalid. Correct length is: " + field.Length));
				}
			}
		}

		private void BodyLengthValidating(ValidationFixMessage fields, FixErrorContainer errors)
		{
			var tagValue = fields.GetTag(Tags.BodyLength);
			if (tagValue != null)
			{
				var messageBodyLength = FixTypes.ParseInt(tagValue);
				var correctBodyLength = fields.CalculateBodyLength();
				if (correctBodyLength != messageBodyLength)
				{
					errors.Add(FixErrorBuilder.BuildError(FixErrorCode.Other, tagValue,
						"The body " + messageBodyLength + " length is invalid. Correct body length is " +
						correctBodyLength));
				}
			}
		}

		private void ChecksumValidating(ValidationFixMessage fields, FixErrorContainer errors)
		{
			var tagValue = fields.GetTag(Tags.CheckSum);
			if (tagValue != null)
			{
				var messageChecksum = FixTypes.ParseInt(tagValue);
				var correctChecksum = fields.CalculateChecksum();
				if (correctChecksum != messageChecksum)
				{
					errors.Add(FixErrorBuilder.BuildError(FixErrorCode.Other, tagValue,
						"Message checksum " + messageChecksum + " is invalid. Correct checksum is " + correctChecksum));
				}
			}
		}

		private void TrailerValidating(ValidationFixMessage fields, FixErrorContainer errors)
		{
			var trailerTagsSize = Utils.Constants.CriticalTagsOrderTrailer.Length;
			for (var tagIndex = 0; tagIndex < trailerTagsSize; tagIndex++)
			{
				var messageTrailerTag = fields.FullFixMessage[fields.FullFixMessage.Length - (1 + tagIndex)].TagId;
				var trailerTag = Utils.Constants.CriticalTagsOrderTrailer[tagIndex];
				if (messageTrailerTag != trailerTag)
				{
					errors.Add(FixErrorBuilder.BuildError(FixErrorCode.TagSpecifiedOutOfRequiredOrder,
						fields.GetTag(trailerTag), fields.GetMsgSeqNumber(), fields.GetMsgType(), trailerTag));
				}
			}
		}

		private void HeaderValidating(ValidationFixMessage fields, FixErrorContainer errors)
		{
			for (var tagIndex = 0; tagIndex < Utils.Constants.CriticalTagsOrderHeader.Length; tagIndex++)
			{
				var validatedTag = Utils.Constants.CriticalTagsOrderHeader[tagIndex];
				if (fields.FullFixMessage[tagIndex].TagId != validatedTag)
				{
					errors.Add(FixErrorBuilder.BuildError(FixErrorCode.TagSpecifiedOutOfRequiredOrder,
						fields.GetTag(validatedTag), fields.GetMsgSeqNumber(), fields.GetMsgType(), validatedTag));
				}
			}
		}
	}
}
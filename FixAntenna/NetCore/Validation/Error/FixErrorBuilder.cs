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
using Epam.FixAntenna.NetCore.Validation.Error.Resource;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation.Error;

namespace Epam.FixAntenna.NetCore.Validation.Error
{
	internal sealed class FixErrorBuilder
	{
		private static FixErrorBuilder _errorBuilder;

		private readonly IDictionary<FixErrorCode, FixError> _errorCodeStringMap = new Dictionary<FixErrorCode, FixError>();

		private FixErrorBuilder()
		{
			InitializeErrorsCache();
		}

		private void InitializeErrorsCache()
		{
			_errorCodeStringMap[FixErrorCode.InvalidTagNumber]
				= new FixError(
					FixErrorCode.InvalidTagNumber,
					ResourceHelper.GetStringMessage("INVALID_MESSAGE_INVALID_TAG_NUMBER"),
					null);

			_errorCodeStringMap[FixErrorCode.RequiredTagMissing]
				= new FixError(
					FixErrorCode.RequiredTagMissing,
					ResourceHelper.GetStringMessage("INVALID_MESSAGE_REQUIRED_TAG_MISSING"),
					null);

			_errorCodeStringMap[FixErrorCode.CondrequiredTagMissing]
				= new FixError(
					FixErrorCode.CondrequiredTagMissing,
					ResourceHelper.GetStringMessage("INVALID_MESSAGE_REQUIRED_TAG_MISSING"),
					null);

			_errorCodeStringMap[FixErrorCode.TagNotDefinedForThisMessageType]
				= new FixError(
					FixErrorCode.TagNotDefinedForThisMessageType,
					ResourceHelper.GetStringMessage("INVALID_MESSAGE_TAG_NOT_DEFINED"),
					null);

			_errorCodeStringMap[FixErrorCode.UndefinedTag]
				= new FixError(
					FixErrorCode.UndefinedTag,
					ResourceHelper.GetStringMessage("INVALID_MESSAGE_UNDEFINED_TAG"),
					null);

			_errorCodeStringMap[FixErrorCode.TagSpecifiedWithoutValue]
				= new FixError(
					FixErrorCode.TagSpecifiedWithoutValue,
					ResourceHelper.GetStringMessage("INVALID_MESSAGE_MISSING_VALUE"),
					null);

			_errorCodeStringMap[FixErrorCode.ValueIncorrectOutOfRangeForTag]
				= new FixError(
					FixErrorCode.ValueIncorrectOutOfRangeForTag,
					ResourceHelper.GetStringMessage("INVALID_MESSAGE__OUT_OF_RANGE_FOR_TAG"),
					null);

			_errorCodeStringMap[FixErrorCode.IncorrectDataFormatForValue]
				= new FixError(
					FixErrorCode.IncorrectDataFormatForValue,
					ResourceHelper.GetStringMessage("INVALID_MESSAGE_INCORRECT_DATA_TYPE"),
					null);

			_errorCodeStringMap[FixErrorCode.InvalidMsgtype]
				= new FixError(
					FixErrorCode.InvalidMsgtype,
					ResourceHelper.GetStringMessage("INVALID_MESSAGE_INVALID_MESSAGE_TYPE"),
					null);

			_errorCodeStringMap[FixErrorCode.TagAppearsMoreThanOnce]
				= new FixError(
					FixErrorCode.TagAppearsMoreThanOnce,
					ResourceHelper.GetStringMessage("INVALID_MESSAGE_TAG_APPEARS_MORE_THEN_ONCE"),
					null);

			_errorCodeStringMap[FixErrorCode.TagSpecifiedOutOfRequiredOrder]
				= new FixError(
					FixErrorCode.TagSpecifiedOutOfRequiredOrder,
					ResourceHelper.GetStringMessage("INVALID_MESSAGE_TAG_IS_OUT_OF_ORDER"),
					null);

			_errorCodeStringMap[FixErrorCode.RepeatingGroupFieldsOutOfOrder]
				= new FixError(
					FixErrorCode.RepeatingGroupFieldsOutOfOrder,
					ResourceHelper.GetStringMessage("INVALID_MESSAGE_TAG_IN_GROUP_IS_OUT_OF_ORDER"),
					null);

			_errorCodeStringMap[FixErrorCode.IncorrectNumingroupCountForRepeatingGroup]
				= new FixError(
					FixErrorCode.IncorrectNumingroupCountForRepeatingGroup,
					ResourceHelper.GetStringMessage("INVALID_MESSAGE_TAG_IS_REPEATED_INCORRECT_NUMBER_TIME"),
					null); //!

			_errorCodeStringMap[FixErrorCode.Other] = new FixError(FixErrorCode.Other, "", null); //Other errors
		}

		public static FixErrorBuilder CreateBuilder()
		{
			if (_errorBuilder == null)
			{
				_errorBuilder = new FixErrorBuilder();
			}

			return _errorBuilder;
		}

		private FixError BuildError(FixErrorCode fixErrorCode, TagValue tagValue)
		{
			var error = _errorCodeStringMap[fixErrorCode];
			if (error != null)
			{
				var newFixError = error.Clone();
				if (tagValue != null)
				{
					newFixError.TagValue = tagValue.Clone(); // clone original TagValue, as far as it refer same object in foreach
				}

				return newFixError;
			}

			return null;
		}

		public FixError BuildError(FixErrorCode fixErrorCode, TagValue tagValue, string text)
		{
			var fixError = BuildError(fixErrorCode, tagValue);
			if (fixError == null)
			{
				return new FixError(FixErrorCode.Other, text, tagValue);
			}

			fixError.Description = text;
			return fixError;
		}

		public FixError BuildError(FixErrorCode fixErrorCode, TagValue tagValue, long sequenceNumber, string messageType, int? tag)
		{
			var tagVal = tagValue == null && tag != null ? new TagValue(tag.Value) : tagValue;

			var fixError = BuildError(fixErrorCode, tagVal);

			if (fixError == null)
			{
				return null;
			}

			if (fixError.FixErrorCode.Code == FixErrorCode.Other.Code)
			{
				return fixError;
			}

			var tagValueNa = "N/A";
			if (tag != null)
			{
				tagValueNa = tag.ToString();
			}

			if (string.IsNullOrEmpty(messageType))
			{
				messageType = "unknown";
			}

			var custoumErrorText = string.Format(fixError.Description, sequenceNumber, messageType, tagValueNa);
			fixError.Description = custoumErrorText;
			return fixError;
		}

		public FixError BuildError(FixErrorCode fixErrorCode, TagValue tagValue, string text, long sequenceNumber, string messageType, int? tag)
		{
			var fixError = BuildError(fixErrorCode, tagValue);
			var tagValueNa = "N/A";

			if (tag != null)
			{
				tagValueNa = tag.ToString();
			}

			if (string.IsNullOrWhiteSpace(messageType))
			{
				messageType = "unknown";
			}

			var custoumErrorText = string.Format(text, sequenceNumber, messageType, tagValueNa);

			if (fixError == null)
			{
				return new FixError(FixErrorCode.Other, custoumErrorText, tagValue);
			}

			fixError.Description = custoumErrorText;
			return fixError;
		}

		public FixError BuildError(FixErrorCode fixErrorCode, string text)
		{
			return BuildError(fixErrorCode, (TagValue)null, text);
		}

		public FixError BuildError(FixErrorCode fixErrorCode, long sequenceNumber, string messageType, int? tag)
		{
			return BuildError(fixErrorCode, (TagValue)null, sequenceNumber, messageType, tag);
		}
	}
}
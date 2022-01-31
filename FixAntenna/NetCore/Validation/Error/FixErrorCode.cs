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
using System.Collections.Generic;

namespace Epam.FixAntenna.NetCore.Validation.Error
{
	public sealed class FixErrorCode
	{
		public enum InnerEnum
		{
			InvalidTagNumber,
			RequiredTagMissing,
			CondrequiredTagMissing,
			TagNotDefinedForThisMessageType,
			UndefinedTag,
			TagSpecifiedWithoutValue,
			ValueIncorrectOutOfRangeForTag,
			IncorrectDataFormatForValue,
			DecryptionProblem,
			SignatureProblem,
			CompidProblem,
			SendingTimeAccuracyProblem,
			InvalidMsgtype,
			XmlValidationError,
			TagAppearsMoreThanOnce,
			TagSpecifiedOutOfRequiredOrder,
			RepeatingGroupFieldsOutOfOrder,
			IncorrectNumingroupCountForRepeatingGroup,
			NonDataValueIncludesFieldDelimiter,
			Other
		}

		// '0' Invalid tag number
		public static readonly FixErrorCode InvalidTagNumber =
			new FixErrorCode("INVALID_TAG_NUMBER", InnerEnum.InvalidTagNumber, 0);

		//             '1' 	Required tag missing
		public static readonly FixErrorCode RequiredTagMissing =
			new FixErrorCode("REQUIRED_TAG_MISSING", InnerEnum.RequiredTagMissing, 1);

		public static readonly FixErrorCode CondrequiredTagMissing =
			new FixErrorCode("CONDREQUIRED_TAG_MISSING", InnerEnum.CondrequiredTagMissing, 1);

		//             '2' 	Tag not defined for this message type
		public static readonly FixErrorCode TagNotDefinedForThisMessageType =
			new FixErrorCode("TAG_NOT_DEFINED_FOR_THIS_MESSAGE_TYPE", InnerEnum.TagNotDefinedForThisMessageType,
				2);

		//             '3' 	Undefined Tag
		public static readonly FixErrorCode UndefinedTag =
			new FixErrorCode("UNDEFINED_TAG", InnerEnum.UndefinedTag, 3);

		//             '4' 	Tag specified without a value
		public static readonly FixErrorCode TagSpecifiedWithoutValue =
			new FixErrorCode("TAG_SPECIFIED_WITHOUT_VALUE", InnerEnum.TagSpecifiedWithoutValue, 4);

		//             '5' 	Value is incorrect (out of range) for this tag
		public static readonly FixErrorCode ValueIncorrectOutOfRangeForTag =
			new FixErrorCode("VALUE_INCORRECT_OUT_OF_RANGE_FOR_TAG", InnerEnum.ValueIncorrectOutOfRangeForTag, 5);

		//             '6' 	Incorrect data format for value
		public static readonly FixErrorCode IncorrectDataFormatForValue =
			new FixErrorCode("INCORRECT_DATA_FORMAT_FOR_VALUE", InnerEnum.IncorrectDataFormatForValue, 6);

		//             '7' 	Decryption problem
		public static readonly FixErrorCode DecryptionProblem =
			new FixErrorCode("DECRYPTION_PROBLEM", InnerEnum.DecryptionProblem, 7);

		//             '8' 	Signature problem
		public static readonly FixErrorCode SignatureProblem =
			new FixErrorCode("SIGNATURE_PROBLEM", InnerEnum.SignatureProblem, 8);

		//             '9' 	CompID problem
		public static readonly FixErrorCode CompidProblem =
			new FixErrorCode("COMPID_PROBLEM", InnerEnum.CompidProblem, 9);

		//             '10' 	SendingTime (52) accuracy problem
		public static readonly FixErrorCode SendingTimeAccuracyProblem =
			new FixErrorCode("SENDINGTIME_ACCURACY_PROBLEM", InnerEnum.SendingTimeAccuracyProblem, 10);

		//             '11' 	Invalid MsgType (35)
		public static readonly FixErrorCode InvalidMsgtype =
			new FixErrorCode("INVALID_MSGTYPE", InnerEnum.InvalidMsgtype, 11);

		//             '12' 	XML Validation error
		public static readonly FixErrorCode XmlValidationError =
			new FixErrorCode("XML_VALIDATION_ERROR", InnerEnum.XmlValidationError, 12);

		//             '13' 	Tag appears more than once
		public static readonly FixErrorCode TagAppearsMoreThanOnce =
			new FixErrorCode("TAG_APPEARS_MORE_THAN_ONCE", InnerEnum.TagAppearsMoreThanOnce, 13);

		//             '14' 	Tag specified out of required order
		public static readonly FixErrorCode TagSpecifiedOutOfRequiredOrder =
			new FixErrorCode("TAG_SPECIFIED_OUT_OF_REQUIRED_ORDER", InnerEnum.TagSpecifiedOutOfRequiredOrder, 14);

		//             '15' 	Repeating group fields out of order
		public static readonly FixErrorCode RepeatingGroupFieldsOutOfOrder =
			new FixErrorCode("REPEATING_GROUP_FIELDS_OUT_OF_ORDER", InnerEnum.RepeatingGroupFieldsOutOfOrder, 15);

		//             '16' 	Incorrect NumInGroup count for repeating group
		public static readonly FixErrorCode IncorrectNumingroupCountForRepeatingGroup =
			new FixErrorCode("INCORRECT_NUMINGROUP_COUNT_FOR_REPEATING_GROUP",
				InnerEnum.IncorrectNumingroupCountForRepeatingGroup, 16);

		//             '17' 	Non "data" value includes field delimiter (SOH character)
		public static readonly FixErrorCode NonDataValueIncludesFieldDelimiter =
			new FixErrorCode("NON_DATA_VALUE_INCLUDES_FIELD_DELIMITER",
				InnerEnum.NonDataValueIncludesFieldDelimiter, 17);

		//             '99' 	Other
		public static readonly FixErrorCode Other = new FixErrorCode("OTHER", InnerEnum.Other, 99);

		private static readonly IList<FixErrorCode> ValueList = new List<FixErrorCode>();
		private static int _nextOrdinal;
		private readonly string _nameValue;
		private readonly int _ordinalValue;

		public readonly InnerEnum InnerEnumValue;

		static FixErrorCode()
		{
			ValueList.Add(InvalidTagNumber);
			ValueList.Add(RequiredTagMissing);
			ValueList.Add(CondrequiredTagMissing);
			ValueList.Add(TagNotDefinedForThisMessageType);
			ValueList.Add(UndefinedTag);
			ValueList.Add(TagSpecifiedWithoutValue);
			ValueList.Add(ValueIncorrectOutOfRangeForTag);
			ValueList.Add(IncorrectDataFormatForValue);
			ValueList.Add(DecryptionProblem);
			ValueList.Add(SignatureProblem);
			ValueList.Add(CompidProblem);
			ValueList.Add(SendingTimeAccuracyProblem);
			ValueList.Add(InvalidMsgtype);
			ValueList.Add(XmlValidationError);
			ValueList.Add(TagAppearsMoreThanOnce);
			ValueList.Add(TagSpecifiedOutOfRequiredOrder);
			ValueList.Add(RepeatingGroupFieldsOutOfOrder);
			ValueList.Add(IncorrectNumingroupCountForRepeatingGroup);
			ValueList.Add(NonDataValueIncludesFieldDelimiter);
			ValueList.Add(Other);
		}

		internal FixErrorCode(string name, InnerEnum innerEnum, int code)
		{
			Code = code;
			_nameValue = name;
			_ordinalValue = _nextOrdinal++;
			InnerEnumValue = innerEnum;
		}

		public int Code { get; }

		public override string ToString()
		{
			return "FIXErrorCode{code=" + Code + '}';
		}

		public static IList<FixErrorCode> Values()
		{
			return ValueList;
		}

		public int Ordinal
		{
			get { return _ordinalValue; }
		}

		public static FixErrorCode ValueOf(string name)
		{
			foreach (var enumInstance in ValueList)
			{
				if (enumInstance._nameValue == name)
				{
					return enumInstance;
				}
			}

			throw new ArgumentException(name);
		}
	}
}
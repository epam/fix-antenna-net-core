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
using System.Text;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Transport
{
	/// <summary>
	/// The garbled message error enum.
	/// Describes errors that occur when parsing messages.
	/// </summary>
	internal sealed class GarbledMessageError
	{
		internal enum InnerEnum
		{
			InvalidTagNumber,
			Field8TagExpected,
			Field9TagExpected,
			Field10TagExpected,
			Field35TagExpected,
			Field8TagValueDelimiterExpected,
			Field9TagValueDelimiterExpected,
			Field10TagValueDelimiterExpected,
			Field35TagValueDelimiterExpected,
			Field8FieldDelimiterExpected,
			Field10FieldDelimiterExpected,
			Field35FieldDelimiterExpected,
			Field9DecimalValueExpected,
			Field10DecimalValueExpected,
			Field10InvalidChecksum
		}

		public static readonly GarbledMessageError InvalidTagNumber =
			new GarbledMessageError("INVALID_TAG_NUMBER", InnerEnum.InvalidTagNumber, "Invalid tag number", -1);

		public static readonly GarbledMessageError Field8TagExpected =
			new GarbledMessageError("FIELD_8_TAG_EXPECTED", InnerEnum.Field8TagExpected, "Tag expected", 8);

		public static readonly GarbledMessageError Field9TagExpected =
			new GarbledMessageError("FIELD_9_TAG_EXPECTED", InnerEnum.Field9TagExpected, "Tag expected", 9);

		public static readonly GarbledMessageError Field10TagExpected =
			new GarbledMessageError("FIELD_10_TAG_EXPECTED", InnerEnum.Field10TagExpected, "Tag expected", 10);

		public static readonly GarbledMessageError Field35TagExpected =
			new GarbledMessageError("FIELD_35_TAG_EXPECTED", InnerEnum.Field35TagExpected, "Tag expected", 35);

		public static readonly GarbledMessageError Field8TagValueDelimiterExpected =
			new GarbledMessageError("FIELD_8_TAG_VALUE_DELIMITER_EXPECTED",
				InnerEnum.Field8TagValueDelimiterExpected, "Tag/value delimiter expected", 8);

		public static readonly GarbledMessageError Field9TagValueDelimiterExpected =
			new GarbledMessageError("FIELD_9_TAG_VALUE_DELIMITER_EXPECTED",
				InnerEnum.Field9TagValueDelimiterExpected, "Tag/value delimiter expected", 9);

		public static readonly GarbledMessageError Field10TagValueDelimiterExpected =
			new GarbledMessageError("FIELD_10_TAG_VALUE_DELIMITER_EXPECTED",
				InnerEnum.Field10TagValueDelimiterExpected, "Tag/value delimiter expected", 10);

		public static readonly GarbledMessageError Field35TagValueDelimiterExpected =
			new GarbledMessageError("FIELD_35_TAG_VALUE_DELIMITER_EXPECTED",
				InnerEnum.Field35TagValueDelimiterExpected, "Tag/value delimiter expected", 35);

		public static readonly GarbledMessageError Field8FieldDelimiterExpected =
			new GarbledMessageError("FIELD_8_FIELD_DELIMITER_EXPECTED", InnerEnum.Field8FieldDelimiterExpected,
				"Field delimiter expected", 8);

		public static readonly GarbledMessageError Field10FieldDelimiterExpected =
			new GarbledMessageError("FIELD_10_FIELD_DELIMITER_EXPECTED", InnerEnum.Field10FieldDelimiterExpected,
				"Field delimiter expected", 10);

		public static readonly GarbledMessageError Field35FieldDelimiterExpected =
			new GarbledMessageError("FIELD_35_FIELD_DELIMITER_EXPECTED", InnerEnum.Field35FieldDelimiterExpected,
				"Field delimiter expected", 35);

		public static readonly GarbledMessageError Field9DecimalValueExpected =
			new GarbledMessageError("FIELD_9_DECIMAL_VALUE_EXPECTED", InnerEnum.Field9DecimalValueExpected,
				"Decimal value expected", 9);

		public static readonly GarbledMessageError Field10DecimalValueExpected =
			new GarbledMessageError("FIELD_10_DECIMAL_VALUE_EXPECTED", InnerEnum.Field10DecimalValueExpected,
				"Decimal value expected", 10);

		public static readonly GarbledMessageError Field10InvalidChecksum =
			new GarbledMessageError("FIELD_10_INVALID_CHECKSUM", InnerEnum.Field10InvalidChecksum,
				"Invalid checksum", 10);

		private static readonly IList<GarbledMessageError> ValueList = new List<GarbledMessageError>();
		private static int _nextOrdinal;
		private readonly string _nameValue;
		private readonly int _ordinalValue;

		public readonly InnerEnum InnerEnumValue;

		static GarbledMessageError()
		{
			ValueList.Add(InvalidTagNumber);
			ValueList.Add(Field8TagExpected);
			ValueList.Add(Field9TagExpected);
			ValueList.Add(Field10TagExpected);
			ValueList.Add(Field35TagExpected);
			ValueList.Add(Field8TagValueDelimiterExpected);
			ValueList.Add(Field9TagValueDelimiterExpected);
			ValueList.Add(Field10TagValueDelimiterExpected);
			ValueList.Add(Field35TagValueDelimiterExpected);
			ValueList.Add(Field8FieldDelimiterExpected);
			ValueList.Add(Field10FieldDelimiterExpected);
			ValueList.Add(Field35FieldDelimiterExpected);
			ValueList.Add(Field9DecimalValueExpected);
			ValueList.Add(Field10DecimalValueExpected);
			ValueList.Add(Field10InvalidChecksum);
		}

		internal GarbledMessageError(string name, InnerEnum innerEnum, string message, int refTagId)
		{
			Message = message;
			RefTagId = refTagId;

			_nameValue = name;
			_ordinalValue = _nextOrdinal++;
			InnerEnumValue = innerEnum;
		}

		public string Message { get; }

		public int RefTagId { get; }

		public static IList<GarbledMessageError> Values()
		{
			return ValueList;
		}

		public int Ordinal()
		{
			return _ordinalValue;
		}

		public override string ToString()
		{
			return _nameValue;
		}

		public static GarbledMessageError ValueOf(string name)
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

		public string Format(MsgBuf buf, int errorPosition)
		{
			var sb = new StringBuilder("Garbled message: ");
			sb.Append(Message);
			if (this == GarbledMessageError.Field10InvalidChecksum)
			{
				var expectedChecksum = RawFixUtil.GetChecksum(buf.Buffer, buf.Offset, buf.Length - 7);
				var gotChecksum = (int)FixTypes.ParseInt(buf.Buffer, buf.Offset + buf.Length - 4, 3);
				sb.Append(" - expected ").Append(expectedChecksum).Append(", got ").Append(gotChecksum);
			}
			sb.Append(" [Position: ").Append(errorPosition);
			var seqNum = RawFixUtil.GetRawValue(buf.Buffer, buf.Offset, buf.Length, 34);
			if (seqNum != null)
			{
				sb.Append(", RefSeqNum: ").Append(StringHelper.NewString(seqNum));
			}
			var msgType = RawFixUtil.GetRawValue(buf.Buffer, buf.Offset, buf.Length, 35);
			if (msgType != null)
			{
				sb.Append(", RefMsgType: ").Append(StringHelper.NewString(msgType));
			}
			sb.Append(", RefTagID: ").Append(RefTagId);
			sb.Append("]");
			sb.Append(":").Append(buf.ToMaskedString());
			return sb.ToString();
		}
	}
}
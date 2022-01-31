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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation.Entities;
using Epam.FixAntenna.NetCore.Validation.Error;
using Epam.FixAntenna.NetCore.Validation.Error.Resource;
using Epam.FixAntenna.NetCore.Validation.FixMessage;
using Epam.FixAntenna.NetCore.Validation.Utils;

namespace Epam.FixAntenna.NetCore.Validation.Validators
{
	internal class FieldsDefinitionsTypeValidator : AbstractValidator
	{
		/// <summary>
		/// Creates <c>FieldsDefinitionsTypeValidator</c>.
		/// </summary>
		/// <param name="util"> Fix Utils </param>
		public FieldsDefinitionsTypeValidator(FixUtil util) : base(util)
		{
		}

		/// <inheritdoc />
		public override FixErrorContainer Validate(string msgType, IValidationFixMessage message,
			bool isContentValidation)
		{
			var fixMessage = (ValidationFixMessage)message;
			var errors = new FixErrorContainer();
			string versionOfMessage = null;
			var fieldList = fixMessage.FullFixMessage;

			if (fieldList.IsTagExists(8))
			{
				versionOfMessage = fieldList.GetTagValueAsString(8);
			}

			double fixVersion = 0;
			if (versionOfMessage != null)
			{
				fixVersion = Convert.ToDouble(versionOfMessage.Substring(versionOfMessage.Length - 3), CultureInfo.InvariantCulture);
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

				if (tagValue.Length == 0)
				{
					errors.Add(FixErrorBuilder.BuildError(FixErrorCode.TagSpecifiedWithoutValue, tagValue,
						fixMessage.GetMsgSeqNumber(), msgType, tag));
				}
				else
				{
					var fieldsdef = Util.GetFieldDef();
					var fieldDefSize = fieldsdef.Length;
					var allTags = Util.GetAllTags();
					for (var fieldDefCount = 0; fieldDefCount < fieldDefSize; fieldDefCount++)
					{
						if (allTags[fieldDefCount] == tag)
						{
							var fielddef = fieldsdef[fieldDefCount];
							var type = (FixTypesEnum)Enum.Parse(typeof(FixTypesEnum),
								PrepareType(fielddef.Type), true);
							try
							{
								if (IsInvalidTypeValue(type, tagValue, fixVersion))
								{
									errors.Add(FixErrorBuilder.BuildError(
										FixErrorCode.IncorrectDataFormatForValue, tagValue, fixMessage.GetMsgSeqNumber(),
										msgType, tag));
								}
								else if (!IsCorrectValue(type, tagValue, fielddef))
								{
									errors.Add(FixErrorBuilder.BuildError(
										FixErrorCode.ValueIncorrectOutOfRangeForTag, tagValue,
										fixMessage.GetMsgSeqNumber(), msgType, tag));
								}
							}
							catch (ArgumentException ex)
							{
								errors.Add(FixErrorBuilder.BuildError(FixErrorCode.IncorrectDataFormatForValue,
									tagValue, ex.Message));
							}

							break;
						}
					}
				}
			}

			return errors;
		}

		private bool IsCorrectValue(FixTypesEnum type, TagValue tagValue, Fielddef fielddef)
		{
			switch (type)
			{
				case FixTypesEnum.Boolean:
					return !FixTypes.IsInvalidBoolean(tagValue);
				case FixTypesEnum.DayOfMonth:
					var dayOfMonth = FixTypes.ParseInt(tagValue);
					return dayOfMonth > 0 && dayOfMonth <= 31;
				default:
					return IsCorrectValue(tagValue, fielddef);
			}
		}

		private bool IsInvalidTypeValue(FixTypesEnum type, TagValue tagValue, double fixVersion)
		{
			switch (type)
			{
				case FixTypesEnum.Int:
				case FixTypesEnum.Length:
				case FixTypesEnum.NumInGroup:
				case FixTypesEnum.SeqNum:
				case FixTypesEnum.TagNum:
				case FixTypesEnum.DayOfMonth:
					return FixTypes.IsInvalidInt(tagValue);
				case FixTypesEnum.Float:
				case FixTypesEnum.Qty:
				case FixTypesEnum.Price:
				case FixTypesEnum.PriceOffset:
				case FixTypesEnum.Amt:
				case FixTypesEnum.Percentage:
					return FixTypes.IsInvalidFloat(tagValue);
				case FixTypesEnum.UtcTimestamp:
					return fixVersion == 4.0
						? FixTypes.IsInvalidTimestamp40(tagValue)
						: FixTypes.IsInvalidTimestamp(tagValue);
				case FixTypesEnum.TzTimestamp
					: // fixed bug 15088 Correct value of TZTimestamp considered as incorrect data type
					return FixTypes.IsInvalidTzTimestamp(tagValue);
				case FixTypesEnum.Time:
					return FixTypes.IsInvalidTime(tagValue);
				case FixTypesEnum.UtcTimeOnly:
					return FixTypes.isInvalidTimeOnly(tagValue);
				case FixTypesEnum.TzTimeOnly: // fixed bug [Bug 14775] New: TZTimeOnly validation should be implemented
					return FixTypes.isInvalidTZTimeOnly(tagValue);
				case FixTypesEnum.Date:
				case FixTypesEnum.UtcDate:
				case FixTypesEnum.UtcDateOnly:
				case FixTypesEnum.LocalMktDate:
					return FixTypes.IsInvalidDate(tagValue);
				case FixTypesEnum.MonthYear:
					// Fixed 15090: Correct value of Month-Year considered as incorrect data type.
					if (fixVersion == 4.0 || fixVersion == 4.1 || fixVersion == 4.2 || fixVersion == 4.3)
					{
						return FixTypes.IsInvalidMonthYear(tagValue);
					}
					else
					{
						return FixTypes.IsInvalidMonthYear44(tagValue);
					}

				case FixTypesEnum.Boolean:
					return FixTypes.IsInvalidBoolean(tagValue);
				case FixTypesEnum.Char:
					if (fixVersion == 4.0 || fixVersion == 4.1)
					{
						// no String type for this version.
						return false;
					}
					else
					{
						return tagValue.Length != 1;
					}

				case FixTypesEnum.String:
				case FixTypesEnum.MultipleValueString:
				case FixTypesEnum.MultipleStringValue:
				case FixTypesEnum.MultipleCharValue:
				case FixTypesEnum.Currency:
				case FixTypesEnum.Exchange:
				case FixTypesEnum.Data:
				case FixTypesEnum.Country:
				case FixTypesEnum.Language:
				case FixTypesEnum.XmlData:
					// check only some data and range
					return false;
				default:
					throw new ArgumentException("Invalid type of field");
			}
		}

		/// <summary>
		/// Verifies whether value is present in list of valid values
		/// </summary>
		/// <param name="tagValue">    Value of field </param>
		/// <param name="fielddef"> list of valid values </param>
		/// <returns> <c>true</c> if present, otherwise <c>false</c> </returns>
		private bool IsCorrectValue(TagValue tagValue, Fielddef fielddef)
		{
			//fixed bug 14817: Message which Contains tag with only spaces was not rejected
			IList<Item> items = fielddef.Item;
			var range = fielddef.Range;
			var valblock = fielddef.Valblock;
			var stringValue = tagValue.StringValue;
			var multi = fielddef.Multi;
			var isCorrect = false;
			// check null of all rules
			if ((items == null || items.Count == 0) && range == null && valblock == null && multi == null)
			{
				isCorrect = true;
			}
			else
			{
				if (items != null && items.Count > 0)
				{
					// check items values
					isCorrect = CheckItems(items, stringValue);
				}

				// compare multi values
				if (!isCorrect && multi != null && multi.Item.Count > 0)
				{
					isCorrect = CheckMulti(multi, stringValue);
				}

				// compare range
				if (range != null && !isCorrect)
				{
					isCorrect = CheckRange(tagValue, range);
				}

				if (valblock != null)
				{
					// check valblock
					var valblockdef = Util.GetValblockdef(valblock.GetIdref());
					IList list = valblockdef.ItemOrRangeOrDescr;
					foreach (var o in list)
					{
						if (o is Item)
						{
							var item = (Item)o;
							if (item.Val.Equals(stringValue))
							{
								isCorrect = true;
								break;
							}
						}

						if (o is Range rng && !isCorrect)
						{
							isCorrect = CheckRange(tagValue, rng);
						}

						if (o is Multi valBlockMulti && !isCorrect)
						{
							// fixed 15091 : Correct value of MultipleCharValue considered as incorrect data type
							isCorrect = CheckMulti(valBlockMulti, stringValue);
						}
					}
				}
			}

			return isCorrect;
		}

		/// <summary>
		/// find item in list
		/// </summary>
		/// <param name="items"> list </param>
		/// <param name="value"> item </param>
		private static bool CheckItems(IList<Item> items, string value)
		{
			if (items != null && items.Count > 0)
			{
				// check items values
				var size = items.Count;
				for (var i = 0; i < size; i++)
				{
					var item = items[i];
					if (item.Val.Equals(value))
					{
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// count delimeter ch in array of bytes
		/// </summary>
		/// <param name="values"> array of bytes </param>
		/// <param name="ch">     delimeter </param>
		private static int CountDelimeters(byte[] values, char ch)
		{
			var count = 1;
			for (var i = 0; i < values.Length; i++)
			{
				if ((char)values[i] == ch)
				{
					count++;
				}
			}

			return count;
		}

		private static bool CheckRange(TagValue tag, Range range)
		{
			// check range
			var isCorrect = false;
			if (!FixTypes.IsInvalidFloat(tag))
			{
				var doubleValue = FixTypes.ParseFloat(tag);
				isCorrect = doubleValue >= range.Minval && doubleValue <= range.Maxval;
			} 

			return isCorrect;
		}

		/// <summary>
		/// check multi element
		/// </summary>
		/// <param name="multi"> </param>
		/// <returns> true if valid </returns>
		internal static bool CheckMulti(Multi multi, string text)
		{
			IList<string> values =
				new List<string>(text.Split(new[] { ' ' }, CountDelimeters(text.AsByteArray(), ' ')).ToList());
			IList<Item> items = multi.Item;
			for (var i = 0; i < items.Count; i++)
			{
				var itemValue = items[i].Val;
				for (var j = values.Count - 1; j >= 0; j--)
				{
					var multiValue = values[j];
					if (multiValue.Equals(itemValue))
					{
						values.RemoveAt(j);
					}
				}
			}

			return values.Count <= 0;
		}

		/// <summary>
		/// Prepare string for exception
		/// </summary>
		/// <param name="tag">            Tag </param>
		/// <param name="sequenceNumber"> Sequence number of FIX message. </param>
		/// <param name="messageType">    Type of FIX message. </param>
		/// <returns> Formated error string </returns>
		private string PrepareMessage(int tag, long sequenceNumber, string messageType)
		{
			return ResourceHelper.GetStringMessage("INVALID_MESSAGE_INCORRECT_DATA_TYPE", sequenceNumber, messageType,
				tag);
		}

		/// <summary>
		/// Returns string without "-"
		/// </summary>
		/// <param name="type"> Input String </param>
		/// <returns> String without "-" </returns>
		private string PrepareType(string type)
		{
			var index = type.IndexOf("-", StringComparison.Ordinal);
			if (index != -1)
			{
				var builder = new StringBuilder(type);
				builder.Remove(index, 1);
				index = builder.ToString().IndexOf("-");
				if (index != -1)
				{
					builder.Remove(index, 1);
				}

				return builder.ToString().ToUpper();
			}

			return type.ToUpper();
		}
	}
}
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
using Epam.FixAntenna.NetCore.Validation.Exceptions.Validate;
using Epam.FixAntenna.NetCore.Validation.Validators.Condition.Container;
using Epam.FixAntenna.NetCore.Validation.Validators.Condition.Operators;

namespace Epam.FixAntenna.NetCore.Validation.Validators.Condition
{
	/// <summary>
	/// This class uses for parsing condition for conditionary required fields and generate some executable Condition
	/// implementation.
	/// <p/>
	/// Supporting comparing operators: "=", "!=", "&lt;", "&gt;", "in";
	/// Supporting logical operators: "not", "or", "and"
	/// Supporting searching operator: "existtags";
	/// Supporting special fake operator: "false"
	/// <p/>
	/// Examples of valid conditions:
	/// false
	/// existtags(T$91)
	/// T$28='C' or T$28='R'
	/// T$20 in ('1','2')
	/// T$59='GTD' and (not existtags(T$126))
	/// not (T$71=2 or T$626 in ('5', '7'))
	///
	/// @version $Revision: 1.20 $ $Date: 2008/05/06 15:27:24 $
	/// </summary>
	internal class ConditionValidateParser
	{
		private readonly string _condition;
		private bool _isGroupCondition;
		private readonly IList<ICondition> _stack = new List<ICondition>();

		/// <summary>
		/// Create parser and pass condition string for parsing.
		/// </summary>
		/// <param name="condition"> condition string. </param>
		public ConditionValidateParser(string condition)
		{
			_condition = condition.Trim();
			var endIndex = Parse(0);
			while (endIndex < _condition.Length)
			{
				endIndex = Parse(endIndex);
			}

			if (_stack.Count > 1)
			{
				throw new ConditionParserException("Invalid condition: " + condition);
			}
		}

		/// <summary>
		/// Returns full list of classes of conditional operator by input string
		/// </summary>
		/// <returns> full list of classes of conditional operator </returns>
		public virtual ICondition GetCondition()
		{
			return _stack[0];
		}

		/// <summary>
		/// Build operator starting from given position
		/// </summary>
		/// <param name="startPos"> Strat position for parsing of condition </param>
		/// <returns> position, from which should be processed next instruction. </returns>
		private int Parse(int startPos)
		{
			//skip whitespaces
			while (startPos < _condition.Length && char.IsWhiteSpace(_condition[startPos]))
			{
				startPos++;
			}

			if (StartWith(_condition, Utils.Constants.FalseWord, 0))
			{
				_stack.Add(new FalseValidateOperator(_isGroupCondition));
				return startPos + Utils.Constants.FalseWord.Length;
			}

			if (StartWith(_condition, Utils.Constants.ExisttagWord, startPos))
			{
				return BuildExistTagCondition(startPos);
			}

			if (StartWith(_condition, Utils.Constants.TagIdent, startPos))
			{
				return BuildComparisonOperator(startPos, _condition);
			}

			if (StartWith(_condition, Utils.Constants.GroupIdent, startPos))
			{
				_isGroupCondition = true;
				return BuildGroupComparisonOperator(startPos);
			}

			if (StartWith(_condition, Utils.Constants.OrWord, startPos))
			{
				return BuildOrOperator(startPos);
			}

			if (StartWith(_condition, Utils.Constants.AndWord, startPos))
			{
				return BuildAndOperator(startPos);
			}

			if (StartWith(_condition, Utils.Constants.NotWord, startPos))
			{
				return BuildNotOperator(startPos);
			}

			if (_condition[startPos] == '(')
			{
				//detected block
				var endPos = Parse(startPos + 1);
				var foundPos = _condition.IndexOf(")", endPos, StringComparison.Ordinal);
				while (_condition.Substring(endPos, foundPos - endPos).Trim().Length > 0)
				{
					endPos = Parse(endPos);
					foundPos = _condition.IndexOf(")", endPos, StringComparison.Ordinal);
				}

				if (foundPos == -1)
				{
					throw new ConditionParserException("Invalid condition: " + _condition);
				}

				return foundPos + 1;
			}

			throw new ConditionParserException("Invalid condition: " + _condition);
		}

		private int BuildExistTagCondition(int startPos)
		{
			var endPos = FindParametersEnd(startPos);
			var singleCondition = _condition.Substring(startPos, endPos - startPos);

			var startParamPos = singleCondition.IndexOf('(') + 1;
			var endParamPos = singleCondition.IndexOf(')');
			var isGroup = _condition.IndexOf(Utils.Constants.GroupIdent) > 0;
			if (isGroup)
			{
				_isGroupCondition = true;
			}

			_stack.Add(new ExistTagsValidateOperator(
				ParseTags(singleCondition.Substring(startParamPos, endParamPos - startParamPos)), _isGroupCondition));
			return isGroup ? endPos + 1 : endPos;
		}

		private int BuildNotOperator(int startPos)
		{
			var endPos = Parse(startPos + 3);
			if (_stack.Count < 1)
			{
				throw new ConditionParserException("Invalid condition: " + _condition);
			}

			var condition = _stack[_stack.Count - 1];
			_stack.RemoveAt(_stack.Count - 1);
			_stack.Add(new NotValidateOperator(condition, _isGroupCondition));
			return endPos;
		}

		private int BuildAndOperator(int startPos)
		{
			var endPos = Parse(startPos + 3);
			if (_stack.Count < 2)
			{
				throw new ConditionParserException("Invalid condition: " + _condition);
			}

			var condition2 = _stack[_stack.Count - 1];
			_stack.RemoveAt(_stack.Count - 1);
			var condition1 = _stack[_stack.Count - 1];
			_stack.RemoveAt(_stack.Count - 1);
			_stack.Add(new AndValidateOperator(condition1, condition2, _isGroupCondition));
			return endPos;
		}

		private int BuildOrOperator(int startPos)
		{
			var endPos = Parse(startPos + 2);
			if (_stack.Count < 2)
			{
				throw new ConditionParserException("Invalid condition: " + _condition);
			}

			var condition2 = _stack[_stack.Count - 1];
			_stack.RemoveAt(_stack.Count - 1);
			var condition1 = _stack[_stack.Count - 1];
			_stack.RemoveAt(_stack.Count - 1);
			_stack.Add(new OrValidateOperator(condition1, condition2, _isGroupCondition));
			return endPos;
		}

		private int BuildComparisonOperator(int startPos, string condition)
		{
			var startTag = startPos + 2;
			var pointer = startTag;

			// Get tag id from condition string
			while (pointer < condition.Length && char.IsDigit(condition[pointer]))
			{
				pointer++;
			}

			var tagId = int.Parse(condition.Substring(startTag, pointer - startTag));
			// end

			while (pointer < condition.Length && char.IsWhiteSpace(condition[pointer]))
			{
				pointer++;
			}

			var condPart = condition.Substring(pointer, condition.Length - pointer).Trim();
			if (condPart.StartsWith("=", StringComparison.Ordinal))
			{
				pointer = UpdatePointer(pointer, 1);
				return BuildEqOperator(tagId, pointer, condition);
			}

			if (condPart.StartsWith("!=", StringComparison.Ordinal))
			{
				pointer = UpdatePointer(pointer, 2);
				return BuildNotEquanExpresion(tagId, pointer, condition);
			}

			if (condPart.StartsWith(">", StringComparison.Ordinal))
			{
				pointer = UpdatePointer(pointer, 1);
				return BuildGreatThanOperator(tagId, pointer, condition);
			}

			if (condPart.StartsWith("<", StringComparison.Ordinal))
			{
				pointer = UpdatePointer(pointer, 1);
				return BuildLessThanOperator(tagId, pointer, condition);
			}

			if (condPart.StartsWith("in", StringComparison.Ordinal))
			{
				return BuildInOperator(tagId, startPos, condition);
			}

			//by default, definition of tag without any operations means the same as "existtags" operator
			_stack.Add(new ExistTagsValidateOperator(new[] { tagId }, _isGroupCondition));
			return pointer;
		}

		/// <summary>
		/// Increases incoming Pointer to the number of spaces after the significant characters
		/// </summary>
		/// <param name="pointer">        Incoming pointer </param>
		/// <param name="countOfSymbols"> Count of significant characters </param>
		/// <returns> Increased incoming Pointer </returns>
		private int UpdatePointer(int pointer, int countOfSymbols)
		{
			var conditionLength = _condition.Length;
			var tempPointer = pointer + countOfSymbols;
			for (var count = 0; count < conditionLength; count++)
			{
				if (char.ConvertToUtf32(_condition, tempPointer) == ' ')
				{
					++tempPointer;
				}
				else
				{
					break;
				}
			}

			return tempPointer - countOfSymbols;
		}

		private int BuildGroupComparisonOperator(int startPos)
		{
			var startTagPosition = startPos + 2;
			var carriage = startTagPosition;
			// gets tag id from string of condition
			while (carriage < _condition.Length && char.IsDigit(_condition[carriage]))
			{
				carriage++;
			}

			//        end
			while (carriage < _condition.Length && char.IsWhiteSpace(_condition[carriage]))
			{
				carriage++;
			}

			// delete (for tag)
			var condPart = _condition.Substring(carriage, _condition.Length - carriage).Trim();
			var container = PrepareGroupTags(condPart, 0);
			var tagCarriageResult = BuildComparisonOperator(0, container.GetCondition());
			// TODO
			//        stack.add(new ExistTagsValidateOperator(new Integer[]{tagId}));
			return container.GetCarriage() + carriage + tagCarriageResult;
			//        }
		}

		/// <summary>
		/// Prepare input condition for build correct tags condition
		/// </summary>
		/// <param name="condPart"> Part of condition </param>
		/// <param name="carriage"> Carriage for start of parsing </param>
		/// <returns> instance  of ConditionParserContainer that contains condition and carriage </returns>
		private ConditionParserContainer PrepareGroupTags(string condPart, int carriage)
		{
			var stringLength = condPart.Trim().Length;
			var tempCarriage = carriage;
			var builder = new StringBuilder(condPart.Trim());
			for (var index = carriage; index < stringLength; index++)
			{
				// created delta index for position carriage in string builder
				var deltaIndex = index - GetDelta(carriage, tempCarriage);
				var charOfString = builder[deltaIndex];
				// check if char equals '(' if true delete this char from string
				if (charOfString == '(')
				{
					builder.Remove(deltaIndex, 1);
					carriage++;
				}
				// check if char equals ')' if true delete this char from string
				else if (charOfString == ')')
				{
					builder.Remove(deltaIndex, 1);
					carriage++;
				}
				// check if carriage in Whitespace if true breaks of loop
				else if (char.IsWhiteSpace(charOfString))
				{
					break;
				}
			}

			return new ConditionParserContainer(builder.ToString(), carriage);
		}

		/// <summary>
		/// Returns delta of new and old carriages
		/// </summary>
		/// <param name="carriage">     new carriage </param>
		/// <param name="tempCarriage"> old carriage </param>
		/// <returns> delta of carriages </returns>
		private int GetDelta(int carriage, int tempCarriage)
		{
			return carriage - tempCarriage;
		}

		private int BuildEqOperator(int tagId, int valueStartPos, string condition)
		{
			var valueEndPos = FindValueEnd(valueStartPos + 1, condition);
			var valueStr = GetValue(valueStartPos + 1, valueEndPos, condition);
			_stack.Add(new EqValidateOperator(tagId, valueStr, _isGroupCondition));
			return valueEndPos;
		}

		private int BuildNotEquanExpresion(int tagId, int valueStartPos, string condition)
		{
			var valueEndPos = FindValueEnd(valueStartPos + 2, condition);
			var valueStr = GetValue(valueStartPos + 2, valueEndPos, condition);
			_stack.Add(new NotValidateOperator(new EqValidateOperator(tagId, valueStr, _isGroupCondition),
				_isGroupCondition));
			return valueEndPos;
		}

		private int BuildGreatThanOperator(int tagId, int valueStartPos, string condition)
		{
			var valueEndPos = FindValueEnd(valueStartPos + 1, condition);
			var valueStr = GetValue(valueStartPos + 1, valueEndPos, condition);
			int intValue;
			try
			{
				intValue = int.Parse(valueStr);
			}
			catch (FormatException)
			{
				throw new ConditionParserException("Invalid value for condition: " + _condition);
			}

			_stack.Add(new GreatThanValidateOperator(tagId, intValue, _isGroupCondition));
			return valueEndPos;
		}

		private int BuildLessThanOperator(int tagId, int valueStartPos, string condition)
		{
			var valueEndPos = FindValueEnd(valueStartPos + 1, condition);
			var valueStr = GetValue(valueStartPos + 1, valueEndPos, condition);
			int intValue;
			try
			{
				intValue = int.Parse(valueStr);
			}
			catch (FormatException)
			{
				throw new ConditionParserException("Invalid value for condition: " + _condition);
			}

			_stack.Add(new LessThanValidateOperator(tagId, intValue, _isGroupCondition));
			return valueEndPos;
		}

		private int BuildInOperator(int tagId, int startPos, string condition)
		{
			var endPos = FindParametersEnd(startPos);
			var values = GetInParams(startPos);

			_stack.Add(new InValidateOperator(tagId, values, _isGroupCondition));
			return endPos;
		}

		private string GetValue(int valueStartPos, int valueEndPos, string condition)
		{
			var valueStr = condition.Substring(valueStartPos, valueEndPos - valueStartPos);
			valueStr = PrepareValue(valueStr);
			return valueStr;
		}

		private string PrepareValue(string valueStr)
		{
			valueStr = valueStr.Trim();
			if (valueStr.StartsWith("'", StringComparison.Ordinal))
			{
				//cut leading and trailing '
				valueStr = valueStr.Substring(0, valueStr.Length - 1).Substring(1);
			}

			return valueStr;
		}

		private string[] GetInParams(int startPos)
		{
			var paramStartPos = _condition.IndexOf('(', startPos) + 1;
			var paramEndPos = _condition.IndexOf(')', startPos);
			var paramString = _condition.Substring(paramStartPos, paramEndPos - paramStartPos);
			var @params = paramString.Split(",", true);
			var values = new string[@params.Length];
			for (var i = 0; i < @params.Length; i++)
			{
				values[i] = PrepareValue(@params[i]);
			}

			return values;
		}

		private int FindParametersEnd(int startPos)
		{
			var foundPos = _condition.IndexOf('(', startPos);
			if (foundPos == -1)
			{
				throw new ConditionParserException("Can't separate parameters, required '(': " + _condition);
			}

			foundPos = _condition.IndexOf(')', foundPos);

			if (foundPos == -1)
			{
				throw new ConditionParserException("Can't separate parameters, required ')': " + _condition);
			}

			return foundPos + 1;
		}

		private int FindValueEnd(int startPos, string condition)
		{
			var whitespacePos = condition.IndexOf(' ', startPos);
			var bracketPos = condition.IndexOf(')', startPos);

			var endPos = Math.Min(whitespacePos, bracketPos);
			if (endPos == -1)
			{
				endPos = Math.Max(whitespacePos, bracketPos);
			}

			while (endPos != -1 && condition.Substring(startPos, endPos - startPos).Trim().Length == 0)
			{
				endPos = condition.IndexOf(' ', startPos);
			}

			if (endPos == -1)
			{
				return condition.Length;
			}

			return endPos;
		}

		private int[] ParseTags(string operands)
		{
			var tagsStr = operands.Split(",", true);
			var res = new List<int>();
			foreach (var aTagsStr in tagsStr)
			{
				var tag = aTagsStr.Trim();
				if (tag.StartsWith(Utils.Constants.TagIdent, StringComparison.Ordinal))
				{
					try
					{
						res.Add(int.Parse(tag.Substring(2, tag.Length - 2)));
					}
					catch (FormatException)
					{
						throw new ConditionParserException("Invalid tag definition '" + tag + "': " + operands);
					}
				}
				else if (tag.StartsWith(Utils.Constants.GroupIdent, StringComparison.Ordinal))
				{
					try
					{
						var startTagInGroup = tag.IndexOf("(", StringComparison.Ordinal);
						res.Add(int.Parse(tag.Substring(2, startTagInGroup - 2)));
						if (tag.IndexOf(Utils.Constants.TagIdent) > 0)
						{
							res.Add(int.Parse(tag.Substring(startTagInGroup + 3, tag.Length - (startTagInGroup + 3))));
						}
					}
					catch (FormatException)
					{
						throw new ConditionParserException("Invalid tag definition '" + tag + "': " + operands);
					}
				}
				else
				{
					throw new ConditionParserException("Invalid tag definition '" + tag + "': " + operands);
				}
			}

			return ((List<int>)res).ToArray();
		}

		/// <summary>
		/// return true if <b>string</b> Contains <b>prefix</b> from <b>startPosition</b>,
		/// otherwise false
		/// </summary>
		/// <param name="string"> </param>
		/// <param name="prefix"> </param>
		/// <param name="startPosition"> </param>
		private static bool StartWith(string @string, string prefix, int startPosition)
		{
			if (@string.Length < prefix.Length)
			{
				return false;
			}

			for (var i = 0; i < prefix.Length; i++)
			{
				var stringChar = @string[startPosition + i];
				var prefixChar = prefix[i];
				if (char.ToLower(stringChar) != char.ToLower(prefixChar))
				{
					return false;
				}
			}

			return true;
		}
	}
}
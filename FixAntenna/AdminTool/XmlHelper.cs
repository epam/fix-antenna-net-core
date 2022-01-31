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
using System.Text.RegularExpressions;

namespace Epam.FixAntenna.AdminTool
{
	internal class XmlHelper
	{
		/// <summary>
		/// Returns the string where all non-ascii and &lt;, &amp;, > are encoded as numeric entities.
		/// I.e. "&lt;A &amp; B &gt;"  .... (insert result here).
		/// The result is safe to include anywhere in a text field in an XML-string.
		/// If there was no characters to protect, the original string is returned.
		/// </summary>
		/// <param name="originalUnprotectedString">
		/// original string which may contain characters either reserved in XML or with different
		/// representation in different encodings (like 8859-1 and UFT-8)
		/// </param>
		public static string ProtectSpecialCharacters(string originalUnprotectedString)
		{
			if (string.IsNullOrWhiteSpace(originalUnprotectedString))
			{
				return null;
			}

			var anyCharactersProtected = false;
			var stringBuffer = new StringBuilder();

			for (var i = 0; i < originalUnprotectedString.Length; i++)
			{
				var ch = originalUnprotectedString[i];
				var controlCharacter = ch < (char)32;
				var unicodeButNotAscii = ch > (char)126;
				var characterWithSpecialMeaningInXml = ch == '<' || ch == '&' || ch == '>';

				if (characterWithSpecialMeaningInXml || unicodeButNotAscii || controlCharacter)
				{
					stringBuffer.Append($"&#{(int)ch};");
					anyCharactersProtected = true;
				}
				else
				{
					stringBuffer.Append(ch);
				}
			}
			if (anyCharactersProtected == false)
			{
				return originalUnprotectedString;
			}

			return stringBuffer.ToString();
		}

		public static string RestoreSpecialCharacters(string xml)
		{
			var xmlEntityRegex = new Regex("&(#?)([^&;]+);");
			var evaluator = new MatchEvaluator(MatchReplacer);
			return xmlEntityRegex.Replace(xml, evaluator);
		}

		private static string MatchReplacer(Match match)
		{
			var m1 = match.Groups[1];
			var m2 = match.Groups[2];
			if (m1.Success && m1.Length > 0)
			{
				return int.TryParse(m2.Value, out var code) ? $"{(char)code}" : match.Value;
			}
			else
			{
				return BuiltinEntities.TryGetValue(m2.Value, out var ch) ? ch : match.Value;
			}
		}

		private static readonly Dictionary<string, string> BuiltinEntities = new Dictionary<string, string>(5)
		{
			{"lt", "<"},
			{"gt", ">"},
			{"amp", "&"},
			{"apos", "'"},
			{"quot", "\""}
		};
	}
}
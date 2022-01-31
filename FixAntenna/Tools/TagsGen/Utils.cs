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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Epam.FixAntenna.NetCore.Validation.Entities;

namespace Epam.FixAntenna.TagsGen
{
	/// <summary>
	/// Helper methods for generator.
	/// </summary>
	internal static class Utils
	{
		private const string IllegalCharacters = "[!%{}()<>=@#.$&?*'\"=_/+/^/-]";
		private const string IllegalCharactersRegexp = ".*" + IllegalCharacters + ".*";
		private const int MaxWords = 7;

		/// <summary>
		/// Pascalize the string.
		/// </summary>
		/// <param name="value">Input string</param>
		/// <returns></returns>
		public static string Pascalize(this string value)
		{
			var rx = new Regex(@"(?:[^a-zA-Z0-9]*)(?<first>[a-zA-Z0-9])(?<reminder>[a-zA-Z0-9]*)(?:[^a-zA-Z0-9]*)");
			return rx.Replace(value, m => m.Groups["first"].ToString().ToUpper() + m.Groups["reminder"].ToString().ToLower());
		}

		/// <summary>
		/// Adds quotes in case if value not integer
		/// </summary>
		/// <param name="value">Input value</param>
		/// <returns></returns>
		public static string Prepare(object value)
		{
			return value is int ? value.ToString() : $"\"{value}\"";
		}

		/// <summary>
		/// Adds "Value" prefix if provided string starts with digit char.
		/// </summary>
		/// <param name="value">Input value.</param>
		/// <returns></returns>
		public static string SafeDigits(this string value)
		{
			// Prepend with "Value" prefix in case when value starts with digit char
			if (!string.IsNullOrEmpty(value) && value[0] >= '0' && value[0] <= '9')
			{
				return "Value" + value;
			}

			return value;
		}

		/// <summary>
		/// Extracts item`s description from content element.
		/// </summary>
		/// <param name="content"></param>
		/// <returns></returns>
		public static string ExtractDescription(List<object> content)
		{
			// take strings in one line
			var description = JoinToString(content).Replace("\n", " ").Replace("\r", " ");
			// drop any HTML tags
			description = Regex.Replace(description, "<.*?>", " ");
			// drop extra spaces
			return Regex.Replace(description, "[ ]{2,}", " ").Trim();
		}

		/// <summary>
		/// Extracts Name of the item.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public static string ExtractName(Item item)
		{
			if (string.IsNullOrEmpty(item.Id))
			{
				var descrToName = GetNameFromContent(JoinToString(item.Content));
				if (descrToName == null)
				{
					var valToName = "Value_" + item.Val;
					Console.WriteLine($" {valToName} will be used instead.");
					return valToName;
				}
				else
				{
					return descrToName;
				}
			}
			return item.Id;
		}

		private static string JoinToString(List<object> content)
		{
			var str = new StringBuilder();
			foreach (var part in content)
			{
				str.Append(" " + (part is XmlElement xml ? xml.InnerText : part.ToString()));
			}
			return str.ToString().Trim();
		}

		private static string GetNameFromContent(string content)
		{
			if (!ValidateNameFromDescription(content, out _))
			{
				// trying to fix validation errors
				// 1. shorten the word count till first '('
				if (content.Contains('('))
					content = content.Substring(0, content.IndexOf('('));
				// 2. replace illegal characters
				content = Regex.Replace(content, IllegalCharacters, string.Empty);
			}

			if (!ValidateNameFromDescription(content, out var reason))
			{
				Console.Write($"Warning: cannot create name from description. {reason}");
				return null;
			}

			var arrayWords = SplitString(content);
			var result = "";
			if (arrayWords.Length > MaxWords)
			{
				return result;
			}
			foreach (var word in arrayWords)
			{
				if (result.Length > 0)
				{
					result += "_";
				}
				result += word.ToUpper();
			}
			return result;
		}

		private static bool ValidateNameFromDescription(string content, out string reason)
		{
			if (string.IsNullOrEmpty(content))
			{
				reason = "Description is empty";
				return false;
			}
			if (Regex.IsMatch(content, IllegalCharactersRegexp))
			{
				reason = "Description contains illegal characters. Description: " + content;
				return false;
			}
			if ((content[0] >= '0' && content[0] <= '9'))
			{
				reason = "Description starts from number. Description: " + content;
				return false;
			}
			var arrayWords = SplitString(content);
			if (arrayWords.Length > MaxWords)
			{
				reason = "Description is longer than " + MaxWords + " words. Description: " + content;
				return false;
			}

			reason = string.Empty;
			return true;
		}

		private static string[] SplitString(string content)
		{
			var parts = content.Split(' ', '\t', '\n', '\r', ',', '.');
			return parts;
		}

		/// <summary>
		/// Generated dictionary`s description.
		/// </summary>
		/// <param name="dictionary"></param>
		/// <returns></returns>
		public static string GetDictionaryDescription(Fixdic dictionary)
		{
			return $"ID: {dictionary.Id}, Title: {dictionary.Title}, Date: {dictionary.Date}";
		}

		/// <summary>
		/// Prepare IEnumerable list of the input files.
		/// </summary>
		/// <param name="options">TagsGen starting parameters.</param>
		/// <returns></returns>
		public static IEnumerable<string> GetInputFiles(CommandLineOptions options)
		{
			var first = options.InputFiles.First();
			var fi = new FileInfo(first);
			if (first.Contains('*') || first.Contains('?'))
			{
				return Directory.GetFiles(fi.DirectoryName, fi.Name);
			}

			return options.InputFiles;
		}
	}
}

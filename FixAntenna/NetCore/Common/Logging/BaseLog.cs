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

namespace Epam.FixAntenna.NetCore.Common.Logging
{
	internal class BaseLog
	{
		private const string PasswordMatch = "Password=";
		internal const string Asterisks = "***";
		internal const string NullString = "(null)";

		/// <summary>
		/// Returns string representation of the input object using default object.ToString().
		/// </summary>
		/// <param name="obj">Input object.</param>
		/// <returns>String representation of the input object.</returns>
		public static string ToMessage(object obj)
		{
			if (obj == null)
				return NullString;

			return ToSafeString(obj.ToString());
		}

		/// <summary>
		/// This method hides quoted password value from input string.
		/// </summary>
		/// <remarks>Story BBP-17118: Hide passwords in application logs.</remarks>
		/// <param name="msg">Input object.</param>
		/// <param name="startIndex">Start index to search 'Password=' string, default value 0.</param>
		/// <returns>String with password replaced with asterisks.</returns>
		private static string ToSafeString(string msg, int startIndex = 0)
		{
			while (true)
			{
				var passwordIndex = msg.IndexOf(PasswordMatch, startIndex, StringComparison.Ordinal);

				if (passwordIndex < startIndex)
					return msg;

				var firstQuoteIndex = passwordIndex + PasswordMatch.Length;
				var quote = msg[firstQuoteIndex];
				var secondQuoteIndex = msg.IndexOf(quote, firstQuoteIndex + 1);

				if (secondQuoteIndex - firstQuoteIndex < 1)
					return msg;

				var result = msg.Remove(firstQuoteIndex + 1, secondQuoteIndex - firstQuoteIndex - 1)
					.Insert(firstQuoteIndex + 1, Asterisks);

				msg = result;
				startIndex = firstQuoteIndex + Asterisks.Length + 2;
			}
		}
	}
}

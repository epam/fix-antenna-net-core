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

namespace Epam.FixAntenna.NetCore.Message
{
	internal class FixMessagePrintableFormatter
	{
		internal const string DelimiterPropertyName = "fixPrintableDelimiter";
		private const string DefaultDelimiter = " | ";
		private const string Soh = "\u0001";

		private static readonly string Delimiter =
			string.IsNullOrEmpty(Environment.GetEnvironmentVariable(DelimiterPropertyName))
				? DefaultDelimiter
				: Environment.GetEnvironmentVariable(DelimiterPropertyName);

		public static string ToPrintableString(string msg)
		{
			return msg.Replace(Soh, Delimiter);
		}
	}
}
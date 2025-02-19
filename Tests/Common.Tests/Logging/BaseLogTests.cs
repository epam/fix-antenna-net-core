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
using Epam.FixAntenna.NetCore.Common.Logging;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Common.Tests.Logging
{
	[TestFixture, Property("Story", "https://jira.epam.com/jira/browse/BBP-17118")]
	public class BaseLogTests
	{
		private const string RowWithPassword ="Some string with Password={0}42{0} and message.";
		private const string RowWithPasswordExpected = "Some string with Password={0}***{0} and message.";

		private const string RowWithTwoPasswords = "Some string with Password={0}42{0}, message and other Password={0}4444{0} .";
		private const string RowWithTwoPasswordsExpected = "Some string with Password={0}***{0}, message and other Password={0}***{0} .";

		private const string SpecialCase1 = @" Password=""Logon_20150326 - 15:18:12"" Password=""summer1!"" Password=""Logon_20150326 - 15:18:12"" ";
		private const string SpecialCase1Expected = @" Password=""***"" Password=""***"" Password=""***"" ";

		private const string SpecialCase2 = @" P=""Logon_20150326 - 15:18:12"" Pd=""summer1!"" Password=""Logon_20150326 - 15:18:12"" ";
		private const string SpecialCase2Expected = @" P=""Logon_20150326 - 15:18:12"" Pd=""summer1!"" Password=""***"" ";

		private const string RowWithEmptyPassword = "Some string with Password={0}{0} and message.";
		private const string RowWithEmptyPasswordExpected = "Some string with Password={0}***{0} and message.";

		private const string RowWithoutPassword = "String.";
		private const string RowWithoutPasswordExpected = "String.";

		private static string[] Quotes = { "\'", "\"" };

		private Tuple<string, string>[] TestData = new[]
		{
			new Tuple<string, string>(RowWithPassword, RowWithPasswordExpected),
			new Tuple<string, string>(RowWithTwoPasswords, RowWithTwoPasswordsExpected),
			new Tuple<string, string>(RowWithEmptyPassword, RowWithEmptyPasswordExpected),
			new Tuple<string, string>(RowWithoutPassword, RowWithoutPasswordExpected),
			new Tuple<string, string>(SpecialCase1, SpecialCase1Expected),
			new Tuple<string, string>(SpecialCase2, SpecialCase2Expected)
		};

		private void PasswordHiddenClassicAssert(Func<string, string> method)
		{
			foreach (var tuple in TestData)
			{
				foreach (var quote in Quotes)
				{
					var source = string.Format(tuple.Item1, quote);
					var expected = string.Format(tuple.Item2, quote);
					var result = method(source);
					ClassicAssert.That(result, Is.EqualTo(expected));
				}
			}
		}

		[Test, Property("Implements", "https://jira.epam.com/jira/browse/BBP-17120")]
		public void BaseLogToMessageTest()
		{
			PasswordHiddenClassicAssert(BaseLog.ToMessage);
		}
	}
}

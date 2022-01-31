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
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Message.Format;
using NUnit.Framework;

namespace Epam.FixAntenna.Message.Tests.Validators
{
	internal class FixMessageUtcDateOnlyOperationsTest : AbstractGetTagValueAsXxxDateValidator
	{
		private static readonly string[] Dates = { "20130101", "20131231" };
		private static readonly string Sdf = "yyyyMMdd";
		private static readonly DateTimeOffset[] UtcCalendars = new DateTimeOffset[3];

		private static readonly FixDateFormatterFactory.FixDateType Format =
			FixDateFormatterFactory.FixDateType.UtcDateOnly;

		public FixMessageUtcDateOnlyOperationsTest()
		{
			UtcCalendars[0] = new DateTimeOffset(2013, 1, 1, 0, 0, 0, TimeSpan.Zero);
			UtcCalendars[1] = new DateTimeOffset(2013, 12, 31, 0, 0, 0, TimeSpan.Zero);
		}

		public override int GetMaxOccurrence(int messageMaxOccurrence)
		{
			return Math.Min(Dates.Length, messageMaxOccurrence);
		}

		public override string PrepareTagValueForRead(string ffl, int tagId, int occurrence)
		{
			return ReplaceFieldValue(tagId, occurrence, Dates[occurrence - 1], ffl);
		}

		[ExpectedExceptionOnFail(typeof(FieldNotFoundException))]
		public override void CheckGetter(FixMessage ffl, int tagId)
		{
			var actual = ffl.getTagValueAsDateOnly(tagId);
			var expected = GetCalendar(1);
			assertDateOnlyEquals(expected, actual);
		}

		[ExpectedExceptionOnFail(typeof(FieldNotFoundException))]
		public override void CheckGetterWithOccurrence(FixMessage ffl, int tagId, int occurrence)
		{
			DateTimeOffset actual = ffl.getTagValueAsDateOnly(tagId, occurrence);
			var expected = GetCalendar(occurrence);
			assertDateOnlyEquals(expected, actual);
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override void CheckGetterAtIndex(FixMessage ffl, int occurrence, int firstTagIndex)
		{
			DateTimeOffset actual = ffl.getTagValueAsDateOnlyAtIndex(firstTagIndex);
			var expected = GetCalendar(occurrence);
			assertDateOnlyEquals(expected, actual);
		}

		private DateTimeOffset GetCalendar(int occurrence)
		{
			return UtcCalendars[occurrence - 1];
		}

		private void assertDateOnlyEquals(DateTimeOffset expected, DateTimeOffset actual)
		{
			var diff = "Expected " + expected.ToString(Sdf) + " but get " + actual.ToString(Sdf);
			Assert.AreEqual(expected.Year, actual.Year, GetValidatorName() + "invalid year." + diff);
			Assert.AreEqual(expected.Month, actual.Month, GetValidatorName() + "invalid month. " + diff);
			Assert.AreEqual(expected.Day, actual.Day, GetValidatorName() + "invalid date. " + diff);
		}

		public override FixMessage AddTag(FixMessage msg, int tagId, int occurrence)
		{
			var cal = GetCalendar(occurrence);
			msg.AddCalendarTag(tagId, cal, Format);
			return msg;
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override FixMessage AddTagAtIndex(FixMessage msg, int index, int tagId, int occurrence)
		{
			var cal = GetCalendar(occurrence);
			msg.AddCalendarTagAtIndex(index, tagId, cal, Format);
			return msg;
		}

		public override FixMessage SetTag(FixMessage msg, int tagId, int occurrence)
		{
			var cal = GetCalendar(occurrence);
			msg.SetCalendarValue(tagId, cal, Format);
			return msg;
		}

		public override FixMessage SetTagWithOccurrence(FixMessage msg, int tagId, int occurrence)
		{
			var cal = GetCalendar(occurrence);
			msg.SetCalendarValue(tagId, occurrence, cal, Format);
			return msg;
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override FixMessage SetTagAtIndex(FixMessage msg, int index, int occurrence)
		{
			var cal = GetCalendar(occurrence);
			msg.SetCalendarValueAtIndex(index, cal, Format);
			return msg;
		}
	}
}
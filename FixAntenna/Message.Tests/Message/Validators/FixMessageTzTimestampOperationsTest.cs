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

namespace Epam.FixAntenna.Message.Tests.Validators
{
	internal class FixMessageTzTimestampOperationsTest : AbstractGetTagValueAsXxxDateValidator
	{
		private static readonly string[] Times =
			{ "19981231-07:39Z", "20130101-12:01:59.999Z", "19981231-15:39+08", "20130101-13:09+05:30" };

		private static readonly string[] WriteValues =
			{ "19981231-07:39:00Z", "20130101-12:01:59.999Z", "19981231-15:39:00+08", "20130101-13:09:00+05:30" };

		private static readonly DateTimeOffset[] Calendars = new DateTimeOffset[4];

		private static readonly string[] Sdfs =
		{
			"yyyyMMdd-HH:mmZ",
			"yyyyMMdd-HH:mm:ss.SSSZ",
			"yyyyMMdd-HH:mmZ",
			"yyyyMMdd-HH:mmZ"
		};

		private static readonly FixDateFormatterFactory.FixDateType[] Formats =
		{
			FixDateFormatterFactory.FixDateType.TzTimestamp,
			FixDateFormatterFactory.FixDateType.TzTimestampMillis,
			FixDateFormatterFactory.FixDateType.TzTimestamp, FixDateFormatterFactory.FixDateType.TzTimestamp
		};

		public FixMessageTzTimestampOperationsTest()
		{
			Calendars[0] = new DateTimeOffset(1998, 12, 31, 7, 39, 0, TimeSpan.Zero);
			Calendars[1] = new DateTimeOffset(2013, 1, 01, 12, 01, 59, 999, TimeSpan.Zero);
			Calendars[2] = new DateTimeOffset(1998, 12, 31, 15, 39, 0, TimeSpan.FromHours(8));
			Calendars[3] = new DateTimeOffset(2013, 1, 1, 13, 9, 0, TimeSpan.FromMinutes(330));
		}

		public override int GetMaxOccurrence(int messageMaxOccurrence)
		{
			return Math.Min(Times.Length, messageMaxOccurrence);
		}

		public override string PrepareTagValueForRead(string ffl, int tagId, int occurrence)
		{
			return ReplaceFieldValue(tagId, occurrence, Times[occurrence - 1], ffl);
		}

		public override string PrepareTagValueForCheckAfterWrite(string ffl, int tagId, int occurrence)
		{
			return ReplaceFieldValue(tagId, occurrence, WriteValues[occurrence - 1], ffl);
		}

		[ExpectedExceptionOnFail(typeof(FieldNotFoundException))]
		public override void CheckGetter(FixMessage ffl, int tagId)
		{
			var actual = ffl.GetTagValueAsTzTimestamp(tagId);
			AssertCalendarsEquals(1, actual);
		}

		[ExpectedExceptionOnFail(typeof(FieldNotFoundException))]
		public override void CheckGetterWithOccurrence(FixMessage ffl, int tagId, int occurrence)
		{
			var actual = ffl.GetTagValueAsTzTimestamp(tagId, occurrence);
			AssertCalendarsEquals(occurrence, actual);
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override void CheckGetterAtIndex(FixMessage ffl, int occurrence, int firstTagIndex)
		{
			var actual = ffl.GetTagValueAsTzTimestampAtIndex(firstTagIndex);
			AssertCalendarsEquals(occurrence, actual);
		}

		private DateTimeOffset GetCalendar(int occurrence)
		{
			return Calendars[occurrence - 1];
		}

		private void AssertCalendarsEquals(int occurrence, DateTimeOffset actual)
		{
			AssertCalendarsEquals(GetCalendar(occurrence), actual, Sdfs[occurrence - 1]);
		}

		public override FixMessage AddTag(FixMessage msg, int tagId, int occurrence)
		{
			var cal = GetCalendar(occurrence);
			msg.AddCalendarTag(tagId, cal, Formats[occurrence - 1]);
			return msg;
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override FixMessage AddTagAtIndex(FixMessage msg, int index, int tagId, int occurrence)
		{
			var cal = GetCalendar(occurrence);
			msg.AddCalendarTagAtIndex(index, tagId, cal, Formats[occurrence - 1]);
			return msg;
		}

		public override FixMessage SetTag(FixMessage msg, int tagId, int occurrence)
		{
			var cal = GetCalendar(occurrence);
			msg.SetCalendarValue(tagId, cal, Formats[occurrence - 1]);
			return msg;
		}

		public override FixMessage SetTagWithOccurrence(FixMessage msg, int tagId, int occurrence)
		{
			var cal = GetCalendar(occurrence);
			msg.SetCalendarValue(tagId, occurrence, cal, Formats[occurrence - 1]);
			return msg;
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override FixMessage SetTagAtIndex(FixMessage msg, int index, int occurrence)
		{
			var cal = GetCalendar(occurrence);
			msg.SetCalendarValueAtIndex(index, cal, Formats[occurrence - 1]);
			return msg;
		}
	}
}
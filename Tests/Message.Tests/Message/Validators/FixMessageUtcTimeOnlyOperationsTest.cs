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
	internal class FixMessageUtcTimeOnlyOperationsTest : AbstractGetTagValueAsXxxDateValidator
	{
		private static readonly string[] Times = { "23:59:59", "12:01:59.999" };

		private static readonly string[] Sdfs =
		{
			"HH:mm:ss",
			"HH:mm:ss.SSS"
		};

		private static readonly FixDateFormatterFactory.FixDateType[] Formats =
		{
			FixDateFormatterFactory.FixDateType.UtcTimeOnlyShort,
			FixDateFormatterFactory.FixDateType.UtcTimeOnlyWithMillis
		};

		private static readonly DateTimeOffset[] UtcCalendars = new DateTimeOffset[2];

		public FixMessageUtcTimeOnlyOperationsTest()
		{
			UtcCalendars[0] = new DateTimeOffset(1, 1, 1, 23, 59, 59, TimeSpan.Zero);
			UtcCalendars[1] = new DateTimeOffset(1, 1, 1, 12, 1, 59, 999, TimeSpan.Zero);
		}

		public override int GetMaxOccurrence(int messageMaxOccurrence)
		{
			return Math.Min(Times.Length, messageMaxOccurrence);
		}

		public override string PrepareTagValueForRead(string ffl, int tagId, int occurrence)
		{
			return ReplaceFieldValue(tagId, occurrence, Times[occurrence - 1], ffl);
		}

		[ExpectedExceptionOnFail(typeof(FieldNotFoundException))]
		public override void CheckGetter(FixMessage ffl, int tagId)
		{
			DateTimeOffset actual = ffl.getTagValueAsTimeOnly(tagId);
			AssertCalendarsEquals(1, actual);
		}

		[ExpectedExceptionOnFail(typeof(FieldNotFoundException))]
		public override void CheckGetterWithOccurrence(FixMessage ffl, int tagId, int occurrence)
		{
			DateTimeOffset actual = ffl.getTagValueAsTimeOnly(tagId, occurrence);
			AssertCalendarsEquals(occurrence, actual);
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override void CheckGetterAtIndex(FixMessage ffl, int occurrence, int firstTagIndex)
		{
			DateTimeOffset actual = ffl.getTagValueAsTimeOnlyAtIndex(firstTagIndex);
			AssertCalendarsEquals(occurrence, actual);
		}

		private DateTimeOffset GetCalendar(int occurrence)
		{
			return UtcCalendars[occurrence - 1];
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
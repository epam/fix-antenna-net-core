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
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Message.Tests.Validators
{
	internal class FixMessageHighPrecisionTzTimestampOperationsTest : AbstractGetTagValueAsXxxDateValidator
	{
		private static readonly string[] Times =
		{
			"20101231-12:12-05:30", "20101231-23:59:59Z", "20100101-12:01:59.123+05", "20101020-12:01:59.000001+05:30",
			"20101121-01:01:01.999999900Z"
		};

		private static readonly DateTimeOffset[] OffsetDateTimes = new DateTimeOffset[5];

		private static readonly TimestampPrecision[] Precisions =
		{
			TimestampPrecision.Minute, TimestampPrecision.Second, TimestampPrecision.Milli, TimestampPrecision.Micro,
			TimestampPrecision.Nano
		};

		public FixMessageHighPrecisionTzTimestampOperationsTest()
		{
			OffsetDateTimes[0] = new DateTimeOffset(2010, 12, 31, 12, 12, 0, 0, TimeSpan.FromMinutes(-330));
			OffsetDateTimes[1] = new DateTimeOffset(2010, 12, 31, 23, 59, 59, 0, TimeSpan.Zero);
			OffsetDateTimes[2] = new DateTimeOffset(2010, 1, 1, 12, 1, 59, 123, TimeSpan.FromMinutes(300));
			OffsetDateTimes[3] = new DateTimeOffset(2010, 10, 20, 12, 1, 59, 0, TimeSpan.FromMinutes(330)).AddTicks(10);
			OffsetDateTimes[4] = new DateTimeOffset(2010, 11, 21, 1, 1, 1, 0, TimeSpan.Zero).AddTicks(9999999);
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
			var actual = ffl.GetTagValueAsTzTimestamp(tagId);
			ClassicAssert.IsTrue(OffsetDateTimes[0].Equals(actual));
		}

		[ExpectedExceptionOnFail(typeof(FieldNotFoundException))]
		public override void CheckGetterWithOccurrence(FixMessage ffl, int tagId, int occurrence)
		{
			var actual = ffl.GetTagValueAsTzTimestamp(tagId, occurrence);
			ClassicAssert.IsTrue(OffsetDateTimes[occurrence - 1].Equals(actual));
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override void CheckGetterAtIndex(FixMessage ffl, int occurrence, int firstTagIndex)
		{
			var actual = ffl.GetTagValueAsTzTimestampAtIndex(firstTagIndex);
			ClassicAssert.IsTrue(OffsetDateTimes[occurrence - 1].Equals(actual));
		}

		public override FixMessage AddTag(FixMessage msg, int tagId, int occurrence)
		{
			msg.AddDateTimeTag(tagId, OffsetDateTimes[occurrence - 1], Precisions[occurrence - 1]);
			return msg;
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override FixMessage AddTagAtIndex(FixMessage msg, int index, int tagId, int occurrence)
		{
			msg.AddDateTimeTagAtIndex(index, tagId, OffsetDateTimes[occurrence - 1], Precisions[occurrence - 1]);
			return msg;
		}

		public override FixMessage SetTag(FixMessage msg, int tagId, int occurrence)
		{
			msg.SetDateTimeValue(tagId, OffsetDateTimes[occurrence - 1], Precisions[occurrence - 1]);
			return msg;
		}

		public override FixMessage SetTagWithOccurrence(FixMessage msg, int tagId, int occurrence)
		{
			msg.SetDateTimeValue(tagId, occurrence, OffsetDateTimes[occurrence - 1], Precisions[occurrence - 1]);
			return msg;
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override FixMessage SetTagAtIndex(FixMessage msg, int index, int occurrence)
		{
			msg.SetDateTimeValueAtIndex(index, OffsetDateTimes[occurrence - 1], Precisions[occurrence - 1]);
			return msg;
		}
	}
}
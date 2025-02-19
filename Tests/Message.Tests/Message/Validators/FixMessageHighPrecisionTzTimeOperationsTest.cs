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
	internal class FixMessageHighPrecisionTzTimeOperationsTest : AbstractGetTagValueAsXxxDateValidator
	{
		private static readonly string[] Times =
			{ "12:12-05:30", "23:59:59Z", "12:01:59.123+05", "12:01:59.000010+05:30", "01:01:01.999999900Z" };

		private static readonly DateTimeOffset[] OffsetTimes = new DateTimeOffset[5];

		private static readonly TimestampPrecision[] Precisions =
		{
			TimestampPrecision.Minute, TimestampPrecision.Second, TimestampPrecision.Milli, TimestampPrecision.Micro,
			TimestampPrecision.Nano
		};

		public FixMessageHighPrecisionTzTimeOperationsTest()
		{
			OffsetTimes[0] = new DateTimeOffset(1, 1, 1, 12, 12, 0, 0, TimeSpan.FromMinutes(-330));
			OffsetTimes[1] = new DateTimeOffset(1, 1, 1, 23, 59, 59, 0, TimeSpan.Zero);
			OffsetTimes[2] = new DateTimeOffset(1, 1, 1, 12, 1, 59, 123, TimeSpan.FromHours(5));
			OffsetTimes[3] = new DateTimeOffset(1, 1, 1, 12, 1, 59, 0, TimeSpan.FromMinutes(330)).AddTicks(100);
			OffsetTimes[4] = new DateTimeOffset(1, 1, 1, 1, 1, 1, 0, TimeSpan.Zero).AddTicks(9999999);
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
			var actual = ffl.getTagValueAsTZTimeOnly(tagId);
			ClassicAssert.IsTrue(OffsetTimes[0].Equals(actual));
		}

		[ExpectedExceptionOnFail(typeof(FieldNotFoundException))]
		public override void CheckGetterWithOccurrence(FixMessage ffl, int tagId, int occurrence)
		{
			var actual = ffl.getTagValueAsTZTimeOnly(tagId, occurrence);
			ClassicAssert.IsTrue(OffsetTimes[occurrence - 1].Equals(actual));
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override void CheckGetterAtIndex(FixMessage ffl, int occurrence, int firstTagIndex)
		{
			var actual = ffl.getTagValueAsTZTimeOnlyAtIndex(firstTagIndex);
			ClassicAssert.IsTrue(OffsetTimes[occurrence - 1].Equals(actual));
		}

		public override FixMessage AddTag(FixMessage msg, int tagId, int occurrence)
		{
			msg.AddTimeTag(tagId, OffsetTimes[occurrence - 1], Precisions[occurrence - 1]);
			return msg;
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override FixMessage AddTagAtIndex(FixMessage msg, int index, int tagId, int occurrence)
		{
			msg.AddTimeTagAtIndex(index, tagId, OffsetTimes[occurrence - 1], Precisions[occurrence - 1]);
			return msg;
		}

		public override FixMessage SetTag(FixMessage msg, int tagId, int occurrence)
		{
			msg.SetTimeValue(tagId, OffsetTimes[occurrence - 1], Precisions[occurrence - 1]);
			return msg;
		}

		public override FixMessage SetTagWithOccurrence(FixMessage msg, int tagId, int occurrence)
		{
			msg.SetTimeValue(tagId, occurrence, OffsetTimes[occurrence - 1], Precisions[occurrence - 1]);
			return msg;
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override FixMessage SetTagAtIndex(FixMessage msg, int index, int occurrence)
		{
			msg.SetTimeValueAtIndex(index, OffsetTimes[occurrence - 1], Precisions[occurrence - 1]);
			return msg;
		}
	}
}
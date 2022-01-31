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

namespace Epam.FixAntenna.Message.Tests.Validators
{
	internal class FixMessageHighPrecisionTimestampOperationsTest : AbstractGetTagValueAsXxxDateValidator
	{
		private static readonly string[] Times =
			{ "20101231-23:59:59", "20100101-12:01:59.123", "20101020-12:01:59.000001", "20101121-01:01:01.999999900" };

		private static readonly DateTime[] LocalDateTimes = new DateTime[4];

		private static readonly TimestampPrecision[] Precisions =
			{ TimestampPrecision.Second, TimestampPrecision.Milli, TimestampPrecision.Micro, TimestampPrecision.Nano };

		public FixMessageHighPrecisionTimestampOperationsTest()
		{
			LocalDateTimes[0] = new DateTime(2010, 12, 31, 23, 59, 59);
			LocalDateTimes[1] = new DateTime(2010, 1, 1, 12, 1, 59, 123);
			LocalDateTimes[2] = new DateTime(2010, 10, 20, 12, 1, 59, 0).AddTicks(10);
			LocalDateTimes[3] = new DateTime(2010, 11, 21, 1, 1, 1, 0).AddTicks(9999999);
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
			var actual = ffl.GetTagValueAsTimestamp(tagId);
			Assert.IsTrue(LocalDateTimes[0].Equals(actual));
		}

		[ExpectedExceptionOnFail(typeof(FieldNotFoundException))]
		public override void CheckGetterWithOccurrence(FixMessage ffl, int tagId, int occurrence)
		{
			var actual = ffl.GetTagValueAsTimestamp(tagId, occurrence);
			Assert.IsTrue(LocalDateTimes[occurrence - 1].Equals(actual));
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override void CheckGetterAtIndex(FixMessage ffl, int occurrence, int firstTagIndex)
		{
			var actual = ffl.GetTagValueAsTimestampAtIndex(firstTagIndex);
			Assert.AreEqual(LocalDateTimes[occurrence - 1], actual);
		}

		public override FixMessage AddTag(FixMessage msg, int tagId, int occurrence)
		{
			msg.AddDateTimeTag(tagId, LocalDateTimes[occurrence - 1], Precisions[occurrence - 1]);
			return msg;
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override FixMessage AddTagAtIndex(FixMessage msg, int index, int tagId, int occurrence)
		{
			msg.AddDateTimeTagAtIndex(index, tagId, LocalDateTimes[occurrence - 1], Precisions[occurrence - 1]);
			return msg;
		}

		public override FixMessage SetTag(FixMessage msg, int tagId, int occurrence)
		{
			msg.SetDateTimeValue(tagId, LocalDateTimes[occurrence - 1], Precisions[occurrence - 1]);
			return msg;
		}

		public override FixMessage SetTagWithOccurrence(FixMessage msg, int tagId, int occurrence)
		{
			msg.SetDateTimeValue(tagId, occurrence, LocalDateTimes[occurrence - 1], Precisions[occurrence - 1]);
			return msg;
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override FixMessage SetTagAtIndex(FixMessage msg, int index, int occurrence)
		{
			msg.SetDateTimeValueAtIndex(index, LocalDateTimes[occurrence - 1], Precisions[occurrence - 1]);
			return msg;
		}
	}
}
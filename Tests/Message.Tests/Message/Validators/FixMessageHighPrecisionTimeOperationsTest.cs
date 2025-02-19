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
	internal class FixMessageHighPrecisionTimeOperationsTest : AbstractGetTagValueAsXxxDateValidator
	{
		private static readonly string[] Times =
			{ "23:59:59", "12:01:59.123", "12:01:59.000001", "01:01:01.999999900" };

		private static readonly DateTime[] LocalTimes = new DateTime[4];

		private static readonly TimestampPrecision[] Precisions =
			{ TimestampPrecision.Second, TimestampPrecision.Milli, TimestampPrecision.Micro, TimestampPrecision.Nano };

		public FixMessageHighPrecisionTimeOperationsTest()
		{
			LocalTimes[0] = new DateTime(1, 1, 1, 23, 59, 59);
			LocalTimes[1] = new DateTime(1, 1, 1, 12, 1, 59, 123);
			LocalTimes[2] = new DateTime(1, 1, 1, 12, 1, 59).AddTicks(10);
			LocalTimes[3] = new DateTime(1, 1, 1, 1, 1, 1, 0).AddTicks(9999999);
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
			var actual = ffl.getTagValueAsTimeOnly(tagId);
			ClassicAssert.IsTrue(LocalTimes[0].Equals(actual));
		}

		[ExpectedExceptionOnFail(typeof(FieldNotFoundException))]
		public override void CheckGetterWithOccurrence(FixMessage ffl, int tagId, int occurrence)
		{
			var actual = ffl.getTagValueAsTimeOnly(tagId, occurrence);
			ClassicAssert.IsTrue(LocalTimes[occurrence - 1].Equals(actual));
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override void CheckGetterAtIndex(FixMessage ffl, int occurrence, int firstTagIndex)
		{
			var actual = ffl.getTagValueAsTimeOnlyAtIndex(firstTagIndex);
			ClassicAssert.IsTrue(LocalTimes[occurrence - 1].Equals(actual));
		}

		public override FixMessage AddTag(FixMessage msg, int tagId, int occurrence)
		{
			msg.AddTimeTag(tagId, LocalTimes[occurrence - 1], Precisions[occurrence - 1]);
			return msg;
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override FixMessage AddTagAtIndex(FixMessage msg, int index, int tagId, int occurrence)
		{
			msg.AddTimeTagAtIndex(index, tagId, LocalTimes[occurrence - 1], Precisions[occurrence - 1]);
			return msg;
		}

		public override FixMessage SetTag(FixMessage msg, int tagId, int occurrence)
		{
			msg.SetTimeValue(tagId, LocalTimes[occurrence - 1], Precisions[occurrence - 1]);
			return msg;
		}

		public override FixMessage SetTagWithOccurrence(FixMessage msg, int tagId, int occurrence)
		{
			msg.SetTimeValue(tagId, occurrence, LocalTimes[occurrence - 1], Precisions[occurrence - 1]);
			return msg;
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override FixMessage SetTagAtIndex(FixMessage msg, int index, int occurrence)
		{
			msg.SetTimeValueAtIndex(index, LocalTimes[occurrence - 1], Precisions[occurrence - 1]);
			return msg;
		}
	}
}
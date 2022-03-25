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
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework;

namespace Epam.FixAntenna.Message.Tests.Validators
{
	internal class GetTagValueAsByteWithOffsetTest : AbstractFixMessageGetterValidator
	{
		private static readonly string[] Values = { "abcd", "efgh" };
		public const int Offset = 3;

		public override int GetMaxOccurrence(int messageMaxOccurrence)
		{
			return Math.Min(Values.Length, messageMaxOccurrence);
		}

		public override string PrepareTagValueForRead(string ffl, int tagId, int occurrence)
		{
			return ReplaceFieldValue(tagId, occurrence, Values[occurrence - 1], ffl);
		}

		[ExpectedExceptionOnFail(typeof(FieldNotFoundException))]
		public override void CheckGetter(FixMessage ffl, int tagId)
		{
			var actual = ffl.GetTagValueAsByte(tagId, Offset);
			Assert.AreEqual(Values[0].AsByteArray()[Offset], actual,
				GetValidatorName() + "invalid value for getTagValueAsByte(" + tagId + ", " + Offset + ")");
		}

		[ExpectedExceptionOnFail(typeof(FieldNotFoundException))]
		public override void CheckGetterWithOccurrence(FixMessage ffl, int tagId, int occurrence)
		{
			var actual = ffl.GetTagValueAsByte(tagId, Offset, occurrence);
			Assert.AreEqual(Values[occurrence - 1].AsByteArray()[Offset], actual,
				GetValidatorName() + "invalid value for getTagValueAsByte(" + tagId + ", " + Offset + ", " +
				occurrence + ")");
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override void CheckGetterAtIndex(FixMessage ffl, int occurrence, int firstTagIndex)
		{
			var actual = ffl.GetTagValueAsByteAtIndex(firstTagIndex, Offset);
			Assert.AreEqual(Values[occurrence - 1].AsByteArray()[Offset], actual,
				GetValidatorName() + "invalid value for getTagValueAsByteAtIndex(" + firstTagIndex + ", " + Offset +
				")");
		}
	}
}
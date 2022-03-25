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
using System.Text;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework;

namespace Epam.FixAntenna.Message.Tests.Validators
{
	internal class FixMessageStringBuffOperationsTest : AbstractFixMessageGetAddSetValidator
	{
		private static readonly string[] Values = { "aaaa", "b" };

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
			var actual = new StringBuilder();
			ffl.GetTagValueAsStringBuff(tagId, actual);
			Assert.AreEqual(Values[0], actual.ToString(),
				GetValidatorName() + "invalid value for getTagValueAsStringBuff(" + tagId + ")");
		}

		[ExpectedExceptionOnFail(typeof(FieldNotFoundException))]
		public override void CheckGetterWithOccurrence(FixMessage ffl, int tagId, int occurrence)
		{
			var actual = new StringBuilder();
			ffl.GetTagValueAsStringBuff(tagId, actual, occurrence);
			Assert.AreEqual(Values[occurrence - 1], actual.ToString(),
				GetValidatorName() + "invalid value for getTagValueAsStringBuff(" + tagId + ", " + occurrence + ")");
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override void CheckGetterAtIndex(FixMessage ffl, int occurrence, int firstTagIndex)
		{
			var actual = new StringBuilder();
			ffl.GetTagValueAsStringBuffAtIndex(firstTagIndex, actual);
			Assert.AreEqual(Values[occurrence - 1], actual.ToString(),
				GetValidatorName() + "invalid value for getTagValueAsStringBuffAtIndex(" + firstTagIndex + ")");
		}

		public override FixMessage AddTag(FixMessage msg, int tagId, int occurrence)
		{
			msg.AddTag(tagId, new StringBuilder(Values[occurrence - 1]).ToString());
			return msg;
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override FixMessage AddTagAtIndex(FixMessage msg, int index, int tagId, int occurrence)
		{
			msg.AddTagAtIndex(index, tagId, new StringBuilder(Values[occurrence - 1]).ToString());
			return msg;
		}

		public override FixMessage SetTag(FixMessage msg, int tagId, int occurrence)
		{
			msg.Set(tagId, new StringBuilder(Values[occurrence - 1]));
			return msg;
		}

		public override FixMessage SetTagWithOccurrence(FixMessage msg, int tagId, int occurrence)
		{
			msg.Set(tagId, occurrence, new StringBuilder(Values[occurrence - 1]).ToString());
			return msg;
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override FixMessage SetTagAtIndex(FixMessage msg, int index, int occurrence)
		{
			msg.SetAtIndex(index, new StringBuilder(Values[occurrence - 1]).ToString());
			return msg;
		}
	}
}
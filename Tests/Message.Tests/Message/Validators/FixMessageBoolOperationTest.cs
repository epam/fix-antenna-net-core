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
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Message.Tests.Validators
{
	internal class FixMessageBoolOperationTest : AbstractFixMessageGetAddSetValidator
	{
		private static readonly string[] Values = { "Y", "N" };
		private static readonly bool[] BoolValues = { true, false };

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
			var actual = ffl.GetTagValueAsBool(tagId);
			ClassicAssert.AreEqual(BoolValues[0], actual,
				GetValidatorName() + "invalid value for getTagValueAsBool(" + tagId + ")");
		}

		[ExpectedExceptionOnFail(typeof(FieldNotFoundException))]
		public override void CheckGetterWithOccurrence(FixMessage ffl, int tagId, int occurrence)
		{
			var actual = ffl.GetTagValueAsBool(tagId, occurrence);
			ClassicAssert.AreEqual(BoolValues[occurrence - 1], actual,
				GetValidatorName() + "invalid value for getTagValueAsBool(" + tagId + ", " + occurrence + ")");
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override void CheckGetterAtIndex(FixMessage ffl, int occurrence, int firstTagIndex)
		{
			var actual = ffl.GetTagValueAsBoolAtIndex(firstTagIndex);
			ClassicAssert.AreEqual(BoolValues[occurrence - 1], actual,
				GetValidatorName() + "invalid value for getTagValueAsBoolAtIndex(" + firstTagIndex + ")");
		}

		public override FixMessage AddTag(FixMessage msg, int tagId, int occurrence)
		{
			msg.AddTag(tagId, BoolValues[occurrence - 1]);
			return msg;
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override FixMessage AddTagAtIndex(FixMessage msg, int index, int tagId, int occurrence)
		{
			msg.AddTagAtIndex(index, tagId, BoolValues[occurrence - 1]);
			return msg;
		}

		public override FixMessage SetTag(FixMessage msg, int tagId, int occurrence)
		{
			msg.Set(tagId, BoolValues[occurrence - 1]);
			return msg;
		}

		public override FixMessage SetTagWithOccurrence(FixMessage msg, int tagId, int occurrence)
		{
			msg.Set(tagId, occurrence, BoolValues[occurrence - 1]);
			return msg;
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override FixMessage SetTagAtIndex(FixMessage msg, int index, int occurrence)
		{
			msg.SetAtIndex(index, BoolValues[occurrence - 1]);
			return msg;
		}
	}
}
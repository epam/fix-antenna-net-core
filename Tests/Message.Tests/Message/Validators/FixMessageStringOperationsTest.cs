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
using Epam.FixAntenna.Fix.Message.validators;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Message.Tests.Validators
{
	internal class FixMessageStringOperationsTest : AbstractFixMessageGetAddSetValidator
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

		[ExpectedExceptionOnFail(typeof(NullValueException))]
		public override void CheckGetter(FixMessage ffl, int tagId)
		{
			var actual = ffl.GetTagValueAsString(tagId);
			if (ReferenceEquals(actual, null))
			{
				throw new NullValueException();
			}

			ClassicAssert.AreEqual(Values[0], actual,
				GetValidatorName() + "invalid value for getTagValueAsString(" + tagId + ")");
		}

		[ExpectedExceptionOnFail(typeof(NullValueException))]
		public override void CheckGetterWithOccurrence(FixMessage ffl, int tagId, int occurrence)
		{
			var actual = ffl.GetTagValueAsString(tagId, occurrence);
			if (ReferenceEquals(actual, null))
			{
				throw new NullValueException();
			}

			ClassicAssert.AreEqual(Values[occurrence - 1], actual,
				GetValidatorName() + "invalid value for getTagValueAsString(" + tagId + ", " + occurrence + ")");
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override void CheckGetterAtIndex(FixMessage ffl, int occurrence, int firstTagIndex)
		{
			var actual = ffl.GetTagValueAsStringAtIndex(firstTagIndex);
			ClassicAssert.AreEqual(Values[occurrence - 1], actual,
				GetValidatorName() + "invalid value for getTagValueAsStringAtIndex(" + firstTagIndex + ")");
		}

		public override FixMessage AddTag(FixMessage msg, int tagId, int occurrence)
		{
			msg.AddTag(tagId, Values[occurrence - 1]);
			return msg;
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override FixMessage AddTagAtIndex(FixMessage msg, int index, int tagId, int occurrence)
		{
			msg.AddTagAtIndex(index, tagId, Values[occurrence - 1]);
			return msg;
		}

		public override FixMessage SetTag(FixMessage msg, int tagId, int occurrence)
		{
			msg.Set(tagId, Values[occurrence - 1]);
			return msg;
		}

		public override FixMessage SetTagWithOccurrence(FixMessage msg, int tagId, int occurrence)
		{
			msg.Set(tagId, occurrence, Values[occurrence - 1]);
			return msg;
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override FixMessage SetTagAtIndex(FixMessage msg, int index, int occurrence)
		{
			msg.SetAtIndex(index, Values[occurrence - 1]);
			return msg;
		}
	}
}
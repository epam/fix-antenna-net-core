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
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework;

namespace Epam.FixAntenna.Message.Tests.Validators
{
	internal class FixMessageBytesOperationsTest : AbstractFixMessageGetAddSetValidator
	{
		private static readonly string[] Values = { "a", "bcd" };

		public override int GetMaxOccurrence(int messageMaxOccurrence)
		{
			return Math.Min(Values.Length, messageMaxOccurrence);
		}

		public override string PrepareTagValueForRead(string ffl, int tagId, int occurrence)
		{
			return ReplaceFieldValue(tagId, occurrence, Values[occurrence - 1], ffl);
		}

		[ExpectedExceptionOnFail(typeof(NullValueException))]
		public override void CheckGetter(FixMessage msg, int tagId)
		{
			var actual = msg.GetTagValueAsBytes(tagId);
			if (actual == null)
			{
				throw new NullValueException();
			}

			Assert.AreEqual(Values[0], StringHelper.NewString(actual),
				GetValidatorName() + "invalid value for getTagValueAsBytes(" + tagId + ")");
		}

		[ExpectedExceptionOnFail(typeof(NullValueException))]
		public override void CheckGetterWithOccurrence(FixMessage msg, int tagId, int occurrence)
		{
			var actual = msg.GetTagValueAsBytes(tagId, occurrence);
			if (actual == null)
			{
				throw new NullValueException();
			}

			Assert.AreEqual(Values[occurrence - 1], StringHelper.NewString(actual),
				GetValidatorName() + "invalid value for getTagValueAsBytes(" + tagId + ", " + occurrence + ")");
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override void CheckGetterAtIndex(FixMessage msg, int occurrence, int firstTagIndex)
		{
			var actual = msg.GetTagValueAsBytesAtIndex(firstTagIndex);
			Assert.AreEqual(Values[occurrence - 1], StringHelper.NewString(actual),
				GetValidatorName() + "invalid value for getTagValueAsBytesAtIndex(" + firstTagIndex + ")");
		}

		public override FixMessage AddTag(FixMessage msg, int tagId, int occurrence)
		{
			msg.AddTag(tagId, Values[occurrence - 1].AsByteArray());
			return msg;
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override FixMessage AddTagAtIndex(FixMessage msg, int index, int tagId, int occurrence)
		{
			msg.AddTagAtIndex(index, tagId, Values[occurrence - 1].AsByteArray());
			return msg;
		}

		public override FixMessage SetTag(FixMessage msg, int tagId, int occurrence)
		{
			msg.Set(tagId, Values[occurrence - 1].AsByteArray());
			return msg;
		}

		public override FixMessage SetTagWithOccurrence(FixMessage msg, int tagId, int occurrence)
		{
			msg.Set(tagId, occurrence, Values[occurrence - 1].AsByteArray());
			return msg;
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override FixMessage SetTagAtIndex(FixMessage msg, int index, int occurrence)
		{
			msg.SetAtIndex(index, Values[occurrence - 1].AsByteArray());
			return msg;
		}
	}
}
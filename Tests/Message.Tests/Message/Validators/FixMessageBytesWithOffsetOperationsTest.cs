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
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Message.Tests.Validators
{
	internal class FixMessageBytesWithOffsetOperationsTest : AbstractFixMessageGetAddSetValidator
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

		[ExpectedExceptionOnFail(typeof(FieldNotFoundException))]
		public override void CheckGetter(FixMessage ffl, int tagId)
		{
			var actual = new byte[Values[0].Length];
			var actualLength = ffl.GetTagValueAsBytes(tagId, actual, 0);
			ClassicAssert.AreEqual(Values[0], StringHelper.NewString(actual),
				GetValidatorName() + "invalid value for getTagValueAsBytes(" + tagId + ", buff, 0)");
			ClassicAssert.AreEqual(Values[0].Length, actualLength,
				GetValidatorName() + "invalid value length for getTagValueAsBytes(" + tagId + ", buff, 0)");
		}

		[ExpectedExceptionOnFail(typeof(FieldNotFoundException))]
		public override void CheckGetterWithOccurrence(FixMessage ffl, int tagId, int occurrence)
		{
			var length = 100;
			if (occurrence <= Values.Length)
			{
				length = Values[occurrence - 1].Length;
			}

			var actual = new byte[length];
			var actualLength = ffl.GetTagValueAsBytes(tagId, occurrence, actual, 0);
			ClassicAssert.AreEqual(Values[occurrence - 1], StringHelper.NewString(actual),
				GetValidatorName() + "invalid value for getTagValueAsBytes(" + tagId + ", " + occurrence + "," +
				"buff, 0)");
			ClassicAssert.AreEqual(Values[occurrence - 1].Length, actualLength,
				GetValidatorName() + "invalid value length for getTagValueAsBytes(" + tagId + ", " + occurrence +
				", buff, 0)");
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override void CheckGetterAtIndex(FixMessage ffl, int occurrence, int firstTagIndex)
		{
			var length = 100;
			if (occurrence <= Values.Length)
			{
				length = Values[occurrence - 1].Length;
			}

			var actual = new byte[length];
			var actualLength = ffl.GetTagValueAsBytesAtIndex(firstTagIndex, actual, 0);
			ClassicAssert.AreEqual(Values[occurrence - 1], StringHelper.NewString(actual),
				GetValidatorName() + "invalid value for getTagValueAsBytesAtIndex(" + firstTagIndex + ", buff, 0)");
			ClassicAssert.AreEqual(Values[occurrence - 1].Length, actualLength,
				GetValidatorName() + "invalid value length for getTagValueAsBytesAtIndex(" + firstTagIndex +
				", buff, 0)");
		}

		public override FixMessage AddTag(FixMessage msg, int tagId, int occurrence)
		{
			var bytes = Values[occurrence - 1].AsByteArray();
			msg.AddTag(tagId, bytes, 0, bytes.Length);
			return msg;
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override FixMessage AddTagAtIndex(FixMessage msg, int index, int tagId, int occurrence)
		{
			var bytes = Values[occurrence - 1].AsByteArray();
			msg.AddTagAtIndex(index, tagId, bytes, 0, bytes.Length);
			return msg;
		}

		public override FixMessage SetTag(FixMessage msg, int tagId, int occurrence)
		{
			var bytes = Values[occurrence - 1].AsByteArray();
			msg.Set(tagId, bytes, 0, bytes.Length);
			return msg;
		}

		public override FixMessage SetTagWithOccurrence(FixMessage msg, int tagId, int occurrence)
		{
			var bytes = Values[occurrence - 1].AsByteArray();
			msg.Set(tagId, occurrence, bytes, 0, bytes.Length);
			return msg;
		}

		[ExpectedExceptionOnFail(typeof(IndexOutOfRangeException))]
		public override FixMessage SetTagAtIndex(FixMessage msg, int index, int occurrence)
		{
			var bytes = Values[occurrence - 1].AsByteArray();
			msg.SetAtIndex(index, bytes, 0, bytes.Length);
			return msg;
		}
	}
}
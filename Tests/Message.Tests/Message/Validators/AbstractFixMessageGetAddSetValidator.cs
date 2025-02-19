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
using System.Reflection;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Message.Tests.Validators
{
	[TestFixture]
	internal abstract class AbstractFixMessageGetAddSetValidator : AbstractFixMessageGetterValidator
	{
		protected internal const string TestMessageWithAdd =
			"8=FIX.4.3\u00019=94\u000135=C\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u000133=2\u00011158=1\u00011158=2\u00011158=3\u00011158=4\u00011158=5\u000110=124\u00012258=124\u0001";

		protected internal const string TestMessageWithInsert =
			"8=FIX.4.3\u00019=94\u000135=C\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u00012258=124\u000133=2\u00011158=1\u00011158=2\u00011158=3\u00011158=4\u00011158=5\u000110=124\u0001";

		protected internal const int TestAddTagId = 2258;
		protected internal const int TestAddTagIndex = 9;

		public virtual string PrepareTagValueForCheckAfterWrite(string msg, int tagId, int occurrence)
		{
			return PrepareTagValueForRead(msg, tagId, occurrence);
		}

		public abstract FixMessage AddTag(FixMessage msg, int tagId, int occurrence);

		public abstract FixMessage AddTagAtIndex(FixMessage msg, int index, int tagId, int occurrence);

		public abstract FixMessage SetTag(FixMessage msg, int tagId, int occurrence);

		public abstract FixMessage SetTagWithOccurrence(FixMessage msg, int tagId, int occurrence);

		public abstract FixMessage SetTagAtIndex(FixMessage msg, int index, int occurrence);

		[Test]
		public virtual void TestAddTag()
		{
			var msg = RawFixUtil.GetFixMessage(TestMessage.AsByteArray());
			var actualMsg = AddTag(msg, TestAddTagId, 1);

			var resultMsg = PrepareTagValueForCheckAfterWrite(TestMessageWithAdd, TestAddTagId, 1);
			var expectedMsg = RawFixUtil.GetFixMessage(resultMsg.AsByteArray());
			ClassicAssert.AreEqual(expectedMsg.ToString(), actualMsg.ToString());
		}

		[Test]
		public virtual void TestAddTagAtIndex()
		{
			var msg = RawFixUtil.GetFixMessage(TestMessage.AsByteArray());
			var actualMsg = AddTagAtIndex(msg, TestAddTagIndex, TestAddTagId, 1);

			var resultMsg = PrepareTagValueForCheckAfterWrite(TestMessageWithInsert, TestAddTagId, 1);
			var expectedMsg = RawFixUtil.GetFixMessage(resultMsg.AsByteArray());
			ClassicAssert.AreEqual(expectedMsg.ToString(), actualMsg.ToString());
		}

		[Test]
		public virtual void testAddTagOnSetUnExistTagValue()
		{
			var msg = RawFixUtil.GetFixMessage(TestMessage.AsByteArray());
			var actualMsg = SetTag(msg, UnexistTestTag, 1);

			var resultMsg =
				PrepareTagValueForCheckAfterWrite(TestMessage + UnexistTestTag + "=1\u0001", UnexistTestTag, 1);
			var expectedMsg = RawFixUtil.GetFixMessage(resultMsg.AsByteArray());
			ClassicAssert.AreEqual(expectedMsg.ToString(), actualMsg.ToString());
		}

		[Test]
		public virtual void TestAddTagOutOfIndex()
		{
			var method = GetType().GetMethod("AddTagAtIndex",
				BindingFlags.Public | BindingFlags.Instance,
				null,
				CallingConventions.Any,
				new[] { typeof(FixMessage), typeof(int), typeof(int), typeof(int) },
				null);

			var failAnnotation = method.GetCustomAttribute<ExpectedExceptionOnFail>();
			if (failAnnotation != null)
			{
				try
				{
					var msg = RawFixUtil.GetFixMessage(TestMessage.AsByteArray());
					AddTagAtIndex(msg, 1000, TestAddTagId, 1);
					ClassicAssert.Fail(GetValidatorName() + " Expected exception: " + failAnnotation.Value);
				}
				catch (Exception e)
				{
					ClassicAssert.AreEqual(failAnnotation.Value, e.GetType(),
						GetValidatorName() + " Invalid exception thrown: " + e.ToString());
				}
			}
		}

		[Test]
		public virtual void testAddTagsOnSetUnExistTagsWithOccurrence()
		{
			var occurrence = GetMaxOccurrence(TestTagMaxOccurrence);
			var actualMsg = RawFixUtil.GetFixMessage(TestMessage.AsByteArray());
			for (var i = 1; i <= occurrence; i++)
			{
				actualMsg = SetTagWithOccurrence(actualMsg, UnexistTestTag, i);
			}

			var testMsg = TestMessage;
			for (var i = 1; i <= occurrence; i++)
			{
				testMsg = PrepareTagValueForCheckAfterWrite(testMsg + UnexistTestTag + "=1\u0001", UnexistTestTag,
					i);
			}

			var expectedMsg = RawFixUtil.GetFixMessage(testMsg.AsByteArray());
			ClassicAssert.AreEqual(expectedMsg.ToString(), actualMsg.ToString());
		}

		[Test]
		public virtual void TestFailedSetTagAtIndex()
		{
			var msg = RawFixUtil.GetFixMessage(TestMessage.AsByteArray());

			var method = GetType().GetMethod("SetTagAtIndex",
				BindingFlags.Public | BindingFlags.Instance,
				null,
				CallingConventions.Any,
				new[] { typeof(FixMessage), typeof(int), typeof(int) },
				null);

			var failAnnotation = method.GetCustomAttribute<ExpectedExceptionOnFail>();
			if (failAnnotation != null)
			{
				try
				{
					SetTagAtIndex(msg, 1000, 1);
					ClassicAssert.Fail(GetValidatorName() + " Expected exception: " + failAnnotation.Value);
				}
				catch (Exception e)
				{
					ClassicAssert.AreEqual(failAnnotation.Value, e.GetType(),
						GetValidatorName() + " Invalid exception thrown: " + e.ToString());
				}
			}
		}

		[Test]
		public virtual void TestSetTag()
		{
			var msg = RawFixUtil.GetFixMessage(TestMessage.AsByteArray());
			var actualMsg = SetTag(msg, TestTag, 1);

			var resultMsg = PrepareTagValueForCheckAfterWrite(TestMessage, TestTag, 1);
			var expectedMsg = RawFixUtil.GetFixMessage(resultMsg.AsByteArray());
			ClassicAssert.AreEqual(expectedMsg.ToString(), actualMsg.ToString());
		}

		[Test]
		public virtual void TestSetTagAtIndex()
		{
			var msg = RawFixUtil.GetFixMessage(TestMessage.AsByteArray());
			var actualMsg = SetTagAtIndex(msg, TestTagIndex, 1);

			var resultMsg = PrepareTagValueForCheckAfterWrite(TestMessage, TestTag, 1);
			var expectedMsg = RawFixUtil.GetFixMessage(resultMsg.AsByteArray());
			ClassicAssert.AreEqual(expectedMsg.ToString(), actualMsg.ToString());
		}

		[Test]
		public virtual void TestSetTagWithOccurrence()
		{
			var occurrence = GetMaxOccurrence(TestTagMaxOccurrence);
			var actualMsg = RawFixUtil.GetFixMessage(TestMessage.AsByteArray());
			for (var i = 1; i <= occurrence; i++)
			{
				actualMsg = SetTagWithOccurrence(actualMsg, TestTag, i);
			}

			var resultMsg = TestMessage;
			for (var i = 1; i <= occurrence; i++)
			{
				resultMsg = PrepareTagValueForCheckAfterWrite(resultMsg, TestTag, i);
			}

			var expectedMsg = RawFixUtil.GetFixMessage(resultMsg.AsByteArray());
			ClassicAssert.AreEqual(expectedMsg.ToString(), actualMsg.ToString());
		}
	}
}
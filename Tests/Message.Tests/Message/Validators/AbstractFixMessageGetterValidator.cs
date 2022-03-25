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

namespace Epam.FixAntenna.Message.Tests.Validators
{
	[TestFixture]
	internal abstract class AbstractFixMessageGetterValidator
	{
		protected internal const string TestMessage =
			"8=FIX.4.3\u00019=94\u000135=C\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u000133=2\u00011158=1\u00011158=2\u00011158=3\u00011158=4\u00011158=5\u000110=124\u0001";

		protected internal const int TestTag = 1158;
		protected internal const int TestTagMaxOccurrence = 5;
		protected internal const int UnexistTestTag = 2000;

		protected internal static readonly int TestTagIndex =
			RawFixUtil.GetFixMessage(TestMessage.AsByteArray()).GetTagIndex(TestTag);

		private FixMessage PrepareFixMessage(string msg, int occurrence)
		{
			var resultMsg = msg;
			for (var i = 1; i <= occurrence; i++)
			{
				resultMsg = PrepareTagValueForRead(resultMsg, TestTag, i);
			}

			return RawFixUtil.GetFixMessage(resultMsg.AsByteArray());
		}

		public virtual string ReplaceFieldValue(int tagId, int occurrence, string value, string msg)
		{
			var fieldPrefix = "" + '\u0001' + tagId + '=';
			var startIndex = 0;
			for (var i = 0; i < occurrence; i++)
			{
				startIndex = msg.IndexOf(fieldPrefix, startIndex, StringComparison.Ordinal);
				if (startIndex < 0)
				{
					return msg;
				}

				startIndex += fieldPrefix.Length;
			}

			var endIndex = msg.IndexOf('\u0001', startIndex);
			if (endIndex < 0)
			{
				endIndex = msg.Length;
			}

			return msg.Substring(0, startIndex) + value + msg.Substring(endIndex, msg.Length - endIndex);
		}

		public virtual string GetValidatorName()
		{
			return "[" + GetType().Name + "]";
		}

		public abstract int GetMaxOccurrence(int messageMaxOccurrence);

		public abstract string PrepareTagValueForRead(string ffl, int tagId, int occurrence);

		public abstract void CheckGetter(FixMessage ffl, int tagId);

		public abstract void CheckGetterWithOccurrence(FixMessage ffl, int tagId, int occurrence);

		public abstract void CheckGetterAtIndex(FixMessage ffl, int occurrence, int tagIndex);

		[Test]
		public virtual void TestFailedGetTagValue()
		{
			var ffl = RawFixUtil.GetFixMessage(TestMessage.AsByteArray());

			var method = GetType().GetMethod("CheckGetter",
				BindingFlags.Public | BindingFlags.Instance,
				null,
				CallingConventions.Any,
				new[] { typeof(FixMessage), typeof(int) },
				null);

			var failAnnotation = method.GetCustomAttribute<ExpectedExceptionOnFail>();
			if (failAnnotation != null)
			{
				try
				{
					CheckGetter(ffl, UnexistTestTag);
					Assert.Fail(GetValidatorName() + " Expected exception: " + failAnnotation.Value);
				}
				catch (Exception e)
				{
					Assert.AreEqual(failAnnotation.Value, e.GetType(),
						GetValidatorName() + " Invalid exception thrown: " + e.ToString());
				}
			}
		}

		[Test]
		public virtual void TestFailedGetTagValueAtIndex()
		{
			var ffl = RawFixUtil.GetFixMessage(TestMessage.AsByteArray());

			var method = GetType().GetMethod("CheckGetterAtIndex",
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
					CheckGetterAtIndex(ffl, 1, 1000);
					Assert.Fail(GetValidatorName() + " Expected exception: " + failAnnotation.Value);
				}
				catch (Exception e)
				{
					Assert.AreEqual(failAnnotation.Value, e.GetType(),
						GetValidatorName() + " Invalid exception thrown: " + e.ToString());
				}
			}
		}

		[Test]
		public virtual void TestFailedGetTagValueWithOccurrenceForInvalidOccurrence()
		{
			var ffl = RawFixUtil.GetFixMessage(TestMessage.AsByteArray());

			var method = GetType().GetMethod("CheckGetterWithOccurrence",
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
					CheckGetterWithOccurrence(ffl, TestTag, 1000);
					Assert.Fail(GetValidatorName() + " Expected exception: " + failAnnotation.Value);
				}
				catch (Exception e)
				{
					Assert.AreEqual(failAnnotation.Value, e.GetType(),
						GetValidatorName() + " Invalid exception thrown: " + e.ToString());
				}
			}
		}

		[Test]
		public virtual void TestFailedGetTagValueWithOccurrenceForInvalidTag()
		{
			var ffl = RawFixUtil.GetFixMessage(TestMessage.AsByteArray());
			var method = GetType().GetMethod("CheckGetterWithOccurrence",
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
					CheckGetterWithOccurrence(ffl, UnexistTestTag, 1);
					Assert.Fail(GetValidatorName() + " Expected exception: " + failAnnotation.Value);
				}
				catch (Exception e)
				{
					Assert.AreEqual(failAnnotation.Value, e.GetType(),
						GetValidatorName() + " Invalid exception thrown: " + e.ToString());
				}
			}
		}

		[Test]
		public virtual void TestGetTagValue()
		{
			var msg = PrepareTagValueForRead(TestMessage, TestTag, 1);
			var ffl = RawFixUtil.GetFixMessage(msg.AsByteArray());
			CheckGetter(ffl, TestTag);
		}

		[Test]
		public virtual void TestGetTagValueAtIndex()
		{
			var occurrence = GetMaxOccurrence(TestTagMaxOccurrence);
			var ffl = PrepareFixMessage(TestMessage, occurrence);

			for (var i = 1; i <= occurrence; i++)
			{
				CheckGetterAtIndex(ffl, i, TestTagIndex + i - 1);
			}
		}

		[Test]
		public virtual void TestGetTagValueWithOccurrence()
		{
			var occurrence = GetMaxOccurrence(TestTagMaxOccurrence);
			var ffl = PrepareFixMessage(TestMessage, occurrence);

			for (var i = 1; i <= occurrence; i++)
			{
				CheckGetterWithOccurrence(ffl, TestTag, i);
			}
		}
	}
}
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

using Epam.FixAntenna.Fix.Validation.Engine.Validators;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation;
using Epam.FixAntenna.NetCore.Validation.Error;
using Epam.FixAntenna.NetCore.Validation.Error.Resource;
using Epam.FixAntenna.NetCore.Validation.Utils;
using Epam.FixAntenna.NetCore.Validation.Validators;
using Epam.FixAntenna.Validation.Tests.Engine.Validators.Util;
using NUnit.Framework;

namespace Epam.FixAntenna.Validation.Tests.Engine.Validators
{
	[TestFixture]
	internal class GroupValidatorTest : AbstractValidatorTst
	{
		[SetUp]
		public virtual void Before()
		{
			Validator = GetValidator(FixVersion.Fix43);
		}

		public override IValidator GetValidator(FixVersion fixVersion)
		{
			var versionContainer = FixVersionContainer.GetFixVersionContainer(fixVersion);
			FixUtil = FixUtilFactory.Instance.GetFixUtil(versionContainer);
			return new GroupValidator(FixUtil);
		}

		[Test]
		public virtual void TestCheckOutsideGroupTag()
		{
			Validator = GetValidator(FixVersion.Fix44);
			var fieldList =
				GroupValidatorTestHelper.GetMessageWithOutsideInnerGroupTags(FixVersion.Fix44.MessageVersion);
			var errorContainer = Validator.Validate("m", CreateValidationMessage(fieldList), false);
			Assert.IsTrue(errorContainer.IsEmpty, "Unexpected error:" + errorContainer.IsPriorityError);
		}

		[Test]
		public virtual void TestInvalidCheckOutsideGroupGroupTagAfterGroup()
		{
			Validator = GetValidator(FixVersion.Fix44);
			var fieldList =
				GroupValidatorTestHelper.GetMessageWithOutsideGroupTagAfterGroup(FixVersion.Fix44.MessageVersion);

			var errorContainer = Validator.Validate("AE", CreateValidationMessage(fieldList), false);
			Assert.IsFalse(errorContainer.IsEmpty, "Unexpected error:" + errorContainer.IsPriorityError);

			var expectedError = GetError(
				FixErrorCode.Other,
				1,
				"AE",
				fieldList.GetTag(381),
				ResourceHelper.GetStringMessage("INVALID_MESSAGE_TAG_IS_OUTSIDE_REPEATING_GROUP"));

			Assert.That(errorContainer.Errors, Does.Contain(expectedError));
		}

		[Test]
		public virtual void TestInvalidCheckOutsideGroupTagBeforeGroup()
		{
			Validator = GetValidator(FixVersion.Fix44);

			var fieldList =
				GroupValidatorTestHelper.GetMessageWithOutsideGroupTag(FixVersion.Fix44.MessageVersion);

			var errorContainer = Validator.Validate("AE", CreateValidationMessage(fieldList), false);
			Assert.IsFalse(errorContainer.IsEmpty, "Unexpected error:" + errorContainer.IsPriorityError);
			var error = errorContainer.IsPriorityError;
			Assert.That(error.Description, Does.Contain("MsgType: AE, Tag: 381"));
		}

		[Test]
		public virtual void TestMessageFromGroupButGroupIsAbsent()
		{
			Validator = GetValidator(FixVersion.Fix44);
			var fieldList =
				GroupValidatorTestHelper.GetMessageWithTagFromGroupButGroupIsAbsent(
					FixVersion.Fix44.MessageVersion);
			var errorContainer = Validator.Validate("m", CreateValidationMessage(fieldList), false);
			Assert.IsFalse(errorContainer.IsEmpty, "Must be at least one error");
			Assert.AreEqual(311, errorContainer.IsPriorityError.TagValue.TagId,
				"RG tag is absent in message. Message must be not valid");
		}

		[Test]
		public virtual void TestValidMessage()
		{
			var fieldList = RawFixUtil.GetFixMessage(
				"8=FIX.4.3\u00019=1034\u000135=b\u000156=STRING\u000157=STRING\u0001143=STRING\u0001128=STRING\u0001129=STRING\u0001145=STRING\u000152=20010101-0x1:0x1:0x1.001\u000143=Y\u000197=Y\u0001347=ISO-2022-JP\u0001627=1\u0001628=STRING\u0001629=20010101-0x1:0x1:0x1.001\u0001630=1\u0001131=STRING\u0001117=STRING\u0001297=0\u0001300=1\u0001301=0\u0001537=0\u0001453=1\u0001448=STRING\u0001447=B\u0001452=1\u0001523=STRING\u00011=STRING\u0001581=1\u000158=STRING\u0001296=1\u0001302=STRING\u0001311=STRING\u0001312=STRING\u0001309=STRING\u0001305=1\u0001310=FAC\u0001242=20010101\u0001247=20010101\u0001436=1.0\u0001435=1.0\u0001308=XABJ\u0001306=STRING\u0001362=1\u0001363=D\u0001307=STRING\u0001364=1\u0001365=D\u0001304=0\u0001295=1\u0001299=STRING\u000155=STRING\u000165=STRING\u000148=STRING\u000122=1\u0001454=1\u0001455=STRING\u0001456=1\u0001460=1\u0001461=STRING\u0001167=FAC\u0001225=20010101\u0001240=20010101\u0001231=1.0\u0001223=1.0\u0001200=200101\u0001541=20010101\u0001543=STRING\u0001470=AF\u0001471=STRING\u0001472=STRING\u0001207=XABJ\u0001106=STRING\u0001348=1\u0001349=D\u0001107=STRING\u0001350=1\u0001351=D\u0001132=1.0\u0001133=1.0\u0001134=1.0\u0001135=1.0\u000162=20010101-0x1:0x1:0x1.001\u0001188=1.0\u0001190=1.0\u0001189=1.0\u0001191=1.0\u0001631=1.0\u0001632=1.0\u0001633=1.0\u0001634=1.0\u000160=20010101-0x1:0x1:0x1.001\u0001336=STRING\u000140=1\u0001193=20010101\u0001192=1.0\u0001642=1.0\u0001643=1.0\u000115=AFA\u0001368=1\u000110=0x97\u0001"
					.AsByteArray());
			var errorContainer = Validator.Validate("b", CreateValidationMessage(fieldList), false);
			Assert.IsTrue(errorContainer.IsEmpty, "Unexpected error:" + errorContainer.IsPriorityError);
		}

		[Test]
		public virtual void TestValidMessageWithGroupsTags()
		{
			var fieldList = GroupValidatorTestHelper.GetMessageWithValidGroups(FixVersion.Fix43.MessageVersion);

			var errorContainer = Validator.Validate("b", CreateValidationMessage(fieldList), false);
			Assert.IsTrue(errorContainer.IsEmpty, "Unexpected error:" + errorContainer.IsPriorityError);
		}

		[Test]
		public virtual void TestValidMessageWithoutGroupsTags()
		{
			var fieldList = GroupValidatorTestHelper.GetMessageWithoutGroupTag(FixVersion.Fix43.MessageVersion);

			var errorContainer = Validator.Validate("b", CreateValidationMessage(fieldList), false);
			Assert.IsTrue(errorContainer.IsEmpty, "Unexpected error:" + errorContainer.IsPriorityError);
		}
	}
}
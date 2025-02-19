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

using System.Linq;
using Epam.FixAntenna.Fix.Validation.Engine.Validators;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation;
using Epam.FixAntenna.NetCore.Validation.Error;
using Epam.FixAntenna.NetCore.Validation.Utils;
using Epam.FixAntenna.NetCore.Validation.Validators;
using Epam.FixAntenna.Validation.Tests.Engine.Validators.Util;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Validation.Tests.Engine.Validators
{
	[TestFixture]
	internal class ConditionalValidatorTest : AbstractValidatorTst
	{
		[SetUp]
		public virtual void Before()
		{
			Validator = GetValidator(FixVersion.Fix44);
			_message = new FixMessage();
		}

		private FixMessage _message;
		private FixErrorContainer _errorContainer;

		public override IValidator GetValidator(FixVersion fixVersion)
		{
			var versionContainer = FixVersionContainer.GetFixVersionContainer(fixVersion);
			FixUtil = FixUtilFactory.Instance.GetFixUtil(versionContainer);
			return new ConditionalValidator(FixUtil);
		}

		private void ValidateValidConditionField()
		{
			_errorContainer = Validator.Validate("7", CreateValidationMessage(_message), false);

			ClassicAssert.IsTrue(!_errorContainer.Errors.Any(),
				"Error occurred : " + _errorContainer.IsPriorityError);
		}

		private void ValidateInvalidConditionalField(int tag)
		{
			_errorContainer =
				Validator.Validate(_message.GetTagValueAsString(35), CreateValidationMessage(_message), false);

			ClassicAssert.That(_errorContainer.Errors,
				Does.Contain(GetError(FixErrorCode.CondrequiredTagMissing, -1, _message.GetTagValueAsString(35), tag)));
		}

		private void ValidateAllInvalidConditionalField()
		{
			_errorContainer =
				Validator.Validate(_message.GetTagValueAsString(35), CreateValidationMessage(_message), false);

			var errors = _errorContainer.Errors;

			ClassicAssert.IsTrue(errors.Count >= 2);
		}

		[Test]
		public virtual void TestConditionalAllRequiredTagMissing()
		{
			_message = ConditionalValidatorTestHelper.GetMessageWithoutAllRequredTag(
				FixVersion.Fix44.MessageVersion);
			ValidateAllInvalidConditionalField();
		}

		[Test]
		public virtual void TestConditionalRequiredTag355Exist()
		{
			_message =
				ConditionalValidatorTestHelper.GetMessageWithRequiredTag355(FixVersion.Fix44.MessageVersion);
			ValidateValidConditionField();
		}

		[Test]
		public virtual void TestConditionalRequiredTagExist()
		{
			_message = ConditionalValidatorTestHelper.GetMessageWithConditionalAndRequiredTag(
				FixVersion.Fix44.MessageVersion);
			ValidateValidConditionField();
		}

		[Test]
		public virtual void TestConditionalRequiredTagMissing3()
		{
			_message = ConditionalValidatorTestHelper.GetMessageWithoutTag3(FixVersion.Fix44.MessageVersion);
			ValidateInvalidConditionalField(3);
		}

		[Test]
		public virtual void TestConditionalRequiredTagMissing355()
		{
			_message = ConditionalValidatorTestHelper.GetMessageWithoutRequredTag355(
				FixVersion.Fix44.MessageVersion);
			ValidateInvalidConditionalField(355);
		}
	}
}
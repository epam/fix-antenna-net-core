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
using Epam.FixAntenna.NetCore.Validation;
using Epam.FixAntenna.NetCore.Validation.Error;
using Epam.FixAntenna.NetCore.Validation.Utils;
using Epam.FixAntenna.NetCore.Validation.Validators;
using Epam.FixAntenna.Validation.Tests.Engine.Validators.Util;
using NUnit.Framework;

namespace Epam.FixAntenna.Validation.Tests.Engine.Validators
{
	[TestFixture]
	internal class FieldAllowedInMessageValidatorTest : AbstractValidatorTst
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
			return new FieldAllowedInMessageValidator(FixUtil);
		}

		[Test]
		public virtual void TestCustomTagNoError()
		{
			var message = FixMessageDuplicateHelper.GetMessage(FixVersion.Fix43, "D");
			var errorContainer = Validator.Validate("D", CreateValidationMessage(message), false);
			message.AddTag(5001, "a");
			Assert.IsTrue(!errorContainer.Errors.Any(),
				"Error occurred:" + errorContainer.IsPriorityError);
		}

		[Test]
		public virtual void TestUnexpectedTagReturnError()
		{
			var message = FixMessageDuplicateHelper.GetMessage(FixVersion.Fix43, "D");
			message.AddTag(654, "a");
			var errorContainer = Validator.Validate("D", CreateValidationMessage(message), false);
			Assert.That(errorContainer.Errors,
				Does.Contain(GetError(FixErrorCode.TagNotDefinedForThisMessageType, 34, "D", message.GetTag(654))));
		}

		[Test]
		public virtual void TestValidMessageNoError()
		{
			var message = FixMessageDuplicateHelper.GetMessage(FixVersion.Fix43, "D");
			var errorContainer = Validator.Validate("D", CreateValidationMessage(message), false);

			Assert.IsTrue(!errorContainer.Errors.Any(),
				"Error occurred:" + errorContainer.IsPriorityError);
		}
	}
}
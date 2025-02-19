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
	internal class RequiredFieldValidatorTest : AbstractValidatorTst
	{
		[SetUp]
		public virtual void Before()
		{
			Validator = GetValidator(FixVersion.Fix50);
		}

		public override IValidator GetValidator(FixVersion fixVersion)
		{
			var versionContainer = FixVersionContainer.GetFixVersionContainer(fixVersion);
			var fixtContainer = FixVersionContainer.GetFixVersionContainer(FixVersion.Fixt11);
			FixUtil = FixUtilFactory.Instance.GetFixUtil(fixtContainer, versionContainer);
			return new RequiredFieldValidator(FixUtil);
		}

		[Test]
		public virtual void TestInvalidSenderTagExist()
		{
			var fieldList = FixMessageDuplicateHelper.GetMessage(FixVersion.Fixt11, "0");
			var subList = new FixMessage();
			for (var i = 0; i <= 4; i++)
			{
				subList.Add(fieldList[i]);
			}

			var errorContainer = Validator.Validate("0", CreateValidationMessage(subList), false);
			ClassicAssert.That(
				errorContainer.Errors,
				Does.Contain(
					GetError(FixErrorCode.RequiredTagMissing, -1, "0", new TagValue(49))));
		}

		[Test]
		public virtual void TestValidMessage()
		{
			var fieldList = FixMessageDuplicateHelper.GetMessage(FixVersion.Fixt11, "0");

			var errorContainer = Validator.Validate("0", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsFalse(errorContainer.Errors.Any());
		}
	}
}
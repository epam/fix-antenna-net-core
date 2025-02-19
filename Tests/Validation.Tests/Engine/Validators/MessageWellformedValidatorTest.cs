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
	internal class MessageWellformedValidatorTest : AbstractValidatorTst
	{
		[SetUp]
		public virtual void Before()
		{
			Validator = GetValidator(FixVersion.Fix44);
			_message = new FixMessage();
		}

		private FixMessage _message;

		public override IValidator GetValidator(FixVersion fixVersion)
		{
			var versionContainer = FixVersionContainer.GetFixVersionContainer(fixVersion);
			FixUtil = FixUtilFactory.Instance.GetFixUtil(versionContainer);
			return new MessageWelformedValidator(FixUtil);
		}

		[Test]
		public virtual void TestCheckSumTagOutOfOrder44()
		{
			_message = MessageWelformedValidatorHelper.GetMessageWithOutofOrderChecksum();

			var errorContainer = Validator.Validate("B", CreateValidationMessage(_message), false);

			ClassicAssert.That(errorContainer.Errors,
				Does.Contain(GetError(FixErrorCode.TagSpecifiedOutOfRequiredOrder, -1, "B", _message.GetTag(10))));
		}

		[Test]
		public virtual void TestInvalidCheckSum44()
		{
			_message = MessageWelformedValidatorHelper.GetMessageWithInvalidCheckSum();

			var errorContainer = Validator.Validate("B", CreateValidationMessage(_message), false);

			ClassicAssert.IsTrue(IsExistErrorCode(FixErrorCode.Other, errorContainer),
				"Error should be : " + FixErrorCode.Other);

			var errorCodeDescription = GetErrorCodeDescription(FixErrorCode.Other, errorContainer);
			ClassicAssert.IsTrue(errorCodeDescription.Contains("checksum"), "Unexpected error:" + errorCodeDescription);
		}

		[Test]
		public virtual void TestInvalidLength44()
		{
			_message = MessageWelformedValidatorHelper.GetMessageWithInvalidBodyLength();

			var errorContainer = Validator.Validate("B", CreateValidationMessage(_message), false);

			ClassicAssert.IsTrue(IsExistErrorCode(FixErrorCode.Other, errorContainer),
				"Error should be : " + FixErrorCode.Other);

			var errorCodeDescription = GetErrorCodeDescription(FixErrorCode.Other, errorContainer);
			ClassicAssert.IsTrue(errorCodeDescription.Contains("Correct length is: 3"),
				"Unexpected error:" + errorCodeDescription);
		}

		[Test]
		public virtual void TestOutOfOrderBodyLengthTag44()
		{
			_message = MessageWelformedValidatorHelper.GetMessageWithOutofOrderBodyLength();

			var errorContainer = Validator.Validate("B", CreateValidationMessage(_message), false);

			ClassicAssert.That(errorContainer.Errors,
				Does.Contain(GetError(FixErrorCode.TagSpecifiedOutOfRequiredOrder, -1, "B", _message.GetTag(9))));
		}

		[Test]
		public virtual void TestValidMessage44()
		{
			_message = MessageWelformedValidatorHelper.GetValidMessageForTest();

			var errorContainer = Validator.Validate("B", CreateValidationMessage(_message), false);
			ClassicAssert.IsTrue(!errorContainer.Errors.Any());
		}
	}
}
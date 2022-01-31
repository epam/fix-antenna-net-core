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

using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation;
using Epam.FixAntenna.NetCore.Validation.Error;
using Epam.FixAntenna.NetCore.Validation.Utils;
using Epam.FixAntenna.NetCore.Validation.Validators;
using NUnit.Framework;

namespace Epam.FixAntenna.Fix.Validation.Engine.Validators
{
	[TestFixture]
	internal class MessageTypeValidatorTest : AbstractValidatorTst
	{
		[SetUp]
		public virtual void Before()
		{
			_message = new FixMessage();
			Validator = GetValidator(FixVersion.Fix50Sp2);
		}

		private FixMessage _message;

		public override IValidator GetValidator(FixVersion fixVersion)
		{
			var versionContainer = FixVersionContainer.GetFixVersionContainer(fixVersion);
			FixUtil = FixUtilFactory.Instance.GetFixUtil(versionContainer);
			return new MessageTypeValidator(FixUtil);
		}

		[Test]
		public virtual void TestMessageWithInvalidMsgType()
		{
			var errorContainer = Validator.Validate("09", CreateValidationMessage(_message), false);
			Assert.That(errorContainer.Errors,
				Does.Contain(GetError(FixErrorCode.InvalidMsgtype, -1, "09", 35)));
		}

		[Test]
		public virtual void TestValidMsgType()
		{
			var errorContainer = Validator.Validate("9", CreateValidationMessage(_message), false);
			Assert.IsTrue(errorContainer.IsEmpty, "Error, message type '9' must exist");
		}

		[Test]
		public virtual void TestValidMsgTypeWithContentValidation()
		{
			_message.AddTag(8, "FIX.5.0");
			_message.AddTag(35, "9");

			var errorContainer = Validator.Validate("9", CreateValidationMessage(_message), true);
			Assert.IsTrue(errorContainer.IsEmpty, "Error, message type '9' must exist");
		}
	}
}
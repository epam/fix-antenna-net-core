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
using Epam.FixAntenna.NetCore.Validation.Utils;
using Epam.FixAntenna.NetCore.Validation.Validators;
using Epam.FixAntenna.Validation.Tests.Engine.Validators.Util;

using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Validation.Tests.Engine.Validators
{
	[TestFixture]
	internal class DuplicatedFieldValidatorTest : AbstractValidatorTst
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
			return new DuplicatedFieldValidator(FixUtil);
		}

		private void ValidateMessage(FixMessage message)
		{
			var errorContainer =
				Validator.Validate(message.GetTagValueAsString(35), CreateValidationMessage(message), false);
			ClassicAssert.IsTrue(errorContainer.IsEmpty, errorContainer.IsPriorityError + "");
		}

		private void ValidateDuplicateField(FixMessage message, int tag)
		{
			var errorContainer = Validator.Validate(message.GetTagValueAsString(35), CreateValidationMessage(message), false);

			var expectedError = GetError(FixErrorCode.TagAppearsMoreThanOnce, 34, message.GetTagValueAsString(35), message.GetTag(tag));

			ClassicAssert.That(errorContainer.Errors, Does.Contain(expectedError));
		}

		[Test]
		public virtual void TestInvalidDuplicatedChecksum()
		{
			var message = FixMessageDuplicateHelper.GetMessageWithDuplicateFields(FixVersion.Fix43, "b", 10);
			ValidateDuplicateField(message, 10);
		}

		[Test]
		public virtual void TestInvalidInnerRg()
		{
			var message = FixMessageDuplicateHelper.GetMessageWithDuplicateFields(FixVersion.Fix43, "b", 631);
			ValidateDuplicateField(message, 631);
		}

		[Test]
		public virtual void TestInvalidMessageWithDuplicateField()
		{
			var message = FixMessageDuplicateHelper.GetMessageWithDuplicateFields(FixVersion.Fix43, "b", 336, 10);
			ValidateDuplicateField(message, 10);
		}

		[Test]
		public virtual void TestMessageFromSendRecvBm()
		{
			var rawMsg =
				"8=FIX.4.2\u00019=000\u000135=D\u000149=BLP\u000156=SCHB\u000134=0000000000\u000150=30737\u000197=Y\u000152=20000809-20:20:50.000\u000111=SELL00000000\u00011=10030003\u000121=2\u000155=TESTA\u000154=1\u000138=0000000\u000140=2\u000159=0\u000144=00000000\u000147=I\u000160=20000809-18:20:32.000\u000110=000\u0001"
					.AsByteArray();
			var message = RawFixUtil.GetFixMessage(rawMsg);
			ValidateMessage(message);
		}

		[Test]
		public virtual void TestRgInARow()
		{
			var executionReport =
				"8=FIX.4.3\u00019=94\u000135=8\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-0x4:15:18.308\u0001" +
				"454=1\u0001455=5\u0001456=abc\u0001" + "232=2\u0001233=N\u0001234=8.23\u0001233=9\u0001234=9\u0001" +
				"555=2\u0001600=123\u0001604=1\u0001605=124\u0001600=123\u0001604=1\u0001605=124\u0001" +
				"518=2\u0001519=10\u0001520=11\u0001519=10\u0001520=11\u0001" + "10=124\u0001";

			var message = RawFixUtil.GetFixMessage(executionReport.AsByteArray());
			ValidateMessage(message);
		}

		[Test]
		public virtual void TestValidMessage()
		{
			var message = FixMessageDuplicateHelper.GetMessage(FixVersion.Fix43, "b");
			ValidateMessage(message);
		}
	}
}
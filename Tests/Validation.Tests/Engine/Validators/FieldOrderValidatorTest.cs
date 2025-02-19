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
	internal class FieldOrderValidatorTest : AbstractValidatorTst
	{
		[SetUp]
		public virtual void Before()
		{
			Validator = GetValidator(FixVersion.Fix44);
		}

		public override IValidator GetValidator(FixVersion fixVersion)
		{
			var versionContainer = FixVersionContainer.GetFixVersionContainer(fixVersion);
			FixUtil = FixUtilFactory.Instance.GetFixUtil(versionContainer);
			return new FieldOrderValidator(FixUtil);
		}

		[Test]
		public virtual void TestInvalidOrderFields()
		{
			var message = FixMessageDuplicateHelper.GetMessage(FixVersion.Fix44, "B");
			var fixField = message[2];
			message.RemoveTagAtIndex(2);
			message.AddAtIndex(message.Count - 1, fixField);

			var errorContainer = Validator.Validate("B", CreateValidationMessage(message), false);

			ClassicAssert.That(errorContainer.Errors,
				Does.Contain(GetError(FixErrorCode.TagSpecifiedOutOfRequiredOrder, 34, "B", fixField)));
		}

		[Test]
		public virtual void TestInvalidOrderInsideBody()
		{
			var message = RawFixUtil.GetFixMessage(
				"8=FIX.4.0\x00019=56\x000135=A\x000134=1\x000149=TW\x000152=20110218-15:0x1:37\x000156=ISLD\x0001108=0\x000198=2\x000110=213\x0001"
					.AsByteArray());

			var errorContainer = Validator.Validate("A", CreateValidationMessage(message), false);

			ClassicAssert.IsTrue(!errorContainer.Errors.Any(),
				"Error occurred:" + errorContainer.IsPriorityError);
		}

		[Test]
		public virtual void TestInvalidOrderInsideHeader()
		{
			var message = RawFixUtil.GetFixMessage(
				"8=FIX.4.0\x00019=45\x000135=0\x000134=2\x000149=TW\x000152=20110218-15:0x1:40\x000156=ISLD\x000110=213\x0001"
					.AsByteArray());

			var errorContainer = Validator.Validate("0", CreateValidationMessage(message), false);

			ClassicAssert.IsTrue(!errorContainer.Errors.Any(),
				"Error occurred:" + errorContainer.IsPriorityError);
		}

		[Test]
		public virtual void TestValidOrderFields()
		{
			var message = FixMessageDuplicateHelper.GetMessage(FixVersion.Fix44, "B");

			var errorContainer = Validator.Validate("B", CreateValidationMessage(message), false);

			ClassicAssert.IsTrue(!errorContainer.Errors.Any(),
				"Error occurred:" + errorContainer.IsPriorityError);
		}
	}
}
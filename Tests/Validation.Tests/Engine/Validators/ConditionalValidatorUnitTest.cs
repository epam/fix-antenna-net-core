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

using System.Collections.Generic;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation;
using Epam.FixAntenna.NetCore.Validation.Error;
using Epam.FixAntenna.NetCore.Validation.Utils;
using Epam.FixAntenna.NetCore.Validation.Validators;
using Epam.FixAntenna.NetCore.Validation.Validators.Condition;
using NUnit.Framework;

namespace Epam.FixAntenna.Fix.Validation.Engine.Validators
{
	public class ConditionalValidatorUnitTest
	{
		internal FixUtil FixUtil;
		internal ValidationFixMessageBuilder ValidationFixMessageBuilder;
		internal ConditionalValidator Validator;

		[SetUp]
		public virtual void Setup()
		{
			var fix40Custom = new FixVersionContainer("fix42custom", FixVersion.Fix40,
				"fix/validation/engine/validators/fixdic40custom.xml");
			FixUtil = FixUtilFactory.Instance.GetFixUtil(fix40Custom);
			ValidationFixMessageBuilder = ValidationFixMessageBuilder.CreateBuilder(FixUtil);
			Validator = new ConditionalValidator(FixUtil);
		}

		[Test]
		public virtual void ValidateRequiredValidCondition()
		{
			var requiredAndAlwaysValidCondition = new DummyCondition(this, true, true);
			var errors = ValidateWithCondition(requiredAndAlwaysValidCondition);
			Assert.IsTrue(errors.IsEmpty, "No errors expected");
		}

		[Test]
		public virtual void ValidateNotRequiredValidCondition()
		{
			var notRequiredAndValidCondition = new DummyCondition(this, false, true);
			var errors = ValidateWithCondition(notRequiredAndValidCondition);
			Assert.IsTrue(errors.IsEmpty, "No errors expected");
		}

		[Test]
		public virtual void ValidateRequiredNotValidCondition()
		{
			var requiredAndInvalidCondition = new DummyCondition(this, true, false);
			var errors = ValidateWithCondition(requiredAndInvalidCondition);
			Assert.IsFalse(errors.IsEmpty, "Tag is expected in message");
		}

		[Test]
		public virtual void ValidateNotRequiredNotValidCondition()
		{
			var notRequiredAndInvalidCondition = new DummyCondition(this, false, false);
			var errors = ValidateWithCondition(notRequiredAndInvalidCondition);
			Assert.IsTrue(errors.IsEmpty, "No errors expected");
		}

		private FixErrorContainer ValidateWithCondition(DummyCondition condition)
		{
			var msg = new FixMessage();
			msg.AddTag(35, "0");


			IDictionary<int, ICondition> conditionMap = new Dictionary<int, ICondition>();
			conditionMap[1] = condition;

			var fixMessage = ValidationFixMessageBuilder.BuildValidationFixMessage(msg);

			var errors = new FixErrorContainer();
			Validator.ValidateConditionalTags(conditionMap, fixMessage, msg, false, errors);
			return errors;
		}

		internal class DummyCondition : ICondition
		{
			private readonly ConditionalValidatorUnitTest _outerInstance;
			internal bool Required;

			internal bool ValidationResult;

			public DummyCondition(ConditionalValidatorUnitTest outerInstance, bool required, bool validationResult)
			{
				_outerInstance = outerInstance;
				ValidationResult = validationResult;
				Required = required;
			}

			public virtual bool ValidateCondition(int validateTag, FixMessage msg, string msgType,
				IDictionary<int, ICondition> tagsMap, bool inversion)
			{
				return ValidationResult;
			}

			public virtual bool IsRequired(FixMessage msg)
			{
				return Required;
			}

			public virtual bool IsGroupTags()
			{
				return false;
			}

			public virtual void SetGroupTags(bool isGroup)
			{
			}

			public virtual IList<int> GetTags()
			{
				return new List<int>();
			}
		}
	}
}
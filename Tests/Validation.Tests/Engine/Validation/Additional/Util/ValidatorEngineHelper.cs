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
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Validation;
using Epam.FixAntenna.NetCore.Validation.Utils;
using Epam.FixAntenna.NetCore.Validation.Validators;
using Epam.FixAntenna.NetCore.Validation.Validators.Factory;
using NUnit.Framework;

namespace Epam.FixAntenna.Validation.Tests.Engine.Validation.Additional.Util
{
	[TestFixture]
	internal class ValidatorEngineHelper
	{
		public static ValidationEngine CreateValidator(FixVersion fixVersion)
		{
			var validators = new ValidatorContainer();
			var util = ReflectFixUtilHack.GetInstance(fixVersion);
			// create message type validator
			validators.PutNewValidator(ValidatorType.MessageType, new MessageTypeValidator(util));
			// create field allowed validator
			validators.PutNewValidator(ValidatorType.FieldAllowed, new FieldAllowedInMessageValidator(util));
			// create required field validator
			validators.PutNewValidator(ValidatorType.RequiredFields, new RequiredFieldValidator(util));
			// create field order validator
			validators.PutNewValidator(ValidatorType.FieldOrder, new FieldOrderValidator(util));
			// create dublicate field validator
			validators.PutNewValidator(ValidatorType.Duplicate, new DuplicatedFieldValidator(util));
			// create fields definitions validator
			validators.PutNewValidator(ValidatorType.FieldDefinition, new FieldsDefinitionsTypeValidator(util));
			// create conditional validator
			validators.PutNewValidator(ValidatorType.Conditional, new ConditionalValidator(util));
			// create group validator
			validators.PutNewValidator(ValidatorType.Group, new GroupValidator(util));

			return new ValidationEngine(validators);
		}

		public class ReflectFixUtilHack
		{
			internal static FixUtil GetInstance(FixVersion version)
			{
				ConstructorInfo constructor = null;
				try
				{
					var fixVersionContainer = FixVersionContainer.GetFixVersionContainer(version);
					constructor = typeof(FixUtil).GetConstructor(new[] { typeof(FixVersionContainer) });
					return (FixUtil)constructor.Invoke(new object[] { fixVersionContainer });
				}
				catch (Exception e)
				{
					throw new Exception(e.Message, e);
				}
			}
		}
	}
}
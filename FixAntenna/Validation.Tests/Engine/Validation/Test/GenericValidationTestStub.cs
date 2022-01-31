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
using System.IO;
using System.Linq;
using System.Reflection;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Common.ResourceLoading;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation;
using Epam.FixAntenna.NetCore.Validation.Error;
using Epam.FixAntenna.NetCore.Validation.Utils;
using Epam.FixAntenna.NetCore.Validation.Validators;
using Epam.FixAntenna.NetCore.Validation.Validators.Factory;
using NUnit.Framework;

namespace Epam.FixAntenna.Validation.Tests.Engine.Validation.Test
{
	[TestFixture]
	internal abstract class GenericValidationTestStub
	{
		[SetUp]
		public virtual void Setup()
		{
			Errors = new FixErrorContainer();

			var validators = InitValidations();
			if (validators != null)
			{
				_fastValidator = new ValidationEngine(validators, false);
			}
		}

		private static readonly ILog Log = LogFactory.GetLog(typeof(GenericValidationTestStub));
		private StreamReader _scanner;
		private string _msgType;
		private ValidationEngine _fastValidator;
		public FixErrorContainer Errors;
		private string _message;

		protected static IEnumerable<string> GetResourcePaths(string resourceFolder)
		{
			var prefix = "Epam.FixAntenna.Validation.Tests.resources." + resourceFolder;
			var resourceNames = Assembly.GetExecutingAssembly()
				.GetManifestResourceNames()
				.Where(name => name.StartsWith(prefix));

			return resourceNames.Select(resourceName => resourceName.Replace("Epam.FixAntenna.Validation.Tests.", ""));
		}

		private ValidatorContainer InitValidations()
		{
			var validators = new ValidatorContainer();
			var fixInfo = GetFixInfo();
			if (fixInfo == null)
			{
				return null;
			}

			var fixUtil = FixUtilFactory.Instance.GetFixUtil(fixInfo.GetVersion(), fixInfo.GetAppVersion());
			validators.PutNewValidator(ValidatorType.MessageType, new MessageTypeValidator(fixUtil));
			validators.PutNewValidator(ValidatorType.FieldAllowed, new FieldAllowedInMessageValidator(fixUtil));
			validators.PutNewValidator(ValidatorType.RequiredFields, new RequiredFieldValidator(fixUtil));
			validators.PutNewValidator(ValidatorType.FieldOrder, new FieldOrderValidator(fixUtil));
			validators.PutNewValidator(ValidatorType.Duplicate, new DuplicatedFieldValidator(fixUtil));
			validators.PutNewValidator(ValidatorType.FieldDefinition, new FieldsDefinitionsTypeValidator(fixUtil));
			validators.PutNewValidator(ValidatorType.Conditional, new ConditionalValidator(fixUtil));
			validators.PutNewValidator(ValidatorType.Group, new GroupValidator(fixUtil));
			validators.PutNewValidator(ValidatorType.MessageWelformed, new MessageWelformedValidator(fixUtil));
			return validators;
		}

		public virtual void Validate(string path, bool getFromPathFromClass, bool errorShouldOccur)
		{
			if (!getFromPathFromClass)
			{
				using (var stream = ResourceLoader.DefaultLoader.LoadResource(path))
				{
					Validate(path, stream, errorShouldOccur);
				}
			}
			else
			{
				using (var stream = ResourceLoader.DefaultLoader.LoadResource(path))
				{
					Validate(path, stream, errorShouldOccur);
				}
			}
		}

		public virtual void Validate(string resourceName, Stream source, bool errorShouldOccur)
		{
			Log.Info("Validate: " + resourceName);
			Validate(source, errorShouldOccur);
		}

		public virtual void Validate(Stream source, bool errorShouldOccur)
		{
			if (_fastValidator == null)
			{
				var validators = InitValidations();
				if (validators != null)
				{
					_fastValidator = new ValidationEngine(validators, false);
				}
			}

			Assert.IsFalse(_fastValidator == null, "The validators should be not null");
			_scanner = new StreamReader(source);

			var msg = _scanner.ReadLine();
			var wasError = false;
			var errorMessage = string.Empty;
			while (msg != null && msg.Trim().Length > 0 && !wasError)
			{
				var fields = RawFixUtil.GetFixMessage(msg.AsByteArray());
				_msgType = StringHelper.NewString(fields.MsgType);

				Errors = _fastValidator.ValidateFixMessage(_msgType, fields);
				var errorSize = Errors.Errors.Count;
				if (errorSize > 0)
				{
					errorMessage = "Error occurred: "
												+ Errors.IsPriorityError
												+ ", \n\tmessage: [" + fields.ToPrintableString() + "]";
					wasError = true;
				}

				msg = _scanner.ReadLine();
			}

			if (errorShouldOccur)
			{
				Assert.IsTrue(wasError, errorMessage);
			}
			else
			{
				Assert.IsFalse(wasError, errorMessage);
			}
		}

		public virtual void CollectAllMessageErrors(string path, bool getFromPathFromClass, bool errorShouldOccur)
		{
			if (!getFromPathFromClass)
			{
				using (var stream = ResourceLoader.DefaultLoader.LoadResource(path))
				{
					Validate(path, stream, errorShouldOccur);
				}

				//collectAllMessageErrors(Thread.CurrentThread.getContextClassLoader().getResourceAsStream(path));
			}
			else
			{
				using (var stream = ResourceLoader.DefaultLoader.LoadResource(path))
				{
					Validate(path, stream, errorShouldOccur);
				}

				//collectAllMessageErrors(this.GetType().getResourceAsStream(path));
			}
		}

		public virtual void CollectAllMessageErrors(Stream source)
		{
			if (_fastValidator == null)
			{
				var validators = InitValidations();
				if (validators != null)
				{
					_fastValidator = new ValidationEngine(validators, false);
				}
			}

			Assert.IsFalse(_fastValidator == null, "The validators should be not null");
			_scanner = new StreamReader(source);

			var msg = _scanner.ReadLine();
			while (msg != null && msg.Trim().Length > 0)
			{
				var fields = RawFixUtil.GetFixMessage(msg.AsByteArray());
				_msgType = StringHelper.NewString(fields.MsgType);

				var errors = _fastValidator.ValidateFixMessage(_msgType, fields);
				msg = _scanner.ReadLine();
				Errors.Add(errors);
			}
		}

		public virtual void Validate(string message)
		{
			if (_fastValidator == null)
			{
				var validators = InitValidations();
				if (validators != null)
				{
					_fastValidator = new ValidationEngine(validators, false);
				}
			}

			Assert.IsFalse(_fastValidator == null, "The validators should be not null");

			var fields = RawFixUtil.GetFixMessage(message.AsByteArray());
			_msgType = StringHelper.NewString(fields.MsgType);

			Errors = _fastValidator.ValidateFixMessage(_msgType, fields);
			var errorSize = Errors.Errors.Count;
			Assert.IsTrue(errorSize > 0, "Error occurred: " + Errors.IsPriorityError);
		}

		public virtual void ValidateAndCheckTime(Stream source, int countOfLoops)
		{
			_scanner = new StreamReader(source);

			var msg = _scanner.ReadLine();
			while (msg != null && msg.Trim().Length > 0)
			{
				var fields = RawFixUtil.GetFixMessage(msg.AsByteArray());
				_msgType = StringHelper.NewString(fields.MsgType);

				for (var i = 0; i < countOfLoops; i++)
				{
					Errors.Add(_fastValidator.ValidateFixMessage(_msgType, fields));
				}

				var errorSize = Errors.Errors.Count;
				Assert.IsTrue(errorSize > 0);

				msg = _scanner.ReadLine();
			}
		}

		public virtual string GetMessage()
		{
			return _message;
		}

		public virtual void SetMessage(string message)
		{
			_message = message;
		}

		public virtual ValidationEngine GetFastValidator()
		{
			return _fastValidator;
		}

		public abstract FixInfo GetFixInfo();
	}
}
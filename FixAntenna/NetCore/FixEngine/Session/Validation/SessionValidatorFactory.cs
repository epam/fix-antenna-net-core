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
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation;
using Epam.FixAntenna.NetCore.Validation.Validators.Factory;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.Validation
{
	/// <summary>
	/// The session validator factory creates the validator for sessions.
	/// <p/>
	/// If <c>validation</c> parameter is set to true, the factory loads the <see cref="ValidationEngine"/> implementation,
	/// otherwise if error occurred or <c>validation</c> parameter is set to false the
	/// <see cref="DummyMessageValidator"/> will be used instead.
	/// </summary>
	/// <seealso cref="DummyMessageValidator"></seealso>
	internal class SessionValidatorFactory
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(SessionValidatorFactory));

		/// <summary>
		/// Gets the <c>IMessageValidator</c> for session.
		/// </summary>
		/// <param name="sessionParameters"> the session parameters </param>
		public static IMessageValidator GetMessageValidator(SessionParameters sessionParameters)
		{
			return Instance.CreateMessageValidator(sessionParameters);
		}

		private static SessionValidatorFactory Instance { get; } = new SessionValidatorFactory();

		private IMessageValidator CreateMessageValidator(SessionParameters sessionParameters)
		{
			var createDummyValidator = false;
			IMessageValidator messageValidator = null;
			try
			{
				var configuration = new ConfigurationAdapter(sessionParameters.Configuration);
				if (!configuration.IsValidationEnabled)
				{
					Log.Info("Validation disabled according to config parameter");
					createDummyValidator = true;
				}
				else
				{
					messageValidator = CreateRealValidator(sessionParameters, configuration);
				}
			}
			catch (Exception e)
			{
				Log.Warn("Failed to create validator. Validation disabled.");
				if (Log.IsDebugEnabled)
				{
					//problem with creating instance of IValidatorFactory
					if (e is TargetInvocationException exception)
					{
						Log.Debug("Validation disable reason: ", exception?.InnerException);
					}
					else
					{
						Log.Debug("Validation disable reason: ", e);
					}
				}
				createDummyValidator = true;
			}
			finally
			{
				if (createDummyValidator)
				{
					messageValidator = DummyMessageValidator.Instance;
				}
			}
			return messageValidator;
		}

		private IMessageValidator CreateRealValidator(SessionParameters sessionParameters, ConfigurationAdapter configuration)
		{
			var method = typeof(ValidatorFactory).GetMethod("CreateFactory", new [] { typeof(FixVersionContainer), typeof(FixVersionContainer) });
			var validatorFactory = method.Invoke(null, new object[] { sessionParameters.FixVersionContainer, sessionParameters.AppVersionContainer }) as ValidatorFactory;

			var validators = validatorFactory.CreateRequiredValidator();

			if (configuration.IsWelformedValidationEnabled)
			{
				validators.PutNewValidator(ValidatorType.MessageWelformed, validatorFactory.CreateValidator(ValidatorType.MessageWelformed));
				Log.Debug("Message wellformeness validator is on");
			}
			if (configuration.IsFieldsValidationEnabled)
			{
				Log.Debug("Allowed fields validation is on");
				validators.PutNewValidator(ValidatorType.FieldAllowed, validatorFactory.CreateValidator(ValidatorType.FieldAllowed));
			}
			if (configuration.IsRequiredFieldsValidationEnabled)
			{
				validators.PutNewValidator(ValidatorType.RequiredFields, validatorFactory.CreateValidator(ValidatorType.RequiredFields));
				Log.Debug("Required fields validation is on");
			}
			if (configuration.IsFieldOrderValidationEnabled)
			{
				validators.PutNewValidator(ValidatorType.FieldOrder, validatorFactory.CreateValidator(ValidatorType.FieldOrder));
				Log.Debug("Field order validation is on");
			}
			if (configuration.IsDuplicateFieldsValidationEnabled)
			{
				validators.PutNewValidator(ValidatorType.Duplicate, validatorFactory.CreateValidator(ValidatorType.Duplicate));
				Log.Debug("Duplicate field validation is on");
			}
			if (configuration.IsFieldTypeValidationEnabled)
			{
				validators.PutNewValidator(ValidatorType.FieldDefinition, validatorFactory.CreateValidator(ValidatorType.FieldDefinition));
				Log.Debug("Field type validation is on");
			}
			if (configuration.IsConditionalValidationEnabled)
			{
				validators.PutNewValidator(ValidatorType.Conditional, validatorFactory.CreateValidator(ValidatorType.Conditional));
				Log.Debug("Conditional validation is on");
			}
			if (configuration.IsGroupValidationEnabled)
			{
				validators.PutNewValidator(ValidatorType.Group, validatorFactory.CreateValidator(ValidatorType.Group));
				Log.Debug("Group validation is on");
			}

			//TODO: why Activator and not simple create?
			var validator = Activator.CreateInstance(typeof(ValidationEngine), new object[]{ validators, false }) as ValidationEngine;

			return new MessageValidatorImpl(validator);
		}

		private class MessageValidatorImpl : IMessageValidator
		{
			private readonly IFixMessageValidator _validator;

			public MessageValidatorImpl(IFixMessageValidator validator)
			{
				_validator = validator;
			}

			/// <inheritdoc />
			public IValidationResult ValidateContent(string msgType, FixMessage content)
			{
				var errors = _validator.ValidateFixMessage(msgType, content);
				return new ValidationResultWrapper(errors);
			}

			/// <inheritdoc />
			public bool ContentValidation
			{
				get => _validator.ContentValidation;
				set => _validator.ContentValidation = value;
			}

			/// <inheritdoc />
			public IValidationResult Validate(FixMessage message)
			{
				var errors = _validator.ValidateFixMessage(message);
				return new ValidationResultWrapper(errors);
			}

			/// <inheritdoc />
			public void Validate(FixMessage message, IValidationResult result)
			{
				var errors = _validator.ValidateFixMessage(message);
				result.Errors.Add(errors);
			}
		}
	}
}
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
using Epam.FixAntenna.NetCore.Dictionary;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation.Error;
using Epam.FixAntenna.NetCore.Validation.Exceptions;
using Epam.FixAntenna.NetCore.Validation.FixMessage;
using Epam.FixAntenna.NetCore.Validation.Utils;
using Epam.FixAntenna.NetCore.Validation.Validators;

namespace Epam.FixAntenna.NetCore.Validation
{
	/// <summary>
	/// Validator provides functionality for validation FIX message using custom
	/// validators.
	/// </summary>
	public class ValidationEngine : IFixMessageValidator
	{
		private static readonly IDictionaryBuilder Builder = new DictionaryBuilder();

		/// <summary>
		/// Field validators
		/// </summary>
		private readonly IValidatorContainer _validators;

		/// <summary>
		/// Constructor Validator creates a new Validator instance.
		/// </summary>
		/// <param name="validators"></param>
		/// <param name="isContentValidation"> Indicates that will be validation only content
		///                            of FIXMessage. </param>
		public ValidationEngine(IValidatorContainer validators, bool isContentValidation)
		{
			_validators = validators;
			ContentValidation = isContentValidation;
		}

		/// <summary>
		/// Constructor Validator creates a new Validator instance.
		/// </summary>
		/// <param name="validators"> </param>
		public ValidationEngine(IValidatorContainer validators)
		{
			_validators = validators;
		}

		public FixErrorContainer ValidateFixMessage(Message.FixMessage message)
		{
			var msgType = message.MsgType;
			if (msgType == null)
			{
				var errorContainer = new FixErrorContainer();
				errorContainer.Add(FixErrorBuilder.CreateBuilder().BuildError(FixErrorCode.RequiredTagMissing,
					(TagValue)null, message.MsgSeqNumber, null, 35));
				return errorContainer;
			}

			return ValidateFixMessage(StringHelper.NewString(message.MsgType), message);
		}

		/// <summary>
		/// Method validateFIXMessage validates the FIXMessage.
		/// </summary>
		/// <param name="msgType"> of type String </param>
		/// <param name="message"> of type FixMessage </param>
		/// <returns> List of errors of validation , if validate is successful method
		///         returns empty list </returns>
		public FixErrorContainer ValidateFixMessage(string msgType, Message.FixMessage message)
		{
			var errors = new FixErrorContainer();
			ValidationFixMessage fixMessage;

			// checks MessageTypeValidator;
			var msgTypeValidator = (MessageTypeValidator)_validators.MessageTypeValidator;
			if (msgTypeValidator == null)
			{
				errors.Add(FixErrorBuilder.CreateBuilder().BuildError(FixErrorCode.Other,
					"Validator for type of message does not declared"));
				return errors;
			}

			// validate msg type
			var container = msgTypeValidator.Validate(msgType, message, ContentValidation);
			if (!container.IsEmpty)
			{
				return container;
			}

			// build fix message
			var fixUtil = msgTypeValidator.Util;
			try
			{
				fixMessage = BuildValidationMessage(fixUtil, message);
			}
			catch (CommonValidationException r)
			{
				var validationError = r.ValidationError;
				if (validationError != null)
				{
					errors.Add(validationError);
				}

				return errors;
			}

			// Validating FIXMessage
			var validatorsList = _validators.ValidatorsWithOutMessageType;
			var validatorsCount = validatorsList.Count;
			for (var validatorCount = 0; validatorCount < validatorsCount; validatorCount++)
			{
				var validator = validatorsList[validatorCount];
				errors.Add(validator.Validate(msgType, fixMessage, ContentValidation));
			}

			return errors;
		}

		/// <summary>
		/// Enable/disable content validation.
		/// </summary>
		/// <value> </value>
		public bool ContentValidation { get; set; }

		/// <summary>
		/// Pre-loads dictionaries from path <c>uriToDictionary</c>. If <c>uriToDictionary</c> is empty or null.
		/// </summary>
		/// <param name="fixVersionContainer"> Version of FIX dictionary. </param>
		/// <param name="replaceData">         If <c> true</c> systems replace the old dictionary(if old dictionary is null system create new dictionary with input elements.) </param>
		public static void PreloadDictionary(FixVersionContainer fixVersionContainer, bool replaceData)
		{
			lock (Builder)
			{
				Builder.BuildDictionary(fixVersionContainer, replaceData);
			}
		}

		/// <summary>
		/// Pre-loads dictionaries from path <c>uriToDictionary</c>. If <c>uriToDictionary</c> is empty or null.
		/// </summary>
		/// <param name="version">         Version of FIX dictionary. </param>
		/// <param name="uriToDictionary"> Path to the dictionary. If path empty or null system loads dictionary from the  class path. </param>
		/// <param name="replaceData">     If <c> true</c> systems replace the old dictionary(if old dictionary is null system create new dictionary with input elements.)
		///                        In other way system  adds input elements or replace if found the differences in the same elements. </param>
		public static void PreloadDictionary(FixVersion version, string uriToDictionary, bool replaceData)
		{
			var fixVersionContainer = GetFixVesrionContainer(version, uriToDictionary, replaceData);
			lock (Builder)
			{
				if (replaceData)
				{
					Builder.BuildDictionary(fixVersionContainer, true);
				}
				else
				{
					Builder.UpdateDictionary(fixVersionContainer);
				}
			}
		}

		private static FixVersionContainer GetFixVesrionContainer(FixVersion version, string uriToDictionary,
			bool replaceData)
		{
			var baseFixVersion = FixVersionContainer.GetFixVersionContainer(version);
			if (string.IsNullOrEmpty(uriToDictionary))
			{
				return baseFixVersion;
			}

			if (replaceData)
			{
				return new FixVersionContainer(baseFixVersion.DictionaryId, version, uriToDictionary);
			}

			return new FixVersionContainer(baseFixVersion.DictionaryId, version, null, uriToDictionary);
		}

		/// <summary>
		/// Pre-loads dictionaries from path <c>uriToDictionary</c>. If <c>uriToDictionary</c> is empty or null.
		/// </summary>
		/// <param name="dictionaryId">    Unique ID of dictionary </param>
		/// <param name="version">         Version of FIX dictionary. </param>
		/// <param name="uriToDictionary"> Path to the dictionary. If path empty or null system loads dictionary from the  class path. </param>
		/// <param name="replaceData">     If <c> true</c> systems replace the old dictionary (if old dictionary is null system create new dictionary with input elements.)
		///                        In other way system  adds input elements or replace if found the differences in the same elements. </param>
		public static void PreloadDictionary(string dictionaryId, FixVersion version, string uriToDictionary,
			bool replaceData)
		{
			var baseVersion = FixVersionContainer.GetFixVersionContainer(version);
			var customVersion = new FixVersionContainer(dictionaryId, baseVersion.FixVersion, baseVersion.DictionaryFile, uriToDictionary);
			lock (Builder)
			{
				Builder.BuildDictionary(customVersion, replaceData);
			}
		}

		public override string ToString()
		{
			return "ValidationEngine{" + "validators=" + _validators + ", isContentValidation=" + ContentValidation +
					'}';
		}

		private ValidationFixMessage BuildValidationMessage(FixUtil fixUtil, Message.FixMessage fields)
		{
			var fixMessageBuilder = ValidationFixMessageBuilder.CreateBuilder(fixUtil);
			var fixMessage = fixMessageBuilder.BuildValidationFixMessage(fields);
			return fixMessage;
		}
	}
}
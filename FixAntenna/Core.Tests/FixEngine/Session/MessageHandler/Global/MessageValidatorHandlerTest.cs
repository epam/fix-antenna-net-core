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
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation;
using Epam.FixAntenna.NetCore.Validation.Error;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global
{
	[TestFixture]
	internal class MessageValidatorHandlerTest
	{
		private MessageValidatorHandler _validatorHandler;
		private TestFixSession _testFixSession;

		[SetUp]
		public virtual void SetUp()
		{
			_testFixSession = new TestFixSession();
			_validatorHandler = new MessageValidatorHandler();
			_validatorHandler.Session = _testFixSession;
		}

		[Test]
		public virtual void CheckFailedValidationForLogon()
		{
			var ex = Assert.Throws<MessageValidationException>(() =>
			{
				_testFixSession.SetMessageValidator(new FailMessageValidator());
				var message = new FixMessage();
				message.AddTag(8, "FIX.4.4");
				message.AddTag(35, "A");
				_validatorHandler.OnNewMessage(message);
			});

			Assert.IsTrue(ex.IsCritical());
			Assert.AreEqual("Invalid logon message", _testFixSession.DisconnectReason);
			Assert.AreEqual(SessionState.WaitingForLogoff, _testFixSession.SessionState);
		}

		[Test]
		public virtual void CheckFailedValidationForNonLogon()
		{
			var ex = Assert.Throws<MessageValidationException>(() =>
			{
				_testFixSession.SetMessageValidator(new FailMessageValidator());
				var message = new FixMessage();
				message.AddTag(8, "FIX.4.4");
				message.AddTag(35, "D");
				_validatorHandler.OnNewMessage(message);
			});

			Assert.IsFalse(ex.IsCritical());
			Assert.IsTrue(_testFixSession.Messages.Count > 0);
			var responseMessage = _testFixSession.Messages[0];
			Assert.AreEqual("3", responseMessage.GetTagValueAsString(35));
			Assert.AreEqual(FailMessageValidator.FakeValidationErrorDescription,
				responseMessage.GetTagValueAsString(58));
		}

		private class FailMessageValidator : IMessageValidator
		{
			internal const string FakeValidationErrorDescription = "fake error";

			internal readonly IValidationResult FailResult = new ValidationResult();

			internal class ValidationResult : IValidationResult
			{
				public bool IsMessageValid => false;

				public FixErrorContainer Errors
				{
					get
					{
						var fixErrors = new List<FixError>();
						var tagValue = new TagValue(55);
						fixErrors.Add(new FixError(FixErrorCode.InvalidMsgtype, FakeValidationErrorDescription, tagValue));
						return new FixErrorContainer(fixErrors);
					}
				}

				public void Reset()
				{
				}
			}

			public virtual IValidationResult Validate(FixMessage message)
			{
				return FailResult;
			}

			public virtual void Validate(FixMessage message, IValidationResult result)
			{
				result.Errors.Add(FailResult.Errors);
			}

			public virtual IValidationResult ValidateContent(string msgType, FixMessage content)
			{
				return FailResult;
			}

			public virtual bool ContentValidation { get; set; }
		}
	}
}
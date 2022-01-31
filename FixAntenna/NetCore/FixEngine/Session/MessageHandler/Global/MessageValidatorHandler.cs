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

using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.FixEngine.Session.Validation;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation;
using Epam.FixAntenna.NetCore.Validation.Error;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global
{
	/// <summary>
	/// The message validator handler.
	/// If validation is enabled, handler uses the real validator from validation module,
	/// otherwise dummy implementation.
	/// </summary>
	internal class MessageValidatorHandler : AbstractGlobalMessageHandler
	{
		public const string ErrorInvalidLogon = "Invalid logon message";
		private IValidationResult _result = new ValidationResultWrapper(new FixErrorContainer());

		/// <summary>
		/// This handler calls the indirect validation library, if this library exists in class patch.
		/// If received message not valid the reject message will be send with reject code.
		/// <p/>
		/// Reject codes: <br/>
		/// '0'  Invalid tag number <br/>
		/// '1' 	Required tag missing <br/>
		/// '2' 	Tag not defined for this message type <br/>
		/// '3' 	Undefined Tag <br/>
		/// '4' 	Tag specified without a value <br/>
		/// '5' 	Value is incorrect (out of range) for this tag <br/>
		/// '6' 	Incorrect data format for value <br/>
		/// '7' 	Decryption problem <br/>
		/// '8' 	Signature problem <br/>
		/// '9' 	CompID problem <br/>
		/// '10' SendingTime (52) accuracy problem <br/>
		/// '11' Invalid MsgType (35) <br/>
		/// '12' XML Validation error <br/>
		/// '13' Tag appears more than once <br/>
		/// '14' Tag specified out of required order <br/>
		/// '15' Repeating group fields out of order <br/>
		/// '16' Incorrect NumInGroup count for repeating group <br/>
		/// '17' Non "data" value includes field delimiter (SOH character) <br/>
		/// '99' Other
		/// </summary>
		/// <seealso cref="IFixMessageListener.OnNewMessage"> </seealso>
		public override void OnNewMessage(FixMessage message)
		{
			HandleMessage(message);
		}

		//TODO (OVR): looks like we need pool of MessageValidationException
		private void HandleMessage(FixMessage message)
		{
			var session = Session;
			_result.Reset();
			session.MessageValidator.Validate(message, _result);

			if (_result.IsMessageValid)
			{
				CallNextHandler(message);
			}
			else
			{
				var error = _result.Errors.IsPriorityError;
				var reject = session.MessageFactory.GetRejectForMessageTag(message, error.TagValue.TagId, error.FixErrorCode.Code, error.Description);
				if (FixMessageUtil.IsLogon(message))
				{
					session.ClearQueue();
					session.SendMessageOutOfTurn(MsgType.Reject, reject);
					session.Disconnect(DisconnectReason.InvalidMessage, ErrorInvalidLogon);

					throw new MessageValidationException(message, _result, ErrorInvalidLogon + ": " + error.Description, true);
				}
				else
				{
					session.SendMessageOutOfTurn(MsgType.Reject, reject);
					throw new MessageValidationException(message, _result, error.Description, false);
				}
			}
		}
	}
}
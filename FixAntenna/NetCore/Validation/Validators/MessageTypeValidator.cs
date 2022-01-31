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
using Epam.FixAntenna.NetCore.Validation.Error;
using Epam.FixAntenna.NetCore.Validation.FixMessage;
using Epam.FixAntenna.NetCore.Validation.Utils;

namespace Epam.FixAntenna.NetCore.Validation.Validators
{
	/// <summary>
	/// The message type validator. The validator checks only 35 tag.
	/// </summary>
	internal class MessageTypeValidator : AbstractValidator
	{
		/// <summary>
		/// Constructor MessageTypeValidator creates a new MessageTypeValidator instance.
		/// </summary>
		/// <param name="util"> of type FixUtils </param>
		public MessageTypeValidator(FixUtil util) : base(util)
		{
		}

		public override FixErrorContainer Validate(string msgType, IValidationFixMessage message,
			bool isContentValidation)
		{
			var fixMessage = (ValidationFixMessage)message;
			return Validate(msgType, fixMessage.FullFixMessage, isContentValidation);
		}

		public virtual FixErrorContainer Validate(string msgType, Message.FixMessage fixMessage, bool isContentValidation)
		{
			var errors = new FixErrorContainer();

			if (!IsMessageTypeExist(msgType))
			{
				errors.Add(FixErrorBuilder.BuildError(FixErrorCode.InvalidMsgtype, fixMessage.MsgSeqNumber,
					msgType, Tags.MsgType));
				return errors;
			}

			// this rule was check in previous step in isMessageTypeExist
			//        if (!isContentValidation && (msgType == null || !util.getMessageDefUtils().Contains(msgType))) {
			//            errors.Add(fixErrorBuilder.BuildError(FixErrorCode.INVALID_MSGTYPE, fixMessage.getMsgSeqNumber(), msgType, Tags.MsgType));
			//        }
			return errors;
		}
	}
}
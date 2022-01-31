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
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.PerType.Util
{
	internal class RrMessageValidator
	{
		private bool _instanceFieldsInitialized = false;

		public RrMessageValidator()
		{
			if (!_instanceFieldsInitialized)
			{
				InitializeInstanceFields();
				_instanceFieldsInitialized = true;
			}
		}

		private void InitializeInstanceFields()
		{
			Log = LogFactory.GetLog(GetType());
		}


		protected internal ILog Log;

		private IExtendedFixSession _session;
		private long _beginSeqNum, _endSeqNum;
		private RrMessageCarrierSender _carrierSender;

		public virtual void SetSession(IExtendedFixSession session)
		{
			_session = session;
			_carrierSender = new RrMessageCarrierSender(session);
		}

		public virtual long GetBeginSeqNum()
		{
			return _beginSeqNum;
		}

		public virtual long GetEndSeqNum()
		{
			return _endSeqNum;
		}

		public virtual bool Validate(FixMessage message)
		{

			var sessionParameters = _session.Parameters;
			var runtimeState = _session.RuntimeState;

			IValidatorStrategy validatorStrategy;
			if (sessionParameters.Configuration.GetPropertyAsBoolean(Config.Validation))
			{
				validatorStrategy = new ValidationIsOnStrategy(this);
			}
			else
			{
				validatorStrategy = new ValidationIsOffStrategy(this);
			}

			if (!validatorStrategy.Validate(message))
			{
				return false;
			}

			_beginSeqNum = message.GetTagValueAsLong(Tags.BeginSeqNo);


			var requestToSeqNum = message.GetTagValueAsLong(Tags.EndSeqNo);
			_endSeqNum = CalculateEndSeqNum(sessionParameters, runtimeState.OutSeqNum, requestToSeqNum);

			if (_beginSeqNum > requestToSeqNum && (sessionParameters.FixVersion.CompareTo(FixVersion.Fix40) == 0 || sessionParameters.FixVersion.CompareTo(FixVersion.Fix41) == 0))
			{
				var problemDescription = "Invalid Resend Request: BeginSeqNo (" + _beginSeqNum + ") > EndSeqNo (" + requestToSeqNum + ")";
				var list = _session.MessageFactory.GetRejectForMessageTag(message, Tags.BeginSeqNo, 5, problemDescription);
				_session.ErrorHandler.OnWarn(problemDescription, new InvalidMessageException(message, problemDescription));
				_session.SendMessageOutOfTurn(MsgType.Reject, list);
				return false;
			}

			var lastAvailableNumber = runtimeState.OutSeqNum - 1;
			if (_beginSeqNum > lastAvailableNumber)
			{
				_carrierSender.SendSequenceReset(message.GetTagValueAsBytes(Tags.SendingTime));
				return false;
			}

			if (_beginSeqNum > _endSeqNum)
			{
				var problemDescription = "Invalid Resend Request: BeginSeqNo (" + _beginSeqNum + ") > EndSeqNo (" + _endSeqNum + ")";
				var list = _session.MessageFactory.GetRejectForMessageTag(message, Tags.BeginSeqNo, 5, problemDescription);
				_session.ErrorHandler.OnWarn(problemDescription, new InvalidMessageException(message, problemDescription));
				_session.SendMessageOutOfTurn(MsgType.Reject, list);
				return false;
			}

			// we can't resend more massages than we have
			if (_endSeqNum > lastAvailableNumber)
			{
				_endSeqNum = lastAvailableNumber;
			}

			return true;
		}

		private long CalculateEndSeqNum(SessionParameters sessionParameters, long lastOutSeqNum, long endSeqNum)
		{
			//if ((endSeqNum == 0 && (sessionParameters.GetFixVersion().isFIXT() ||
			//        sessionParameters.GetFixVersion().CompareTo(FIXVersion.FIX40) >= 0)) ||
			//        (endSeqNum == 999999 && sessionParameters.GetFixVersion().CompareTo(FIXVersion.FIX42) < 0)) {
			//    endSeqNum = lastOutSeqNum;
			//}
			if ((endSeqNum == 0 || endSeqNum == 999999) && (sessionParameters.FixVersion.IsFixt || sessionParameters.FixVersion.CompareTo(FixVersion.Fix40) >= 0))
			{
				endSeqNum = lastOutSeqNum;
			}
			else
			{
				if (endSeqNum > lastOutSeqNum)
				{
					endSeqNum = lastOutSeqNum;
				}
			}

			return endSeqNum;
		}

		internal interface IValidatorStrategy
		{
			bool Validate(FixMessage message);
		}

		internal class ValidationIsOffStrategy : IValidatorStrategy
		{
			private readonly RrMessageValidator _outerInstance;

			public ValidationIsOffStrategy(RrMessageValidator outerInstance)
			{
				_outerInstance = outerInstance;
			}


			public virtual bool Validate(FixMessage message)
			{
				var fromTag = message.GetTag(Tags.BeginSeqNo);
				try
				{
					_ = fromTag.LongValue;
				}
				catch (Exception)
				{
					SendReject(message, fromTag);
					return false;
				}

				var toTag = message.GetTag(Tags.EndSeqNo);
				try
				{
					_ = toTag.LongValue;
				}
				catch (Exception)
				{
					SendReject(message, toTag);
					return false;
				}
				return true;
			}

			public virtual void SendReject(FixMessage message, TagValue tag)
			{
				var problemDescription = "Invalid message - incorrect data type.";
				var list = _outerInstance._session.MessageFactory.GetRejectForMessageTag(message, tag.TagId, 5, problemDescription);
				_outerInstance._session.ErrorHandler.OnWarn(problemDescription, new InvalidMessageException(message, problemDescription));
				_outerInstance._session.SendMessageOutOfTurn(MsgType.Reject, list);
			}
		}

		internal class ValidationIsOnStrategy : ValidationIsOffStrategy
		{
			private readonly RrMessageValidator _outerInstance;

			public ValidationIsOnStrategy(RrMessageValidator outerInstance) : base(outerInstance)
			{
				_outerInstance = outerInstance;
			}

			public override bool Validate(FixMessage message)
			{
				var validator = _outerInstance._session.MessageValidator;
				if (validator == null)
				{
					return base.Validate(message);
				}

				var result = validator.Validate(message);
				if (!result.IsMessageValid)
				{
					return false; // message invalid, the message validator handler validates and send reject
				}

				return true;
			}
		}
	}
}
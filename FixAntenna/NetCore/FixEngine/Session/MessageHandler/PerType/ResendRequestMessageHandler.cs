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
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.PerType.Util;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.PerType
{
	/// <summary>
	/// The Resend request message handler.
	/// </summary>
	internal class ResendRequestMessageHandler : AbstractSessionMessageHandler
	{
		private bool _instanceFieldsInitialized = false;

		private void InitializeInstanceFields()
		{
			Log = LogFactory.GetLog(this.GetType());
		}

		protected internal new ILog Log;

		private RrMessageValidator _validator;
		private bool _skipDuplicatedResendRequests = false;

		public ResendRequestMessageHandler()
		{
			if (!_instanceFieldsInitialized)
			{
				InitializeInstanceFields();
				_instanceFieldsInitialized = true;
			}
			_validator = new RrMessageValidator();
		}

		/// <inheritdoc />
		public override IExtendedFixSession Session
		{
			set
			{
				base.Session = value;
				var configuration = new ConfigurationAdapter(value.Parameters.Configuration);
				_skipDuplicatedResendRequests = configuration.SkipDuplicatedResendRequests;
				_validator.SetSession(value);
			}
		}

		/// <inheritdoc />
		public override void OnNewMessage(FixMessage message)
		{
			Log.Debug("ResendRequest message handler");
			if (_skipDuplicatedResendRequests && IsAlreadyProcessedRr(message))
			{
				if (Log.IsDebugEnabled)
				{
					Log.Debug("Duplicated Resend Request with seNum=" + message.GetTagValueAsString(Tags.MsgSeqNum) + " will be skipped");
				}
				return;
			}

			if (!_validator.Validate(message))
			{
				return;
			}

			var fixSession = Session;
			fixSession.SetAttribute(ExtendedFixSessionAttribute.IsResendRequestProcessed.Name, ExtendedFixSessionAttribute.YesValue);
			try
			{
				fixSession.LockSending();

				var beginSeqNum = _validator.GetBeginSeqNum();
				var endSeqNum = _validator.GetEndSeqNum();

				fixSession.FixSessionOutOfSyncListener.OnResendRequestReceived(beginSeqNum, endSeqNum);

				ExtractAllMessagesAndResend(message, fixSession, beginSeqNum, endSeqNum);
			}
			finally
			{
				fixSession.RemoveAttribute(ExtendedFixSessionAttribute.IsResendRequestProcessed.Name);
				fixSession.UnlockSending();
				fixSession.FixSessionOutOfSyncListener.OnResendRequestProcessed(_validator.GetEndSeqNum());
			}
		}

		/// <summary>
		/// Handle the resend request.
		/// </summary>
		/// <param name="resendRequestMessage">  original ResendRequest </param>
		/// <param name="fixSession"> current FIXSession object </param>
		/// <param name="beginSeqNum"> start of requested interval </param>
		/// <param name="endSeqNum"> end of requested interval </param>
		public virtual void ExtractAllMessagesAndResend(FixMessage resendRequestMessage, IExtendedFixSession fixSession, long beginSeqNum, long endSeqNum)
		{
			var storageExtractor = new StorageExtractor(fixSession, resendRequestMessage.GetTagValueAsBytes(Tags.SendingTime), beginSeqNum, endSeqNum);
			storageExtractor.ExtractAllMessagesAndResend();
		}

		private bool IsAlreadyProcessedRr(FixMessage message)
		{
			var msgSeqNum = message.GetTag(Tags.MsgSeqNum);
			if (msgSeqNum == null)
			{
				// garbled message detected
				//disconnect("Seq Number error");
				return true;
			}
			var incomingSeqNum = FixTypes.ParseInt(msgSeqNum);
			var expectedSeqNum = GetSequenceManager().GetExpectedIncomingSeqNumber();
			if (Log.IsTraceEnabled)
			{
				Log.Trace("incomingSeqNum=" + incomingSeqNum + " expectedSeqNum=" + expectedSeqNum);
			}
			return incomingSeqNum < expectedSeqNum && FixMessageUtil.IsPosDup(message);
		}

		private ISessionSequenceManager GetSequenceManager()
		{
			return ((AbstractFixSession) Session).SequenceManager;
		}
	}
}
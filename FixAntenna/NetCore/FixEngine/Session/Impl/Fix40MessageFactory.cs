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
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Message.Format;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.Impl
{
	/// <summary>
	/// The FIX 4.0 message factory implementation.
	/// </summary>
	internal class Fix40MessageFactory : StandardMessageFactory
	{
		/// <summary>
		/// Creates the <c>FIX40MessageFactory</c>.
		/// </summary>
		public Fix40MessageFactory()
		{
		}

		/// <inheritdoc />
		public override void SetSessionParameters(SessionParameters sessionParameters)
		{
			base.SetSessionParameters(sessionParameters);
			IncludeLastProcessed = false;
			var conf = sessionParameters.Configuration;
			if (conf.GetPropertyAsBoolean(Config.AllowedSecondsFractionsForFix40))
			{
				SendingTimeObj = GetSendingTime(conf);
			}
			else
			{
				SendingTimeObj = new SendingTimeSecond();
			}
		}

		/// <inheritdoc />
		public override FixMessage GetRejectForMessageTag(FixMessage rejectMessage, int refTagId, int rejectReason, string rejectText)
		{
			var fieldList = new FixMessage();

			if (rejectMessage != null)
			{
				fieldList.SetPaddedLongTag(Tags.RefSeqNum, rejectMessage.MsgSeqNumber, MinSeqNumFieldsLength);
			}
			if (!string.IsNullOrWhiteSpace(rejectText))
			{
				fieldList.AddTag(Tags.Text, rejectText);
			}
			return fieldList;
		}

		/// <inheritdoc />
		public override long GetEndSequenceNumber()
		{
			return 999999;
		}
	}
}
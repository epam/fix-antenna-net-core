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

using System.Text;
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.Impl
{
	/// <summary>
	/// The FIX 4.4 message factory implementation.
	/// </summary>
	internal class Fix44MessageFactory : StandardMessageFactory
	{
		protected internal bool IncludeNextExpectedMsgSeqNum;

		/// <inheritdoc />
		public override void SetSessionParameters(SessionParameters sessionParameters)
		{
			base.SetSessionParameters(sessionParameters);
			IncludeNextExpectedMsgSeqNum = sessionParameters.Configuration.GetPropertyAsBoolean(Config.HandleSeqnumAtLogon);
		}

		/// <inheritdoc />
		public override byte[] GetLoginHeader()
		{
			var sb = new StringBuilder();
			sb.Append(Tags.EncryptMethod).Append(Equal).Append(0).Append(Separator); // NONE encryption
			sb.Append(Tags.HeartBtInt).Append(Equal).Append(HeartbeatInterval).Append(Separator);
			if (IncludeNextExpectedMsgSeqNum)
			{
				var seqNumFormat = "{0:D" + MinSeqNumFieldsLength + "}";
				sb.Append(Tags.NextExpectedMsgSeqNum).Append(Equal).AppendFormat(seqNumFormat, RuntimeState.InSeqNum).Append(Separator);
			}
			sb.Append(GetOutLogonFields().ToString());
			return sb.ToString().AsByteArray();
		}

		/// <inheritdoc />
		public override void CompleteLogin(FixMessage content)
		{
			if (IncludeNextExpectedMsgSeqNum)
			{
				content.SetPaddedLongTag(Tags.NextExpectedMsgSeqNum, RuntimeState.InSeqNum, MinSeqNumFieldsLength);
			}
			base.CompleteLogin(content);
		}
	}
}
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
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Message.Format;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	internal class MessageGenerator
	{
		internal string Sender;
		internal string Target;

		public MessageGenerator(string sender, string target)
		{
			Sender = sender;
			Target = target;
		}

		public virtual FixMessage GetNewsMessage(int seqNum)
		{
			var rawMsg = "8=FIX.4.4#9=92#35=B#34=2#49=initiator#56=acceptor#52=20130409-08:09:16.499#148=Hello there:1#33=1#58=line1#10=232#".Replace('#', '\u0001');
			return PrepareMsg(rawMsg, seqNum);
		}

		public virtual FixMessage GetTestRequest(int seqNum)
		{
			return GetTestRequest(seqNum, "Test-" + DateTimeHelper.CurrentTicks);
		}

		public virtual FixMessage GetTestRequest(int seqNum, string testReqId)
		{
			var rawMsg = ("8=FIX.4.4#9=92#35=1#34=2#49=initiator#56=acceptor#52=20130409-08:09:16.499#112=" + testReqId + "#10=232#").Replace('#', '\u0001');
			return PrepareMsg(rawMsg, seqNum);
		}

		public virtual FixMessage GetLogonMessage()
		{
			var rawLogon = "8=FIX.4.4#9=72#35=A#34=1#49=initiator#56=acceptor#52=20130409-07:49:00.678#98=0#108=30#10=045#".Replace('#', '\u0001');
			return PrepareMsg(rawLogon, 1);
		}

		public virtual FixMessage PrepareMsg(string rawMsg, int seqNum)
		{
			var msg = RawFixUtil.GetFixMessage(rawMsg.AsByteArray());
			msg.Set(49, Sender);
			msg.Set(56, Target);
			msg.SetCalendarValue(52, DateTimeOffset.UtcNow, FixDateFormatterFactory.FixDateType.UtcTimestampWithMillis);
			msg.Set(34, seqNum);
			msg.Set(9, msg.CalculateBodyLength());
			msg.Set(10, FormatChecksum(msg.CalculateChecksum()));

			return msg;
		}

		private static byte[] FormatChecksum(int checksum)
		{
			var val = new byte[3];
			val[0] = (byte)(checksum / 100 + (byte) '0');
			val[1] = (byte)(((checksum / 10) % 10) + (byte) '0');
			val[2] = (byte)((checksum % 10) + (byte) '0');
			return val;
		}
	}
}
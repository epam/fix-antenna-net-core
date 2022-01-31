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

using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.Tester.Updater
{
	public class FullSmartUpdater : LazyUpdater
	{

		public override string UpdateChecksum(string message)
		{
			FixMessage msg = RawFixUtil.GetFixMessage(message);
			msg.Set(10, FixTypes.FormatCheckSum(msg.CalculateChecksum()));
			return msg.ToString();
		}

		public override string UpdateLength(string message)
		{
			FixMessage msg = RawFixUtil.GetFixMessage(message);
			msg.Set(9, msg.CalculateBodyLength());
			return msg.ToString();
		}

		public override byte[] UpdateMessage(byte[] message)
		{
			FixMessage msg = RawFixUtil.GetFixMessage(message);
			msg.Set(9,msg.CalculateBodyLength());
			msg.Set(10,FixTypes.FormatCheckSum(msg.CalculateChecksum()));
			return msg.AsByteArray();
		}

		public override string UpdateSendingTime(string message)
		{
			string result = message;
	//        if (message.indexOf("52=00000000-00:00:00") >= 0) {
	//            result = super.updateSendingTime(result);
	//        }
	//        if (message.indexOf("112=00000000-00:00:00") >= 0) {
	//            result = super.updateOriginalSendingTime(result);
	//        }
			return result;
		}
	}

}
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

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global.Helper
{
	internal class MessageHelper
	{
		public static FixMessage GetLoginMessage()
		{
			var message = CreateMessage();

			message.Set(35, "A");
			message.Set(9, message.CalculateBodyLength());
			message.Set(10, FixTypes.FormatCheckSum(message.CalculateChecksum()));

			return message;
		}

		public static FixMessage GetHbMessage()
		{
			var message = CreateMessage();

			message.Set(35, "0");
			message.Set(9, message.CalculateBodyLength());
			message.Set(10, FixTypes.FormatCheckSum(message.CalculateChecksum()));

			return message;
		}

		public static FixMessage GetMessageWithInvalidOrderOfThreeTags()
		{
			var message = CreateMessage();

			message.Set(35, "0");
			message.Set(9, message.CalculateBodyLength());
			message.Set(10, FixTypes.FormatCheckSum(message.CalculateChecksum()));

			var fixTypeField = message[0];
			message.RemoveTagAtIndex(0);
			var bodyLengthField = message[0];
			message.RemoveTagAtIndex(0);
			message.AddTagAtIndex(1, fixTypeField);
			message.AddTagAtIndex(2, bodyLengthField);

			return message;
		}

		private static FixMessage CreateMessage()
		{
			var message = new FixMessage();

			message.AddTag(8, "FIX.4.2");
			message.AddTag(9, (long)0);
			message.AddTag(35, "A");
			message.AddTag(34, (long)1);
			message.AddTag(52, "000000");
			message.AddTag(10, "0");

			return message;
		}
	}

}
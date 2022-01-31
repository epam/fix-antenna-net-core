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

namespace Epam.FixAntenna.Validation.Tests.Engine.Validators.Util
{
	internal class MessageWelformedValidatorHelper
	{
		public static FixMessage GetValidMessageForTest()
		{
			var message = new FixMessage();
			message.AddTag(8, "FIX.4.4");
			message.AddTag(9, "5");
			message.AddTag(35, "B");
			message.AddTag(93, "3");
			message.AddTag(89, "999");
			message.AddTag(10, "");

			message.Set(9, message.CalculateBodyLength());
			message.Set(10, FixTypes.FormatCheckSum(message.CalculateChecksum()));

			return message;
		}

		public static FixMessage GetMessageWithOutofOrderBodyLength()
		{
			var message = new FixMessage();
			message.AddTag(8, "FIX.4.4");
			message.AddTag(35, "B");
			message.AddTag(9, "5");
			message.AddTag(10, "");

			message.Set(9, message.CalculateBodyLength());
			message.Set(10, FixTypes.FormatCheckSum(message.CalculateChecksum()));
			return message;
		}

		public static FixMessage GetMessageWithInvalidCheckSum()
		{
			var message = new FixMessage();
			message.AddTag(8, "FIX.4.4");
			message.AddTag(9, "5");
			message.AddTag(35, "B");
			message.AddTag(10, "1");

			message.Set(9, message.CalculateBodyLength());
			return message;
		}

		public static FixMessage GetMessageWithOutofOrderChecksum()
		{
			var message = new FixMessage();
			message.AddTag(8, "FIX.4.4");
			message.AddTag(9, "5");
			message.AddTag(35, "B");
			message.AddTag(10, "1");
			message.AddTag(89, "1");

			message.Set(9, message.CalculateBodyLength());
			message.Set(10, FixTypes.FormatCheckSum(message.CalculateChecksum()));

			return message;
		}

		public static FixMessage GetMessageWithInvalidBodyLength()
		{
			var message = new FixMessage();
			message.AddTag(8, "FIX.4.4");
			message.AddTag(9, "5");
			message.AddTag(35, "B");
			message.AddTag(93, "2");
			message.AddTag(89, "999");
			message.AddTag(10, "1");

			message.Set(9, message.CalculateBodyLength());
			message.Set(10, FixTypes.FormatCheckSum(message.CalculateChecksum()));

			return message;
		}
	}
}
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
	internal class ConditionalValidatorTestHelper
	{
		public static FixMessage GetMessageWithoutTag3(string messageVersion)
		{
			var message = new FixMessage();
			message.AddTag(8, messageVersion);
			message.AddTag(35, "7");
			message.AddTag(5, "C");

			return message;
		}

		public static FixMessage GetMessageWithConditionalAndRequiredTag(string messageVersion)
		{
			var message = new FixMessage();

			message.AddTag(8, messageVersion);
			message.AddTag(35, "7");
			message.AddTag(5, "C");
			message.AddTag(3, "A");

			return message;
		}

		public static FixMessage GetMessageWithoutRequredTag355(string messageVersion)
		{
			var message = new FixMessage();
			message.AddTag(8, messageVersion);
			message.AddTag(35, "8");
			message.AddTag(354, "1");
			return message;
		}

		public static FixMessage GetMessageWithRequiredTag355(string messageVersion)
		{
			var message = new FixMessage();
			message.AddTag(8, messageVersion);
			message.AddTag(35, "7");
			message.AddTag(354, "1");
			message.AddTag(355, "X");
			return message;
		}

		public static FixMessage GetMessageWithoutAllRequredTag(string messageVersion)
		{
			var message = new FixMessage();
			message.AddTag(8, messageVersion);
			message.AddTag(35, "7");
			message.AddTag(354, "1");
			message.AddTag(5, "C");
			return message;
		}
	}
}
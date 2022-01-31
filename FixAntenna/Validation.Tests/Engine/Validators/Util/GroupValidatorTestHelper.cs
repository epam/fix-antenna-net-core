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
	internal class GroupValidatorTestHelper
	{
		public static FixMessage GetMessageWithValidGroups(string messageVersion)
		{
			var fieldList = new FixMessage();
			fieldList.AddTag(8, messageVersion);
			fieldList.AddTag(35, "b");
			fieldList.AddTag(58, "x");

			// group
			fieldList.AddTag(296, "1");
			fieldList.AddTag(302, "1");

			// block
			fieldList.AddTag(311, "1");

			// group
			fieldList.AddTag(295, "1");
			fieldList.AddTag(299, "1");
			fieldList.AddTag(134, "1");
			fieldList.AddTag(336, "x");
			fieldList.AddTag(625, "x");

			fieldList.AddTag(10, "x");
			return fieldList;
		}

		public static FixMessage GetMessageWithOutsideGroupTag(string messageVersion)
		{
			var fieldList = new FixMessage();
			fieldList.AddTag(8, messageVersion);
			fieldList.AddTag(9, "11");
			fieldList.AddTag(35, "AE");
			fieldList.AddTag(34, "1");
			fieldList.AddTag(381, "2000");
			fieldList.AddTag(552, "1"); // start group
			fieldList.AddTag(54, "1");
			fieldList.AddTag(37, "1");
			fieldList.AddTag(381, "2000");
			fieldList.AddTag(797, "x"); // end group
			fieldList.AddTag(10, "2000");
			return fieldList;
		}

		public static FixMessage GetMessageWithOutsideGroupTagAfterGroup(string messageVersion)
		{
			var fieldList = new FixMessage();
			fieldList.AddTag(8, messageVersion);
			fieldList.AddTag(9, "11");
			fieldList.AddTag(35, "AE");
			fieldList.AddTag(34, "1");
			fieldList.AddTag(555, "1");
			fieldList.AddTag(600, "LegSymbol");
			fieldList.AddTag(574, "1");
			fieldList.AddTag(552, "1"); // start group
			fieldList.AddTag(54, "1");
			fieldList.AddTag(37, "1");
			fieldList.AddTag(381, "2000"); // end group
			fieldList.AddTag(797, "x");
			fieldList.AddTag(852, "2000");
			fieldList.AddTag(381, "2000"); // tag out side group

			fieldList.AddTag(10, "10");
			return fieldList;
		}

		public static FixMessage GetMessageWithoutGroupTag(string messageVersion)
		{
			var fieldList = new FixMessage();
			fieldList.AddTag(8, messageVersion);
			fieldList.AddTag(35, "b");
			fieldList.AddTag(58, "x");

			// group
			fieldList.AddTag(296, "1");
			fieldList.AddTag(302, "1");

			// block
			fieldList.AddTag(311, "1");

			// group
			fieldList.AddTag(295, "1");
			fieldList.AddTag(299, "1");
			fieldList.AddTag(134, "1");

			fieldList.AddTag(10, "x");
			return fieldList;
		}

		public static FixMessage GetMessageWithOutsideInnerGroupTags(string messageVersion)
		{
			var fieldList = new FixMessage();
			fieldList.AddTag(8, messageVersion);
			fieldList.AddTag(9, "11");
			fieldList.AddTag(35, "m");
			fieldList.AddTag(34, "1");
			fieldList.AddTag(428, "1");
			fieldList.AddTag(55, "TEST");
			fieldList.AddTag(711, "1");
			fieldList.AddTag(311, "IBM"); // tag outside of instrument group
			fieldList.AddTag(44, "1.9");
			fieldList.AddTag(10, "2000");
			return fieldList;
		}

		public static FixMessage GetMessageWithTagFromGroupButGroupIsAbsent(string messageVersion)
		{
			var fieldList = new FixMessage();
			fieldList.AddTag(8, messageVersion);
			fieldList.AddTag(9, "11");
			fieldList.AddTag(35, "m");
			fieldList.AddTag(34, "1");
			fieldList.AddTag(428, "1");
			fieldList.AddTag(55, "TEST");
			//        fixMessage.AddTag(711, "1")); // RG is absent
			fieldList.AddTag(311, "IBM"); // tag of RG
			fieldList.AddTag(10, "2000");
			return fieldList;
		}
	}
}
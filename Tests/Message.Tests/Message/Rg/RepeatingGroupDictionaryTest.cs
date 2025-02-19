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
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Message.Tests.Rg
{
	[TestFixture]
	internal class RepeatingGroupDictionaryTest
	{
		//Bug https://jira.epam.com/jira/browse/RBSFAJ-46
		[Test]
		public virtual void ParseInnerBlock()
		{
			var msg = new FixMessage();

			msg.Set(8, "FIX.4.4");
			msg.Set(35, "D");
			msg.Set(711, 1);
			msg.Set(311, "a");
			msg.Set(887, 1);
			msg.Set(888, "b");

			ClassicAssert.AreEqual("b",
				msg.GetRepeatingGroup(711).GetEntry(0).GetRepeatingGroup(887).GetEntry(0).GetTagValueAsString(888));
		}
	}
}
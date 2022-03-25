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

using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	[TestFixture]
	internal class FixSessionPreparedMsgTest
	{
		internal TestFixSession SessionHelper;

		[SetUp]
		public void SetUp()
		{
			SessionHelper = new TestFixSession();
		}

		[Test]
		public virtual void TestSendPreparedMessage()
		{
			var ms = new MessageStructure();
			ms.Reserve(148, 11);

			//create msg from pool
			var pm = SessionHelper.PrepareMessage("B", ms);
			pm.Set(148, "Hello there");
			SessionHelper.SendMessage(pm);
			var pmMessage = SessionHelper.Messages[0];
			SessionHelper.Messages.Remove(pmMessage);
			Assert.AreEqual(pm, pmMessage);
			pm.ReleaseInstance();

			//get message from pool - it should be clean and not prepared
			var pooledMsg = FixMessageFactory.NewInstanceFromPool();
			Assert.AreEqual("", pooledMsg.ToString());
			Assert.IsTrue(pooledMsg.IsEmpty);
			Assert.IsFalse(pooledMsg.IsPreparedMessage);
		}
	}
}
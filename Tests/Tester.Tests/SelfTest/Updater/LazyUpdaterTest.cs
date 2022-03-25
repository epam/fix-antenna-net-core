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

using Epam.FixAntenna.Tester.Updater;
using NUnit.Framework;

namespace Epam.FixAntenna.Tester.SelfTest
{
	[TestFixture]
	public class LazyUpdaterTest
	{
		internal LazyUpdater SuperUpdater;

		[SetUp]
		public virtual void SetUp()
		{
			SuperUpdater = new LazyUpdater();
		}

		[Test]
		public virtual void TestUpdateLength()
		{
			string result = SuperUpdater.UpdateLength("8=FIX.4.1\x00019=45\x000135=5\x000149=B2B\x000156=123\x000134=2\x000152=20040227-11:53:34\x000110=088\x0001");
			Assert.AreEqual("8=FIX.4.1\x00019=45\x000135=5\x000149=B2B\x000156=123\x000134=2\x000152=20040227-11:53:34\x000110=088\x0001", result);
			result = (new LazyUpdater()).UpdateLength("8=FIX.4.1\x00019=57\x000135=A\x000149=B2B\x000156=123\x000134=1\x000152=20040227-11:53:34\x000198=0\x0001108=30\x000110=127\x0001");
			Assert.AreEqual("8=FIX.4.1\x00019=57\x000135=A\x000149=B2B\x000156=123\x000134=1\x000152=20040227-11:53:34\x000198=0\x0001108=30\x000110=127\x0001", result);
		}

		[Test]
		public virtual void TestUpdateSendingTime()
		{
			string date = SuperUpdater.GetDate();
			string result = SuperUpdater.UpdateSendingTime("8=FIX.4.1\x00019=45\x000135=5\x000149=B2B\x000156=123\x000134=2\x000152=20040227-11:53:34\x000110=088\x0001");
			Assert.AreEqual("8=FIX.4.1\x00019=45\x000135=5\x000149=B2B\x000156=123\x000134=2\x000152=" + date + "\x000110=088\x0001", result);
		}

		[Test]
		public virtual void TestGetDate()
		{
			Assert.AreEqual(SuperUpdater.GetDate().Length, "00000000-00:00:00".Length);
		}

	}
}
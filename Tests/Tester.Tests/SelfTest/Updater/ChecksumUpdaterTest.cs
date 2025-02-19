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
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Tester.SelfTest
{
	[TestFixture]
	public class ChecksumUpdaterTest
	{
		[Test]
		public virtual void TestUpdateOnBadMessage()
		{
			string message = "";
			try
			{
				ClassicAssert.AreEqual((new LazyUpdater()).UpdateMessage(message), message);
				ClassicAssert.Fail();
			}
			catch (System.ArgumentException)
			{

			}
		}

		[Test]
		public virtual void TestUpdateMessage()
		{
			string message = "8=FIX.4.2\x00019=115\x000135=A\x000149=ADAPTOR\x000156=123\x000134=1\x000150=SENDERSUBID\x0001142=SENDERLOCATIONID\x000157=TARGETSUBID\x000152=20040219-11:02:43.334\x000198=0\x0001108=0\x000110=207\x0001";
			ClassicAssert.AreEqual((new DefaultUpdater()).UpdateMessage(message), message);
		}
	}
}
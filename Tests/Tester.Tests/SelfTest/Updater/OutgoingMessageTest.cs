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

using Epam.FixAntenna.Tester.Stage;
using Epam.FixAntenna.Tester.Updater;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Tester.SelfTest
{
	[TestFixture]
	public class OutgoingMessageTest
	{
		internal OutgoingMessage OutgoingMessage;

		[Test]
		public virtual void TestGetUpdatedMessage()
		{
			string message = "8=FIX.4.2\x00019=138\x000135=A\x000149=123\x000156=ADAPTOR\x000134=7\x000150=SENDERSUBID\x0001142=SENDERLOCATIONID\x0001" + "57=TARGETSUBID\x000152=20040219-11:02:43.327\x0001369=0\x000198=0\x0001108=0\x000195=8\x000196=PASSWORD\x000110=";

			string correctChecksum = "240\x0001";
			string incorrectChecksum = "000\x0001";
			OutgoingMessage = new OutgoingMessage(message + incorrectChecksum, new SmartUpdater());
			ClassicAssert.AreEqual(OutgoingMessage.GetUpdatedMessage(), message + correctChecksum);
		}
	}
}
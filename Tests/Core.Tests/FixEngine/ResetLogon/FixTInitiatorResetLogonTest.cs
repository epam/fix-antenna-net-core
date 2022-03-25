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

using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.FixEngine;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.ResetLogon
{
	[TestFixture]
	internal class FixtInitiatorResetLogonTest : AbstractInitiatorResetTst
	{
		public override SessionParameters AcceptorDefaultProperties
		{
			get
			{
				var acceptorDefaultProperties = base.AcceptorDefaultProperties;
				acceptorDefaultProperties.FixVersion = FixVersion.Fixt11;
				acceptorDefaultProperties.AppVersion = FixVersion.Fix42;
				return acceptorDefaultProperties;
			}
		}

		public override SessionParameters InitiatorDefaultProperties
		{
			get
			{
				var initiatorDefaultProperties = base.InitiatorDefaultProperties;
				initiatorDefaultProperties.FixVersion = FixVersion.Fixt11;
				initiatorDefaultProperties.AppVersion = FixVersion.Fix42;
				return initiatorDefaultProperties;
			}
		}

		[Test]
		public virtual void TestReceiveResetLogonForFixt11()
		{
			ResetSeqNums();

			var message = AcceptorSessionEmulator.GetLastReceivedMessage();
			Assert.AreEqual(message.MsgSeqNumber, 1);
			Assert.AreEqual(message.GetTagValueAsString(141), "Y");
			Assert.AreEqual(message.GetTagValueAsString(8), "FIXT.1.1");
			Assert.AreEqual(message.GetTagValueAsString(1137), "4");
		}
	}
}
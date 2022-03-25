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

using Epam.FixAntenna.Fixicc.Message;
using NUnit.Framework;

namespace Epam.FixAntenna.AdminTool.Tests.Commands
{
	internal class ReceivedStatTest : AdminToolHelper
	{
		private ReceivedStat _receivedStat;

		[SetUp]
		public void Setup()
		{
			base.Before();
			_receivedStat = new ReceivedStat();
			RequestID = GetNextRequest();
			_receivedStat.RequestID = RequestID;

			FixSession = FindAdminSession();
		}

		[Test]
		public void TestReceivedStat()
		{
			var response = GetReponse(_receivedStat);
			Assert.AreEqual(RequestID, response.RequestID);
			Assert.IsNotNull(response.ReceivedStatData);

			var receivedStatData = response.ReceivedStatData;
			Assert.IsNotNull(receivedStatData.ReceivedMessages);
			Assert.IsTrue(receivedStatData.ReceivedMessages >= 1);
		}
	}
}
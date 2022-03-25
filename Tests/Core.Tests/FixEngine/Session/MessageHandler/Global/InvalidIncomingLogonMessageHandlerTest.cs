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

using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global.Helper;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global
{
	[TestFixture]
	internal class InvalidIncomingLogonMessageHandlerTest
	{
		private InvalidIncomingLogonMessageHandlerChainHelper _messageHandler;

		[SetUp]
		public virtual void SetUp()
		{
			_messageHandler = new InvalidIncomingLogonMessageHandlerChainHelper();
		}

		[TearDown]
		public virtual void TearDown()
		{
			_messageHandler.FreeResources();
		}

		[Test]
		public virtual void FirstMessageLogin()
		{
			_messageHandler.ProcessMessage(MessageHelper.GetLoginMessage());
			Assert.IsNotNull(_messageHandler.GetMessage());
		}

		[Test]
		public virtual void FirstMessageIsNotLogin()
		{
			var ex = Assert.Throws<InvalidMessageException>(() =>
			{
				_messageHandler.ProcessMessage(MessageHelper.GetHbMessage());
			});

			Assert.IsTrue(ex.IsCritical());
			Assert.IsNull(_messageHandler.GetMessage());
		}
	}

}
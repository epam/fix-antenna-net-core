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

using Epam.FixAntenna.TestUtils;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global.Helper;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global
{
	[TestFixture]
	internal class GarbledMessageHandlerTest
	{
		private GarbledMessageHandlerChainHelper _messageHandler;

		[SetUp]
		public virtual void SetUp()
		{
			_messageHandler = new GarbledMessageHandlerChainHelper();
		}

		[TearDown]
		public virtual void TearDown()
		{
			_messageHandler.FreeResources();
			Assert.IsTrue(ClearLogs(), "Can't clean logs after tests");
		}

		public virtual bool ClearLogs()
		{
			var logsCleaner = new LogsCleaner();
			return logsCleaner.Clean("./logs") && logsCleaner.Clean("./logs/backup");
		}

		[Test]
		public virtual void MessageWithInvalidOrderOfFirstThreeTags()
		{
			var ex = Assert.Throws<GarbledMessageException>(() =>
			{
				_messageHandler.ProcessMessage(MessageHelper.GetMessageWithInvalidOrderOfThreeTags());
			});

			Assert.IsFalse(ex.IsCritical());
			Assert.IsNull(_messageHandler.GetMessage());
		}

		[Test]
		public virtual void MessageWithValidOrder()
		{
			_messageHandler.ProcessMessage(MessageHelper.GetLoginMessage());
			Assert.IsNotNull(_messageHandler.GetMessage());
		}

		[Test]
		public virtual void MessageWithoutSeqNum()
		{
			var ex = Assert.Throws<GarbledMessageException>(() =>
			{
				var message = MessageHelper.GetLoginMessage();
				message.RemoveTag(34);
				_messageHandler.ProcessMessage(message);
			});

			Assert.IsTrue(ex.IsCritical());
			Assert.IsNull(_messageHandler.GetMessage());
		}
	}
}
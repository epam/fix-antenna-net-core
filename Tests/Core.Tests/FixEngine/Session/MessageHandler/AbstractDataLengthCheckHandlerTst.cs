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

using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.TestUtils;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler
{
	internal class AbstractDataLengthCheckHandlerTst : ISessionMessageHandler
	{
		private FixMessage _messageInNextHandler;

		[SetUp]
		public virtual void SetUp()
		{
			Reset();
		}

		[TearDown]
		public virtual void TearDown()
		{
			ClearLogs();
		}

		public virtual void ClearLogs()
		{
			var logsCleaner = new LogsCleaner();
			logsCleaner.Clean("./logs");
			logsCleaner.Clean("./logs/backup");
		}

		public virtual void Reset()
		{
			_messageInNextHandler = null;
		}

		public virtual void ClassicAssertNoMessagePassedForNext()
		{
			ClassicAssert.IsNull(_messageInNextHandler);
		}

		public virtual void ClassicAssertThatMessagePassedToNextHandlerIs(FixMessage message)
		{
			ClassicAssert.AreEqual(message.ToString(), _messageInNextHandler.ToString());
		}

		public virtual void ClassicAssertThatMessagePassedToNextHandlerIs(string message)
		{
			ClassicAssert.AreEqual(message, _messageInNextHandler.ToString());
		}

		public virtual IExtendedFixSession Session
		{
			set { }
		}

		public virtual void OnNewMessage(FixMessage message)
		{
			_messageInNextHandler = message;
		}
	}

}
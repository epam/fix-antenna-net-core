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
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global
{
	[TestFixture]
	internal class VersionConsistencyHandlerTest : AbstractDataLengthCheckHandlerTst
	{
		private VersionConsistencyHandler _handler;
		private TestFixSession _testFixSession;

		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			_handler = new VersionConsistencyHandler();
			_testFixSession = new TestFixSession();
			_handler.Session = _testFixSession;
			_handler.NextHandler = this;
		}

		[Test]
		public virtual void EmptyVersionTagProduceException()
		{
			var ex = ClassicAssert.Throws<GarbledMessageException>(() =>
			{
				var message = new FixMessage();
				_handler.OnNewMessage(message);
			});

			ClassicAssert.IsTrue(ex.IsCritical());
			ClassicAssertNoMessagePassedForNext();
			ClassicAssert.AreEqual("FIX version changed suddenly", _testFixSession.DisconnectReason);
			ClassicAssert.AreEqual(SessionState.WaitingForLogoff, _testFixSession.SessionState);
		}

		[Test]
		public virtual void BrokenVersionTagProduceException()
		{
			var ex = ClassicAssert.Throws<GarbledMessageException>(() =>
			{
				var message = new FixMessage();
				message.AddTag(8, "FIX");
				_handler.OnNewMessage(message);
			});

			ClassicAssert.IsTrue(ex.IsCritical());
			ClassicAssertNoMessagePassedForNext();
			ClassicAssert.AreEqual("FIX version changed suddenly", _testFixSession.DisconnectReason);
			ClassicAssert.AreEqual(SessionState.WaitingForLogoff, _testFixSession.SessionState);
		}

		[Test]
		public virtual void InvalidFixVersionTagProduceException()
		{
			var ex = ClassicAssert.Throws<GarbledMessageException>(() =>
			{
				var message = new FixMessage();
				message.AddTag(8, "FIX.4.0");
				_handler.OnNewMessage(message);
			});

			ClassicAssert.IsTrue(ex.IsCritical());
			ClassicAssertNoMessagePassedForNext();
			ClassicAssert.AreEqual("FIX version changed suddenly", _testFixSession.DisconnectReason);
			ClassicAssert.AreEqual(SessionState.WaitingForLogoff, _testFixSession.SessionState);
		}
	}

}
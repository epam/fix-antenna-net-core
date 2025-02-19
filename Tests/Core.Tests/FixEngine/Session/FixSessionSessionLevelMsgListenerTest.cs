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

using System;
using System.IO;
using System.Threading;
using Epam.FixAntenna.TestUtils;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	[TestFixture]
	internal class FixSessionSessionLevelMsgListenerTest
	{
		public const int Port = 12345;
		private FixServer _server;
		private IFixSession _acceptorSession;

		[SetUp]
		public void SetUp()
		{
			ClassicAssert.IsTrue(ClearLogs(), "Can't clean logs after tests");
			_server = new FixServer();
			_server.SetPort(Port);
			_server.SetListener(new FixServerListenerAnonymousInnerClass(this));
			_server.Start();
		}

		internal class FixServerListenerAnonymousInnerClass : IFixServerListener
		{
			private readonly FixSessionSessionLevelMsgListenerTest _outerInstance;

			public FixServerListenerAnonymousInnerClass(FixSessionSessionLevelMsgListenerTest outerInstance)
			{
				_outerInstance = outerInstance;
			}

			public void NewFixSession(IFixSession session)
			{
				_outerInstance._acceptorSession = session;
				try
				{
					session.SetFixSessionListener(new FixSessionListenerAnonymousInnerClass());
					session.AddOutSessionLevelMessageListener(new TypedFixMessageListenerAnonymousInnerClass());
					session.Connect();
				}
				catch (IOException)
				{
				}
			}

			private class FixSessionListenerAnonymousInnerClass : IFixSessionListener
			{
				public FixSessionListenerAnonymousInnerClass()
				{
				}

				public void OnSessionStateChange(SessionState sessionState)
				{
				}
				public void OnNewMessage(FixMessage message)
				{
					Console.WriteLine("Received message: " + message.ToPrintableString());
				}
			}

			private class TypedFixMessageListenerAnonymousInnerClass : ITypedFixMessageListener
			{
				public TypedFixMessageListenerAnonymousInnerClass()
				{
				}

				public void OnMessage(string msgType, FixMessage message)
				{
					if (msgType.Equals("A"))
					{
						message.AddTag(58, "Test logon modification");
					}
					else if (msgType.Equals("5"))
					{
						message.RemoveTag(58);
						message.AddTag(1409, "Test logout modification");
					}
				}
			}
		}

		[TearDown]
		public virtual void TearDown()
		{
			FixSessionManager.DisposeAllSession();
			_server.Stop();
			ClassicAssert.IsTrue(ClearLogs(), "Can't clean logs after tests");
		}

		public virtual bool ClearLogs()
		{
			return (new LogsCleaner()).Clean("./logs") && (new LogsCleaner()).Clean("./logs/backup");
		}

		[Test]
		public virtual void TestRejectMessages()
		{
			var gen = new MessageGenerator("s", "t");

			var helper = new SimplestFixSessionHelper("localhost", Port);
			helper.Connect();
			helper.PrepareToReceiveMessages(1);
			helper.SendMessage(gen.GetLogonMessage().AsByteArray());
			helper.WaitForMessages(10000);
			ClassicAssert.AreEqual("A", helper.GetMessages()[0].GetTagValueAsString(35), "First message not Logon");

			const string testReqId = "myTestReqID";
			var testRequestFlag = new System.Threading.CountdownEvent(1);
			_acceptorSession.AddInSessionLevelMessageListener(new FixMessageListenerAnonymousInnerClass(testReqId, testRequestFlag));

			helper.SendMessage(gen.GetTestRequest(2, testReqId).AsByteArray());
			ClassicAssert.IsTrue(testRequestFlag.Wait(TimeSpan.FromSeconds(10)), "We did not received Test Request on user listener");

			helper.Disconnect();
		}

		private class FixMessageListenerAnonymousInnerClass : IFixMessageListener
		{
			private string _testReqId;
			private CountdownEvent _testRequestFlag;

			public FixMessageListenerAnonymousInnerClass(string testReqId, CountdownEvent testRequestFlag)
			{
				_testReqId = testReqId;
				_testRequestFlag = testRequestFlag;
			}

			public void OnNewMessage(FixMessage message)
			{
				if ("1".Equals(StringHelper.NewString(message.MsgType)) && _testReqId.Equals(message.GetTagValueAsString(112)))
				{
					_testRequestFlag.Signal();
				}
			}
		}

		[Test]
		public virtual void TestOutSessionLevelListener()
		{
			var gen = new MessageGenerator("s", "t");

			var helper = new SimplestFixSessionHelper("localhost", Port);
			helper.Connect();
			helper.PrepareToReceiveMessages(1);
			helper.SendMessage(gen.GetLogonMessage().AsByteArray());
			helper.WaitForMessages(10000);
			ClassicAssert.AreEqual("A", helper.GetMessages()[0].GetTagValueAsString(35), "First message not Logon");
			ClassicAssert.IsTrue(helper.GetMessages()[0].HasTagValue(58), "Logon doesn't modified with tag 58");

			helper.PrepareToReceiveMessages(1);
			_acceptorSession.Disconnect("Normal disconnect");
			helper.WaitForMessages(10000);

			ClassicAssert.AreEqual("5", helper.GetMessages()[0].GetTagValueAsString(35), "First message not Logout");
			ClassicAssert.IsTrue(!helper.GetMessages()[0].HasTagValue(58), "Logout doesn't modified, tag 58 still present");
			ClassicAssert.IsTrue(helper.GetMessages()[0].HasTagValue(1409), "Logout doesn't modified with tag 1409");
		}
	}
}
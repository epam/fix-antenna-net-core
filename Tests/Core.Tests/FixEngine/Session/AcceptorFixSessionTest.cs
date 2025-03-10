﻿// Copyright (c) 2021 EPAM Systems
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

using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Acceptor;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.TestUtils;

using NUnit.Framework; 
using NUnit.Framework.Legacy;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using static Epam.FixAntenna.NetCore.FixEngine.Session.TestMessageHelper;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	[TestFixture]
	internal class AcceptorFixSessionTest
	{
		public const int Port = 12345;
		internal readonly string RejectReason = "Test";
		internal FixServer Server;
		internal CountdownEvent WaitReject;

		[SetUp]
		public void SetUp()
		{
			ConfigurationHelper.StoreGlobalConfig();
			// return to default values
			Config.GlobalConfiguration.SetProperty(Config.StorageCleanupMode, "None");
			Config.GlobalConfiguration.SetProperty(Config.IntraDaySeqnumReset, "false");

			ClearLogs();
			WaitReject = new CountdownEvent(1);

			Server = new FixServer();
			Server.SetPort(Port);
			Server.SetListener(new FixServerListenerAnonymousInnerClass(this));
		}

		private class FixServerListenerAnonymousInnerClass : IFixServerListener
		{
			private readonly AcceptorFixSessionTest _outerInstance;

			public FixServerListenerAnonymousInnerClass(AcceptorFixSessionTest outerInstance)
			{
				_outerInstance = outerInstance;
			}

			public void NewFixSession(IFixSession session)
			{
				try
				{
					_outerInstance.WaitReject.Wait();
					session.Reject(_outerInstance.RejectReason);
					session.Dispose();
				}
				catch (IOException e)
				{
					Console.WriteLine(e.ToString());
					Console.Write(e.StackTrace);
				}
				catch (ThreadInterruptedException e)
				{
					Console.WriteLine(e.ToString());
					Console.Write(e.StackTrace);  
				}
			}
		}

		public virtual bool ClearLogs()
		{
			var logsCleaner = new LogsCleaner();
			return logsCleaner.Clean("./logs") && logsCleaner.Clean("./logs/backup");
		}

		[TearDown]
		public virtual void TearDown()
		{
			Server.Stop();
			FixSessionManager.DisposeAllSession();
			ConfigurationHelper.RestoreGlobalConfig();
		}

		[Test, Timeout(30000)]
		public virtual void TestRejectTwoMessages()
		{
			Server.Start();

			var helper = new SimplestFixSessionHelper("localhost", Port);
			helper.Connect();
			var logonMessage = GetLogonMessage(1);
			var newsMessage = GetNewsMessage(2);
			var buff = new byte[logonMessage.Length + newsMessage.Length];
			Array.Copy(logonMessage, 0, buff, 0, logonMessage.Length);
			Array.Copy(newsMessage, 0, buff, logonMessage.Length, newsMessage.Length);
			helper.SendMessage(buff);
			WaitReject.Signal();
			helper.WaitTransportDown();
			Server.Stop();
			ValidateReject(helper.GetMessages());
		}

		[Test, Timeout(30000)]
		public virtual void TestManualResetSeqNumOnConnectedSession()
		{
			var serverListener = new EchoServer();
			Server.SetListener(serverListener);
			Server.Start();

			var helper = new SimplestFixSessionHelper("localhost", Port);
			// first connect
			helper.Connect();
			helper.PrepareToReceiveMessages(2);
			helper.SendMessage(GetLogonMessage(1));
			helper.SendMessage(GetNewsMessage(2));
			helper.WaitForMessages(5000);
			ClassicAssertTypeAndSeqNum(helper.GetMessages()[0], "A", 1);
			ClassicAssertTypeAndSeqNum(helper.GetMessages()[1], "B", 2);

			// reset seqNum
			helper.PrepareToReceiveMessages(1);

			CheckingUtils.CheckWithinTimeout(() => serverListener.Session != null, TimeSpan.FromSeconds(1));

			// make manual reset
			serverListener.Session.ResetSequenceNumbers();
			helper.WaitForMessages(3000);
			ClassicAssertTypeAndSeqNum(helper.GetMessages()[0], "A", 1);

			helper.PrepareToReceiveMessages(1);
			helper.SendMessage(GetLogonMessage(1, "141=Y#"));
			// send msg foe test new seqNum
			helper.SendMessage(GetNewsMessage(2));
			helper.WaitForMessages(3000);
			ClassicAssertTypeAndSeqNum(helper.GetMessages()[0], "B", 2);

			// logout
			helper.SendMessage(GetLogoutMessage(3));
			helper.WaitTransportDown();
			serverListener.Session.Dispose();

			// second connect
			helper.Connect();
			helper.PrepareToReceiveMessages(1);
			// expect to continue seqNum
			helper.SendMessage(GetLogonMessage(4));
			helper.WaitForMessages(3000);
			ClassicAssertTypeAndSeqNum(helper.GetMessages()[0], "A", 4);
			Server.Stop();
		}

		[Test, Timeout(30000)]
		public virtual void TestManualResetSeqNumOnConfiguredOfflineSession()
		{
			var serverListener = new EchoServer();
			Server.SetListener(serverListener);
			Server.Start();

			var helper = new SimplestFixSessionHelper("localhost", Port);
			// first connect
			helper.Connect();
			helper.PrepareToReceiveMessages(2);
			helper.SendMessage(GetLogonMessage(1));
			helper.SendMessage(GetNewsMessage(2));
			helper.WaitForMessages(5000);
			ClassicAssertTypeAndSeqNum(helper.GetMessages()[0], "A", 1);
			ClassicAssertTypeAndSeqNum(helper.GetMessages()[1], "B", 2);

			// logout
			helper.SendMessage(GetLogoutMessage(3));
			helper.WaitTransportDown();

			// reset on offline session
			serverListener.Session.ResetSequenceNumbers();
			serverListener.Session.Dispose();

			// second connect
			helper.Connect();
			helper.PrepareToReceiveMessages(2);
			helper.SendMessage(GetLogonMessage(1));
			helper.SendMessage(GetNewsMessage(2));
			helper.WaitForMessages(5000);
			ClassicAssertTypeAndSeqNum(helper.GetMessages()[0], "A", 1);
			ClassicAssertTypeAndSeqNum(helper.GetMessages()[1], "B", 2);

			// logout
			helper.SendMessage(GetLogoutMessage(3));
			helper.WaitTransportDown();
			serverListener.Session.Dispose();

			// third connect
			helper.Connect();
			helper.PrepareToReceiveMessages(2);
			helper.SendMessage(GetLogonMessage(4));
			helper.SendMessage(GetNewsMessage(5));
			helper.WaitForMessages(500000);
			ClassicAssertTypeAndSeqNum(helper.GetMessages()[0], "A", 4);
			ClassicAssertTypeAndSeqNum(helper.GetMessages()[1], "B", 5);

			Server.Stop();
		}

		[Test, Timeout(30000)]
		public virtual void TestCustomResetSeqNumOnConfiguredOfflineSession()
		{
			FixSessionManager.DisposeAllSession();
			var serverListener = new EchoServer();
			Server.SetListener(serverListener);
			Server.Start();

			var helper = new SimplestFixSessionHelper("localhost", Port);
			// first connect
			helper.Connect();
			helper.PrepareToReceiveMessages(2);
			helper.SendMessage(GetLogonMessage(1));
			helper.SendMessage(GetNewsMessage(2));
			helper.WaitForMessages(5000);
			ClassicAssertTypeAndSeqNum(helper.GetMessages()[0], "A", 1);
			ClassicAssertTypeAndSeqNum(helper.GetMessages()[1], "B", 2);

			// logout
			helper.SendMessage(GetLogoutMessage(3));
			helper.WaitTransportDown();
			// reset on offline session
			serverListener.Session.Dispose();
			var sp = serverListener.Session.Parameters;
			Server.UnregisterAcceptorSession(sp);
	//        sp.SetIncomingSequenceNumber(0);
			sp.IncomingSequenceNumber = 1;
	//        sp.SetOutgoingSequenceNumber(0);
			sp.OutgoingSequenceNumber = 1;
			Server.RegisterAcceptorSession(sp);

			// second connect
			helper.Connect();
			helper.PrepareToReceiveMessages(2);
			helper.SendMessage(GetLogonMessage(1));
			helper.SendMessage(GetNewsMessage(2));
			helper.WaitForMessages(5000);
			ClassicAssertTypeAndSeqNum(helper.GetMessages()[0], "A", 1);
			ClassicAssertTypeAndSeqNum(helper.GetMessages()[1], "B", 2);

			// logout
			helper.SendMessage(GetLogoutMessage(3));
			helper.WaitTransportDown();
			serverListener.Session.Dispose();

			// third connect
			helper.Connect();
			helper.PrepareToReceiveMessages(2);
			helper.SendMessage(GetLogonMessage(4));
			helper.SendMessage(GetNewsMessage(5));
			helper.WaitForMessages(5000);
			ClassicAssertTypeAndSeqNum(helper.GetMessages()[0], "A", 4);
			ClassicAssertTypeAndSeqNum(helper.GetMessages()[1], "B", 5);

			Server.Stop();
		}

		[Test, Timeout(30000)]
		public virtual void TestCustomSetupSeqNumOnConfiguredOfflineSession()
		{
			var serverListener = new EchoServer();
			Server.SetListener(serverListener);
			Server.Start();

			var helper = new SimplestFixSessionHelper("localhost", Port);
			// first connect
			helper.Connect();
			helper.PrepareToReceiveMessages(2);
			helper.SendMessage(GetLogonMessage(1));
			helper.SendMessage(GetNewsMessage(2));
			helper.WaitForMessages(5000);
			ClassicAssertTypeAndSeqNum(helper.GetMessages()[0], "A", 1);
			ClassicAssertTypeAndSeqNum(helper.GetMessages()[1], "B", 2);

			// logout
			helper.SendMessage(GetLogoutMessage(3));
			helper.WaitTransportDown();
			// reset on offline session
			serverListener.Session.Dispose();

			var sp = serverListener.Session.Parameters;
			Server.UnregisterAcceptorSession(sp);
	//        sp.SetIncomingSequenceNumber(0);
			sp.IncomingSequenceNumber = 11;
	//        sp.SetOutgoingSequenceNumber(0);
			sp.OutgoingSequenceNumber = 11;
			Server.RegisterAcceptorSession(sp);

			// second connect
			helper.Connect();
			helper.PrepareToReceiveMessages(2);
			helper.SendMessage(GetLogonMessage(11));
			helper.SendMessage(GetNewsMessage(12));
			helper.WaitForMessages(5000);
			ClassicAssertTypeAndSeqNum(helper.GetMessages()[0], "A", 11);
			ClassicAssertTypeAndSeqNum(helper.GetMessages()[1], "B", 12);

			// logout
			helper.SendMessage(GetLogoutMessage(13));
			helper.WaitTransportDown();
			serverListener.Session.Dispose();

			// third connect
			helper.Connect();
			helper.PrepareToReceiveMessages(2);
			helper.SendMessage(GetLogonMessage(14));
			helper.SendMessage(GetNewsMessage(15));
			helper.WaitForMessages(5000);
			ClassicAssertTypeAndSeqNum(helper.GetMessages()[0], "A", 14);
			ClassicAssertTypeAndSeqNum(helper.GetMessages()[1], "B", 15);

			Server.Stop();
		}

		[Test, Timeout(10000)]
		public virtual void TestAcceptorSessionRegistration()
		{
			var serverListener = new EchoServer();
			Server.SetListener(serverListener);
			Server.Start();

			var helper = new SimplestFixSessionHelper("localhost", Port);
			// first connect
			helper.Connect();
			helper.PrepareToReceiveMessages(1);
			helper.SendMessage(GetLogonMessage(1));
			helper.WaitForMessages(5000);
			ClassicAssertTypeAndSeqNum(helper.GetMessages()[0], "A", 1);

			// logout
			helper.SendMessage(GetLogoutMessage(2));
			helper.WaitTransportDown();
			// register without port
			serverListener.Session.Dispose();
			var sp = serverListener.Session.Parameters;
			Server.UnregisterAcceptorSession(sp);
			Server.RegisterAcceptorSession(sp);

			// register with custom port
			Server.UnregisterAcceptorSession(sp);
			sp.Port = Port;
			Server.RegisterAcceptorSession(sp);

			Server.Stop();
		}

		[Test, Timeout(10000)]
		public virtual void TestAcceptorSessionRegistrationWithIllegalCustomPort()
		{
			var serverListener = new EchoServer();
			Server.SetListener(serverListener);
			Server.Start();

			var helper = new SimplestFixSessionHelper("localhost", Port);
			// first connect
			helper.Connect();
			helper.PrepareToReceiveMessages(1);
			helper.SendMessage(GetLogonMessage(1));
			helper.WaitForMessages(5000);
			ClassicAssertTypeAndSeqNum(helper.GetMessages()[0], "A", 1);

			// logout
			helper.SendMessage(GetLogoutMessage(2));
			helper.WaitTransportDown();
			// register with illegal port
			serverListener.Session.Dispose();
			var sp = serverListener.Session.Parameters;
			Server.UnregisterAcceptorSession(sp);
			sp.Port = Port + 1;
			try
			{
				ClassicAssert.Throws<ArgumentException>(() => Server.RegisterAcceptorSession(sp));
			}
			finally
			{
				Server.Stop();
			}
		}

		[Test, Timeout(30000)]
		public virtual void TestCustomPortOfRegisteredSession()
		{
			var serverListener = new EchoServer();
			Server.SetListener(serverListener);
			Server.Start();

			var helper = new SimplestFixSessionHelper("localhost", Port);
			// first connect
			helper.Connect();
			helper.PrepareToReceiveMessages(1);
			helper.SendMessage(GetLogonMessage(1));
			helper.WaitForMessages(5000);
			ClassicAssertTypeAndSeqNum(helper.GetMessages()[0], "A", 1);

			// logout
			helper.SendMessage(GetLogoutMessage(2));
			helper.WaitTransportDown();

			// reset custom port on offline session
			serverListener.Session.Dispose();
			var sp = serverListener.Session.Parameters;
			Server.UnregisterAcceptorSession(sp);
			Server.Stop();

			Config.GlobalConfiguration.SetProperty(Config.ServerAcceptorStrategy, typeof(DenyNonRegisteredAcceptorStrategyHandler).FullName);
			ClearLogs();

			var port2 = Port + 1;
			Server = new FixServer();
			Server.Ports = new []{Port, port2};
			Server.SetListener(serverListener);
			Server.Start();

			sp.Port = port2;
			Server.RegisterAcceptorSession(sp);

			// second connect to the prev port (exception is expected)
			helper.Connect();
			helper.Messages.Clear();
			helper.SendMessage(GetLogonMessage(1));
			helper.WaitTransportDown();
			ClassicAssert.AreEqual(0, helper.Messages.Count, "Session should be closed without communication");

			//start new initiator to PORT2
			helper = new SimplestFixSessionHelper("localhost", port2);
			// connect to expected port
			helper.Connect();
			helper.PrepareToReceiveMessages(1);
			helper.SendMessage(GetLogonMessage(1));
			helper.WaitForMessages(5000);
			ClassicAssertTypeAndSeqNum(helper.GetMessages()[0], "A", 1);

			// logout
			helper.SendMessage(GetLogoutMessage(2));
			helper.WaitTransportDown();

			// reset custom port on offline session
			serverListener.Session.Dispose();
			//assetTypeAndSeqNum(helper.GetMessages().get(0), "5", 3);
		}

		[Test, Timeout(60000)]
		public virtual void TestTwoRejectAndOneAcceptSession()
		{
			Server.SetListener(new FixServerListenerAnonymousInnerClass2(this));
			var helper = new SimplestFixSessionHelper("localhost", Port);

			Server.Start();
			// first connect
			helper.Connect();
			helper.SendMessage(GetLogonMessage(1));

			// session reject
			helper.WaitTransportDown();
			ValidateReject(helper.GetMessages());
			helper.GetMessages().Clear();

			// second connect
			helper.Connect();
			helper.SendMessage(GetLogonMessage(3)); // seqNum=2 is response on logout

			// session reject
			helper.WaitTransportDown();
			ValidateReject(helper.GetMessages());

			// third connect
			helper.PrepareToReceiveMessages(2);
			helper.Connect();
			helper.SendMessage(GetLogonMessage(5)); // seqNum=4 is response on logout
			helper.WaitForMessages(2000);

			var successResponse = helper.GetMessages();


			ClassicAssert.AreEqual("A", successResponse[0].GetTagValueAsString(35), "Respond Logon");
			var rrMsg = successResponse[1];
			ClassicAssert.AreEqual("2", rrMsg.GetTagValueAsString(35), "Respond RR: " + rrMsg);
			ClassicAssert.AreEqual(2, rrMsg.GetTagValueAsLong(7), "BeginSeqNo " + rrMsg);
			ClassicAssert.AreEqual(0, rrMsg.GetTagValueAsLong(16), "EndSeqNo");

			Server.Stop();
		}

		private class FixServerListenerAnonymousInnerClass2 : IFixServerListener
		{
			private readonly AcceptorFixSessionTest _outerInstance;

			public FixServerListenerAnonymousInnerClass2(AcceptorFixSessionTest outerInstance)
			{
				_outerInstance = outerInstance;
				Count = 0;
			}

			internal int Count;
			public void NewFixSession(IFixSession session)
			{
				try
				{
					if (Count++ < 2)
					{
						session.Reject(_outerInstance.RejectReason);
						session.Dispose();
					}
					else
					{
						session.Connect();
					}
				}
				catch (Exception e)
				{
					Console.WriteLine(e.ToString());
					Console.Write(e.StackTrace);
				}
			}
		}

		[Test, Timeout(60000)]
		public virtual void TestTwoRejectRestartAcceptorAndOneAcceptSession()
		{
			Server.SetListener(new FixServerListenerAnonymousInnerClass3(this));
			var helper = new SimplestFixSessionHelper("localhost", Port);

			Server.Start();
			// first connect
			helper.Connect();
			helper.SendMessage(GetLogonMessage(1));

			// session reject
			helper.WaitTransportDown();
			ValidateReject(helper.GetMessages());
			helper.GetMessages().Clear();

			// second connect
			helper.Connect();
			helper.SendMessage(GetLogonMessage(3)); // seqNum=2 is response on logout

			// session reject
			helper.WaitTransportDown();
			ValidateReject(helper.GetMessages());

			// reset acceptor
			Server.Stop();

			Server.Start();

			// third connect
			helper.PrepareToReceiveMessages(2);
			helper.Connect();
			helper.SendMessage(GetLogonMessage(5)); // seqNum=4 is response on logout
			helper.WaitForMessages(2000);
			helper.Disconnect();

			var successResponse = helper.GetMessages();

			ClassicAssert.AreEqual("A", successResponse[0].GetTagValueAsString(35), "Respond Logon");
			var rrMsg = successResponse[1];
			ClassicAssert.AreEqual("2", rrMsg.GetTagValueAsString(35), "Respond RR: " + rrMsg);
			ClassicAssert.AreEqual(2, rrMsg.GetTagValueAsLong(7), "BeginSeqNo " + rrMsg);
			ClassicAssert.AreEqual(0, rrMsg.GetTagValueAsLong(16), "EndSeqNo");

			Server.Stop();
		}

		private class FixServerListenerAnonymousInnerClass3 : IFixServerListener
		{
			private readonly AcceptorFixSessionTest _outerInstance;

			public FixServerListenerAnonymousInnerClass3(AcceptorFixSessionTest outerInstance)
			{
				_outerInstance = outerInstance;
				Count = 0;
			}

			internal int Count;
			public void NewFixSession(IFixSession session)
			{
				try
				{
					if (Count++ < 2)
					{
						session.Reject(_outerInstance.RejectReason);
						session.Dispose();
					}
					else
					{
						session.Connect();
					}
				}
				catch (Exception e)
				{
					Console.WriteLine(e.ToString());
					Console.Write(e.StackTrace);
				}
			}
		}

		[Test, Timeout(3000)]
		public virtual void TestDuplicateResetSequenceNumberTagOnAcceptorLogon()
		{
			var serverListener = new EchoServer();
			Server.SetListener(serverListener);
			Server.Start();

			var helper = new SimplestFixSessionHelper("localhost", Port);
			helper.Connect();
			helper.PrepareToReceiveMessages(1);
			helper.SendMessage(GetLogonMessage(1, "141=Y#"));
			helper.WaitForMessages(5000);
			ClassicAssertTypeAndSeqNum(helper.GetMessages()[0], "A", 1);
			ClassicAssertUnexpectedDuplicateTag(helper.GetMessages()[0], Tags.ResetSeqNumFlag, 1);

			helper.SendMessage(GetLogoutMessage(2));
			helper.WaitTransportDown();
			serverListener.Session.Dispose();

			Server.Stop();
		}

		private void ValidateReject(IList<FixMessage> messages)
		{
			ClassicAssert.AreEqual(1, messages.Count, "Invalid message count: " + messages);
			var response = messages[0];
			ClassicAssert.AreEqual("5", response.GetTagValueAsString(35), "Respond Logout");
			ClassicAssert.AreEqual(RejectReason, response.GetTagValueAsString(58), "Logout reason");
		}

		private void ClassicAssertUnexpectedDuplicateTag(FixMessage msg, int tagId, int allowableCount)
		{
			ClassicAssert.AreEqual(FixMessage.NotFound, msg.GetTagIndex(tagId, ++allowableCount), string.Format("Unexpected duplicate tag: '{0}' in message: {1}", tagId, msg));
		}

		private class EchoServer : IFixServerListener
		{
			internal IFixSession Session;
			public virtual void NewFixSession(IFixSession session)
			{
				session.SetFixSessionListener(new FixSessionListenerAnonymousInnerClass(session));
				try
				{
					session.Connect();
				}
				catch (IOException e)
				{
					Console.WriteLine(e.ToString());
					Console.Write(e.StackTrace);
				}
				Session = session;
			}

			private class FixSessionListenerAnonymousInnerClass : IFixSessionListener
			{
				private IFixSession _session;

				public FixSessionListenerAnonymousInnerClass(IFixSession session)
				{
					_session = session;
				}

				public void OnSessionStateChange(SessionState sessionState)
				{
				}
				public void OnNewMessage(FixMessage message)
				{
					_session.SendMessage(message);
				}
			}
		}
	}
}
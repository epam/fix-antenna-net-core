// Copyright (c) 2022 EPAM Systems
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
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.TestUtils;
using NUnit.Framework;
using static Epam.FixAntenna.NetCore.FixEngine.Session.TestMessageHelper;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	internal class AcceptorResetSeqNumFromFirstLogonTest
	{
		private const int Port = 12345;
		private FixServer _server;

		[SetUp]
		public void SetUp()
		{
			ConfigurationHelper.StoreGlobalConfig();
			// return to default values
			Config.GlobalConfiguration.SetProperty(Config.StorageCleanupMode, "None");
			Config.GlobalConfiguration.SetProperty(Config.IntraDaySeqnumReset, "false");
			LogsCleaner.ClearDefaultLogs();

			_server = new FixServer();
			_server.SetPort(Port);
		}

		[TearDown]
		public void TearDown(){
			FixSessionManager.DisposeAllSession();
			_server?.Stop();
			ConfigurationHelper.RestoreGlobalConfig();
		}

		public SimplestFixSessionHelper SetupTestCase(
			ResetSeqNumFromFirstLogonMode givenResetMode,
			string givenTradePeriodBegin,
			string givenTradePeriodEnd,
			long givenLastSeqNumResetTimestamp)
		{
			Config.GlobalConfiguration.SetProperty(Config.ResetSeqNumFromFirstLogon, givenResetMode.ToString());

			if (givenTradePeriodBegin != null)
			{
				Config.GlobalConfiguration.SetProperty(Config.TradePeriodBegin, givenTradePeriodBegin);
			}

			if (givenTradePeriodEnd != null)
			{
				Config.GlobalConfiguration.SetProperty(Config.TradePeriodEnd, givenTradePeriodEnd);
			}

			_server.SetListener(
				new ServerListener(session => session.Parameters.LastSeqNumResetTimestamp = givenLastSeqNumResetTimestamp)
				);
			_server.Start();

			var helper = new SimplestFixSessionHelper("localhost", Port);
			// first connect
			helper.Connect();
			helper.PrepareToReceiveMessages(2);
			helper.SendMessage(GetLogonMessage(1));
			helper.SendMessage(GetNewsMessage(2));
			helper.WaitForMessages(5000);
			AssertTypeAndSeqNum(helper.GetMessages()[0], "A", 1);
			AssertTypeAndSeqNum(helper.GetMessages()[1], "B", 2);

			// logout
			helper.SendMessage(GetLogoutMessage(3));
			helper.WaitTransportDown();

			return helper;
		}

		[Test, Timeout(30000)]
		public void TestResetSeqNumbersFromFirstLogon()
		{
			/*
			 * Timeline:
			 * _____TRADING____                _________TRADING_________
			 *                 |              |                         |
			 * ----------------*-------*------*------|------------------*--
			 *                END    RESET  BEGIN   NOW                END
			 *                        -2m    -1m                       +10m
			 */
			var now = DateTimeOffset.UtcNow;
			var helper = SetupTestCase(
				ResetSeqNumFromFirstLogonMode.Schedule,
				GetCronExpression(now - TimeSpan.FromMinutes(1)),
				GetCronExpression(now + TimeSpan.FromMinutes(10)),
				(now - TimeSpan.FromMinutes(2)).TotalMilliseconds());

			// second connect
			helper.Connect();
			helper.PrepareToReceiveMessages(2);

			//When
			//Send Logon with sequence too high and high next expected sequence
			helper.SendMessage(GetLogonMessage(5, "789=8#"));
			helper.SendMessage(GetNewsMessage(6));
			helper.WaitForMessages(5000);

			//Then
			//Reset by Logon is applied as there was no reset in current trading period
			AssertTypeAndSeqNum(helper.GetMessages()[0], "A", 8);
			AssertTypeAndSeqNum(helper.GetMessages()[1], "B", 9);
		}

		[Test, Timeout(30000)]
		public void TestResetOutSeqNumbersFromFirstLogonFirstSessionConnection()
		{
	        /*
	         * Timeline:
	         *                  _________TRADING_________
	         *                 |                         |
	         *        *    X---*------|------------------*--
	         *       END     BEGIN   NOW                END
	         *                -1m                       +10m
	         */

	        var now = DateTimeOffset.UtcNow;
	        Config.GlobalConfiguration.SetProperty(Config.ResetSeqNumFromFirstLogon, ResetSeqNumFromFirstLogonMode.Schedule.ToString());
	        Config.GlobalConfiguration.SetProperty(Config.TradePeriodBegin, GetCronExpression(now - TimeSpan.FromMinutes(1)));
	        Config.GlobalConfiguration.SetProperty(Config.TradePeriodEnd, GetCronExpression(now + TimeSpan.FromMinutes(10)));

	        _server.SetListener(
		        new ServerListener(session => session.Parameters.LastSeqNumResetTimestamp = 0)
	        );
	        _server.Start();

			var helper = new SimplestFixSessionHelper("localhost", Port);
			// first connect
			helper.Connect();
			helper.PrepareToReceiveMessages(2);

			//When
			//Send Logon with ordered 34=1 and too high next expected (789)
			helper.SendMessage(GetLogonMessage(1, "789=8#"));
			helper.SendMessage(GetNewsMessage(2));
			helper.WaitForMessages(5000);

			//Then
			//Rest by Logon is applied as the session is new and there was no reset in current trading period
			AssertTypeAndSeqNum(helper.GetMessages()[0], "A", 8);
			AssertTypeAndSeqNum(helper.GetMessages()[1], "B", 9);
		}

		[Test, Timeout(30000)]
		public void TestResetSeqNumbersFromFirstLogonWithResetFlagN()
		{
			/*
			 * Timeline:
			 * _____TRADING____                __________TRADING__________
			 *                 |              |                           |
			 * ----------------*------|-------*---------------------------*--
			 *                END    NOW    BEGIN                        END
			 *                               +1m                         +10m
			 */
			var now = DateTimeOffset.UtcNow;
			var helper = SetupTestCase(
				ResetSeqNumFromFirstLogonMode.Schedule,
				GetCronExpression(now + TimeSpan.FromMinutes(1)),
				GetCronExpression(now + TimeSpan.FromMinutes(10)),
				0);

			// second connect
			helper.Connect();
			helper.PrepareToReceiveMessages(2);
			helper.SendMessage(GetLogonMessage(5, "141=N#789=8#"));
			helper.SendMessage(GetNewsMessage(6));
			helper.WaitForMessages(5000);
			AssertTypeAndSeqNum(helper.GetMessages()[0], "A", 8);
			AssertTypeAndSeqNum(helper.GetMessages()[1], "B", 9);
		}

		[Test, Timeout(30000)]
		public void TestResetSeqNumbersFromFirstLogonWithResetFlagNStartInTradePeriod()
		{
			/*
			 * Timeline:
			 * _____TRADING____           ____________TRADING_____________
			 *                 |         |                                |
			 * ----------------*---------*---------|----------------------*--
			 *                END      BEGIN      NOW                    END
			 *                          -1m                             +10m
			 */
			var now = DateTimeOffset.UtcNow;
			var helper = SetupTestCase(
				ResetSeqNumFromFirstLogonMode.Schedule,
				GetCronExpression(now - TimeSpan.FromMinutes(1)),
				GetCronExpression(now + TimeSpan.FromMinutes(10)),
				0);

			// second connect
			helper.Connect();
			helper.PrepareToReceiveMessages(2);
			helper.SendMessage(GetLogonMessage(5, "141=N#789=8#"));
			helper.SendMessage(GetNewsMessage(6));
			helper.WaitForMessages(5000);
			AssertTypeAndSeqNum(helper.GetMessages()[0], "A", 8);
			AssertTypeAndSeqNum(helper.GetMessages()[1], "B", 9);
		}

		[Test, Timeout(30000)]
		public void TestResetBothSeqNumbersFromFirstLogonFirstSessionConnection()
		{
			/*
			 * Timeline:
			 *                  _________TRADING_________
			 *                 |                         |
			 *        *    X---*------|------------------*--
			 *       END     BEGIN   NOW                END
			 *                -1m                       +10m
			 */
			var now = DateTimeOffset.UtcNow;
			Config.GlobalConfiguration.SetProperty(Config.ResetSeqNumFromFirstLogon, ResetSeqNumFromFirstLogonMode.Schedule.ToString());
			Config.GlobalConfiguration.SetProperty(Config.TradePeriodBegin, GetCronExpression(now - TimeSpan.FromMinutes(1)));
			Config.GlobalConfiguration.SetProperty(Config.TradePeriodEnd, GetCronExpression(now + TimeSpan.FromMinutes(10)));

			_server.SetListener(
				new ServerListener(session => session.Parameters.LastSeqNumResetTimestamp = 0)
			);
			_server.Start();

			var helper = new SimplestFixSessionHelper("localhost", Port);
			// first connect
			helper.Connect();
			helper.PrepareToReceiveMessages(2);

			//When
			//Send Logon with too high sequence number and too high next expected (789)
			helper.SendMessage(GetLogonMessage(5, "789=8#"));
			helper.SendMessage(GetNewsMessage(6));
			helper.WaitForMessages(5000);

			//Then
			//Rest by Logon is applied as the session is new and there was no reset in current trading period
			AssertTypeAndSeqNum(helper.GetMessages()[0], "A", 8);
			AssertTypeAndSeqNum(helper.GetMessages()[1], "B", 9);
		}

		[Test, Timeout(30000)]
		public void TestResetSeqNumbersFromFirstLogon789TagInResponseLogon()
		{
			Config.GlobalConfiguration.SetProperty(Config.HandleSeqnumAtLogon, "true");
			var now = DateTimeOffset.UtcNow;
			var helper = SetupTestCase(
				ResetSeqNumFromFirstLogonMode.Schedule,
				GetCronExpression(now - TimeSpan.FromMinutes(1)),
				GetCronExpression(now + TimeSpan.FromMinutes(10)),
				0);

			// second connect
			helper.Connect();
			helper.PrepareToReceiveMessages(1);
			helper.SendMessage(GetLogonMessage(5, "789=8#"));
			helper.WaitForMessages(5000);
			AssertTypeAndSeqNum(helper.GetMessages()[0], "A", 8);
			Assert.IsTrue(helper.GetMessages()[0].IsTagExists(789));
			Assert.AreEqual(6, helper.GetMessages()[0].GetTagAsInt(789));
			Config.GlobalConfiguration.SetProperty(Config.HandleSeqnumAtLogon, "false");
		}

		[Test, Timeout(30000)]
		public void TestNotResetSeqNumbersFromFirstLogonLastTimestampAfterStart()
		{
			/*
			 * Timeline:
			 * _____TRADING____         ___________TRADING_____________
			 *                 |       |                               |
			 * ----------------*-------*-----*------|------------------*--
			 *                END    BEGIN RESET   NOW                END
			 *                        -1m  -0.5m                      +10m
			 */
			var now = DateTimeOffset.UtcNow;
			var helper = SetupTestCase(
				ResetSeqNumFromFirstLogonMode.Schedule,
				GetCronExpression(now - TimeSpan.FromMinutes(1)),
				GetCronExpression(now + TimeSpan.FromMinutes(10)),
				(now - TimeSpan.FromMinutes(0.5)).TotalMilliseconds());

			// second connect
			helper.Connect();
			helper.PrepareToReceiveMessages(1);

			//When
			//Send Logon with too high sequence number and too high next expected (789)
			helper.SendMessage(GetLogonMessage(5, "789=8#"));
			helper.SendMessage(GetNewsMessage(6));
			helper.WaitForMessages(1000);

			//Then
			//Reset by Logon not applied because it was made in current trading period
			AssertTypeAndSeqNum(helper.GetMessages()[0], "5", 4);
		}

		[Test, Timeout(30000)]
		public void TestDisabledResetSeqNumbersFromFirstLogonForNeverMode()
		{
			/*
			 * Timeline:
			 * _____TRADING____                _________TRADING_________
			 *                 |              |                         |
			 * ----------------*-------*------*------|------------------*--
			 *                END    RESET  BEGIN   NOW                END
			 *                        -2m    -1m                       +10m
			 */
			var now = DateTimeOffset.UtcNow;
			var helper = SetupTestCase(
				ResetSeqNumFromFirstLogonMode.Never,
				GetCronExpression(now - TimeSpan.FromMinutes(1)),
				GetCronExpression(now + TimeSpan.FromMinutes(10)),
				(now - TimeSpan.FromMinutes(2)).TotalMilliseconds());

			// second connect
			helper.Connect();
			helper.PrepareToReceiveMessages(1);

			//When
			//Send Logon with too high sequence number and too high next expected (789)
			helper.SendMessage(GetLogonMessage(10, "789=10#"));
			helper.SendMessage(GetNewsMessage(11));
			helper.WaitForMessages(5000);

			//Then
			//Reset by Logon not applied for mode NEVER
			AssertTypeAndSeqNum(helper.GetMessages()[0], "5", 4);
		}

		[Test, Timeout(30000)]
		public void TestDisabledResetSeqNumbersFromFirstLogonWithHighSequences()
		{
			/*
			 * Timeline:
			 * _____TRADING____                _________TRADING_________
			 *                 |              |                         |
			 * ----------------*-------*------*------|------------------*--
			 *                END    RESET  BEGIN   NOW                END
			 *                        -2m    -1m                       +10m
			 */
			var now = DateTimeOffset.UtcNow;
			var helper = SetupTestCase(
				ResetSeqNumFromFirstLogonMode.Never,
				GetCronExpression(now - TimeSpan.FromMinutes(1)),
				GetCronExpression(now + TimeSpan.FromMinutes(10)),
				(now - TimeSpan.FromMinutes(2)).TotalMilliseconds());

			// second connect
			helper.Connect();
			helper.PrepareToReceiveMessages(3);

			//When
			//Send Logon with too high sequence number
			helper.SendMessage(GetLogonMessage(10));
			helper.SendMessage(GetNewsMessage(11));
			helper.WaitForMessages(5000);

			//Then
			//Reset by Logon not applied for mode NEVER and RR is sent for each message with too high sequence
			AssertTypeAndSeqNum(helper.GetMessages()[0], "A", 4);
			AssertTypeAndSeqNum(helper.GetMessages()[1], "2", 5);
			AssertTypeAndSeqNum(helper.GetMessages()[2], "2", 6);
		}

		[Test, Timeout(30000)]
		public void TestDisabledResetSeqNumbersFromFirstLogonWithLowSequences()
		{
			/*
			 * Timeline:
			 * _____TRADING____                _________TRADING_________
			 *                 |              |                         |
			 * ----------------*-------*------*------|------------------*--
			 *                END    RESET  BEGIN   NOW                END
			 *                        -2m    -1m                       +10m
			 */
			var now = DateTimeOffset.UtcNow;
			var helper = SetupTestCase(
				ResetSeqNumFromFirstLogonMode.Never,
				GetCronExpression(now - TimeSpan.FromMinutes(1)),
				GetCronExpression(now + TimeSpan.FromMinutes(10)),
				(now - TimeSpan.FromMinutes(2)).TotalMilliseconds());

			// second connect
			helper.Connect();
			helper.PrepareToReceiveMessages(1);

			//When
			//Send Logon with too low sequence number
			helper.SendMessage(GetLogonMessage(1));
			helper.SendMessage(GetNewsMessage(1));
			helper.WaitForMessages(5000);

			//Then
			//Reset by Logon not applied for mode NEVER and the session is closed due to low incoming sequence
			AssertTypeAndSeqNum(helper.GetMessages()[0], "5", 4);
		}

		[Test, Timeout(30000)]
		public void TestResetSeqNumbersFromFirstLogonWithResetFlag()
		{
			/*
			 * Timeline:
			 * _____TRADING____         ___________TRADING_____________
			 *                 |       |                               |
			 * ----------------*-------*-----*------|------------------*--
			 *                END    BEGIN RESET   NOW                END
			 *                        -1m  -0.5m                      +10m
			 */
			var now = DateTimeOffset.UtcNow;
			var helper = SetupTestCase(
				ResetSeqNumFromFirstLogonMode.Schedule,
				GetCronExpression(now - TimeSpan.FromMinutes(1)),
				GetCronExpression(now + TimeSpan.FromMinutes(10)),
				(now - TimeSpan.FromMinutes(0.5)).TotalMilliseconds());

			// second connect
			helper.Connect();
			helper.PrepareToReceiveMessages(2);

			//When
			//Send Logon with forced sequence reset flag
			helper.SendMessage(GetLogonMessage(1, "141=Y#"));
			helper.SendMessage(GetNewsMessage(2));
			helper.WaitForMessages(5000);

			//Then
			//Reset is applied due to forced sequence reset flag
			AssertTypeAndSeqNum(helper.GetMessages()[0], "A", 1);
			AssertTypeAndSeqNum(helper.GetMessages()[1], "B", 2);
		}

		[Test, Timeout(30000)]
		public void TestResetSeqNumbersFromFirstLogonWithoutNextExpectedSeqNum()
		{
			/*
			 * Timeline:
			 * _____TRADING____                _________TRADING_________
			 *                 |              |                         |
			 * ----------------*-------*------*------|------------------*--
			 *                END    RESET  BEGIN   NOW                END
			 *                        -2m    -1m                       +10m
			 */
			var now = DateTimeOffset.UtcNow;
			var helper = SetupTestCase(
				ResetSeqNumFromFirstLogonMode.Schedule,
				GetCronExpression(now - TimeSpan.FromMinutes(1)),
				GetCronExpression(now + TimeSpan.FromMinutes(10)),
				(now - TimeSpan.FromMinutes(2)).TotalMilliseconds());

			// second connect
			helper.Connect();
			helper.PrepareToReceiveMessages(2);

			//When
			//Send Logon with too low sequence number
			helper.SendMessage(GetLogonMessage(5));
			helper.SendMessage(GetNewsMessage(6));
			helper.WaitForMessages(5000);

			//Then
			//The reset by incoming Logon is applied as there was no reset in current trading period
			AssertTypeAndSeqNum(helper.GetMessages()[0], "A", 1);
			AssertTypeAndSeqNum(helper.GetMessages()[1], "B", 2);
		}
		
		[Test, Timeout(30000)]
		public void TestResetSeqNumbersFromFirstLogonAlreadyOccurredInThePeriod()
		{
			/*
			 * Timeline:
			 * _____TRADING____         ___________TRADING_____________
			 *                 |       |                               |
			 * ----------------*-------*-----*------|------------------*--
			 *                END    BEGIN RESET   NOW                END
			 *                        -1m  -0.5m                      +10m
			 */
			var now = DateTimeOffset.UtcNow;
			var helper = SetupTestCase(
				ResetSeqNumFromFirstLogonMode.Schedule,
				GetCronExpression(now - TimeSpan.FromMinutes(1)),
				GetCronExpression(now + TimeSpan.FromMinutes(10)),
				(now - TimeSpan.FromMinutes(0.5)).TotalMilliseconds());

			// second connect
			helper.Connect();
			helper.PrepareToReceiveMessages(2);

			//When
			//Send Logon with sequence too high
			helper.SendMessage(GetLogonMessage(10));
			helper.SendMessage(GetNewsMessage(11));
			helper.WaitForMessages(5000);

			//Then
			// Reset by Logon is not applied as it was done in this trading period
			// The gap should be recovered
			AssertTypeAndSeqNum(helper.GetMessages()[0], "A", 4);
			AssertTypeAndSeqNum(helper.GetMessages()[1], "2", 5);
		}

		[Test, Timeout(30000)]
		public void TestResetSeqNumbersFromLowSequenceFirstLogonAlreadyOccurredInThePeriod()
		{
			/*
			 * Timeline:
			 * _____TRADING____         ___________TRADING_____________
			 *                 |       |                               |
			 * ----------------*-------*-----*------|------------------*--
			 *                END    BEGIN RESET   NOW                END
			 *                        -1m  -0.5m                      +10m
			 */
			var now = DateTimeOffset.UtcNow;
			var helper = SetupTestCase(
				ResetSeqNumFromFirstLogonMode.Schedule,
				GetCronExpression(now - TimeSpan.FromMinutes(1)),
				GetCronExpression(now + TimeSpan.FromMinutes(10)),
				(now - TimeSpan.FromMinutes(0.5)).TotalMilliseconds());

			// second connect
			helper.Connect();
			helper.PrepareToReceiveMessages(1);

			//When
			//Send Logon with sequence too low
			helper.SendMessage(GetLogonMessage(1));
			helper.WaitForMessages(5000);

			//Then
			//Reset by Logon is not applied as it was done in this trading period
			//The session is closed due to low incoming sequence
			AssertTypeAndSeqNum(helper.GetMessages()[0], "5", 4);
		}

		[Test, Timeout(30000)]
		public void TestResetSeqNumbersFromFirstLogonOccurredAfterEndOfPeriod()
		{
			/*
			 * Timeline (weird case as reset can't be done in future):
			 * _____TRADING____        ______TRADING______
			 *                 |      |                   |
			 * ----------------*------*-------|-----------*--------*---
			 *                END   BEGIN    NOW         END     RESET
			 *                       -1m                 +10m    +20m
			 */
			var now = DateTimeOffset.UtcNow;
			var helper = SetupTestCase(
				ResetSeqNumFromFirstLogonMode.Schedule,
				GetCronExpression(now - TimeSpan.FromMinutes(1)),
				GetCronExpression(now + TimeSpan.FromMinutes(10)),
				(now + TimeSpan.FromMinutes(20)).TotalMilliseconds());

			// second connect
			helper.Connect();
			helper.PrepareToReceiveMessages(1);

			//When
			//Send Logon with too high sequence
			helper.SendMessage(GetLogonMessage(5));
			helper.SendMessage(GetNewsMessage(6));
			helper.WaitForMessages(5000);

			//Then
			//The session is closed as an internal state (last reset timestamp) is invalid
			AssertTypeAndSeqNum(helper.GetMessages()[0], "5", 4);
		}

		[Test, Timeout(30000)]
		public void TestResetSeqNumbersFromFirstLogonWithUndefinedSchedule()
		{
			var helper = SetupTestCase(ResetSeqNumFromFirstLogonMode.Schedule, null, null,  0);

			// second connect
			helper.Connect();
			helper.PrepareToReceiveMessages(1);

			//When
			//Send Logon with too high sequence number and too high next expected (789)
			helper.SendMessage(GetLogonMessage(5, "789=15#"));
			helper.WaitForMessages(5000);

			//Then
			//The session is worked, but seq num is not reset. 789 tag will be ignored
			// (trading period not defined)
			AssertTypeAndSeqNum(helper.GetMessages()[0], "A", 4);
		}

		[Test, Timeout(30000)]
		public void TestResetSeqNumbersFromFirstLogonWithUndefinedEndOfPeriod()
		{
			/*
			 * Timeline:
			 *                        _________TRADING________
			 *                       |                        
			 * ---------------*------*------|-----------------
			 *              RESET  BEGIN   NOW                
			 *               -2m    -1m                       
			 */
			var now = DateTimeOffset.UtcNow;
			var helper = SetupTestCase(
				ResetSeqNumFromFirstLogonMode.Schedule,
				GetCronExpression(now - TimeSpan.FromMinutes(1)),
				null,
				(now - TimeSpan.FromMinutes(2)).TotalMilliseconds());

			// second connect
			helper.Connect();
			helper.PrepareToReceiveMessages(2);

			//When
			//Send Logon with too high sequence number and too high next expected (789)
			helper.SendMessage(GetLogonMessage(5, "789=5#"));
			helper.SendMessage(GetNewsMessage(6));
			helper.WaitForMessages(5000);

			//Then
			//Rest by Logon is applied as the session is new and there was no reset in current trading period
			AssertTypeAndSeqNum(helper.GetMessages()[0], "A", 5);
			AssertTypeAndSeqNum(helper.GetMessages()[1], "B", 6);
		}

		[Test, Timeout(30000)]
		public void TestNotResetSeqNumbersFromFirstLogonWithUndefinedEndOfPeriod()
		{
			/*
			 * Timeline:
			 *                  _______________TRADING________
			 *                 |                              
			 * ----------------*------*-----|-----------------
			 *               BEGIN  RESET  NOW                
			 *                -1m   -0.5m                      
			 */
			var now = DateTimeOffset.UtcNow;
			var helper = SetupTestCase(
				ResetSeqNumFromFirstLogonMode.Schedule,
				GetCronExpression(now - TimeSpan.FromMinutes(1)),
				null,
				(now - TimeSpan.FromMinutes(0.5)).TotalMilliseconds());

			// second connect
			helper.Connect();
			helper.PrepareToReceiveMessages(1);

			//When
			//Send Logon with too high sequence number and too high next expected (789)
			helper.SendMessage(GetLogonMessage(5, "789=5#"));
			helper.WaitForMessages(5000);

			//Then
			//Reset by Logon is not applied as it was done in this trading period
			//The session is closed due to low incoming sequence
			AssertTypeAndSeqNum(helper.GetMessages()[0], "5", 4);
		}

		private class ServerListener: IFixServerListener
		{
			private readonly Action<IFixSession> _initSession;

			public ServerListener(Action<IFixSession> initSession)
			{
				_initSession = initSession;
			}

			public void NewFixSession(IFixSession session)
			{
				_initSession?.Invoke(session);
				session.SetFixSessionListener(new FixSessionListenerAnonymousInnerClass(session));

				try {
					session.Connect();
				} catch (IOException e) {
					Console.WriteLine(e.StackTrace);
				}
			}
		}

		private class FixSessionListenerAnonymousInnerClass : IFixSessionListener
		{
			private readonly IFixSession _session;

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

		private string GetCronExpression(DateTimeOffset date)
		{
			return $"0 {date.Minute} {date.Hour} * * ?";
		}
	}
}

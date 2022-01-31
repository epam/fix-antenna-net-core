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
using Epam.FixAntenna.NetCore.FixEngine.ResetLogon.Util;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.ResetLogon
{
	[TestFixture]
	internal class AbstractInitiatorResetTst
	{
		private const int Port = 9778;

		protected internal AcceptorSessionEmulator AcceptorSessionEmulator;
		protected internal IExtendedFixSession Session;

		[SetUp]
		public void SetUp()
		{
			ClearLogs();

			var sessionParameters = AcceptorDefaultProperties;
			AcceptorSessionEmulator = new AcceptorSessionEmulator(sessionParameters);

			Session = CreateInitiatorFixSession();

			AcceptorSessionEmulator.Open();
			Session.Connect();
			ExchangeWithLogons();
		}

		[TearDown]
		public virtual void TearDown()
		{
			AcceptorSessionEmulator.Close();
			FixSessionManager.DisposeAllSession();
			ClearLogs();
		}

		public virtual void ClearLogs()
		{
			new LogsCleaner().Clean("./logs");
			new LogsCleaner().Clean("./logs/backup");
		}

		public virtual SessionParameters AcceptorDefaultProperties
		{
			get
			{
				var sessionParameters = new SessionParameters();
				sessionParameters.Host = "localhost";
				sessionParameters.Port = Port;
				sessionParameters.HeartbeatInterval = 10;
				sessionParameters.SenderCompId = "1";
				sessionParameters.TargetCompId = "2";
				sessionParameters.IncomingSequenceNumber = 1;
				sessionParameters.OutgoingSequenceNumber = 1;
				return sessionParameters;
			}
		}

		public virtual IExtendedFixSession CreateInitiatorFixSession()
		{
			var sessionParameters = InitiatorDefaultProperties;
			return CreateInitiatorFixSession(sessionParameters);
		}

		public virtual SessionParameters InitiatorDefaultProperties
		{
			get
			{
				var sessionParameters = new SessionParameters();
				sessionParameters.Port = Port;
				sessionParameters.HeartbeatInterval = 10;
				sessionParameters.Host = "localhost";
				sessionParameters.SenderCompId = "2";
				sessionParameters.TargetCompId = "1";
				sessionParameters.Configuration.SetProperty(Config.CheckSendingTimeAccuracy, "false");
				return sessionParameters;
			}
		}

		public virtual IExtendedFixSession CreateInitiatorFixSession(SessionParameters sessionParameters)
		{
			return (IExtendedFixSession) sessionParameters.CreateNewFixSession();
		}

		public virtual void ResetSeqNums()
		{
			Session.ResetSequenceNumbers(true);

			AcceptorSessionEmulator.ReceiveTestRequest();
			AcceptorSessionEmulator.SendResponseToTestRequest();
			AcceptorSessionEmulator.ReceiveResetLogon();
		}

		public virtual void ExchangeWithLogons()
		{
			AcceptorSessionEmulator.ReceiveLogon();
			AcceptorSessionEmulator.SendLogon();
		}
	}
}
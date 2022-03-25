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
using Epam.FixAntenna.Core.Tests;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using NUnit.Framework;

namespace Epam.FixAntenna.NetCore.FixEngine.ResetLogon
{
	[TestFixture]
	internal class InitiatorBackupStorageOnOfflineResetTest : AbstractInitiatorResetTst
	{
		public override SessionParameters AcceptorDefaultProperties
		{
			get
			{
				var acceptorDefaultProperties = base.AcceptorDefaultProperties;
				acceptorDefaultProperties.Configuration.SetProperty(Config.IntraDaySeqnumReset, "false");
				return acceptorDefaultProperties;
			}
		}

		public override SessionParameters InitiatorDefaultProperties
		{
			get
			{
				var initiatorDefaultProperties = base.InitiatorDefaultProperties;
				initiatorDefaultProperties.Configuration.SetProperty(Config.CheckSendingTimeAccuracy, "false");
				initiatorDefaultProperties.Configuration.SetProperty(Config.IntraDaySeqnumReset, "false");
				initiatorDefaultProperties.Configuration.SetProperty(Config.StorageCleanupMode,
					StorageCleanupMode.Backup.ToString());
				return initiatorDefaultProperties;
			}
		}

		[Test]
		public virtual void TestBackupStorageWhenResetLogonReceived()
		{
			CheckEmptyBackupStorage(Session.Parameters);

			Session.Disconnect("request");
			AcceptorSessionEmulator.SendLogout("answer");
			CheckingUtils.CheckWithinTimeout(() => SessionState.IsDisconnected(Session.SessionState), TimeSpan.FromMilliseconds(Session.Parameters.HeartbeatInterval * 1000 + 500));
			AcceptorSessionEmulator.Close();

			Session.ResetSequenceNumbers();

			AcceptorSessionEmulator.Open();
			Session.Connect();

			AcceptorSessionEmulator.SendResetLogon();
			AcceptorSessionEmulator.ReceiveAnyMessage();

			CheckCreatedBackupStorage(Session.Parameters);
		}

		private void CheckCreatedBackupStorage(SessionParameters sessionParameters)
		{
			var configurationAdapter = new ConfigurationAdapter(sessionParameters.Configuration);
			Assert.AreEqual(3, CountMatchedFiles(configurationAdapter.BackupStorageDirectory, GetLogFileName(sessionParameters)));
		}

		private void CheckEmptyBackupStorage(SessionParameters sessionParameters)
		{
			var configurationAdapter = new ConfigurationAdapter(sessionParameters.Configuration);
			Assert.AreEqual(0, CountMatchedFiles(configurationAdapter.BackupStorageDirectory, GetLogFileName(sessionParameters)));
		}

		private string GetLogFileName(SessionParameters sessionParameters)
		{
			return sessionParameters.SenderCompId + "-" + sessionParameters.TargetCompId;
		}

		private void BackupSessionStorage()
		{
			ResetSeqNums();
		}

		private int CountMatchedFiles(string dir, string fileMaskPrefix)
		{
			string[] matches = { ".*\\.in$", ".*\\.out$", ".*\\.idx$" };
			return FileHelper.CountMatchedFiles(dir, fileMaskPrefix, matches);
		}
	}
}
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

using System.IO;
using System.Threading;
using Epam.FixAntenna.Core.Tests;
using Epam.FixAntenna.TestUtils;
using Epam.FixAntenna.NetCore.FixEngine.ResetLogon.Util;
using Epam.FixAntenna.NetCore.FixEngine.Storage.File;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.Helpers;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.ResetLogon
{
	[TestFixture]
	internal class InitiatorStorageIsNotBackupOnPrevTimeTest
	{
		private const int Port = 1778;
		private const long MillisInDay = 24 * 60 * 60 * 1000;

		private AcceptorSessionEmulator _sessionAcceptor;
		private IExtendedFixSession _session;
		private static string _dateFormat;
		private SessionParameters _initiatorSessionParameters;

		[SetUp]
		public void SetUp()
		{
			ClearLogs();

			var acceptorSessionParameters = new SessionParameters();
			acceptorSessionParameters.Configuration.SetProperty(Config.IntraDaySeqnumReset, "false");
			acceptorSessionParameters.Host = "localhost";
			acceptorSessionParameters.Port = Port;
			acceptorSessionParameters.SenderCompId = "1";
			acceptorSessionParameters.TargetCompId = "2";
			acceptorSessionParameters.IncomingSequenceNumber = 1;
			acceptorSessionParameters.OutgoingSequenceNumber = 1;
			_sessionAcceptor = new AcceptorSessionEmulator(acceptorSessionParameters);

			_initiatorSessionParameters = new SessionParameters();
			_initiatorSessionParameters.Port = Port;
			_initiatorSessionParameters.Host = "localhost";
			_initiatorSessionParameters.HeartbeatInterval = 10;
			_initiatorSessionParameters.SenderCompId = "2";
			_initiatorSessionParameters.TargetCompId = "1";
			_initiatorSessionParameters.Configuration.SetProperty(Config.CheckSendingTimeAccuracy, "false");
			_initiatorSessionParameters.Configuration.SetProperty(Config.PerformResetSeqNumTime, "true");

			var timeZone = "UTC";
			_dateFormat = "HH:mm:ss";

			// when session initialize - it matter whether the time has come today.
			// for example: last reset seqNum was in 3 AM
			//              reset seqNum is setup in config at 4 AM
			//              if we initiate session before 4 AM - we do nothing
			//              if we initiate session after 4 AM - we have to do reset SeqNum
			_initiatorSessionParameters.Configuration.SetProperty(Config.ResetSequenceTime,
				(DateTimeHelper.CurrentMilliseconds - 10000).ToDateTimeString(_dateFormat));

			_initiatorSessionParameters.Configuration.SetProperty(Config.ResetSequenceTimeZone, timeZone);
			_initiatorSessionParameters.Configuration.SetProperty(Config.IntraDaySeqnumReset, "false");
			_initiatorSessionParameters.Configuration.SetProperty(Config.StorageCleanupMode, StorageCleanupMode.Backup.ToString());

			_sessionAcceptor.Open();
		}

		[TearDown]
		public virtual void TearDown()
		{
			_sessionAcceptor.Close();
			FixSessionManager.DisposeAllSession();

	//        clearLogs();
		}

		private void ClearLogs()
		{
			var logsCleaner = new LogsCleaner();
			logsCleaner.Clean("./logs");
			logsCleaner.Clean("./logs/backup");
		}

		[Test]
		public virtual void TestSessionWhenBackupDidNotExistMoreThanOneDay()
		{
			_initiatorSessionParameters.LastSeqNumResetTimestamp = DateTimeHelper.CurrentMilliseconds - MillisInDay - 100000; // 1 day ago
			new SessionParameterPersistenceHelper(_initiatorSessionParameters).Store();

			WriteSomeDateToStorage();

			_session = (IExtendedFixSession) _initiatorSessionParameters.CreateInitiatorSession();

			ConnectAndExchangeWithLogons();

			Thread.Sleep(1000);

			CheckCreatedBackupStorage(_session.Parameters);
		}

		[Test]
		public virtual void TestSessionWhenBackupDidNotExistMoreThanHalfDayAndSessionInitiateBeforeNewReset()
		{
			var dateTime = (DateTimeHelper.CurrentMilliseconds + 10 * 60 * 1000).ToDateTimeString(_dateFormat);
			_initiatorSessionParameters.Configuration.SetProperty(Config.ResetSequenceTime, dateTime);
			_initiatorSessionParameters.LastSeqNumResetTimestamp = DateTimeHelper.CurrentMilliseconds - MillisInDay / 2; // half day ago
			new SessionParameterPersistenceHelper(_initiatorSessionParameters).Store();

			_session = (IExtendedFixSession)_initiatorSessionParameters.CreateInitiatorSession();

			WriteSomeDateToStorage();

			ConnectAndExchangeWithLogons();

			Thread.Sleep(2000);

			CheckEmptyBackupStorage(_session.Parameters);
		}

		[Test]
		public virtual void TestSessionWhenBackupDidNotExistMoreThanHalfDayAndSessionInitiateAfterNewReset()
		{
			var dateTime = (DateTimeHelper.CurrentMilliseconds - 10 * 60 * 1000).ToDateTimeString(_dateFormat);
			_initiatorSessionParameters.Configuration.SetProperty(Config.ResetSequenceTime, dateTime);
			_initiatorSessionParameters.LastSeqNumResetTimestamp = DateTimeHelper.CurrentMilliseconds - MillisInDay / 2; // half day ago
			new SessionParameterPersistenceHelper(_initiatorSessionParameters).Store();

			WriteSomeDateToStorage();

			_session = (IExtendedFixSession) _initiatorSessionParameters.CreateInitiatorSession();

			ConnectAndExchangeWithLogons();

			Thread.Sleep(1000);

			CheckCreatedBackupStorage(_session.Parameters);
		}

		private void WriteSomeDateToStorage()
		{
			var sessionParameters = _initiatorSessionParameters;
			var configurationAdapter = new ConfigurationAdapter(sessionParameters.Configuration);
			var storageFileName = new FileNameHelper(sessionParameters.SenderCompId, sessionParameters.TargetCompId)
				.GetStorageFileName(configurationAdapter.StorageDirectory);
			using (var fileOutStream =
				new FileStream(storageFileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
			{
				var bytes = "HelloWorld!!!\n".AsByteArray();
#if NET48
				fileOutStream.Write(bytes, 0, bytes.Length);
				fileOutStream.Write(bytes, 0, bytes.Length);
				fileOutStream.Write(bytes, 0, bytes.Length);
#else
				fileOutStream.Write(bytes);
				fileOutStream.Write(bytes);
				fileOutStream.Write(bytes);
#endif
			}
		}

		private void CheckCreatedBackupStorage(SessionParameters sessionParameters)
		{
			var configurationAdapter = new ConfigurationAdapter(sessionParameters.Configuration);
			ClassicAssert.AreEqual(1, CountMatchedFiles(configurationAdapter.BackupStorageDirectory));
		}

		private void CheckEmptyBackupStorage(SessionParameters sessionParameters)
		{
			var configurationAdapter = new ConfigurationAdapter(sessionParameters.Configuration);
			ClassicAssert.AreEqual(0, CountMatchedFiles(configurationAdapter.BackupStorageDirectory));
		}

		private void ConnectAndExchangeWithLogons()
		{
			_session.Connect();
			_sessionAcceptor.ReceiveLogon();
			_sessionAcceptor.SendLogon();
		}

		private int CountMatchedFiles(string dir)
		{
			string[] matches = {".*\\.in$", ".*\\.out$", ".*\\.idx$"};
			return FileHelper.CountMatchedFiles(dir, null, matches);
		}
	}
}
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
using System.Text;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Storage.File;
using Epam.FixAntenna.NetCore.FixEngine.Storage.Timestamp;
using Epam.FixAntenna.NetCore.Helpers;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Storage.File
{
	internal class FlatFileMessageStorageTest : AbstractMessageStorageTest
	{
		private const string PrefixFormatMilli = "yyyyMMdd HH:mm:ss.fff - ";
		private const string PrefixFormatMicro = "yyyyMMdd HH:mm:ss.ffffff - ";
		private const string PrefixFormatNano = "yyyyMMdd HH:mm:ss.fffffff00 - ";
		private long _instant;

		private string _storageFileName;

		[SetUp]
		public override void SetUp()
		{
			base.SetUp();

			_storageFileName =
				new FileNameHelper(Sender, Target).GetStorageFileName(ConfigurationAdapter.StorageDirectory);
			_instant = DateTimeHelper.CurrentTicks + 111222333;
		}

		public override AbstractFileMessageStorage GetInstanceMessageStorage()
		{
			return new FlatFileMessageStorage(ConfigurationAdapter.Configuration);
		}

		[Test]
		public virtual void TestRetrieveSequenceNumberFileLessThen1Kb()
		{
			using (var stream = new FileStream(_storageFileName, FileMode.OpenOrCreate, FileAccess.Write,
				FileShare.ReadWrite))
			{
				var bytes = "\x000134=2\x0001".AsByteArray();
#if NET48
				stream.Write(bytes, 0, bytes.Length);
#else
				stream.Write(bytes);
#endif
			}

			ClassicAssert.AreEqual(2,
				FlatFileMessageStorage.RetrieveSequenceNumber(
					FileNameHelper.GetStorageFileName(ConfigurationAdapter.StorageDirectory)));
		}

		[Test]
		public virtual void TestRetrieveSequenceNumberFileSizeBiggerThen1Kb()
		{
			using (var stream = new FileStream(_storageFileName, FileMode.OpenOrCreate, FileAccess.Write,
				FileShare.ReadWrite))
			{
				var bytes55 = "\x000134=55\x0001".AsByteArray();
				var bytes2 = "\x0001134=2\x0001".AsByteArray();
#if NET48
				stream.Write(bytes55, 0, bytes55.Length);
#else
				stream.Write(bytes55);
#endif
				for (var i = 0; i < 171; i++)
				{
#if NET48
					stream.Write(bytes2, 0, bytes2.Length);
#else
					stream.Write(bytes2);
#endif
				}
			}

			var foundSequenceNumber =
				FlatFileMessageStorage.RetrieveSequenceNumber(
					FileNameHelper.GetStorageFileName(ConfigurationAdapter.StorageDirectory));
			ClassicAssert.AreEqual(55, foundSequenceNumber);
		}

		[Test]
		public virtual void TestWrite1KMessages()
		{
			UpMessageStorage();
			var msgNum = 1000;
			var messages = new string[msgNum];
			for (var i = 0; i < msgNum; i++)
			{
				var arrMsg = GetNextMessage().AsByteArray();
				MessageStorage.AppendMessage(arrMsg);
				messages[i] = StringHelper.NewString(arrMsg);
			}

			ClassicAssertEqualsMessages(messages);
		}

		[Test]
		public virtual void TestWrite1KMessagesWithTimestamps()
		{
			UpMessageStorage();
			var msgNum = 100;

			IStorageTimestamp storageTimestamp = new StorageTimestampNano();
			var currentTime = DateTimeHelper.CurrentTicks;
			var currentTimeFormatted = new byte[storageTimestamp.GetFormatLength()];
			storageTimestamp.Format(currentTime, currentTimeFormatted);
			var expectedTimeStr = StringHelper.NewString(currentTimeFormatted);

			var messages = new string[msgNum];
			for (var i = 0; i < msgNum; i++)
			{
				var arrMsg = GetNextMessage().AsByteArray();
				MessageStorage.AppendMessage(currentTimeFormatted, arrMsg);
				messages[i] = StringHelper.NewString(arrMsg);
			}

			ClassicAssertEqualsMessagesWithTimestamps(messages, expectedTimeStr);
		}

		[Test]
		public virtual void TestWriteLongMessage()
		{
			var longMsgBuilder = new StringBuilder("34=1\u0001");
			var size = 1024 * 1024 + 10;
			while (longMsgBuilder.Length < size)
			{
				longMsgBuilder.Append("58=Hello World!!!\u0001");
			}

			UpMessageStorage();
			var longMsg = longMsgBuilder.ToString();
			MessageStorage.AppendMessage(longMsg.AsByteArray());
			var nextMsg = GetNextMessage().ToString();
			MessageStorage.AppendMessage(nextMsg.AsByteArray());

			ClassicAssertEqualsMessages(longMsg, nextMsg);
		}

		[Test]
		public virtual void TestMessageInDefaultLocaleWithMilli()
		{
			ConfigurationAdapter.Configuration.SetProperty(Config.LogFilesTimeZone, TimeZoneInfo.Local.Id);

			UpMessageStorage();

			var timestamp = GetStorageTimestampPrefixFromStorage(_instant);

			var dateTime = new DateTimeOffset(_instant, TimeSpan.Zero).LocalDateTime;
			ClassicAssert.AreEqual(dateTime.ToString(PrefixFormatMilli), timestamp);
		}

		[Test]
		public virtual void TestMessageInDefaultLocaleWithMicro()
		{
			ConfigurationAdapter.Configuration.SetProperty(Config.LogFilesTimeZone, TimeZoneInfo.Local.Id);
			ConfigurationAdapter.Configuration.SetProperty(Config.TimestampsPrecisionInLogs,
				TimestampPrecision.Micro.ToString());

			UpMessageStorage();

			var timestamp = GetStorageTimestampPrefixFromStorage(_instant);

			var dateTime = new DateTimeOffset(_instant, TimeSpan.Zero).LocalDateTime;
			ClassicAssert.AreEqual(dateTime.ToString(PrefixFormatMicro), timestamp);
		}

		[Test]
		public virtual void TestMessageInDefaultLocaleWithNano()
		{
			ConfigurationAdapter.Configuration.SetProperty(Config.LogFilesTimeZone, TimeZoneInfo.Local.Id);
			ConfigurationAdapter.Configuration.SetProperty(Config.TimestampsPrecisionInLogs,
				TimestampPrecision.Nano.ToString());

			UpMessageStorage();

			var timestamp = GetStorageTimestampPrefixFromStorage(_instant);

			var dateTime = new DateTimeOffset(_instant, TimeSpan.Zero).LocalDateTime;
			ClassicAssert.AreEqual(dateTime.ToString(PrefixFormatNano), timestamp);
		}

		[Test]
		public virtual void TestMessageInCustomLocaleWithMilli()
		{
			ConfigurationAdapter.Configuration.SetProperty(Config.LogFilesTimeZone, "UTC");
			ConfigurationAdapter.Configuration.SetProperty(Config.TimestampsPrecisionInLogs,
				TimestampPrecision.Milli.ToString());

			UpMessageStorage();

			var timestamp = GetStorageTimestampPrefixFromStorage(_instant);

			var dateTime = new DateTimeOffset(_instant, DateTimeHelper.UtcOffset);
			ClassicAssert.AreEqual(dateTime.ToString(PrefixFormatMilli), timestamp);
		}

		[Test]
		public virtual void TestMessageInCustomLocaleWithMicro()
		{
			ConfigurationAdapter.Configuration.SetProperty(Config.LogFilesTimeZone, "GMT+05");
			ConfigurationAdapter.Configuration.SetProperty(Config.TimestampsPrecisionInLogs,
				TimestampPrecision.Micro.ToString());

			UpMessageStorage();

			var timestamp = GetStorageTimestampPrefixFromStorage(_instant);

			var dateTime = new DateTimeOffset(_instant, TimeSpan.Zero).ToOffset(TimeSpan.FromHours(5));
			ClassicAssert.AreEqual(dateTime.ToString(PrefixFormatMicro), timestamp);
		}

		[Test]
		public virtual void TestMessageInCustomLocaleWithNano()
		{
			ConfigurationAdapter.Configuration.SetProperty(Config.LogFilesTimeZone, "GMT-05:30");
			ConfigurationAdapter.Configuration.SetProperty(Config.TimestampsPrecisionInLogs,
				TimestampPrecision.Nano.ToString());

			UpMessageStorage();

			var timestamp = GetStorageTimestampPrefixFromStorage(_instant);

			var dateTime = new DateTimeOffset(_instant, TimeSpan.Zero).ToOffset(new TimeSpan(-5, -30, 0));
			ClassicAssert.AreEqual(dateTime.ToString(PrefixFormatNano), timestamp);
		}

		private string GetStorageTimestampPrefixFromStorage(long time)
		{
			var fixMessage = GetNextMessage();
			MessageStorage.AppendMessageInternal(time, fixMessage.AsByteArray(), 0,
				fixMessage.AsByteArray().Length);
			var message = RetrieveMessage();
			ClassicAssert.IsTrue(message.Length > 24);
			return StringHelper.NewString(message, 0, MessageStorage.CalculateFormatLength());
		}

		private byte[] RetrieveMessage()
		{
			var storageFileName = FileNameHelper.GetStorageFileName(ConfigurationAdapter.StorageDirectory);
			using (var stream = new FileStream(storageFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				var bytes = new byte[2048];
				var len = stream.Read(bytes, 0, 2048);
				var result = new byte[len];
				Array.Copy(bytes, 0, result, 0, len);
				return result;
			}
		}
	}
}
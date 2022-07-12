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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Authentication;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Storage;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using static Epam.FixAntenna.NetCore.Configuration.Config;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.Util
{
	internal sealed class ConfigurationAdapter
	{
		private const string DefaultOutTemplate = "{0}.out";
		private const string DefaultBackupOutTemplate = "{0}-{3}.out";
		private const string DefaultInTemplate = "{0}.inc";
		private const string DefaultBackupInTemplate = "{0}-{3}.inc";
		private const string DefaultPropertyTemplate = "{0}.properties";
		private const string DefaultStateTemplate = "{0}.state";
		private const string DefaultQueueTemplate = "{0}.outq";
		private const string DefaultDir = ".";
		private const string StorageBackupDefaultDir = "backup";
		private static readonly ILog Log = LogFactory.GetLog(typeof(ConfigurationAdapter));
		private static readonly TimeSpan DefaultTimezone = DateTimeHelper.UtcOffset;

		public Config Configuration { get; }

		public ConfigurationAdapter(Config configuration)
		{
			Configuration = configuration;
		}

		public bool IsEnableMessageRejecting => Configuration.GetPropertyAsBoolean(Config.EnableMessageRejecting, false);

		public int MaxMessagesToSendInBatch => Configuration.GetPropertyAsInt(Config.MaxMessagesToSendInBatch, 1, int.MaxValue, true);

		public int ThresholdSize => Configuration.GetPropertyAsInt(Config.QueueThresholdSize, 0, int.MaxValue, true);

		public int ForceLogoff => Configuration.GetPropertyAsInt(Config.ForcedLogoffTimeout, 0, int.MaxValue, true);

		public string StorageFactoryClass => Configuration.GetProperty(Config.StorageFactory, typeof(FilesystemStorageFactory).FullName);

		public int AutoReconnectAttempts => Configuration.GetPropertyAsInt(Config.AutoreconnectAttempts);

		public int GetAutoReconnectAttempts(int defaultValue)
		{
			return Configuration.GetPropertyAsInt(Config.AutoreconnectAttempts, defaultValue);
		}

		public int AutoReconnectDelay => Configuration.GetPropertyAsInt(Config.AutoreconnectDelayInMs, 0, int.MaxValue, true);

		public bool IsResetOnSwitchToBackupEnabled => Configuration.GetPropertyAsBoolean(Config.ResetOnSwitchToBackup, false);

		public bool IsResetOnSwitchToPrimaryEnabled => Configuration.GetPropertyAsBoolean(Config.ResetOnSwitchToPrimary, false);

		public bool IsAutoSwitchToBackupConnectionEnabled => Configuration.GetPropertyAsBoolean(Config.EnableAutoSwitchToBackupConnection, true);

		public bool IsCyclicSwitchBackupConnectionEnabled => Configuration.GetPropertyAsBoolean(Config.CyclicSwitchBackupConnection, true);

		public bool IsSslEnabled => Configuration.GetPropertyAsBoolean(Config.RequireSsl, false);

		public bool IsNagleEnabled => Configuration.GetPropertyAsBoolean(Config.EnableNagle);

		public bool IsMessageStatisticEnabled => Configuration.GetPropertyAsBoolean(Config.EnableMessageStatistic, true);

		public long MaxMessageSize => Configuration.GetPropertyAsBytesLength(Config.MaxMessageSize);

		public string ConnectAddress => Configuration.GetProperty(Config.ConnectAddress);

		public bool IsSendingTimeAccuracyCheckEnabled => Configuration.GetPropertyAsBoolean(Config.CheckSendingTimeAccuracy, false);

		public int GetReasonableDelay(int defaultValue)
		{
			var reasonableDelay = Configuration.GetPropertyAsInt(Config.Delay, defaultValue);
			if (reasonableDelay < 0)
			{
				reasonableDelay = defaultValue;
				if (Log.IsWarnEnabled)
				{
					Log.Warn("Parameter \"" + Config.Delay + "\" must be integer and not negative");
				}
			}

			return reasonableDelay;
		}

		public int GetAccuracy(int defaultValue)
		{
			var accuracyInMs = Configuration.GetPropertyAsInt(Config.Accuracy, defaultValue);
			if (accuracyInMs < 0)
			{
				accuracyInMs = defaultValue;
				if (Log.IsWarnEnabled)
				{
					Log.Warn("Parameter \"" + Config.Accuracy + "\" must be integer and not negative");
				}
			}

			return accuracyInMs;
		}

		public TimestampPrecision TimestampsPrecisionInTags => GetTimestampsPrecision(Config.TimestampsPrecisionInTags);

		public TimestampPrecision TimestampsPrecisionInLogs => GetTimestampsPrecision(Config.TimestampsPrecisionInLogs);

		public TimestampPrecision BackupTimestampsPrecision => GetTimestampsPrecision(Config.BackupTimestampsPrecision);

		private TimestampPrecision GetTimestampsPrecision(string precisionPropertyName)
		{
			var value = Configuration.GetProperty(precisionPropertyName, "MILLI")
				.ToUpper(CultureInfo.InvariantCulture);

			if (!(Enum.TryParse(value, true, out TimestampPrecision timestampsPrecision) 
			      && Enum.IsDefined(typeof(TimestampPrecision), timestampsPrecision)))
			{
				Log.Warn("Incorrect value in '" + precisionPropertyName + "', value:'" + value + "'." +
						"Only 'Second', 'Milli', 'Micro' and 'Nano' values are valid. Using default: Milli");
				timestampsPrecision = TimestampPrecision.Milli;
			}

			return timestampsPrecision;
		}

		public int MaxDifference => Configuration.GetPropertyAsInt(Config.ResendRequestNumberOfMessagesLimit, 0, int.MaxValue, true);

		public bool IsWelformedValidationEnabled => Configuration.GetPropertyAsBoolean(Config.WelformedValidator, false);

		public bool IsFieldsValidationEnabled => Configuration.GetPropertyAsBoolean(Config.AllowedFieldsValidation, false);

		public bool IsRequiredFieldsValidationEnabled => Configuration.GetPropertyAsBoolean(Config.RequiredFieldsValidation, false);

		public bool IsFieldOrderValidationEnabled => Configuration.GetPropertyAsBoolean(Config.FieldOrderValidation, false);

		public bool IsDuplicateFieldsValidationEnabled => Configuration.GetPropertyAsBoolean(Config.DuplicateFieldsValidation, false);

		public bool IsFieldTypeValidationEnabled => Configuration.GetPropertyAsBoolean(Config.FieldTypeValidation, false);

		public bool IsConditionalValidationEnabled => Configuration.GetPropertyAsBoolean(Config.ConditionalValidation, false);

		public bool IsGroupValidationEnabled => Configuration.GetPropertyAsBoolean(Config.GroupValidation, false);

		public bool IsValidationEnabled => Configuration.GetPropertyAsBoolean(Config.Validation, false);

		#region Reset SeqNum sheduled
		public bool IsResetSeqNumTimeEnabled => Configuration.GetPropertyAsBoolean(Config.PerformResetSeqNumTime, false);

		public string ResetSequenceTime => Configuration.GetProperty(Config.ResetSequenceTime, "00:00:00");

		public string ResetSequenceTimeZone => Configuration.GetProperty(Config.ResetSequenceTimeZone, "UTC");

		private TimeSpan ParseResetSequenceTimeZone()
		{
			var userTimeZoneId = ResetSequenceTimeZone;

			if (DateTimeHelper.TryParseTimeZoneOffset(userTimeZoneId, out var offset))
			{
				return offset;
			}

			Log.Warn("Incorrect value in" + Config.ResetSequenceTimeZone + "', value:'" + userTimeZoneId +
					"'. Using default: UTC");
			offset = DefaultTimezone;

			return offset;
		}

		public long ResetSequenceTimeInUserTimestamp
		{
			get
			{
				var timeZone = ParseResetSequenceTimeZone();
				var resetSeqNumTime = ResetSequenceTime;
				DateTimeBuilder builder;
				try
				{
					var time = FixTypes.ParseShortTime(resetSeqNumTime.AsByteArray());
					builder = new DateTimeBuilder(DateTime.UtcNow.Date).SetHour(time.Hour).SetMinute(time.Minute)
						.SetSecond(time.Second);
				}
				catch (Exception)
				{
					Log.Warn("Incorrect value in '" + Config.ResetSequenceTime + "', value:'" + resetSeqNumTime +
									"'. Using default: 00:00:00");
					builder = new DateTimeBuilder(DateTime.UtcNow.Date).SetHour(0).SetMinute(0).SetSecond(0);
				}

				builder = builder.SetMillisecond(0);
				return builder.Build(timeZone).TotalMilliseconds();
			}
		}
		#endregion

		public StorageCleanupMode StorageCleanupMode
		{
			get
			{
				var propertyValue =
					Configuration.GetProperty(Config.StorageCleanupMode, StorageCleanupMode.Backup.ToString());

				if (!(Enum.TryParse(propertyValue, true, out StorageCleanupMode cleanupMode) 
				      && Enum.IsDefined(typeof(StorageCleanupMode), cleanupMode)))
				{
					Log.Warn("Incorrect value in '" + Config.StorageCleanupMode + "', value:'" + propertyValue +
									"'. Using default: Backup");
					cleanupMode = StorageCleanupMode.Backup;
				}

				return cleanupMode;
			}
		}

		public string StorageDirectory => Configuration.GetProperty(Config.StorageDirectory, DefaultDir);

		public string BackupStorageDirectory => Configuration.GetProperty(Config.StorageBackupDir, StorageBackupDefaultDir);

		public string OutgoingStorageTemplate => Configuration.GetProperty(Config.OutgoingLogFile, DefaultOutTemplate);

		public string BackupOutgoingStorageTemplate => Configuration.GetProperty(Config.BackupOutgoingLogFile, DefaultBackupOutTemplate);

		public string IncomingStorageTemplate => Configuration.GetProperty(Config.IncomingLogFile, DefaultInTemplate);

		public string BackupIncomingStorageTemplate => Configuration.GetProperty(Config.BackupIncomingLogFile, DefaultBackupInTemplate);

		public string PropertiesTemplate => Configuration.GetProperty(Config.SessionInfoFile, DefaultPropertyTemplate);

		//FIXME_NOW
		public string StateTemplate => Configuration.GetProperty("sessionStateFile", DefaultStateTemplate);

		public string OutgoingQueueTemplate => Configuration.GetProperty(Config.OutgoingQueueFile, DefaultQueueTemplate);

		public bool IsIncomingStorageIndexed => Configuration.GetPropertyAsBoolean(Config.IncomingStorageIndexed, false);

		public bool IsOutgoingStorageIndexed => Configuration.GetPropertyAsBoolean(Config.OutgoingStorageIndexed, true);

		public bool IsInMemoryQueueEnabled => Configuration.GetPropertyAsBoolean(Config.InMemoryQueue);

		public bool IsMemoryMappedQueueEnabled => Configuration.GetPropertyAsBoolean(Config.MemoryMappedQueue, true);

		public bool IsIntraDeySeqNumResetEnabled => Configuration.GetPropertyAsBoolean(Config.IntraDaySeqnumReset, false);

		public int HbtReasonableTransmissionTime => Configuration.GetPropertyAsInt(Config.HbtReasonableTransmissionTime, 0, int.MaxValue, true);

		public bool IsOrigSendingTimeCheckingEnabled => Configuration.GetPropertyAsBoolean(Config.OrigSendingTimeChecking, true);

		public bool IsIgnorePossDupForGapFill => Configuration.GetPropertyAsBoolean(Config.IgnorePossDupForGapFill, true);

		public bool IsQuietLogonModeEnabled => Configuration.GetPropertyAsBoolean(Config.QuietLogonMode, false);

		public int TestRequestsNumberUponDisconnection => Configuration.GetPropertyAsInt(Config.TestRequestsNumberUponDisconnection, 1);

		public bool AdvancedResendRequestProcessing => Configuration.GetPropertyAsBoolean(Config.AdvancedResendRequestProcessing, false);

		public bool SkipDuplicatedResendRequests => Configuration.GetPropertyAsBoolean(Config.SkipDuplicatedResendRequest, false);

		public bool PossDupSmartDelivery => Configuration.GetPropertyAsBoolean(Config.PossDupSmartDelivery, false);

		public long LogonWaitTimeout => Configuration.GetPropertyAsInt(Config.LoginWaitTimeout, 0, int.MaxValue, true);

		public bool ValidateCheckSum => Configuration.GetPropertyAsBoolean(Config.ValidateCheckSum, true);

		public int LogoutWaitTimeout => Configuration.GetPropertyAsInt(Config.LogoutWaitTimeout);

		public string ServerAcceptorStrategy => Configuration.GetProperty(Config.ServerAcceptorStrategy,
				"Epam.FixAntenna.NetCore.FixEngine.Acceptor.AllowNonRegisteredAcceptorStrategyHandler");

		public bool SwitchOffSendingMultipleResendRequests => Configuration.GetPropertyAsBoolean(Config.SwitchOffSendingMultipleResendRequests, false);

		public SendingMode PreferredSendingMode
		{
			get
			{
				var sendingMode = Configuration.GetProperty(Config.PreferredSendingMode, SendingMode.Sync.ToString());
				if (!(Enum.TryParse(sendingMode.ToUpper(CultureInfo.InvariantCulture), true, out SendingMode mode) 
				      && Enum.IsDefined(typeof(SendingMode), mode)))
				{
					mode = SendingMode.Sync;
					if (Log.IsWarnEnabled)
					{
						Log.Warn("Invalid value for  property \"" + Config.PreferredSendingMode + "\". Will be used sync mode by default");
					}
				}

				return mode;
			}
		}

		public int GetWaitForQueuingMessages(int defaultVal) => Configuration.GetPropertyAsInt(Config.WaitForMsgQueuingDelay, defaultVal);

		public bool IgnoreSeqNumTooLowAtLogon => Configuration.GetPropertyAsBoolean(Config.IgnoreSeqNumTooLowAtLogon);

		public int AllowedCountOfSimilarRr => Configuration.GetPropertyAsInt(Config.AllowedCountOfSimilarRr);

		public bool HandleSeqNumAtLogon => Configuration.GetPropertyAsBoolean(Config.HandleSeqnumAtLogon);

		public int TcpSendBufferSize => Configuration.GetPropertyAsInt(Config.TcpSendBufferSize, 0);

		public int TcpReceiveBufferSize => Configuration.GetPropertyAsInt(Config.TcpReceiveBufferSize, 0);

		public bool ValidateGarbledMessage => Configuration.GetPropertyAsBoolean(Config.ValidateGarbledMessage, true);

		public bool MarkInMessageTime => Configuration.GetPropertyAsBoolean(Config.MarkIncomingMessageTime, false);

		public string SslCertificate => Configuration.GetProperty(Config.SslCertificate);

		public string SslCertificatePassword => Configuration.GetProperty(Config.SslCertificatePassword);

		public string RawTags => Configuration.GetProperty(Config.RawTags);

		public SslProtocols SslProtocol
		{
			get
			{
				var ssl = Configuration.GetProperty(Config.SslProtocol);
				if (Enum.TryParse<SslProtocols>(ssl, out var value) && Enum.IsDefined(typeof(SslProtocols), value))
				{
					return value;
				}

				throw new ArgumentException("Property sslProtocol have wrong value:" + ssl);
			}
		}

		public bool SslCheckCertificateRevocation => Configuration.GetPropertyAsBoolean(Config.SslCheckCertificateRevocation);

		public string SslServerName => Configuration.GetProperty(Config.SslServerName);

		public string SslCaCertificate => Configuration.GetProperty(Config.SslCaCertificate);

		public bool SslValidatePeerCertificate => Configuration.GetPropertyAsBoolean(Config.SslValidatePeerCertificate);

		public List<int> SslPorts => ParsePorts(Configuration.GetProperty(Config.SslPort, new ValidatorIntegerList(1,65535), nullable: true, warnInLog: true)).ToList();

		public List<int> Ports => ParsePorts(Configuration.GetProperty(Config.Port, new ValidatorIntegerList(1, 65535), nullable: true, warnInLog: true)).ToList();

		public bool IsSslPort(int port) => SslPorts.Contains(port);

		private static IEnumerable<int> ParsePorts(string rawPorts) =>
			(rawPorts ?? string.Empty)
				.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(p => int.TryParse(p.Trim(), out var port) ? port : -1)
				.Where(port => port > -1);
	}
}
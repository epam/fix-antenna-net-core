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

using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Epam.FixAntenna.NetCore.Configuration
{
	/// <summary>
	/// Engine configuration class.
	/// Engine used <c>Configuration.GetGlobalConfiguration</c> method to configure itself,
	/// user can change this properties in runtime only for initiator sessions, not for acceptors.
	/// </summary>
	public class Config : ICloneable
	{
		public const string InfinityAutoreconnect = "0";
		public const string NoAutoreconnect = "-1";
		public const string UndefinedAffinity = "-1";
		public const string LogonCustomizationStrategyUndefined = "none";
		public const string CmeSecureLogonStrategy = "Epam.FixAntenna.NetCore.FixEngine.Session.Impl.CmeSecureLogonStrategy";

		/// <summary>
		/// Reset sequences on switch to backup.
		/// </summary>
		[DefaultValue("false")]
		public const string ResetOnSwitchToBackup = "resetOnSwitchToBackup";

		/// <summary>
		/// Reset sequences on switch back to primary connection
		/// </summary>
		[DefaultValue("false")]
		public const string ResetOnSwitchToPrimary = "resetOnSwitchToPrimary";

		/// <summary>
		/// This parameter switches on mode which prevent sending multiple RR for the same gap
		/// </summary>
		[DefaultValue("false")]
		public const string SwitchOffSendingMultipleResendRequests = "switchOffSendingMultipleResendRequests";

		/// <summary>
		/// Specifies number of autoreconnect attempts before give up:
		/// negative number = no reconnects (NoAutoreconnect), <br/>
		/// 0 - infinite number of reconnects (InfinityAutoreconnect), <br/>
		/// positive number = number of reconnect attempts <br/>
		/// Please use 0 wisely - it means reconnect infinitely
		/// </summary>
		[DefaultValue(NoAutoreconnect)]
		public const string AutoreconnectAttempts = "autoreconnectAttempts";

		/// <summary>
		/// Allows user to replace storage factory with user own implementation
		/// </summary>
		[DefaultValue("Epam.FixAntenna.NetCore.FixEngine.Storage.FilesystemStorageFactory")]
		public const string StorageFactory = "storageFactory";

		/// <summary>
		/// Allows user to replace session sequence manager with user own implementation
		/// </summary>
		[DefaultValue("Epam.FixAntenna.NetCore.FixEngine.Session.StandardSessionSequenceManager")]
		public const string SessionSequenceManager = "sessionSequenceManager";

		/// <summary>
		/// Sends a reject if user application is not available.
		/// If the value is false and client application is not available, acts like a "black hole" - accepts and ignores all valid messages.
		/// </summary>
		[DefaultValue("true")]
		public const string SendRejectIfApplicationIsNotAvailable = "sendRejectIfApplicationIsNotAvailable";

		/// <summary>
		/// Enable auto switching to backup connection, the default value is true.
		/// </summary>
		[DefaultValue("true")]
		public const string EnableAutoSwitchToBackupConnection = "enableAutoSwitchToBackupConnection";

		/// <summary>
		/// Enable switch to primary connection, default value true
		/// </summary>
		[DefaultValue("true")]
		public const string CyclicSwitchBackupConnection = "cyclicSwitchBackupConnection";

		/// <summary>
		/// Specifies delay between autoreconnect attempts in milliseconds, default value is 1000ms.
		/// </summary>
		[DefaultValue("1000")]
		public const string AutoreconnectDelayInMs = "autoreconnectDelayInMs";

		/// <summary>
		/// Suppress session qualifier tag in logon message.
		/// </summary>
		[DefaultValue("false")]
		public const string SuppressSessionQualifierTagInLogonMessage =
			"suppressSessionQualifierTagInLogonMessage";

		/// <summary>
		/// Specifies tag number for session qualifier in logon message.
		/// </summary>
		[DefaultValue("9012")]
		public const string LogonMessageSessionQualifierTag = "logonMessageSessionQualifierTag";

		/// <summary>
		/// Name of the custom package for admin commands processing
		/// This property is using for the extending the count of admin-commands.
		/// By default package is null, but if custom commands is present this property should be initialized,
		/// for example (autostart.acceptor.commands.package=com.admin.commands).
		/// </summary>
		public const string AutostartAcceptorCommandPackage = "autostart.acceptor.commands.package";

		/// <summary>
		/// Disable/enable Nagle's algorithm for TCP sockets.
		/// This option has the opposite meaning to TCP_NO_DELAY socket option.
		/// With enabled Nagle's algorithm will be better throughput (TcpNoDelay=false)
		/// but with disabled option you will get better result for latency on single message (TcpNoDelay=true)
		/// </summary>
		[DefaultValue("true")]
		public const string EnableNagle = "enableNagle";

		/// <summary>
		/// Limits the maximum number of messages buffered during the resend request.
		/// The parameter must be integer and positive.
		/// Otherwise the default value for this parameter will be used.
		/// </summary>
		[DefaultValue("32")]
		public const string SequenceResendManagerMessageBufferSize = "sequenceResendManagerMessageBufferSize";

		/// <summary>
		/// Limits the maximum number of messages during the resend request.
		/// If more messages are requested, the reject will be sent in response.
		/// The parameter must be integer and not negative.
		/// Otherwise the default value for this parameter will be used.
		/// </summary>
		[DefaultValue("0")]
		public const string ResendRequestNumberOfMessagesLimit = "resendRequestNumberOfMessagesLimit";

		/// <summary>
		/// Toggle on/off validation of fields with tag 8, 9, 35 and 10 values.
		/// If "validation=false" then this parameter always reads as false.
		/// </summary>
		[DefaultValue("true")]
		public const string WelformedValidator = "wellformenessValidation";

		/// <summary>
		/// Toggle on/off validation of allowed message fields.
		/// If "validation=false" then this parameter always reads as false.
		/// </summary>
		[DefaultValue("true")]
		public const string AllowedFieldsValidation = "allowedFieldsValidation";

		/// <summary>
		/// Toggle on/off validation of required message fields.
		/// If "validation=false" then this parameter always reads as false.
		/// </summary>
		[DefaultValue("true")]
		public const string RequiredFieldsValidation = "requiredFieldsValidation";

		/// <summary>
		/// Toggle on/off validation of field values according to defined data types.
		/// </summary>
		[DefaultValue("true")]
		public const string FieldTypeValidation = "fieldTypeValidation";

		/// <summary>
		/// enable/disable conditional validator,
		/// default value is false.
		/// </summary>
		[DefaultValue("true")]
		public const string ConditionalValidation = "conditionalValidation";

		/// <summary>
		/// Enable/disable group validator,
		/// default value is false.
		/// </summary>
		[DefaultValue("true")]
		public const string GroupValidation = "groupValidation";

		/// <summary>
		/// Toggle on/off validation of duplicated message fields.
		/// If "validation=false" then this parameter always reads as false.
		/// </summary>
		[DefaultValue("true")]
		public const string DuplicateFieldsValidation = "duplicateFieldsValidation";

		/// <summary>
		/// Toggle on/off validation of fields order in message.
		/// With this option engine will check that tags from the header, body and trailer were not mixed up.
		/// If "validation=false" then this parameter always reads as false.
		/// </summary>
		[DefaultValue("true")]
		public const string FieldOrderValidation = "fieldOrderValidation";

		/// <summary>
		/// Sending time delay for incoming messages in milliseconds.
		/// </summary>
		[DefaultValue("120000")]
		public const string Delay = "reasonableDelayInMs";

		/// <summary>
		/// The desired precision of timestamps in appropriate tags of the FIX message.
		/// Valid values: Second | Milli | Micro | Nano.
		/// </summary>
		[DefaultValue("Milli")]
		public const string TimestampsPrecisionInTags = "timestampsPrecisionInTags";

		/// <summary>
		/// Use timestamp with precision defined by timestampsPrecisionInTags option for FIX 4.0 if enabled.
		/// </summary>
		[DefaultValue("false")]
		public const string AllowedSecondsFractionsForFix40 = "allowedSecondsFractionsForFIX40";

		/// <summary>
		/// Measurement accuracy in milliseconds, default value is 1 ms.
		/// </summary>
		[DefaultValue("1")]
		public const string Accuracy = "measurementAccuracyInMs";

		/// <summary>
		/// Toggle on/off the check of SendingTime (52) accuracy for received messages.
		/// </summary>
		[DefaultValue("true")]
		public const string CheckSendingTimeAccuracy = "checkSendingTimeAccuracy";

		/// <summary>
		/// Enable/disable message rejecting, default value is false
		/// </summary>
		[DefaultValue("false")]
		public const string EnableMessageRejecting = "enableMessageRejecting";

		/// <summary>
		/// Enable/disable message statistic, default value is true
		/// </summary>
		[DefaultValue("true")]
		public const string EnableMessageStatistic = "enableMessageStatistic";

		/// <summary>
		/// Maximum number of messages in a queue before we pause a pumper thread to let the queued message be sent out.
		/// <ul>
		/// <li>Set rather high for max performance.</li>
		/// <li>Set 1 or pretty low for realtime experience.</li>
		/// <li>0 - disable queue control, do not pause the pumper thread.</li>
		/// </ul>
		/// </summary>
		[DefaultValue("0")]
		public const string QueueThresholdSize = "queueThresholdSize";

		/// <summary>
		/// The maximum number of messages in buffer before we
		/// write message to transport.
		/// NOTE: Value for this property should be always > 0.
		/// default value is 10
		/// </summary>
		[DefaultValue("10")]
		public const string MaxMessagesToSendInBatch = "maxMessagesToSendInBatch";

		/// <summary>
		/// Maximum message size supported by this FIX engine instance.
		/// The parameter must be integer and not negative. Otherwise, the default value for this parameter will be used.
		/// Should be set to a greater than expected maximum message by approximately 1-5%.
		/// <ul>
		/// <li>positive number - maximum allowed size of incoming message</li>
		/// <li>0 - any size message allowed (not recommended, could lead to OutOfMemoryError if counterparty will send invalid stream).</li>
		/// </ul>
		/// </summary>
		[DefaultValue("1Mb")]
		public const string MaxMessageSize = "maxMessageSize";

		/// <summary>
		/// Validate or not message CheckSum(10)
		/// Is relevant only if validateGarbledMessage=true
		/// default value is true
		/// </summary>
		[DefaultValue("true")]
		public const string ValidateCheckSum = "validateCheckSum";

		/// <summary>
		/// Toggle on/off validation garbled message for incoming flow.
		/// Validates the existence and order of the following fields: BeginString(8), BodyLength(9), MsgType(35), CheckSum(10).
		/// Also validates value of BodyLength(9).
		/// default value is true
		/// </summary>
		[DefaultValue("true")]
		public const string ValidateGarbledMessage = "validateGarbledMessage";

		/// <summary>
		/// Transport will set the additional time mark in nanoseconds for incoming messages right after read data from
		/// socket if this option is set to true.
		/// AbstractFIXTransport.getLastReadMessageTimeNano() method could return this value.
		/// <p/>
		/// default value is false
		/// </summary>
		[DefaultValue("false")]
		public const string MarkIncomingMessageTime = "markIncomingMessageTime";

		/// <summary>
		/// Include last processed sequence 369 tag in every message for FIX versions>4.2
		/// </summary>
		[DefaultValue("false")]
		public const string IncludeLastProcessed = "includeLastProcessed";

		/// <summary>
		/// Sets the disconnect timeout in seconds for a Logout ack only when waiting for.
		/// The Logout ack from the counterparty is caused by the incoming sequence number less then expected.
		/// The parameter must be integer and not negative.
		/// Otherwise the default value for this parameter will be used.
		/// </summary>
		[DefaultValue("2")]
		public const string ForcedLogoffTimeout = "forcedLogoffTimeout";

		/// <summary>
		/// Sets the timeout interval after which a connected acceptor session will be timed out
		/// and disposed if Logon is not received for this session.
		/// default value is 5000
		/// </summary>
		[DefaultValue("5000")]
		public const string LoginWaitTimeout = "loginWaitTimeout";

		/// <summary>
		/// Sets disconnect timeout in seconds for logoff,
		/// default value is equal to session's HeartbeatInterval
		/// </summary>
		public const string LogoutWaitTimeout = "logoutWaitTimeout";

		/// <summary>
		/// This parameter specifies whether to process 789-NextExpectedMsgSeqNum tag.
		/// If true, outgoing sequence number must be updated by 789-NextExpectedMsgSeqNum tag value.
		/// </summary>
		[DefaultValue("false")]
		public const string HandleSeqnumAtLogon = "handleSeqNumAtLogon";

		/// <summary>
		/// Check and disconnect session if Logon answer contains other HeartBtInt(108) value than defined in session
		/// configuration.
		///
		/// Default value: true
		/// </summary>
		[DefaultValue("true")]
		public const string DisconnectOnLogonHbtMismatch = "disconnectOnLogonHeartbeatMismatch";

		/// <summary>
		/// Sets queue mode. Set to "false" for persistent queue (slower but no messages will be lost),
		/// "true" for in memory queue (faster but less safe, some messages may be lost).
		/// This property makes sense only if FilesystemStorageFactory is set.
		/// </summary>
		[DefaultValue("false")]
		public const string InMemoryQueue = "inMemoryQueue";

		/// <summary>
		/// Sets persistent queue mode for MMFStorageFactory.
		/// Set to "false" for persistent queue (slower but no messages will be lost),
		/// "true" for memory mapped queue (faster but less safe, some messages may be lost)
		/// </summary>
		[DefaultValue("true")]
		public const string MemoryMappedQueue = "memoryMappedQueue";

		/// <summary>
		/// True will enables incoming storage index.
		/// Enabled index - messages in incoming storage will be available via API
		/// </summary>
		public const string IncomingStorageIndexed = "incomingStorageIndexed";

		/// <summary>
		/// Outgoing storage index.
		/// This property makes sense only if FilesystemStorageFactory or MMFStorageFactory is set.
		/// Set to "true" to enable outgoing storage index that is to be used in decision making in resend request handler.
		/// Enabled index - support resend request, disabled - never resend messages and always send gap fill.
		/// </summary>
		[DefaultValue("true")]
		public const string OutgoingStorageIndexed = "outgoingStorageIndexed";

		/// <summary>
		/// Sets path to fa home.
		/// </summary>
		[DefaultValue(".")]
		public const string FaHome = "fa.home";

		/// <summary>
		/// Storage directory could be either absolute path (like /tmp/logs or c:\fixengine\logs)
		/// or relative path e.g. logs (this one is relative to the application start directory).
		/// </summary>
		[DefaultValue("${fa.home}/logs")]
		public const string StorageDirectory = "storageDirectory";

		/// <summary>
		/// Raw tags. List all tags here engine should treat as raw. Raw tag may contain SOH symbol inside it
		/// and it should be preceided by rawTagLength field.
		/// </summary>
		[DefaultValue("96, 91, 213, 349, 351, 353, 355, 357, 359, 361, 363, 365, 446, 619, 622")]
		public const string RawTags = "rawTags";

		/// <summary>
		/// Toggle on/off validation of incoming messages according to base of custom dictionaries
		/// Following parameters with the Validation suffix works only if this property set to true.
		/// </summary>
		[DefaultValue("false")]
		public const string Validation = "validation";

		/// <summary>
		/// Outgoing log filename template. {0} will be replaced with actual sessionID, {1} with actual SenderCompID,
		/// {2} with actual TargetCompID and {4} with actual session qualifier.
		/// </summary>
		[DefaultValue("{0}.out")]
		public const string OutgoingLogFile = "outgoingLogFile";

		/// <summary>
		/// Backup outgoing log filename template. {0} will be replaced with actual sessionID, {1} with SenderCompID,
		/// {2} with actual TargetCompID, {3} with timestamp and {4} with actual session qualifier.
		/// </summary>
		[DefaultValue("{0}-{3}.out")]
		public const string BackupOutgoingLogFile = "backupOutgoingLogFile";

		/// <summary>
		/// Incoming log filename template. {0} will be replaced with actual sessionID, {1} with actual SenderCompID,
		/// {2} with actual TargetCompID and {4} with actual session qualifier.
		/// </summary>
		[DefaultValue("{0}.in")]
		public const string IncomingLogFile = "incomingLogFile";

		/// <summary>
		/// Backup incoming log filename template. {0} will be replaced with actual sessionID, {1} with SenderCompID,
		/// {2} with actual TargetCompID, {3} with timestamp and {4} with actual session qualifier.
		/// </summary>
		[DefaultValue("{0}-{3}.in")]
		public const string BackupIncomingLogFile = "backupIncomingLogFile";

		/// <summary>
		/// Info filename template. {0} will be replaced with actual sessionID, {1} with actual SenderCompID,
		/// {2} with actual TargetCompID and {4} with actual session qualifier.
		/// </summary>
		[DefaultValue("{0}.properties")]
		public const string SessionInfoFile = "sessionInfoFile";

		/// <summary>
		/// Out queue file template. {0} will be replaced with actual sessionID, {1} with actual SenderCompID,
		/// {2} with actual TargetCompID and {4} with actual session qualifier.
		/// </summary>
		[DefaultValue("{0}.outq")]
		public const string OutgoingQueueFile = "outgoingQueueFile";

		/// <summary>
		/// This parameter allow to automatically resolve sequence gap problem (for example, when there is every day sequence reset).
		/// Supported values: Always, OneTime, Never.
		/// If this parameter is set to:
		/// <ul>
		/// <li>Always - session will send logon with 34= 1 and 141=Y every times (during connection and reconnection)</li>
		/// <li>OneTime - session will send logon with 34= 1 and 141=Y only one time (during connection)</li>
		/// <li>Never - this means that user can sets the 34= 1 and 141=Y from session parameters by hand</li>
		/// </ul>
		/// </summary>
		[DefaultValue("Never")]
		public const string ForceSeqNumReset = "forceSeqNumReset";

		/// <summary>
		/// Time zone for prefix in out/in logs
		/// </summary>
		public const string LogFilesTimeZone = "logFilesTimeZone";

		/// <summary>
		/// Ability to write timestamps in the in/out log files.
		/// Default value true.
		/// </summary>
		[DefaultValue("true")]
		public const string TimestampsInLogs = "timestampsInLogs";

		/// <summary>
		/// The desired pecision of timestamps in the in/out log files.
		/// Valid values: Milli | Micro | Nano.
		/// </summary>
		[DefaultValue("Milli")]
		public const string TimestampsPrecisionInLogs = "timestampsPrecisionInLogs";

		/// <summary>
		/// The desired pecision of timestamps in names of storage backup files.
		/// Valid values: Milli | Micro | Nano.
		/// </summary>
		[DefaultValue("Milli")]
		public const string BackupTimestampsPrecision = "backupTimestampsPrecision";

		/// <summary>
		/// Specifies the maximum size of the storage file after which the engine creates a new storage file with a different name.
		/// Parameter must be integer and not negative.
		/// This property makes sense only if SlicedFileStorageFactory is set.
		/// </summary>
		[DefaultValue("100Mb")]
		public const string MaxStorageSliceSize = "maxStorageSliceSize";

		/// <summary>
		/// Sets the maximum storage grow size in bytes.
		/// Parameter must be integer and not negative.
		/// </summary>
		[DefaultValue("1Mb")]
		public const string MaxStorageGrowSize = "maxStorageGrowSize";

		/// <summary>
		/// Sets the storage grow size in bytes for memory mapped implementation.
		/// Parameter must be integer and not negative.
		/// This property makes sense only if MMFStorageFactory is set.
		/// </summary>
		[DefaultValue("100Mb")]
		public const string MmfStorageGrowSize = "mmfStorageGrowSize";

		/// <summary>
		/// Sets the index grow size in bytes for memory mapped implementation.
		/// Used only for storage with memory mapped index file.
		/// Parameter must be integer and not negative.
		/// This property makes sense only if MMFStorageFactory is set and at least one of incomingStorageIndexed or outgoingStorageIndexed is true.
		/// </summary>
		[DefaultValue("20Mb")]
		public const string MmfIndexGrowSize = "mmfIndexGrowSize";

		/// <summary>
		/// Enable/disable storage grow.
		/// Default value: false.
		/// </summary>
		[DefaultValue("false")]
		public const string StorageGrowSize = "storageGrowSize";

		/// <summary>
		/// Engine's local IP address to send from.
		/// It can be used on a multi-homed host for a FIX Engine that will only send IP datagrams from one of its addresses.
		/// If this parameter is commented, the engine will send IP datagrams from any/all local addresses.
		/// </summary>
		public const string ConnectAddress = "connectAddress";

		/// <summary>
		/// The max requested messages in block. This parameter defines how many messages will be request in one block.
		/// The value must be integer and not less than 0.
		/// </summary>
		[DefaultValue("0")]
		public const string MaxRequestResendInBlock = "maxRequestResendInBlock";

		/// <summary>
		/// The pause before sending application messages from outgoing queue in milliseconds after receiving Logon.
		/// This pause is need to handle possible incoming ResendRequest. In other case a bunch of messages with
		/// invalid sequence can be sent.
		/// The value must be integer and not less than 0.
		/// </summary>
		[DefaultValue("50")]
		public const string MaxDelayToSendAfterLogon = "maxDelayToSendAfterLogon";

		/// <summary>
		/// This parameter specifies whether to reset sequence number at time defined in resetSequenceTime.
		/// </summary>
		[DefaultValue("false")]
		public const string PerformResetSeqNumTime = "performResetSeqNumTime";

		/// <summary>
		/// This parameter specifies GMT time when the FIX Engine initiates the reset of sequence numbers.
		/// Valid time format: HH:MM:SS
		/// </summary>
		[DefaultValue("00:00:00")]
		public const string ResetSequenceTime = "resetSequenceTime";

		/// <summary>
		/// Time zone id for resetSequenceTime property.
		/// </summary>
		[DefaultValue("UTC")]
		public const string ResetSequenceTimeZone = "resetSequenceTimeZone";

		/// <summary>
		/// This parameter specifies cleaning mode for message storage of closed sessions.
		/// Valid values: None | Backup | Delete.
		/// </summary>
		[DefaultValue("None")]
		public const string StorageCleanupMode = "storageCleanupMode";

		/// <summary>
		/// This parameter specifies back-up directory for message logs
		/// of closed sessions when storageCleanupMode=backup.
		/// Valid values: existent directory name (relative or absolute path)
		/// Default value not defined
		/// See FA_HOME description in the Configuration section of the
		/// FIX Antenna .NET Core User and Developer Manual.
		/// </summary>
		[DefaultValue("${fa.home}/logs/backup")]
		public const string StorageBackupDir = "storageBackupDir";

		/// <summary>
		/// This parameter specifies whether to reset sequence number after session is closed.
		/// Valid values: true | false
		/// </summary>
		[DefaultValue("false")]
		public const string IntraDaySeqnumReset = "intraDaySeqNumReset";

		/// <summary>
		/// This parameter specifies whether to send ResetSeqNumFlag (141=Y) after sequence rest or not.
		/// Valid values: true | false
		/// </summary>
		[DefaultValue("false")]
		public const string IgnoreResetSeqNumFlagOnReset = "ignoreResetSeqNumFlagOnReset";

		public const string DesKeyPrefix = "desKey";

		public const string PubKeyFile = "pubKeyFile";

		public const string SecKeyFile = "secKeyFile";

		public const string PubKey = "pgpKey";

		/// <summary>
		/// This parameter specifies encryption config file name.
		/// Valid values: existent valid config file name (relative or absolute path)
		/// Default value not defined
		/// </summary>
		[DefaultValue("${fa.home}/encryption/encryption.cfg")]
		public const string EncryptionCfgFile = "encryptionConfig";

		/// <summary>
		/// This parameter specifies the default value of encryptionMode.
		/// Valid values: None | Des | PgpDesMd5
		/// </summary>
		[DefaultValue("None")]
		public const string EncryptionMode = "encryptionMode";

		public const string EnableLoggingOfIncomingMessages = "enableLoggingOfIncomingMessages";

		/// <summary>
		/// This parameter specifies "some reasonable transmission time" of FIX specification, measured in milliseconds.
		/// Valid values: positive integer
		/// Default value: 200
		/// </summary>
		[DefaultValue("200")]
		public const string HbtReasonableTransmissionTime = "heartbeatReasonableTransmissionTime";

		/// <summary>
		/// This parameter specifies whether to check the OrigSendingTime(122) field value for incoming possible
		/// duplicated messages (PossDupFlag(43) = 'Y').
		/// Valid values: true | false
		/// Default value: true
		/// </summary>
		[DefaultValue("true")]
		public const string OrigSendingTimeChecking = "origSendingTimeChecking";

		/// <summary>
		/// Enable this option if it need to handle SequenceReset-GapFill message without PossDupFlag(43).
		/// Also this option allow to ignore absence of OrigSendingTime(122) in such message.
		/// Valid values: true | false
		/// Default value: true
		/// </summary>
		[DefaultValue("true")]
		public const string IgnorePossDupForGapFill = "ignorePossDupForGapFill";

		/// <summary>
		/// This parameter specifies number of Test Request messages, which will be sent before connection loss
		/// is reported when no messages are received from the counterparty.
		/// Default value: 1
		/// </summary>
		[DefaultValue("1")]
		public const string TestRequestsNumberUponDisconnection = "testRequestsNumberUponDisconnection";

		/// <summary>
		/// This parameter specifies whether to issue subsequently duplicates
		/// (PossDupFlag(43) = 'Y') of last Resend Request for continuing gaps resting on
		/// LastMsgSeqNumProcessed(369) field values of incoming messages.
		/// The counterparty then must respond only to the original request or
		/// a subsequent duplicate Resend Request if it missed the original.
		/// The duplicate(s), otherwise, can be discarded, as it does not have a unique
		/// message sequence number of its own.
		/// </summary>
		[DefaultValue("false")]
		public const string AdvancedResendRequestProcessing = "advancedResendRequestProcessing";

		/// <summary>
		/// This parameter specifies whether respond only to the original request or
		/// a subsequent duplicate Resend Request if it missed the original.
		/// If this option is disabled, Fix Antenna will respond to any Resend Request.
		/// </summary>
		[DefaultValue("false")]
		public const string SkipDuplicatedResendRequest = "skipDuplicatedResendRequests";

		/// <summary>
		/// This parameter enables delivery of only those PossDup messages that wasn't received previously.
		/// Discarding already processed possDups.
		/// </summary>
		[DefaultValue("false")]
		public const string PossDupSmartDelivery = "possDupSmartDelivery";

		/// <summary>
		/// This parameter specifies the default Acceptor Strategy.
		/// Valid values: subclasses of FixAntenna.NetCore.FixEngine.Acceptor.SessionAcceptorStrategyHandler
		/// <br/>
		/// Possible values:
		/// <br/>
		/// <c>FixAntenna.NetCore.FixEngine.Acceptor.SessionAcceptorStrategyHandler</c>
		/// <br/>
		/// <c>FixAntenna.NetCore.FixEngine.Acceptor.DenyNonRegisteredAcceptorStrategyHandler</c>
		/// <br/>
		/// <c>FixAntenna.NetCore.FixEngine.Acceptor.DenyNonRegisteredAcceptorStrategyHandler</c>
		/// </summary>
		[DefaultValue("Epam.FixAntenna.NetCore.FixEngine.Acceptor.AllowNonRegisteredAcceptorStrategyHandler")]
		public const string ServerAcceptorStrategy = "serverAcceptorStrategy";

		/// <summary>
		/// This parameter specifies the way the session will send most of its messages:<br/>
		/// Async - session will send all message asynchronously and it will be optimized for this<br/>
		/// Sync - session will be optimized to send messages from user thread, but it still can make asynchronous
		/// operation and it allows to add messages to internal queue<br/>
		/// SyncNoqueue - session sends message only from user thread and doesn't use internal queue. It's impossible to
		/// send messages to disconnected session. <br/>
		/// <p/>
		/// Valid values: Async/Sync/SyncNoqueue
		/// Default value: sync
		/// </summary>
		[DefaultValue("sync")]
		public const string PreferredSendingMode = "preferredSendingMode";

		/// <summary>
		/// This parameter specifies the maximum delay interval on message sending if the internal session queue is full.
		/// If the internal session's queue is full then FIX Antenna pause the sending thread till the message
		/// pumper thread send some messages and free some space in the queue. If after the delay interval queue still full,
		/// then message will be pushed to the queue anyway.
		/// <p/>
		/// Valid values: positive integer
		/// Default value: 1000
		/// </summary>
		[DefaultValue("1000")]
		public const string WaitForMsgQueuingDelay = "waitForMsgQueuingDelay";

		/// <summary>
		/// This parameter allow to resolve wrong incoming sequence at Logon.
		/// When it true - session continue with received seqNum.
		/// <p/>
		/// Valid values: true/false
		/// Default value: false
		/// </summary>
		[DefaultValue("false")]
		public const string IgnoreSeqNumTooLowAtLogon = "ignoreSeqNumTooLowAtLogon";

		/// <summary>
		/// When disabled prevents outgoing queue reset if client connecting with lower than expected sequence number.
		/// Once session is reestablished queued messages will be sent out.
		/// Please note that queued messages won't be sent out till session if fully established regardless of that parameter.
		/// <para>
		/// Default value: true
		/// </para>
		/// </summary>
		[DefaultValue("true")]
		public const string ResetQueueOnLowSequenceNum = "resetQueueOnLowSequence";

		/// <summary>
		/// Enable this option if it need to quiet handle Logout as a first session message.
		/// FIX Specification requires that first message should be Logon. In other case it needs to send with answer Logout
		/// message warning "First message is not logon". Also sоmetimes first incoming Logout has a wrong sequence
		/// (for example if you send Logon with 141=Y). This option allow to skip sending ResendRequest and warning
		/// to counterparty.
		/// Valid values: true | false
		/// Default value: false
		/// </summary>
		[DefaultValue("false")]
		public const string QuietLogonMode = "quietLogonMode";

		/// <summary>
		/// Sets the timeout interval in seconds for waiting reading thread finishing during session shutdown. Reading thread
		/// may be interrupted after this interval if it was blocked.
		///
		/// The parameter must be integer and not negative. Otherwise, the standard value for this parameter will be used.
		/// Default value: session heartbeat interval
		/// </summary>
		[DefaultValue("-1")]
		public const string ReadingThreadShutdownTimeout = "readingThreadShutdownTimeout";

		/// <summary>
		/// Sets the timeout interval in seconds for waiting writing thread finishing during session shutdown. Writing thread
		/// may be interrupted after this interval if it was blocked.
		///
		/// The parameter must be integer and not negative. Otherwise, the standard value for this parameter will be used.
		/// Default value: session heartbeat interval
		/// </summary>
		[DefaultValue("-1")]
		public const string WritingThreadShutdownTimeout = "writingThreadShutdownTimeout";

		/// <summary>
		/// This option indicate how many similar ResendRequests (for same range of sequences) engine may sends before
		/// detecting possible infinite resend loop. This should prevent infinite loop for requesting many times same
		/// corrupted messages or if user logic cann't correctly handle some message and every time throws exception.
		///
		/// The parameter must be integer and not negative. Otherwise, the standard value for this parameter will be used.
		/// Default value: 3
		/// </summary>
		[DefaultValue("3")]
		public const string AllowedCountOfSimilarRr = "allowedCountOfSimilarRR";

		/// <summary>
		/// This parameter specifies cpu id for a thread of session that receives the data from socket.
		/// </summary>
		[DefaultValue(UndefinedAffinity)]
		public const string RecvCpuAffinity = "recvCpuAffinity";

		/// <summary>
		/// This parameter specifies cpu id for a thread of session that sends the data in socket.
		/// </summary>
		[DefaultValue(UndefinedAffinity)]
		public const string SendCpuAffinity = "sendCpuAffinity";

		/// <summary>
		/// This parameter specifies cpu id for the threads of session that send and receive the data from/in socket.
		/// </summary>
		[DefaultValue(UndefinedAffinity)]
		public const string CpuAffinity = "cpuAffinity";

		/// <summary>
		/// This parameter specifies <seealso cref="System.Net.Sockets.Socket.SendBufferSize"/> property.
		///
		/// Default value is 0, it means the parameter is not specified.
		/// </summary>
		[DefaultValue("0")]
		public const string TcpSendBufferSize = "tcpSendBufferSize";

		/// <summary>
		/// This parameter specifies <seealso cref="System.Net.Sockets.Socket.ReceiveBufferSize"/> property.
		///
		/// Default value is 0, it means the parameter is not specified.
		/// </summary>
		[DefaultValue("0")]
		public const string TcpReceiveBufferSize = "tcpReceiveBufferSize";

		/// <summary>
		/// If option is disabled it should be possible to receive messages with none session specific Comp IDs
		/// if option is enabled than all the messages with compIds not aligned to session parameters will be rejected
		///
		/// Default value is true, it means that consistency check is enabled
		/// </summary>
		[DefaultValue("true")]
		public const string SenderTargetIdConsistencyCheck = "senderTargetIdConsistencyCheck";

		/// <summary>
		/// This parameter specifies a gap in sequences during connecting, which may be treated as missed sequence reset
		/// event by the counterpart. It works if current session has reset sequences and expects message (Logon) with 34=1
		/// but counterparty is still sending messages with much higher sequences (they didn't do reset on their side).
		/// This option helps to control bidirectional agreed sequence reset events and prevents to request old messages.
		/// This option is working only for acceptor session.
		/// Default value is 0, it means the check is not going to be performed.
		///
		/// </summary>
		[DefaultValue("0")]
		public const string ResetThreshold = "resetThreshold";

		/// <summary>
		/// This parameter enables slow consumer detection in pumpers
		/// </summary>
		[DefaultValue("false")]
		public const string SlowConsumerDetectionEnabled = "slowConsumerDetectionEnabled";

		/// <summary>
		/// This parameter used for decision making in slow consumer detection in pumpers.
		/// It defined a maximum timeframe for sending a message. If session transport can't send a message during this
		/// timeframe it will notify about a slow consumer.
		/// </summary>
		[DefaultValue("10")]
		public const string SlowConsumerWriteDelayThreshold = "slowConsumerWriteDelayThreshold";

		/// <summary>
		/// Enables throttling checks per message type
		/// If this option is enabled, engine counts how many times session receives messages with some message type during throttleCheckingPeriod.
		/// If this counter will be greater than the value in throttleChecking.MsgType.threshold, the session will be
		/// closed with reason THROTTLING
		///
		/// Default value: false
		/// </summary>
		[DefaultValue("false")]
		public const string ThrottleCheckingEnabled = "throttleCheckingEnabled";

		/// <summary>
		/// Defines period common for all throttling per message type checks.
		///
		/// Default value: 1000 milliseconds
		/// </summary>
		[DefaultValue("1000")]
		public const string ThrottleCheckingPeriod = "throttleCheckingPeriod";

		/// <summary>
		/// Defines name of keys file. It should be located in classpath, root directory of running application or home directory.
		/// </summary>
		public const string CmeSecureKeysFile = "cmeSecureKeysFile";

		/// <summary>
		/// Defines strategy that will be applied for logon messages right before sending.
		/// <p/>
		/// Valid values: subclasses of <c>FixAntenna.NetCore.FixEngine.Session.Impl.LogonCustomizationStrategy</c>/>
		/// <p/>
		/// Implemented strategies:
		///       <seealso cref="CmeSecureLogonStrategy"/> - It tells engine to use CME secure logon scheme.
		///                          This strategy requires defined <seealso cref="CmeSecureKeysFile"/>.
		/// </summary>
		[DefaultValue(LogonCustomizationStrategyUndefined)]
		public const string LogonCustomizationStrategy = "logonCustomizationStrategy";

		/// <summary>
		/// Print socket address to debug log for incoming and outgoing log.
		/// If this option is enabled, Antenna will print messages to debug log in format: <br/>
		/// [127.0.0.1]>>8=FIX.4.2 | 9=250...
		/// </summary>
		[DefaultValue("false")]
		public const string WriteSocketAddressToLog = "writeSocketAddressToLog";

		/// <summary>
		/// Determines if sequence numbers should be accepted from the incoming Logon message. The option allows to reduce
		/// miscommunication between sides and easier connect after scheduled sequence reset.
		/// <p/>
		/// The option doesn’t change behavior if the Logon message contains ResetSeqNumFlag(141) equals to “Y” (in this
		/// case session sequence numbers will be reset).<p/>
		/// The value ‘Schedule’ allows to adopt to the sequence numbers from the incoming Logon message if the reset time
		/// is outdated (the session recovers after scheduled reset time). In this case session’s incoming sequence number
		/// will be set to the value of MsgSeqNum(34) tag from the incoming Logon and outgoing sequence number become
		/// equivalent to NextExpectedMsgSeqNum (789) tag value (if the tag is present) or will be reset to 1.
		/// <p/>
		/// If the parameter 'ResetSeqNumFromFirstLogon' is set to 'Schedule' on the acceptor's side, then:
		/// If the tag ResetSeqNumFlag (141) in the received Logon message is set to 'Y', then:
		/// The incoming sequence number must be set to 1
		/// The outgoing sequence number must be set to 1
		/// If the tag ResetSeqNumFlag (141) in the received Logon message is set to 'N' or missing, then
		/// If the sequence numbers reset date and time is outdated, then:
		/// The incoming sequence number must be set to the value of the tag MsgSeqNum (34)
		/// If the tag NextExpectedMsgSeqNum (789) is specified in the received Logon message, then
		/// The outgoing sequence number must be set to the value of the tag NextExpectedMsgSeqNum (789)
		/// If the tag NextExpectedMsgSeqNum (789) is missing in the received Logon message, then
		/// The outgoing sequence number must be set to the 1
		/// If the sequence numbers reset date and time is actual, then:
		/// The incoming and outgoing sequence numbers are handled according to the FIX protocol
		/// Valid values: Never | Schedule.
		/// </summary>
		[DefaultValue("Never")]
		public const string ResetSeqNumFromFirstLogon = "resetSeqNumFromFirstLogon";

		/// <summary>
		/// Masked tags. List all tags here engine should hide value with asterisks in logs.
		/// </summary>
		[DefaultValue("554, 925")]
		public const string MaskedTags = "maskedTags";

		/// <summary>
		/// Turn on CME Enhanced Resend Request logic for filling gaps
		/// </summary>
		[DefaultValue("false")]
		public const string EnhancedCmeResendLogic = "enhancedCmeResendLogic";

		#region SSL/TLS related
		/// <summary>
		/// Requires establishing of secured transport for individual session,
		/// or for all sessions, when used on top level configuration.
		/// </summary>
		[DefaultValue("false")]
		public const string RequireSsl = "requireSSL";

		/// <summary>
		/// Selected SslProtocol as defined in <see cref="System.Security.Authentication.SslProtocols"/>
		/// Default value is "None" as recommended by Microsoft - in this case best suitable protocol be used.
		/// </summary>
		[DefaultValue("None")]
		public const string SslProtocol = "sslProtocol";

		/// <summary>
		/// Name of Certificate.
		/// Could be file name, or distinguished name (CN=...) of certificate in case when certificate store is used.
		/// </summary>
		[DefaultValue("")]
		public const string SslCertificate = "sslCertificate";

		/// <summary>
		/// Password for SSL certificate.
		/// </summary>
		[DefaultValue("")]
		public const string SslCertificatePassword = "sslCertificatePassword";

		/// <summary>
		/// If true, remote certificate must be validated for successful connection.
		/// If false, also disables sslCheckCertificateRevocation.
		/// </summary>
		[DefaultValue("true")]
		public const string SslValidatePeerCertificate = "sslValidatePeerCertificate";

		/// <summary>
		/// If true and also sslValidatePeerCertificate=true, remote certificate will be checked for revocation.
		/// </summary>
		[DefaultValue("true")]
		public const string SslCheckCertificateRevocation = "sslCheckCertificateRevocation";

		/// <summary>
		/// Name of CA certificate.
		/// Could be file name, or distinguished name (CN=...) of certificate in case when certificate store is used.
		/// </summary>
		[DefaultValue("")]
		public const string SslCaCertificate = "sslCaCertificate";

		/// <summary>
		/// Used on initiator only. Should match with CN=[serverName] in the acceptor certificate.
		/// </summary>
		[DefaultValue("")]
		public const string SslServerName = "sslServerName";

		/// <summary>
		/// Acceptor: listening port(s) for SSL/TLS connections.
		/// Initiator: ignored.
		/// </summary>
		[DefaultValue("")]
		public const string SslPort = "sslPort";
		#endregion

		/// <summary>
		/// Cron expression to set a scheduled session start.
		/// </summary>
		public const string TradePeriodBegin = "tradePeriodBegin";

		/// <summary>
		/// Cron expression to set the end of the scheduled session.
		/// </summary>
		public const string TradePeriodEnd = "tradePeriodEnd";

		/// <summary>
		/// Time zone id for tradePeriodBegin and TradePeriodEnd properties.
		/// </summary>
		[DefaultValue("UTC")]
		public const string TradePeriodTimeZone = "tradePeriodTimeZone";

		/// <summary>
		/// Acceptor: Default listening port(s) for unsecured connections.
		/// Initiator: Target port for connection.
		/// </summary>
		[DefaultValue("")]
		public const string Port = "port";

		/// <summary>
		/// Minimal length of the SeqNum fields. Possible value is integer in range 1..10.
		/// If the actual SeqNum length less than defined, leading zeros will be added to the SeqNum fields.
		/// </summary>
		[DefaultValue("1")]
		public const string SeqNumLength = "seqNumLength";

		public const string DefaultEngineProperties = "fixengine.properties";

		public const string CustomFixVersions = "customFixVersions";

		public const string CustomFixVersionPrefix = "customFixVersion.";
		public const string CustomFixVersionVersionSuffix = ".fixVersion";
		public const string CustomFixVersionFileNameSuffix = ".fileName";

		private static readonly ILog Log = LogFactory.GetLog(typeof(Config));

		/// <summary>
		/// Identifies a maximum HBI in seconds.
		/// </summary>
		public static int MaxTimeoutValue = int.MaxValue / 1000;

		private static readonly TemplatePropertiesWrapper DefaultProperties = new TemplatePropertiesWrapper();

		private IDictionary<string, CustomFixVersionConfig> _customFixVersionConfigs = new ConcurrentDictionary<string, CustomFixVersionConfig>();
		private TemplatePropertiesWrapper _properties;

		static Config()
		{
			try
			{
				LoadDefault();
			}
			catch (Exception e)
			{
				Log.Fatal("Can't load default properties. " + e.Message, e);
			}
		}

		/// <summary>
		/// Create a Configuration based on Map properties.
		/// </summary>
		/// <param name="map"> properties </param>
		public Config(IDictionary<string, string> map)
		{
			_properties = new TemplatePropertiesWrapper(map);
			PrepareCustomFixVersionConfigs();
		}

		/// <summary>
		/// Create a Configuration.
		/// Load properties from prop file
		/// </summary>
		/// <param name="propFileName"> the properties file. </param>
		/// <exception cref="ArgumentException"> if file not exists. </exception>
		public Config(string propFileName)
		{
			try
			{
				_properties = new TemplatePropertiesWrapper(Common.Properties.FromFile(propFileName));
				PrepareCustomFixVersionConfigs();
			}
			catch (IOException e)
			{
				var message = "Unable to load: " + propFileName;
				if (Log.IsDebugEnabled)
				{
					Log.Warn(message, e);
				}
				else
				{
					Log.Warn(message + ". Reason: " + e.Message);
				}

				throw new ArgumentException(message, e);
			}
		}

		private Config()
		{
			Properties props = new Properties();
			try
			{
				props = Common.Properties.FromFile(DefaultEngineProperties);
			}
			catch (Exception)
			{
				Log.Info("Unable to load user properties: " + DefaultEngineProperties +
						". Default properties will be used instead");
			}

			_properties = new TemplatePropertiesWrapper(props);
			PrepareCustomFixVersionConfigs();
		}

		private void PrepareCustomFixVersionConfigs()
		{
			var customFixVersions = _properties.GetProperty(CustomFixVersions);
			if (string.IsNullOrEmpty(customFixVersions))
			{
				return;
			}

			string fixVersion;
			string fileName;

			foreach (var i in customFixVersions.Split(','))
			{
				var customFixVersion = i.Trim();
				try
				{
					fixVersion = _properties.GetProperty(string.Concat(CustomFixVersionPrefix, customFixVersion, CustomFixVersionVersionSuffix));
					fileName = _properties.GetProperty(string.Concat(CustomFixVersionPrefix, customFixVersion, CustomFixVersionFileNameSuffix));

					if (!string.IsNullOrEmpty(fixVersion) && !string.IsNullOrEmpty(fileName))
					{
						_customFixVersionConfigs[customFixVersion] = new CustomFixVersionConfig(fixVersion, fileName);
					}
				}
				catch (Exception ex)
				{
					Log.Error("Error occurred while reading custom FIX version: " + customFixVersion, ex);
				}
			}
		}

		public CustomFixVersionConfig GetCustomFixVersionConfig(string customFixVersion)
		{
			return _customFixVersionConfigs.GetValueOrDefault(customFixVersion);
		}

		public object Clone()
		{
			var configuration = MemberwiseClone() as Config;
			configuration._properties = (TemplatePropertiesWrapper)_properties.Clone();
			configuration._customFixVersionConfigs = new Dictionary<string, CustomFixVersionConfig>(_customFixVersionConfigs);
			return configuration;
		}

		/// <summary>
		/// Get global configuration. Method returned default configuration loaded on startup.
		/// </summary>
		/// <value> instance of global configuration. </value>
		public static Config GlobalConfiguration => LazyGlobalConfiguration.Value;

		private static readonly Lazy<Config> LazyGlobalConfiguration = new Lazy<Config>(() => new Config());
		private static volatile string _configurationDirectory;

		/// <summary>
		/// Allows setting a directory that will be used to load configuration e.g. to load fixengine.properties.
		/// The parameter does not affect the loading of dictionaries.<br/>
		/// The order of searching for fixengine.properties will be <br/>
		/// 1. Directory defined by this parameter <br/>
		/// 2. Current directory <br/>
		/// 3. Home directory <br/>
		/// 4. Embedded resources inside libraries from method callstack.
		/// Set it before reading any configuration.
		/// </summary>
		public static string ConfigurationDirectory
		{
			get => _configurationDirectory;
			set => _configurationDirectory = value;
		}

		/// <summary>
		/// Setter for properties.
		/// </summary>
		/// <param name="propertyName">  the name of property </param>
		/// <param name="propertyValue"> the value for property </param>
		public virtual void SetProperty(string propertyName, string propertyValue)
		{
			if (Log.IsTraceEnabled)
			{
				Log.Trace("Set property " + propertyName + "=" + propertyValue);
			}

			_properties.Put(propertyName, propertyValue);
			PrepareCustomFixVersionConfigs();
		}

		/// <summary>
		/// Setter for properties.
		/// </summary>
		/// <param name="propertyName">  the name of property </param>
		/// <param name="propertyValue"> the value for property </param>
		public virtual void SetProperty(string propertyName, int propertyValue)
		{
			if (Log.IsTraceEnabled)
			{
				Log.Trace("Set property " + propertyName + "=" + propertyValue);
			}

			_properties.Put(propertyName, propertyValue.ToString());
		}

		public virtual void AddAllProperties(IDictionary<string, string> newProps)
		{
			foreach (var key in newProps.Keys)
			{
				SetProperty(key, newProps[key]);
			}
			PrepareCustomFixVersionConfigs();
		}

		public virtual void SetAllProperties(IDictionary<string, string> newProps)
		{
			_properties.Clear();
			_customFixVersionConfigs.Clear();
			AddAllProperties(newProps);
		}

		/// <summary>
		/// Getter for property.
		/// </summary>
		/// <param name="propertyName"> the name of property </param>
		/// <returns> value for property or null if property not exists </returns>
		public virtual string GetProperty(string propertyName)
		{
			var value = _properties.GetProperty(propertyName);

			if (!string.IsNullOrWhiteSpace(value))
			{
				return value;
			}

			ParamSources.Instance.Set(propertyName, ParamSource.Default);
			return GetDefaultProperty(propertyName, true);
		}

		/// <summary>
		/// Getter for property.
		/// </summary>
		/// <param name="propertyName"> the name of property </param>
		/// <param name="validator">    validate value from user config. For invalid value will be return default value. </param>
		/// <param name="nullable">     true - for case when property not exist can return null(nullable=true) or throws exception(nullable=false) </param>
		/// <returns> value for property. If property not exists can return null or throws exception(depends on <c>nullable</c> value) </returns>
		internal virtual string GetProperty(string propertyName, IValidator validator, bool nullable)
		{
			return GetProperty(propertyName, validator, nullable, false);
		}

		/// <summary>
		/// Getter for property.
		/// </summary>
		/// <param name="propertyName"> the name of property </param>
		/// <param name="validator">    validate value from user config. For invalid value will be return default value. </param>
		/// <param name="nullable">     true - for case when property not exist can return null(nullable=true) or throws exception(nullable=false) </param>
		/// <param name="warnInLog">    write warning to log if value from config is not fit to validator </param>
		/// <param name="customMessage">customized warning message</param>
		/// <returns> value for property. If property not exists can return null or throws exception(depends on <c>nullable</c> value) </returns>
		internal virtual string GetProperty(string propertyName, IValidator validator, bool nullable, bool warnInLog, string customMessage = null)
		{
			var value = _properties.GetProperty(propertyName);
			//        boolean exceptionalCase = false;
			if (!string.IsNullOrWhiteSpace(value))
			{
				if (validator.Valid(value))
				{
					return value;
				}

				// value from user config is not satisfy validator
				// try use value from default config
				if (warnInLog)
				{
					if (customMessage != null)
						Log.Warn(customMessage);
					else
						Log.Warn("'" + propertyName + "=" + value + "' value does not satisfy the conditions: " +
							validator.ToString() + ". Using default value: '" + GetDefaultProperty(propertyName, true) + "'");
				}
			}

			ParamSources.Instance.Set(propertyName, ParamSource.Default);
			value = GetDefaultProperty(propertyName, nullable);
			if (!string.IsNullOrWhiteSpace(value) && validator.Valid(value))
			{
				return value;
			}

			if (nullable)
			{
				value = null;
			}
			else
			{
				throw new ArgumentException("'" + propertyName + "=" + value +
											"' value does not satisfy the demands of the validator");
			}

			return value;
		}

		private string GetDefaultProperty(string propertyName, bool nullable)
		{
			var value = DefaultProperties.GetProperty(propertyName);
			if (!nullable && string.IsNullOrEmpty(value))
			{
				throw new ArgumentException("Property '" + propertyName + "' was not found");
			}

			ParamSources.Instance.Set(propertyName, ParamSource.Default);
			return value;
		}

		/// <summary>
		/// Getter for property.
		/// </summary>
		/// <param name="propertyName"> the name of property </param>
		/// <param name="defaultValue"> the default value for property </param>
		/// <returns> value for property or defaultValue if property not exists </returns>
		public virtual string GetProperty(string propertyName, string defaultValue)
		{
			var value = GetProperty(propertyName);
			return value ?? defaultValue;
		}

		/// <summary>
		/// Get property value as boolean.
		/// </summary>
		/// <param name="propertyName"> the name of property </param>
		/// <param name="defaultValue"> the default value for property </param>
		/// <param name="warnToLog">    write warning to log if value from user config is not in range </param>
		/// <param name="customMessage">customized message for warning</param>
		/// <returns> value for property or defaultValue if property not exists </returns>
		public virtual bool GetPropertyAsBoolean(string propertyName, bool defaultValue, bool warnToLog = false, string customMessage = null)
		{
			var property = GetProperty(propertyName, new ValidatorBoolean(), true, warnToLog, customMessage);
			if (property == null)
			{
				ParamSources.Instance.Set(propertyName, ParamSource.Default);
				return defaultValue;
			}

			return ParseBool(property);
		}

		/// <summary>
		/// Get property value as boolean.
		/// </summary>
		/// <param name="propertyName"> the name of property </param>
		/// <returns> value for property or false if property not exists </returns>
		public virtual bool GetPropertyAsBoolean(string propertyName)
		{
			return GetPropertyAsBoolean(propertyName, false);
		}

		/// <summary>
		/// Get property value as int.
		/// </summary>
		/// <param name="propertyName"> the name of property </param>
		/// <param name="defaultValue"> the default value for property </param>
		/// <returns> value for property or defaultValue if property not exists </returns>
		public virtual int GetPropertyAsInt(string propertyName, int defaultValue)
		{
			try
			{
				return ParseInt(GetProperty(propertyName), defaultValue);
			}
			catch (Exception)
			{
				return defaultValue;
			}
		}

		/// <summary>
		/// Get property value as int. If value is not in range than return default value or throws exception(if default value is not in range too)
		/// </summary>
		/// <param name="propertyName"> the name of property </param>
		/// <param name="min">          the minimal value. Value >= min </param>
		/// <param name="max">          the maximal value. max >= Value </param>
		/// <returns> value for property or defaultValue if property not exists or in rage </returns>
		public virtual int GetPropertyAsInt(string propertyName, int min, int max)
		{
			return ParseInt(GetProperty(propertyName, new ValidatorInteger(min, max), true));
		}

		/// <summary>
		/// Get property value as int. If value is not in range than return default value or throws exception(if default value is not in range too)
		/// </summary>
		/// <param name="propertyName"> the name of property </param>
		/// <param name="min">          the minimal value. Value >= min </param>
		/// <param name="max">          the maximal value. max >= Value </param>
		/// <param name="warnToLog">    write warning to log if value from user config is not in range </param>
		/// <param name="customMessage">customized message for warning</param>
		/// <returns> value for property or defaultValue if property not exists or in rage </returns>
		public virtual int GetPropertyAsInt(string propertyName, int min, int max, bool warnToLog, string customMessage = null)
		{
			return ParseInt(GetProperty(propertyName, new ValidatorInteger(min, max), false, warnToLog, customMessage));
		}

		/// <summary>
		/// Get property value as int.
		/// </summary>
		/// <param name="propertyName"> the name of property </param>
		/// <returns> value for property or -1 if property not exists </returns>
		public virtual int GetPropertyAsInt(string propertyName)
		{
			return GetPropertyAsInt(propertyName, -1);
		}

		/// <summary>
		/// Get property value as long.
		/// </summary>
		/// <param name="propertyName"> the name of property </param>
		/// <param name="defaultValue"> the default value for property </param>
		/// <returns> value for property or defaultValue if property not exists </returns>
		public virtual long GetPropertyAsLong(string propertyName, long defaultValue)
		{
			if (long.TryParse(GetProperty(propertyName), out var result))
				return result;

			return defaultValue;
		}

		/// <summary>
		/// Get property value as long.
		/// </summary>
		/// <param name="propertyName"> the name of property </param>
		/// <returns> value for property or -1 if property not exists </returns>
		public virtual long GetPropertyAsLong(string propertyName)
		{
			return GetPropertyAsLong(propertyName, -1L);
		}

		/// <summary>
		/// Get property value as bytes length. Example:
		/// <br/>
		/// 1=1
		/// <br/>
		/// 1b=1
		/// <br/>
		/// 1Kb=1024
		/// <br/>
		/// 1Mb=1048576
		/// <br/>
		/// 1Gb=1073741824
		/// </summary>
		/// <param name="propertyName"> the name of property </param>
		/// <returns> value for property or -1 if property not exists </returns>
		public virtual long GetPropertyAsBytesLength(string propertyName)
		{
			return GetPropertyAsBytesLength(propertyName, -1);
		}

		/// <summary>
		/// Get property value as bytes length. Example:
		/// <br/>
		/// 1=1
		/// <br/>
		/// 1b=1
		/// <br/>
		/// 1Kb=1024
		/// <br/>
		/// 1Mb=1048576
		/// <br/>
		/// 1Gb=1073741824
		/// </summary>
		/// <param name="propertyName"> the name of property </param>
		/// <param name="defaultValue"> the default value for property </param>
		/// <returns> value for property or defaultValue if property not exists </returns>
		public virtual long GetPropertyAsBytesLength(string propertyName, int defaultValue)
		{
			try
			{
				return ParseBytesLength(GetProperty(propertyName), defaultValue);
			}
			catch (Exception)
			{
				return defaultValue;
			}
		}

		internal static long ParseBytesLength(string str, int defaultValue)
		{
			if (string.IsNullOrWhiteSpace(str))
			{
				return defaultValue;
			}

			var cleanStr = str.Trim();
			var limit = cleanStr.Length;
			var tailNotNumber = 0;
			while (tailNotNumber < limit)
			{
				var ch = cleanStr[limit - tailNotNumber - 1];
				if (ch < '0' || ch > '9')
				{
					tailNotNumber++;
				}
				else
				{
					break;
				}
			}

			var numValue = ParseInt(cleanStr.Substring(0, cleanStr.Length - tailNotNumber), defaultValue);
			if (tailNotNumber == 0 || numValue == defaultValue)
			{
				// some problem with parse or value not have suffix
				return numValue;
			}

			var sufix = cleanStr.Substring(cleanStr.Length - tailNotNumber).Trim();
			const long kiloConst = 1024;
			if (sufix.Equals("gb", StringComparison.OrdinalIgnoreCase))
			{
				return numValue * kiloConst * kiloConst * kiloConst;
			}

			if (sufix.Equals("mb", StringComparison.OrdinalIgnoreCase))
			{
				return numValue * kiloConst * kiloConst;
			}

			if (sufix.Equals("kb", StringComparison.OrdinalIgnoreCase))
			{
				return numValue * kiloConst;
			}

			if (sufix.Equals("b", StringComparison.OrdinalIgnoreCase))
			{
				return numValue;
			}

			// unsupported sufix
			return defaultValue;
		}

		/// <summary>
		/// Parses the <c>integer</c> value from <c>str</c>.
		/// </summary>
		/// <param name="str"> the string representation of an integer. </param>
		internal static int ParseInt(string str, int defaultValue)
		{
			if (string.IsNullOrWhiteSpace(str))
			{
				return defaultValue;
			}

			var start = 0;
			for (var i = 0; i < str.Length; i++)
			{
				if (str[i] != ' ')
				{
					start = i;
					break;
				}
			}

			var length = str.Length - start;
			for (var i = str.Length; i > 0; i--)
			{
				if (str[i - 1] != ' ')
				{
					length = i - start;
					break;
				}
			}

			return ParseInt(str, start, length, defaultValue);
		}

		/// <summary>
		/// Parses the <c>integer</c> value from <c>str</c>.
		/// </summary>
		/// <param name="str"> the string representation of an integer. </param>
		internal static int ParseInt(string str)
		{
			if (string.IsNullOrWhiteSpace(str))
			{
				throw new ArgumentNullException(nameof(str));
			}

			var start = 0;
			for (var i = 0; i < str.Length; i++)
			{
				if (str[i] != ' ')
				{
					start = i;
					break;
				}
			}

			var length = str.Length - start;
			for (var i = str.Length; i > 0; i--)
			{
				if (str[i - 1] != ' ')
				{
					length = i - start;
					break;
				}
			}

			return ParseInt(str, start, length);
		}

		/// <summary>
		/// Parses the <c>integer</c> value from <c>str</c>.
		/// <p/>
		/// The <c>offset</c> argument is the
		/// index of the first char of the substring and the <c>count</c>
		/// argument specifies the length of the substring.
		/// </summary>
		/// <param name="str">    a string representation of an integer. </param>
		/// <param name="offset"> the initial offset </param>
		/// <param name="count">  the length </param>
		/// <exception cref="FormatException"> </exception>
		internal static int ParseInt(string str, int offset, int count)
		{
			var limit = offset + count;
			var isNegative = limit > offset && str[offset] == '-';
			if (isNegative)
			{
				offset++;
			}

			if (limit == offset)
			{
				throw new FormatException("Can't parse int from value '" + str + "'");
			}

			var value = 0;
			while (offset < limit)
			{
				var ch = str[offset++];
				if (ch < '0' || ch > '9' || value > int.MaxValue / 10)
				{
					throw new FormatException("Can't parse int from value '" + str + "'");
				}

				value = value * 10 + (ch - '0');
			}

			return isNegative ? -value : value;
		}

		/// <summary>
		/// Parses the <c>integer</c> value from <c>str</c>.
		/// <p/>
		/// The <c>offset</c> argument is the
		/// index of the first char of the substring and the <c>count</c>
		/// argument specifies the length of the substring.
		/// </summary>
		/// <param name="str">    a string representation of an integer. </param>
		/// <param name="offset"> the initial offset </param>
		/// <param name="count">  the length </param>
		internal static int ParseInt(string str, int offset, int count, int defaultValue)
		{
			var limit = offset + count;
			var isNegative = limit > offset && str[offset] == '-';
			if (isNegative)
			{
				offset++;
			}

			if (limit == offset)
			{
				return defaultValue;
			}

			int value = 0;
			while (offset < limit)
			{
				var ch = str[offset++];
				if (ch < '0' || ch > '9' || value > int.MaxValue / 10)
				{
					return defaultValue;
				}

				value = value * 10 + (ch - '0');
			}

			return isNegative ? -value : value;
		}

		/// <summary>
		/// Parses the <c>boolean</c> value from <c>str</c>.
		/// </summary>
		/// <param name="str"> the string representation of a boolean. </param>
		internal static bool ParseBool(string str)
		{

			if (string.IsNullOrWhiteSpace(str))
			{
				throw new ArgumentNullException(nameof(str));
			}

			str = str.Trim();
			var sc = StringComparison.OrdinalIgnoreCase;

			if ("Yes".Equals(str, sc) || "True".Equals(str, sc))
			{
				return true;
			}

			if ("No".Equals(str, sc) || "False".Equals(str, sc))
			{
				return false;
			}

			throw new FormatException("Can't parse bool from value '" + str + "'");
		}

		/// <summary>
		/// Get all properties.
		/// </summary>
		/// <value> returned value is cloned. </value>
		public virtual Dictionary<string, string> Properties
		{
			get
			{
				var dict = new Dictionary<string, string>();
				foreach (var prop in _properties.GetAllProperties())
				{
					dict.Add(prop.Key, prop.Value);
				}

				return dict;
			}
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}

			if (o == null || GetType() != o.GetType())
			{
				return false;
			}

			var that = (Config)o;

			if (!_properties?.Equals(that._properties) ?? that._properties != null)
			{
				return false;
			}

			return true;
		}

		public override int GetHashCode()
		{
			var result = _properties != null ? _properties.GetHashCode() : 0;
			//        result = 31 * result + (exceptionRuntimeValues != null ? exceptionRuntimeValues.hashCode() : 0);
			return result;
		}

		public virtual bool Exists(string propertyName)
		{
			return _properties.Exists(propertyName);
		}

		private static void LoadDefault()
		{
			var cl = typeof(Config);
			var fields = cl.GetFields();
			foreach (var fd in fields)
			{
				if (Attribute.IsDefined(fd, typeof(DefaultValue)) && fd.FieldType == typeof(string))
				{
					var attr = (DefaultValue)Attribute.GetCustomAttribute(fd, typeof(DefaultValue));
					DefaultProperties.Put((string)fd.GetValue(null), attr.Value);
				}
			}
		}

		[AttributeUsage(AttributeTargets.Field)]
		public class DefaultValue : Attribute
		{
			internal string Value;

			public DefaultValue(string value)
			{
				Value = value;
			}
		}

		internal interface IValidator
		{
			bool Valid(string value);
		}

		/// <summary>
		/// User value validator for range max >= UserValue >= min
		/// </summary>
		internal class ValidatorInteger : IValidator
		{
			internal int Max;

			internal int Min;

			public ValidatorInteger(int min, int max)
			{
				Min = min;
				Max = max;
			}

			public virtual bool Valid(string value)
			{
				int valueInt;
				try
				{
					valueInt = ParseInt(value);
				}
				catch (Exception)
				{
					return false;
				}

				return Max >= valueInt && valueInt >= Min;
			}

			public override string ToString()
			{
				return "min=" + Min + ", max=" + Max;
			}
		}

		/// <summary>
		/// User values list validator for range max >= UserValue >= min
		/// </summary>
		internal class ValidatorIntegerList : IValidator
		{
			internal ValidatorInteger Validator;

			public ValidatorIntegerList(int min, int max)
			{
				Validator = new ValidatorInteger(min, max);
			}

			public virtual bool Valid(string value)
			{
				return value.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
							.All(v => Validator.Valid(v));
			}

			public override string ToString()
			{
				return "list of min=" + Validator.Min + ", max=" + Validator.Max;
			}
		}

		/// <summary>
		/// User value validator for bool
		/// </summary>
		internal class ValidatorBoolean : IValidator
		{
			public ValidatorBoolean()
			{
			}

			public virtual bool Valid(string value)
			{
				try
				{
					var valueBool = ParseBool(value);
				}
				catch (Exception)
				{
					return false;
				}

				return true;
			}

			public override string ToString()
			{
				return "True|False";
			}
		}
	}
}
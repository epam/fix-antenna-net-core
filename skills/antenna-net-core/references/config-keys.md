# fixengine.properties — complete key index (verified against 1.2.3 source)

Every key recognized by `Epam.FixAntenna.NetCore` 1.2.3. Names and defaults extracted from
`Configuration/Config.cs` (`[DefaultValue]` attributes) and actual parsing code — not from upstream docs.

Format reminder:
- `sessionIDs=id1,id2` declares configured sessions; `sessions.<id>.<key>=...` sets a key for one session.
- `sessions.default.<key>=...` sets a default for all declared sessions; unprefixed `<key>=...` is process-wide.
- Env vars override file: `FANET_<key>` / `FANET_sessions__<id>__<key>` (dots → `__`, names case-insensitive).

Scope column: **S** = per-session (usable under `sessions.<id>.`), **G** = process/server-wide only,
**S/G** = meaningful at both levels. Session config is built by overlaying `sessions.<id>.*` on a clone of the
global config, so any **S** key also works unprefixed as a global default. Default `—` = no `[DefaultValue]` in code.

## Session identity & connection

| Key | Default | Scope | Meaning |
|---|---|---|---|
| `port` | `""` | S/G | Acceptor: listening port(s), comma-separated. Initiator: connect port (per-session). |
| `connectAddress` | — | S | Local IP to send from on multi-homed host (initiator). |
| `suppressSessionQualifierTagInLogonMessage` | `false` | S | Don't add session-qualifier tag to Logon. |
| `logonMessageSessionQualifierTag` | `9012` | S | Tag number used for session qualifier in Logon. |
| `logonCustomizationStrategy` | `none` | S | Strategy applied to Logon before sending; e.g. `Epam.FixAntenna.NetCore.FixEngine.Session.Impl.CmeSecureLogonStrategy`. |
| `cmeSecureKeysFile` | — | S | Keys file for CME secure logon strategy. |
| `customFixVersions` | — | G | Comma-separated list of custom FIX dictionary aliases. |
| `customFixVersion.<alias>.fixVersion` | — | G | Base standard FIX version for the alias. |
| `customFixVersion.<alias>.fileName` | — | G | Custom dictionary file for the alias. |

## Sequence numbers & recovery

| Key | Default | Scope | Meaning |
|---|---|---|---|
| `forceSeqNumReset` | `Never` | S | Auto-send Logon with 34=1/141=Y: `Always` \| `OneTime` \| `Never`. |
| `performResetSeqNumTime` | `false` | S | Reset seq nums at `resetSequenceTime`. |
| `resetSequenceTime` | `00:00:00` | S | Time (HH:MM:SS) of scheduled seq num reset. |
| `resetSequenceTimeZone` | `UTC` | S | Time zone id for `resetSequenceTime`. |
| `intraDaySeqNumReset` | `false` | S | Reset seq nums after session close. |
| `ignoreResetSeqNumFlagOnReset` | `false` | S | Don't send 141=Y after sequence reset. |
| `resetSeqNumFromFirstLogon` | `Never` | S | Adopt seq nums from incoming Logon after outdated scheduled reset: `Never` \| `Schedule`. |
| `handleSeqNumAtLogon` | `false` | S | Process 789-NextExpectedMsgSeqNum: update outgoing seq num from it. |
| `ignoreSeqNumTooLowAtLogon` | `false` | S | Continue with received (too-low) seq num at Logon. |
| `resetThreshold` | `0` | S | Acceptor-only: incoming seq gap treated as missed counterparty reset (0 = off). |
| `switchOffSendingMultipleResendRequests` | `false` | S | Don't send multiple ResendRequests for the same gap. |
| `sequenceResendManagerMessageBufferSize` | `32` | S | Max messages buffered while a resend request is in flight. |
| `resendRequestNumberOfMessagesLimit` | `0` | S | Max messages a counterparty may request per RR; excess → Reject (0 = unlimited). |
| `maxRequestResendInBlock` | `0` | S | Split own resend requests into blocks of N messages (0 = no split). |
| `allowedCountOfSimilarRR` | `3` | S | Identical RRs allowed before infinite-resend-loop detection. |
| `advancedResendRequestProcessing` | `false` | S | Issue PossDup duplicates of last RR for continuing gaps (369-based). |
| `skipDuplicatedResendRequests` | `false` | S | Respond only to original RR, ignore duplicates. |
| `possDupSmartDelivery` | `false` | S | Deliver only PossDup messages not received before. |
| `ignorePossDupForGapFill` | `true` | S | Accept SequenceReset-GapFill without 43=Y / missing 122. |
| `origSendingTimeChecking` | `true` | S | Validate OrigSendingTime(122) on PossDup messages. |
| `enhancedCmeResendLogic` | `false` | S | CME Enhanced Resend Request gap-fill logic. |
| `seqNumLength` | `1` | S | Min digits of SeqNum fields (1–10); zero-padded. |

## Storage & logs

| Key | Default | Scope | Meaning |
|---|---|---|---|
| `storageFactory` | `Epam.FixAntenna.NetCore.FixEngine.Storage.FilesystemStorageFactory` | S | Storage impl: also `SlicedFileStorageFactory`, `MmfStorageFactory`, `InMemoryStorageFactory`. |
| `fa.home` | `.` | G | Base dir referenced by `${fa.home}` in other values. |
| `storageDirectory` | `${fa.home}/logs` | S | Directory for message logs/queues (file storage only). |
| `incomingLogFile` | `{0}.in` | S | Incoming log template ({0}=sessionID {1}=Sender {2}=Target {4}=qualifier). |
| `outgoingLogFile` | `{0}.out` | S | Outgoing log template. |
| `backupIncomingLogFile` | `{0}-{3}.in` | S | Backup incoming log template ({3}=timestamp). |
| `backupOutgoingLogFile` | `{0}-{3}.out` | S | Backup outgoing log template. |
| `sessionInfoFile` | `{0}.properties` | S | Session state/properties file template. |
| `outgoingQueueFile` | `{0}.outq` | S | Outgoing queue file template. |
| `incomingStorageIndexed` | — | S | Index incoming storage (messages readable via API). No default ⇒ off. |
| `outgoingStorageIndexed` | `true` | S | Index outgoing storage; disabled ⇒ never resend, always gap-fill. |
| `maxStorageSliceSize` | `100Mb` | S | Max file size before a new slice (SlicedFileStorageFactory only). |
| `storageGrowSize` | `false` | S | Enable storage pre-grow (persistent storage). |
| `maxStorageGrowSize` | `1Mb` | S | Max storage grow size in bytes. |
| `mmfStorageGrowSize` | `100Mb` | S | MMF storage grow size (MmfStorageFactory only). |
| `mmfIndexGrowSize` | `20Mb` | S | MMF index grow size (MmfStorageFactory + indexed storage). |
| `storageCleanupMode` | `None` | S | On session close: `None` \| `Backup` \| `Delete` storage. |
| `storageBackupDir` | `${fa.home}/logs/backup` | S | Backup dir when `storageCleanupMode=Backup`. |
| `timestampsInLogs` | `true` | S | Prefix in/out log lines with timestamps. |
| `timestampsPrecisionInLogs` | `Milli` | S | Log timestamp precision: `Milli` \| `Micro` \| `Nano`. |
| `backupTimestampsPrecision` | `Milli` | S | Timestamp precision in backup file names. |
| `logFilesTimeZone` | — | S | Time zone for log timestamps (default: system). |

## Queue & sending

| Key | Default | Scope | Meaning |
|---|---|---|---|
| `inMemoryQueue` | `false` | S | In-memory (vs persistent) queue for FilesystemStorageFactory. |
| `memoryMappedQueue` | `true` | S | Memory-mapped (vs persistent) queue for MmfStorageFactory. |
| `queueThresholdSize` | `0` | S | Queue size that pauses the pumper thread (0 = no control). |
| `maxMessagesToSendInBatch` | `10` | S | Max messages buffered before a transport write (> 0). |
| `preferredSendingMode` | `sync` | S | `Async` \| `Sync` \| `SyncNoqueue`. |
| `waitForMsgQueuingDelay` | `1000` | S | Max ms to wait for queue space before forcing enqueue. |
| `maxDelayToSendAfterLogon` | `50` | S | Pause (ms) before sending queued app messages after Logon. |
| `enableMessageRejecting` | `false` | S | On close, pass queued messages to IRejectMessageListener and clear queue. |
| `resetQueueOnLowSequence` | `true` | S | Reset outgoing queue when client connects with low seq num. |
| `enableNagle` | `true` | S | Nagle's algorithm (false ⇒ TCP_NODELAY, lower latency). |
| `tcpSendBufferSize` | `0` | S | Socket.SendBufferSize (0 = OS default). |
| `tcpReceiveBufferSize` | `0` | S | Socket.ReceiveBufferSize (0 = OS default). |

## Validation

| Key | Default | Scope | Meaning |
|---|---|---|---|
| `validation` | `false` | S | Master switch for dictionary-based validation of incoming messages. |
| `wellformenessValidation` | `true` | S | Validate tags 8, 9, 35, 10 (only if `validation=true`). |
| `allowedFieldsValidation` | `true` | S | Validate allowed message fields (only if `validation=true`). |
| `requiredFieldsValidation` | `true` | S | Validate required fields (only if `validation=true`). |
| `fieldTypeValidation` | `true` | S | Validate field values vs data types (only if `validation=true`). |
| `conditionalValidation` | `true` | S | Conditionally-required field validation; expensive (only if `validation=true`). |
| `groupValidation` | `true` | S | Repeating group validation (only if `validation=true`). |
| `duplicateFieldsValidation` | `true` | S | Duplicate field validation (only if `validation=true`). |
| `fieldOrderValidation` | `true` | S | Header/body/trailer tag order validation (only if `validation=true`). |
| `validateGarbledMessage` | `true` | S | Check 8/9/35/10 existence+order and BodyLength value on incoming flow. |
| `validateCheckSum` | `true` | S | Validate CheckSum(10); only if `validateGarbledMessage=true`. |
| `maxMessageSize` | `1Mb` | S | Max incoming message size (0 = unlimited, risky). |
| `senderTargetIdConsistencyCheck` | `true` | S | Reject messages whose 49/56 don't match session parameters. |
| `rawTags` | `96, 91, 213, 349, 351, 353, 355, 357, 359, 361, 363, 365, 446, 619, 622` | S | Tags treated as raw data (may contain SOH). |

## Timers & session lifecycle

| Key | Default | Scope | Meaning |
|---|---|---|---|
| `reasonableDelayInMs` | `120000` | S | Allowed SendingTime(52) delay for incoming messages. |
| `measurementAccuracyInMs` | `1` | S | Accuracy used in sending-time checks. |
| `checkSendingTimeAccuracy` | `true` | S | Toggle SendingTime(52) accuracy check. |
| `heartbeatReasonableTransmissionTime` | `200` | S | "Reasonable transmission time" (ms) added to HBI timeouts. |
| `testRequestsNumberUponDisconnection` | `1` | S | TestRequests sent before declaring connection lost. |
| `forcedLogoffTimeout` | `2` | S | Seconds to wait for Logout ack (low-seqnum disconnect case). |
| `logoutWaitTimeout` | — | S | Seconds to wait for logoff (default: session HBI). |
| `disconnectOnLogonHeartbeatMismatch` | `true` | S | Disconnect if Logon reply's 108 differs from configured HBI. |
| `quietLogonMode` | `false` | S | Quietly accept Logout as first message (no RR/warning). |
| `readingThreadShutdownTimeout` | `-1` | S | Seconds to wait for reading thread at shutdown (-1 = HBI). |
| `writingThreadShutdownTimeout` | `-1` | S | Seconds to wait for writing thread at shutdown (-1 = HBI). |
| `timestampsPrecisionInTags` | `Milli` | S | Timestamp precision in FIX tags: `Second` \| `Milli` \| `Micro` \| `Nano`. |
| `allowedSecondsFractionsForFIX40` | `false` | S | Allow sub-second timestamps for FIX 4.0. |
| `includeLastProcessed` | `false` | S | Add LastMsgSeqNumProcessed(369) to every message (FIX > 4.2). |

## TLS/SSL

| Key | Default | Scope | Meaning |
|---|---|---|---|
| `requireSSL` | `false` | S/G | Require secured transport (per-session or all sessions). |
| `sslPort` | `""` | G | Acceptor SSL/TLS listening port(s); ignored on initiator. |
| `sslProtocol` | `None` | S | SslProtocols value; `None` = best available. |
| `sslCertificate` | `""` | S | Certificate: file name or store DN (`CN=...`). |
| `sslCertificatePassword` | `""` | S | Password for the certificate. |
| `sslValidatePeerCertificate` | `true` | S | Validate remote certificate (false also disables revocation check). |
| `sslCheckCertificateRevocation` | `true` | S | Check remote certificate revocation (needs peer validation on). |
| `sslCaCertificate` | `""` | S | CA certificate: file name or store DN. |
| `sslServerName` | `""` | S | Initiator-only: must match CN in acceptor certificate. |

## Scheduler

| Key | Default | Scope | Meaning |
|---|---|---|---|
| `tradePeriodBegin` | — | S/G | Cron (Quartz) for session/server start; combine with `\|`. |
| `tradePeriodEnd` | — | S/G | Cron for session/server stop. |
| `tradePeriodTimeZone` | `UTC` | S/G | Time zone for the two cron expressions. |

## Autoreconnect & backup connection

| Key | Default | Scope | Meaning |
|---|---|---|---|
| `autoreconnectAttempts` | `-1` | S | <0 = no reconnects, 0 = infinite, >0 = attempt count. |
| `autoreconnectDelayInMs` | `1000` | S | Delay between reconnect attempts (ms). |
| `enableAutoSwitchToBackupConnection` | `true` | S | Auto-switch to backup destination on failure. |
| `cyclicSwitchBackupConnection` | `true` | S | Cycle back to primary connection. |
| `resetOnSwitchToBackup` | `false` | S | Reset seq nums when switching to backup. |
| `resetOnSwitchToPrimary` | `false` | S | Reset seq nums when switching back to primary. |

> ⚠ `backupHost` / `backupPort` appear in upstream Configuration.md but are **not parsed anywhere in 1.2.3**.
> Backup destinations are set via `SessionParameters.AddDestination(...)` in code, or restored from persisted
> `socketConnectAddress_<N>` entries (`host:port`).

## Acceptor behavior & autostart admin

| Key | Default | Scope | Meaning |
|---|---|---|---|
| `serverAcceptorStrategy` | `Epam.FixAntenna.NetCore.FixEngine.Acceptor.AllowNonRegisteredAcceptorStrategyHandler` | G | Acceptor strategy; alt: `DenyNonRegisteredAcceptorStrategyHandler`. |
| `loginWaitTimeout` | `5000` | G | Ms before a connected-but-not-logged-on acceptor session is disposed. |
| `sendRejectIfApplicationIsNotAvailable` | `true` | S | Reject when app listener unavailable; false = silently swallow. |
| `autostart.acceptor.targetIds` | — | G | Comma-separated TargetCompIDs auto-accepted by the server. |
| `autostart.acceptor.<TargetID>.login` | `admin` | G | Username(553) required for the autostart session. |
| `autostart.acceptor.<TargetID>.password` | `admin` | G | Password(554) required. |
| `autostart.acceptor.<TargetID>.ip` | `*` | G | Allowed source IPs (`*`, list, or subnet masks `a.b.c.d/n`). |
| `autostart.acceptor.<TargetID>.fixServerListener` | — | G | `IFixServerListener` type, e.g. `Epam.FixAntenna.AdminTool.AdminTool,Epam.FixAntenna.AdminTool`. |
| `autostart.acceptor.<TargetID>.storageType` | `Transient` | G | `Transient` (in-memory) or `Persistent` (file) storage for that session. |
| `autostart.acceptor.commands.package` | — | G | Custom admin-command package for the AdminTool plugin. |

> ⚠ Configuration.md spells it `autostart.acceptor.command.package` in one place — the code key is
> `autostart.acceptor.commands.package` (plural `commands`).

## Performance & affinity

| Key | Default | Scope | Meaning |
|---|---|---|---|
| `cpuAffinity` | `-1` | S | CPU id pinned for both send and receive threads (-1 = none). |
| `recvCpuAffinity` | `-1` | S | CPU id for the receive thread. |
| `sendCpuAffinity` | `-1` | S | CPU id for the send thread. |
| `enableMessageStatistic` | `true` | S | Count messages/bytes read and sent. |
| `markIncomingMessageTime` | `false` | S | Nanosecond read-time mark on incoming messages (GetLastReadMessageTimeNano). |
| `slowConsumerDetectionEnabled` | `false` | S | Detect slow consumers in pumpers. |
| `slowConsumerWriteDelayThreshold` | `10` | S | Max ms to send a message before slow-consumer notification. |

## Logging & masking

| Key | Default | Scope | Meaning |
|---|---|---|---|
| `maskedTags` | `554, 925` | S | Tag values replaced with asterisks in logs. |
| `writeSocketAddressToLog` | `false` | S | Prefix debug-log message dumps with `[ip]`. |
| `enableLoggingOfIncomingMessages` | — | S | Extra logging of incoming messages (no default ⇒ off). |

## Throttling

| Key | Default | Scope | Meaning |
|---|---|---|---|
| `throttleCheckingEnabled` | `false` | S | Per-MsgType incoming rate checks; breach ⇒ disconnect (THROTTLING). |
| `throttleCheckingPeriod` | `1000` | S | Period (ms) common to all throttle checks. |
| `throttleChecking.<MsgType>.threshold` | `-1` | S | Allowed messages of `<MsgType>` per period (-1 = disabled). No `Config.*` constant — literal key built in `ThrottleCheckingHandler`. |

## Message handlers

Literal keys (constants live in `AbstractFixSessionFactory`, not `Config`):

| Key | Default | Scope | Meaning |
|---|---|---|---|
| `system.messagehandler.global.<N>` | built-in chain | S | Replace system global handler chain; applied in reverse numeric order. |
| `user.messagehandler.global.<N>` | — | S | User handlers, applied after system handlers. |
| `system.messagehandler.<MsgType>` | built-in | S | Per-type handler override for session msg types A,0,1,2,3,4,5. |

## Misc

| Key | Default | Scope | Meaning |
|---|---|---|---|
| `sessionSequenceManager` | `Epam.FixAntenna.NetCore.FixEngine.Session.StandardSessionSequenceManager` | S | Replaceable sequence-manager implementation. |
| `encryptionMode` | `None` | S | Declared values `None` \| `Des` \| `PgpDesMd5` — **non-functional, see below**. |
| `encryptionConfig` | `${fa.home}/encryption/encryption.cfg` | S | Encryption config file — non-functional. |
| `desKey` | — | S | DES key (prefix) — non-functional. |
| `pubKeyFile` | — | S | PGP public key file — non-functional. |
| `secKeyFile` | — | S | PGP secret key file — non-functional. |
| `pgpKey` | — | S | PGP key — non-functional. |

> ⚠ **Encryption is not implemented in 1.2.3.** The six keys above are declared in `Config.cs` but never read by
> any engine code, and `StandardMessageFactory.GetLoginHeader()`/`CompleteLogin()` hardcode `98=0` (EncryptMethod
> NONE) into every outgoing Logon.

## Keys with no `Config.*` constant (literal-string parsing)

Parsed by exact string in `SessionParametersBuilder` / `SessionParameters.FromProperties` — all per-session
(under `sessions.<id>.` / `sessions.default.`) except `sessionIDs`:

| Key | Default | Meaning |
|---|---|---|
| `sessionIDs` | — | Global: comma-separated list of configured session ids. |
| `sessionType` | — | `acceptor` \| `initiator`; unset = session usable as both. |
| `sessionID` | — | Custom session id override (also written to the session info file). |
| `senderCompID` | — | SenderCompID(49). |
| `targetCompID` | — | TargetCompID(56). |
| `senderSubID` | — | SenderSubID(50). |
| `targetSubID` | — | TargetSubID(57). |
| `senderLocationID` | — | SenderLocationID(142). |
| `targetLocationID` | — | TargetLocationID(143). |
| `sessionQualifier` | — | Session qualifier (distinguishes sessions with same comp ids). |
| `host` | — | Initiator connect host. |
| `port` | — | Initiator connect port (parsed via `Convert.ToInt32`). |
| `bindIP` | — | Local IP to bind the initiator socket. |
| `fixVersion` | — | BeginString(8): `FIX.4.0`–`FIX.4.4`, `FIXT.1.1`, or a `customFixVersions` alias. |
| `appVersion` | — | ApplVer(1128) for FIXT.1.1: `FIX.4.0`–`FIX.5.0SP2` or custom alias. |
| `heartbeatInterval` | `30` | HeartBtInt(108) in seconds (property default in `SessionParameters`). |
| `username` | — | Username(553) for Logon. |
| `password` | — | Password(554) for Logon. |
| `incomingSequenceNumber` | — | Initial incoming seq num (used if `inSeqNumsForNextConnect` absent). |
| `outgoingSequenceNumber` | — | Initial outgoing seq num (used if `outSeqNumsForNextConnect` absent). |
| `inSeqNumsForNextConnect` | — | Incoming seq num for next connect (engine-persisted; wins over the above). |
| `outSeqNumsForNextConnect` | — | Outgoing seq num for next connect (engine-persisted). |
| `lastSeqNumResetTimestamp` | — | Persisted ticks of last seq reset (engine-written). |
| `FixMessage` | — | Raw tag=value list appended to every outgoing message (note exact casing). |
| `outgoingLoginFixMessage` | — | Extra raw fields for outgoing Logon. |
| `incomingLoginFixMessage` | — | Persisted incoming Logon (engine-written). |
| `socketConnectAddress_<N>` | — | Backup destination list, `host:port`, N = 0,1,2... |

Plus the literal-pattern keys listed above: `throttleChecking.<MsgType>.threshold`,
`system.messagehandler.global.<N>`, `system.messagehandler.<MsgType>`, `user.messagehandler.global.<N>`,
and `autostart.acceptor.*`.

> ⚠ None of these can be reached via `Config.X` from code — set them as strings, or use the typed
> `SessionParameters` properties (`SenderCompId`, `Port`, `HeartbeatInterval`, ...). The only constant is
> `SessionParametersBuilder.SessionTypeProp` (= `"sessionType"`).

> ⚠ Upstream Configuration.md also lists `processedIncomingSequenceNumber` — it is **not parsed** by any 1.2.3
> code path. Same for `backupHost`/`backupPort` (see Autoreconnect section).

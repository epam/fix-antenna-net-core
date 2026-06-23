# 07 Persistence & Recovery ★

> Targets **Epam.FixAntenna.NetCore 1.2.3**. See `../SKILL.md` for root API rules.

This is the single most under-designed area in LLM-generated FIX code. Read carefully. The engine does most of the work — the job is to NOT fight it.

## What the engine handles (DON'T DO THESE FROM APP CODE)

| Concern | Engine behavior |
|---|---|
| Sequence number assignment on outbound | Engine increments `MsgSeqNum` per outbound message. |
| Sequence number validation on inbound | Engine checks against expected next-incoming. |
| Resend Request on detected gap | Engine sends `ResendRequest (2)` automatically when inbound seq > expected. |
| Response to peer's Resend Request | Engine replays from storage, gap-filling admin messages. |
| Heartbeats / TestRequests | Engine sends and responds. |
| Restart recovery | Engine reads persistent storage on startup; resumes sequence numbers. |
| Logon negotiation (incl. ResetSeqNumFlag) | Engine handles per `SessionParameters.ForceSeqNumReset`. |

## What the application handles

| Concern | Application code |
|---|---|
| Choose persistent storage | Set `storageFactory` to a filesystem-backed implementation in `fixengine.properties`. |
| Decide ResetSeqNumFlag policy | Per-counterparty agreement: daily reset? On corruption only? |
| Application-level idempotency | Even with PossDupFlag, business code must dedupe orders (ClOrdID), executions (ExecID). |

## Storage configuration

In `fixengine.properties` (full file layout — note `sessionIDs` and `sessions.<id>.*` prefix):

```properties
# Process-wide
storageFactory   = Epam.FixAntenna.NetCore.FixEngine.Storage.FilesystemStorageFactory
storageDirectory = ./logs

# Sessions
sessionIDs = demoInit
sessions.default.fixVersion        = FIX.4.4
sessions.default.heartbeatInterval = 30

sessions.demoInit.sessionType       = initiator
sessions.demoInit.senderCompID      = BUYSIDE
sessions.demoInit.targetCompID      = VENUE
sessions.demoInit.host              = 127.0.0.1
sessions.demoInit.port              = 9876
sessions.demoInit.forceSeqNumReset  = OneTime
```

> ⚠️ Property KEYS are case-insensitive — `Common/Properties.cs` builds the underlying dictionary with `StringComparer.OrdinalIgnoreCase`. So `storageFactory`, `STORAGEFACTORY`, and `storagefactory` all resolve to the same key. But unknown keys are still **silently ignored** by the engine, so `storage_factory` (or any other typo) is just an ignored unknown key — the engine then uses the *default*, which is `FilesystemStorageFactory` (`Config.cs`), so recovery still works but the factory/settings you intended silently don't apply. (A misspelled factory *value* is not silent: `ReflectStorageFactory.cs` logs a WARN — "Can not load storage factory: … Loaded default FilesystemStorageFactory." — and falls back to the durable default.) Same trap with `sessions=` (no `IDs`) or `<id>.<key>` lacking the `sessions.` prefix: the session list will be empty at runtime with no error. The `sessionType` *value* is also case-insensitive (`SessionParametersBuilder.cs` compares with `OrdinalIgnoreCase`), so `initiator` / `Initiator` / `INITIATOR` all work.

Each session writes these files under `storageDirectory` (names are templates, `{0}` = sessionID — verified in `Config.cs` / `FilesystemStorageFactory.cs`):

| File | Template property | Content |
|---|---|---|
| `<sessionId>.out` | `outgoingLogFile` | Outbound message log — the replay source for ResendRequest. |
| `<sessionId>.in` | `incomingLogFile` | Inbound message log, for audit. |
| `<sessionId>.properties` | `sessionInfoFile` | Session parameters + sequence numbers — what survives restart. |
| `<sessionId>.outq` | `outgoingQueueFile` | Persistent outgoing queue (queued-but-unsent messages). |

Timestamped backups (`backupOutgoingLogFile` = `{0}-{3}.out`, `backupIncomingLogFile` = `{0}-{3}.in`, where `{3}` is a timestamp) are written under `storageBackupDir` (default `${fa.home}/logs/backup`).

In-memory storage (`InMemoryStorageFactory`) exists for tests. **Never use it for production sessions.** See `08-custom-storage-replication.md` for the full factory list.

## ForceSeqNumReset — engine-managed

`SessionParameters.ForceSeqNumReset` is an enum with **exactly three** values:

| Value | Behavior |
|---|---|
| `Always` | Send `ResetSeqNumFlag (141) = Y` on every Logon (rare; defeats restart recovery). |
| `OneTime` | Send `141=Y` on next Logon only, then revert. Use for first connect or after manual recovery. |
| `Never` | Default. Production norm. |

> ⚠️ There is no `OnLogon` value. `ForceSeqNumReset.OnLogon` will not compile.

## The recovery flow (engine-driven, observable from the listener)

### Scenario A — peer disconnected, reconnected, missing messages 100–103

```
Peer reconnects and requests 100–103 — either via ResendRequest(2) after Logon,
  or via NextExpectedMsgSeqNum(789) on its Logon (789 is sent only by peers
  configured to do so; for a FIX Antenna peer that's handleSeqNumAtLogon=true)
Engine reads outbound log, replays 100, 101, 102, 103 with PossDupFlag=Y
Peer is caught up
```

### Scenario B — peer sent 100, 101 then 105 (102–104 missed locally)

```
Engine detects gap on inbound 105
Engine sends ResendRequest(BeginSeqNo=102, EndSeqNo=104)
Peer replays 102, 103, 104 (with PossDupFlag=Y on app msgs, GapFill for admin msgs)
Engine accepts, then processes the queued 105
```

### Scenario C — process restart

```
Engine reads <sessionId>.properties: next outbound = 49, next incoming = 89
Engine logs on at MsgSeqNum=49
Peer's Logon carries NextExpectedMsgSeqNum(789)=47
  → engine replays 47, 48 from the outbound log with PossDupFlag=Y
If instead the peer claims MORE than the engine ever sent (789 > 49)
  → engine force-disconnects: "NextExpectedMsgSeqNum(789) request sequence ...
    which is higher then actual ..." (LogonMessageHandler)
Inbound side: first post-logon message arriving with seq > 89
  → engine sends ResendRequest(BeginSeqNo=89, ...) (OutOfSequenceMessageHandler)
Engine resumes normal operation
```

Tag 789 mechanics (verified in source): the engine **always processes** a `NextExpectedMsgSeqNum(789)` received on the peer's Logon — no setting needed on the receiving side. The `handleSeqNumAtLogon` key (default **false**, `Config.cs`) is needed on the side that wants to **send** 789 on its *own* Logon (FIX 4.4+ only); with it enabled, the engine puts 789 on its Logon instead of sending a post-logon ResendRequest.

### Seq-num policy knobs (verified in `Config.cs`)

| Key | Default | Meaning |
|---|---|---|
| `handleSeqNumAtLogon` | `false` | Send `NextExpectedMsgSeqNum(789)` on own Logon (FIX 4.4+) instead of a post-logon ResendRequest. A *received* 789 is always honored regardless. |
| `intraDaySeqNumReset` | `false` | Reset sequence numbers after the session is closed. |
| `ignoreSeqNumTooLowAtLogon` | `false` | Accept a too-low incoming seq num at Logon and continue with the received value instead of disconnecting. |
| `resetThreshold` | `0` | Acceptor only: an incoming-seq gap at connect larger than this is treated as a missed counterparty seq-reset event (prevents requesting old messages). `0` = check disabled. |
| `resetSeqNumFromFirstLogon` | `Never` | `Never` \| `Schedule`: on the first Logon after a scheduled reset point, align sequences instead of treating the drop as a gap. |
| `quietLogonMode` | `false` | Tolerate a Logout as the first incoming message (possibly with wrong seq) without sending a ResendRequest/warning. |

**Observe** these scenarios — log them — but do not implement them. Note *where* they are observable: recovery traffic (ResendRequest, SequenceReset, Logon/Logout) is session-level and never reaches the main `IFixSessionListener` — register an in-session-level listener for it (see below).

## What the application code should do — verified pattern

```csharp
using System;                                     // Console
using System.Collections.Generic;                 // HashSet for dedup
using System.Linq;                                // .First()
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.Message;
using Tags = Epam.FixAntenna.Constants.Fix44.Tags;

internal static class Program
{
    public static int Main()
    {
        var configured = SessionParametersBuilder.BuildSessionParametersList("fixengine.properties");
        var parameters = configured.Values.First();
        // ForceSeqNumReset comes from properties (forceSeqNumReset=OneTime|Never|Always).
        // Do NOT set parameters.OutgoingSequenceNumber / IncomingSequenceNumber here.

        var session = parameters.CreateInitiatorSession();
        session.SetFixSessionListener(new RecoveryListener());
        // The main listener receives APPLICATION messages only. To observe recovery
        // traffic (ResendRequest, SequenceReset, Logon/Logout, Heartbeat...) register
        // a separate in-session-level listener:
        session.AddInSessionLevelMessageListener(new RecoveryTrafficLogger());

        session.Connect();

        Console.WriteLine("Connected. Press ENTER to logout cleanly.");
        Console.ReadLine();
        session.Disconnect("operator shutdown");
        session.Dispose();
        return 0;
    }
}

// Main listener — receives application messages only (the engine routes
// session-level messages A, 0, 1, 2, 3, 4, 5 to its own handlers and to
// in-session-level listeners; they never arrive here).
internal sealed class RecoveryListener : IFixSessionListener
{
    private readonly HashSet<string> _seenClOrdIds = new();

    public void OnSessionStateChange(SessionState state)
    {
        Console.WriteLine($"[state] {state}");
        // Observe transitions; do NOT manipulate seq nums here.
    }

    public void OnNewMessage(FixMessage m)
    {
        // Dedupe app-level replays on ClOrdID (orders) / ExecID (executions).
        var possDup = m.IsTagExists(Tags.PossDupFlag)
                   && m.GetTagValueAsString(Tags.PossDupFlag) == "Y";
        if (possDup && m.IsTagExists(Tags.ClOrdID))
        {
            var clOrdId = m.GetTagValueAsString(Tags.ClOrdID);
            if (!_seenClOrdIds.Add(clOrdId)) return;
        }

        // dispatch to business layer
    }
}

// Observe-only view of session-level traffic (ResendRequest, SequenceReset, ...).
// The engine has already handled these messages — log, never act.
internal sealed class RecoveryTrafficLogger : IFixMessageListener
{
    public void OnNewMessage(FixMessage m)
    {
        Console.WriteLine($"[session-level] 35={m.GetTagValueAsString(Tags.MsgType)} 34={m.GetTagValueAsString(Tags.MsgSeqNum)}");
    }
}
```

## Common LLM mistakes

1. **Writing to `session.OutSeqNum` / `session.InSeqNum` from app code.** Yes, the properties are settable in 1.2.3. **Don't.** They're for operational tooling, not for "let me simulate a gap." Touching them corrupts recovery.
2. **Calling `session.SetSequenceNumbers(...)` or `session.ResetSequenceNumbers()` from app code.** Same as above. Operational only.
3. **Implementing custom resend logic.** The engine handles `ResendRequest (2)` automatically — replaying from outbound log, gap-filling admin slots. Custom logic conflicts and produces double-sends.
4. **Setting `MsgSeqNum (34)` on outbound messages.** Engine sets it.
5. **Calling some "reset sequence" API on every Logon.** That defeats recovery.
6. **Not enabling persistent storage.** In-memory works in tests but loses everything on restart — counterparty sees seq num drop, sends session-level reject.
7. **`ForceSeqNumReset.OnLogon`.** Doesn't exist. Only `Always`, `OneTime`, `Never`.
8. **Treating `PossDupFlag=Y` messages as new.** They are replays; business code must dedupe (by `ClOrdID` for orders, `ExecID` for executions).
9. **Persisting parsed/normalized data instead of letting the engine persist the wire log.** Use the engine's storage as the source of truth for FIX wire data; persist parsed/normalized data separately for business needs.

## When intervention is appropriate (rare, operational)

- After a poison message neither side can decode: operator sends a manual SequenceReset, both sides resume from the next number.
- Counterparty out-of-band request: documented procedure, not app code.

For these, the seq-num setters and `ResetSequenceNumbers()` method exist. They're intentionally accessible — but operational, not normal-path.

## See also

- `08-custom-storage-replication.md` — making the storage itself HA.
- `06-multi-session-router.md` — sequence handling is independent per session.

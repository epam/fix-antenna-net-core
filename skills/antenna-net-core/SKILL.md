---
name: FIX Antenna .NET Core
description: This skill should be used when the user asks to "create a FIX session", "build a FIX acceptor", "build a FIX initiator", "send a NewOrderSingle", "handle ExecutionReport", "set up drop copy", "configure fixengine.properties", "implement FIX session recovery", "add a FIX repeating group", or mentions Epam.FixAntenna.NetCore, IFixSession, IFixSessionListener, IFixMessageListener, FixServer, FixMessage, SessionParametersBuilder, ForceSeqNumReset, or FIXT 1.1 in a .NET / C# context. Skip for QuickFIX/n or other FIX libraries — APIs differ.
version: 0.2.0
---

# FIX Antenna .NET Core

Guidance for coding agents working in projects that depend on **`Epam.FixAntenna.NetCore`**. This skill is verified against version **1.2.3**.

## What FIX Antenna .NET Core is

A high-performance FIX protocol engine for .NET Standard 2.0+, maintained by EPAM / B2Bits, open-sourced under Apache 2.0. It implements **FIX 4.0 – 5.0** and **FIXT 1.1** session and application layers.

- Repository: `https://github.com/epam/fix-antenna-net-core`
- NuGet: `Epam.FixAntenna.NetCore`
- Official docs: <https://github.com/epam/fix-antenna-net-core/tree/main/Docs> — indexed and cross-mapped in `references/official-docs.md`. **Consult the bundled `references/` first; fetch upstream pages only when the topic isn't covered there** (see "When in doubt").

### What it is NOT

- **Not QuickFIX/n.** APIs differ. Do not translate QuickFIX/n samples and rename types — the session model, message construction, and config keys are different.
- **Not a broker connectivity SDK.** It speaks FIX. It does not normalize venue quirks or ship pre-built drop-in connectors per venue.
- **Not an OMS or EMS.** It gives session, message, persistence, recovery. Order state machine and business logic are out of scope.

## Mental model

Four concepts. Master these or every generation will be wrong.

```
Configuration ──► Session ──► IFixSessionListener (one class, two callbacks)
   (file/code)        │             ├─ OnSessionStateChange(SessionState)
                      │             └─ OnNewMessage(FixMessage)        ← inherited from IFixMessageListener
                      │
                      └────► Storage (durable seq + log, engine-managed)
```

| Concept | Type(s) | Role |
|---|---|---|
| **Configuration** | `Config`, `SessionParameters`, `fixengine.properties` | Where session params, storage, TLS live. Prefer file-based for production. |
| **Server / acceptor host** | `FixServer`, `IFixServerListener` | Listens for inbound connections. Per-session `NewFixSession(IFixSession)` callback decides accept/reject. |
| **Session** | `IFixSession` | One FIX session. Owns sequence numbers, heartbeats, logon/logout, resend. |
| **Session listener** | `IFixSessionListener : IFixMessageListener` | **Implements BOTH** `OnSessionStateChange(SessionState)` (declared on `IFixSessionListener`) AND `OnNewMessage(FixMessage)` (inherited from `IFixMessageListener`). Register via `session.SetFixSessionListener(...)`. Receives **application messages only** — admin messages (35=A,0,1,2,3,4,5) never reach it. |
| **Admin-message observers** (optional) | `IFixMessageListener` | Session-level (admin) messages are consumed by the engine and NOT passed to the main listener. To observe inbound admin traffic (log Logons, watch 35=3 rejects, recovery flows), register via `session.AddInSessionLevelMessageListener(...)`. |
| **Message** | `FixMessage : ITagList` | Add tags with `AddTag(int, value)`, read with `GetTagValueAsString(int)` etc. |
| **Tags (constants)** | `Epam.FixAntenna.Constants.Fix44.Tags` | Static class of tag-number `const int` fields per FIX version. |

> ⚠️ **Inheritance trap.** `IFixSessionListener` *extends* `IFixMessageListener`. The compiler will demand that any class implementing `IFixSessionListener` provide BOTH methods (`OnSessionStateChange` and `OnNewMessage`). Implement both on the same class. This is true in 1.2.3 and in 2.x master — same shape, different declaration site.

## Hard rules — DO and DON'T

These are non-negotiable for production-quality code. Agents repeatedly violate these; this section exists to stop that.

### DO

1. **Use the dictionary.** Validate every inbound message against the FIX dictionary. Reject malformed messages at session layer; do not attempt business processing on a structurally invalid message.
2. **Treat FIX as binary-typed, not text.** Read with `FixMessage.GetTagValueAsLong(Fix44.Tags.OrderQty)`, not `int.Parse(message.ToString().Split('|')...)`.
3. **Separate session handling from business logic.** `OnNewMessage` should dispatch to a business handler that does NOT know about FIX wire format. The listener is a boundary.
4. **Use a persistent storage factory in production.** Set `storageFactory = Epam.FixAntenna.NetCore.FixEngine.Storage.FilesystemStorageFactory` (default, durable) or `MmfStorageFactory` (memory-mapped, higher throughput). Never `InMemoryStorageFactory` for a live session — it's for tests. Persistent storage is what makes gap fill / resend / restart recovery work.
5. **Acknowledge state transitions explicitly.** `OnSessionStateChange` will be called many times per session lifecycle (`WaitingForLogon` → `LogonReceived` → `Connected` → `Disconnected` → `Reconnecting`, etc.). Branch via `SessionState.IsDisconnected(state)`, not on string compares.
6. **Dispose sessions.** Always `session.Dispose()` after disconnect.
7. **Know the difference between `AddTag` and `Set`.** `AddTag(tag, value)` *appends* a new occurrence — calling it twice for the same tag on the same message yields a malformed (duplicate-tag) outbound. `Set(tag, value)` (inherited from `ExtendedIndexedStorage`, available on every `FixMessage`) *updates the tag in place and adds it if absent* — every `Set` overload delegates to `UpdateValue(..., IndexedStorage.MissingTagHandling.AddIfNotExists)`, so `Set` is already the "replace if present, add if missing" call. Reach for `UpdateValue(tag, value, IndexedStorage.MissingTagHandling.DontAddIfNotExists)` only when you want the opposite — update *only* if the tag already exists, otherwise leave the message untouched (the call returns `-1`). (The enum is nested in `IndexedStorage` — fully-qualify it, or `using static Epam.FixAntenna.NetCore.Message.IndexedStorage;`.) Use `AddTag` for fresh outbound construction; use `Set` when mutating an existing message (e.g., echoing an inbound back with one field changed).
8. **Engine fills the header and trailer.** BeginString (8), BodyLength (9), MsgSeqNum (34), MsgType (35), SenderCompID (49), SendingTime (52), TargetCompID (56), CheckSum (10) are engine-managed.

### DON'T

1. **Don't hardcode sequence numbers.** Never write `session.OutSeqNum = 1` on every start, never use `SetSequenceNumbers(...)` from app code unless explicitly implementing manual recovery tooling. The engine and persistent storage manage this; touching it breaks recovery and triggers session-level rejects.
2. **Don't ignore Resend Requests.** If a counterparty asks for messages N–M, the engine handles replay automatically *if* persistent storage is configured. Don't write custom resend logic.
3. **Don't parse FIX wire format with `Split('|')` or `Split('')`.** Use `FixMessage`'s tag-typed accessors.
4. **Don't treat ExecutionReport as fire-and-forget.** `ExecType` (150) and `OrdStatus` (39) define a state machine.
5. **Don't swallow session-level rejects (msg type 3).** Means the previous outbound was malformed. Log loudly.
6. **Don't bypass admin messages.** Logon (A), Logout (5), Heartbeat (0), TestRequest (1), ResendRequest (2), SequenceReset (4), Reject (3) are handled by the engine. Don't construct them from app code.
7. **Don't catch and continue on listener exceptions silently.** Log loudly, then either rethrow or disconnect.
8. **Don't invent enum values.** `ForceSeqNumReset` has exactly three values: `Always`, `OneTime`, `Never`. There is no `OnLogon`.
9. **Don't reference `SessionState.LoggedOn`.** It does not exist in 1.2.3. The fully-logged-on state is `SessionState.Connected` (verify with `SessionState.IsConnected(state)`).

## API surface

The full verified API surface lives in `references/api-reference.md`: namespaces, session construction (acceptor/initiator/listener), `FixMessage` read/write, `SessionState` and `ForceSeqNumReset` enums, useful `SessionParameters` properties, the "Other IFixSession methods worth knowing" table, and repeating-group iteration.

Load that reference before generating non-trivial FIX Antenna code. The signatures there were verified by reflection against 1.2.3.

### Critical pitfalls captured in `references/api-reference.md`

- **Single-arg `SendMessage(content)` is NOT interchangeable with two-arg `SendMessage(msgType, content)`** on a raw `new FixMessage()`. With no MsgType (35) it throws `ArgumentException: Message type(35 tag) cannot be null` at serialization; with 35 set but no header it serializes without 8/9/34/49/52/56 (header updates use `DontAddIfNotExists`) and crashes at the storage layer with `"Tag 34 is missing or has invalid value"`. Use the two-arg overload for fresh construction, or `PrepareMessage(...)`.
- **`IFixSession.Connect()` is polymorphic by role**: opens TCP + sends Logon on an initiator; *accepts* a pending inbound on an acceptor (inside `IFixServerListener.NewFixSession`).
- **`IFixSessionListener` inherits `IFixMessageListener`** — implement BOTH `OnSessionStateChange` and `OnNewMessage` on the same class.
- **`SessionState.LoggedOn` does not exist.** Use `SessionState.Connected` / `SessionState.IsConnected(state)`.
- **`ForceSeqNumReset` values: only `Always`, `OneTime`, `Never`.** No `OnLogon`.
- **CompID property casing: `SenderCompId` / `TargetCompId`** (lowercase `d`) — the properties-file key, in contrast, is `senderCompID` / `targetCompID` (uppercase).
- **`AddTag` appends; `Set` replaces-or-adds.** Two `AddTag(sameTag, ...)` calls = malformed outbound. `Set(tag, value)` updates in place and adds the tag if absent (every overload delegates to `UpdateValue(..., MissingTagHandling.AddIfNotExists)`), so `Set` *is* the "replace if present, add if missing" call — don't reach for `UpdateValue` just to get that behavior. Use `UpdateValue(tag, value, IndexedStorage.MissingTagHandling.DontAddIfNotExists)` only for update-only-if-present (returns `-1`, no-op when absent). The enum is nested in `IndexedStorage` (namespace `Epam.FixAntenna.NetCore.Message`).
- **Repeating groups on a fresh `FixMessage`** require `RawFixUtil.IndexRepeatingGroup(msg, FixVersion.Fix44, "<msgType>")` before `AddRepeatingGroup(leadingTag)`, otherwise it throws "no info about FIX version in message".

### Namespaces (quick)

```csharp
using Epam.FixAntenna.NetCore.Configuration;            // Config
using Epam.FixAntenna.NetCore.FixEngine;                // FixServer, IFixSession, IFixSessionListener,
                                                        // IFixMessageListener, IFixServerListener,
                                                        // SessionParameters, SessionState, ForceSeqNumReset
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;   // SessionParametersBuilder
using Epam.FixAntenna.NetCore.Message;                  // FixMessage, ITagList
using Epam.FixAntenna.Constants.Fix44;                  // Tags  ← FIX 4.4 tag numbers
// Other FIX versions: Epam.FixAntenna.Constants.{Fix40,Fix41,Fix42,Fix43,Fix44,Fix50,Fix50sp1,Fix50sp2,Fixt11,Fixt11ep}
```

## Configuration: `fixengine.properties`

> The keys documented in this section and in `references/12-perf-profiles.md` cover the common cases — use them as-is, no lookup needed. For any other key, do not invent it: the **complete 1.2.3 key index** (every recognized key, default, scope) is bundled in `references/config-keys.md`. The upstream [`Docs/Configuration.md`](https://github.com/epam/fix-antenna-net-core/blob/main/Docs/Configuration.md) is only needed for long-form prose on a key already found in the index.

The shipped `Epam.FixAntenna.NetCore` NuGet's `SessionParametersBuilder` reads a specific property-file layout (verified against `epam/fix-antenna-net-core/Samples/ConnectToGateway/fixengine.properties`):

- **List sessions** with the key `sessionIDs` (comma-separated).
- **Per-session properties** are prefixed `sessions.<id>.<key>`.
- **Shared defaults** can live under `sessions.default.<key>`.
- The session-type key is **`sessionType`** with values `initiator` | `acceptor` — NOT `connectionType`.

```properties
# Process-wide settings (no session prefix)
storageFactory   = Epam.FixAntenna.NetCore.FixEngine.Storage.FilesystemStorageFactory
storageDirectory = ./logs

# Autoreconnect is controlled SOLELY by autoreconnectAttempts.
# There is NO `enableAutoreconnect` property — agents commonly invent this
# (the engine ignores it silently). Values for autoreconnectAttempts:
#   -1 = no autoreconnect  (Config.NoAutoreconnect)
#    0 = infinite          (Config.InfinityAutoreconnect)
#    N = N attempts
autoreconnectAttempts  = 0
autoreconnectDelayInMs = 2000

# Session list
sessionIDs = session1

# Defaults shared across all sessions
sessions.default.fixVersion        = FIX.4.4              # FIX.4.0..FIX.5.0SP2 or FIXT.1.1+appVersion
sessions.default.heartbeatInterval = 30

# Per-session
sessions.session1.sessionType      = initiator             # initiator | acceptor
sessions.session1.senderCompID     = BUYSIDE
sessions.session1.targetCompID     = VENUE
sessions.session1.host             = fix.example.com       # initiator only
sessions.session1.port             = 9876
sessions.session1.forceSeqNumReset = Never                 # Always | OneTime | Never

# Optional Logon credentials (tags 553/554)
sessions.session1.username = TRADER
sessions.session1.password = pass
```

> ⚠️ **Silent failure trap.** The engine accepts unknown property keys with no warning. A typo (`sessions=` instead of `sessionIDs=`, or `connectionType` instead of `sessionType`) yields an empty session list at runtime, not a config error. Use the exact keys above.

> ⚠️ For an **acceptor**, `FixServer` reads the process-wide `port` at the top of the properties file (no per-session host/port required since sessions are created dynamically as counterparties connect). Verified in `Samples/EchoServer/fixengine.properties`:
> ```properties
> # default listening port
> port = 4000
> ```

> ⚠️ **CompID mirroring.** A FIX session pair must have inverse CompIDs — each side's `senderCompID` equals the other's `targetCompID`:
> ```
> Side A (initiator):  senderCompID = BUYSIDE,   targetCompID = VENUE
> Side B (acceptor):   senderCompID = VENUE,     targetCompID = BUYSIDE
> ```
> If the pairing doesn't mirror, Logon is refused — and the engine doesn't surface this as a config error. It surfaces as a refused connection or a session-level reject (35=3) at runtime. When writing a gateway against a counterparty whose config is fixed (e.g., the engine's shipped `Samples/EchoServer`), copy *their* CompIDs and swap them.

> ⚠️ The `Config` class exposes 141 string constants for property keys — see `Config.StorageFactory`, `Config.TradePeriodBegin`, `Config.SslCertificate`, etc. Use these in code for compiler-checked key names. **But not every recognised property key has a `Config.X` constant.** Common per-session keys parsed by literal string in `SessionParameters.cs` — `senderCompID`, `targetCompID`, `host`, `fixVersion`, `appVersion`, `heartbeatInterval`, `username`, `password`, `sessionType` (which lives on `SessionParametersBuilder.SessionTypeProp`, not `Config`) — have **no** corresponding `Config.X` constant in 1.2.3. Reaching for `Config.SenderCompID` or `Config.Username` from code is a compile error. Use the literal string for those, or write a small `Keys` static class in the project.

## Mini-glossary (terms agents confuse)

| Term | Meaning | Common mistake |
|---|---|---|
| **Admin message** | Session-layer: Logon (A), Logout (5), Heartbeat (0), TestRequest (1), ResendRequest (2), SequenceReset (4), Reject (3). | Treating admin messages as app messages. The engine handles them. |
| **Application message** | Business-layer: NewOrderSingle (D), ExecutionReport (8), MarketDataRequest (V), etc. | Generating admin messages from app code. |
| **Session-level reject** (3) | Peer couldn't parse/validate the outbound message. | Treating as a business reject. Means the local engine sent malformed FIX. |
| **Business reject** (j) | Outbound parsed; peer business logic refused it. | Same as above in reverse. |
| **Gap fill** | A `SequenceReset (4)` with `GapFillFlag (123) = Y`, used to fill admin-message slots during a resend. | Sending gap fill for app messages — replay them instead. |
| **Sequence reset** | A `SequenceReset (4)` with `GapFillFlag = N`. Force-resets the counterparty's expected next seq num. Operational. | Using as a routine recovery tool. |
| **Resend Request** (2) | Counterparty asking for retransmission of messages N..M. | Ignoring it. Engine handles it if storage is persistent. |
| **ExecType vs OrdStatus** | ExecType (150) = what THIS report IS (New, Trade, Canceled). OrdStatus (39) = current order state. | Confusing them; sending illegal transitions. |
| **PossDupFlag (43)** | Engine-set on replayed messages. | Ignoring in app-level dedupe. |
| **Logon ResetSeqNumFlag (141)** | Y = reset seq nums on this logon. Negotiated. | Setting Y always; should be rare. |

Full glossary in `references/glossary.md`.

## Pattern references

Each pattern is a complete, focused reference with its own file under `references/`. Load on demand.

| # | Reference | Use case |
|---|---|---|
| 01 | `references/01-minimal-acceptor.md` | Acceptor: receive `NewOrderSingle`, reply with `ExecutionReport`. |
| 02 | `references/02-minimal-initiator.md` | Initiator: connect, logon, heartbeat, graceful logout. |
| 03 | `references/03-order-entry-client.md` | Full order flow: New → Ack → PartialFill → Fill → Cancel/Replace → Cancel. |
| 04 | `references/04-drop-copy-consumer.md` | Read-only: consume `ExecutionReport` / `TradeCaptureReport`, normalize, persist. |
| 05 | `references/05-custom-dictionary.md` | Add custom tags / message types via dictionary. |
| 06 | `references/06-multi-session-router.md` | One process, many sessions, routing rules between them. |
| 07 | `references/07-persistence-recovery.md` | Resend, gap fill, sequence reset, restart recovery. |
| 08 | `references/08-custom-storage-replication.md` | Pluggable storage with async replication. |
| 09 | `references/09-tls-secure-session.md` | TLS, mutual auth, cert rotation, reconnect. |
| 10 | `references/10-fix-to-kafka-bridge.md` | FIX → normalized DTO → Kafka/MQ with backpressure. |
| 11 | `references/11-scheduled-sessions.md` | Time-windowed sessions, TZ handling, daily seq reset. |
| 12 | `references/12-perf-profiles.md` | Throughput vs latency tuning, CPU affinity, GC settings. |
| 13 | `references/13-admin-monitoring.md` | Remote session admin/monitoring via the AdminTool plugin (autostart admin sessions, XML-over-FIX commands). |

Plus `references/glossary.md` for FIX terminology, `references/config-keys.md` for the complete verified `fixengine.properties` key index, and `references/official-docs.md` for the maintainer-written upstream docs (indexed + cross-mapped to the patterns above).

## Error handling & threading — cross-cutting

1. **Never let exceptions escape `OnNewMessage`.** The engine logs the exception and **rethrows** it; it reaches the session's reader-thread catch-all, which shuts the session down (only `ArgumentException` is swallowed and logged). There is no redelivery — a message is consumed the moment `OnNewMessage` returns. Catch, log, dead-letter; persist application-level intent synchronously inside the callback (transactional outbox).
2. **`OnNewMessage` fires on the session's single dedicated reader thread.** Calls are serialized per session; blocking it stalls ALL inbound processing for that session, including the engine's admin handling (TestRequest replies).
3. **The inbound `FixMessage` instance is reused by the engine** (one parse buffer per session, cleared after every message). Handing it to a queue or another thread requires `Clone()` first — or extract scalars synchronously.
4. **Inside `OnSessionStateChange`**: don't throw. Log and react.
5. **Session-level rejects (3)**: log as `error`, alert. Indicates engine/config mismatch with counterparty. Note: inbound 35=3 only reaches app code via `AddInSessionLevelMessageListener`.
6. **Business rejects (j)** and **Order rejects** (`ExecType=8`): log as `warn`, surface to OMS/upstream.
7. **Disconnect with reason**: always pass a reason: `session.Disconnect("reason text")`. The reason ends up in the peer's Logout.

## When in doubt

- **Don't invent.** When unsure whether an API or config key exists, look it up instead of fabricating signatures. Lookup order:
  1. **Bundled references first** — `references/api-reference.md`, `references/config-keys.md` (every recognized config key), the pattern files, and this document. They are pre-verified against 1.2.3 and cost no web round-trip; they answer the overwhelming majority of questions.
  2. **The installed package** — decompile / inspect the actual `Epam.FixAntenna.NetCore` assembly in the project when the question is about an exact signature or constant.
  3. **Upstream GitHub docs last** — only when the topic is genuinely not covered locally. Use `references/official-docs.md` to pick the right page instead of browsing. Remember the upstream `main` branch documents 2.x, so for 1.2.3 the bundled references win on any disagreement.
- **Verify against the installed version.** This skill targets 1.2.3 publicly. If a project pulls a newer EPAM-internal build, some APIs above may have evolved (notably: in 2.x, `IFixSessionListener` itself carries `OnNewMessage`, eliminating the separate `IFixMessageListener` registration).


### Confirmed unresolvable in 1.2.3 (use the workaround; do not invent)

| Gap | Why it can't be filled | Documented workaround |
|---|---|---|
| **Custom `IStorageFactory` implementation** | `IStorageFactory` and `IMessageStorage` are `internal` in 1.2.3; customer assemblies cannot implement them. Verified by inspection of the interface declarations. | Use a shipped factory; replicate at OS / filesystem layer. See `references/08-custom-storage-replication.md`. |
| **Per-session client-cert fingerprint pinning** | No `Config.*` constant exists for "session X accepts only cert with thumbprint Y." TLS trust is at the CA level only (`sslCaCertificate`). | TLS-terminating sidecar (stunnel / Envoy / HAProxy) does the pinning; engine runs plaintext behind it. See `references/09-tls-secure-session.md`. |
| **Exchange-holiday calendar on scheduled sessions** | `tradePeriodBegin`/`End`/`TimeZone` accept Quartz cron only; no documented property for a date-list calendar. Verified absent in [`Docs/Scheduler.md`](https://github.com/epam/fix-antenna-net-core/blob/main/Docs/Scheduler.md). | App-level pre-logon check that consults a holiday calendar and skips `Connect()` on holidays. |
| **"Before-logon" engine callback / hook** | `IFixSession` exposes `Init` / `Connect` / `Disconnect`; no callback fires *before* the engine sends Logon (35=A). | Enforce pre-logon ordering in `Main()`: build the session object, call `Init()`, do whatever catch-up is needed against the engine's storage, THEN call `Connect()`. See `references/10-fix-to-kafka-bridge.md`. |
| **FIX-level encryption (tag 98)** | The `encryptionMode`/`encryptionConfig` config keys exist in `Config`, but 1.2.3 ships no cipher implementation — `GetLoginHeader` hardcodes `98=0` (NONE) into every Logon. | Use TLS for transport security. See `references/09-tls-secure-session.md`. |

### TLS 1.3 spelling — partially verified

`sslProtocol` accepts the standard .NET `SslProtocols` enum names. Per [`Docs/TlsSupport.md`](https://github.com/epam/fix-antenna-net-core/blob/main/Docs/TlsSupport.md), the **default is `None` (let .NET pick the best mutually supported protocol)**. `Tls12` is the value used in every shipped sample and is fully verified. `Tls13` is the natural spelling for the .NET enum but is not used in any shipped sample, so its parser handling has not been observed end-to-end. For TLS 1.3 only:

1. Set `sslProtocol = Tls13` and assume it works (this is the documented .NET parsing pattern).
2. **Verify by observing the negotiated protocol after handshake** — do not assume the engine refuses a 1.2 downgrade.

### Behavior when a true gap remains

For the four items in the **Confirmed unresolvable** table above, the expected agent behavior is:
1. Write surrounding code correctly.
2. Use the documented workaround.
3. Flag `// TODO: verify <X>` at the gap so a human can re-check against future engine releases.

A composition that returns with honest TODOs on the four unresolvable items is **better** than one that invents a property key to fill them.

## Additional resources

- `references/api-reference.md` — full verified API surface (session construction, listeners, message read/write, `SessionState` / `ForceSeqNumReset` enums, `SessionParameters`, other `IFixSession` methods, repeating-group iteration).
- `references/config-keys.md` — complete verified index of every `fixengine.properties` key recognized in 1.2.3 (name, default, scope, one-line meaning). Check here before fetching any upstream doc.
- `references/glossary.md` — FIX protocol terminology.
- `references/official-docs.md` — index of the official upstream `Docs/` pages with direct links, one-line summaries, and a reference→doc cross-map.
- `references/01-minimal-acceptor.md` through `references/13-admin-monitoring.md` — per-pattern canonical skeletons (see "Pattern references" above).

# API Reference — verified against 1.2.3

Every signature on this page was verified by reflection. See `../SKILL.md` for the high-level mental model and hard rules.

## Namespaces

```csharp
using Epam.FixAntenna.NetCore.Configuration;            // Config
using Epam.FixAntenna.NetCore.FixEngine;                // FixServer, IFixSession, IFixSessionListener,
                                                        // IFixMessageListener, IFixServerListener,
                                                        // SessionParameters, SessionState, ForceSeqNumReset,
                                                        // FixSessionSendingType, MessageStructure,
                                                        // PreparedMessageUtil, IErrorHandler, IRejectMessageListener
using Epam.FixAntenna.NetCore.FixEngine.Session;        // ITypedFixMessageListener, IFixSessionSlowConsumerListener,
                                                        // SlowConsumerReason
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;   // SessionParametersBuilder
using Epam.FixAntenna.NetCore.Common;                   // FixVersion
using Epam.FixAntenna.NetCore.Message;                  // FixMessage, ITagList, RawFixUtil
using Epam.FixAntenna.Constants.Fix44;                  // Tags  ← for FIX 4.4 tag numbers
// Other FIX versions: Epam.FixAntenna.Constants.{Fix40,Fix41,Fix42,Fix43,Fix44,Fix50,Fix50sp1,Fix50sp2,Fixt11,Fixt11ep}
//   (Fixt11ep = FIXT 1.1 extension pack — separate dialect from Fixt11)
```

## Loading configuration

```csharp
// Option A — explicit Config object from a properties file
var configuration = new Config("fixengine.properties");

// Option B — load session list directly (most common in samples).
// Use the Config.DefaultEngineProperties constant ("fixengine.properties")
// for a compiler-checked path, or pass any literal path.
var configured = SessionParametersBuilder.BuildSessionParametersList(Config.DefaultEngineProperties);

// Option C — filtered variants. Useful when one process runs BOTH an
// acceptor lane and an initiator lane (gateway / hub pattern, reference 06):
// take only the initiator-type sessions to create with CreateInitiatorSession();
// let FixServer auto-load the acceptor-type sessions via ConfigPath.
// Both variants live on SessionParametersBuilder alongside the unfiltered Build().
var initiators = SessionParametersBuilder.BuildInitiatorSessionParametersList(Config.DefaultEngineProperties);
var acceptors  = SessionParametersBuilder.BuildAcceptorSessionParametersList(Config.DefaultEngineProperties);
```

> ⚠️ `Config.LoadConfiguration` and `Config.LoadProperties` do **not** exist. Use the constructor or `SessionParametersBuilder`.

## Creating an acceptor (server)

```csharp
var config = new Config("fixengine.properties");
var server = new FixServer(config) { ConfigPath = "fixengine.properties" };
server.SetListener(new MyServerListener());        // implements IFixServerListener
server.Start();
// ... later
server.Stop();
```

Programmatic listen-port setup (instead of the `port` / `sslPort` properties), all **before** `Start()` — setting them while running only logs an error and changes nothing:

```csharp
server.Ports    = new[] { 9876, 9877 };   // plain ports
server.SslPorts = new[] { 9443 };         // SSL ports
server.SetPort(9876);                     // single-port shorthand (sets Ports)
```

`Start()` returns `bool` — `true` only if all configured listeners started (failures are logged as warnings). It throws `IOException` if no port could be bound at all, and `InvalidOperationException` when no port was configured.

Inside `NewFixSession(IFixSession session)`:

```csharp
public void NewFixSession(IFixSession session)
{
    var p = session.Parameters;
    // Credentials from the inbound Logon (tags 553/554) are exposed here:
    if (!Authenticate(p.IncomingUserName, p.IncomingPassword))
    {
        session.Disconnect("Invalid credentials");
        session.Dispose();
        return;
    }

    // One listener handles both state and messages.
    session.SetFixSessionListener(new MySessionListener(session));

    session.Connect();   // accepts the session
}
```

## Creating an initiator (client)

```csharp
var configured = SessionParametersBuilder.BuildSessionParametersList("fixengine.properties");
foreach (var p in configured.Values)
{
    var session = p.CreateInitiatorSession();          // also: CreateAcceptorSession, CreateNewFixSession
    session.SetFixSessionListener(new MySessionListener(session));
    session.Connect();
}
```

> ⚠️ **`IFixSession.Connect()` is polymorphic by session role.** On an initiator session, it opens the TCP connection and sends Logon. On an acceptor session (inside `IFixServerListener.NewFixSession`), it **accepts** the already-pending inbound connection. Same method, role-dependent semantics. To **reject** an inbound connection on the acceptor side, call `Reject(string reason)` instead of `Connect()` — see "Other IFixSession methods worth knowing" below.

> ⚠️ **There is no public session registry in 1.2.3.** `FixSessionManager` is `internal` (`FixEngine/Manager/FixSessionManager.cs`) — code like `FixSessionManager.Instance...` does not compile against the public API. Keep your own references to every `IFixSession` you create (initiator) or receive in `IFixServerListener.NewFixSession` (acceptor).

## The listener

`IFixSessionListener` inherits `IFixMessageListener`. One class, two methods:

```csharp
internal sealed class MySessionListener : IFixSessionListener
{
    private readonly IFixSession _session;
    public MySessionListener(IFixSession session) => _session = session;

    // Declared on IFixSessionListener.
    public void OnSessionStateChange(SessionState state)
    {
        if (SessionState.IsDisconnected(state))
            _session.Dispose();
    }

    // Inherited from IFixMessageListener.
    public void OnNewMessage(FixMessage message)
    {
        var msgType = message.GetTagValueAsString(Tags.MsgType);   // Fix44.Tags.MsgType = 35
        if (msgType == "D")
        {
            // dispatch NewOrderSingle to business layer — DO NOT do business logic here
        }
    }
}
```

> ⚠️ **The primary `SetFixSessionListener(...)` receives application messages only.** In `CompositeMessageHandler.OnNewMessage`, any message where `RawFixUtil.IsSessionLevelMessage` is true — 35=A (Logon), 0 (Heartbeat), 1 (Test Request), 2 (Resend Request), 3 (Reject), 4 (Sequence Reset), 5 (Logout) — is routed to the engine's internal handlers and is **not** passed to the user listener. `AddInSessionLevelMessageListener(IFixMessageListener)` is the only app-level way to observe inbound admin traffic (e.g. log Logons, watch 35=3 rejects); `AddOutSessionLevelMessageListener(ITypedFixMessageListener)` is the outbound counterpart.

## Reading a message

```csharp
public void OnNewMessage(FixMessage m)
{
    if (m.GetTagValueAsString(Tags.MsgType) != "D") return;

    var clOrdId  = m.GetTagValueAsString(Tags.ClOrdID);   // Fix44.Tags.ClOrdID = 11
    var symbol   = m.GetTagValueAsString(Tags.Symbol);    // 55
    var side     = m.GetTagValueAsString(Tags.Side);      // 54
    var qty      = m.GetTagValueAsLong(Tags.OrderQty);    // 38
    // ...
}
```

To parse raw FIX *outside* the receive path (log files, captures, test fixtures), use `RawFixUtil.GetFixMessage(string)` / `RawFixUtil.GetFixMessage(byte[])` (`Epam.FixAntenna.NetCore.Message`). Throws `GarbledMessageException` if the input is garbled.

## Writing a message

```csharp
var er = new FixMessage();
// Do NOT set Tags.MsgType here — it's passed to SendMessage below and
// would otherwise be duplicated by the header-writing path.
er.AddTag(Tags.ClOrdID,   clOrdId);
er.AddTag(Tags.OrderID,   orderId);
er.AddTag(Tags.ExecID,    execId);
er.AddTag(Tags.ExecType,  "0");                    // New
er.AddTag(Tags.OrdStatus, "0");                    // New
er.AddTag(Tags.Symbol,    symbol);
er.AddTag(Tags.Side,      side);
er.AddTag(Tags.OrderQty,  qty);
er.AddTag(Tags.LeavesQty, qty);
er.AddTag(Tags.CumQty,    0L);
er.AddTag(Tags.AvgPx,     0.0, precision: 4);
// Two-arg overload — engine writes BeginString/BodyLength/MsgType/MsgSeqNum/
// SenderCompID/TargetCompID/SendingTime/CheckSum from scratch.
session.SendMessage("8", er);
```

Every `SendMessage` / `SendAsIs` / `SendWithChanges` overload returns `bool`: `true` = sent synchronously from the calling thread, `false` = queued for later sending. Each also has an overload taking `FixSessionSendingType` — a `[Flags]` enum in `Epam.FixAntenna.NetCore.FixEngine`: `SendAsync = 1` (enqueue instead of sending from the calling thread), `SendSync = 2` (send synchronously), `DefaultSendingOption = SendSync`.

> ⚠️ **Single-arg vs two-arg `SendMessage` is not interchangeable.** `SendMessage(content)` calls into `AbstractFixSession.SendMessage(DontRedefineType="", message, ...)` → `StandardMessageFactory.Serialize` with an empty `msgType` → `SerializeWithUpdatedHeaderAndTrailer`, which uses `IndexedStorage.MissingTagHandling.DontAddIfNotExists` for every header tag (including `MsgSeqNum`). On a `FixMessage` built via `new FixMessage()` + `AddTag` (MsgType set, but no BeginString/SenderCompID/TargetCompID/MsgSeqNum/SendingTime on the message) those header tags are silently skipped during serialization. The storage layer (e.g., `IndexedMessageStorage`, `MmfIndexedMessageStorage`, `SlicedIndexedMessageStorage`) then reads the resulting bytes via `RawFixUtil.GetSequenceNumber` (line ~338) and throws `ArgumentException("Tag 34 is missing or has invalid value")` from `RawFixUtil.GetLongValue`. (Note: if MsgType is also missing, the earlier check at `StandardMessageFactory.Serialize` throws `"Message type(35 tag) cannot be null"` first.) Use the two-arg `SendMessage(msgType, content)` overload for fresh outbound construction — it routes to `SerializeWithAddedHeaderAndTrailer` which writes the full header from scratch. Alternatively, build via `IFixSession.PrepareMessage(...)`, which seeds placeholder header tags so the in-place update path can actually update them. Single-arg `SendMessage` is the correct call **only** when the `FixMessage` already has a full header — i.e., the inbound message in `OnNewMessage` being forwarded or echoed (the engine populates the header on parse).

> ⚠️ `AddTag` *appends* — two `AddTag(Tags.OrdStatus, …)` calls on the same message yield two `39=…` tags and a malformed outbound. To overwrite a tag, use `Set(int, value)` (inherited from `ExtendedIndexedStorage`): every `Set` overload delegates to `UpdateValue(int, value, IndexedStorage.MissingTagHandling.AddIfNotExists)`, so `Set` overwrites the tag when present and adds it when absent — it does **not** require the tag to pre-exist. Call `UpdateValue(int, value, IndexedStorage.MissingTagHandling.DontAddIfNotExists)` directly only for update-only-if-present semantics (it returns `-1` and changes nothing when the tag is absent). The enum is nested in `IndexedStorage`, namespace `Epam.FixAntenna.NetCore.Message`. For fresh outbound construction shown above, `AddTag` is correct.

## SessionState values (1.2.3)

```
WaitingForLogon              ← inbound Logon expected but not yet received
LogonReceived                ← Logon received, validating
Connecting                   ← initiator: TCP being established
Connected                    ← fully logged on, business traffic flowing
WaitingForLogoff             ← Logout sent, awaiting peer
WaitingForForcedLogoff
WaitingForForcedDisconnect
Reconnecting                 ← engine attempting auto-reconnect
Disconnected                 ← clean disconnect
DisconnectedAbnormally       ← unclean / error path
Dead                         ← terminal; resource cleanup
```

There is **no `LoggedOn`**. The state most callers want is `Connected`.

Test with: `SessionState.IsConnected(state)`, `SessionState.IsDisconnected(state)`, `SessionState.IsDisposed(state)`, `SessionState.IsNotDisconnected(state)`.

## ForceSeqNumReset values

```
Always       ← send 141=Y on every Logon (rare; defeats restart recovery)
OneTime      ← send 141=Y on next Logon only, then revert to Never
Never        ← never set 141=Y from this side (default; production norm)
```

There is no `OnLogon`. For "reset at start of trading day," use the engine's scheduled reset: set `performResetSeqNumTime = true` (boolean toggle) and the time in `resetSequenceTime` (`HH:MM:SS`) / `resetSequenceTimeZone` — see `11-scheduled-sessions.md`.

## IFixSession sequence number API (exists — do NOT use from app code)

These exist on `IFixSession`:
- `long InSeqNum { get; set; }`
- `long OutSeqNum { get; set; }`
- `void SetSequenceNumbers(long inSeqNum, long outSeqNum)`
- `void ResetSequenceNumbers()` / `void ResetSequenceNumbers(bool checkGapFillBefore)`

**Settable, yet do not touch them from regular app code.** They exist for operational tooling (manual recovery, sequence reconciliation utilities), not for "simulate a gap" code. Reaching for these is almost always wrong.

## Useful `SessionParameters` properties

Verified on `FixAntenna/NetCore/FixEngine/SessionParameters.cs`. Read these from `IFixSession.Parameters` inside the listener, or from the `SessionParameters` value yielded by `SessionParametersBuilder.*`.

| Property | Type | Use |
|---|---|---|
| `SenderCompId` / `TargetCompId` | `string` | CompIDs for this session. **Note casing: `Id`, not `ID`.** The properties-file key is `senderCompID`/`targetCompID` (uppercase), but the C# property is `Id`. |
| `Host` | `string` | Remote host (initiator) or bind host (acceptor). |
| `Port` | `int?` | Remote port (initiator) or per-session port (acceptor — usually unset; use process-wide `port`). **Nullable setter, but the getter returns `0` when unset** (`_port ?? 0`) — test absence with `HasPort`, not `null`. There is no engine default port; an acceptor without a per-session port uses the process-wide `port` key read by `FixServer`. |
| `SessionId` | `SessionId` | Composite identifier (sender + target). `SessionId.ToString()` is the natural log key. |
| `UserName` / `Password` | `string` | Outbound Logon credentials (tags 553/554) — what THIS side sends. |
| `IncomingUserName` / `IncomingPassword` | `string` | Credentials presented in the *inbound* Logon. Validate inside `IFixServerListener.NewFixSession`. |
| `SessionQualifier` | `string` | Distinguishes multiple sessions with the same CompID pair (e.g. order-entry + drop-copy to the same counterparty). Setting it also writes the qualifier into the outbound Logon at the tag configured by `logonMessageSessionQualifierTag` (default `9012`); set `suppressSessionQualifierTagInLogonMessage = true` (default `false`) to keep it out of the Logon. |
| `AddOutgoingLoginField(int tag, ...)` | method | The sanctioned way to add custom tags to the outbound Logon without violating "engine owns admin messages". Overloads for `string`, `byte[]`, `byte[] + offset/length`, `long`, `double + precision`, `DateTimeOffset + FixDateType`. |
| `OutgoingLoginMessage` / `IncomingLoginMessage` | `FixMessage` | Tags this side adds to its Logon / the full set of fields the peer presented in its Logon (acceptor side). Inspect the peer's Logon here. |

> ⚠️ **CompID property naming.** `SenderCompId` and `TargetCompId` use **`Id`** (lowercase d). Writing `p.SenderCompID` (uppercase) is a compile error. The properties-file key, on the other hand, *is* uppercase: `senderCompID = X`.

### Code-first `SessionParameters` (no properties file)

`SessionParameters` has a public parameterless constructor (clones `Config.GlobalConfiguration`) and a `SessionParameters(Config)` overload. All connection settings are settable, so an initiator can be built entirely in code:

```csharp
var p = new SessionParameters
{
    FixVersion = FixVersion.Fix44,            // defaults to Fix42 when unset
    Host = "fix.example.com",
    Port = 9876,
    SenderCompId = "A",
    TargetCompId = "B",
    HeartbeatInterval = 30,                   // property default is 30
    ForceSeqNumReset = ForceSeqNumReset.OneTime,
};
p.AddOutgoingLoginField(8013, "X");           // custom tag on the outbound Logon
p.AddDestination("backup.example.com", 9876); // backup host — appends to Destinations
var session = p.CreateInitiatorSession();     // also: CreateAcceptorSession()
```

`Destinations` (`IList<DnsEndPoint>`, read-only property) holds the alternative/backup destinations an initiator fails over to; manage it via `AddDestination(string, int)` / `AddDestination(DnsEndPoint)` / `AddAllDestinations(...)` / `RemoveDestination(...)`.

## Other IFixSession methods worth knowing

All verified on `FixAntenna/NetCore/FixEngine/IFixSession.cs`. The minimal skeleton uses `SetFixSessionListener` + `Connect` + `SendMessage(FixMessage)` + `Disconnect` + `Dispose`. Reach for the rest when the use case calls for them.

| Member | When to use |
|---|---|
| `SessionState SessionState { get; set; }` | Read the current state on demand, outside `OnSessionStateChange` (e.g. health checks). Has a setter, but state transitions are engine business — read it, don't write it. |
| `PrepareMessage(...)` (3 overloads) and `PrepareMessageFromString(byte[], ...)` (2 overloads) | Build a `FixMessage` pre-filled with the session's header (`PrepareMessage`) or parse + pre-fill from raw bytes (`PrepareMessageFromString`). Alternative to `new FixMessage() + AddTag(Tags.MsgType, …)`. Every overload requires a `MessageStructure` — see below. |
| `SendMessage(string msgType, FixMessage content)` (+ `FixSessionSendingType` overload) | Send and set MsgType implicitly. Skips one `AddTag(Tags.MsgType, …)` line. |
| `SendAsIs(FixMessage)` / `SendWithChanges(FixMessage, ChangesType?)` | Send without engine header rewrite, or with a controlled rewrite. Use for replay / drop-copy / proxy patterns where header rewrites would corrupt provenance. |
| `Reject(string reason)` | **Acceptor-only.** Reject an incoming connection inside `IFixServerListener.NewFixSession` before calling `Connect()`. Semantically distinct from `Disconnect(reason)` (which logoffs an already-connected session). |
| `Init()` *(optional)* | Initialise the session and allow messages to be enqueued *before* `Connect()` — they are sent once logon completes. **Most apps don't need this** — `Connect()` alone is sufficient. Reach for `Init()` only with a specific need to enqueue outbound messages before the session is connected. |
| `ConnectAsync()` / `DisconnectAsync(reason)` | Async lifecycle variants. |
| `OutgoingQueueSize` (getter), `GetOutgoingQueueMessages()` | First-class backpressure signals. Watch these in long-running senders; growing queue = slow consumer or stalled session. |
| `ErrorHandler` / `RejectMessageListener` / `SlowConsumerListener` | Hook points for engine-level errors, messages dropped from the outgoing queue, and slow-consumer detection. Wire these in production. Signatures below. |
| `AddInSessionLevelMessageListener(IFixMessageListener)` / `AddOutSessionLevelMessageListener(ITypedFixMessageListener)` | Observers on inbound / outbound session-layer (admin) traffic — the **only** app-level way to see 35=A,0,1,2,3,4,5, since the primary `SetFixSessionListener(...)` receives **application messages only** (see "The listener" above). |
| `RetrieveSentMessage(long seqNumber)` / `RetrieveReceivedMessage(long seqNumber)` | Fetch a stored message by sequence number as `byte[]` (throws `IOException` on error). See the indexing caveat below. |
| `MessageValidator` (getter) | The session's `IMessageValidator`, for ad-hoc validation outside the receive path. |

> ⚠️ **`RetrieveReceivedMessage` does not work with default config.** Retrieval from incoming storage requires `incomingStorageIndexed = true` (default `false`). With the default, incoming storage is a `FlatFileMessageStorage`, whose retrieval path throws `IOException("Message retrieval is not possible for flat files!")`. Outgoing storage *is* indexed by default (`outgoingStorageIndexed` defaults to `true`), so `RetrieveSentMessage` works out of the box.

Hook-interface signatures:

```csharp
// Epam.FixAntenna.NetCore.FixEngine
public interface IErrorHandler
{
    void OnWarn(string description, Exception throwable);
    void OnError(string description, Exception throwable);
    void OnFatalError(string description, Exception throwable);
}
public interface IRejectMessageListener     // message removed from the SENDING queue
{                                           // (undeliverable), NOT inbound 35=3 rejects
    void OnRejectMessage(FixMessage message);
}

// Epam.FixAntenna.NetCore.FixEngine.Session
public interface ITypedFixMessageListener
{
    void OnMessage(string msgType, FixMessage message);
}
public interface IFixSessionSlowConsumerListener
{
    void OnSlowConsumerDetected(SlowConsumerReason reason, long expected, long current);
}
```

## PrepareMessage and MessageStructure

`MessageStructure` (`Epam.FixAntenna.NetCore.FixEngine`, sealed) declares the body layout of a prepared message: which tags, in order, and how many bytes each value reserves. `Reserve(int tagId, int length)` reserves raw bytes; `ReserveString(int tagId[, int length])` and `ReserveLong(int tagId, int length)` are typed variants; `MessageStructure.VariableLength` (= `-1`) marks a value whose size may grow at runtime (slower, allocates).

```csharp
var structure = new MessageStructure();
structure.ReserveString(Tags.ClOrdID, 12);                        // fixed 12-byte value
structure.ReserveLong(Tags.OrderQty, 8);
structure.ReserveString(Tags.Symbol, MessageStructure.VariableLength);
var order = session.PrepareMessage("D", structure);               // header pre-filled
```

To build prepared messages *without* a session, `PreparedMessageUtil` is public: `new PreparedMessageUtil(sessionParameters)` then `PrepareMessage(string|byte[]|FixMessage, MessageStructure[, bool fromPool])`.

## Repeating groups — verified API (1.2.3)

Iterating a repeating group inside a `FixMessage`:

```csharp
const int NoMDEntries = 268;
const int MDEntryType = 269;
const int MDEntryPx   = 270;

if (msg.IsRepeatingGroupExists(NoMDEntries))
{
    var group = msg.GetRepeatingGroup(NoMDEntries);
    for (var i = 0; i < group.Count; i++)
    {
        var entry = group.GetEntry(i);
        var type  = entry.GetTagValueAsString(MDEntryType);
        var price = entry.GetTagValueAsDouble(MDEntryPx);
        // ...
    }
}
```

Key facts (verified against `Docs/RepeatingGroupApi.md` and `ITagList`):
- `RepeatingGroup.Entry` implements `ITagList`, so the same `GetTagValueAsString` / `GetTagValueAsLong` / `IsTagExists` used on `FixMessage` work on entries.
- `IsRepeatingGroupExists(leadingTag)` is the existence check.
- `GetRepeatingGroup(leadingTag)` returns the group; iterate `0..Count-1` with `GetEntry(i)`.
- For an N-th occurrence of a flat tag (rare, mostly admin), use `msg.GetTag(tagId, occurrence)` on `FixMessageAdapter`.
- `AddRepeatingGroup(leadingTag)` on outbound construction returns a group to populate with entries.
- A bare `new FixMessage()` has no FIX version attached, so `AddRepeatingGroup` throws `ArgumentException: "There is no info about FIX version in message. Please use method indexRepeatingGroup(FixMessage, FixVersion, MessageType"`. Call `RawFixUtil.IndexRepeatingGroup(msg, FixVersion.Fix44, "<msgType>")` before `AddRepeatingGroup` — it takes msgType explicitly, so `Tags.MsgType` in the body is not needed (and shouldn't be set if `SendMessage(msgType, msg)` is the call, since that writes its own 35=). Inbound parsing is fine — engine-decoded messages already carry the version.

Do NOT iterate a message as a flat tag list when walking a group — the indices won't be stable across the group boundary.

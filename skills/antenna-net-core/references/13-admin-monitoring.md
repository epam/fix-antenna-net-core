# 13 Admin & Monitoring (AdminTool)

> Targets **Epam.FixAntenna.NetCore 1.2.3**. See `../SKILL.md` for root API rules.

## Pattern

The engine ships a remote-administration plugin (**AdminTool**, a.k.a. RAI — Remote Admin Interface). An *autostart acceptor session* with a reserved TargetCompID is routed to the `AdminTool` listener instead of your application. Commands and results travel as XML inside FIX `XmlData (n)` messages: tag `212 XmlDataLen` + tag `213 XmlData` (verified in `AdminConstants`: `XmlDataLenTag = 212`, `XmlDataTag = 213`, `MessageType = "n"`).

```
[admin client] ──Logon 56=admin, 553/554=login/password──► [FixServer]
                                                              │ TargetCompID ∈ autostart.acceptor.targetIds?
                                                              ▼
                                              [AdminTool listener, not your IFixServerListener]
[admin client] ──35=n, 212=len, 213=<SessionsList .../>──► AdminTool
[admin client] ◄─35=n, 212=len, 213=<Response ResultCode="0" .../>──┘
```

The AdminTool's outgoing Logon carries two custom tags: `10003` (component time zone, e.g. `+03:00`) and `10004` (admin protocol version).

## When to use

- Remote monitoring of sessions (list, status, params, traffic statistics) — e.g. from FIXICC or your own ops tool.
- Remote intervention: change/reset sequence numbers, send TestRequest/Heartbeat/raw messages, create or delete sessions, switch a session to backup/primary connection.

## When NOT to use

- In-process monitoring — read `IFixSession.Parameters` / session state directly.
- Business message flow — this channel is for engine administration only.

## Packaging (verified)

| Piece | Where it ships |
|---|---|
| `AdminTool` listener + command classes | **Separate NuGet package `Epam.FixAntenna.AdminTool`** ("Epam.FixAntenna.NetCore administrative add-on"), assembly `Epam.FixAntenna.AdminTool`. NOT part of `Epam.FixAntenna.NetCore`. |
| Admin XML message classes (`SessionsList`, `Response`, `MessageUtils`, …) | Namespace `Epam.FixAntenna.Fixicc.Message`, generated from `message.xsd` into the **same** `Epam.FixAntenna.AdminTool` assembly. |
| Autostart-session plumbing (`autostart.acceptor.*` keys) | Built into `Epam.FixAntenna.NetCore` (`FixEngine.Acceptor.Autostart.AutostartAcceptorSessions`). |
| `Epam.FixAntenna.Constants.Fixt11.Tags` (`XmlData`, `XmlDataLen`) | `Epam.FixAntenna.Dialects` package (transitive dependency of NetCore). |

Server process must reference `Epam.FixAntenna.AdminTool` — the listener is loaded by `Type.GetType` from the properties file; if the assembly is missing, the lookup yields `null` and the admin logon fails with `InvalidOperationException("Cannot instantiate FIXServerListener class…")`.

## Server side — properties (verified key names)

From `Samples/EchoServer/fixengine.properties` (1.2.3):

```properties
# FIXICC admin protocol session
autostart.acceptor.targetIds=admin
autostart.acceptor.admin.login=admin
autostart.acceptor.admin.password=admin
autostart.acceptor.admin.ip=*
autostart.acceptor.admin.fixServerListener=Epam.FixAntenna.AdminTool.AdminTool,Epam.FixAntenna.AdminTool
autostart.acceptor.admin.storageType=Persistent
```

Key semantics (all verified in `AutostartAcceptorSessions.cs`):

| Key | Meaning |
|---|---|
| `autostart.acceptor.targetIds` | Comma/space/semicolon-separated list of reserved CompIDs. A client whose Logon has `TargetCompID (56)` equal to one of these is routed to the configured listener. Each ID gets its own `autostart.acceptor.<ID>.*` block. |
| `autostart.acceptor.<ID>.login` | Expected `Username (553)` in the incoming Logon. Default `admin`. |
| `autostart.acceptor.<ID>.password` | Expected `Password (554)` in the incoming Logon. Default `admin`. Plaintext. |
| `autostart.acceptor.<ID>.ip` | Allowed source IP mask(s), parsed by `SubnetUtils.ParseIpMasks`; `*` = any host. Default `*`. Invalid mask throws `ArgumentException` at engine start; non-matching source IP rejects the logon (warning logged). |
| `autostart.acceptor.<ID>.fixServerListener` | Assembly-qualified type name (`Namespace.Class,Assembly`) of the `IFixServerListener` to instantiate. For the stock admin tool: `Epam.FixAntenna.AdminTool.AdminTool,Epam.FixAntenna.AdminTool`. |
| `autostart.acceptor.<ID>.storageType` | `Transient` (default, in-memory storage) or `Persistent` (file storage) for the admin session itself. |
| `autostart.acceptor.commands.package` | (`Config.AutostartAcceptorCommandPackage`) root namespace for custom `Command` subclasses to extend the command set. ⚠️ The 1.2.3 `Docs/MonitoringAndAdministration.md` misspells this as `autostart.acceptor.command.package` — the constant in `Config.cs` is `autostart.acceptor.commands.package`. |

There is no separate admin port — the admin client connects to the server's normal listen `port`. No `FixServer` code changes are needed; the autostart block alone activates the feature.

## Client side — minimal skeleton (verified against `Samples/SimpleAdminClient`, 1.2.3)

Requires packages `Epam.FixAntenna.NetCore` **and** `Epam.FixAntenna.AdminTool` (for `Epam.FixAntenna.Fixicc.Message`).

```csharp
using System;
using System.Text;
using System.Threading;
using Epam.FixAntenna.Fixicc.Message;          // SessionsList, Response, MessageUtils (AdminTool pkg)
using Epam.FixAntenna.NetCore.Common;          // FixVersion
using Epam.FixAntenna.NetCore.FixEngine;       // SessionParameters, IFixSession, ChangesType
using Epam.FixAntenna.NetCore.Message;         // FixMessage
using Tags = Epam.FixAntenna.Constants.Fixt11.Tags;

public static class AdminClient
{
    public static void Main()
    {
        var details = new SessionParameters
        {
            FixVersion = FixVersion.Fix44,
            Host = "127.0.0.1",
            Port = 3000,                 // the server's normal FIX port
            HeartbeatInterval = 30,
            SenderCompId = "sender",
            TargetCompId = "admin",      // must be one of autostart.acceptor.targetIds
            UserName = "admin",          // -> tag 553, checked against ...admin.login
            Password = "admin"           // -> tag 554, checked against ...admin.password
        };

        var session = details.CreateNewFixSession();
        session.SetFixSessionListener(new AdminResponseListener(session));
        session.Connect();

        // Build a SessionsList request (SubscriptionRequestType: 0=snapshot,
        // 1=subscribe+snapshot, 2=unsubscribe, 3=subscribe — verified in SessionsList.cs)
        var request = new SessionsList { RequestID = 1L, SubscriptionRequestType = 0 };
        var xml = MessageUtils.ToXml(request);

        var message = new FixMessage();
        message.AddTag(Tags.MsgType, Encoding.UTF8.GetBytes("n"));
        message.AddTag(Tags.XmlDataLen, xml.Length);   // 212
        message.AddTag(Tags.XmlData, xml);             // 213
        session.SendWithChanges(message, ChangesType.AddSmhAndSmt); // engine adds header/trailer

        Thread.Sleep(3000);
        session.Disconnect("close app");
    }
}

internal sealed class AdminResponseListener : IFixSessionListener
{
    private readonly IFixSession _session;
    public AdminResponseListener(IFixSession session) => _session = session;

    public void OnSessionStateChange(SessionState state)
    {
        if (SessionState.IsDisconnected(state)) _session.Dispose();
    }

    public void OnNewMessage(FixMessage message)
    {
        if (message.GetTagValueAsString(Tags.MsgType) != "n") return;
        var response = (Response)MessageUtils.FromXml(message.GetTagValueAsString(Tags.XmlData));
        // Response.ResultCode is the generated enum: Item0..Item9 map to XML values 0..9
        Console.WriteLine($"ResultCode={response.ResultCode} Description={response.Description}");
        // payload, when present: response.SessionsListData, .SessionParamsData, .SessionStatusData, ...
    }
}
```

## Key API (1.2.3 verified)

| Type | Purpose |
|---|---|
| `Epam.FixAntenna.AdminTool.AdminTool` | The stock admin listener (`internal`, instantiated by the engine via reflection — you never `new` it; reference it only by its FQN string in properties). |
| `Epam.FixAntenna.Fixicc.Message.MessageUtils` | `ToXml(IMessage)` / `FromXml(string)` — serialize admin requests, parse responses. |
| `Epam.FixAntenna.Fixicc.Message.*` | One generated class per command (`SessionsList`, `ChangeSeqNum`, `Delete`, …) plus `Response` with per-command `*Data` payload properties. |
| `Epam.FixAntenna.Constants.Fixt11.Tags` | `Tags.XmlDataLen` = 212, `Tags.XmlData` = 213, `Tags.MsgType` = 35. |
| `IFixSession.SendWithChanges(FixMessage, ChangesType.AddSmhAndSmt)` | Send a hand-built `35=n` message; engine completes header/trailer. |
| `Config.AutostartAcceptorCommandPackage` | `"autostart.acceptor.commands.package"` — namespace for custom commands extending `Command`. |

## Command reference (all 23 commands registered in `CommandHandler`, 1.2.3)

Session-addressing commands take `SenderCompID` + `TargetCompID` (**from the server's perspective**) and optional `SessionQualifier`. Most commands require `RequestID`; the `Response` echoes it.

| Command | What it does | Key request fields → response payload |
|---|---|---|
| **Monitoring** | | |
| `SessionsList` | Snapshot and/or subscription to the session list (subscription pushes add/remove updates). | `SubscriptionRequestType` 0/1/2/3 → `SessionsListData` (per session: CompIDs, `Status`, `StatusGroup`, `Timestamp`, `Action`) |
| `SessionsSnapshot` | Detailed info for all sessions (active + preconfigured). | `View` = `STATUS` \| `STATUS_PARAMS` \| `STATUS_PARAMS_STAT`, optional per-session `SessionView` → `SessionsSnapshotData` |
| `SessionParams` | Full parameters of one session. | CompIDs → `SessionParamsData` (`Version`, `Role`, `RemoteHost`, `RemotePort`, `ExtraSessionParams`…) |
| `SessionStatus` | Status of one session. | CompIDs → `SessionStatusData` (`Status`, `StatusGroup`, `BackupState`, `InSeqNum`, `OutSeqNum`) |
| **Statistics** | | |
| `SessionStat` | Per-session traffic stats. | CompIDs → `SessionStatData` (bytes/messages in/out, established/terminated/last-message timestamps) |
| `GeneralSessionsStat` | Engine-wide stats. | – → `GeneralSessionsStatData` (active/reconnecting/awaiting/terminated counts, processed messages, lifetimes) |
| `ReceivedStat` / `SentStat` / `ProceedStat` | Engine-wide message counters. | – → `ReceivedStatData` / `SentStatData` / `ProceedStatData` |
| **Administrative** | | |
| `CreateInitiator` | Creates **and connects** a new initiator session. | CompIDs, `Version`, `RemoteHost`, `RemotePort`, optional `ExtraSessionParams` (HBI, StorageType, ForceSeqNumReset, …) |
| `CreateAcceptor` | Registers a new preconfigured acceptor session (`ConfiguredSessionRegister.RegisterSession`). | CompIDs, `Version`, optional `ExtraSessionParams` |
| `Delete` | Disconnects/disposes an active session and/or unregisters a configured one. | CompIDs, `SendLogout` (bool), `LogoutReason` |
| `DeleteAll` | `Delete` for every session. | `SendLogout`, `LogoutReason` |
| `StopSession` | Disconnects and disposes an active session (does not touch configured registrations). | CompIDs, `SendLogout`, `LogoutReason` |
| `ChangeSeqNum` | Sets sequence numbers. | CompIDs, `InSeqNum`, `OutSeqNum` |
| `ResetSeqNum` | Active session: `IFixSession.ResetSequenceNumbers()`; configured-only session: in/out set to 1. | CompIDs |
| `ToBackup` | Switches an initiator to its backup destination (`IBackupFixSession.SwitchToBackUp()`). Rejected for non-backup sessions. | CompIDs |
| `ToPrimary` | Switches back to the primary destination. | CompIDs |
| **Generic** | | |
| `TestRequest` | Sends `35=1` to the target session. | CompIDs, `TestReqID` |
| `Heartbeat` | Sends `35=0` to the target session. | CompIDs |
| `SendMessage` | Sends a raw FIX message on the target session. SOH must be encoded as `&#01;`. | CompIDs, `Message` |
| `GetFIXProtocolsList` | Lists FIX versions the engine supports. | `RequestID` → `FIXProtocolsListData/SupportedProtocol` |
| `Help` | Lists supported request shapes. | – → `HelpData` (`FIXAdminProtocolVersion`, `SupportedRequest`…) |

Unknown command name → `Response` with `ResultCode=1` and a description listing all supported commands (`DefaultCommand`).

## Response result codes (verified in `ResultCode.cs`)

| Code | Name | Meaning |
|---|---|---|
| 0 | `OPERATION_SUCCESS` | Request processed successfully. |
| 1 | `OPERATION_NOT_IMPLEMENTED` | Command not supported / unknown element. |
| 3 | `RESULT_UNKNOWN_SESSION` | No session with the given SenderCompID/TargetCompID. |
| 6 | `OPERATION_UNKNOWN_ERROR` | Unexpected error during processing. |
| 7 | `OPERATION_REJECT` | Request rejected (e.g. not a `35=n` message, empty `XmlData`, session already on backup). |
| 9 | `OPERATION_INVALID_ARGUMENT` | Missing/invalid parameter (e.g. no `SenderCompID`, bad `SubscriptionRequestType`). |

## Common LLM mistakes

1. **Restrictive `ip` mask locking out the admin client.** `autostart.acceptor.<ID>.ip` is checked against the source address on every admin logon; a non-matching mask silently rejects the logon (warning in server log only). Use `*` while testing, then narrow.
2. **Forgetting tags 553/554 on the client.** Set `SessionParameters.UserName` / `Password` — the server compares them to `<ID>.login` / `<ID>.password`. Without them the admin logon is rejected.
3. **Shipping default `admin`/`admin` credentials and plaintext passwords.** The properties file holds the password in cleartext, the protocol sends it in Logon. Restrict `ip`, use unique credentials, consider TLS (`09-tls-secure-session.md`) — the admin session goes over the same listener, so the SSL port works for it too.
4. **Treating the admin session as special infrastructure.** It is a normal acceptor session on the normal `port`: it shows up in `SessionsList`, counts in engine statistics, and uses storage (`<ID>.storageType`, default `Transient`).
5. **Wrong listener FQN format.** The value is loaded with `Type.GetType`, so it must be assembly-qualified: `Epam.FixAntenna.AdminTool.AdminTool,Epam.FixAntenna.AdminTool`. Namespace-only (no `,Assembly`) fails — and so does forgetting to reference the `Epam.FixAntenna.AdminTool` package on the server.
6. **Addressing sessions from the client's perspective.** `SenderCompID`/`TargetCompID` inside commands are the session id as the *engine* sees it (its own CompID first). Run `SessionsList` first and copy the ids from the response.
7. **Hand-rolling the XML or the FIX wrapper.** Use the generated `Epam.FixAntenna.Fixicc.Message` classes + `MessageUtils.ToXml`, and send with `SendWithChanges(msg, ChangesType.AddSmhAndSmt)`. If you build `213 XmlData` by hand, `212 XmlDataLen` must equal the XML length, and any SOH inside `SendMessage` payloads must be written as `&#01;`.
8. **Using the doc's `autostart.acceptor.command.package` key.** The verified constant is `autostart.acceptor.commands.package` (`Config.AutostartAcceptorCommandPackage`). Custom commands must derive from the AdminTool `Command` class and live in that namespace.
9. **Omitting `RequestID`.** Several commands (`CreateInitiator`, `CreateAcceptor`, `GetFIXProtocolsList`, …) answer `ResultCode=9` ("Parameter RequestID is required") without it; responses are correlated only by the echoed `RequestID`.
10. **Expecting `ToBackup`/`ToPrimary` to work on any session.** They require an initiator with backup destinations (`IBackupFixSession`); otherwise `ResultCode=9` ("Not a backup fix session instance").

## Reference

- `Samples/SimpleAdminClient/` and `Samples/EchoServer/fixengine.properties` in the FIX Antenna repo.
- `Docs/MonitoringAndAdministration.md` — protocol doc with full request/response XML examples (note the `commands.package` key spelling issue above).

## See also

- `01-minimal-acceptor.md` — the host `FixServer` this plugs into.
- `07-persistence-recovery.md` — `storageType` semantics.
- `09-tls-secure-session.md` — securing the port the admin session shares.

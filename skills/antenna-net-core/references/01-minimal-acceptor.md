# 01 Minimal Acceptor

> Targets **Epam.FixAntenna.NetCore 1.2.3**. See `../SKILL.md` for root API rules.

## Pattern

Server-side FIX session that accepts a counterparty connection, validates Logon, receives `NewOrderSingle (D)`, and responds with `ExecutionReport (8)` ack.

```
[counterparty] ──Logon──► [FixServer] ──validate──► [IFixSession.Connect]
                              │
                              └──NewOrderSingle──► OnNewMessage ──► reply ExecutionReport(New)
```

## When to use

- First reference for any acceptor-side onboarding.
- Stub for venue / broker simulator / test fixture.

## When NOT to use

- Need actual order matching → out of scope; FIX Antenna gives the wire, not the matching engine.
- Need many sessions in one process → see `06-multi-session-router.md`.
- Need TLS → see `09-tls-secure-session.md`.

## Key API (1.2.3 verified)

| Type | Purpose |
|---|---|
| `FixServer(Config)` | Acceptor host. Also `FixServer()` then `SetPort(int)` and `SetListener(IFixServerListener)`. |
| `IFixServerListener.NewFixSession(IFixSession)` | Called per inbound Logon. Decide accept/reject. |
| `SessionParameters.IncomingUserName/IncomingPassword` | Credentials from incoming Logon (tags 553/554). |
| `IFixSession.Connect()` | Accept the session. |
| `IFixSession.Disconnect(string)` then `Dispose()` | Reject with reason. |
| `IFixSession.Reject(string)` | Purpose-built rejection of an incoming acceptor connection (alternative to `Disconnect` + `Dispose` inside `NewFixSession`). Not applicable for initiators. |
| `IFixSessionListener : IFixMessageListener` | **One class** implements both `OnSessionStateChange(SessionState)` and `OnNewMessage(FixMessage)`. |
| `IFixSession.SetFixSessionListener(IFixSessionListener)` | Wire the listener. (Inherited `OnNewMessage` is dispatched through this too.) |
| `Epam.FixAntenna.Constants.Fix44.Tags` | Tag-number constants for FIX 4.4. |
| `FixMessage.AddTag(int, value)` | Build outbound messages. `AddTag` *appends*; `Set` (inherited from `ExtendedIndexedStorage`) *replaces*. |

## Configuration (fixengine.properties)

The listening port is a **process-wide** key (`port =`), not a per-session one. Minimal acceptor config:

```properties
# process-wide listening port
port = 3000

# sessions this acceptor expects (used to match incoming Logons)
sessionIDs = trader1
sessions.trader1.sessionType = acceptor
sessions.trader1.senderCompID = ACCEPTOR
sessions.trader1.targetCompID = TRADER
sessions.trader1.fixVersion = FIX.4.4
```

## Canonical skeleton (1.2.3, builds clean)

```csharp
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.Message;
using Tags = Epam.FixAntenna.Constants.Fix44.Tags;

public static class Program
{
    public static int Main()
    {
        var config = new Config("fixengine.properties");
        var server = new FixServer(config) { ConfigPath = "fixengine.properties" };
        server.SetListener(new OrderAcceptor());

        try
        {
            // Throws InvalidOperationException if no port is configured,
            // IOException if it cannot bind on any configured port.
            server.Start();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Cannot start FixServer: {e.Message}");
            return 1;
        }

        Console.WriteLine("Acceptor running. Press ENTER to stop.");
        Console.ReadLine();
        // Stop() closes the listening ports and logs out only sessions declared in
        // fixengine.properties (sessionIDs). Sessions accepted via the default
        // allow-non-registered path are NOT logged out — Disconnect/Dispose them
        // explicitly (track them as you accept them in NewFixSession).
        server.Stop();
        return 0;
    }
}

internal sealed class OrderAcceptor : IFixServerListener
{
    public void NewFixSession(IFixSession session)
    {
        var p = session.Parameters;
        if (p.IncomingUserName != "TRADER" || p.IncomingPassword != "pass")
        {
            session.Disconnect("Invalid credentials");
            session.Dispose();
            return;
        }

        // ONE listener — IFixSessionListener inherits OnNewMessage from IFixMessageListener.
        session.SetFixSessionListener(new BusinessListener(session));

        session.Connect();
    }
}

internal sealed class BusinessListener : IFixSessionListener
{
    private readonly IFixSession _session;
    public BusinessListener(IFixSession s) => _session = s;

    // Declared on IFixSessionListener.
    public void OnSessionStateChange(SessionState state)
    {
        if (SessionState.IsDisconnected(state))
            _session.Dispose();
    }

    // Inherited from IFixMessageListener — REQUIRED to compile.
    public void OnNewMessage(FixMessage message)
    {
        if (message.GetTagValueAsString(Tags.MsgType) != "D") return;

        var ack = new FixMessage();
        // Do NOT set Tags.MsgType here — it's passed to SendMessage below.
        // Single-arg SendMessage(ack) is wrong for fresh construction: with no 35 it throws
        // ArgumentException ("Message type(35 tag) cannot be null") at serialization; with 35
        // set but no header it serializes without 8/9/34/49/52/56 and crashes at the storage
        // layer with "Tag 34 is missing or has invalid value". Use two-arg SendMessage("8", ack).
        ack.AddTag(Tags.ClOrdID,   message.GetTagValueAsString(Tags.ClOrdID));
        ack.AddTag(Tags.OrderID,   Guid.NewGuid().ToString("N"));
        ack.AddTag(Tags.ExecID,    Guid.NewGuid().ToString("N"));
        ack.AddTag(Tags.ExecType,  "0");                                       // New
        ack.AddTag(Tags.OrdStatus, "0");                                       // New
        ack.AddTag(Tags.Symbol,    message.GetTagValueAsString(Tags.Symbol));
        ack.AddTag(Tags.Side,      message.GetTagValueAsString(Tags.Side));
        ack.AddTag(Tags.OrderQty,  message.GetTagValueAsLong(Tags.OrderQty));
        ack.AddTag(Tags.LeavesQty, message.GetTagValueAsLong(Tags.OrderQty));
        ack.AddTag(Tags.CumQty,    0L);
        ack.AddTag(Tags.AvgPx,     0.0, 4);
        _session.SendMessage("8", ack);   // two-arg overload writes the full header
    }
}
```

## Hardening (pre-Logon filtering)

- **Reject unknown sessions automatically.** By default the engine accepts Logons for sessions not declared in `fixengine.properties` (`serverAcceptorStrategy` defaults to `Epam.FixAntenna.NetCore.FixEngine.Acceptor.AllowNonRegisteredAcceptorStrategyHandler`). To auto-reject anything not declared under `sessionIDs`, set:

  ```properties
  serverAcceptorStrategy = Epam.FixAntenna.NetCore.FixEngine.Acceptor.DenyNonRegisteredAcceptorStrategyHandler
  ```

- **IP filtering before Logon.** Implement the public `Epam.FixAntenna.NetCore.FixEngine.IConnectionValidator` (single method `bool Allow(IPAddress address)`) and wire it with `server.SetConnectionValidator(validator)` *before* `Start()`. Denied addresses never reach the Logon stage. Demoed in the shipped `SimpleServer` sample.

## Listener rules (1.2.3 verified)

- **Single reader thread.** `OnNewMessage` fires on the session's dedicated reader thread; calls are serialized per session (different sessions = different threads). Blocking it stalls ALL inbound processing for that session, including the engine's admin handling (TestRequest replies).
- **The engine reuses ONE `FixMessage` per session.** The reader parses every inbound message into the same instance and clears it as soon as `OnNewMessage` returns. Extract scalars synchronously, or `Clone()` before handing the reference to a queue, another thread, or storage.
- **Exceptions tear the session down.** An exception escaping `OnNewMessage` is logged, rethrown, and shuts the session down via the reader thread. Catch, log, dead-letter — never let exceptions escape.

## Common LLM mistakes

1. **Implementing only `OnSessionStateChange` on `IFixSessionListener`.** Won't compile. `IFixSessionListener` inherits `IFixMessageListener`, so `OnNewMessage(FixMessage)` is required on the same class.
2. **Confusing `AddTag` and `Set` semantics.** `AddTag(tag, value)` *appends* an occurrence — calling it twice for the same tag yields a malformed (duplicate-tag) outbound and a session-level reject from the peer. `Set(tag, value)` is inherited from `ExtendedIndexedStorage` and *replaces*. Use `AddTag` for fresh construction (this skeleton); use `Set` (or `UpdateValue(..., IndexedStorage.MissingTagHandling.AddIfNotExists)`) when mutating an existing message.
3. **Referencing `Tags` without the FIX-version namespace.** There is no root `Tags` class. Use `Epam.FixAntenna.Constants.Fix44.Tags` (or alias: `using Tags = Epam.FixAntenna.Constants.Fix44.Tags;`).
4. **Calling `Config.LoadConfiguration(...)` / `Config.LoadProperties(...)`.** Neither exists. Use the constructor: `new Config("fixengine.properties")`.
5. **Validating credentials inside `OnNewMessage`.** Validate in `NewFixSession` against `session.Parameters.IncomingUserName / IncomingPassword`. By the time `OnNewMessage` fires, Logon has already succeeded — too late to reject.
6. **Not disposing the session on disconnect.** Pattern: dispose inside `OnSessionStateChange` when `SessionState.IsDisconnected(state)` is true.
7. **Setting MsgSeqNum, SendingTime, BodyLength, CheckSum manually.** Engine sets those.
8. **Returning an ExecutionReport with `OrdStatus=Filled` as the ack.** The ack is `ExecType=0 (New)`, `OrdStatus=0 (New)`. Fills come later.

## See also

- `02-minimal-initiator.md` — the client side of this conversation.
- `03-order-entry-client.md` — full order flow on the initiator side.
- Root `SKILL.md` — verified API surface table.

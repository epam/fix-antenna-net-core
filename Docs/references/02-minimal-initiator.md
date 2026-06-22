# 02 Minimal Initiator

> Targets **Epam.FixAntenna.NetCore 1.2.3**. See `../SKILL.md` for root API rules.

## Pattern

Client session that connects to a venue/broker, logs on, exchanges heartbeats / test requests automatically, and logs off cleanly.

```
[client app] ──Connect──► [venue]
              ◄──Logon──── (engine sends Logon automatically on Connect)
              ◄──Heartbeats──► (engine handles every HeartBtInt)
              ──Logout──►
```

## When to use

- Buy-side or order-routing app connecting OUT to a venue.
- Any client that initiates the TCP connection.

## When NOT to use

- Server-side (accepting) → `01-minimal-acceptor.md`.
- Many venues at once → `06-multi-session-router.md`.
- Scheduled connection (e.g., trading-hours-only) → `11-scheduled-sessions.md`.

## Key API

| Type | Purpose |
|---|---|
| `SessionParametersBuilder.BuildSessionParametersList(path)` | Load sessions from `fixengine.properties`. |
| `SessionParameters.CreateInitiatorSession()` → `IFixSession` | Instantiate from config. |
| `IFixSession.Connect()` | Open TCP, send Logon. |
| `IFixSession.Disconnect(string)` | Send Logout with reason, then drop. |
| `SessionState.IsNotDisconnected(state)` | Guard for clean shutdown. |

## Canonical skeleton (1.2.3, builds clean)

```csharp
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.Message;
using Tags = Epam.FixAntenna.Constants.Fix44.Tags;

var configured = SessionParametersBuilder.BuildSessionParametersList(Config.DefaultEngineProperties);
var sessions = new List<IFixSession>();

foreach (var p in configured.Values)
{
    var session = p.CreateInitiatorSession();
    session.SetFixSessionListener(new MyListener(session));   // implements BOTH methods (see below)
    try
    {
        session.Connect();   // throws IOException if the connection is refused
        sessions.Add(session);
    }
    catch (IOException e)
    {
        Console.Error.WriteLine($"Cannot start session {p.SessionId}: {e.Message}");
        session.Dispose();
    }
}

// Keep the process alive while the sessions trade.
Console.WriteLine("Press ENTER to logout.");
Console.ReadLine();

// graceful shutdown — note: Disconnect is asynchronous; it does NOT guarantee
// immediate shutdown (FIX requires waiting for the counterparty's Logout reply).
foreach (var s in sessions)
{
    if (SessionState.IsNotDisconnected(s.SessionState))
        s.Disconnect("Shutting down");
    s.Dispose();
}

// One listener — IFixSessionListener : IFixMessageListener,
// so the class MUST implement both methods to compile.
internal sealed class MyListener : IFixSessionListener
{
    private readonly IFixSession _session;
    public MyListener(IFixSession session) => _session = session;

    public void OnSessionStateChange(SessionState state)
    {
        Console.WriteLine($"[state] {state}");
        if (SessionState.IsDisconnected(state))
            _session.Dispose();   // release session resources once it goes down
    }

    public void OnNewMessage(FixMessage message)
    {
        var msgType = message.GetTagValueAsString(Tags.MsgType);
        // dispatch to business code
    }
}
```

## Venue failover (backup connection)

Declare a backup destination per session: `sessions.<id>.backupHost` / `sessions.<id>.backupPort`. With `enableAutoSwitchToBackupConnection = true` (the default) the engine fails over to the backup automatically; `cyclicSwitchBackupConnection = true` (default) cycles primary → backup → primary across reconnect attempts. If the venue requires fresh sequence numbers after a site switch, set `resetOnSwitchToBackup` / `resetOnSwitchToPrimary` (both default `false`).

## Listener rules (1.2.3 verified)

- **Single reader thread.** `OnNewMessage` fires on the session's dedicated reader thread; calls are serialized per session (different sessions = different threads). Blocking it stalls ALL inbound processing for that session, including the engine's admin handling (TestRequest replies).
- **The engine reuses ONE `FixMessage` per session.** The reader parses every inbound message into the same instance and clears it as soon as `OnNewMessage` returns. Extract scalars synchronously, or `Clone()` before handing the reference to a queue, another thread, or storage.
- **Exceptions tear the session down.** An exception escaping `OnNewMessage` is logged, rethrown, and shuts the session down via the reader thread. Catch, log, dead-letter — never let exceptions escape.

## Common LLM mistakes

1. **Implementing only `OnSessionStateChange` on `IFixSessionListener`.** Won't compile in 1.2.3 — the interface inherits `IFixMessageListener`, so `OnNewMessage(FixMessage)` is also required on the same class.
2. **Constructing the Logon (msg type A) manually.** Don't. `Connect()` does it, using `SessionParameters` (`HeartbeatInterval`, `ForceSeqNumReset`, `UserName`, `Password` — those are the real property names; HeartBtInt/ResetSeqNumFlag/Username are the FIX *field* names, not API members).
3. **Sending heartbeats manually.** The engine sends them every `HeartBtInt` seconds. Sending Heartbeat (0) from app code will desync the timer.
4. **Responding to TestRequest (1) manually.** Engine handles it.
5. **Reconnecting on disconnect by calling `Connect()` again immediately.** The engine has reconnect logic configurable in `fixengine.properties` via `autoreconnectAttempts` (`-1` = off, `0` = infinite, `N` = N tries) and `autoreconnectDelayInMs`. Use it. Manual reconnects fight the engine's state machine. ⚠️ Do NOT invent `enableAutoreconnect = true` — that property does not exist in 1.2.3; the engine silently ignores it. Autoreconnect is controlled by `autoreconnectAttempts` alone.
6. **Hardcoding `senderCompID`, `targetCompID`, host, port in code.** Put them in `fixengine.properties`. Code that builds these from env vars at runtime is fine; literal strings inline aren't.

## See also

- `09-tls-secure-session.md` — when TLS is required.
- `11-scheduled-sessions.md` — when connection is time-windowed.

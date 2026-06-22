# 03 Order Entry Client ★

> Targets **Epam.FixAntenna.NetCore 1.2.3**. See `../SKILL.md` for root API rules.

## Pattern

Initiator that drives the full order lifecycle:

```
NewOrderSingle (D)
   ──► ExecutionReport ExecType=New, OrdStatus=New      (ack)
   ──► ExecutionReport ExecType=Trade (F), partial/full (fills)

OrderCancelReplaceRequest (G)
   ──► ExecutionReport ExecType=Replaced
       (new ClOrdID is now live; old ClOrdID terminal)

OrderCancelRequest (F)
   ──► ExecutionReport ExecType=Canceled

OrderCancelReject (9)
   ──► the cancel did NOT happen — read OrdStatus (39) for the actual state
       (may still be working, or already Filled: CxlRejReason=0 "too late to cancel")
```

This is the canonical pattern that catches the most LLM errors in generated trading code.

## When to use

- OMS, EMS, smart order router, algo execution layer.
- Any client that submits orders and tracks lifecycle.

## When NOT to use

- Drop-copy (read-only) → `04-drop-copy-consumer.md`.
- Quote requests / market data → different message families.

## Key API (1.2.3 verified)

| Type | Purpose |
|---|---|
| `SessionParametersBuilder.BuildSessionParametersList(string)` | Load session list from properties file. |
| `SessionParameters.CreateInitiatorSession()` | Build the initiator. |
| `IFixSession.Connect()` | Open and Logon. |
| `IFixSession.SendMessage(string msgType, FixMessage)` | Two-arg overload — use this for fresh outbound construction. Engine writes the full header. Single-arg `SendMessage(FixMessage)` is wrong for fresh construction: with no 35 it throws `ArgumentException: Message type(35 tag) cannot be null` at serialization; with 35 set but no header it serializes without 8/9/34/49/52/56 and crashes at the storage layer with "Tag 34 is missing or has invalid value". Either way: use the two-arg overload — see api-reference.md "Writing a message". |
| `IFixSessionListener : IFixMessageListener` | **One class**, both `OnSessionStateChange(SessionState)` and `OnNewMessage(FixMessage)`. |
| `IFixSession.SetFixSessionListener(IFixSessionListener)` | Wire the listener — receives state changes AND messages. |
| `Epam.FixAntenna.Constants.Fix44.Tags` | Tag constants. |
| `FixMessage.AddTag(int, value)` | Build messages. `AddTag` *appends*; `Set` (inherited from `ExtendedIndexedStorage`) *replaces*. |
| `FixMessage.GetTagValueAsString/Long/Double/Decimal/Bool(int)` | Read fields. `GetTagValueAsDecimal` is inherited from `ExtendedIndexedStorage`. `GetTagValueAsLong/Double/Decimal/Bool` throw `FieldNotFoundException` if the tag is missing — guard with `IsTagExists(int)` for optional fields. `GetTagValueAsString` is the exception: it returns `null` for a missing tag. |
| `SessionState.IsConnected(state)` / `IsDisconnected(state)` | Branch on state. |

## State machine the application must track

Per order, keyed by `ClOrdID`:

```
states = { PendingNew, New, PartiallyFilled, Filled,
           PendingCancel, Canceled,
           PendingReplace, Replaced,
           Rejected, Expired, DoneForDay }
```

On every inbound `ExecutionReport`:
1. Look up the order by `ClOrdID (11)`.
2. Read `ExecType (150)` — what this report IS.
3. Read `OrdStatus (39)` — current state per the venue.
4. **Dedupe trade fills on `ExecID (17)`** — replays can land twice.
5. Update `LeavesQty (151)`, `CumQty (14)`, `AvgPx (6)` from the report — **trust venue numbers**, don't compute.
6. For `ExecType = Trade (F)`: this is a fill.
7. For Cancel/Replace: also track `OrigClOrdID (41)`.

## ClOrdID rules

1. Unique per session per trading day. Format: `{prefix}-{yyyyMMdd}-{counter}`.
2. On Replace, the new request needs a new `ClOrdID` AND the old `ClOrdID` in `OrigClOrdID (41)`.
3. Never reuse a ClOrdID even across days unless the venue agreement allows it.

## Canonical skeleton (1.2.3, builds clean)

```csharp
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

        var session = parameters.CreateInitiatorSession();
        var book = new OrderBook();
        var listener = new OrderClientListener(book);

        session.SetFixSessionListener(listener);   // one listener handles both
        session.Connect();

        listener.WaitForConnected(TimeSpan.FromSeconds(30));

        // Submit a seed order: MSFT BUY 100 @ 350.00 LIMIT DAY
        var clOrdId = $"BUY-{DateTime.UtcNow:yyyyMMdd}-{1:D6}";
        var order = new FixMessage();
        // Do NOT set Tags.MsgType here — it's passed to SendMessage("D", order) below.
        // Single-arg SendMessage(order) is wrong for fresh construction: with no 35 it throws
        // ArgumentException ("Message type(35 tag) cannot be null") at serialization; with 35
        // set but no header it serializes without 8/9/34/49/52/56 and crashes at the storage
        // layer with "Tag 34 is missing or has invalid value". Use two-arg SendMessage("D", order).
        order.AddTag(Tags.ClOrdID,      clOrdId);
        order.AddTag(Tags.HandlInst,    "1");
        order.AddTag(Tags.Symbol,       "MSFT");
        order.AddTag(Tags.Side,         "1");                              // 1 = Buy
        order.AddTag(Tags.OrderQty,     100L);
        order.AddTag(Tags.OrdType,      "2");                              // 2 = Limit
        order.AddTag(Tags.Price,        350.00, 4);
        order.AddTag(Tags.TimeInForce,  "0");                              // 0 = Day
        order.AddTag(Tags.TransactTime, DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff"));
        book.MarkPendingNew(clOrdId, "MSFT", "1", 100L, 350.00m);
        session.SendMessage("D", order);   // two-arg overload writes the full header

        Console.WriteLine("Order sent. Press ENTER to logout.");
        Console.ReadLine();
        session.Disconnect("user shutdown");
        session.Dispose();
        return 0;
    }
}

// One listener — IFixSessionListener inherits IFixMessageListener.
internal sealed class OrderClientListener : IFixSessionListener
{
    private readonly OrderBook _book;
    private readonly ManualResetEventSlim _connected = new(false);

    public OrderClientListener(OrderBook book) => _book = book;
    public bool WaitForConnected(TimeSpan t) => _connected.Wait(t);

    public void OnSessionStateChange(SessionState state)
    {
        if (SessionState.IsConnected(state)) _connected.Set();
        if (SessionState.IsDisconnected(state)) _connected.Reset();
    }

    public void OnNewMessage(FixMessage m)
    {
        switch (m.GetTagValueAsString(Tags.MsgType))
        {
            case "8": HandleExecutionReport(m); break;
            case "9": HandleCancelReject(m); break;
            case "j": /* business reject — log */ break;
            case "3": /* session reject — log */ break;
        }
    }

    private void HandleExecutionReport(FixMessage m)
    {
        var clOrdId   = m.GetTagValueAsString(Tags.ClOrdID);
        var execType  = m.GetTagValueAsString(Tags.ExecType);
        var ordStatus = m.GetTagValueAsString(Tags.OrdStatus);
        var execId    = m.IsTagExists(Tags.ExecID) ? m.GetTagValueAsString(Tags.ExecID) : null;

        // Dedupe trades on ExecID — PossDupFlag replays would otherwise double-count.
        if (execType == "F" && execId is not null && _book.SeenExecId(execId)) return;

        var cumQty    = m.IsTagExists(Tags.CumQty)    ? m.GetTagValueAsLong(Tags.CumQty)    : 0L;
        var leavesQty = m.IsTagExists(Tags.LeavesQty) ? m.GetTagValueAsLong(Tags.LeavesQty) : 0L;
        // AvgPx — use GetTagValueAsDecimal (public on ExtendedIndexedStorage in 1.2.3,
        // inherited on FixMessage). Throws FieldNotFoundException if tag missing,
        // so guard with IsTagExists for optional fields.
        var avgPx = m.IsTagExists(Tags.AvgPx) ? m.GetTagValueAsDecimal(Tags.AvgPx) : 0m;

        _book.Apply(clOrdId, execType, ordStatus, cumQty, leavesQty, avgPx);
    }

    private void HandleCancelReject(FixMessage m)
    {
        // The cancel did NOT happen — never mark the order canceled here.
        // Read OrdStatus (39) (required on the 9) for the venue's actual state:
        // it may still be working, or already Filled (CxlRejReason (102) = 0, too late to cancel).
        var ordStatus = m.GetTagValueAsString(Tags.OrdStatus);
        var origClOrd = m.IsTagExists(Tags.OrigClOrdID) ? m.GetTagValueAsString(Tags.OrigClOrdID) : null;
        if (origClOrd is not null) _book.UnmarkPending(origClOrd);
    }
}

// Replace this with real OMS state. Shown here only to make the skeleton
// compile and to name the boundary between FIX-aware code and business state.
internal sealed class OrderBook
{
    private readonly HashSet<string> _seenExecIds = new();

    public void MarkPendingNew(string clOrdId, string symbol, string side, long qty, decimal price) { }
    public bool SeenExecId(string execId) => !_seenExecIds.Add(execId);
    public void Apply(string clOrdId, string execType, string ordStatus, long cumQty, long leavesQty, decimal avgPx) { }
    public void UnmarkPending(string clOrdId) { }
}
```

## OrderCancelRequest (F) construction

Same construction rules as the order: fresh `FixMessage`, no 35, two-arg `SendMessage`. Per the ClOrdID rules above: the cancel gets a **new** `ClOrdID (11)`; the order being canceled goes in `OrigClOrdID (41)`.

```csharp
var cancelClOrdId = $"BUY-{DateTime.UtcNow:yyyyMMdd}-{2:D6}";
var cancel = new FixMessage();
cancel.AddTag(Tags.OrigClOrdID,  clOrdId);          // the live order's ClOrdID
cancel.AddTag(Tags.ClOrdID,      cancelClOrdId);    // NEW id for the cancel request itself
cancel.AddTag(Tags.Symbol,       "MSFT");
cancel.AddTag(Tags.Side,         "1");              // must match the original order
cancel.AddTag(Tags.TransactTime, DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff"));
session.SendMessage("F", cancel);
// Expect ExecType=PendingCancel then Canceled — or OrderCancelReject (9), see below.
```

## Listener rules (1.2.3 verified)

- **Single reader thread.** `OnNewMessage` fires on the session's dedicated reader thread; calls are serialized per session (different sessions = different threads). Blocking it stalls ALL inbound processing for that session, including the engine's admin handling (TestRequest replies).
- **The engine reuses ONE `FixMessage` per session.** The reader parses every inbound message into the same instance and clears it as soon as `OnNewMessage` returns. Extract scalars synchronously (as the skeleton does), or `Clone()` before handing the reference to a queue, another thread, or storage.
- **Exceptions tear the session down.** An exception escaping `OnNewMessage` is logged, rethrown, and shuts the session down via the reader thread. Catch, log, dead-letter — never let exceptions escape.

## Common LLM mistakes

1. **Implementing only `OnSessionStateChange` on `IFixSessionListener`.** Won't compile — `IFixSessionListener : IFixMessageListener` so `OnNewMessage` is required on the same class.
2. **Confusing `AddTag` and `Set` semantics.** `AddTag` *appends* — twice for the same tag = duplicate (malformed) outbound. `Set(tag, value)` is inherited from `ExtendedIndexedStorage` and *replaces*. Use `AddTag` for new messages, `Set` (or `UpdateValue(..., IndexedStorage.MissingTagHandling.AddIfNotExists)`) for mutations.
3. **`Tags.MsgType` without `Fix44.` prefix.** No root `Tags` class. Alias `using Tags = Epam.FixAntenna.Constants.Fix44.Tags;`.
4. **`SessionState.LoggedOn`.** Doesn't exist. Use `SessionState.Connected` (verify with `SessionState.IsConnected(state)`).
5. **`ForceSeqNumReset.OnLogon`.** Invented. Only `Always`, `OneTime`, `Never` exist.
6. **No `ExecID` dedup on trades.** PossDup replays cause double-counted fills.
7. **Computing `CumQty`/`AvgPx` locally** by summing `LastQty` * `LastPx`. Trust the venue.
8. **Treating `ExecType=New` as a fill.** It's "accepted, working." Wait for `Trade (F)` / `Filled (2)`.
9. **Confusing `ExecType` and `OrdStatus`.** Independent fields. `ExecType=Trade` with `OrdStatus=PartiallyFilled` is normal.
10. **Reusing `ClOrdID`.** Generates rejects depending on venue.
11. **Cancel without `OrigClOrdID (41)`.** Malformed; rejected.
12. **Treating `ExecType=PendingCancel` as terminal.** Order isn't canceled yet — wait for `Canceled`.
13. **Misreading `OrderCancelReject (9)`.** A Cancel Reject means the cancel did NOT happen — never mark the order canceled. But don't assume "still working" either: read `OrdStatus (39)` (required on the 9) for the venue's actual state — the order may still be live, or already `Filled` (`CxlRejReason (102) = 0`, too late to cancel). UI must reflect that state.
14. **Sending orders before `SessionState.Connected`.** Buffer or reject until logged on.
15. **An OrderBook that isn't thread-safe.** `OnNewMessage` mutates the book on the session's reader thread while the app thread sends orders and reads state — use a lock or concurrent collections around shared order state.
16. **Assuming every ExecutionReport carries `ClOrdID (11)`.** Unsolicited/venue-initiated reports may omit it, and `GetTagValueAsString` returns `null` for a missing tag — handle a `null` book key instead of letting it NRE or silently dropping the report.

## See also

- `01-minimal-acceptor.md` — the server side.
- `07-persistence-recovery.md` — what happens to in-flight orders on disconnect.
- `04-drop-copy-consumer.md` — same `ExecutionReport` parsing, read-only.

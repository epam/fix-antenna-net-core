# 06 Multi-Session Router

> Targets **Epam.FixAntenna.NetCore 1.2.3**. See `../SKILL.md` for root API rules.

## Pattern

One process running many FIX sessions — some acceptors (clients connect IN), some initiators (this process connects OUT to venues). Messages flow across sessions per routing rules. This is the FIXEdge-style FIX gateway pattern.

```
                  ┌─► [venue A initiator] ──► VenueA
[client acceptor] ─┤
                  └─► [venue B initiator] ──► VenueB

ExecutionReport from VenueA ──► routed back to the originating client session
```

## When to use

- FIX hub / gateway sitting between buy-side clients and multiple venues.
- White-label broker connectivity layer.
- Aggregator / smart order router with venue selection logic.

## When NOT to use

- Single counterparty pair → `01-minimal-acceptor.md` or `02-minimal-initiator.md`.
- Read-only fan-out → `04-drop-copy-consumer.md`.

## Architecture

```
                 ┌─────────────────────────────────────┐
                 │              Router                 │
                 │  routing rules (symbol/account/etc) │
                 │  correlation table (ClOrdID → src)  │
                 └─────────────────────────────────────┘
                       ▲                       ▲
       client session  │                       │  venue session(s)
       (acceptor)      │                       │  (initiator)
                       │                       │
                  client FIX                venue FIX
```

## Wiring the sessions (1.2.3)

- **Acceptor side (clients connect in):** create a `FixServer`, configure it, and implement `IFixServerListener` — the engine calls `NewFixSession(IFixSession session)` for every inbound session; set the session's listener there and call `session.Connect()`.
- **Initiator side (out to venues):** build `SessionParameters` per venue and call `parameters.CreateInitiatorSession()`.
- **There is no public session registry in 1.2.3.** The engine's `FixSessionManager` (which tracks all live sessions) is `internal` (`FixEngine/Manager/FixSessionManager.cs`) — customer code cannot query it. The router must keep its own map, e.g. `Dictionary<string, IFixSession>`, populated from `NewFixSession` on the acceptor side and at creation time on the initiator side.

## Key rules

1. **Each session has its own ClOrdID space.** The same ClOrdID can exist on the client side and on the venue side — they are unrelated. Translation is required.
2. **Maintain a correlation table.** Key: `(client_session, client_clOrdID)` ↔ `(venue_session, venue_clOrdID)`. This table is critical state — it must be durable if the process restarts mid-day.
3. **Don't blindly forward.** Each message must be re-serialized (the routing layer is not a TCP proxy). Headers (SenderCompID, TargetCompID, MsgSeqNum, SendingTime) are per-session; the engine sets them when sending on the target session.
4. **Replicate state, not packets.** Routing logic should be on parsed messages, not byte streams.
5. **Drop-copy is a separate concern.** A router that also drop-copies to a third party should use a separate session for it, not multiplex.

## Forwarding mechanics — `SendMessage` vs `SendAsIs`

When an inbound `FixMessage` is passed straight to another session's `SendMessage(FixMessage)`, the engine **does** restamp the session header — verified in `StandardMessageFactory.cs`: BeginString, SenderCompID, TargetCompID, Sender/TargetSubID, Sender/TargetLocationID, plus MsgSeqNum, SendingTime, BodyLength, CheckSum (see root `SKILL.md` "Hard rules" DO #8). That much is safe. What it does **not** do is rewrite business-identity tags (ClOrdID, OrigClOrdID) nor the optional third-party header tags (OnBehalfOfCompID `115`, DeliverToCompID `128`) — all of those leak through from the source side and cause the collisions called out in mistakes #2/#3 below.

| Method | Header rewrite? | Seq num? | Use for |
|---|---|---|---|
| `SendMessage(FixMessage)` | **Yes** — engine restamps full session header | Incremented by engine | Gateway forwarding (this pattern). Translate body tags first; pass the message body through. |
| `SendAsIs(FixMessage)` | **No** — bytes preserved | **NOT incremented** | Drop-copy / proxy / replay where the *original* wire bytes must go out unchanged — and the bytes already carry correct seq nums for the target session. |

Picking `SendAsIs` for a gateway forward is the common mistake — the venue then sees the client's `SenderCompID` and rejects the Logon-bound message. `SendAsIs` also bypasses sequence numbering entirely: the message is written as raw bytes and the session's outbound MsgSeqNum is **not** incremented (verified: `SyncMessagePumper.cs` increments the out seq num only for typed sends; the `msgType == null` "as is" path in `StandardMessageFactory.Serialize` copies the buffer untouched). So even a replay/drop-copy use desynchronizes the target session unless the bytes already carry the correct sequence numbers.

## Common LLM mistakes

1. **Passing the inbound `FixMessage` directly to `SendMessage` without translating identity tags.** The session header gets restamped (good), but the business-identity tags `ClOrdID`/`OrigClOrdID` — and the third-party header tags `OnBehalfOfCompID (115)`/`DeliverToCompID (128)`, which the engine does not restamp — carry over from the source side. Translate (or strip) them before forwarding (see #2, #3).
2. **Reusing the client's `ClOrdID` on the venue session.** Causes ClOrdID collisions across clients hitting the same venue.
3. **Forgetting to translate `ExecutionReport.ClOrdID` back to the client's ID on the return path.** Client receives a report for an order it doesn't know about.
4. **Using `SendAsIs` for forwarding.** That preserves the source's `SenderCompID`/`TargetCompID` in the wire bytes — the target venue then rejects the message (CompIDs don't match the agreed session pair) — and the outbound seq num is not incremented (see table above). `SendAsIs` is for drop-copy / replay, not routing.
5. **In-memory-only correlation table.** Restart loses orders. Persist (file, DB, or custom storage).
6. **One listener instance serving multiple sessions.** Works but invites accidental cross-session state. Prefer one listener per session, sharing a router via DI.
7. **Blocking inside `OnNewMessage` while routing to the other session.** Causes heartbeat timeouts. Use a queue.

## State that must be durable

| State | Why |
|---|---|
| Correlation table | Restart recovery; orphan-order detection. |
| Active orders per session | For end-of-day reconciliation. |
| Per-session seq nums | Handled by engine's persistent storage — don't duplicate this. |

## See also

- `07-persistence-recovery.md` — for sequence handling across reconnects per session.
- `08-custom-storage-replication.md` — for HA of the correlation table.
- `12-perf-profiles.md` — multi-session apps usually hit threading model trade-offs.

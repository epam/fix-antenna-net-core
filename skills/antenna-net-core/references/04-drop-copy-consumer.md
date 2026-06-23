# 04 Drop Copy Consumer

> Targets **Epam.FixAntenna.NetCore 1.2.3**. See `../SKILL.md` for root API rules.

## Pattern

A FIX session subscribed to receive a read-only stream of executions / trades from a venue, broker, or middle-office system. Decode, normalize, persist (and usually fan out to downstream systems like surveillance, P&L, regulatory reporting).

```
[venue / OMS] ──ExecutionReport / TradeCaptureReport──► [drop-copy session] ──► normalize ──► persist
                                                                              └─► fan out (Kafka, DB, file)
```

Drop-copy is **not** the same as a trading session. It is one-way (in) and should not generate orders.

## When to use

- Surveillance / compliance.
- Post-trade reporting (TCA, regulatory T+1).
- Back-office reconciliation feeds.
- Building an independent audit trail.

## When NOT to use

- Need to send orders → `03-order-entry-client.md`.
- Want to bridge FIX to Kafka/MQ → `10-fix-to-kafka-bridge.md` (this pattern is one of its inputs).

## Key API

Same listener pattern as `02-minimal-initiator.md` — **one class implementing `IFixSessionListener`** (which inherits `IFixMessageListener`), providing both `OnSessionStateChange` and `OnNewMessage`. The `OnNewMessage` body filters for `ExecutionReport (8)` and/or `TradeCaptureReport (AE)` and dispatches to a normalization layer. Tag constants live in `Epam.FixAntenna.Constants.Fix44.Tags`. Build messages with `AddTag(int, value)` only if needed (rare in drop-copy).

## Configuration (fixengine.properties)

A drop-copy feed is typically an **initiator** session into the venue's drop-copy gateway (some venues invert this — check the spec; if the venue connects to you, configure an acceptor per `01-minimal-acceptor.md`):

```properties
sessionIDs = dropcopy
sessions.dropcopy.sessionType = initiator
sessions.dropcopy.host = dc.venue.example
sessions.dropcopy.port = 9880
sessions.dropcopy.senderCompID = DCCLIENT
sessions.dropcopy.targetCompID = VENUEDC
sessions.dropcopy.fixVersion = FIX.4.4

# Dictionary validation of incoming messages is OFF by default (validation = false).
# Opt in if you want the engine to validate against the dictionary:
validation = true
```

## Listener rules (1.2.3 verified)

- **Single reader thread.** `OnNewMessage` fires on the session's dedicated reader thread; calls are serialized per session (different sessions = different threads). Blocking it stalls ALL inbound processing for that session, including the engine's admin handling (TestRequest replies).
- **The engine reuses ONE `FixMessage` per session.** The reader parses every inbound message into the same instance and clears it as soon as `OnNewMessage` returns. **Critical for the queue-decoupling this pattern recommends:** never enqueue the raw reference — `Clone()` (or `DeepClone`) the message, or extract scalars synchronously, before handing it to a queue, another thread, or storage.
- **Exceptions tear the session down.** An exception escaping `OnNewMessage` is logged, rethrown, and shuts the session down via the reader thread. Catch, log, dead-letter — never let exceptions escape.

## Canonical processing pipeline

```
OnNewMessage
   │
   ├─ filter: msg type ∈ {8, AE}
   ├─ extract: ExecID, ClOrdID, OrderID, Symbol, Side, LastQty, LastPx,
   │           TransactTime, ExecType, OrdStatus, CumQty, AvgPx
   ├─ validate against dictionary (engine does this only if validation = true; default false)
   ├─ idempotency: dedupe on ExecID
   ├─ normalize to internal DTO (decoupled from FIX)
   └─ persist (write-then-publish, or transactional outbox)
```

## Common LLM mistakes

1. **Generating outbound messages from drop-copy code.** The session is read-only by convention. Do not respond with orders. (Heartbeats, TestRequest replies, Logout are handled by the engine — that's fine.)
2. **Failing to dedupe.** Drop-copy streams can replay on reconnect. **Dedupe on `ExecID (17)`**, not `ClOrdID`. ClOrdIDs are not unique per execution event.
3. **Treating `ExecutionReport` and `TradeCaptureReport` as interchangeable.** They overlap but `TradeCaptureReport` exists for post-trade workflows; ExecutionReport is the order lifecycle. Different venues use different conventions — read the venue's drop-copy spec.
4. **Persisting the FIX wire format.** Persist the *normalized* DTO. Raw FIX is fine for audit logs but is brittle for queries.
5. **Skipping malformed messages silently.** A drop-copy stream missing a trade is a regulatory issue. Persist the malformed message to a dead-letter store; alert.
6. **Synchronous downstream writes inside `OnNewMessage`.** Blocks the session's reader thread; risks heartbeat timeout. Decouple via in-memory queue or outbox table — but **enqueue a `Clone()`, never the raw `FixMessage` reference**: the engine reuses one parse instance per session and clears it as soon as `OnNewMessage` returns, so a queued reference is wiped before the consumer sees it.

## Reliability requirements (production)

| Requirement | How |
|---|---|
| No message loss | Persistent storage on the FIX session + transactional outbox for downstream fan-out. |
| Replay on restart | Persistent storage replays gaps on next logon via ResendRequest. The engine has no application-level ack — a message is consumed the moment `OnNewMessage` returns (throwing doesn't redeliver; it tears the session down). Commit the outbox write synchronously inside `OnNewMessage` before returning (fast local append); recovery after a crash mid-callback comes from reconciling the outbox against the engine's persistent incoming log (`IFixSession.RetrieveReceivedMessage(seqNum)` — requires `incomingStorageIndexed = true`, default false), not from FIX redelivery. |
| Dedup | ExecID-based, with a TTL'd Bloom filter or a unique index in the DB. |
| Throughput | Decouple `OnNewMessage` from heavy work. |

## See also

- `03-order-entry-client.md` — the writer side; same message types parsed.
- `07-persistence-recovery.md` — what happens on reconnect.
- `10-fix-to-kafka-bridge.md` — drop-copy → Kafka, the most common production deployment.

# 10 FIX-to-Kafka / MQ / JSON Bridge

> Targets **Epam.FixAntenna.NetCore 1.2.3**. See `../SKILL.md` for root API rules. (Bridge code is mostly Kafka/messaging — minimal FIX Antenna API surface here.)

## Pattern

Consume FIX from one or more sessions (typically drop-copy or order entry), normalize to a domain DTO, publish to Kafka / MQ / event bus. The bus is then consumed by downstream platforms (analytics, surveillance, P&L, dashboards).

```
[FIX session(s)] ──OnNewMessage──► [normalizer] ──DTO──► [outbox] ──► [Kafka / RabbitMQ / Pulsar]
                                                            │
                                                            └─► retries, dedup, ordering guarantees
```

## When to use

- Building a "FIX-to-events" backbone for a modern data platform.
- Feeding post-trade systems built on streaming infra.
- Decoupling FIX-fluent code from downstream consumers (most teams don't want to learn FIX).

## When NOT to use

- The downstream system also speaks FIX. Don't translate twice.
- Sub-millisecond latency requirements. Bus adds latency — see `12-perf-profiles.md` for in-process patterns.

## Architecture: the transactional outbox

The critical rule: **do not publish to Kafka directly from `OnNewMessage`.** The session thread must not block on Kafka, and Kafka must not see partial-publish duplicates after a crash. A durable record of "this was received" that survives the crash and can be replayed is required.

The engine gives ONE such record automatically (`FilesystemStorageFactory`'s journal) and exposes it via two public methods on `IFixSession`:

```csharp
byte[] RetrieveSentMessage(long seqNumber);
byte[] RetrieveReceivedMessage(long seqNumber);
```

Both return the raw on-the-wire FIX bytes of the message at that sequence number, or throw `IOException` on storage error. There is no range-retrieve in the public surface (`IMessageStorage.RetrieveMessages` exists but is on the `internal` interface). To drain the journal, walk seq numbers explicitly.

> ⚠️ **`session.InSeqNum` is the NEXT-EXPECTED incoming sequence number, not the last received.** Verified in `FixSessionRuntimeState` (`InSeqNum--; LastProcessedSeqNum = InSeqNum > 0 ? InSeqNum - 1 : 0;`). The last successfully processed message has seq number `InSeqNum - 1`. The engine tracks `LastProcessedSeqNum` internally but does **not** expose it on `IFixSession`. To loop over already-received messages: iterate `for (var seqN = publishedThrough + 1; seqN < session.InSeqNum; seqN++)` — strictly less-than, not less-than-or-equal. Calling `RetrieveReceivedMessage(session.InSeqNum)` will attempt to read the not-yet-received message.

This gives **two valid outbox patterns**. Pick by use case:

### Pattern A — engine journal as outbox source

Use `RetrieveReceivedMessage` directly. No second durable store needed; the engine's `FilesystemStorageFactory` is already the source of truth — **but only with one non-default property set**:

```properties
# REQUIRED for Pattern A. Default is false — the incoming journal is then a flat file,
# and RetrieveReceivedMessage throws IOException ("Message retrieval is not possible for flat files!").
incomingStorageIndexed = true
```

With the default (`false`), the incoming storage is `FlatFileMessageStorage`, whose retrieve path throws unconditionally (verified `FlatFileMessageStorage.RetrieveMessagesImplementation`); `incomingStorageIndexed = true` switches it to `IndexedMessageStorage` (verified `FilesystemStorageFactory.CreateIncomingMessageStorage`). The OUTGOING side is indexed by default (`outgoingStorageIndexed` defaults to `true`), which is why `RetrieveSentMessage` works out of the box and `RetrieveReceivedMessage` does not.

```
OnNewMessage
   │
   ├─ note the MsgSeqNum (engine fills it on inbound)
   └─ wake the pump  ← cheap signal; durability already on disk via engine

(separate worker / pump)
   │
   ├─ for seqN = publishedThrough+1; seqN < session.InSeqNum; seqN++:    # exclusive — InSeqNum is next-expected
   │     bytes = session.RetrieveReceivedMessage(seqN)
   │     parse → DTO → publish to Kafka (transactional/idempotent)
   ├─ on commit: advance publishedThrough (atomically: write-then-rename)
   └─ on failure: retry; transactional producer + stable key (SenderCompID, seqN) dedupes
```

When to choose: simpler, one durable store, no extra dependency. DTOs aren't materialized until publish time so the outbox doesn't double-store.

Caveat — journal lifetime: `storageCleanupMode` (valid values `None | Backup | Delete`, default `None` — verified `Config.StorageCleanupMode`) backs up or deletes the message storage of **closed sessions**. If it's set to anything but `None`, the incoming log that the Pattern-A watermark points into can be rotated away on session close — the pump must tolerate `IOException` for sequence numbers no longer in storage.

### Pattern B — tee to a separate durable outbox (SQLite WAL / append-only file)

```
OnNewMessage
   │
   ├─ normalize FIX → DTO
   ├─ write DTO to dedicated durable outbox (SQLite WAL, append-only file, local KV)
   │     keyed by a monotonic counter (or the engine MsgSeqNum)
   └─ return  ← session listener exits fast

(separate worker)
   │
   ├─ read outbox
   ├─ publish to Kafka (transactional/idempotent producer)
   ├─ on success, advance outbox watermark
   └─ on failure, retry with backoff
```

When to choose: a **DTO-shaped** durable record (post-normalization) is desired, or the audit log must outlive the engine's session-storage lifecycle (which can rotate / be reset by operator action), or one outbox tees to multiple downstreams.

## Exactly-once into Kafka

The combination that works:

1. **Tee in `OnNewMessage`** — write `(my-monotonic-id, DTO, status=pending)` to the local durable outbox **synchronously, before returning**. Know exactly what throwing does: the engine logs a warning and **rethrows** (verified `CompositeMessageHandler`), and the rethrown exception reaches the reader thread's catch-all, which shuts the session down (verified `MessageReader`). A throw is a crash-stop, not a redelivery request — there is no engine-level retry of `OnNewMessage`. "If I can't record it, stop the session" can be an acceptable strategy, but adopt it deliberately.
2. **Kafka producer = idempotent + transactional.** `EnableIdempotence=true`, set `TransactionalId`, `Acks=All`. The idempotence key downstream is the monotonic id (or a stable business id like `ExecID`).
3. **Background pump.** Reads `status=pending` rows in order, opens a Kafka transaction, produces, commits, then marks `status=published` (or deletes). On crash mid-transaction, `InitTransactions()` at next start fences the prior producer and aborts in-flight; pump re-reads `pending` rows and republishes — Kafka idempotence + the stable key handle the duplicate at the broker level.
4. **No business state in the listener.** `OnNewMessage` does normalize → tee → return. The OMS state machine, the Kafka producer, all the moving parts live on the pump side or further downstream.

### Ordering with respect to the FIX session — the "catch-up before logon" pattern

If downstream consumers must see ExecutionReports in venue-sent order across process restarts (i.e. a stale restart cannot emit `E_new` before pre-crash `E_old`), `session.Connect()` must NOT be called until the outbox pump has drained the previous-run backlog to Kafka.

The pattern depends on which outbox style is in use:

**Pattern A (engine journal):** Catch-up reads from `session.RetrieveReceivedMessage` for each `seqN` from the persisted `publishedThrough` watermark up to (and including) `session.InSeqNum - 1` AS OBSERVED BEFORE `Connect()` — `InSeqNum` is the next-expected seq number, so the last *received* one is `InSeqNum - 1`. The engine's persisted `InSeqNum` survives restart (`forceSeqNumReset=Never`), so it's the right ceiling.

**Pattern B (separate SQLite outbox):** Catch-up drains all `status=pending` rows from the SQLite store before `Connect()`.

```csharp
var session = parameters.CreateInitiatorSession();            // build but DO NOT Connect
session.Init();                                                // create the storages — REQUIRED before Retrieve*
await outbox.CatchUpAsync(session);                            // drain backlog → Kafka
session.Connect();                                             // NOW allow new inbound
```

`Init()` is public on `IFixSession` ("Initialize FIX session. This allows to put messages to session..."). It matters for Pattern A: the incoming/outgoing storages are only created inside `Init()` (or on connect — verified `AbstractFixSession.InitSessionInternal`), so calling `RetrieveReceivedMessage` on a freshly built, un-inited session throws `NullReferenceException`. `session.InSeqNum` itself is loaded from the persisted parameters when the session object is constructed and IS readable before `Init()`/`Connect()`.

Why this matters: if `Connect()` runs first and the pump runs in parallel, a fresh inbound `E_new` from the venue lands AS A NEW JOURNAL ENTRY (Pattern A) or AS A NEW PENDING ROW (Pattern B) and races the pre-crash backlog to Kafka — whichever publishes first wins, and once the Kafka offset advances past `E_new` the correct order for the older events cannot be recovered.

There is **no documented engine "before-logon" hook** (verified absent in `IFixSession`). Ordering must be enforced in `Main()`, not via a listener callback.

## Normalization

The DTO should be:
- **Bus-friendly.** JSON, Avro, or Protobuf. Schema-versioned.
- **FIX-agnostic.** No `MsgType`, no tag numbers in downstream contracts. Use domain names: `executionId`, `clientOrderId`, `symbol`, `side`, `lastQuantity`, `lastPrice`, etc.
- **Idempotent.** Carry a stable unique ID (`ExecID` for executions, `ClOrdID` for orders) so consumers can dedup.
- **Timestamped.** Include both `transactTime` (from FIX) and a normalizer-side ingest timestamp.

## Backpressure

If Kafka is slow:
1. The outbox grows. That's the desired behavior — trade memory/disk for not blocking FIX.
2. Once the outbox crosses a threshold, alert. Investigate why downstream is slow.
3. Do NOT drop FIX messages or stop the session to "catch up." Once dropping starts, the regulatory trail is lost.

If Kafka is down for longer than the outbox can hold:
1. Page operations.
2. Consider degraded mode: persist to file, replay later. The FIX session must keep running.

## Ordering

Per-session, per-instrument ordering usually matters. Strategies:
- **Kafka key = ClOrdID** — preserves order per order, but each order's events may land on different partitions.
- **Kafka key = symbol** — preserves order per instrument; partition skew possible.
- **Kafka key = session ID** — strongest ordering but limits parallelism downstream.

Pick based on consumer needs. Document it.

## Common LLM mistakes

1. **Publishing directly from `OnNewMessage`.** Blocks the session. Use outbox.
2. **Skipping the outbox "because Kafka has retries."** Kafka retries don't survive process crash. The outbox does.
3. **Forwarding raw FIX wire format.** Brittle. Downstream consumers shouldn't parse FIX.
4. **No idempotency key.** Consumers can't dedup on Kafka offset alone after a producer retry. Include `ExecID` / `ClOrdID`.
5. **Single partition for ordering "just in case."** Throughput cliff. Choose a key that preserves the ordering actually needed.
6. **Mutating the message after publish.** DTOs in the outbox must be immutable.
7. **Acking the inbound FIX message before outbox write.** If outbox write fails and the ack already happened, the message is lost.

## Operational concerns

| Concern | Mitigation |
|---|---|
| Schema evolution | Schema registry (Confluent / Glue). Backward-compatible changes only. |
| Sensitive data | Mask account/PII at normalize step if downstream consumers shouldn't see it. |
| Replay | Outbox should support "republish from time T" for downstream reprocessing. |
| Observability | Lag metrics on outbox; publish latency p99; FIX → Kafka end-to-end latency. |

## See also

- `04-drop-copy-consumer.md` — the inbound side of the bridge.
- `06-multi-session-router.md` — outbox helps here too, when fan-out is across many sessions.

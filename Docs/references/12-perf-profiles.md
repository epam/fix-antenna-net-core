# 12 Performance Profiles: Throughput vs Latency

> Targets **Epam.FixAntenna.NetCore 1.2.3**. See `../SKILL.md` for root API rules.

## Pattern

The same FIX session code, run under two different tuning profiles:

| Profile | Goal | Trade-off |
|---|---|---|
| **Throughput** | Max msgs/sec | Higher tail latency; batching, larger queues, more concurrency. |
| **Latency** | Min p99 / p99.9 | Lower throughput; pre-allocated, pinned, minimal GC. |

This reference extends the existing `Samples/Latency/` reference (Sender + Server) with explicit profile configurations.

## When to use

- Latency-sensitive trading paths (HFT, market-making) → latency profile.
- Drop-copy ingest, post-trade pipelines, bulk reconciliation → throughput profile.

## Throughput profile — what to change

| Knob | Setting |
|---|---|
| GC | `Server` GC, concurrent. `<ServerGarbageCollection>true</ServerGarbageCollection>`, `<ConcurrentGarbageCollection>true</ConcurrentGarbageCollection>`. |
| `OnNewMessage` work | Offload to a queue. Listener thread does parse + dispatch only. |
| Socket buffer sizes | `tcpReceiveBufferSize`, `tcpSendBufferSize` in `fixengine.properties` (verified `Config.TcpReceiveBufferSize` / `Config.TcpSendBufferSize`). |
| Nagle / write coalescing | `enableNagle = true` for throughput (engine coalesces small writes). **This is the engine default** (`[DefaultValue("true")]` on `Config.EnableNagle`) — so no change needed for a throughput profile. |
| Send batching | `maxMessagesToSendInBatch` larger (50–500, default 10); `preferredSendingMode = async` (queues + coalesces — the throughput-favored mode; default is `sync`); `queueThresholdSize = 0` (the default — queue control off, the pumper thread is never paused). |
| Storage | Pick `MmfStorageFactory` if write latency matters; `FilesystemStorageFactory` otherwise. Replicate the storage directory at the OS / filesystem layer for HA — `IStorageFactory` is internal in 1.2.3 (see `08-custom-storage-replication.md`). |
| Threading | One thread per session is fine; for many sessions, share a worker pool. |
| Logging | Async logger (e.g., NLog/Serilog async sinks). Synchronous logging is a throughput killer. |

## Latency profile — what to change

| Knob | Setting |
|---|---|
| GC | `Server` GC + `GCLatencyMode.SustainedLowLatency` during trading windows. |
| Object allocation | Pre-allocate buffers, message objects, listener state. Avoid LINQ in hot path. Avoid `string` concatenation. Avoid boxing. |
| Hot path | Pin one thread per session to a dedicated CPU core (see below). |
| `OnNewMessage` work | **Inline the critical path** — do not enqueue. Enqueue only the bookkeeping that can wait. |
| Logging | OFF on hot path. Sample only. |
| Sockets | `enableNagle = false` to turn TCP_NODELAY ON. **NOT the engine default** — default is `enableNagle = true` (Nagle on, throughput-favored). Set `enableNagle = false` explicitly for the latency profile. Also set OS-level NIC interrupt affinity. |
| Send batching | `maxMessagesToSendInBatch = 1`, `preferredSendingMode = syncNoqueue` — send from the user thread, no outbound queue, no coalescing (lowest single-message latency; also forces the queue object in-memory). Alternative (used by the vendor's own Latency sample, which keeps the default `sync` mode): `queueThresholdSize = 1` + `inMemoryQueue = true` — see "What the vendor's own latency benchmark sets" below. |
| Queue durability | With the default `FilesystemStorageFactory` + `inMemoryQueue = false` (the default), **every queued send is written to a persistent `.outq` file before going to the wire** — this dominates send latency. `inMemoryQueue = true` skips the disk write (faster, queued messages can be lost on crash). For `MmfStorageFactory` the analogous knob is `memoryMappedQueue` (default `true`). |
| Storage | `storageFactory = Epam.FixAntenna.NetCore.FixEngine.Storage.MmfStorageFactory` (memory-mapped) for lowest write latency — **target x64**: x86 fails with `IOException: Not enough memory resources are available to process this command.` Replicate at OS / filesystem layer on a separate volume if HA is needed (see `08-custom-storage-replication.md`). |

### Socket / network

| Property | `Config.*` constant | Effect |
|---|---|---|
| `tcpReceiveBufferSize` | `Config.TcpReceiveBufferSize` | Socket SO_RCVBUF in bytes. |
| `tcpSendBufferSize`    | `Config.TcpSendBufferSize`    | Socket SO_SNDBUF in bytes. |
| `enableNagle`          | `Config.EnableNagle`          | Nagle's algorithm toggle. **Default is `true`** (Nagle on, TCP_NODELAY off, throughput-favored) — `[DefaultValue("true")]` on `Config.EnableNagle`, also observable in runtime session-params dump. Set to `false` explicitly for the latency profile (TCP_NODELAY on, no small-write coalescing). The engine's source comment is the authority: `"With enabled Nagle's algorithm will be better throughput (TcpNoDelay=false) but with disabled option you will get better result for latency on single message (TcpNoDelay=true)"`. |

### Send batching / queue depth

| Property | `Config.*` constant | Effect |
|---|---|---|
| `maxMessagesToSendInBatch`        | `Config.MaxMessagesToSendInBatch`         | Engine-level send batching: max messages in the buffer before the engine writes to transport. Default **10**; must be > 0. **Latency** profile: small (1–5). **Throughput** profile: larger (50–500). |
| `preferredSendingMode`            | `Config.PreferredSendingMode`             | Sending mode — exactly three values (enum `SendingMode`), default **`sync`**: `Async` (everything sent asynchronously via the queue — throughput-favored), `Sync` (optimized to send from the user thread *when possible*, but can still operate asynchronously and add messages to the internal queue — NOT "caller blocks until queued"), `SyncNoqueue` (sends only from the user thread, no internal queue; cannot send to a disconnected session; forces an in-memory queue object). There is **no** `queued` value. |
| `queueThresholdSize`              | `Config.QueueThresholdSize`               | **Queue control, not slow-consumer detection**: max messages in the outbound queue before the engine pauses the *pumper thread* to let the queue drain. Default **0 = queue control disabled** (pumper never paused). Set 1 / very low for real-time behavior (the vendor's Latency sample sets `1`); leave 0 / set high for max throughput. |
| `waitForMsgQueuingDelay`          | `Config.WaitForMsgQueuingDelay`           | **Backpressure on the sender, not a flush timer**: max time the *sending (user) thread* is paused when the internal queue is full; if the queue is still full after the delay, the message is pushed to the queue anyway. Default **1000** ms. Irrelevant while the queue has room — it does not delay or batch normal sends. |
| `inMemoryQueue`                   | `Config.InMemoryQueue`                    | Queue durability for `FilesystemStorageFactory`: default **false** = persistent queue (slower, no messages lost), `true` = in-memory queue (faster, queued messages may be lost). |
| `memoryMappedQueue`               | `Config.MemoryMappedQueue`                | Queue durability for `MmfStorageFactory`: default **true** = memory-mapped queue (faster, less safe), `false` = persistent. |
| `slowConsumerDetectionEnabled`    | `Config.SlowConsumerDetectionEnabled`     | Turn on slow-consumer detection in the pumpers (default `false`). This — not `queueThresholdSize` — is the slow-consumer mechanism. |
| `slowConsumerWriteDelayThreshold` | `Config.SlowConsumerWriteDelayThreshold`  | Max timeframe for sending a message; if the transport can't send within it, the engine notifies about a slow consumer. Default **10**. |

### Threading affinity

| Property | `Config.*` constant | Effect |
|---|---|---|
| `cpuAffinity`     | `Config.CpuAffinity`     | Fallback core pin for **both** of a session's I/O threads (used where the send/recv-specific keys are unset). |
| `sendCpuAffinity` | `Config.SendCpuAffinity` | Core pin for the session's **sending (pumper) thread**. |
| `recvCpuAffinity` | `Config.RecvCpuAffinity` | Core pin for the session's **receiving (reader) thread**. |

> All three are read from the **per-session** configuration — the session's pumper and reader threads apply affinity from the session's own parameters (verified `AsyncMessagePumper` / `SyncMessagePumper` / `SyncBlockingMessagePumper` / `MessageReader`, all calling `ApplyAffinity` with `_sessionParameters.Configuration`). So `sessions.<id>.cpuAffinity` **is** supported; an unprefixed (top-level) key acts as the default inherited by every session.
>
> ⚠️ A **global** (top-level) `cpuAffinity` pins every session's I/O threads to ONE core — on an acceptor terminating many sessions in one process that creates a hot spot. Scope affinity per session (`sessions.<id>.*`), or leave the global key unset and let the OS scheduler place threads.

### What the vendor's own latency benchmark sets

The shipped latency harness config (`Samples/Latency/Sender/fixengine.properties` in the 1.2.3 source) is the engine authors' own "minimum latency" tuning — useful as a checklist of what *they* disable (all lines verbatim from that file):

```properties
inMemoryQueue = true                # no on-disk queue write per send
validation = false                  # message validation off       (default: false)
queueThresholdSize = 1              # their comment: "set queue size to 1 to get more correct result"
storageFactory = Epam.FixAntenna.NetCore.FixEngine.Storage.InMemoryStorageFactory   # no durable storage at all
enableNagle = false                 # TCP_NODELAY on
validateCheckSum = false            # checksum validation off      (default: true)
validateGarbledMessage = false      # garbled-message check off    (default: true)
markIncomingMessageTime = true      # timestamp at socket read, for measurement
cpuAffinity = 0                     # pinned (single-session benchmark process)
```

A production latency profile usually cannot copy this wholesale — `InMemoryStorageFactory` means no recovery after a crash — but the validation toggles (`validateCheckSum` / `validateGarbledMessage`) and `enableNagle = false` carry over directly. For durable-but-fast storage use `storageFactory = Epam.FixAntenna.NetCore.FixEngine.Storage.MmfStorageFactory` instead (memory-mapped storage; listed as a valid `storageFactory` value in the official configuration docs — target x64, see the Storage row above).

### What still does NOT exist (verified absent)

A short list of names that aren't real, despite being plausible:
- `tcpNoDelay` — there's `enableNagle` instead (logical inverse: `enableNagle=false` ≡ TCP_NODELAY on). The engine default for `enableNagle` is **`true`** (Nagle on, throughput-favored) — set `enableNagle=false` explicitly for low-latency profiles.
- `dispatchMode` / `singleThreaded` / `inlineDispatch` — threading model is fixed in 1.2.3.
- `messagePoolSize` / `enableMessagePooling` — `FixMessage` reuse / `ReleaseInstance` is the application's responsibility in hot-path code.

If a property isn't in the tables above, check `Config.cs` (~140 constants) before assuming it exists.

## CPU affinity / core pinning

The three CPU affinity properties (`Config.CpuAffinity`, `Config.SendCpuAffinity`, `Config.RecvCpuAffinity`) are per-session settings: set them top-level as a process-wide default, or scope them with the `sessions.<id>.` prefix:

```properties
# top-level = default for every session (fine for a single-session process)
cpuAffinity = 3           # both I/O threads of each session pinned to core 3

# multi-session process: pin per session instead
sessions.hot1.recvCpuAffinity = 4    # hot1's reader (receiving) thread → core 4
sessions.hot1.sendCpuAffinity = 5    # hot1's pumper (sending) thread → core 5
```

For pinning at the OS process / .NET thread level (when finer control than the engine's per-session settings is needed), do it in hosting code:

### .NET on Linux

Pin the whole process at launch via `taskset` — simplest and avoids P/Invoke:

```bash
taskset -c 3 dotnet ./MyTradingApp.dll
```

For per-thread pinning, call `sched_setaffinity` via P/Invoke. Sketch:

```csharp
[DllImport("libc", SetLastError = true)]
private static extern int sched_setaffinity(int pid, IntPtr cpusetsize, ref ulong mask);

// from inside the thread to pin (pid=0 means current thread on Linux):
ulong mask = 1UL << 3;            // core 3
sched_setaffinity(0, (IntPtr)sizeof(ulong), ref mask);
```

### .NET on Windows

`Thread.ProcessorAffinity` is not available — use `ProcessThread` on the OS-level thread, looked up via its native ID:

```csharp
var proc = Process.GetCurrentProcess();
foreach (ProcessThread t in proc.Threads)
{
    if (t.Id == myTradingThreadId)            // OS thread id, not managed id
        t.ProcessorAffinity = (IntPtr)(1 << 3); // core 3
}
```

Capture `myTradingThreadId` inside the thread to pin via `AppDomain.GetCurrentThreadId()` / `GetCurrentThreadId()` (kernel32).

### Recommendations

- Pin one core per latency-critical session.
- Reserve the chosen cores by setting OS-level CPU isolation (`isolcpus` boot param on Linux, processor affinity for the process group on Windows).
- Avoid pinning to core 0 — OS interrupts land there.
- NUMA: pin all session threads to cores on the same NUMA node as the NIC.

## Measurement

Tuning requires measurement. Required metrics:

| Metric | How |
|---|---|
| End-to-end latency (wire-in → app-out) | Timestamp on `OnNewMessage` entry; timestamp on `SendMessage` return; aggregate p50/p99/p99.9. |
| Throughput (msgs/sec) | Counter per session. |
| GC pauses | `dotnet-counters` / EventPipe. |
| Allocations per message | dotnet-trace allocation profiler. |
| Heartbeat slip | Time between scheduled and actual heartbeats. |

Use the existing `Samples/Latency/Sender/` + `Samples/Latency/Server/` as the baseline harness.

## Common LLM mistakes

1. **Optimizing without measuring.** "Faster" code that allocates more is slower. Always profile.
2. **Using `Task.Run` on the hot path.** Adds thread-pool indirection, allocations, scheduling jitter.
3. **`async / await` everywhere.** Async is great for I/O-bound work, bad for the latency hot path (capture, state machine boxing).
4. **`string.Format` / interpolation in the hot path.** Allocates. Use `Span<char>`, `StringBuilder`, or pre-formatted templates.
5. **Logging every message at `Info`.** A 100k msg/sec stream + sync logger = stalls.
6. **Workstation GC.** Default on .NET Framework / older configs. Switch to Server GC for any FIX workload.
7. **Pinning the main thread.** Pin session/listener threads instead.
8. **Sharing one CPU core across two latency-critical sessions.** They starve each other.

## Profile A vs Profile B — config-driven

Make the profile a config setting, not a code branch. The trading-hot path doesn't have `if (profile == Latency) ...` in it. Instead:
- Different `fixengine.properties` per profile.
- Different launch scripts (set GC vars, set CPU affinity).
- Same code.

## See also

- `08-custom-storage-replication.md` — storage choice dominates many tuning decisions.
- `06-multi-session-router.md` — threading model interacts with router design.

# 08 Storage Choices & Replication ★

> Targets **Epam.FixAntenna.NetCore 1.2.3**. See `../SKILL.md` for root API rules.

> ⚠️ **In 1.2.3, `IStorageFactory` and `IMessageStorage` are `internal`** to the engine assembly. `InternalsVisibleTo` exposes them only to EPAM's own test / tooling assemblies — not to customer code. **Customer projects cannot implement these interfaces.** Pluggable storage in 1.2.3 means **picking one of the shipped factories**; HA / replication is achieved at the OS or filesystem layer below the engine.

## Pattern

Two decisions for any production session:

1. **Which shipped storage factory.** Match durability and throughput to workload.
2. **How to replicate the storage** (if cross-host failover or DR is needed). The engine's storage is files on disk — replicate those.

```
[engine] ──writes──► [shipped IStorageFactory impl] ──► storage files on disk
                                                              │
                                                              ▼
                                              [OS-level replication]
                                              DRBD / cluster FS / NAS / MMF mirror
```

## Shipped factories (verified in 1.2.3 source: `FixAntenna/NetCore/FixEngine/Storage/`)

| Factory FQN | Use when |
|---|---|
| `Epam.FixAntenna.NetCore.FixEngine.Storage.FilesystemStorageFactory` | **Default. Production.** Durable file-backed storage. Use unless there's a specific reason not to. |
| `Epam.FixAntenna.NetCore.FixEngine.Storage.MmfStorageFactory` | Memory-mapped file. Lower write latency than plain filesystem; still durable. Pick when latency p99 matters and the MMF storage path can be verified against the target kernel/disk. |
| `Epam.FixAntenna.NetCore.FixEngine.Storage.SlicedFileStorageFactory` | Rotated / sliced files. Pick when individual log files would grow unmanageably large within a session lifetime. |
| `Epam.FixAntenna.NetCore.FixEngine.Storage.InMemoryStorageFactory` | **Tests only.** Loses everything on restart; counterparty sees seq drop and rejects logon. Never production. |

Set via the `storageFactory` property:

```properties
storageFactory   = Epam.FixAntenna.NetCore.FixEngine.Storage.FilesystemStorageFactory
storageDirectory = ./logs
```

> ⚠️ The property value is a fully-qualified type name resolved by the engine at runtime. On a misspelling the engine logs a WARN — `"Can not load storage factory: … Loaded default FilesystemStorageFactory."` — and falls back to `FilesystemStorageFactory` (`ReflectStorageFactory.cs`). So a misspelled `MmfStorageFactory` costs you the MMF latency profile, **not** durability. Watch the engine log on first run and verify the intended factory actually loaded.

## What's on disk — scope the replication correctly

All sessions share `storageDirectory`; file names are per-session, built from templates (`{0}` = sessionID, `{3}` = timestamp — verified in `Config.cs` and `FilesystemStorageFactory.cs`):

| Template property | Default | Content |
|---|---|---|
| `outgoingLogFile` | `{0}.out` | Outbound wire log — the replay source for resend. |
| `incomingLogFile` | `{0}.in` | Inbound wire log (audit). |
| `sessionInfoFile` | `{0}.properties` | Session parameters + sequence numbers. |
| `outgoingQueueFile` | `{0}.outq` | Persistent outgoing queue. |
| `backupOutgoingLogFile` / `backupIncomingLogFile` | `{0}-{3}.out` / `{0}-{3}.in` | Timestamped backups, written under `storageBackupDir` (default `${fa.home}/logs/backup`). |

Two replication-scoping consequences:

- **`.outq` is part of the durability story.** Replicating only `.in`/`.out` misses queued-but-unsent messages. (The persistent queue is bypassed entirely when `inMemoryQueue=true` or `preferredSendingMode=SyncNoqueue` — the factory then builds an in-memory queue, `FilesystemStorageFactory.cs`.)
- **`storageBackupDir` is a separate directory.** If audit continuity across sequence resets matters, replicate it too.

## When HA / replication is needed

The engine doesn't ship a "replicated storage" factory. The supported paths:

| Approach | Mechanism | Trade-offs |
|---|---|---|
| **Shared storage** | NAS / SAN mounted on standby host. Failover means re-mounting on the standby and starting the engine. | Single point of failure unless the NAS itself is redundant. Recovery time is mount + start. |
| **Block-level replication** | DRBD, LVM mirroring, ZFS send/recv to a standby. | Storage replication is below the engine. Engine on primary owns the files; standby is read-only until promoted. |
| **Cluster filesystem** | GFS2, OCFS2 — shared across cluster nodes. | Multiple nodes can read; only the active engine writes (engine doesn't support concurrent multi-host writes to the same session storage). |
| **MMF mirror** | If using `MmfStorageFactory`, mirror the backing files. | Same constraints as block-level. |
| **App-level audit replication** | Tee the wire traffic into a Kafka bridge (`10-fix-to-kafka-bridge.md`) for downstream audit / compliance. | Doesn't help session recovery — engine still needs its own storage files on disk. Solves the *business audit* problem, not the *FIX session recovery* problem. |

For most production deployments, **shared storage + active-standby + engine restart on failover** is sufficient. The session reconnects on the standby; counterparty either accepts the resume (with sequence numbers intact) or negotiates a sequence reset.

## The durability invariant — still applies

Even without writing the storage layer, code depends on it. Understand the contract:

```
engine writes outbound → factory's IMessageStorage persists it
                       → engine releases to TCP only after the write returns
                       → on restart, sequence numbers and outbound log resume
```

If the storage directory is mounted on a path where writes are async (e.g., a slow NFS without sync mounts, a network filesystem with caching), the durability contract weakens silently. Test:

1. Kill the process mid-write under load. On restart, every outbound message the counterparty saw must be present in the outbound log.
2. Same test, but kill the host (power-off VM). Same expectation.

If either fails, the storage substrate is lying about durability — not the engine's fault, but yours to fix.

## Custom factory — not supported in 1.2.3

For a hard requirement on a custom factory (e.g., write to S3, write to a remote KV store, transparent encryption-at-rest beyond what the OS gives), the available paths are:

1. **Request EPAM** expose `IStorageFactory` and `IMessageStorage` as `public` in a future release (or add the consuming assembly to `InternalsVisibleTo`).
2. **Fork the engine source** and change the access modifiers. Carries the obvious maintenance cost.
3. **Implement at the OS layer**: FUSE filesystem, layered block device, kernel-level encryption (LUKS, dm-crypt). The engine sees a normal filesystem; the OS does the work.

Any doc or sample claiming to "implement `IStorageFactory`" against the public 1.2.3 NuGet is wrong — the interface isn't accessible.

## Common LLM mistakes

1. **Inventing a custom `IStorageFactory` implementation against the public NuGet.** It won't compile — the interface is `internal`. Use a shipped factory; replicate at OS layer.
2. **Picking `InMemoryStorageFactory` because it's faster.** Loses recovery. The right answer is `MmfStorageFactory` if filesystem write latency is the problem.
3. **Pointing `storageDirectory` at an asynchronously-replicated filesystem and calling it "HA."** Async replication can lose the last N writes on crash. Use sync replication or shared storage for zero-loss failover.
4. **"Each session needs its own `storageDirectory`."** Wrong — sharing one `storageDirectory` across all sessions is the design: file names are per-session templates (`{0}.out` / `{0}.in` / `{0}.properties` / `{0}.outq`, `{0}` = sessionID — `Config.cs`, `FilesystemStorageFactory.cs`). The real hazards are *duplicate session IDs* (two sessions resolving to the same file names) and two *processes* pointed at the same directory for the same session.
5. **Running two engines against the same storage on the same counterparty.** Active-active is not supported. Active-standby with failover is the supported pattern.
6. **Treating Kafka-bridge audit as session-recovery replication.** Different problems. Kafka audit answers "what trades were observed?"; session storage answers "what FIX sequence number does the session resume at?"

## Testing the storage choice

1. **Process kill mid-write under load** → restart, verify counterparty resume succeeds without gaps.
2. **Host power-loss** → same.
3. **Disk full** → engine should surface error via session state callback, not silently drop.
4. **10k-message resend** → confirm replay completes in < 1 second. If not, switch to `MmfStorageFactory`.

## See also

- `07-persistence-recovery.md` — the recovery flows the storage factory enables.
- `06-multi-session-router.md` — correlation tables are *application* state needing *application*-level durability (a separate concern from session storage).
- `10-fix-to-kafka-bridge.md` — for business audit, not session recovery.

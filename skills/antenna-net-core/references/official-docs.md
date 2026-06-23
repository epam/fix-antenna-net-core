# Official upstream documentation — index

Canonical, maintainer-written docs for **`Epam.FixAntenna.NetCore`**, living in the
repository's `Docs/` folder. This file is the authoritative map of those pages.

**This is the fallback tier, not the first stop.** The bundled `references/*.md` are
pre-verified against 1.2.3 and answer most questions without any web access — check
them (and `SKILL.md`) first. Fetch an upstream page only when the topic is genuinely
not covered locally, or when you need to confirm behavior in a newer engine version.
When you do need one, use the page map below to fetch the single right page rather
than browsing the repository.

- Docs root: <https://github.com/epam/fix-antenna-net-core/tree/main/Docs>
- All links below target the `main` branch (`blob/main/Docs/<file>`). If a project
  pins an older release tag, swap `main` for that tag to read the version-matched doc.

> These are **upstream** docs (FIX Antenna 2.x `main`). This skill is verified against
> **1.2.3** — where the skill's `references/*.md` and the upstream doc disagree, the
> skill's verified notes win for 1.2.3, and the divergence is usually called out in
> `SKILL.md` (e.g. the `IFixSessionListener` / `IFixMessageListener` split).

## Page map

| Upstream doc | What's in it | Load when… |
|---|---|---|
| [QuickStart.md](https://github.com/epam/fix-antenna-net-core/blob/main/Docs/QuickStart.md) | First acceptor + initiator, end to end. | Bootstrapping a project from zero. Pairs with `references/01-minimal-acceptor.md`, `references/02-minimal-initiator.md`. |
| [BasicConcepts.md](https://github.com/epam/fix-antenna-net-core/blob/main/Docs/BasicConcepts.md) | Engine vocabulary: sessions, messages, storage, listeners. | Verifying the "Mental model" section of `SKILL.md`. |
| [Backgrounder.md](https://github.com/epam/fix-antenna-net-core/blob/main/Docs/Backgrounder.md) | FIX protocol primer and engine positioning. | Onboarding; FIX terminology. Pairs with `references/glossary.md`. |
| [Configuration.md](https://github.com/epam/fix-antenna-net-core/blob/main/Docs/Configuration.md) | Full `fixengine.properties` key reference. | Rarely — the complete verified 1.2.3 key index is bundled in `references/config-keys.md`; fetch this page only for long-form prose on a key already found there. |
| [FixSession.md](https://github.com/epam/fix-antenna-net-core/blob/main/Docs/FixSession.md) | `IFixSession` lifecycle, state, send/receive. | Anything touching session state, `Connect`/`Disconnect`, listeners. |
| [FixSessionAcceptor.md](https://github.com/epam/fix-antenna-net-core/blob/main/Docs/FixSessionAcceptor.md) | `FixServer`, `IFixServerListener`, inbound accept/reject. | Pairs with `references/01-minimal-acceptor.md`, `references/06-multi-session-router.md`. |
| [FixSessionInitiator.md](https://github.com/epam/fix-antenna-net-core/blob/main/Docs/FixSessionInitiator.md) | Outbound connect, logon, reconnect. | Pairs with `references/02-minimal-initiator.md`, `references/03-order-entry-client.md`. |
| [FixMessage.md](https://github.com/epam/fix-antenna-net-core/blob/main/Docs/FixMessage.md) | `FixMessage`/`ITagList` read & write API. | **Ground truth for `AddTag` vs `Set` vs `UpdateValue`** and tag-typed accessors. |
| [FixPreparedMessage.md](https://github.com/epam/fix-antenna-net-core/blob/main/Docs/FixPreparedMessage.md) | `PrepareMessage` / prepared-message fast path. | Hot-path send construction; `references/12-perf-profiles.md`. |
| [RepeatingGroupApi.md](https://github.com/epam/fix-antenna-net-core/blob/main/Docs/RepeatingGroupApi.md) | Repeating-group build & iterate API. | **Ground truth for `IndexRepeatingGroup` / `AddRepeatingGroup`.** Backs the repeating-group pitfall in `SKILL.md`. |
| [Validation.md](https://github.com/epam/fix-antenna-net-core/blob/main/Docs/Validation.md) | Dictionary validation behavior & config. | The "Use the dictionary" DO rule; `references/05-custom-dictionary.md`. |
| [Recovery.md](https://github.com/epam/fix-antenna-net-core/blob/main/Docs/Recovery.md) | Resend, gap fill, sequence reset, restart. | **Ground truth for recovery.** Backs `references/07-persistence-recovery.md`. |
| [Scheduler.md](https://github.com/epam/fix-antenna-net-core/blob/main/Docs/Scheduler.md) | Time-windowed sessions, Quartz cron, TZ. | Backs `references/11-scheduled-sessions.md` and the scheduled-session gap in `SKILL.md`. |
| [TlsSupport.md](https://github.com/epam/fix-antenna-net-core/blob/main/Docs/TlsSupport.md) | TLS config, `sslProtocol`, CA certs. | **Ground truth for the TLS 1.3 spelling note.** Backs `references/09-tls-secure-session.md`. |
| [MonitoringAndAdministration.md](https://github.com/epam/fix-antenna-net-core/blob/main/Docs/MonitoringAndAdministration.md) | Admin/monitoring endpoints & tooling. | Rarely — covered locally by `references/13-admin-monitoring.md` (note: that doc's `autostart.acceptor.command.package` is a typo; the real key is `commands.package`). |
| [TagsGen.md](https://github.com/epam/fix-antenna-net-core/blob/main/Docs/TagsGen.md) | Tag-constant generation per FIX version. | The `Epam.FixAntenna.Constants.*` tag classes; `references/05-custom-dictionary.md`. |
| [InstallationAndUninstallation.md](https://github.com/epam/fix-antenna-net-core/blob/main/Docs/InstallationAndUninstallation.md) | NuGet install / setup. | Project setup. |
| [OtherTopics.md](https://github.com/epam/fix-antenna-net-core/blob/main/Docs/OtherTopics.md) | Misc. topics not covered elsewhere. | Catch-all; check before concluding a feature is absent. |
| [features.md](https://github.com/epam/fix-antenna-net-core/blob/main/Docs/features.md) | Feature matrix. | Checking whether a capability exists. |
| [benchmarking.md](https://github.com/epam/fix-antenna-net-core/blob/main/Docs/benchmarking.md) | Benchmark methodology & numbers. | `references/12-perf-profiles.md`. |
| [latency_sample.md](https://github.com/epam/fix-antenna-net-core/blob/main/Docs/latency_sample.md) | Latency-measurement sample. | Latency tuning; `references/12-perf-profiles.md`. |
| [LicenseAgreement.md](https://github.com/epam/fix-antenna-net-core/blob/main/Docs/LicenseAgreement.md) | License terms (Apache 2.0). | Licensing questions. |

## Skill reference → upstream doc cross-map

| Skill reference | Backing upstream doc(s) |
|---|---|
| `references/01-minimal-acceptor.md` | QuickStart, FixSessionAcceptor, FixSession |
| `references/02-minimal-initiator.md` | QuickStart, FixSessionInitiator, FixSession |
| `references/03-order-entry-client.md` | FixSessionInitiator, FixMessage, RepeatingGroupApi |
| `references/04-drop-copy-consumer.md` | FixSession, FixMessage, Validation |
| `references/05-custom-dictionary.md` | Validation, TagsGen |
| `references/06-multi-session-router.md` | FixSessionAcceptor, FixSession, Configuration |
| `references/07-persistence-recovery.md` | Recovery, Configuration |
| `references/08-custom-storage-replication.md` | Configuration, OtherTopics |
| `references/09-tls-secure-session.md` | TlsSupport, Configuration |
| `references/10-fix-to-kafka-bridge.md` | FixSession, FixMessage |
| `references/11-scheduled-sessions.md` | Scheduler, Configuration |
| `references/12-perf-profiles.md` | benchmarking, latency_sample, FixPreparedMessage, Configuration |
| `references/13-admin-monitoring.md` | MonitoringAndAdministration |
| `references/api-reference.md` | FixSession, FixMessage, RepeatingGroupApi, Configuration |
| `references/config-keys.md` | Configuration |
| `references/glossary.md` | Backgrounder, BasicConcepts |

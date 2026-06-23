# 11 Scheduled Sessions

> Targets **Epam.FixAntenna.NetCore 1.2.3**. See `../SKILL.md` for root API rules.

## Pattern

Sessions that are only active during defined windows (trading hours, market open–close, weekly schedule). Engine manages connect/disconnect at boundaries. Many venues require connections to drop outside their hours.

```
schedule: Mon–Fri  08:00–17:00 ET, with daily seq reset at start
                    │
                    ▼
                 [engine: connect]
                    │
                    ... trading ...
                    │
                    ▼
                 [engine: logout, disconnect]
```

## When to use

- Connecting to an exchange with explicit trading hours.
- Sessions that should NOT consume resources off-hours.
- Daily sequence-reset workflows.

## When NOT to use

- 24×7 venues / FX / crypto markets. Use `02-minimal-initiator.md` with reconnect.
- Ad-hoc on-demand connections triggered by user action.

## Configuration (verified property keys for 1.2.3)

Scheduling is configured in `fixengine.properties` using the following **verified** keys (string constants exposed on `Config.*`):

```properties
sessionIDs = sched1

sessions.sched1.sessionType    = initiator
sessions.sched1.senderCompID   = BUYSIDE
sessions.sched1.targetCompID   = VENUE
sessions.sched1.host           = 127.0.0.1
sessions.sched1.port           = 9876

sessions.sched1.tradePeriodBegin    = 0 0 8 ? * MON-FRI      # Config.TradePeriodBegin (cron)
sessions.sched1.tradePeriodEnd      = 0 0 17 ? * MON-FRI     # Config.TradePeriodEnd
sessions.sched1.tradePeriodTimeZone = America/New_York       # Config.TradePeriodTimeZone — TimeZoneInfo ID; see "Time zone" below
sessions.sched1.forceSeqNumReset    = OneTime                # Always | OneTime | Never
sessions.sched1.performResetSeqNumTime = true               # Config.PerformResetSeqNumTime — BOOLEAN toggle (default false)
sessions.sched1.resetSequenceTime      = 00:00:00            # Config.ResetSequenceTime — the actual reset time, HH:MM:SS (default 00:00:00)
sessions.sched1.resetSequenceTimeZone  = America/New_York    # Config.ResetSequenceTimeZone — TZ for resetSequenceTime (default UTC)
```

> ⚠️ `performResetSeqNumTime` is a **boolean** (`true`/`false`, default `false`) that merely *enables* the scheduled reset — it does **not** take a time. The reset clock is `resetSequenceTime` (`HH:MM:SS`, default `00:00:00`) and its zone is `resetSequenceTimeZone` (default `UTC`). Putting a time in `performResetSeqNumTime` parses as non-`true` → reset stays off, silently.
> ⚠️ Property prefix is `sessions.<id>.<key>`. There is no nested `.schedule.` namespace.
> ⚠️ `forceSeqNumReset` values are `Always`, `OneTime`, `Never` (the enum names; parsing is case-insensitive, so `onetime` also works). `ONE_TIME` (underscore) will not parse — the engine logs `Invalid forceSeqNumReset parameter.` at Warn level and falls back to `Never` (verified `SessionParameters.ForceSeqNumReset`).

The cron dialect is **Quartz.NET** — the engine depends on the Quartz package (3.13.1) and uses its cron-expression implementation (see the Quartz cron-trigger docs for the format). `tradePeriodBegin` / `tradePeriodEnd` may each contain **several cron expressions joined with `|`** (e.g. `0 0 8 * * ?|0 0 13 * * ?` for a split day — verified `Docs/Scheduler.md` / `MultipartCronExpression`).

### Programmatic API

Building the session is not enough — **`Schedule()` is the activation call**; without it nothing ever connects:

```csharp
var sessionParams = SessionParametersBuilder.BuildSessionParameters("sched1");
var session = sessionParams.CreateScheduledInitiatorSession();   // IScheduledFixSession
session.SetFixSessionListener(listener);
session.Schedule();        // start automatic connect/disconnect per tradePeriod*.
                           // If called while already inside the window, connects immediately.

// later:
session.Deschedule();      // stop automatic management (manual Connect() still possible)
```

`IScheduledFixSession` (`Epam.FixAntenna.NetCore.FixEngine.Scheduler`) adds exactly two members to `IFixSession`: `Schedule()` and `Deschedule()` (verified).

**Acceptor side:** use `ScheduledFixServer` (same namespace, a `FixServer` subclass) instead of `FixServer`. Top-level `tradePeriodBegin` / `tradePeriodEnd` / `tradePeriodTimeZone` then apply to all accepted sessions and can be overridden per session (`sessions.<id>.tradePeriod*`). Accepted sessions are automatically disconnected at `tradePeriodEnd`; if only `tradePeriodEnd` is set (no `tradePeriodBegin`), connections are allowed at any time and dropped at the period end (verified `Docs/Scheduler.md`).

Reference samples in the FIX Antenna source repo: `Samples/SimpleScheduledClient/`, `Samples/SimpleScheduledServer/`.

## Time zone — the #1 cause of bugs

`tradePeriodTimeZone` is resolved with `TimeZoneInfo.FindSystemTimeZoneById`, with a `GMT±hh:mm` fallback parser (verified `DateTimeHelper.TryParseTimeZone`). **If the ID cannot be resolved, the engine logs one Warn line (`Using UTC time zone instead of '...'`) and silently runs the schedule in UTC** (verified `ConfigurationAdapter.TradePeriodTimeZone`) — it does not fail fast. So the value must be a `TimeZoneInfo` ID that is valid on the deployment OS/runtime (the official docs document the format as `System.TimeZoneInfo.Id`):

- `America/New_York` (IANA) — resolves on Linux, and on Windows only with .NET 6+ / ICU available.
- `Eastern Standard Time` (Windows ID) — resolves on Windows, including older runtimes (.NET Core 3.x / .NET 5 without ICU).
- `GMT+hh:mm` / `GMT-hh:mm` — resolves everywhere via the fallback parser, but is a **fixed offset: no DST**.

| Mistake | Effect |
|---|---|
| Storing schedule in UTC but venue is in local time | DST shifts misalign by an hour twice a year. |
| Using server's local time | Servers move across regions / containers. Schedule shifts. |
| Hardcoding offset (e.g., `GMT-05:00`) | Parses, but doesn't handle DST. |
| TZ ID the deployment OS can't resolve (e.g. an IANA ID on an older Windows runtime) | **Silent UTC fallback** — schedule runs hours off; the only clue is the single Warn log line. |
| Region TZ ID that resolves on the deployment OS | **Correct.** Handles DST per region's rules. |

Use a region time-zone ID that resolves on the machine the engine runs on, and verify at startup that the `Using UTC time zone instead of ...` warning is absent.

## Sequence numbers at boundaries

Two patterns:

### Pattern A — daily reset
```
End of window: Logout. Engine writes final state to storage.
Start of next window: Logon with ResetSeqNumFlag=Y (141=Y).
Both sides start fresh at 1.
```

Use when counterparty agreement says "reset daily."

### Pattern B — continuous sequence
```
End of window: Logout.
Start of next window: Logon with ResetSeqNumFlag=N. Both sides resume.
```

Use when counterparty agreement says sequence persists across windows.

The choice is **negotiated with the counterparty**. Don't guess. Read the venue's onboarding spec.

## Holiday handling

The engine's schedule is cron-based, and **1.2.3 has no native holiday-calendar or external schedule-file facility** (verified — nothing in `Config` or the `Scheduler` namespace consults a holiday source). Holiday handling is application logic on top of the scheduler:
- A wrapper that calls `Deschedule()` (or skips `Schedule()`) on holiday dates, or
- A pre-logon check that refuses to start the session when today is a holiday.

## Late-finish handling

What if the window ends but trading is in-flight? Typical policies:
- **Hard cutoff**: engine sends Logout at exactly window-end. Anything in-flight is the venue's problem.
- **Drain period**: window-end triggers "no new orders" but allows fills to land for N minutes.

FIX Antenna's scheduler handles hard cutoff out of the box. Drain is application logic on top.

## Common LLM mistakes

1. **Implementing the scheduler in app code.** Use the engine's scheduler. Hand-rolled timers fight the engine's lifecycle.
2. **Calling `Connect()` / `Disconnect()` from a `Timer`.** Same problem.
3. **Forgetting `forceSeqNumReset` when the venue requires it.** Counterparty rejects logon every morning.
4. **Sending orders outside the window.** App must check `SessionState`. Buffer or reject.
5. **Treating the disconnect at window-end as an error.** It's scheduled. `OnSessionStateChange` will fire; don't trigger reconnect logic.
6. **Cron expressions in server local time.** Use the engine's TZ setting.
7. **One schedule for all sessions.** Each session can have its own — venues have different hours.

## Reference

- `Samples/SimpleScheduledClient/` and `Samples/SimpleScheduledServer/` in the FIX Antenna repo.

## See also

- `06-multi-session-router.md` — each session may have its own schedule.
- `07-persistence-recovery.md` — storage survives across windows; sequence behavior depends on reset policy.

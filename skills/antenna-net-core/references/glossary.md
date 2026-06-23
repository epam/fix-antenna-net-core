# FIX Glossary — terms LLM agents confuse

Quick reference for FIX protocol terms that AI coding agents repeatedly misuse. Keep this short and grounded in the wire-level reality.

## Message classes

| Term | What it is | Confused with |
|---|---|---|
| **Admin message** | Session-layer message handled by the engine. Types: Logon (A), Logout (5), Heartbeat (0), TestRequest (1), ResendRequest (2), SequenceReset (4), Reject (3). | App messages. Don't write app code that constructs these. |
| **Application message** | Business-layer message. NewOrderSingle (D), ExecutionReport (8), OrderCancelRequest (F), MarketDataRequest (V), TradeCaptureReport (AE), etc. | Admin messages. |
| **Session-level reject** (3) | The peer couldn't parse/validate the outbound message structurally. | Business reject. Session reject = the local engine sent malformed FIX. |
| **Business reject** (j) | The peer parsed the message but the business logic refused it. | Session reject. Business reject = the app intent was rejected. |

## Sequence and recovery

| Term | What it is | Common mistake |
|---|---|---|
| **MsgSeqNum (34)** | Per-direction monotonic counter. Each side has its own next-expected-incoming and next-outgoing. | Treating it as a global counter, or trying to manage it manually. |
| **ResendRequest (2)** | "Send me messages N through M, I missed them." | Ignoring it. Engine will replay from storage if persistent storage is configured. |
| **Gap Fill** | A `SequenceReset (4)` with `GapFillFlag (123) = Y`, sent in *response to a resend request* for admin messages that shouldn't be replayed. | Sending gap fill for app messages. Replay app messages, gap-fill admin. |
| **Sequence Reset** | A `SequenceReset (4)` with `GapFillFlag = N`. Operational tool to force-reset counterparty's expected next seq. | Using it routinely. It's a manual intervention, not a daily mechanism. |
| **ResetSeqNumFlag (141)** on Logon | Y = reset both sides' sequences to 1 on this logon. Negotiated. | Setting Y every logon. Use only at start of trading day per agreement, or after manual recovery. |
| **PossDupFlag (43)** | "This message may have been sent before." Set by engine on replay. | Ignoring it. Use in dedup logic on the app side. |
| **PossResend (97)** | "This message may carry duplicate business content (different seq num, same intent)." Application-level dedup hint. | Confusing with PossDupFlag (which is session-level). |

## Order lifecycle

| Term | What it is | Common mistake |
|---|---|---|
| **ExecType (150)** | What THIS ExecutionReport represents: 0=New, 1=Partial, 2=Fill (deprecated), 4=Canceled, 5=Replaced, 6=Pending Cancel, 8=Rejected, F=Trade, etc. | Confusing with OrdStatus. |
| **OrdStatus (39)** | The current state of the order: 0=New, 1=Partially Filled, 2=Filled, 4=Canceled, 6=Pending Cancel, 8=Rejected, A=Pending New, E=Pending Replace. | Confusing with ExecType. They are independent fields. |
| **ClOrdID (11)** | Client-assigned order ID, must be unique per session per day per side. | Reusing across days or sessions. |
| **OrigClOrdID (41)** | On cancel/replace, the ID of the order being modified. | Omitting it on `OrderCancelRequest` / `OrderCancelReplaceRequest`. |
| **LeavesQty (151) / CumQty (14) / AvgPx (6)** | LeavesQty = remaining open. CumQty = filled so far. AvgPx = volume-weighted average fill. | Computing them locally instead of trusting the venue's ExecutionReport. |
| **Pending Cancel / Pending Replace** | Order has acknowledged the cancel/replace request but not yet processed it. | Treating as final state. The terminal state is Canceled or Replaced or Rejected. |

## Order state transitions (the legal moves)

```
                   ┌──► Partially Filled ──┐
   Pending New ──► New                     ├──► Filled
                   └──► (Pending Cancel) ──► Canceled
                   └──► (Pending Replace) ──► Replaced ──► (back to New on the new ClOrdID)
                   └──► Rejected (terminal)
```

Illegal: Filled → New. Filled → Canceled. Rejected → anything. Replaced → anything (the new ClOrdID takes over).

## Identifiers

| Term | Purpose |
|---|---|
| **SenderCompID (49)** | Sender firm/system ID at session level. |
| **TargetCompID (56)** | The counterparty's firm/system ID. |
| **OnBehalfOfCompID (115) / DeliverToCompID (128)** | Routing through a hub: original sender / final destination. |
| **ExecID (17)** | Venue-assigned, unique per execution event. Use for idempotency. |
| **OrderID (37)** | Venue-assigned order ID, often differs from ClOrdID. |

## Versions

| Version | Notes |
|---|---|
| FIX 4.0 / 4.1 | Legacy, rare in new builds. |
| **FIX 4.2** | Still common in cash equities. |
| **FIX 4.4** | Most common in OTC, FX, derivatives. |
| **FIX 5.0 / 5.0 SP2** | Uses FIXT 1.1 for session layer + app version negotiation. |
| **FIXT 1.1** | Session layer only. Always paired with an app version. |

## Pitfalls (every agent gets these wrong eventually)

1. **SOH delimiter is `0x01` (a single non-printable byte), not `|`.** `|` is the human-readable log convention.
2. **CheckSum (10)** is mod 256 of all bytes incl. trailing SOH. Engine computes it; don't.
3. **BodyLength (9)** is bytes from after SOH following `9=…` up to and including SOH before `10=`. Engine computes it; don't.
4. **SendingTime (52)** is UTC, format `YYYYMMDD-HH:MM:SS[.sss[sss[sss]]]`. Engine sets it; don't.
5. **MsgSeqNum (34)** is set by engine. Setting it from app code breaks recovery.
6. **Repeating groups have a count tag preceding them** (e.g., `NoMDEntries=2` before two entries). Engine handles serialization; agent code reading these must iterate via the FixMessage API, not by scanning tag indices.
7. **Heartbeat interval (108)** is in seconds, not ms.

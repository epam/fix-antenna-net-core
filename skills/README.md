# B2BITS Agent Skills

## Executive summary

Software development is moving to AI-assisted and agentic workflows. Developers
increasingly use AI coding assistants (Claude Code, Cursor, Copilot, etc.) to
understand libraries, generate integration code, troubleshoot, and configure
systems. Plain API reference documentation was written for humans reading
top-to-bottom — it is not enough to make an AI agent produce correct code against
a specialized library like a FIX engine.

B2BITS is addressing this by shipping **Agent Skills**: structured, version-pinned
knowledge packages that teach an AI assistant the correct usage patterns,
lifecycle rules, and failure modes of our products — not just the API surface.

The goals:

- Reduce client onboarding time.
- Improve the correctness of AI-generated integration code (fewer
  plausible-but-wrong snippets).
- Reduce support load for recurring "how do I..." questions.
- Accelerate adoption of B2BITS products.

A Skill is a self-contained package (a `SKILL.md` guide plus task-oriented
reference scenarios) that an AI agent loads automatically when it detects a
relevant task. It is distributed alongside the product, but it does **not** modify
the product binary or its source.

## What ships today

### FIX Antenna .NET Core — Agent Skill

A complete Agent Skill, verified against `Epam.FixAntenna.NetCore` 1.2.3, covering
the .NET / C# binding.

Structure (as actually built):

- **A `SKILL.md` guide** — mental model, hard DO/DON'T rules, and the API patterns
  agents most often get wrong (session-listener inheritance trap,
  persistent-storage selection, repeating-group indexing, two-arg `SendMessage`,
  sequence/recovery semantics).
- **Reference scenarios** — task-oriented, production-quality: minimal acceptor,
  minimal initiator, order-entry client, drop-copy consumer, custom dictionary,
  multi-session router, persistence and recovery, custom storage replication, TLS
  secure session, FIX-to-Kafka bridge, scheduled sessions, performance profiles,
  admin/monitoring.
- **Supporting references** — API reference, config keys, glossary, and an index
  cross-mapped to the upstream GitHub docs.

What this Skill gives an AI agent:

- Architecture and mental model (Config → Session → Listener → Storage)
- Session lifecycle, state transitions, logon/logout
- Threading and storage model, persistence choice
- Error handling and validation rules
- Recovery: resend, gap-fill, restart, sequence reset
- Performance profiles
- Reference integration patterns

This is the one fully real, shippable artifact, and the template every future
Skill should follow. A working sample project ships alongside it, so it is
demonstrable today.

## AI-assisted client use cases (scoped to what's real today)

- **Generate integration code.** Example: *"Create a FIX Antenna .NET application
  connecting to an exchange using FIX 4.4 with automatic reconnect."* The agent
  produces starter code that follows the engine's real session/storage/recovery
  model.
- **Avoid the common traps.** Listener inheritance, in-memory vs. persistent
  storage, repeating-group indexing, and header-dropping `SendMessage` overloads
  are pre-empted in the Skill — exactly where AI-generated FIX code tends to break.
- **Troubleshooting guidance.** Example: *"Why is my FIX session continuously
  sending ResendRequest?"* The Skill carries the recovery/resend/sequence
  references the agent needs to reason about the cause and recommend a fix.

## Benefits

- **Faster implementation** — common integrations start from reviewed reference
  patterns, not from model guessing.
- **Better quality** — generated code follows official B2BITS patterns and the
  engine's actual lifecycle and recovery semantics.
- **Lower operational risk** — the assistant is taught the threading, storage, and
  session behavior that determine correctness under reconnect/recovery.
- **Reduced dependence on expert knowledge** — new developers become productive
  against the engine faster.

## Conclusion

AI-ready artifacts turn B2BITS products from traditional libraries into
agent-friendly platforms. Starting with FIX Antenna .NET today and expanding
across the FIX Antenna and FIXEdge family, we ship not just APIs and docs, but the
knowledge an AI assistant needs to correctly build, configure, troubleshoot, and
extend production trading systems — with less dependence on product experts and
support tickets.

## Available skills

| Skill | Verified against | Location |
|---|---|---|
| FIX Antenna .NET Core | `Epam.FixAntenna.NetCore` 1.2.3 | [`antenna-net-core/`](antenna-net-core/SKILL.md) |

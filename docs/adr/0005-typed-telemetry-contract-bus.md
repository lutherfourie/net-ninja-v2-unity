# ADR-0005 — Typed telemetry contract bus — schema in Contracts, sink in Core, MessagePipe at the edge

> **Partially superseded by ADR-0019** (stack strip): MessagePipe is dropped; telemetry fan-out is a plain typed sink (`List<Action<T>>`). The schema-in-Contracts / hand-rolled-sink-in-Core split below still stands.

**Decision.** Event schema lives as readonly structs in NetNinja.CONTRACTS (revised from 'defined in Core'). Core writes them to a deterministic per-tick journal via a hand-rolled ITelemetrySink (Contracts interface). The Unity adapter is the only layer that knows MessagePipe; it drains the journal post-tick, republishes via IPublisher<T>, and stamps run-context with one global MessageHandlerFilter.

**Rationale.** View subscribes to (StateSnapshot, events) but references only Contracts, so the event types MUST live in Contracts for View to name them — the prior 'defined in Core' wording made View structurally unable to receive events. Moving schema to Contracts fixes the crossing while keeping the hand-rolled deterministic sink in Core.

**Alternatives.** Events in Core + View references Core (rejected: breaks the seam); SO event channels in the sim (rejected: determinism trap, unassertable); Action fan-out (rejected: no run-context stamping, alloc-heavy).

**Consequences.** MessagePipe open-generics need explicit per-type registration for IL2CPP/WebGL — generate the list from the event enum (Tools/registration-gen) and verify on the arm64 + WebGL rings.

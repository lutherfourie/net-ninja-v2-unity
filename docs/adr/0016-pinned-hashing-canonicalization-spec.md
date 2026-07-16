# ADR-0016 — Pinned hashing canonicalization spec (FNV state + configHash)

**Decision.** Before porting, pin docs/hashing-spec.md matched to net-lab: canonical field order for the FNV state hash, NaN forbidden in hashed state (assert, do not hash), -0.0 normalized to +0.0, fixed little-endian IEEE-754 8-byte double encoding, and the configHash input canonicalization (key sort order + double formatting). Add a hasher micro-vector (a fixed StateSnapshot → known FNV hash) and a configHash micro-check to the pure-.NET gate.

**Rationale.** One mismatched byte makes every downstream hash diverge, so field order, NaN/-0.0 policy, and byte layout must be a written spec and a micro-test, not an open question discovered mid-port.

**Alternatives.** Leave canonicalization implicit and debug at the full-run level (rejected: a single-bit divergence is undiagnosable at 3600 ticks without a micro-vector); tolerate NaN in state (rejected: NaN != NaN breaks self-determinism).

**Consequences.** The micro-vector is the first parity test written; the spec is referenced from Core's AGENTS.md and is a prerequisite of the port.

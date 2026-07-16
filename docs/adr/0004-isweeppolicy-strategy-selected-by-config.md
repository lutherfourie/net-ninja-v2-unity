# ADR-0004 — ISweepPolicy strategy selected by config

**Decision.** ISweepPolicy is an interface in NetNinja.Contracts; FairSweepPolicy and TriageSweepPolicy are POCOs in NetNinja.Core; the active policy is chosen by net.sweep.policy. An abstract SweepPolicySO factory in the adapter layer constructs the Core POCO. Evolving the mechanic = adding a policy + optional SO subclass, never editing WaveManager.

**Rationale.** Preserves net-lab spine pattern 1. The interface lives in Contracts (not Core) so both View and Adapters can reference the type without Core internals, resolving the previous dual-home ambiguity. Both policies fall under the golden-vector gate.

**Alternatives.** Interface in Core (rejected: forces Core reference on consumers that only need the contract); policies as ScriptableObjects directly (rejected: drags UnityEngine into the gate); branching in WaveManager (rejected: violates the Law).

**Consequences.** PolicyRegistry keeps additions conflict-free across worktrees; TriageSweepPolicy ships day one, off by default, and its config must not alter the 6 existing Fair-cell hashes.

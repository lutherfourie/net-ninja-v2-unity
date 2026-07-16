# ADR-0017 — State read-model DTO in Contracts; double-only crossing, float only in View

**Decision.** Define StateSnapshot in NetNinja.Contracts using the double Vec3 struct (also in Contracts) plus held counts, wave/lives, tick. Core emits it read-only. The double→float conversion for rendering happens explicitly in NetNinja.View (or an Adapters projector). UnityEngine.Vector3 (float) is forbidden anywhere in Contracts/Core.

**Rationale.** 'Core-emitted Vector3 state' was ambiguous and invited UnityEngine.Vector3 (float) into Core, which would break parity. Pinning the crossing type as a double Vec3 in Contracts, with conversion isolated to View, keeps the engine-free assemblies float-free and gives View a concrete type to subscribe to.

**Alternatives.** Cross with UnityEngine.Vector3 (rejected: float in the gate); leave the crossing type unspecified (rejected: View has nothing to name); put Vec3 in Core (rejected: consumers would need Core internals — Contracts is the shared home).

**Consequences.** Vec3 lives in Contracts and is analyzer-guarded; the double→float boundary is a single audited site in View; the law guard forbids UnityEngine.Vector3 in Contracts/Core.

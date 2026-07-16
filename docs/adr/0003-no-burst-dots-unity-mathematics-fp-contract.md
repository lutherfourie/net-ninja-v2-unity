# ADR-0003 — No Burst/DOTS/Unity.Mathematics in the core; IL2CPP fp-contract=off

**Decision.** Compile Contracts+Core as plain managed C# via locked asmdefs; never Burst-compile, never enable fastmath, never reference Unity.Mathematics. Additionally set IL2CPP C++ compiler fp-contract=off (no a*b+c → fma) and verify strict scalar codegen on each on-target ring.

**Rationale.** Unity does not guarantee FloatMode.Deterministic cross-arch, and fastmath reorders/contracts (FMA) ops. Disabling Burst/fastmath alone does NOT control the IL2CPP C++ backend's default fp-contract, which on some clang configs contracts a*b+c on ARM and changes rounding. Explicitly pinning fp-contract=off closes that gap.

**Alternatives.** Burst FloatMode.Deterministic (rejected: not guaranteed, no cross-arch CI); DOTS ECS core (rejected: drags math libs into the gate); leaving fp-contract at default (rejected: silent ARM FMA divergence).

**Consequences.** Core stays managed single-threaded; law guard asserts no Burst/Unity.Mathematics reference; the build pipeline must inject the fp-contract=off flag and the arm64 parity ring proves it worked.

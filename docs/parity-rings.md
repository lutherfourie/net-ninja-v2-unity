# Parity rings — as shipped

The determinism gate is layered into concentric **rings**, cheapest and most-trusted first.
This documents the rings **as they actually exist today**, not the aspirational full set.
Referenced by `.github/workflows/arm64-parity.yml`.

## Ring 0 — dotnet CoreCLR parity (LIVE, merge gate)

- **Status:** live, **15/15 green**. This is the tier-1 gate that runs on every diff.
- **What runs:** `Tools/parity-dotnet/NetNinja.Core.Parity.Tests.csproj` via `dotnet test`.
  Pure managed .NET — **no Unity, no license, no seat contention** (possible only because
  Contracts+Core are engine-free; ADR-0009/0012). The csproj globs the engine-free
  `Runtime/**` sources directly and runs them under CoreCLR.
- **Coverage:**
  - `ConfigHash_MatchesGolden` + `configHash == 6c3a8288f02919a3` (config-drift guard).
  - `GoldenVectors_AllSixCells_BitExact_ViaOracleTraces` — **sim-only** conformance via
    **oracle-trace replay**: net-lab's exported per-tick target frames
    (`golden/traces/{persona}@{seed}.json`) are replayed into the C# sim and FNV run+checkpoint
    hashes are asserted with zero tolerance. This isolates sim bit-parity from persona plant
    libm ULP (ADR-0018).
  - `SelfDeterminism` — same (seed, persona) run twice yields the same runHash (catches hidden
    global state).
  - Hasher micro-vectors + `-0.0` normalization + canonical 16-hex configHash.
  - `AnalyzerTripTests` — the **live determinism guard**: a regex scan (`\bfloat\b` + `Math.Exp`)
    over `Core`/`Contracts` sources. (The Roslyn allowlist analyzer is NOT yet wired — fast-follow,
    ADR-0002/0018.)
  - `LivePersona_MatchesOracleTargets_UntilPlantUlp` — **documents** (does not gate) that a live
    C# persona diverges from the oracle at **tick 125** on the first Box–Muller Cos/Log ULP.

## Ring 1 — Android arm64 IL2CPP (PLACEHOLDER, deferred)

- **Status:** placeholder workflow `.github/workflows/arm64-parity.yml` — `workflow_dispatch`
  only; the job echoes its requirement and `exit 0`. Deliberately deferred (ADR-0012/0015).
- **Why it matters:** the **primary FMA-divergence ring** (ADR-0015). WebAssembly f64 is strict
  IEEE-754 with no FMA, so a WebGL-only gate never exercises the dangerous case: clang
  contracting `a*b+c` into `fma` on ARM64. The product ships on ARM targets.
- **What would make it real:** `secrets.UNITY_LICENSE`; an Android IL2CPP player built with
  **fp-contract=off** injected into the C++ backend (ADR-0003); an on-device/emulator
  hash-capture harness that replays the same oracle traces as ring 0 and asserts **identical
  runHashes**.

## Ring 2 — WebGL / wasm (PLACEHOLDER, deferred)

- **Status:** placeholder workflow `.github/workflows/webgl-parity.yml` — `workflow_dispatch`
  only; echoes its requirement and `exit 0`. Deferred (ADR-0012/0015).
- **Why it's kept:** WebGL is a shipped responsive target with its own emscripten/libc codegen
  risk, even though wasm f64 (no FMA) agrees with the Mono editor trivially.
- **What would make it real:** `secrets.UNITY_LICENSE`; a WebGL player build; a headless
  (node/puppeteer) hash-capture harness replaying the oracle traces and asserting runHash
  parity vs ring 0.

## Open risk — on-target persona parity (bot-only, ADR-0018)

To stay consistent with ring 0, the on-target rings would replay the **sim-only** oracle
traces. **Live-persona** on-target parity is separately **UNMET**: the persona plant uses
libm `Log`/`Log2`/`Cos` (Box–Muller + Fitts), which are not correctly-rounded across
CoreCLR / IL2CPP-arm64 / emscripten. This risk is **bot-only** — it never touches sim
evolution, scoring, or the hashed state. Resolution is OWNER-PENDING (ADR-0018).

## Deferred beyond the rings

iOS device parity is deferred to a documented macOS runner and ledgered (ADR-0015).

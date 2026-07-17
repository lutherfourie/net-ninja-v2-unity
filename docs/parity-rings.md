# Parity rings — as shipped

The determinism gate is layered into concentric **rings**, cheapest and most-trusted first.
This documents the rings **as they actually exist today**, not the aspirational full set.

Net Ninja v2 ships as a **mobile-first browser game** (WebGL/wasm, portrait) — no native mobile
target is planned (`docs/DESIGN-PREMISE.md`, ADR-0015 owner ruling 2026-07-16). The rings are
therefore scoped to the **wasm** surface. There is **no arm64 ring** (see the flip condition at
the bottom); the old `.github/workflows/arm64-parity.yml` + `scripts/run-arm64-parity.sh` were
removed under ADR-0019.

## Ring 0 — dotnet CoreCLR parity (LIVE, merge gate)

- **Status:** live, **15/15 green**. This is the tier-1 gate that runs on every diff.
- **What runs:** `Tools/parity-dotnet/NetNinja.Core.Parity.Tests.csproj` via `dotnet test`.
  Pure managed .NET — **no Unity, no license, no seat contention** (possible only because
  Contracts+Core are engine-free; ADR-0012/0019). The csproj globs the engine-free
  `Assets/_NetNinja/{Contracts,Core}/**` sources directly and runs them under CoreCLR.
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
  - **Roslyn allowlist analyzer (wired):** `Tools/determinism-analyzer` is referenced from the
    parity csproj as `OutputItemType="Analyzer"` and fails the build on `NNDET001` (float),
    `NNDET002` (disallowed `System.Math`), `NNDET003` (engine types). Unity package ships the
    built DLL at `Packages/com.netninja.determinism-analyzer/Editor/RoslynAnalyzers/` with the
    `RoslynAnalyzer` label. Deliberate red-build trip:
    `dotnet build Tools/parity-dotnet/NetNinja.Core.Parity.Tests.csproj -c Release -p:TripAnalyzer=true`
    (see `Tools/parity-dotnet/analyzer-trip/`).
  - `AnalyzerTripTests` — belt-and-suspenders regex scan (`\bfloat\b` + `Math.Exp`) over
    `Core`/`Contracts` sources (still useful as a no-Roslyn fallback).
  - `LivePersona_MatchesOracleTargets_UntilPlantUlp` — **documents** (does not gate) that a live
    C# persona diverges from the oracle at **tick 125** on the first Box–Muller Cos/Log ULP.

## Ring 1 — WebGL / wasm ship gate (PLACEHOLDER, deferred)

- **Status:** placeholder workflow `.github/workflows/webgl-parity.yml` — `workflow_dispatch`
  only; the job echoes its requirement and `exit 0`. Deferred (ADR-0012/0015).
- **Why it matters:** it gates the **artifact the game actually releases** — the WebGL/wasm
  build served to mobile + desktop browsers. This is the ship-blocking ring once real.
- **What would make it real:** `secrets.UNITY_LICENSE`; a WebGL player build; a headless
  (node/puppeteer) hash-capture harness that replays the same oracle traces as Ring 0 and
  asserts **identical runHashes**.

## Ring 2 — mobile-browser vs desktop-browser wasm cross-hash (PLACEHOLDER, deferred)

- **Status:** placeholder; no dedicated workflow yet (folds into the Ring 1 harness build-out).
- **Why it's kept:** the **honest shipped-surface divergence check** — same wasm build, run in a
  mobile browser and a desktop browser, assert identical runHashes. WebAssembly f64 is strict
  IEEE-754 with **no FMA**, so **zero divergence is expected**; this ring exists to catch any
  emscripten/libc or browser-engine codegen surprise on the surface that actually ships.
- **What would make it real:** the same node/puppeteer harness as Ring 1, driven against a mobile
  browser profile (or device-lab) in addition to desktop, cross-asserting the two runHashes.

## Open risk — on-target persona parity (bot-only, ADR-0018)

To stay consistent with Ring 0, the wasm rings would replay the **sim-only** oracle traces.
**Live-persona** on-target parity is separately **UNMET**: the persona plant uses libm
`Log`/`Log2`/`Cos` (Box–Muller + Fitts), which are not correctly-rounded across CoreCLR /
emscripten. This risk is **bot-only** — it never touches sim evolution, scoring, or the hashed
state. Resolution is OWNER-PENDING (ADR-0018).

## Flip condition — when arm64 comes back

If Luther commits Net Ninja to a **native mobile release** (Play/App Store IL2CPP arm64 binary,
not browser), the arm64 ring is **resurrected as the divergence canary**: clang can contract
`a*b+c → fma` on ARM64 — the one case no wasm target hits — so a native arm64 player becomes the
most-sensitive, ship-blocking ring, and `ProjectSettings/IL2CPP-NOTES.md`'s fp-contract=off
guidance activates. Until such a ship exists, arm64 is not a ring.

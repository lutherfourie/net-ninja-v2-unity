# Net Ninja — Architecture Decision Records

## ADR-0001 — Unity Editor is the engine-native authoring + port-verification surface, not a second design authority

**Decision.** Draw the authoring boundary by ARTIFACT CLASS, not by tool:
- **The web workstation (net-lab, ADR-0024–0028) OWNS rule-bearing authoring** — config/tunables, waves, sweep-policy params, perks/economy, DDA/tempo, batch-matrix definitions, VFX specs, UI artifacts, and scene-LAYOUT data. These export as truth (ADR-0011).
- **The Unity Editor OWNS engine-native authoring ONLY** — prefabs, `.unity` scenes, URP/materials/shaders, sprite + animation import, addressables, the build, and the double→float View projection.
- **Unity Editor windows are IMPORTERS / INSPECTORS / RUNNERS, never rule-authoring surfaces:** config-import (ADR-0007), a read-only parity/telemetry/golden-vector inspector, the persona *runner* (generates traces to check the port), and SO generators that transform exported specs. Any hand-authored rule-bearing data in-editor is an **Untwinned Port** = ledgered fidelity debt (ADR-0011's one-way law).

This REVISES the original "Unity Editor IS the Design Workstation" call, which claimed authoring authority superseding net-lab 0024–0028. See ADR-0019 and `docs/reviews/2026-07-16-4call-stress.md` (adr1-workstation verdict: NARROW).

**Rationale.** The sound half of the original thesis survives: because Contracts+Core are engine-free C#, Editor windows run the real sim headless in edit mode, so Unity is a genuine port-VERIFICATION surface. But rebuilding rule-bearing authoring in-editor recreates the estate's documented death loop — parallel authoring surfaces → drift → abandonment (net-lab already measured a 55.5%-fidelity resim drift from dual authoring). pawfall's real Unity editor surface is almost entirely engine-native asset pipeline; its rule-bearing tuning already lives as exported data, not in editor windows.

**Alternatives.** Unity as authoring authority superseding net-lab (rejected: dual rule-bearing surfaces, the exact drift census); freeze net-lab so Unity can own authoring (rejected: ADR-0011 keeps net-lab a LIVE oracle whose export is re-run, so it stays authored); IMGUI/Odin tooling debates (moot once Unity stops authoring rules).

**Consequences.** The 7 non-functional windows are re-intented author→inspect/import (header comments only this pass; no functional change). The functional Config/Key **Editor** window keeps its place because it already *imports* per ADR-0007 (not re-typing literals). NetNinja.Editor stays a load-bearing seam but a CONSUME/VERIFY one; config remains text-serialized SO under version control, imported (never hand-typed) for parity keys.

## ADR-0002 — Determinism via portable double-only FP with an ALLOWLIST analyzer

**Decision.** Contracts, Core, and the persona drivers use double everywhere with only + - * / and Math.Sqrt. The SHIPPED Roslyn ALLOWLIST analyzer (`AllowlistMathAnalyzer.cs:38-41`) permits Math.{Sqrt,Abs,Min,Max,Floor,Ceiling,Truncate} PLUS the provisional plant set {Log,Log2,Cos} — relaxed for the persona Box–Muller/Fitts terms (see ADR-0018) — and errors on the float keyword, Mathf, UnityEngine.Random, Burst, Unity.Mathematics, and every other System.Math member (Pow/Exp/Sin/Tan/Log10/Atan2/Sinh/Cbrt/IEEERemainder…). NOTE (enforcement, as-shipped, ADR-0018): the guard that actually runs is a REGEX SCAN (`\bfloat\b` + `Math.Exp`) in `AnalyzerTripTests`; the Roslyn allowlist analyzer exists but is NOT yet wired as an `<Analyzer>` — wiring is a declared fast-follow. Non-persona transcendentals are pre-baked into config literals offline. No wall-clock; seeded rng and const DT=1/60 only.

**Rationale.** IEEE-754 mandates + - * / and sqrt are correctly-rounded; C# double == JS binary64, so bit-parity is achievable. The original denylist (float/Mathf/Random) missed System.Math transcendentals, which are plain engine-free double and would compile silently — the exact trap this ADR exists to close. An allowlist is the only guard that catches them.

**Alternatives.** Denylist of engine types only (rejected: lets Math.Exp through); fixed-point 32.32 (rejected: incompatible with JS f64 vectors); float (rejected: breaks parity); soft-float (risk-register escape hatch only).

**Consequences.** The analyzer source ships with its own planted-violation trip tests but is NOT yet wired into the build (regex scan is the live guard; Roslyn wiring is a fast-follow — ADR-0018); Sqrt/Log/Log2/Cos are the audited System.Math call sites (Log/Log2/Cos persona-only); parity is re-proven on arm64 + WebGL, not just editor Mono.

## ADR-0003 — No Burst/DOTS/Unity.Mathematics in the core; IL2CPP fp-contract=off

**Decision.** Compile Contracts+Core as plain managed C# via locked asmdefs; never Burst-compile, never enable fastmath, never reference Unity.Mathematics. Additionally set IL2CPP C++ compiler fp-contract=off (no a*b+c → fma) and verify strict scalar codegen on each on-target ring.

**Rationale.** Unity does not guarantee FloatMode.Deterministic cross-arch, and fastmath reorders/contracts (FMA) ops. Disabling Burst/fastmath alone does NOT control the IL2CPP C++ backend's default fp-contract, which on some clang configs contracts a*b+c on ARM and changes rounding. Explicitly pinning fp-contract=off closes that gap.

**Alternatives.** Burst FloatMode.Deterministic (rejected: not guaranteed, no cross-arch CI); DOTS ECS core (rejected: drags math libs into the gate); leaving fp-contract at default (rejected: silent ARM FMA divergence).

**Consequences.** Core stays managed single-threaded; law guard asserts no Burst/Unity.Mathematics reference; the build pipeline must inject the fp-contract=off flag and the arm64 parity ring proves it worked.

## ADR-0004 — ISweepPolicy strategy selected by config

**Decision.** ISweepPolicy is an interface in NetNinja.Contracts; FairSweepPolicy and TriageSweepPolicy are POCOs in NetNinja.Core; the active policy is chosen by net.sweep.policy. An abstract SweepPolicySO factory in the adapter layer constructs the Core POCO. Evolving the mechanic = adding a policy + optional SO subclass, never editing WaveManager.

**Rationale.** Preserves net-lab spine pattern 1. The interface lives in Contracts (not Core) so both View and Adapters can reference the type without Core internals, resolving the previous dual-home ambiguity. Both policies fall under the golden-vector gate.

**Alternatives.** Interface in Core (rejected: forces Core reference on consumers that only need the contract); policies as ScriptableObjects directly (rejected: drags UnityEngine into the gate); branching in WaveManager (rejected: violates the Law).

**Consequences.** PolicyRegistry keeps additions conflict-free across worktrees; TriageSweepPolicy ships day one, off by default, and its config must not alter the 6 existing Fair-cell hashes.

## ADR-0005 — Typed telemetry contract bus — schema in Contracts, sink in Core, MessagePipe at the edge

> **Partially superseded by ADR-0019** (stack strip): MessagePipe is dropped; telemetry fan-out is a plain typed sink (`List<Action<T>>`). The schema-in-Contracts / hand-rolled-sink-in-Core split below still stands.

**Decision.** Event schema lives as readonly structs in NetNinja.CONTRACTS (revised from 'defined in Core'). Core writes them to a deterministic per-tick journal via a hand-rolled ITelemetrySink (Contracts interface). The Unity adapter is the only layer that knows MessagePipe; it drains the journal post-tick, republishes via IPublisher<T>, and stamps run-context with one global MessageHandlerFilter.

**Rationale.** View subscribes to (StateSnapshot, events) but references only Contracts, so the event types MUST live in Contracts for View to name them — the prior 'defined in Core' wording made View structurally unable to receive events. Moving schema to Contracts fixes the crossing while keeping the hand-rolled deterministic sink in Core.

**Alternatives.** Events in Core + View references Core (rejected: breaks the seam); SO event channels in the sim (rejected: determinism trap, unassertable); Action fan-out (rejected: no run-context stamping, alloc-heavy).

**Consequences.** MessagePipe open-generics need explicit per-type registration for IL2CPP/WebGL — generate the list from the event enum (Tools/registration-gen) and verify on the arm64 + WebGL rings.

## ADR-0006 — VContainer composition root with View wired as ILateTickable

> **Superseded by ADR-0019** (stack strip): VContainer/R3/MessagePipe/UniTask are removed; the composition root is a plain `Bootstrap : MonoBehaviour` (FixedUpdate → sim step, LateUpdate → view-apply). The view-never-writes-core intent is preserved structurally by the seam, not by DI entry-point ordering. Retained below as the historical record.

**Decision.** Adopt VContainer (MIT) with Root/Game/EditorTool LifetimeScopes. The Sim steps via an IFixedTickable entry point owning DT=1/60; View is registered in NetNinja.Composition as an ILateTickable that runs strictly after. To make this real, NetNinja.View references VContainer and NetNinja.Composition references NetNinja.View. Core/Contracts have zero DI reference.

**Rationale.** Nested scopes mirror the seams; entry-point ordering structurally enforces view-never-writes-core — but only if View actually participates in the VContainer lifecycle, which the prior graph did not wire (View referenced neither VContainer nor Composition). Adding those two references makes the 'structurally enforced' claim true, backed by a Composition tick-order test.

**Alternatives.** View outside DI with a plain ordering test only (kept as the fallback assertion but rejected as the sole guarantee); Zenject (rejected: heavier, reflection scans); hand-rolled DI at the edge (rejected: merge-conflict-prone bootstrapper).

**Consequences.** Enable VContainer.SourceGenerator + a WebGL smoke test before trusting AOT; exactly one owner of the 1/60 tick (the IFixedTickable entry point).

## ADR-0007 — ScriptableObject config authority with imported (not re-typed) parity literals

**Decision.** All net.*/tempo.*/autopilot.* tunables live in NetNinjaConfigSO as List<KeyEntry{string key;double value}> fronted by ConfigService.GetDouble(key, codeDefault), flattened into an immutable CoreConfig struct at boot. Parity-gated keys (the golden config) are IMPORTED from exported config/*.json via Tools/config-import; only non-parity keys are hand-authored in a window. lean path = null bindings for byte-identical rollback.

**Rationale.** No magic numbers; every tunable is diffable text. But hand-typing 0.2591817793182821 into a window IS re-typing a baked literal, which ADR-0011 forbids. Importing the parity keys from the exported JSON reconciles editor-authority (ADR-0001) with never-re-type (ADR-0011).

**Alternatives.** Hand-author everything in the window (rejected: transposed-digit risk on parity keys); JSON-only config with no SO (rejected: loses in-editor authority); hard-coded constants (rejected: no authoring surface).

**Consequences.** config-import is a required tool this pass; List<KeyEntry> keeps SerializedObject binding/undo/prefab-override working; config SOs are read-only-at-runtime, cloned to struct to avoid poisoning the next golden run.

## ADR-0008 — Persona/replay injection at the real IInputReader seam, drivers in the engine-free core

**Decision.** IInputReader emits a per-tick InputFrame (Contracts). DeviceInputSource (Input System 1.19) is the engine-coupled production binding in Adapters. PersonaDriver (Perfect/Average/Sloppy — Motor-Plant coefficients baked to literals, latency as integer tick delay, noise from seeded Hash01) and ReplayDriver are the ENGINE-FREE implementations in NetNinja.Core, under the same allowlist analyzer. Never inject at the wall-clock event layer.

**Rationale.** The 6 golden cells are persona-driven, so persona generation MUST be bit-exact — yet the prior plan placed it in Adapters, outside the determinism guard. Moving the deterministic driver into Core puts it under the analyzer and the pure-.NET gate, guaranteeing the cells reproduce; only device I/O stays engine-coupled.

**Alternatives.** Personas in Adapters (rejected: unguarded parity code); side-channel feed (rejected: not the real pipeline); event-layer injection (rejected: wall-clock timestamps).

**Consequences.** Open-question on Fitts/min-jerk/Gaussian reducibility must be resolved BEFORE the port; if any term needs runtime transcendentals it must be pre-baked or the cell is unreproducible; InputEventTrace is human-capture only, resampled offline into per-tick frames.

## ADR-0009 — Layered embedded-UPM packages, one asmdef per seam, Core references only Contracts

> **Superseded by ADR-0019** (right-sizing): the nine embedded UPM packages collapse to FIVE asmdefs under `Assets/_NetNinja/` (Contracts, Core, View, Editor, App); only `com.netninja.determinism-analyzer` stays a package. The engine-free Contracts/Core asmdefs (noEngineReferences) and Core→Contracts-only edge are preserved verbatim — the compiler-enforced parity spine was never the package ceremony. Retained below as the historical record.

**Decision.** Structure the repo as embedded UPM packages under Packages/ (com.netninja.contracts, .core, .config, .adapters, .view, .composition, .telemetry, .editor, .determinism-analyzer) with strict acyclic references, references by GUID, autoReferenced:false on Contracts/Core. NetNinja.Core references EXACTLY [NetNinja.Contracts]; every other consumer references Contracts (never Core internals) where possible. C# logic stays out of Assets/.

**Rationale.** net-lab's lesson: 26 folders / ~5 asmdefs = de-facto monolith. The prior graph declared Core references:[] yet Core must consume InputFrame/ISweepPolicy/ITelemetrySink — impossible. Adding the single Core→Contracts edge (Contracts being engine-free) makes the graph compile while keeping Core engine-free and all noEngineReferences criteria intact.

**Alternatives.** Core references:[] with contract types duplicated into Core (rejected: two homes for one CLR type, unresolvable); folder-only split (rejected: no compiler enforcement); one asmdef per topic (rejected: splits parity logic).

**Consequences.** Parity-critical POCOs (policies, hasher, personas) stay in ONE Core assembly so it hashes as a unit; each package has package.json + Runtime/Editor/Tests; commit all .meta and packages-lock.json.

## ADR-0010 — Worktree + VCS discipline (Force Text, LFS, SmartMerge, per-worktree Library)

**Decision.** Force Text serialization + Visible Meta Files (committed EditorSettings), Git LFS via macro .gitattributes for binaries with golden/vectors.json and config kept PLAIN TEXT, UnityYAMLMerge SmartMerge for .unity/.prefab/.asset, one git worktree per agent each with its own Library, github Unity .gitignore, commit manifest.json + packages-lock.json.

**Rationale.** Version control is a determinism-preserving pipeline: diffable config, auto-mergeable scenes, reproducible package graphs, conflict-free parallel worktrees.

**Alternatives.** Binary serialization (rejected: unmergeable scenes); LFS on all .asset (rejected: swallows text config); shared Library across worktrees (rejected: Unity locks Library, corrupts import state).

**Consequences.** Commit .gitattributes + git lfs install before the first binary; new-worktree seeds a fresh Library and distinct editor title; check gate lints meta discipline and LFS scope via git lfs ls-files.

## ADR-0011 — Twin-codebase golden-vector parity, TS as oracle, export is a blocking prerequisite

**Decision.** net-lab (TS) remains the reference oracle: a pnpm export:golden emits golden/vectors.json + config/*.json (baked literals). AUTHORING/VERIFYING that these exports exist and emit configHash 6c3a8288f02919a3 is an explicit BLOCKING task at the front of this pass. The C# side loads that JSON, runs the ported sim, and asserts FNV run+checkpoint hashes with ZERO tolerance for core hashes; ULP tolerance only at the untwinned view boundary (never a merge gate). A configHash cross-check catches config drift.

**Rationale.** Restricting the op-set makes exact parity winnable; exporting literals prevents transposed-digit drift. But the whole gate is downstream of an export script whose existence was an open question — so it must be a named prerequisite, not an assumption, or the pass cannot deliver its headline claim.

**Alternatives.** Re-type config literals in C# (rejected: silent drift); epsilon tolerance on core hash (rejected: defeats self-determinism); assume the export exists (rejected: unverified, blocks everything).

**Consequences.** Three CI rings plus a pure-.NET tier-1 gate; if the export does not exist it is authored against the TS oracle first; golden vectors and config stay diffable, non-LFS.

## ADR-0012 — GameCI editor-first CI with a pure-.NET tier-1 gate and license placeholders

**Decision.** Two-tier check. Tier-1 (every diff, NO Unity license): pure-.NET `dotnet test` of the golden vectors + self-determinism + hasher micro-vector + configHash, plus the Roslyn analyzer, plus the law guard. Tier-2 (CI, license required): GameCI game-ci/unity-test-runner EditMode/PlayMode + on-target arm64/WebGL parity + Android/iOS/WebGL builds, with documented Unity license placeholders. iOS on Linux stops at the Xcode project; device build/parity runs on a documented macOS runner.

**Rationale.** Editor-first now; parity harness runs immediately. Crucially, because Contracts+Core are engine-free, the fast per-diff gate can run without launching Unity — this removes the Unity Personal seat-contention that would otherwise serialize N parallel agents each spinning batchmode Unity. WebGL/arm64 rings catch on-target float leakage the Mono editor hides.

**Alternatives.** Unity batchmode on every diff (rejected: seat contention undermines the parallel-worktree thesis); no CI until gameplay (rejected: parity must gate from day one); cloud paid CI (rejected: free/OSS-only).

**Consequences.** License secrets are documented placeholders; nightly/pre-merge for expensive on-target parity; tier-1 stays fast so agents self-verify without a seat.

## ADR-0013 — URP renderer feature port of the Built-in OnRenderImage FX rig

**Decision.** Ship one URP Universal Renderer with two quality-tier assets (Mobile, WebGL). Rebuild the Built-in verlet-net ortho-overlay OnRenderImage FX as a URP ScriptableRendererFeature using Render Graph AddBlitPass + Volume overrides; the overlay camera becomes a stacked Overlay camera or a BeforeRenderingPostProcessing pass. A View.Tests smoke asserts the feature constructs.

**Rationale.** URP ignores OnRenderImage; Render Graph is mandatory in URP 17/Unity 6. This is net-lab risk #1; giving it a compile/construction smoke test this pass ensures the riskiest presentation port is not entirely uncovered.

**Alternatives.** Keep Built-in pipeline (rejected: locked to URP); Compatibility Mode CommandBuffer.Blit (rejected: deprecated); zero test coverage (rejected: net-lab risk #1 unguarded).

**Consequences.** Forward (not Forward+) path, HDR off, MSAA ≤2x, render scale as a config key; FX must not leak into Core/Adapters; prove FX on the WebGL/GLES3 + arm64 targets.

## ADR-0014 — AI-navigable docs: terse AGENTS.md + append-only ADRs + generated boot brief + hashing spec

**Decision.** Root AGENTS.md (20-40 lines, hand-written) with a one-line CLAUDE.md shim @AGENTS.md; nested ~15-line AGENTS.md per assembly seam (Core carries the determinism commandments and points at docs/hashing-spec.md); append-only MADR ADRs in docs/adr/; BOOT-BRIEF.generated.md derived by Tools/gen-bootbrief and failing check if git-dirty. Optional CoplayDev unity-mcp for agent verify loops only.

**Rationale.** Short hand-written instruction files beat auto-generated bloat; append-only ADRs are conflict-free under parallel worktrees; the generated brief is the /understand map. The hashing spec is added as a first-class doc because canonicalization is parity-blocking.

**Alternatives.** Auto-generated AGENTS.md (rejected: higher cost, lower agent success); hand-maintained boot brief (rejected: rots).

**Consequences.** Core AGENTS.md is the loudest guardrail; MCP agents still go through human-gated check + fetch-before-push + additive-edit discipline.

## ADR-0015 — Parity rings are wasm-only: dotnet CoreCLR gate + WebGL ship gate + mobile-vs-desktop wasm cross-hash

**Status.** OWNER RULING (Luther, 2026-07-16): Net Ninja v2 ships as a mobile-first BROWSER game (WebGL/wasm, portrait). No native mobile target is planned. This SUPERSEDES the original "on-target parity gate is Android arm64 (primary) plus WebGL" decision, and is stronger than the review's adr15-ring verdict (which kept arm64 as a non-ship divergence-canary): the arm64 ring is DELETED, not merely relabeled, because there is no arm64 shipped surface to canary. See `docs/DESIGN-PREMISE.md` and ADR-0019.

**Decision.** Three concentric rings, all scoped to the actually-shipped wasm surface:
- **Ring 0 — dotnet CoreCLR parity (LIVE merge gate, 15/15).** Pure managed .NET; no Unity, no license; the tier-1 gate on every diff (ADR-0012).
- **Ring 1 — WebGL / wasm ship gate (PLACEHOLDER).** Gates the artifact the game actually releases. Placeholder until the node/puppeteer headless hash-capture harness lands; then it replays the oracle traces and asserts runHash parity vs Ring 0.
- **Ring 2 — mobile-browser vs desktop-browser wasm cross-hash (PLACEHOLDER).** The honest shipped-surface check: same wasm build, mobile vs desktop browser, assert identical runHashes. Expected ZERO divergence under strict IEEE-754 wasm (no FMA); placeholder until the harness exists.

**Flip condition.** A native mobile release (Play/App Store IL2CPP arm64 binary, not browser) RESURRECTS arm64 as the divergence-canary ring — clang can contract `a*b+c → fma` on ARM64, the one case no wasm target hits — and restores the fp-contract=off proof (ADR-0003). Until such a ship exists, arm64 is not a ring.

**Rationale.** WebAssembly f64 is strict IEEE-754 with no FMA. The original ADR kept arm64 as PRIMARY on the premise "the mobile-first product ships on exactly the ARM targets" — false: it ships WebGL. With no native target, an arm64 merge gate blocks around a surface that is never released while under-building the WebGL harness that gates the real one. Ring 2 keeps an on-shipped-surface canary (mobile vs desktop browser) that is honest about what actually ships.

**Alternatives.** Keep arm64 as a non-ship divergence-canary (the review's verdict; rejected by owner: no native ship to de-risk, so it is pure CI/seat debt); WebGL-only single ring (rejected: loses the mobile-vs-desktop cross-hash, the only on-shipped-surface divergence check); arm64-primary as originally written (rejected: gates a non-shipped target).

**Consequences.** `.github/workflows/arm64-parity.yml` + `scripts/run-arm64-parity.sh` are deleted; `docs/parity-rings.md` is rewritten to the three-ring wasm-only model; the node/puppeteer WebGL harness is the real Ring 1/2 build-out. iOS device parity is no longer a ring (browser-only); `ProjectSettings/IL2CPP-NOTES.md`'s fp-contract=off guidance stays as dormant native-build engineering, activated only by the flip condition.

## ADR-0016 — Pinned hashing canonicalization spec (FNV state + configHash)

**Decision.** Before porting, pin docs/hashing-spec.md matched to net-lab: canonical field order for the FNV state hash, NaN forbidden in hashed state (assert, do not hash), -0.0 normalized to +0.0, fixed little-endian IEEE-754 8-byte double encoding, and the configHash input canonicalization (key sort order + double formatting). Add a hasher micro-vector (a fixed StateSnapshot → known FNV hash) and a configHash micro-check to the pure-.NET gate.

**Rationale.** One mismatched byte makes every downstream hash diverge, so field order, NaN/-0.0 policy, and byte layout must be a written spec and a micro-test, not an open question discovered mid-port.

**Alternatives.** Leave canonicalization implicit and debug at the full-run level (rejected: a single-bit divergence is undiagnosable at 3600 ticks without a micro-vector); tolerate NaN in state (rejected: NaN != NaN breaks self-determinism).

**Consequences.** The micro-vector is the first parity test written; the spec is referenced from Core's AGENTS.md and is a prerequisite of the port.

## ADR-0017 — State read-model DTO in Contracts; double-only crossing, float only in View

**Decision.** Define StateSnapshot in NetNinja.Contracts using the double Vec3 struct (also in Contracts) plus held counts, wave/lives, tick. Core emits it read-only. The double→float conversion for rendering happens explicitly in NetNinja.View (or an Adapters projector). UnityEngine.Vector3 (float) is forbidden anywhere in Contracts/Core.

**Rationale.** 'Core-emitted Vector3 state' was ambiguous and invited UnityEngine.Vector3 (float) into Core, which would break parity. Pinning the crossing type as a double Vec3 in Contracts, with conversion isolated to View, keeps the engine-free assemblies float-free and gives View a concrete type to subscribe to.

**Alternatives.** Cross with UnityEngine.Vector3 (rejected: float in the gate); leave the crossing type unspecified (rejected: View has nothing to name); put Vec3 in Core (rejected: consumers would need Core internals — Contracts is the shared home).

**Consequences.** Vec3 lives in Contracts and is analyzer-guarded; the double→float boundary is a single audited site in View; the law guard forbids UnityEngine.Vector3 in Contracts/Core.

## ADR-0018 — Persona transcendentals are provisionally accepted and non-gating; sim is the only parity surface (OWNER-PENDING)

**Status.** Accepted (persona resolution fork is OWNER-PENDING — Luther). Supersedes the contradicted clauses of ADR-0002 (the strict allowlist list) and ADR-0008 (persona bit-exactness + "must be resolved BEFORE the port").

**Decision.** Confine the honesty debt to a single record. The persona/bot PLANT (`IntentMotorDriver` in `Packages/com.netninja.core/Runtime/Personas/MotorPlant.cs`) uses transcendentals: `Fp.Log`/`Fp.Cos` for Box–Muller Gaussian noise at MotorPlant.cs:140 and :147, and `Fp.Log2` for the Fitts movement-time term at :304, :494, :523, :531, :537, :541 (wrappers `Fp.Log`/`Fp.Log2`/`Fp.Cos` at Fp.cs:26-28, each forwarding to `System.Math`). These are PROVISIONALLY ACCEPTED and CONFINED to bot input generation. Grep across `Packages/com.netninja.core/Runtime` + `Packages/com.netninja.contracts/Runtime` verifies ZERO transcendentals in the sim, scoring, or hasher — the twinned (parity-gated) surface is clean; only the untwinned persona surface uses libm.

**Parity scope (as-shipped).** The golden gate (`Tools/parity-dotnet/GoldenVectorTests.cs`) is scoped to SIM-ONLY conformance via ORACLE-TRACE REPLAY: it feeds net-lab's exported per-tick target frames (`golden/traces/{persona}@{seed}.json`) into the C# sim and asserts FNV run+checkpoint parity, deliberately isolating sim bit-parity from plant libm ULP. LIVE-PERSONA runs (running the C# `IntentMotorDriver` itself) DIVERGE from the net-lab oracle at tick 125 (`perfect@42`, first Box–Muller Cos/Log ULP). Therefore persona↔oracle bit-parity is UNMET and is currently NOT a gate; only the sim is a parity surface.

**arm64/WebGL plant parity is an OPEN RISK.** libm `Log`/`Log2`/`Cos` are not correctly-rounded and may differ across CoreCLR/IL2CPP-arm64/emscripten. This risk is BOT-ONLY (persona input generation) and never touches gameplay, sim evolution, scoring, or the hashed state.

**Resolution fork (OWNER-PENDING — Luther).** Until he rules, treat personas as non-gating.
- (A) Bless the split: personas are explicitly NON-TWINNED; the sim is the only parity surface; keep the provisional allowlist (Log/Log2/Cos) and the oracle-trace-replay gate as the permanent shape.
- (B) Hold ADR-0008's original bar: pre-bake the Gaussian/Fitts terms (or otherwise remove runtime transcendentals from the plant), restore the strict Sqrt-only allowlist, and require personas to be bit-exact against the net-lab oracle.

**Rationale.** ADR-0002's allowlist and ADR-0008's "persona generation MUST be bit-exact … resolved BEFORE the port" were written before the port revealed that the plant's Box–Muller/Fitts terms need libm. The shipped analyzer already relaxed the allowlist to admit Log/Log2/Cos (`Tools/determinism-analyzer/AllowlistMathAnalyzer.cs:38-41`), and the gate already replays oracle traces rather than running live personas. This ADR makes the as-built split explicit and honest rather than leaving two ADRs contradicted silently, and hands the real design choice to the owner.

**Consequences.** Personas are non-gating until Luther rules on the fork; the sim-only oracle-trace-replay gate stays green (15/15) as the merge gate; the `LivePersona_MatchesOracleTargets_UntilPlantUlp` test documents the tick-125 boundary rather than asserting parity; arm64/WebGL plant parity stays ledgered as an OPEN, bot-only risk.

## ADR-0019 — Right-sizing the skeleton (packages→asmdefs, stack strip, workstation boundary, wasm-only rings)

**Status.** Accepted (2026-07-16). Records four narrowings ruled by an adversarial 4-judge stress-test (`docs/reviews/2026-07-16-4call-stress.md`, unanimous NARROW ×4) plus one owner ruling. Provenance: that review + `docs/DESIGN-PREMISE.md` (owner premise: mobile-first browser game) + Luther's ADR-0015 ruling in-session. Supersedes ADR-0009 (packaging) and ADR-0006 (DI stack); partially supersedes ADR-0005 (MessagePipe); revises ADR-0001; rewrites ADR-0015. This is a mechanical right-sizing — no new features, gameplay, or model changes; the engine-free Contracts/Core parity spine and the 15/15 gate are preserved.

**1 — Packages → asmdefs (narrows ADR-0009).** The nine embedded UPM packages collapse to FIVE asmdefs under `Assets/_NetNinja/`: `NetNinja.Contracts` + `NetNinja.Core` (both `noEngineReferences:true`, `autoReferenced:false` — moved byte-for-byte, they carry the 15/15 parity proof), `NetNinja.View` (refs Contracts only among game asmdefs), `NetNinja.Editor` (editor-only), and `NetNinja.App` (Config+Adapters+Composition+Telemetry merged; refs Contracts/Core/View + InputSystem/Addressables/Newtonsoft). Runtime sources + their `.meta` were `git mv`'d (GUIDs survive). Only `com.netninja.determinism-analyzer` stays a package (the one rule-of-two candidate — a distributable Roslyn analyzer). No second consumer → packages were single-consumer ceremony; the compiler-enforced asmdefs were not. The `Tools/parity-dotnet` csproj globs + `AnalyzerTripTests` path scan were repointed to the new `Assets/_NetNinja/...` paths.

**2 — Stack strip (narrows ADR-0006).** VContainer, R3, MessagePipe, and UniTask are removed from `Packages/manifest.json` (and the four now-dead scoped-registry scopes: `jp.hadashikick`, `com.cysharp`, plus the already-dead `com.annulusgames`/`com.coffee`) and from every asmdef. Three of the four libs had zero code usage after a day; VContainer's footprint was four empty `Configure()` scopes. The four LifetimeScope/tickable stubs are deleted; one `Assets/_NetNinja/App/Bootstrap.cs` (plain MonoBehaviour) drives the seams by hand: `FixedUpdate()` → `SimPump.Step()` (sim step), `LateUpdate()` → StateProjector/view-apply. UniTask is re-addable in one manifest line the moment an async boot needs it. Unity modules (InputSystem, URP, TMP, Addressables, Newtonsoft) are kept.

**3 — Workstation boundary (revises ADR-0001).** Retitled to "Unity Editor is the engine-native authoring + port-verification surface, not a second design authority." The web workstation owns rule-bearing authoring (exported as truth, ADR-0011); Unity owns engine-native artifacts only; Unity windows are importers/inspectors/runners, never rule-authoring; hand-authored in-editor rule data = ledgered "Untwinned Port" debt. Editor-window stubs re-intented author→inspect/import via header comments only (no functional work).

**4 — wasm-only parity rings (rewrites ADR-0015, OWNER RULING).** Net Ninja v2 ships mobile-first browser; no native target planned. Rings: Ring 0 = dotnet CoreCLR (live merge gate, 15/15); Ring 1 = WebGL/wasm ship gate (placeholder until the node/puppeteer harness lands); Ring 2 = mobile-browser vs desktop-browser wasm cross-hash (placeholder; expected zero divergence under strict IEEE-754 wasm). The arm64 ring is DELETED (`.github/workflows/arm64-parity.yml` + `scripts/run-arm64-parity.sh` removed, `docs/parity-rings.md` rewritten). Flip condition: a native mobile release resurrects arm64 as a canary.

**Consequences.** Migration was cheap because the skeleton is ~1 day old (7 of 8 editor windows were stubs, the parity rings were `exit 0` placeholders, the DI stack had no dependent feature code). The one merge gate (`dotnet test` in `Tools/parity-dotnet`) stays green at 15/15 across the move. Historical package/DI/arm64 layout survives only in the superseded ADRs above and dated docs; living docs (`AGENTS.md`, `README.md`, `docs/parity-rings.md`, `docs/hashing-spec.md`, `docs/seams.md`, `BOOT-BRIEF.generated.md`) are updated to the new layout.

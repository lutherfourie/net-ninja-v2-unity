# Net Ninja — Architecture Decision Records

## ADR-0001 — Unity Editor IS the Design Workstation

**Decision.** Make custom UI Toolkit Editor Windows the first-class authoring authority for Net Ninja (config/wave, persona runner, tempo/DDA, failure-mode diagnostics, batch matrix, telemetry inspector, golden-vector parity), superseding net-lab ADR-0024–0028. The Config/Key Editor window is FUNCTIONAL this pass (exercising the authority claim); the other 7 ship as compiling stubs. The React/net-lab workstation becomes reference/legacy that seeds golden vectors.

**Rationale.** Because the core is engine-free C#, Editor Windows run the real sim headless in edit mode; workstation and runtime share seam definitions, not code paths. Shipping one functional authoring window this pass proves the thesis rather than asserting it. This is Luther's locked BOLD call.

**Alternatives.** Keep the React workstation as authority (rejected: two toolchains, drift); IMGUI tooling (rejected: weaker binding/theming than UITK); paid Odin (rejected: free/OSS-only); all-8-stubs with no functional window (rejected: config-round-trip success criterion would have nothing behind it).

**Consequences.** NetNinja.Editor is a load-bearing seam; windows are one-per-file for agent parallelism; config authored as text-serialized SO under version control; the functional Config window plus config-import must land this pass.

## ADR-0002 — Determinism via portable double-only FP with an ALLOWLIST analyzer

**Decision.** Contracts, Core, and the persona drivers use double everywhere with only + - * / and Math.Sqrt. A Roslyn ALLOWLIST analyzer permits only Math.{Sqrt,Abs,Min,Max,Floor,Ceiling,Truncate} and errors on the float keyword, Mathf, UnityEngine.Random, Burst, Unity.Mathematics, and every other System.Math member (Pow/Exp/Sin/Cos/Tan/Log/Log10/Atan2/Sinh/Cbrt/IEEERemainder…). Transcendentals are pre-baked into config literals offline. No wall-clock; seeded rng and const DT=1/60 only.

**Rationale.** IEEE-754 mandates + - * / and sqrt are correctly-rounded; C# double == JS binary64, so bit-parity is achievable. The original denylist (float/Mathf/Random) missed System.Math transcendentals, which are plain engine-free double and would compile silently — the exact trap this ADR exists to close. An allowlist is the only guard that catches them.

**Alternatives.** Denylist of engine types only (rejected: lets Math.Exp through); fixed-point 32.32 (rejected: incompatible with JS f64 vectors); float (rejected: breaks parity); soft-float (risk-register escape hatch only).

**Consequences.** The analyzer is a shipped asmdef with its own planted-violation tests; Math.Sqrt is the single audited call site; parity is re-proven on arm64 + WebGL, not just editor Mono.

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

**Decision.** Event schema lives as readonly structs in NetNinja.CONTRACTS (revised from 'defined in Core'). Core writes them to a deterministic per-tick journal via a hand-rolled ITelemetrySink (Contracts interface). The Unity adapter is the only layer that knows MessagePipe; it drains the journal post-tick, republishes via IPublisher<T>, and stamps run-context with one global MessageHandlerFilter.

**Rationale.** View subscribes to (StateSnapshot, events) but references only Contracts, so the event types MUST live in Contracts for View to name them — the prior 'defined in Core' wording made View structurally unable to receive events. Moving schema to Contracts fixes the crossing while keeping the hand-rolled deterministic sink in Core.

**Alternatives.** Events in Core + View references Core (rejected: breaks the seam); SO event channels in the sim (rejected: determinism trap, unassertable); Action fan-out (rejected: no run-context stamping, alloc-heavy).

**Consequences.** MessagePipe open-generics need explicit per-type registration for IL2CPP/WebGL — generate the list from the event enum (Tools/registration-gen) and verify on the arm64 + WebGL rings.

## ADR-0006 — VContainer composition root with View wired as ILateTickable

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

## ADR-0015 — On-target parity gate is Android arm64 (primary) plus WebGL

**Decision.** The on-target determinism ring runs the 6 golden cells in BOTH an Android arm64 IL2CPP player (fp-contract=off) and a WebGL/wasm player, asserting identical runHashes. Android arm64 is the primary FMA-divergence ring; WebGL remains a ring because it is the shipped responsive target and catches emscripten/libc codegen drift. iOS device parity is deferred to the macOS runner and ledgered.

**Rationale.** WebAssembly f64 is strict IEEE-754 with no FMA, so a WebGL-only ring trivially agrees with Mono editor and never exercises the dangerous case: clang contracting a*b+c into fma on ARM64. The mobile-first product ships on exactly the ARM targets a WebGL-only gate ignores; arm64 must be the primary gate.

**Alternatives.** WebGL-only (rejected: least-divergence-prone target, false comfort); editor-only (rejected: masks all on-target codegen); arm64-only dropping WebGL (rejected: WebGL is a shipped target with its own codegen risk).

**Consequences.** CI needs an Android build + on-device/emulator hash-capture harness; fp-contract=off must be injected and proven; iOS parity is explicitly ledgered as macOS-runner work.

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

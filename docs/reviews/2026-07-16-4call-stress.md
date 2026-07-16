# 4-call architecture stress-test — verdicts (2026-07-16)

_Luther flagged four ADRs as under-thought; four adversarial judges (Opus) stress-tested each against the REAL pawfall port scope (~195 scripts / 26 systems, plain C#, WebGL ship target). Facts lead-verified; every verdict priced at day-1 migration cost. Unanimous: NARROW ×4 — right-size, don't rebuild._

---
## adr9-packages

Verified the real structure on disk before ruling.

VERDICT: NARROW — the 2 engine-free asmdefs (Contracts, Core) are load-bearing and must stay compiler-enforced; wrapping all 9 seams in embedded UPM packages is single-consumer ceremony the parity proof never needed. (Count correction: it's 9 embedded packages / 8 runtime asmdefs, not 18 — "18" is roughly the whole manifest dep list incl. Unity modules + VContainer/R3/UniTask/MessagePipe.)

THE REPLACEMENT — keep asmdefs, drop packages. 5 asmdefs, 0 game packages, under Assets/_NetNinja/:
- Contracts + Core: separate asmdefs, `noEngineReferences:true`, `autoReferenced:false`. This pair alone gates 15/15 vs the TS oracle. The noEngine flag is an asmdef property, identical in Assets/ as in Packages/ — moving them out preserves 100% of the determinism proof.
- View: own asmdef, refs Contracts only (keeps ADR-0006 view-never-writes-core structural).
- Editor: own asmdef (editor-only is a hard platform boundary, genuinely earned).
- Config+Adapters+Composition+Telemetry → one App asmdef (all single-consumer Unity glue; Telemetry is literally 1 .cs file — the poster child for premature ceremony).
- determinism-analyzer: the ONE thing that stays a package (a distributable Roslyn analyzer is a real rule-of-two candidate estate-wide).

Kills the dual dependency declaration the review already caught: package.json under-declares — View's lists only `{contracts}` while its asmdef actually needs VContainer/URP/TMP/R3/UniTask. Two homes for one fact.

MIGRATION COST NOW (~59 cs files, 1 day old): a few hours — git mv Runtime/*.cs into Assets/, merge asmdefs, delete 9 package.json + file: lines + packages-lock, fix 5 test asmdefs. IN 3 MONTHS (195 scripts, N agent worktrees each with own Library, hand-pinned file: versions + per-package .meta GUIDs): 10–20× and it hardens into untouchable folklore — the exact census-husk death.

WHAT WOULD FLIP TO KEEP: a real second consumer importing Contracts/Core as versioned UPM deps, or a decision to publish the parity spine estate-wide (rule of two met). Also genuine independent per-seam release cadence (absent for one game). No second consumer → packages are vanity; the asmdefs are not.

---
## adr6-stack

Verified against the skeleton: Core/Contracts are engine-free (`noEngineReferences: true`), and the four libs live only at the edge (Adapters/View/Composition). Grep confirms R3, UniTask, MessagePipe have **zero** code usage after a day (MessagePipe appears only in comments); VContainer's "usage" is four empty-bodied `LifetimeScope`/tickable files.

VERDICT: NARROW — three of four libs are unused after a day; VContainer's footprint is four empty-Configure() scopes. The house style is aspirational, not load-bearing: the Cysharp stack chosen as identity before a feature demanded it, in a single-scene catcher built by agents who fare worse in DI/Rx.

Per-dependency attack:
- VContainer — doesn't earn it. One composition point; its tick-ordering guarantee (IFixedTickable→ILateTickable) is bought identically by Unity's own FixedUpdate/LateUpdate in one bootstrap, verified by the SAME test ADR-0006 keeps as fallback — without VContainer.SourceGenerator + a WebGL AOT smoke test to trust.
- R3 — drop. Unused, and fights the design: Core emits a per-tick StateSnapshot (pull/journal, ADR-0005). HUD wants `hud.Apply(snapshot)`, not an Observable graph.
- MessagePipe — drop. ADR-0005 itself flags the tax: open-generics need per-type IL2CPP registration codegen. A hand-rolled ITelemetrySink already exists; fan-out is a `List<Action<T>>`.
- UniTask — only defensible keep: leaf dep, allocation-free (WebGL GC), agents handle async/await fine. But unused today, addable in one asmdef line when Addressables boot needs it.

THE REPLACEMENT: one `Bootstrap : MonoBehaviour` that `new`s the graph; FixedUpdate→sim.Step(), LateUpdate→view.Apply(snapshot); plain C# events/typed sink until fan-out proves insufficient. Keep engine-free Core/Contracts + seam-per-asmdef + the 15/15 gate untouched — that good part is orthogonal to the edge stack. (REWRITE, dropping UniTask too, is near-equivalent; the only real content is killing the DI+Rx+bus triad.)

MIGRATION NOW (~2 hrs): strip 3 asmdefs, drop 3 manifest lines, collapse 4 scope files to one bootstrap. No feature code depends on it; tick-order test survives.
IN 3 MONTHS: multi-day — every construction site resolved through scopes, UniTask flows written, MessagePipe registration-gen built-and-depended-on, R3 subscriptions live. Classic lock-in curve.

WHAT WOULD FLIP IT: game stops being single-scene (real meta/roguelite map → nested scopes earn keep); many independent live telemetry subscribers amortize MessagePipe's IL2CPP cost; a slice built both ways shows agents author DI/Rx as reliably as plain C#; the Cysharp stack actually ships in 2+ estate games (real coherence, not aspiration).

---
## adr1-workstation

I have enough grounding to rule. Key evidentiary find: pawfall's real editor surface (the non-package scripts under `_Pawfall/Editor` + `Scripts/Editor`) is almost entirely engine-native asset pipeline — sprite/animation import, prefab binding, frame postprocessors, WebGL build. Its rule-bearing tuning already lives as data (`pawfall/tuning/pawfall-tuning.v1.json`), not in editor windows. So the "Unity-side authoring exists too" counter is real only for engine-native assets, not for rule-bearing design.

---

**VERDICT: NARROW** — the core thesis (engine-free C# core runs headless in-editor, so Unity gets a real verification surface) is sound, but ADR-0001's "authoring authority... superseding net-lab 0024–0028" clause rebuilds the exact rule-bearing authoring surface net-lab already ships. That's the estate's documented death loop (parallel re-creation → drift → abandonment; the 55.5%-fidelity trace-resim already proved dual-authoring drift).

**THE REPLACEMENT** — retitle to **"Unity Editor is the engine-native authoring + port-verification surface, not a second design authority."** Draw the boundary by artifact class:
- **Web workstation OWNS authoring** of all rule-bearing/oracle-adjacent classes: config/tunables, waves, sweep-policy params, perks/economy, DDA/tempo, batch-matrix definitions, VFX specs, UI artifacts, scene-LAYOUT data. These export as truth (ADR-0011).
- **Unity editor OWNS authoring** of engine-native classes ONLY: prefabs, `.unity` scenes, URP/materials/shaders, sprite+animation import, addressables, build, the double→float View projection.
- **Unity editor gets CONSUME/VERIFY windows, never authoring:** config-import (already ADR-0007), a read-only parity/telemetry/golden-vector inspector, the persona *runner* (generates traces to check the port), SO generators that transform exported specs. Strike the config/wave/DDA *authoring* windows; demote to inspectors. Import net-lab's one-way law: any hand-authored rule-bearing data in-editor is an **Untwinned Port** = ledgered fidelity debt.

**MIGRATION COST — NOW:** near-zero. 7 of 8 windows are compiling stubs; the 1 functional window already *imports* per ADR-0007. Narrowing = ADR text surgery + reclassifying stub intent (author→inspect). **IN 3 MONTHS:** agents fan the 8 "one-per-file" windows into a full second UITK authoring toolchain; you then untangle two live surfaces plus reconcile drifted artifacts — order-of-magnitude worse, and net-lab's exact failure census.

**WHAT WOULD FLIP THIS** — to KEEP: hard evidence net-lab is *frozen* (oracle exported once, never authored again, workstation retired). But ADR-0011 keeps net-lab a LIVE oracle whose export must be re-run, so it stays authored → dual surface persists → NARROW holds. To REWRITE: proof the engine-free core can't actually run headless in edit mode, collapsing the in-editor verification rationale.

---
## adr15-ring

Verified against ADR-0015 (`docs/adr/0015-parity-rings-wasm-only.md`), docs/parity-rings.md, both placeholder workflows/scripts, and the pawfall README. Ruling:

---

**VERDICT: NARROW** — the engineering core (arm64 is the FMA-divergence canary) is correct; only the *ship-priority framing* is inverted, resting on a false present-tense premise. Fix the labels, do not flip the gate.

**THE REPLACEMENT (corrected ADR-0015).** Split the one overloaded word "primary" into two axes:
- **WebGL/wasm = ship-blocking ring.** It gates the artifact pawfall actually releases (README: WebGL build target, IL2CPP, desktop browsers supported, mobile browsers validation-only). On an ARM phone pawfall runs as *wasm* — strict IEEE-754, no FMA — NOT a native IL2CPP arm64 player.
- **arm64 IL2CPP = divergence-canary ring (most-sensitive, non-ship).** Kept precisely because clang can contract `a*b+c → fma`, the one case no shipped target hits, and because it de-risks a *future* native mobile ship. Deferred placeholder, never a merge gate, until such a ship exists.
- Delete the false clause "the mobile-first product ships on exactly the ARM targets." It does not — it ships WebGL. Keep "assert identical runHashes across both."

Reject the lead's primed REWRITE (WebGL-primary, arm64→future option): by your own words wasm is the *friendliest* target, so a WebGL-primary gate is the false comfort ADR-0015 rightly warns against. Friendliest = least-informative canary. Keep arm64 as the hardest ring; just stop calling it the ship surface.

**MIGRATION COST — NOW (1 day old):** prose only. Both rings are `exit 0` placeholders; the sole live gate is Ring 0 (dotnet CoreCLR, 15/15). Edit ADR-0015 + parity-rings.md — minutes. **IN 3 MONTHS:** someone wires Android IL2CPP + on-device hash-capture + a UNITY_LICENSE seat as the "primary" merge gate around a non-shipped target, under-building the node/puppeteer WebGL harness that gates the real release. Unwinding a merge-blocking native-Android gate = CI + seat + developer-friction debt.

**WHAT WOULD FLIP THIS:** Luther commits net-ninja to a NATIVE mobile release (Play/App Store IL2CPP arm64 binary, not browser) — then arm64 is genuinely ship-blocking and "primary" stands as written. Or a measured runHash divergence between desktop-wasm and ARM-mobile-wasm on the same oracle trace — proving the shipped WebGL surface hits FMA drift after all.


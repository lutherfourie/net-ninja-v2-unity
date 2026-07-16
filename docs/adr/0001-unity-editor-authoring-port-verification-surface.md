# ADR-0001 — Unity Editor is the engine-native authoring + port-verification surface, not a second design authority

**Decision.** Draw the authoring boundary by ARTIFACT CLASS, not by tool:
- **The web workstation (net-lab, ADR-0024–0028) OWNS rule-bearing authoring** — config/tunables, waves, sweep-policy params, perks/economy, DDA/tempo, batch-matrix definitions, VFX specs, UI artifacts, and scene-LAYOUT data. These export as truth (ADR-0011).
- **The Unity Editor OWNS engine-native authoring ONLY** — prefabs, `.unity` scenes, URP/materials/shaders, sprite + animation import, addressables, the build, and the double→float View projection.
- **Unity Editor windows are IMPORTERS / INSPECTORS / RUNNERS, never rule-authoring surfaces:** config-import (ADR-0007), a read-only parity/telemetry/golden-vector inspector, the persona *runner* (generates traces to check the port), and SO generators that transform exported specs. Any hand-authored rule-bearing data in-editor is an **Untwinned Port** = ledgered fidelity debt (ADR-0011's one-way law).

This REVISES the original "Unity Editor IS the Design Workstation" call, which claimed authoring authority superseding net-lab 0024–0028. See ADR-0019 and `docs/reviews/2026-07-16-4call-stress.md` (adr1-workstation verdict: NARROW).

**Rationale.** The sound half of the original thesis survives: because Contracts+Core are engine-free C#, Editor windows run the real sim headless in edit mode, so Unity is a genuine port-VERIFICATION surface. But rebuilding rule-bearing authoring in-editor recreates the estate's documented death loop — parallel authoring surfaces → drift → abandonment (net-lab already measured a 55.5%-fidelity resim drift from dual authoring). pawfall's real Unity editor surface is almost entirely engine-native asset pipeline; its rule-bearing tuning already lives as exported data, not in editor windows.

**Alternatives.** Unity as authoring authority superseding net-lab (rejected: dual rule-bearing surfaces, the exact drift census); freeze net-lab so Unity can own authoring (rejected: ADR-0011 keeps net-lab a LIVE oracle whose export is re-run, so it stays authored); IMGUI/Odin tooling debates (moot once Unity stops authoring rules).

**Consequences.** The 7 non-functional windows are re-intented author→inspect/import (header comments only this pass; no functional change). The functional Config/Key **Editor** window keeps its place because it already *imports* per ADR-0007 (not re-typing literals). NetNinja.Editor stays a load-bearing seam but a CONSUME/VERIFY one; config remains text-serialized SO under version control, imported (never hand-typed) for parity keys.

# ADR-0006 — VContainer composition root with View wired as ILateTickable

> **Superseded by ADR-0019** (stack strip): VContainer/R3/MessagePipe/UniTask are removed; the composition root is a plain `Bootstrap : MonoBehaviour` (FixedUpdate → sim step, LateUpdate → view-apply). The view-never-writes-core intent is preserved structurally by the seam, not by DI entry-point ordering. Retained below as the historical record.

**Decision.** Adopt VContainer (MIT) with Root/Game/EditorTool LifetimeScopes. The Sim steps via an IFixedTickable entry point owning DT=1/60; View is registered in NetNinja.Composition as an ILateTickable that runs strictly after. To make this real, NetNinja.View references VContainer and NetNinja.Composition references NetNinja.View. Core/Contracts have zero DI reference.

**Rationale.** Nested scopes mirror the seams; entry-point ordering structurally enforces view-never-writes-core — but only if View actually participates in the VContainer lifecycle, which the prior graph did not wire (View referenced neither VContainer nor Composition). Adding those two references makes the 'structurally enforced' claim true, backed by a Composition tick-order test.

**Alternatives.** View outside DI with a plain ordering test only (kept as the fallback assertion but rejected as the sole guarantee); Zenject (rejected: heavier, reflection scans); hand-rolled DI at the edge (rejected: merge-conflict-prone bootstrapper).

**Consequences.** Enable VContainer.SourceGenerator + a WebGL smoke test before trusting AOT; exactly one owner of the 1/60 tick (the IFixedTickable entry point).

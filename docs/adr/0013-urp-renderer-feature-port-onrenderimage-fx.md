# ADR-0013 — URP renderer feature port of the Built-in OnRenderImage FX rig

**Decision.** Ship one URP Universal Renderer with two quality-tier assets (Mobile, WebGL). Rebuild the Built-in verlet-net ortho-overlay OnRenderImage FX as a URP ScriptableRendererFeature using Render Graph AddBlitPass + Volume overrides; the overlay camera becomes a stacked Overlay camera or a BeforeRenderingPostProcessing pass. A View.Tests smoke asserts the feature constructs.

**Rationale.** URP ignores OnRenderImage; Render Graph is mandatory in URP 17/Unity 6. This is net-lab risk #1; giving it a compile/construction smoke test this pass ensures the riskiest presentation port is not entirely uncovered.

**Alternatives.** Keep Built-in pipeline (rejected: locked to URP); Compatibility Mode CommandBuffer.Blit (rejected: deprecated); zero test coverage (rejected: net-lab risk #1 unguarded).

**Consequences.** Forward (not Forward+) path, HDR off, MSAA ≤2x, render scale as a config key; FX must not leak into Core/Adapters; prove FX on the WebGL/GLES3 + arm64 targets.

# Net Ninja — Architecture Decision Records

One file per decision (append-only; see [ADR-0014](0014-ai-navigable-docs-append-only-adrs.md)). Historical record; superseded ADRs stay on disk.

- [ADR-0001](0001-unity-editor-authoring-port-verification-surface.md) — Unity Editor is the engine-native authoring + port-verification surface, not a second design authority
- [ADR-0002](0002-determinism-portable-double-only-fp-allowlist.md) — Determinism via portable double-only FP with an ALLOWLIST analyzer
- [ADR-0003](0003-no-burst-dots-unity-mathematics-fp-contract.md) — No Burst/DOTS/Unity.Mathematics in the core; IL2CPP fp-contract=off
- [ADR-0004](0004-isweeppolicy-strategy-selected-by-config.md) — ISweepPolicy strategy selected by config
- [ADR-0005](0005-typed-telemetry-contract-bus.md) — Typed telemetry contract bus — schema in Contracts, sink in Core, MessagePipe at the edge
- [ADR-0006](0006-vcontainer-composition-root-ilatetickable.md) — VContainer composition root with View wired as ILateTickable
- [ADR-0007](0007-scriptableobject-config-imported-parity-literals.md) — ScriptableObject config authority with imported (not re-typed) parity literals
- [ADR-0008](0008-persona-replay-injection-iinputreader-seam.md) — Persona/replay injection at the real IInputReader seam, drivers in the engine-free core
- [ADR-0009](0009-layered-embedded-upm-packages-one-asmdef-per-seam.md) — Layered embedded-UPM packages, one asmdef per seam, Core references only Contracts
- [ADR-0010](0010-worktree-vcs-discipline.md) — Worktree + VCS discipline (Force Text, LFS, SmartMerge, per-worktree Library)
- [ADR-0011](0011-twin-codebase-golden-vector-parity.md) — Twin-codebase golden-vector parity, TS as oracle, export is a blocking prerequisite
- [ADR-0012](0012-gameci-editor-first-ci-pure-dotnet-tier1.md) — GameCI editor-first CI with a pure-.NET tier-1 gate and license placeholders
- [ADR-0013](0013-urp-renderer-feature-port-onrenderimage-fx.md) — URP renderer feature port of the Built-in OnRenderImage FX rig
- [ADR-0014](0014-ai-navigable-docs-append-only-adrs.md) — AI-navigable docs: terse AGENTS.md + append-only ADRs + generated boot brief + hashing spec
- [ADR-0015](0015-parity-rings-wasm-only.md) — Parity rings are wasm-only: dotnet CoreCLR gate + WebGL ship gate + mobile-vs-desktop wasm cross-hash
- [ADR-0016](0016-pinned-hashing-canonicalization-spec.md) — Pinned hashing canonicalization spec (FNV state + configHash)
- [ADR-0017](0017-state-read-model-dto-double-only-crossing.md) — State read-model DTO in Contracts; double-only crossing, float only in View
- [ADR-0018](0018-persona-transcendentals-provisional-sim-only-parity.md) — Persona transcendentals are provisionally accepted and non-gating; sim is the only parity surface (OWNER-PENDING)
- [ADR-0019](0019-right-sizing-skeleton-asmdefs-stack-strip-wasm.md) — Right-sizing the skeleton (packages→asmdefs, stack strip, workstation boundary, wasm-only rings)


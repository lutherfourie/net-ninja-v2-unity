# ADR-0012 — GameCI editor-first CI with a pure-.NET tier-1 gate and license placeholders

**Decision.** Two-tier check. Tier-1 (every diff, NO Unity license): pure-.NET `dotnet test` of the golden vectors + self-determinism + hasher micro-vector + configHash, plus the Roslyn analyzer, plus the law guard. Tier-2 (CI, license required): GameCI game-ci/unity-test-runner EditMode/PlayMode + on-target arm64/WebGL parity + Android/iOS/WebGL builds, with documented Unity license placeholders. iOS on Linux stops at the Xcode project; device build/parity runs on a documented macOS runner.

**Rationale.** Editor-first now; parity harness runs immediately. Crucially, because Contracts+Core are engine-free, the fast per-diff gate can run without launching Unity — this removes the Unity Personal seat-contention that would otherwise serialize N parallel agents each spinning batchmode Unity. WebGL/arm64 rings catch on-target float leakage the Mono editor hides.

**Alternatives.** Unity batchmode on every diff (rejected: seat contention undermines the parallel-worktree thesis); no CI until gameplay (rejected: parity must gate from day one); cloud paid CI (rejected: free/OSS-only).

**Consequences.** License secrets are documented placeholders; nightly/pre-merge for expensive on-target parity; tier-1 stays fast so agents self-verify without a seat.

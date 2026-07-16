# ADR-0015 — Parity rings are wasm-only: dotnet CoreCLR gate + WebGL ship gate + mobile-vs-desktop wasm cross-hash

**Status.** OWNER RULING (Luther, 2026-07-16): Net Ninja v2 ships as a mobile-first BROWSER game (WebGL/wasm, portrait). No native mobile target is planned. This SUPERSEDES the original "on-target parity gate is Android arm64 (primary) plus WebGL" decision, and is stronger than the review's adr15-ring verdict (which kept arm64 as a non-ship divergence-canary): the arm64 ring is DELETED, not merely relabeled, because there is no arm64 shipped surface to canary. See `docs/DESIGN-PREMISE.md` and ADR-0019.

**Decision.** Three concentric rings, all scoped to the actually-shipped wasm surface:
- **Ring 0 — dotnet CoreCLR parity (LIVE merge gate, 15/15).** Pure managed .NET; no Unity, no license; the tier-1 gate on every diff (ADR-0012).
- **Ring 1 — WebGL / wasm ship gate (PLACEHOLDER).** Gates the artifact the game actually releases. Placeholder until the node/puppeteer headless hash-capture harness lands; then it replays the oracle traces and asserts runHash parity vs Ring 0.
- **Ring 2 — mobile-browser vs desktop-browser wasm cross-hash (PLACEHOLDER).** The honest shipped-surface check: same wasm build, mobile vs desktop browser, assert identical runHashes. Expected ZERO divergence under strict IEEE-754 wasm (no FMA); placeholder until the harness exists.

**Flip condition.** A native mobile release (Play/App Store IL2CPP arm64 binary, not browser) RESURRECTS arm64 as the divergence-canary ring — clang can contract `a*b+c → fma` on ARM64, the one case no wasm target hits — and restores the fp-contract=off proof (ADR-0003). Until such a ship exists, arm64 is not a ring.

**Rationale.** WebAssembly f64 is strict IEEE-754 with no FMA. The original ADR kept arm64 as PRIMARY on the premise "the mobile-first product ships on exactly the ARM targets" — false: it ships WebGL. With no native target, an arm64 merge gate blocks around a surface that is never released while under-building the WebGL harness that gates the real one. Ring 2 keeps an on-shipped-surface canary (mobile vs desktop browser) that is honest about what actually ships.

**Alternatives.** Keep arm64 as a non-ship divergence-canary (the review's verdict; rejected by owner: no native ship to de-risk, so it is pure CI/seat debt); WebGL-only single ring (rejected: loses the mobile-vs-desktop cross-hash, the only on-shipped-surface divergence check); arm64-primary as originally written (rejected: gates a non-shipped target).

**Consequences.** `.github/workflows/arm64-parity.yml` + `scripts/run-arm64-parity.sh` are deleted; `docs/parity-rings.md` is rewritten to the three-ring wasm-only model; the node/puppeteer WebGL harness is the real Ring 1/2 build-out. iOS device parity is no longer a ring (browser-only); `ProjectSettings/IL2CPP-NOTES.md`'s fp-contract=off guidance stays as dormant native-build engineering, activated only by the flip condition.

# ADR-0014 — AI-navigable docs: terse AGENTS.md + append-only ADRs + generated boot brief + hashing spec

**Decision.** Root AGENTS.md (20-40 lines, hand-written) with a one-line CLAUDE.md shim @AGENTS.md; nested ~15-line AGENTS.md per assembly seam (Core carries the determinism commandments and points at docs/hashing-spec.md); append-only MADR ADRs in docs/adr/; BOOT-BRIEF.generated.md derived by Tools/gen-bootbrief and failing check if git-dirty. Optional CoplayDev unity-mcp for agent verify loops only.

**Rationale.** Short hand-written instruction files beat auto-generated bloat; append-only ADRs are conflict-free under parallel worktrees; the generated brief is the /understand map. The hashing spec is added as a first-class doc because canonicalization is parity-blocking.

**Alternatives.** Auto-generated AGENTS.md (rejected: higher cost, lower agent success); hand-maintained boot brief (rejected: rots).

**Consequences.** Core AGENTS.md is the loudest guardrail; MCP agents still go through human-gated check + fetch-before-push + additive-edit discipline.

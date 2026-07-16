# ADR-0010 — Worktree + VCS discipline (Force Text, LFS, SmartMerge, per-worktree Library)

**Decision.** Force Text serialization + Visible Meta Files (committed EditorSettings), Git LFS via macro .gitattributes for binaries with golden/vectors.json and config kept PLAIN TEXT, UnityYAMLMerge SmartMerge for .unity/.prefab/.asset, one git worktree per agent each with its own Library, github Unity .gitignore, commit manifest.json + packages-lock.json.

**Rationale.** Version control is a determinism-preserving pipeline: diffable config, auto-mergeable scenes, reproducible package graphs, conflict-free parallel worktrees.

**Alternatives.** Binary serialization (rejected: unmergeable scenes); LFS on all .asset (rejected: swallows text config); shared Library across worktrees (rejected: Unity locks Library, corrupts import state).

**Consequences.** Commit .gitattributes + git lfs install before the first binary; new-worktree seeds a fresh Library and distinct editor title; check gate lints meta discipline and LFS scope via git lfs ls-files.

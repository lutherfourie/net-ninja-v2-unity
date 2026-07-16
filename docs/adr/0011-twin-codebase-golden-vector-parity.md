# ADR-0011 — Twin-codebase golden-vector parity, TS as oracle, export is a blocking prerequisite

**Decision.** net-lab (TS) remains the reference oracle: a pnpm export:golden emits golden/vectors.json + config/*.json (baked literals). AUTHORING/VERIFYING that these exports exist and emit configHash 6c3a8288f02919a3 is an explicit BLOCKING task at the front of this pass. The C# side loads that JSON, runs the ported sim, and asserts FNV run+checkpoint hashes with ZERO tolerance for core hashes; ULP tolerance only at the untwinned view boundary (never a merge gate). A configHash cross-check catches config drift.

**Rationale.** Restricting the op-set makes exact parity winnable; exporting literals prevents transposed-digit drift. But the whole gate is downstream of an export script whose existence was an open question — so it must be a named prerequisite, not an assumption, or the pass cannot deliver its headline claim.

**Alternatives.** Re-type config literals in C# (rejected: silent drift); epsilon tolerance on core hash (rejected: defeats self-determinism); assume the export exists (rejected: unverified, blocks everything).

**Consequences.** Three CI rings plus a pure-.NET tier-1 gate; if the export does not exist it is authored against the TS oracle first; golden vectors and config stay diffable, non-LFS.

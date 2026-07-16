# ADR-0009 — Layered embedded-UPM packages, one asmdef per seam, Core references only Contracts

> **Superseded by ADR-0019** (right-sizing): the nine embedded UPM packages collapse to FIVE asmdefs under `Assets/_NetNinja/` (Contracts, Core, View, Editor, App); only `com.netninja.determinism-analyzer` stays a package. The engine-free Contracts/Core asmdefs (noEngineReferences) and Core→Contracts-only edge are preserved verbatim — the compiler-enforced parity spine was never the package ceremony. Retained below as the historical record.

**Decision.** Structure the repo as embedded UPM packages under Packages/ (com.netninja.contracts, .core, .config, .adapters, .view, .composition, .telemetry, .editor, .determinism-analyzer) with strict acyclic references, references by GUID, autoReferenced:false on Contracts/Core. NetNinja.Core references EXACTLY [NetNinja.Contracts]; every other consumer references Contracts (never Core internals) where possible. C# logic stays out of Assets/.

**Rationale.** net-lab's lesson: 26 folders / ~5 asmdefs = de-facto monolith. The prior graph declared Core references:[] yet Core must consume InputFrame/ISweepPolicy/ITelemetrySink — impossible. Adding the single Core→Contracts edge (Contracts being engine-free) makes the graph compile while keeping Core engine-free and all noEngineReferences criteria intact.

**Alternatives.** Core references:[] with contract types duplicated into Core (rejected: two homes for one CLR type, unresolvable); folder-only split (rejected: no compiler enforcement); one asmdef per topic (rejected: splits parity logic).

**Consequences.** Parity-critical POCOs (policies, hasher, personas) stay in ONE Core assembly so it hashes as a unit; each package has package.json + Runtime/Editor/Tests; commit all .meta and packages-lock.json.

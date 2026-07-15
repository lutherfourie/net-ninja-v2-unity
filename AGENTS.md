# Net Ninja — agent brief

- **Repo:** standalone Unity 6000.4.3f1 / URP. NOT gamespree.
- **Seams:** Contracts (engine-free) ← Core (engine-free, Core→Contracts only) ← Config/Adapters/View/Composition/Editor.
- **Parity:** `scripts/check.ps1` = tier-1 pure .NET. No Unity license. Golden via `golden/traces` + `Sim`.
- **Determinism:** double only in Contracts/Core; allowlist analyzer; DT=1/60; FNV hash per `docs/hashing-spec.md`.
- **Never** push/merge from agent; never touch net-lab or gamespree.
- **Config:** import `config/default.json` via Config/Key Editor — do not re-type parity literals.

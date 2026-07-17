# Net Ninja — agent brief

- **Repo:** standalone Unity 6000.4.3f1 / URP. NOT gamespree.
- **Seams (5 asmdefs under `Assets/_NetNinja/`, ADR-0019):** Contracts (engine-free) ← Core (engine-free, Core→Contracts only) ← View / Editor / App. App = Config+Adapters+Composition+Telemetry merged; composition root is `App/Bootstrap.cs` (plain MonoBehaviour: FixedUpdate→sim step, LateUpdate→view-apply). Only `com.netninja.determinism-analyzer` stays a UPM package.
- **Parity:** `scripts/check.ps1` = tier-1 pure .NET. No Unity license. Golden via `golden/traces` + `Sim`.
- **Determinism:** double only in Contracts/Core; guard = Roslyn allowlist analyzer (`Tools/determinism-analyzer`, wired into parity via `OutputItemType=Analyzer` + Unity package `Editor/RoslynAnalyzers/*.dll` labelled `RoslynAnalyzer`) plus regex belt in `AnalyzerTripTests`; red-trip: `dotnet build Tools/parity-dotnet/... -p:TripAnalyzer=true`; DT=1/60; FNV hash per `docs/hashing-spec.md`.
- **Never** push/merge from agent; never touch net-lab or gamespree.
- **Config:** import `config/default.json` via Config/Key Editor — do not re-type parity literals.

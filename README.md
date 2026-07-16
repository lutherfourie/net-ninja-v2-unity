# Net Ninja (Unity)

Editor-first Unity 6000.4.3f1 / URP edition of Pawfall's net-catch mechanic.

## Tier-1 check (no Unity license)

```powershell
./scripts/check.ps1
```

## Structure

Five asmdefs under `Assets/_NetNinja/` — `NetNinja.Contracts`, `NetNinja.Core` (both engine-free),
`NetNinja.View`, `NetNinja.Editor`, `NetNinja.App` (Config+Adapters+Composition+Telemetry merged).
Only `com.netninja.determinism-analyzer` remains an embedded UPM package. See `AGENTS.md`, `docs/adr/ADRS.md`
(ADR-0019), and `docs/`.

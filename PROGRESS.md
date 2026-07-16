# Net Ninja skeleton — PROGRESS

**Branch:** `feat/skeleton`  
**Date:** 2026-07-15

## Status
| Slice | Status |
|-------|--------|
| 1 — deterministic parity spine | **GREEN** (verified) `49ce357` |
| 2 — Unity shell | **STRUCTURAL COMPLETE** (Unity batchmode deferred) |

## Slice 1 — `dotnet test` (paste)
```
Passed!  - Failed:     0, Passed:    15, Skipped:     0, Total:    15, Duration: 177 ms
NetNinja.Core.Parity.Tests.dll (net10.0)
```

- configHash `6c3a8288f02919a3` bit-exact
- 6 golden cells via oracle traces → C# Sim
- Self-determinism, hasher, analyzer trip tests
- ADR-0008: live plant Log/Cos ULP @ tick 125 (documented)

## Slice 2 delivered
- Packages: config, adapters, view, composition, telemetry, editor (+ determinism-analyzer package.json)
- Asmdefs per asmdefGraph (name refs; Unity will GUID-resolve on import)
- Config/Key Editor FUNCTIONAL (import default → SO + hash badge); 7 window stubs
- Packages/manifest.json + packages-lock.json (OpenUPM VContainer/MessagePipe/R3/UniTask)
- ProjectSettings (serializationMode:2 Force Text), IL2CPP-NOTES
- .gitattributes / .gitignore / AGENTS.md / CLAUDE.md / docs/*
- .github/workflows: check.yml (tier-1 live) + unity/arm64/webgl/build-matrix with UNITY_LICENSE placeholders
- scripts: check-full, new-worktree, setup-smartmerge, parity runners (stubs)

## Explicitly deferred
- Unity batchmode compile / EditMode UTF (no license; expected)
- Live persona golden without oracle traces (needs fdlibm Log/Cos)
- packages-lock exact Unity resolve (rewrites on first Editor open)
- Scene YAML (Boot/Game/ConformanceHarness) — .gitkeep placeholders

## Verify
```powershell
./scripts/check.ps1
```

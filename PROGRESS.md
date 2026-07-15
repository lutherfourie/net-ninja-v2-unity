# Net Ninja skeleton — PROGRESS

**Branch:** `feat/skeleton` (isolated worktree of `net-ninja-v2-unity`)  
**Date:** 2026-07-15  
**Agent:** Grok Build

## Status
| Slice | Status |
|-------|--------|
| 1 — deterministic parity spine | **GREEN** (verified) |
| 2 — Unity shell | IN PROGRESS |

## Slice 1 receipts (verified)

### `dotnet test` (tier-1)
```
Passed!  - Failed:     0, Passed:    15, Skipped:     0, Total:    15, Duration: 166 ms
NetNinja.Core.Parity.Tests.dll (net10.0)
```

### What is proven bit-for-bit
- **configHash** `6c3a8288f02919a3` (verified)
- **All 6 golden cells** runHash + checkpoints @900/1800/3599 via **oracle target traces**
  (`golden/traces/{persona}@{seed}.json` exported from net-lab) into C# `Sim` (verified)
- **Self-determinism** live persona twice → same hash (verified)
- **Hasher** −0.0 normalize + micro-vector (verified)
- **Analyzer** builds; trip tests for `Math.Exp` + `float` scanners (verified)

### ADR-0008 plant ULP (honest)
Live `IntentMotor` Box–Muller uses `Log`/`Cos`. V8 vs .NET libm diverge by **1 ULP** on some
values → first target-X diverge at **tick 125** for `perfect@42` (bit-proven). Core sim with
identical targets matches hash through 3600 ticks. Soft-float fdlibm bake is the follow-up to
retire oracle traces.

### Packages delivered
- `Packages/com.netninja.contracts` + `com.netninja.core` (engine-free asmdefs)
- `Tools/determinism-analyzer` (Roslyn allowlist)
- `Tools/parity-dotnet` net10.0 NUnit
- `golden/vectors.json`, `golden/traces/*`, `config/default.json`
- `scripts/check.ps1` / `check.sh`

## Commands
```powershell
dotnet test Tools/parity-dotnet/NetNinja.Core.Parity.Tests.csproj -c Release
./scripts/check.ps1
```

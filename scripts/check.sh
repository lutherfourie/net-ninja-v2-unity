#!/usr/bin/env bash
# Tier-1 gate: pure .NET parity + analyzer (NO Unity license).
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
echo "== tier-1: determinism analyzer build =="
dotnet build Tools/determinism-analyzer/NetNinja.Determinism.Analyzer.csproj -c Release -v q
echo "== tier-1: golden parity + self-determinism + hasher + analyzer trip =="
dotnet test Tools/parity-dotnet/NetNinja.Core.Parity.Tests.csproj -c Release --nologo
echo "TIER-1 CHECK OK"

# Tier-1 gate: pure .NET parity + analyzer (NO Unity license).
$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path (Join-Path $Root "Tools/parity-dotnet"))) {
  $Root = $PSScriptRoot + "/.."
}
Set-Location $Root

Write-Host "== tier-1: determinism analyzer build ==" -ForegroundColor Cyan
dotnet build Tools/determinism-analyzer/NetNinja.Determinism.Analyzer.csproj -c Release -v q
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "== tier-1: golden parity + self-determinism + hasher + analyzer trip ==" -ForegroundColor Cyan
dotnet test Tools/parity-dotnet/NetNinja.Core.Parity.Tests.csproj -c Release --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "TIER-1 CHECK OK" -ForegroundColor Green

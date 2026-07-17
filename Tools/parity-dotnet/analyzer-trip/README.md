# Analyzer trip harness

Deliberate red-build check for the wired Roslyn allowlist analyzer (`NNDET001`/`NNDET002`/`NNDET003`).

## Green (default)

```powershell
dotnet build Tools/parity-dotnet/NetNinja.Core.Parity.Tests.csproj -c Release
```

`analyzer-trip/DisallowedCall.cs` is **not** compiled. Zero `NNDET*` errors expected.

## Red (trip)

```powershell
dotnet build Tools/parity-dotnet/NetNinja.Core.Parity.Tests.csproj -c Release -p:TripAnalyzer=true
```

Compiles `DisallowedCall.cs` (`float` + `System.Math.Exp`) so the analyzer must emit **NNDET001** and **NNDET002**. Non-zero exit is the success signal. Captured reference output: `EXPECTED-RED.txt`.

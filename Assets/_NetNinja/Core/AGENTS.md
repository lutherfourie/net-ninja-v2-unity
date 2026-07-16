# NetNinja.Core

Determinism commandments:

1. `double` only. No `float`.
2. Portable ops: `+ - * /` and `Math.Sqrt` (+ allowlisted Abs/Min/Max/Floor/Ceiling/Truncate).
3. IntentMotor plant (bot-only) provisionally uses Log/Log2/Cos — provisionally accepted, non-gating; ADR-0008 open question, resolution OWNER-PENDING (ADR-0018).
4. No UnityEngine, Burst, Unity.Mathematics, Mathf, Random.
5. Fixed DT = 1/60. Seeded RNG only.
6. Hashing: see `docs/hashing-spec.md`.

Enforcement (as-shipped): the live guard is a REGEX SCAN (`\bfloat\b` + `Math.Exp`) in `AnalyzerTripTests`. The Roslyn allowlist analyzer exists but is NOT yet wired as an `<Analyzer>` — wiring is a declared fast-follow (ADR-0018).

References **exactly** `[NetNinja.Contracts]`.

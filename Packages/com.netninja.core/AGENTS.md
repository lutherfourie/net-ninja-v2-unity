# NetNinja.Core

Determinism commandments:

1. `double` only. No `float`.
2. Portable ops: `+ - * /` and `Math.Sqrt` (+ allowlisted Abs/Min/Max/Floor/Ceiling/Truncate).
3. IntentMotor plant provisionally uses Log/Log2/Cos (ADR-0008 open question for golden parity).
4. No UnityEngine, Burst, Unity.Mathematics, Mathf, Random.
5. Fixed DT = 1/60. Seeded RNG only.
6. Hashing: see `docs/hashing-spec.md`.

References **exactly** `[NetNinja.Contracts]`.

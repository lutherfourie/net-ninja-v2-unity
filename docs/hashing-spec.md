# Hashing spec — FNV state hash + configHash

Pinned canonicalization for the parity gate (ADR-0016). This documents **exactly what the
shipped code does**, not what it should do. Source of truth: `Packages/com.netninja.core/Runtime/State/FnvStateHasher.cs`
(the `Hasher` class + `FnvStateHasher`), a byte-for-byte port of net-lab's oracle
`packages/core/state.ts`. Any divergence between this doc and that code is a doc bug.

## Hash primitive — two-lane 32-bit FNV-1a → 16 hex chars

Two independent FNV-1a lanes, each 32-bit with `unchecked` (mod 2^32) wraparound:

| Lane | init | prime |
|---|---|---|
| a | `0x811c9dc5` | `0x01000193` |
| b | `0x811c9dc5 ^ 0x9e3779b9` (unsigned) | `0x01000197` |

Per input byte `x`: `a = (a ^ x) * primeA`, `b = (b ^ x) * primeB` (both `unchecked` uint).
Output = `a.ToString("x8") + b.ToString("x8")` → 16 lowercase hex chars. Two lanes make
accidental collisions drift-visible; the second prime is the only lane difference.

## Byte encoders (feed order is load-bearing)

- **`U32(n)`** — 4 bytes **little-endian**: `n&255`, `(n>>8)&255`, `(n>>16)&255`, `(n>>24)&255`. `int` overload casts unchecked to uint first.
- **`Num(double x)`** — normalize `-0.0 → +0.0` via `x + 0.0` (exact for all finite x), then IEEE-754 **8-byte little-endian** (`BitConverter.GetBytes`, reversed on big-endian hosts), feeding 8 bytes. Values are hashed as **raw IEEE-754 bytes — never as a formatted decimal string**. Integers/longs pass through `Num` too (widened to double).
- **`Bool(v)`** — one byte, `1` or `0`.
- **`Str(s)`** — `U32(s.Length)` then `U32(charCode)` for each UTF-16 code unit `s[i]`.

### NaN / -0.0 policy (as-shipped)

- `-0.0` **is** normalized to `+0.0` (via `+ 0.0`).
- **NaN is NOT asserted or guarded** in the shipped `Num`; it is hashed as its raw bytes.
  ADR-0016 intends "NaN forbidden in hashed state (assert, do not hash)" — that assertion is
  **not implemented** in `FnvStateHasher`. The sim is expected not to produce NaN in hashed
  state; there is currently no runtime guard enforcing it. (Honest gap; not a parity risk
  unless the sim ever emits NaN, in which case it would surface as divergence, not an assert.)

## State hash — `FnvStateHasher.HashState(Sim)`

Fields are fed in **exactly** this order. Maps are hashed by **sorted key** (removes the
JS-insertion-order vs C#-`Dictionary` ordering hazard). Enums are hashed as the **ordinal
index of their string name** in a fixed table.

1. `sim.Time`
2. **Net:** `Pos.X, Pos.Y, Target.X, Target.Y, VelX, VelY, FacingX, FacingY, PrevX, PrevY, PrevFacingX, PrevFacingY`; `DynamicCapacity` (null → `-1`); `U32(Held.Count)`; then each `Held[i].Id`.
3. **Score:** `Score, Combo, Lives, Misses, WaveCatches, Temper`; then trackers `Sweeps`, then `Trails`.
4. **Cat / WaveManager FSM:** `U32(phaseIndex)`; `CatX, PhaseEndsAt, NextSpawnAt, CeremonyIndex, LastSweepAt, PushedInCeremony, NextPushAt, CurrentSweepId, SweepSerial, CurrentTrailId, TrailSerial, ObjectSerial`; **Pending** — `Bool(present)`, and if present `Count, Direction, StaggerSeconds, Bool(IsSweep)` then each `Xs[i]`; **Telegraph** — `Bool(present)`, and if present `Direction, EndsAt` then each `Xs[i]`.
5. `sim.Tempo.CatIntensity`.
6. **Objects:** `U32(count)`, then per object `Id`, `U32(fallStateIndex)`, `Pos.X, Pos.Y, VelY, VelX, SweepId, TrailId, Bool(Banked), Radius, Value`.
7. **Sim latches:** `ReturnStrokeStartedAt`, `Bool(WasFull)`.

### Tracker sub-hash — `HashTrackers`

Sort the `Dictionary<int, SweepTracker>` keys **ascending**, `U32(count)`, then per key:
`Num(key), Expected, Caught, StartedAt, Deadline, Bool(Failed)`.

### Enum ordinal tables (index via `Array.IndexOf`)

- **Phases** = `{ "cooldown", "walk", "anticipate", "telegraph", "pushing" }` (`WaveManager.PhaseName`).
- **FallStates** = `{ "falling", "caught", "landed" }` (`FallingObject.FallStateName`).

## Config hash — `FnvStateHasher.HashConfig(CoreConfig)`

Iterate keys via `CoreConfig.SortedKeys()` = `StringComparer.Ordinal` ascending (matches
net-lab's `Object.keys(cfg).sort()`, which orders by UTF-16 code unit — identical for the
ASCII dotted keys used). Per key: `Str(key)`, then the value by type:

| value type | encoder |
|---|---|
| `double` / `int` / `long` | `Num` (raw IEEE-754 8-byte LE) |
| `bool` | `Bool` |
| anything else (string) | `Str(Convert.ToString(v))` |

**Pinned target:** `configHash(config/default.json) == 6c3a8288f02919a3` (asserted by the
tier-1 gate; drift = re-export `config/default.json` from the net-lab oracle).

## Why this is portable

FNV-1a over canonical little-endian IEEE-754 bytes + sorted map keys + string-ordinal enums
means a C#/Rust/WASM port is correct **iff** it reproduces the committed golden `hashState`
sequence tick-for-tick. One mismatched byte diverges every downstream hash — hence the
hasher micro-vector and configHash micro-check are the first parity tests in the gate.

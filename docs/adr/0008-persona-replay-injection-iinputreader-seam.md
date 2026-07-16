# ADR-0008 — Persona/replay injection at the real IInputReader seam, drivers in the engine-free core

**Decision.** IInputReader emits a per-tick InputFrame (Contracts). DeviceInputSource (Input System 1.19) is the engine-coupled production binding in Adapters. PersonaDriver (Perfect/Average/Sloppy — Motor-Plant coefficients baked to literals, latency as integer tick delay, noise from seeded Hash01) and ReplayDriver are the ENGINE-FREE implementations in NetNinja.Core, under the same allowlist analyzer. Never inject at the wall-clock event layer.

**Rationale.** The 6 golden cells are persona-driven, so persona generation MUST be bit-exact — yet the prior plan placed it in Adapters, outside the determinism guard. Moving the deterministic driver into Core puts it under the analyzer and the pure-.NET gate, guaranteeing the cells reproduce; only device I/O stays engine-coupled.

**Alternatives.** Personas in Adapters (rejected: unguarded parity code); side-channel feed (rejected: not the real pipeline); event-layer injection (rejected: wall-clock timestamps).

**Consequences.** Open-question on Fitts/min-jerk/Gaussian reducibility must be resolved BEFORE the port; if any term needs runtime transcendentals it must be pre-baked or the cell is unreproducible; InputEventTrace is human-capture only, resampled offline into per-tick frames.

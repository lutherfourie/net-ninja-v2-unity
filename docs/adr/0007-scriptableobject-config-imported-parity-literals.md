# ADR-0007 — ScriptableObject config authority with imported (not re-typed) parity literals

**Decision.** All net.*/tempo.*/autopilot.* tunables live in NetNinjaConfigSO as List<KeyEntry{string key;double value}> fronted by ConfigService.GetDouble(key, codeDefault), flattened into an immutable CoreConfig struct at boot. Parity-gated keys (the golden config) are IMPORTED from exported config/*.json via Tools/config-import; only non-parity keys are hand-authored in a window. lean path = null bindings for byte-identical rollback.

**Rationale.** No magic numbers; every tunable is diffable text. But hand-typing 0.2591817793182821 into a window IS re-typing a baked literal, which ADR-0011 forbids. Importing the parity keys from the exported JSON reconciles editor-authority (ADR-0001) with never-re-type (ADR-0011).

**Alternatives.** Hand-author everything in the window (rejected: transposed-digit risk on parity keys); JSON-only config with no SO (rejected: loses in-editor authority); hard-coded constants (rejected: no authoring surface).

**Consequences.** config-import is a required tool this pass; List<KeyEntry> keeps SerializedObject binding/undo/prefab-override working; config SOs are read-only-at-runtime, cloned to struct to avoid poisoning the next golden run.

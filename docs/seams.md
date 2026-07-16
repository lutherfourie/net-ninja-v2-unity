# Seams

See ARCHITECTURE-FINAL.json seamMap. Five asmdefs under `Assets/_NetNinja/` (ADR-0019):
Contracts = boundary types; Core = sim+personas (engine-free); View = read-only render;
Editor = importer/inspector/runner surface (not a rule-authoring authority, ADR-0001);
App = engine glue (Config+Adapters+Composition+Telemetry merged) with a plain-MonoBehaviour
`Bootstrap` composition root (no VContainer/Rx/bus, ADR-0006 superseded).

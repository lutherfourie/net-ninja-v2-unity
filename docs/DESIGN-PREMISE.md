# Net Ninja v2 — design premise (owner's words, 2026-07-16)

> "Net Ninja v2 is a mobile first browser game […] we are not going to port pawfall exactly,
> the story goes that 4/5 evil cats summoned spirits that the net ninja need to defeat."
> — Luther, 2026-07-16

## What this means for the build (seed — expand in the Slice 3 contract, do not over-derive)

- **Ship surface:** mobile-first **browser** (WebGL/wasm, portrait). No native mobile target is
  planned — ADR-0015's rings are scoped to wasm accordingly (dotnet CoreCLR gate + WebGL ship
  gate + mobile-vs-desktop wasm cross-hash). arm64 IL2CPP is not a ring.
- **Not a 1:1 pawfall port.** The **mechanic spine carries over** (net-catch model = the net-lab
  oracle; feel doctrine; likely candidates from pawfall's systems: Net, Feel, Waves, Scoring,
  Skills/Perks/PowerUps *as systems*), but the **content and fiction are new**:
  - Premise: **4–5 evil cats** have **summoned spirits**; the **net ninja** must defeat them.
  - Implication: the "falling treats to catch" read becomes (or is joined by) **spirits to
    capture/defeat** — catching as combat. Evil cats read as summoners/bosses/act structure.
  - Cat-cafe theming (Dusk Café Pop as-is) does not carry automatically; art direction for v2
    is its own decision (net-lab's bloom-moat *grammar* may carry even if the palette doesn't).
- **Porting posture:** treat pawfall as a **parts库 to salvage from**, not a target to replicate.
  Every ported system must justify itself against the new premise; the Slice 3 contract names
  which systems come over and which stay behind.

_Provenance: owner statement in-session; recorded verbatim above. This file seeds the Slice 3
contract (goal 6ca3b2a1) and is append-only — new owner rulings get dated additions._

## Ruling 2026-07-16 (owner): brief pending — build for iteration, not for the premise

> "We have yet to be briefed with regard to what the net ninja and story entail, therefore the
> best we can do is to use architecture that makes implementing iteratively easy and simple,
> the code needs to be understandable by humans and not just agents." — Luther

- The premise above is a **teaser, not a brief**. The "implications" section is NOT actionable
  design until the owner briefs the team — do not build spirits/bosses/theming from it.
- **Standing architecture bar (applies to every future call in this repo):** optimize for
  (1) cheap, simple iteration and (2) code a HUMAN can read cold — plain Unity idiom, feature
  folders, minimal indirection, no framework cleverness that only agents can navigate.
  Right-sizing (ADR-0019) is the first application of this bar.
- Slice 3's contract therefore starts with an **owner briefing session** (story, the ninja, the
  fantasy, what "defeat spirits" means mechanically); until then Slice 3 prep is limited to
  premise-neutral spine work.

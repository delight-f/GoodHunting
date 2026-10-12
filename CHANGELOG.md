# Changelog

## 2.0.0

Reworked from a content-patch mod into a code mod. Same tuning by default, now configurable.

**Added**
- `ModConfig/goodhunting2.json` with multipliers: `hideMultiplier`, `fatMultiplier`,
  `meatMultiplier`, `featherMultiplier`, and a global `allMultiplier` applied last.
  `1.0` keeps the original balance; values are clamped to `[0, 100]`.
- `enabled` switch to disable all changes.

**Changed**
- Drop and health data now lives in C# tables (`src/Data/`) keyed by entity type instead of
  position-based JSON patch paths (`/server/behaviors/N/drops`). A game update that shifts
  behaviour order can no longer mis-target a change.
- Behaviour is identical at 1× — verified against the engine (0 differences across 479 entity
  types and 81 fish variants).
- Ships as a single code zip (`GoodHunting.dll` + assets).

**Notes**
- Ships under its own mod id, `goodhunting2`, and writes its config to `goodhunting2.json`. It
  no longer collides with the original Good Hunting, but both rewrite the same animals, so run
  one or the other.
- The chicken render-size tweak from 1.x is preserved as a small named-path client asset patch.

## 1.4.7

Legacy content-patch release. Fourteen JSON patch files adjusting per-species health and
harvest drops. Kept as the known-good rollback.

# Changelog

## 3.1.0

Optional support for other mods' animals, so one set of multipliers can cover a whole modded
world. On by default; set the list empty to opt out.

**Added**
- `moddedAnimalDomains`: which other mods' animals follow your multipliers. Defaults to `["*"]`
  (every modded animal); list domains such as `["bovinae", "caninae"]` to narrow it, or set `[]`
  to leave other mods' animals on their own balance.
- Modded drops are scaled from the values the engine already resolved, so nothing is copied
  from the other mod and a release that adds species or retunes its animals needs no change
  here. The `harvestableDrops` handbook copy clients receive is refreshed, as it already is for
  vanilla animals.
- Species pelts are treated as hides, and a drop's flat `offset` (how Fauna of the Stone Age
  writes "always exactly one pelt") scales with the multiplier rather than sitting outside it.
- The startup log names the domains it scaled, and warns when a configured domain matched no
  installed animal or when an animal has no harvestable drops left to scale.

**Changed**
- Animals from other mods now follow your multipliers by default. If you already raise one of
  them, those animals' yields change on upgrade; set `moddedAnimalDomains` to `[]` for the 3.0.0
  behaviour.

**Unchanged**
- Modded animals' health is not touched. Good Hunting's health values are per vanilla species
  and do not generalise to another mod's animals.
- With every multiplier at `1.0` the modded path writes nothing, so switching the key on
  changes no yields by itself.

**Notes**
- Verified on a dedicated 1.22.7 server with Fauna of the Stone Age `bovinae` and `caninae`
  installed: 313 modded entity types across 2 domains. With the feature on and every multiplier
  at `1.0`, drops, the client handbook copy and health were identical to running the packs
  alone (0 differences). With meat `2.0` and hides `0.5`, all 779 modded drop entries matched
  the expected result, including 308 pelts; fat and health were untouched.

## 3.0.0

Renamed to Good Hunting Forked and made server-side only. Same tuning and config keys.

**Breaking**
- Mod id is now `goodhuntingforked` and the config file `ModConfig/goodhuntingforked.json`.
  Remove `goodhunting2_*.zip`; rename `goodhunting2.json` to keep your settings.
- Server-side only (`"side": "Server"`). Players no longer install it, and only the server's
  config is used.
- Requires Vintage Story 1.22.7 (the version it is built and verified against).

**Fixed**
- The chicken render-size tweak never applied in 2.0.0: its patch targeted the client, which
  never loads entity files. It is a server patch again, and keeps vanilla 1.22's poult size.
- The handbook's "obtained by killing" lists showed vanilla drops, because the game copied
  them for clients before this mod ran. The copy is now refreshed.
- The fish yields in a player's handbook followed that player's own config instead of the server's.
- Health changes no longer replace the whole health behaviour, which wiped any other keys on
  it, including other mods' patches.
- Only `game:` entities and items are matched, so another mod's entity or item with the same
  path is no longer overwritten.
- A malformed config is logged clearly and left untouched; the mod runs at defaults until it is
  fixed.
- Table entries that cannot be applied (e.g. renamed by a game update) are listed in a warning
  instead of being skipped silently; the load summary counts only what was applied.

**Changed**
- Ships the original mod icon.
- Tests build and run without a Vintage Story install; `tools/package.sh` runs them first.

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

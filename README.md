# Good Hunting 2

Realistic animal health and harvest yields for [Vintage Story](https://www.vintagestory.at/),
with configurable drop multipliers.

Good Hunting 2 is the successor to the original Good Hunting. It keeps the same tuning --
leaner, more believable yields per species and sex, so a big bear actually yields more than a
rabbit -- but ships it as a code mod with a config file, so you can dial the drops up or down
without editing a single JSON patch.

**Target game version:** Vintage Story 1.22.7 (.NET 10)

---

## What 2.0 changes

The 1.x releases were a *content* mod: fourteen JSON patch files that surgically rewrote each
animal's drop and health values. That worked, but it had two drawbacks:

- Every patch addressed a behaviour by its **array position** (`/server/behaviors/6/drops`).
  If a game update inserted a behaviour, the patch silently landed on the wrong thing.
- There was no way to change the numbers without editing the shipped files.

2.0 moves the same data into **C# tables** and applies it at load time. Benefits:

- **No positional patch paths** — values are keyed by entity type, so a game update can't
  quietly mis-target a change.
- **A config file** with multipliers for hide, fat, meat, feathers, or everything at once.
- **Identical drops at 1x.** With every multiplier left at `1.0`, the drops are numerically
  identical to what 1.4.7 produced — verified against the engine itself (see *Verification*).
- **One zip** that behaves the same on client and server.

---

## Installation

1. Drop `goodhunting2_2.0.0.zip` into your `Mods` folder.
   - Client: `~/.var/app/at.vintagestory.VintageStory/config/VintagestoryData/Mods/`
     (Flatpak) or `%appdata%/VintagestoryData/Mods/` (Windows).
   - Server: `<server data>/Mods/`.
2. **Remove the original Good Hunting mod if you have it.** This mod uses its own id
   (`goodhunting2`), so the two no longer collide — but they both rewrite the same animals, so
   run one or the other. Delete the 1.x `goodhunting_*.zip` (keeping it somewhere safe as your
   rollback).
3. Start the game. The mod writes `ModConfig/goodhunting2.json` on first load.

---

## Configuration

`ModConfig/goodhunting2.json` (created with defaults on first run):

```json
{
  "enabled": true,
  "hideMultiplier": 1.0,
  "fatMultiplier": 1.0,
  "meatMultiplier": 1.0,
  "featherMultiplier": 1.0,
  "allMultiplier": 1.0
}
```

| Key | Category | Matches item codes |
|---|---|---|
| `hideMultiplier` | hides | `hide-raw-*` |
| `fatMultiplier` | fat | `fat` |
| `meatMultiplier` | meat | `redmeat-raw`, `bushmeat-raw`, `fish-raw`, `poultry-raw` |
| `featherMultiplier` | feathers | `feather` |
| `allMultiplier` | everything | any drop, applied **on top of** the category multiplier |

- `1.0` is the identity (vanilla Good Hunting tuning). `2.0` doubles both the average and the
  variation of a drop. `0.5` halves them. `0.0` removes the drop.
- Each field is clamped to `[0, 100]`, and so is the combined multiplier after the category and
  `all` values are multiplied together.
- `allMultiplier` is applied on top of the category multiplier, so `meatMultiplier: 2` with
  `allMultiplier: 3` gives meat drops `6×`.
- `enabled: false` leaves the game untouched.

Restart the game after editing.

> **Hides:** the yield is an average, so a `hideMultiplier` above `1.0` can occasionally
> produce more than one hide from a single animal. If that bothers you, leave hides at `1.0`
> and lift `fat`/`meat` instead.

---

## How it works

- `GoodHuntingModSystem` loads the config at `StartPre` and (unless disabled) injects the data
  at `AssetsFinalize`, on both client and server.
- Entity drops and health are written straight into the engine's
  `EntityServerProperties.BehaviorsAsJsonObj` — the very structure the 1.x content patches
  produced — but addressed by behaviour `code` (`"harvestable"`, `"health"`) rather than by
  array index.
- Fish yields are set on the `CollectibleBehaviorGroundStoredProcessable.ProcessedStacks`
  field of each `fishraw` variant.
- The `src/Data/GoodHuntingData.g.cs` tables are generated from two real-engine dumps of the
  legacy mod (`tools/fixtures/`), so parity at 1× is structural, not hand-maintained.

---

## Building from source

Requires the .NET 10 SDK and a 1.22.7 install for compile-time references.

```bash
# The SDK used here lives at ~/.dotnet (not necessarily on PATH)
export DOTNET_ROOT=$HOME/.dotnet PATH=$HOME/.dotnet:$PATH

dotnet build -c Release                 # -> bin/Release/GoodHunting.dll
dotnet run --project tests -c Release -- .   # unit tests + golden parity
tools/package.sh                        # -> dist/goodhunting2_2.0.0.zip
```

Override the game reference directory if yours differs:

```bash
dotnet build -c Release -p:VintageStoryDir=/path/to/vintagestory
```

### Regenerating the data tables

`src/Data/GoodHuntingData.g.cs` and `tools/golden_drops.json` are generated and committed.

```bash
python3 tools/build_data.py            # fixtures -> golden -> C#
```

To rebuild the fixtures themselves you need the `spike/` probe mod and a local dedicated
server; the probe dumps the engine's resolved drops/health with and without the legacy mod.

---

## Verification

- **Parity:** the new code mod was loaded into a real 1.22.7 dedicated server alongside a
  diagnostic probe, and its resolved output compared against the legacy mod's: **0 differences
  across 479 entity types and 81 fish variants**, and the packaged zip reproduces this.
- **Multipliers:** with `meatMultiplier: 2`, all 828 owned entity drop entries and 133 fish
  entries doubled exactly; health and unowned types were untouched.
- **Tests:** `tests/` runs 4,911 assertions (classifier, clamping, multiplier ordering,
  no-mutation, disabled mode, injected-JSON shape, and full golden parity).

---

## Rollback

1. Delete `goodhunting2_2.0.0.zip` from `Mods/`.
2. Restore the archived 1.x `goodhunting_*.zip` and restart.
3. Optionally delete `ModConfig/goodhunting2.json` (it is ignored when 2.0 is absent).

Server operators: the ids differ, so the game will not complain if both are present -- but
running both means two mods writing the same animal data. Keep only one.

---

## Credits

Original tuning by **TaimatCompany** and **GreenDragonette**. 2.0 code port and config by
**sineapse**.

## License

This is a rework of the original Good Hunting mod by TaimatCompany and GreenDragonette; see
that mod's page for the licensing terms it was released under.

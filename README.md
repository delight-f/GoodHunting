# Good Hunting Forked

Realistic animal health and harvest yields for [Vintage Story](https://www.vintagestory.at/),
with configurable drop multipliers.

Good Hunting Forked is a fork of the original Good Hunting (released as Good Hunting 2 up to
2.0.0). It keeps the same tuning --
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
- **Server-side only.** Clients need neither the mod nor a matching config; the handbook shows
  them the server's numbers.

---

## Installation

1. Drop `goodhuntingforked_3.0.0.zip` into your `Mods` folder.
   - Multiplayer: the server's `<server data>/Mods/` only. Players do not install it.
   - Single player: `~/.var/app/at.vintagestory.VintageStory/config/VintagestoryData/Mods/`
     (Flatpak) or `%appdata%/VintagestoryData/Mods/` (Windows).
2. **Remove the original Good Hunting or Good Hunting 2 if you have it.** This mod uses its own
   id (`goodhuntingforked`), so they do not collide — but they all rewrite the same animals, so
   run only one. Keep the old zip somewhere safe as your rollback. Coming from Good Hunting 2,
   rename `ModConfig/goodhunting2.json` to `goodhuntingforked.json` to keep your settings.
3. Start the game or server. The mod writes `ModConfig/goodhuntingforked.json` on first load.

---

## Configuration

`ModConfig/goodhuntingforked.json` on the server (created with defaults on first run):

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
- Multipliers apply to the 181 entity types and 68 fish variants this mod tunes. Tamed and
  semi-tamed elk, 13 small fish (shad, chub, reef fish) and other mods' animals keep their own
  drops.
- `enabled: false` leaves the game untouched.
- An unreadable file is logged as an error and left as-is; the mod runs at defaults until it is
  fixed.

Restart the game or server after editing.

> **Hides:** the yield is an average, so a `hideMultiplier` above `1.0` can occasionally
> produce more than one hide from a single animal. If that bothers you, leave hides at `1.0`
> and lift `fat`/`meat` instead.

---

## How it works

- `GoodHuntingModSystem` runs on the server only. It loads the config at `StartPre` and
  (unless disabled) injects the data at `AssetsFinalize`.
- Entity drops and health are written into the engine's
  `EntityServerProperties.BehaviorsAsJsonObj` — the structure the 1.x content patches produced —
  addressed by behaviour `code` (`"harvestable"`, `"health"`) rather than by array index. Only
  the `drops`, `maxhealth` and `currenthealth` keys are touched; sibling keys survive.
- The `harvestableDrops` entity attribute (the handbook's copy, synced to clients) is refreshed
  with the same drops, and the client's health behaviour shares the server's token.
- Fish yields replace `CollectibleBehaviorGroundStoredProcessable.ProcessedStacks` on each
  `fishraw` variant, and the behaviour's `propertiesAtString` (what clients rebuild it from)
  is rewritten to match.
- Only `game:` domain codes are matched. Any table entry that finds no entity, item or
  behaviour is listed in a warning, so a game update that renames something does not fail
  silently.
- The chicken render-size tweak is a server-side asset patch; clients receive the size in the
  entity type packet.
- The `src/Data/GoodHuntingData.g.cs` tables are generated from two real-engine dumps of the
  legacy mod (`tools/fixtures/`), so parity at 1× is structural, not hand-maintained.

---

## Building from source

Requires the .NET 10 SDK. The mod needs a 1.22.7 install for compile-time references; the
tests do not.

```bash
# The SDK used here lives at ~/.dotnet (not necessarily on PATH)
export DOTNET_ROOT=$HOME/.dotnet PATH=$HOME/.dotnet:$PATH

dotnet build -c Release                 # -> bin/Release/GoodHunting.dll
dotnet run --project tests -c Release -- .   # unit tests + golden parity
tools/package.sh                        # tests, build -> dist/goodhuntingforked_3.0.0.zip
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
server; the probe dumps the engine's resolved drops/health with and without the legacy mod,
plus what clients receive (the `harvestableDrops` attribute, the client health behaviour,
render size, and the fish behaviour properties string).

---

## Verification

- **Parity:** the packaged zip was loaded into a local 1.22.7 dedicated server alongside the
  probe and its resolved output compared against the legacy mod's: **0 differences across 479
  entity types and 81 fish variants**.
- **Client view:** in the same run the handbook drop attribute, the client health behaviour and
  the fish properties sent to clients matched the server's values for every entity and fish,
  and roosters render at 1.2× (vanilla 1.0).
- **Multipliers:** with `meatMultiplier: 2, hideMultiplier: 0.5`, every owned meat entry doubled
  and every hide entry halved, on the server and in what clients receive; health and unowned
  types were untouched.
- **Tests:** `tests/` runs 4,909 assertions (classifier, clamping, multiplier ordering,
  no-mutation, disabled mode, injected-JSON shape, and table/golden consistency).

---

## Rollback

1. Delete `goodhuntingforked_3.0.0.zip` from `Mods/`.
2. Restore the archived `goodhunting_*.zip` or `goodhunting2_*.zip` and restart.
3. Optionally delete `ModConfig/goodhuntingforked.json` (it is ignored when this mod is absent).

Server operators: the ids differ, so the game will not complain if both are present -- but
running both means two mods writing the same animal data. Keep only one.

---

## Credits

Original tuning by **TaimatCompany** and **GreenDragonette**. 2.0 code port and config by
**sineapse**; Good Hunting Forked continues from there.

## License

This is a rework of the original Good Hunting mod by TaimatCompany and GreenDragonette; see
that mod's page for the licensing terms it was released under.

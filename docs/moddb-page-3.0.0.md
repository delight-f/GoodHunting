# Good Hunting Forked

## What this mod is

A hunt should pay off like a hunt. In vanilla, a rabbit and a brown bear are cut from the same
cloth -- a couple of seconds with a knife and you walk away with a predictable pile of meat and
a hide. It works, but the size of the animal never seems to matter.

Good Hunting rebalances what every animal gives you, species by species and sex by sex, so a
big boar is worth more than a fox and a doe is not a carbon copy of a stag. It adds no new
tools, machines or recipes and changes nothing about how you hunt, kill or harvest. It changes
the numbers behind the harvest, then hands you the keys to those numbers with a config file you
can edit in Notepad.

This is a fork of the original Good Hunting. It keeps that mod's per-species balance exactly as
it was, and adds the settings file on top.

## What it changes

- **Animal health** retuned per species and sex. Vanilla gives males and females identical
  health; this gives males more than females and adjusts each species individually -- pigs,
  hyenas and moose get tougher, while bears and hares get a little softer.
- **Harvest drops** -- hides, fat, red meat, bushmeat, poultry and feathers -- rebalanced per
  species and sex, with the amount scaled to how big the animal actually is.
- **Fish processing** yields tuned per fish, so a 60kg arapaima gives a real meal and a minnow
  gives a snack.
- **A simple settings file** so you can raise or lower hides, fat, meat and feathers -- or all
  of them at once -- without touching code.

## What it does not change

- No new items, blocks, tools, machines or recipes.
- No change to hunting, killing, spawning, breeding or how you butcher a kill. You still use a
  knife the same way you always have.
- **By default the drops are exactly the classic Good Hunting tuning.** Every multiplier starts
  at `1.0`, which means "leave it as it is". Only the numbers change when you ask them to.

## Installing

1. Put `goodhuntingforked_3.0.0.zip` in your `Mods` folder.
   - Windows: `%appdata%\VintagestoryData\Mods`
   - Linux (Flatpak): `~/.var/app/at.vintagestory.VintageStory/config/VintagestoryData/Mods`
   - Server: the `Mods` folder inside your server's data directory.
2. **Upgrading from the original Good Hunting or Good Hunting 2?** Remove the old zip first.
   This version has its own mod id (`goodhuntingforked`), so it will load alongside them, but
   they all rewrite the same animals -- run only one. Keep the old zip somewhere safe as your
   rollback.
   Coming from Good Hunting 2, rename `ModConfig/goodhunting2.json` to
   `ModConfig/goodhuntingforked.json` to keep your settings.
3. Start the game or server. That is all.

**This is a server-side mod.** In multiplayer only the server needs it: players join without
installing anything, and the handbook shows them the server's numbers. In single player,
install it like any other mod.

## Using it

There is nothing new to learn. Hunt, kill and harvest the way you always have, and the new
yields apply automatically. If it is installed correctly you will notice that different animals
give different amounts, and that a bear is finally worth the trouble.

## The settings file (no coding needed)

The first time you run the game with Good Hunting installed, it creates a small settings file
for you. It is plain text with six settings in it. You do not need to know anything about
programming to change it -- if you can edit a text file, you can balance this mod.

**Where the file is:**

```
<VintagestoryData>/ModConfig/goodhuntingforked.json
```

On Windows:

```
C:\Users\<YOUR NAME>\AppData\Roaming\VintagestoryData\ModConfig\goodhuntingforked.json
```

On Linux (Flatpak):

```
~/.var/app/at.vintagestory.VintageStory/config/VintagestoryData/ModConfig/goodhuntingforked.json
```

On a dedicated server it is `ModConfig/goodhuntingforked.json` inside the server's data
directory.

**Can't find it?** In the Vintage Story main menu, open Mods and use the *Open Mod Folder*
button. Go up one level, then into `ModConfig`.

**What the file looks like:**

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

**What each setting means:**

| Setting | Affects | Notes |
|---|---|---|
| `enabled` | Everything | `true` turns the mod on, `false` turns it off completely |
| `hideMultiplier` | Hides | animal skins and pelts |
| `fatMultiplier` | Fat | rendered fat and fat drops |
| `meatMultiplier` | Meat | red meat, bushmeat, poultry and raw fish |
| `featherMultiplier` | Feathers | bird feathers |
| `allMultiplier` | Everything | applied on top of whichever setting above also matches |

**How the numbers work.** A multiplier is just a "times" number:

- `1.0` -- leave this alone (no change). This is the default.
- `2.0` -- twice as much.
- `0.5` -- half as much.
- `0.0` -- none at all.

So if you want more meat from every animal, change `meatMultiplier` from `1.0` to `2.0`. If you
want a generous world, set `allMultiplier` to `1.5` and leave the rest alone.

Multipliers stack. If `meatMultiplier` is `2.0` and `allMultiplier` is `3.0`, meat ends up six
times normal (`2 x 3`). Values are capped at `100`, so you cannot break anything by typing a
silly number.

Multipliers apply to the animals and fish this mod tunes -- 181 animal types and 68 fish
variants. Tamed and semi-tamed elk, 13 small fish (shad, chub and reef fish) and other mods'
animals keep their own drops.

**Three-step example:**

1. Close Vintage Story.
2. Open `goodhuntingforked.json` in Notepad, change `"meatMultiplier": 1.0` to
   `"meatMultiplier": 2.0`, and save.
3. Start the game. Animals now give twice the meat.

**A few things to know:**

- **Restart after editing.** The file is read when the world loads, so a change will not apply
  until you reload.
- **Edit the server's file, not a player's.** In multiplayer the server's copy is the only one
  used; players do not need this mod or a matching file.
- **Do not worry about formatting.** Keep the quotes and the comma at the end of each line and
  you will be fine. If the file cannot be read, the game logs a clear error, leaves your file
  untouched and runs at the defaults until you fix it.
- **Hides are an average.** Setting `hideMultiplier` above `1.0` can occasionally give more than
  one hide from a single animal, and below `1.0` a hide becomes a chance rather than a
  certainty. If that bothers you, leave hides at `1.0` and raise `fatMultiplier` and
  `meatMultiplier` instead.

## Q & A

**Does this give more than vanilla?**
By default it changes the balance rather than simply inflating it -- some animals give a little
less, big ones give much more. Raise the multipliers if you want a more generous world.

**Will it work with other animal mods?**
Yes. This mod only touches the vanilla animals it knows about; animals added by other mods are
left exactly as they are. If another mod changes the drops or health of those same vanilla
animals, only one set of numbers can win -- run one or the other.

**Do I need to start a new world?**
No. Drops and health are read when the game loads, so your existing world keeps working and you
can add or remove the mod at any time.

**Can I turn it off without uninstalling it?**
Yes. Set `"enabled": false` in the settings file, or remove the zip.

**I changed the settings and nothing happened.**
Make sure you saved the file and fully restarted the game or server, and that in multiplayer you
edited the server's file rather than a player's.

**Is this compatible with the original Good Hunting or Good Hunting 2?**
They have different mod ids, so they will load alongside each other -- but all three rewrite the
same animals, so run only one. Keep the old zip as your rollback.

## Details

| | |
|---|---|
| Version | 3.0.0 |
| Game version | Vintage Story 1.22.7 |
| Side | Server (players do not need it) |
| Mod id | `goodhuntingforked` |
| New world required | No |

## Credits

Original balance tuning and the 1.x releases by **TaimatCompany** and **GreenDragonette**.
Version 2.0 reworked the mod into a code mod with a settings file, by **sineapse**, keeping the
original numbers identical at the default settings. Good Hunting Forked continues that work.

Thanks for downloading -- and good hunting.

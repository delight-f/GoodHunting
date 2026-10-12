# Good Hunting 2

## Vision for the mod

Good Hunting 2 is the successor to the original Good Hunting -- the same rebalance, now with a config file. It is about making a hunt pay off like a hunt should. In vanilla, a rabbit and a
brown bear are cut from the same cloth -- a couple of seconds with a knife and you walk away
with a predictable pile of meat and a hide. It works, but it never feels like the size of the
animal matters.

Good Hunting rebalances what every animal gives you, species by species and sex by sex, so a
big boar is worth more than a fox, and a doe is not a carbon copy of a stag. It does not add
new tools, machines or recipes, and it does not change how you hunt, kill or harvest anything.
It changes the numbers behind the harvest, and then hands you the keys to those numbers with a
config file you can edit in Notepad.

If you have ever wanted "more meat from the animals I actually worked for", this is the mod.

## What Good Hunting changes

- **Animal health** retuned per species and sex. Vanilla gives males and females identical
  health; Good Hunting gives males more than females and adjusts each species individually --
  pigs, hyenas and moose get tougher, while bears and hares get a little softer.
- **Harvest drops** -- hides, fat, red meat, bushmeat, poultry and feathers -- rebalanced per
  species and sex, with the amount scaled to how big the animal actually is.
- **Fish processing** yields tuned per fish, so a 60kg arapaima gives a real meal and a
  minnow gives a snack.
- **A simple config file** so you can raise or lower hides, fat, meat and feathers -- or all
  of them at once -- without ever touching code.

## What it does NOT change

- No new items, blocks, tools, machines or recipes.
- No change to hunting, killing, spawning or breeding.
- No change to how you harvest. You still use a knife the same way you always have.
- **By default, the drops are exactly the classic Good Hunting tuning.** Every multiplier
  starts at 1.0, which means "leave it as it is". Install it, and the balance is the one the
  mod has always had. Only the numbers change when you ask them to.

## Installing

1. Put `goodhunting2_2.0.0.zip` in your `Mods` folder.
   - Windows: `%appdata%\VintagestoryData\Mods`
   - Linux (Flatpak): `~/.var/app/at.vintagestory.VintageStory/config/VintagestoryData/Mods`
   - Server: the `Mods` folder inside your server's data directory.
2. **Upgrading from the original Good Hunting?** Remove the old zip. This mod uses its own id
   (`goodhunting2`), so the two no longer clash -- but they both edit the same animals, so
   run one or the other. (Keep the old zip safe in case you want to roll back.)
3. Start the game. That's it -- you are done.

Good Hunting is a **both-sides** mod. In multiplayer, the server and every player who joins
must have it installed, and they must be on the same version.

## Using it

There is nothing new to learn. Hunt, kill and harvest the way you always have, and the new
yields apply automatically. If it is installed correctly you will simply notice that different
animals give different amounts, and that a bear is finally worth the trouble.

## Making it yours -- the config (no coding needed)

The first time you run the game with Good Hunting installed, it creates a small settings file
for you. It is a plain text file with six lines in it. You do not need to know anything about
programming to change it -- if you can edit a text file, you can balance this mod.

**Where the file is:**

```
<VintagestoryData>/ModConfig/goodhunting2.json
```

For example, on Windows:

```
C:\Users\<YOUR NAME>\AppData\Roaming\VintagestoryData\ModConfig\goodhunting2.json
```

On Linux (Flatpak):

```
~/.var/app/at.vintagestory.VintageStory/config/VintagestoryData/ModConfig/goodhunting2.json
```

**Can't find it?** In the Vintage Story main menu, go to Mods, and use the *Open Mod Folder*
button. From there, go up one level and into `ModConfig`.

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

**What each line means:**

| Line | Affects | Notes |
|---|---|---|
| `enabled` | Everything | `true` turns the mod on, `false` turns it off completely |
| `hideMultiplier` | Hides | e.g. animal skins and pelts |
| `fatMultiplier` | Fat | rendered fat and fat drops |
| `meatMultiplier` | Meat | red meat, bushmeat, poultry and raw fish |
| `featherMultiplier` | Feathers | bird feathers |
| `allMultiplier` | Everything | applied on top of whichever category above also matches |

**How the numbers work.** A multiplier is just a "times" number:

- `1.0` -- leave this alone (no change). This is the default.
- `2.0` -- twice as much.
- `0.5` -- half as much.
- `0.0` -- none at all.

So if you want more meat from every animal, change `meatMultiplier` from `1.0` to `2.0`. If
you want a generous world, set `allMultiplier` to `1.5` and leave the rest alone.

Multipliers stack. If `meatMultiplier` is `2.0` and `allMultiplier` is `3.0`, meat ends up
six times normal (`2 x 3`). Numbers are capped at `100`, so you cannot break anything by
typing a silly value.

**Three-step example:**

1. Close Vintage Story.
2. Open `goodhunting2.json` in Notepad, change `"meatMultiplier": 1.0` to `"meatMultiplier": 2.0`,
   and save.
3. Start the game. Animals now give twice the meat.

**A few things to know:**

- **Restart the game after editing.** The file is read when the world loads, so a change will
  not apply until you reload.
- **In multiplayer, edit the file on both the server and your client.** They should match.
- **Do not worry about formatting.** Keep the quotes and the comma at the end of each line and
  you will be fine. If you break the file, delete it -- the game will simply write a fresh one.
- **Hides are an average.** Setting `hideMultiplier` above `1.0` can occasionally give you more
  than one hide from a single animal. If that bothers you, leave hides at `1.0` and boost
  `fatMultiplier` and `meatMultiplier` instead.

## Q & A

**Does this give more than vanilla?**
By default it changes the balance rather than simply inflating it -- some animals give a little
less, big ones give much more. Crank the multipliers up if you want a more generous world.

**Will it work with other animal mods?**
Good Hunting only touches the animals it knows about. Animal mods it has not been told about
are left exactly as they are.

**Do I need to start a new world?**
No. Drops and health are read when the game loads, so your existing world keeps working. You
can add or remove it at any time.

**Can I turn it off without uninstalling it?**
Yes. Set `"enabled": false` in the config, or just remove the zip.

**I changed the config and nothing happened.**
Make sure you saved the file and fully restarted the game, and that you edited the file for the
side you are actually playing on (both sides in multiplayer).

**Is this compatible with the original Good Hunting?**
It has its own mod id, so it will load alongside it -- but both edit the same animals, so
run one or the other, not both.

## Details

| | |
|---|---|
| Game version | Vintage Story 1.22.7 |
| Side | Both (client and server) |
| Mod id | `goodhunting2` |
| New world required | No |

## Credits

Original balance tuning and the 1.x releases by **TaimatCompany** and **GreenDragonette**.
Version 2.0 reworks the mod into a code mod with a config file, by **sineapse**, while keeping
the original numbers identical at the default settings.

Thanks for downloading -- and good hunting.

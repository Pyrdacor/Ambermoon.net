# Modding

Ambermoon.net supports mods. A mod is applied on top of a base game the player already has (the original Ambermoon or Ambermoon Advanced in any language). Mods never contain the original game data.

There are two kinds of mods. Both can be combined in a single mod.

- **Asset mods** change or add data: graphics, music, maps, monsters, items and so on. They don't need any programming.
- **Code mods** contain a .NET assembly which hooks into the game with C# code.

Mods with code are only started after the player has confirmed once that they trust the mod. If the mod assembly changes (for example after an update), the player is asked again.

- [Getting started](#getting-started)
  - [What a mod can do](#what-a-mod-can-do)
  - [Prerequisites](#prerequisites)
  - [Finding indices](#finding-indices)
  - [Your first mod](#your-first-mod)
  - [Packing and sharing](#packing-and-sharing)
  - [Testing and debugging](#testing-and-debugging)
  - [Troubleshooting](#troubleshooting)
- [Installing mods](#installing-mods)
- [Creating a mod](#creating-a-mod)
  - [mod.json](#modjson)
  - [Option defaults](#option-defaults)
- [Asset mods](#asset-mods)
  - [New maps](#new-maps)
- [Code mods](#code-mods)
  - [Hooks](#hooks)
  - [ModApi](#modapi)
- [Quests](#quests)
- [Ambermoon DC](#ambermoon-dc)
  - [Dungeon themes](#dungeon-themes)
  - [Building your own variant](#building-your-own-variant)
  - [Releasing Ambermoon DC](#releasing-ambermoon-dc)
- [Tests](#tests)
- [Known limitations](#known-limitations)


## Getting started

This part takes you from an empty folder to a `.mod` file you can share. You don't need to know C# unless you want to write code mods. The steps link to the reference sections below, which describe every file format in detail.

### What a mod can do

| You want to... | How | C# needed? |
| --- | --- | --- |
| Rename or rebalance monsters, items, NPCs or starting party members | JSON [patch files](#asset-mods) in `assets/` | No |
| Make new monsters or items based on existing ones | JSON file with a `"Template"` | No |
| Build new monster groups | `assets/monstergroups/{index}.json` | No |
| Change map texts, music or flags of an existing map | JSON patch in `assets/maps/` | No |
| Build a completely new (3D) map | [Map definition](#new-maps) with a character grid in `assets/maps/` | No |
| Replace portraits, item icons, UI graphics, ... | PNG files in `assets/graphics/` | No |
| Replace songs | MP3 files in `assets/music/` | No |
| Add your own quest log entries | [`quests.json`](#quests) | No |
| Change default game options (e.g. auto battle) | [`Options`](#option-defaults) in `mod.json` | No |
| Change the start position, react to events, generate content, ... | A .NET assembly ([code mod](#code-mods)) | Yes |

Some things can't be changed by mods at all, for example UI texts, merchants, tilesets, 3D textures and monster battle graphics. Only one mod can be active at a time, and mods don't work on Android. Read the [known limitations](#known-limitations) before you plan a big project.

### Prerequisites

- **Ambermoon.net for desktop** (Windows, Linux or macOS). It must be a version with mod support.
- **A matching base game.** Mods are offered for the game versions built into Ambermoon.net. Each mod says in `mod.json` whether it needs the original Ambermoon, Ambermoon Advanced or works with any of them.
- **A text editor** that handles UTF-8 JSON (e.g. VS Code or Notepad++). A JSON validator plugin helps a lot.
- **An image editor** that can save 8-bit PNG files, if you want to replace graphics.
- **Optional: the .NET 8 SDK and the source code** of Ambermoon.net from GitHub. You need them for the [mod packer](#creating-a-mod), the tests and code mods. You can also make and pack asset mods without them.

### Finding indices

You need the **indices** of the things you want to change: map numbers, item numbers, monster numbers and so on. The cheat console of the game helps here. Set `EnableCheats` to `true` in `ambermoon.cfg` (see [Configuration.md](Configuration.md)), start the game from a terminal and type the commands into that terminal:

| Cheat | Use for modding |
| --- | --- |
| `maps [name]` | List map indices |
| `items [name]` | List item indices |
| `monsters [name]` | List monster *group* indices |
| `members [name]` | List party member indices |
| `where` | Show your current map and position |
| `teleport <map> [x] [y]` | Jump to a map, e.g. your new map |
| `fight <group>` | Start a battle against a monster group |
| `give <item> [amount]` | Get an item |

There is no cheat which lists single monsters (only monster groups).

### Your first mod

In this example you make a small asset-only mod which renames a monster, adds a copy of it, puts both into a new monster group and changes an item. It uses the same values as the automated tests in `Ambermoon.Mod.Tests/AssetModTests.cs`, so these files are known to load.

#### 1. Create the mod folder

With the [mod packer](#creating-a-mod) (from the source code folder):

```
dotnet run --project Ambermoon.ModPacker -- new MyFirstMod my-first-mod "My First Mod"
```

This creates the folder `MyFirstMod` with a `mod.json` and these empty folders:

```
MyFirstMod/
  mod.json
  assets/
    graphics/
    items/
    maps/
    monstergroups/
    monsters/
    music/
    npcs/
    partymembers/
```

Without the packer you create the same folders yourself. You only need the folders you actually use.

#### 2. Edit mod.json

```json
{
  "Id": "my-first-mod",
  "Name": "My First Mod",
  "Description": "A bigger spider and a cheap potion",
  "Version": "1.0",
  "Authors": [ "Your name" ],
  "ReleaseDate": "2026-11-03",
  "ApiVersion": 1,
  "Base": {
    "Game": "Original"
  }
}
```

`Assembly` is left out, because this mod has no code. The game checks the values of `Id`, `Name`, `Version` and `Base.Game`. The rules for all keys are listed in [mod.json](#modjson).

#### 3. Add asset files

The file name is always the index of the thing you change (see [Asset mods](#asset-mods) for the folders and which indices are 1-based).

`assets/monsters/2.json` changes the existing monster 2:

```json
{ "Name": "HUGE SPIDER", "DefeatExperience": 99 }
```

`assets/monsters/90.json` creates a **new** monster 90, a copy of monster 2 with another name. Indices which don't exist yet need a `Template`:

```json
{ "Template": 2, "Name": "COPY SPIDER" }
```

`assets/monstergroups/150.json` creates a group with the old and the new spider: 3 rows of 6 slots, 0 means empty.

```json
{ "Monsters": [ [0,0,0,0,0,0], [0,0,0,0,0,0], [0,0,2,90,0,0] ] }
```

`assets/items/211.json` changes item 211:

```json
{ "Name": "SUPER POTION", "Price": 1 }
```

Before you write your own patches, read how [patches](#asset-mods) work. Especially lists are replaced and not merged, and a misspelled property name stops the mod from loading.

#### 4. Install and try it out

1. Copy the whole `MyFirstMod` folder into the `Mods` folder next to the Ambermoon.net executable (see [Installing mods](#installing-mods)).
2. Start Ambermoon.net. Your mod appears in the version selector with its name, once for every language of a matching base game. Use the flags to pick the language.
3. Start a new game. Mods use their own savegames, so your normal savegames are safe.
4. With the [cheat console](#finding-indices) you can try out the change right away: `fight 150` starts a battle against your new group.

You can keep the folder in `Mods` while you work on it. After every change, restart the game.

From here you can go on with a [new map](#new-maps), replaced graphics and music (see [Asset mods](#asset-mods)), [quests](#quests) or a [code mod](#code-mods). The [Ambermoon DC](#ambermoon-dc) mod in the source code is a complete example of a large mod.

### Packing and sharing

```
dotnet run --project Ambermoon.ModPacker -- pack MyFirstMod
```

This validates the mod and creates `my-first-mod.mod` next to the folder. The other commands of the packer and what it checks are described in [Creating a mod](#creating-a-mod).

**Without the packer:** zip the *contents* of the folder (not the folder itself) and rename the file to `.mod`. If `mod.json` ends up inside a subfolder of the archive, the game won't find the mod.

Players install the `.mod` file like any other mod (see [Installing mods](#installing-mods)). If you release a new version, increase `Version` and keep the `Id`, so players can keep their savegames.

### Testing and debugging

- **Start your mod directly:** `Ambermoon.net --mod my-first-mod --skip-intro`. This skips the version selector and the trust question and uses the configured language if the mod supports it.
- **Watch the console.** Start Ambermoon.net from a terminal. Messages like `Unable to load mod '...'`, `Mod '...' was skipped as no matching base game version is available` and asset warnings (`Mod 'my-first-mod': Ignored ...`) are printed there. If applying the mod fails, the game shows the error `Unable to load mod '<id>': ...` when you start the game.
- **Cheats on the command line:** `--cheat <seconds> "<command>"` runs a cheat after the given time, e.g. `--load 0 --cheat 3 "teleport 700 3 3"`. `--load <slot>` and `--continue` skip the main menu.
- **Scenario scripts:** `--script <file>` runs a text file with commands like `goto`, `trigger`, `waitbattle`, `screenshot`, `save <slot>` and `load <slot>` once the game runs. You can use it to test a map change or the savegames of a mod automatically again and again. All commands are listed at the top of `Ambermoon.net/ScenarioScript.cs`.
- **Code mods:** `Api.Log(...)` writes to the console. Put the `.pdb` file next to the mod assembly to get file names and line numbers in error messages (see [ModApi](#modapi)).
- **Reset your test state:** the savegames of your mod are in `Saves/mod_<id>_<language>`. The trust decisions are stored in `Saves/mods/trusted_mods.json` and the option defaults in `Saves/mods/options/<id>.json`.
- **Automated tests:** with the source code you can test mods headless (see [Tests](#tests)).

### Troubleshooting

| Problem | Likely cause |
| --- | --- |
| The mod doesn't appear in the version selector | `mod.json` is missing or invalid (check the console), the folder isn't directly inside `Mods`, or no installed base game matches `Base.Game`/`Base.MinVersion`. Mods aren't available on Android. |
| The `.mod` file doesn't appear, the folder does | The archive has `mod.json` in a subfolder, or a folder with the same `Id` exists (the folder wins). |
| "The mod needs mod API version ..." | Your `ApiVersion` is newer than the game supports. Update Ambermoon.net or use `1`. |
| "Unable to load mod ...: Could not find member 'X'" (or similar) | A property in a patch is misspelled or doesn't exist on that data class. |
| "Monster N does not exist. Specify an existing 'Template' ..." | You used a new index without `Template`. The same applies to items. |
| "... the file name must be a 1-based index" | Asset files must be named like `12.json`. `0.json` is invalid except for graphics. |
| "The image must have the size WxH" | Replaced graphics must have exactly the original size. |
| "Only PNG files with 8 bits per channel are supported" / "Interlaced PNG files are not supported" | Save the PNG as 8-bit (RGBA or 256-color palette), not interlaced. |
| "The grid of map N must have H rows with W characters each" | A row in `Grid` is too short or too long, or `Width`/`Height` is wrong. |
| "Map N uses grid character 'x' which is not part of the legend" | Add the character to `Legend` (a space counts too). |
| "Map N references unknown labdata ..." | `Labdata` of a new 3D map doesn't exist in the base game. |
| Texts of a patched map are missing | Lists in patches replace the whole list. Write all texts. |
| Events jump to the wrong place | `Next` counts from 1, branch targets count from 0. |
| "The mod assembly must contain exactly one public class deriving from ModBase" | Make other classes non-public or abstract. |
| The trust question appears again | The mod assembly changed (expected after every rebuild). |
| Old progress or old option defaults get in the way | Delete the folder of the mod in `Saves` or `Saves/mods/options/<id>.json`. |


## Installing mods

Put the mod folder or the packed `.mod` file into the `Mods` folder next to the Ambermoon.net executable. Create the folder if it doesn't exist. The game only uses folders directly inside `Mods` which contain a `mod.json`. You can change this path with the configuration option `ModPath` in `ambermoon.cfg` (relative paths start at the folder of the executable).

Each mod is shown in the version selector. It is offered in every language of the base game it needs, so you can choose the language with the flags as usual. The version list can be scrolled if there are more than 5 entries.

Each mod uses its own savegames, which are stored in the folder `Saves/mod_<id>_<language>`.

You can also start a mod directly with the command line option `--mod <id>`. The game then starts in the configured language. The version selector and the trust question are skipped, because you chose the mod explicitly.


## Creating a mod

The mod packer tool (project `Ambermoon.ModPacker`) helps with the setup. Release downloads of the game don't include it. Run it from the source code with `dotnet run --project Ambermoon.ModPacker -- <arguments>`, or build it once (`dotnet build Ambermoon.ModPacker -c Release`) and call `AmbermoonModPacker` from its output folder:

```
AmbermoonModPacker new MyMod my-mod "My Mod"      # creates the folder MyMod with a mod.json and empty asset folders
AmbermoonModPacker validate MyMod                 # checks the mod
AmbermoonModPacker validate my-mod.mod            # checks a packed mod
AmbermoonModPacker pack MyMod                     # checks the mod and creates my-mod.mod next to the folder
AmbermoonModPacker pack MyMod out/MyMod-1.0.mod   # the same with another output file
```

`pack` refuses to pack a mod with errors. It leaves out hidden files and folders (names starting with `.`, e.g. `.git`).

The validator checks the manifest, that the assembly exists, `quests.json`, the JSON syntax, the file names (indices), complete map definitions and that the PNG files can be read. It doesn't know the base game, so it can't check whether an index exists, whether a property name in a patch is right or whether an image has the right size. These errors only show up when the game loads the mod.

While you develop a mod, you can put the folder directly into the `Mods` folder. A packed `.mod` file is a zip archive of the mod folder with `mod.json` at the top level of the archive. If a folder and a `.mod` file have the same id, the folder is used.


### mod.json

Every mod needs a `mod.json` at its root:

```json
{
  "Id": "my-mod",
  "Name": "My Mod",
  "Description": "What the mod is about",
  "Version": "1.0",
  "Authors": [ "Me" ],
  "ReleaseDate": "2026-10-13",
  "ApiVersion": 1,
  "Base": {
    "Game": "Original",
    "MinVersion": "1.19"
  },
  "Assembly": "MyMod.dll",
  "Options": {
    "AutoBattleRounds": 5,
    "ShowFog": true
  }
}
```

| Key | Description |
| --- | --- |
| Id | Unique id. Only letters, digits, `-`, `_` and `.` are allowed. It is used for the savegame folder, so don't change it after a release. |
| Name | Required. Shown in the version selector (the first 20 characters). |
| Version | Version as `Major` or `Major.Minor`, e.g. `1.0`. |
| ApiVersion | Version of the mod API the mod was made for (currently 1). |
| Base.Game | `Original`, `Advanced` or `Any`. Patches use the indices of the base game, which can differ between the original and Advanced. Only use `Any` if the changes work with both. |
| Base.MinVersion | Optional minimum version of the base game, e.g. `1.20`. |
| Assembly | Optional path of the mod assembly. Leave it out for asset-only mods. |
| Options | Optional default values for game options (see below). |

### Option defaults

A mod can provide default values for some game options with `Options`. They are only defaults, the player can still change everything:

- The first time the mod is used, its defaults are stored in a mod config file (`Saves/mods/options/<id>.json`).
- While the mod is active, an option is read from this file and only if it isn't there from the normal configuration.
- When the player changes an option (in the options menu or with F7 for the battle speed), the new value is written to the normal configuration and the option is removed from the mod config file. From then on the choice of the player is used, also in later sessions.
- Defaults which a newer version of the mod adds are added once. Defaults the player already changed are never added again.

Only these options can have mod defaults: `BattleSpeed` (0 to 100 in steps of 10), `AutoBattleRounds` (0 = off, 5, 10 or 15), `AutoDerune`, `ShowButtonTooltips`, `ShowPlayerStatsTooltips`, `ShowFloor`, `ShowCeiling`, `ShowFog`, `ShowSaveLoadMessage`, `Movement3D` (`WASD` or `WASDQE`) and `TurnWithArrowKeys`. Options like the resolution, window mode, music, volume, language or graphic filters stay under the control of the player. Other options or values of the wrong type make the manifest invalid.


## Asset mods

All asset files are placed inside the folder `assets` of the mod. The file name is always the index of the changed object. Maps, monsters, monster groups, items, party members and NPCs use 1-based indices like the game data. Graphics are 0-based.

| File | Content |
| --- | --- |
| `assets/maps/{index}.json` | A patch for an existing map, or a complete new map (see below). |
| `assets/monsters/{index}.json` | A patch for a monster. New monsters need a `"Template"` monster to copy. |
| `assets/monstergroups/{index}.json` | A monster group: `{ "Monsters": [[6 indices], [6 indices], [6 indices]] }`, 0 = empty. |
| `assets/items/{index}.json` | A patch for an item. New items need a `"Template"` item to copy. |
| `assets/partymembers/{index}.json` | A patch for a party member of the initial savegame. |
| `assets/npcs/{index}.json` | A patch for an NPC. |
| `assets/graphics/{GraphicType}/{index}.png` | Replaces a graphic. The size must be the same as the original. |
| `assets/music/{song}.mp3` | Replaces a song. The file name starts with the song number, e.g. `5.mp3` replaces song 5. See [CustomMusic.md](CustomMusic.md) for the song numbers. |

A patch only contains the properties you want to change, using the property names of the data classes in `Ambermoon.Data.Common` (`Monster`, `Item`, `Map`, ...). A property name which doesn't exist (or is misspelled) is an error and the mod isn't loaded, so a typo can't go unnoticed. Enums can be given by name, and flags as comma separated names.

Lists are replaced, not merged. The patch of map 258 below leaves the map with exactly one text, so events which show the texts 2, 3, ... have no text anymore. Always write the complete list.

```json
// assets/monsters/120.json (a new monster)
{ "Template": 2, "Name": "COPY SPIDER" }

// assets/maps/258.json
{ "Texts": [ "MY HOUSE" ], "MusicIndex": 3 }
```

More examples are part of [Your first mod](#3-add-asset-files).

The files are applied in this order: monsters, monster groups, items, party members, NPCs, maps, graphics. So a monster group can use a monster which is new in the same mod.

The graphic types are `Player`, `Portrait`, `Item`, `Layout`, `LabBackground`, `Cursor`, `Pics80x80`, `UIElements`, `EventPictures`, `TravelGfx`, `Transports`, `NPC`, `CombatBackground`, `CombatGraphics`, `BattleFieldIcons`, `AutomapGraphics` and `RiddlemouthGraphics`. For example `assets/graphics/Portrait/0.png` replaces the first portrait (portraits are 32x34 pixels). Only PNG files with 8 bits per channel which are not interlaced are supported (grayscale, RGB, RGBA, grayscale with alpha or 8-bit palette). 16-bit, 1/2/4-bit and interlaced PNG files are rejected. Colors are mapped to the nearest color of the primary UI palette. You can use another palette with an additional `{index}.json` like `{ "Palette": 6 }`. Pixels with an alpha value below 128 become transparent. Ambermoon.net has no tool which exports the original graphics as PNG, so you need another tool to get the originals as a starting point and to find out their exact sizes.


### New maps

A map file with a `Grid` is a complete map, a map file without it is a patch. A complete map is described as a grid of characters and a legend:

```json
{
  "Type": "Map3D",
  "Width": 5,
  "Height": 5,
  "Labdata": 2,
  "Palette": 6,
  "Music": 18,
  "Flags": "Dungeon, Automapper, CanRest, CanUseMagic",
  "Texts": [ "MY DUNGEON", "Hello adventurer!" ],
  "Legend": {
    "#": { "Wall": 1 },
    ".": {},
    "T": { "Event": 1, "Automap": "Exit" },
    "C": { "Object": 25, "Event": 2, "Automap": "Chest" }
  },
  "Grid": [
    "#####",
    "#.T.#",
    "#...#",
    "#C..#",
    "#####"
  ],
  "Events": [
    { "Type": "MapText", "TextIndex": 1, "PopupTrigger": "Move, EyeCursor", "EventImageIndex": 255, "Next": 3 },
    { "Type": "Chest", "ChestIndex": 0, "TextIndex": 255 },
    { "Type": "Teleport", "MapIndex": 258, "X": 2, "Y": 15, "Direction": "Down", "Transition": "MapChange" }
  ],
  "Characters": [
    { "Type": "Monster", "Index": 5, "Graphic": 13, "Flags": "RandomMovement", "CollisionClass": 1,
      "CombatBackground": 7, "Positions": [ { "X": 4, "Y": 4 } ] }
  ]
}
```

- `Grid` must have `Height` rows with exactly `Width` characters each. Every character of the grid (a space too) must be part of the `Legend`.
- 3D maps use `Wall` and `Object` (indices of the labdata given as `Labdata`) in the legend. 2D maps use `Back` and `Front` (tile indices of the tileset, given as `Tileset`). 2D maps are less tested (see [Known limitations](#known-limitations)).
- `Event` in the legend and in characters references an event by its 1-based position in `Events`. Each referenced event is an entry point. A map can have up to 64 of them.
- `Next` links events to a chain (1-based, 0 = end). Branch targets of conditions, doors, chests etc. are 0-based indices into `Events`, and 65535 means none. Mixing these up is the most common mistake.
- Every event needs a `Type` (see `EventType` in `Ambermoon.Data.Common/Event.cs`). The other properties are those of the matching event class.
- Positions of characters and of teleport targets are 1-based. A map can have up to 32 characters.
- New maps should use the indices 529 to 767.

To test a new map, jump to it with the cheat `teleport <map> <x> <y>` (see [Finding indices](#finding-indices)). To connect it to the world, use a code mod, for example to change the target of a teleport in `OnEvent` or to set the start map in `OnGameStarting` (see [Code mods](#code-mods)).


## Code mods

A code mod is a .NET 8 class library which references the project `Ambermoon.Mod`. There is no NuGet package for it, so you build against the source code. The library must contain exactly one public, non-abstract class which derives from `ModBase`. Set `Assembly` in `mod.json` to the path of the library. A code mod can also contain an `assets` folder. The assets are applied first, then `OnInitialize` runs.

The project `Ambermoon.Mod.AmbermoonDC` is a complete example and the easiest start for an own code mod. Copy it including the `CreateModFolder` build step of its `.csproj`, which creates a ready-to-use mod folder in `bin/<configuration>/net8.0/mod/AmbermoonDC/` after each build.

```csharp
public class MyMod : ModBase
{
    public override void OnInitialize()
    {
        // The base game data with the asset changes is loaded. The game does not exist yet.
        Api.GetMonster(2)!.Name = "BIG SPIDER";
    }

    public override void OnGameStarting(Savegame savegame, bool newGame)
    {
        if (newGame)
        {
            savegame.CurrentMapIndex = 300;
            savegame.CurrentMapX = 10;
            savegame.CurrentMapY = 10;
        }
    }

    public override void OnMapChanged(Map map) => Api.Log($"Entered {map.Name}");
}
```

### Hooks

Override the virtual methods of `ModBase` or subscribe to the events of the same name in `Api`.

| Hook | When |
| --- | --- |
| `OnInitialize()` | Once after the game data is loaded. New monsters must be added here, because the battle graphics are prepared when the game is created. |
| `OnGameStarting(savegame, newGame)` | Before a new or loaded game starts. The savegame can still be changed. |
| `OnGameStarted(newGame)` | After the first map is shown. |
| `OnUpdate(deltaTime)` | Every frame while ingame and not paused. |
| `OnMapChanged(map)` | When a map was entered. |
| `OnPlayerMoved(map, x, y, mapChange)` | After each step (1-based position). |
| `OnEvent(map, event, trigger, x, y)` | Before a map event is executed. Return `false` to skip it and the rest of its chain. The event can also be changed here, e.g. the target of a teleport. |
| `OnBattleStarting(monsterGroupIndex)` | Before a battle. The monster group can still be replaced. |
| `OnBattleEnded(monsterGroupIndex, monstersDefeated, partyDefeated)` | After a battle. |
| `OnLevelUp(partyMember)` | When a party member reaches a new level. |
| `OnPartyMemberJoined(partyMember)` / `OnPartyMemberLeft(partyMember)` | When characters join or leave the party. |
| `OnSaving(data)` / `OnLoading(data)` | Store and restore the state of the mod with each savegame. `OnLoading` is not called for new games. |

### ModApi

- **Data:** `GameData`, `Maps`, `Characters`, `Items`, `SetMap`, `GetMap`, `GetLabdata`, `SetMonsterGroup`, `GetMonsterGroup`, `GetMonster`, `CloneMonster` and `GetItem`.
- **State:** `IsIngame`, `Game`, `Savegame`, `CurrentMap`, `PartyMembers` and `CanShowMessage`.
- **Actions:** `Teleport`, `StartBattle`, `ShowMessage`, `AskQuestion`, `RandomInt` and `ResetMapState` (clears the saved state of a map before it is replaced).
- **Helpers:** `Source` gives access to the files of the mod and `Log` writes to the console.
- **Quests:** `Quests` changes the progress of the quests of the mod (see [Quests](#quests)).
- `CharacterHelper` equips items, adds items to inventories and raises the level of party members with the same stat changes as the game.
- `EventFactory` creates map events with all their raw data initialized.
- `LabdataBuilder` creates new 3D labdata from the global 3D textures of the base game (walls with overlays from `Wall3D.amb`/`Overlay3D.amb`, objects from `Object3D.amb`, floors and ceilings from `Floors.amb`) or copies objects of the original labdata. Register the result with `Maps.SetLabdata`.

Mod assemblies are loaded into their own load context. Further assemblies the mod needs can be put next to the mod assembly. If the `.pdb` file is next to the mod assembly, it is loaded too, so error messages contain file names and line numbers.


## Quests

Mods never show the quests of the original game in the quest log. A mod can define its own quests in a `quests.json` at its root. If a mod has no quests, the quest log is disabled (no icon, the shortcut does nothing).

```json
{
  "Quests": [
    {
      "Id": "cave",
      "Name": { "en": "The old cave", "de": "Die alte Höhle" },
      "Steps": [
        { "Id": "enter", "Text": { "en": "Enter the cave" }, "Active": true,
          "CompleteOn": [ { "Type": "Map", "Map": 260 } ] },
        { "Id": "lever", "Text": { "en": "Pull the lever" }, "Hint": { "en": "It is in the north." },
          "ActivateOn": [ { "Type": "StepCompleted", "Step": "enter" } ],
          "CompleteOn": [ { "Type": "Event", "Map": 260, "X": 12, "Y": 3, "EventType": "Action" } ] },
        { "Id": "spiders", "Text": { "en": "Defeat spiders" }, "Amount": 3, "Active": true, "Requires": [ "lever" ],
          "CompleteOn": [ { "Type": "Battle", "MonsterGroup": 2 } ] }
      ]
    }
  ]
}
```

- A quest consists of steps. Texts are given per language (`en`, `de`, `fr`, `pl`, `cz`; English is the fallback). `Hint` is shown when hovering a step.
- A step is inactive until it is activated (`Active` for the start of a new game or `ActivateOn`). An active step is completed by one of its `CompleteOn` triggers. Inactive steps are not shown.
- `Requires`: the step is shown as blocked (and can't be completed) until these steps are completed.
- `Amount`: the step shows its progress like `(2/3)`. Each fired completion trigger adds 1, item triggers show the number of carried items. The step is completed when the amount is reached.
- Triggers (`Type`): `StepCompleted` (`Step`), `Map` (the party enters `Map`, optionally at `X`/`Y`), `Event` (a map event of `Map` is executed, optionally at `X`/`Y` and of an `EventType`), `Item` (the party carries `Amount` items with the index `Item`), `Battle` (a battle is won, optionally against `MonsterGroup`), `PartyMemberJoined` (optionally `PartyMember`) and `Level` (a party member reaches `Level`).
- Code mods can change the progress with `Api.Quests`: `Activate`, `Complete`, `SetAmount`, `AddAmount`, `GetState`, `GetAmount` and the event `StepCompleted`.
- The progress is stored with each savegame (as mod data) and reset for new games. Steps which are added by a newer mod version start in their initial state.

## Ambermoon DC

Ambermoon DC (`Ambermoon.Mod.AmbermoonDC`) is a dungeon crawler built as a code mod for the original Ambermoon. It is not part of the game downloads. Get the `.mod` file from the [Ambermoon DC releases](https://github.com/Pyrdacor/Ambermoon.net/releases?q=ambermoon-dc) and put it into the `Mods` folder (see [Installing mods](#installing-mods)).

- The party starts on floor 1 of an endless dungeon. Each floor is generated from a seed when it is entered and has a way down to the next floor.
- Every 5 floors a random dungeon theme is chosen (never the same one twice in a row). A theme defines the look, the music and the monsters of its floors (see below).
- Monsters roam the floors. Some wander and chase the party, others only move when they see it. Deeper floors have stronger monsters.
- Some floors have a pair of teleporters which connects two distant rooms.
- Every 5th floor a boss guards the way down, every 10th floor a boss with minions. Boss floors have extra treasure, and the boss drops an item which is an upgrade for one of the party members.
- Floors 2, 7, 12, 17, ... have a chest with a good piece of equipment which is an upgrade for one of the party members (the one on floor 2 is a weapon). Together with the boss loot the party finds good equipment every 2 or 3 floors. Story items (like the sword of Mando) are never part of the loot.
- The mod provides these option defaults: auto battle with 5 rounds, floor, ceiling and distance fog are shown. The player can change them as usual.
- The quest log shows milestones: reaching floors 10, 25, 50 and 100, defeating guardians and finding companions.
- Up to floor 15 monsters can't cast curses (lame, poison, petrify, disease, aging, irritation, madness, sleep, fear, blindness, drugs and dissolve victim). Damage spells are allowed.
- Every 12th floor a random companion wanders around and can be asked to join. Name, race, gender, portrait, class, attributes, skills, spells and the (short) dialogues are random. Level and equipment fit the depth. Party members can be dismissed as usual. Dismissed companions are gone, their party member slot is reused for later companions.
- Secret rooms are hidden behind illusory walls or behind walls which open when touched.
- From floor 4 on the floors are dark, so you need torches or light spells.

### Dungeon themes

The themes are JSON files in the folder `themes` of the mod (`Ambermoon.Mod.AmbermoonDC/themes`). Every file is one theme, they are used in the order of their file names. To add a theme, add a file there. All texture indices refer to the global 3D textures of the original game, so walls, objects and sprites of different original labdata can be combined freely.

```json
{
  "Id": "crypt",
  "Name": { "en": "Forgotten Crypt", "de": "Vergessene Gruft" },
  "Palettes": [ 6, 21, 23, 25 ],
  "CombatBackground": 2,
  "WallHeight": 380,
  "FloorTexture": 14, "FloorColor": 13,
  "CeilingTexture": 13, "CeilingColor": 5,
  "Music": [ 24 ],
  "BossMusic": 26,
  "Walls": [
    { "Texture": 89, "Weight": 3 },
    { "Texture": 88 }
  ],
  "Door": { "Closed": 85, "Open": 0 },
  "Chest": { "Labdata": 2, "Object": 25 },
  "Exit": { "Labdata": 2, "Object": 20 },
  "Entrance": { "Labdata": 2, "Object": 21 },
  "Teleporter": { "Labdata": 5, "Object": 2 },
  "Decorations": [ { "Labdata": 7, "Object": 7 } ],
  "Obstacles": [ { "Texture": 33, "Width": 48, "Height": 39 } ],
  "Monsters": [
    {
      "Id": "skeleton",
      "Template": 6,
      "Name": { "en": "Skeleton", "de": "Skelett" },
      "Sprite": { "Labdata": 7, "Object": 8 },
      "Element": "Undead",
      "Flags": "Undead",
      "Spells": [ "Lame" ],
      "SpellChance": 25,
      "Factors": { "HitPoints": 1.2, "Attack": 0.9 }
    }
  ],
  "Boss": { "Monster": "skeleton", "Factors": { "HitPoints": 1.5 } },
  "BossGroup": { "Boss": "skeleton", "Minions": [ "skeleton" ] }
}
```

- **Palettes:** All 3D textures use the palette of the map, so each floor uses one of the `Palettes` of its theme. The palette also colors the frame around the 3D view. Run `dotnet run --project Ambermoon.Mod.Tests -- palettes [details]` to see which palettes fit a theme: *original* palettes are used with all textures of the theme in the original game, *similar* palettes have nearly the same 16 colors the textures use (see `ThemePalettes`). The tests fail if a theme uses a palette which doesn't fit. Textures of original maps with other palettes (e.g. the mine of the forest moon with palette 12) only fit to themes which use them together with textures of the same palettes.
- **Look:** `FloorTexture`/`CeilingTexture` are indices into `Floors.amb` (0 = only use the color). `Walls` are 1 to 3 variations (texture index into `Wall3D.amb` plus optional overlays from `Overlay3D.amb`), each is also used as illusory wall for the secret passages. The open door texture should be transparent. If it is 0, the door disappears when it is opened.
- **Objects:** Either a copy of an object of an original labdata (`Labdata` and `Object`, 1-based) or a single texture of `Object3D.amb` (`Texture`, `Width`, `Height` and optionally `MappedWidth`, `MappedHeight`, `Frames` and `Z`). `Exit` is the way down (hole, stairs, ladder), `Entrance` the blocked way up at the start. `Decorations` are walkable (bones, stones, junk), `Obstacles` block movement (barrels, coffins, statues). They are only placed where they block nothing. `Teleporter` is optional. `RecruitSprite` is the sprite of the companions (default: the person of labdata 6). The sprites of the original monsters are only added if the theme has no monsters or no bosses.
- **Monsters:** Each monster uses an original monster as `Template` for its battle graphic, animations and palette. Name (up to 15 characters), `Sprite` on the map, `Class`, `Element`, `Flags` (battle flags like `Undead`), `Spells`, `SpellChance`, `AttacksPerRound` and `Morale` come from the theme. `Weight` sets how often a monster appears, `MaxCount` how many of it can be in a group, `MinFloor` (1 to 5) on which floor of the theme it appears first and `BossOnly` limits it to the bosses.
- **Combat values** are calculated from the level (the floor number plus `LevelOffset`): hit points, spell points, attack, defense, magic attack and defense, attributes, skills, experience and gold. The optional `Factors` scale each of these values. Bosses are 1 level higher and get extra factors (e.g. 2.5 times the hit points).
- **Bosses:** `Boss` is the boss of the 5th floor of the theme, `BossGroup` the boss with minions of the 10th floor. Without them, the global boss lists of the mod are used. Themes without monsters use the original monster groups.

### Building your own variant

After a build the ready-to-use mod folder is in `Ambermoon.Mod.AmbermoonDC/bin/<configuration>/net8.0/mod/AmbermoonDC/`:

```
AmbermoonDC/
  mod.json                          manifest (Id "ambermoon-dc", Base "Original", MinVersion "1.19", option defaults)
  quests.json                       quest log milestones
  Ambermoon.Mod.AmbermoonDC.dll     the code, referenced by "Assembly" in mod.json
  Ambermoon.Mod.AmbermoonDC.pdb     debug symbols (optional)
  themes/
    01-caves.json
    02-crypt.json
    03-dwarf-mines.json
    04-fire-tower.json
    05-forest-moon-mine.json
```

To build and pack it yourself:

```
dotnet build Ambermoon.Mod.AmbermoonDC/Ambermoon.Mod.AmbermoonDC.csproj -c Release
dotnet run --project Ambermoon.ModPacker -c Release -- pack Ambermoon.Mod.AmbermoonDC/bin/Release/net8.0/mod/AmbermoonDC AmbermoonDC-1.0.mod
```

These are the same steps the [release workflow](#releasing-ambermoon-dc) runs. It also runs `dotnet run --project Ambermoon.Mod.Tests -c Release -- all` in between.

The themes and the quest log are plain JSON and are read from the mod folder at startup, so you can make your own variant without C#:

1. Copy the built `AmbermoonDC` folder into `Mods` and give it a new name, e.g. `MyDungeon`.
2. In `mod.json`, change `Id` (e.g. `my-dungeon`) and `Name`. With a new id your variant gets its own savegames, and it doesn't hide the original Ambermoon DC (a folder and a `.mod` file with the *same* id would replace each other).
3. Edit the files in `themes/` or add a new one, e.g. `06-my-theme.json` as a copy of `02-crypt.json` with a new `Id`. Theme ids must be unique. You can also change `quests.json` (see [Quests](#quests)). Ambermoon DC completes most of its quest steps from its code, so new steps need triggers.
4. Start the game, select your variant and confirm the trust question (the mod contains code).

The palette check of the tests (see **Palettes** above) reads the themes from the last build of `Ambermoon.Mod.AmbermoonDC` (its `bin` folder). To check your theme, put it into `Ambermoon.Mod.AmbermoonDC/themes` and build the project first.

### Releasing Ambermoon DC

The GitHub workflow `Release Ambermoon DC` (`.github/workflows/release-ambermoon-dc.yml`) builds the mod, runs the tests, packs it into `AmbermoonDC-<version>.mod` and publishes it as a GitHub release with the tag `ambermoon-dc-v<version>`:

1. Increase `Version` (and `ReleaseDate`) in `Ambermoon.Mod.AmbermoonDC/mod.json`.
2. Push the tag `ambermoon-dc-v<version>` (e.g. `ambermoon-dc-v1.1`), or run the workflow manually in the Actions tab with `publish` checked.

Without `publish` the workflow only builds the `.mod` file as a workflow artifact. The release is never marked as the latest release of the repository, which stays the game.

For testing, the environment variables `AMBERMOON_DC_SEED` and `AMBERMOON_DC_START_FLOOR` set the seed and the first floor of a new game. They only work in debug builds of the mod.


## Tests

`Ambermoon.Mod.Tests` contains headless tests for the mod system and Ambermoon DC. They use the builtin game data (`Ambermoon.net/versions.dat`):

```
dotnet run --project Ambermoon.Mod.Tests             # all tests
dotnet run --project Ambermoon.Mod.Tests -- render 10 # prints floor 10 of Ambermoon DC as text
```

To test a mod in the running game, use scenario scripts (see [Testing and debugging](#testing-and-debugging)).


## Known limitations

These things are not supported (yet):

**Mod system**
- Only one mod can be active at a time. Mods can't depend on or be combined with other mods.
- Mods only work with the builtin versions of the original game data. They are not offered for game data from an external folder (option `--data` or the external version), and the newer Pyrdacor game data format is not supported as a base.
- Mods are not available on Android. The Android frontend does not list or load mods, and it was not built or tested with the mod changes.
- Mod code is not sandboxed. The trust question is the only protection.
- The version selector shows only the first 20 characters of a mod name and has no scroll bar (use the mouse wheel or the arrow keys).

**Asset mods**
- Graphics can only be replaced for the graphic types listed above. Battle graphics of monsters, 3D textures (walls, objects, floors and ceilings), tilesets, fonts and intro/outro graphics can't be replaced.
- Replaced graphics must have the same size as the original ones.
- General texts (UI texts, the dictionary, places, intro and outro texts) can't be changed. Only texts of maps, items, monsters and characters can be changed with patches.
- Merchants, places, initial chest contents and the initial savegame can't be changed with asset files (code mods can change the savegame in `OnGameStarting`).
- Only MP3 files are supported for music, no Sonic Arranger songs.
- New map definitions were only tested with 3D maps. 2D maps (especially world maps) may need properties which are not covered by the JSON format.
- New monsters need an existing monster as template. Their battle graphic is the one of the template.

**Code mods**
- New monsters (with new indices) must be added in `OnInitialize`. Monsters added later have no battle graphic.
- Party members can only use the existing party member slots (1 to 15), as the texts of party members are not stored in savegames. Mods which change party member texts must restore them after loading (see Ambermoon DC).
- Maps with indices of 768 and above lose tile changes when a game is loaded. Use 529 to 767 for new maps.
- Dismissing party members only works on maps of the world Lyramion.
- The battle loot can only be changed through the inventory and gold of the monsters. There is no hook to add extra loot after a battle.

**Ambermoon DC**
- Only the original game is supported as base, not Ambermoon Advanced.
- All texts of the mod exist in English and German. Other languages use the English texts.
- There are 5 themes so far (caves, crypt, dwarven mines, burning halls, forest moon mine). Some theme monsters share map sprites, as there are only a few monster sprites in the original game.
- The first 4 themes are made of textures of maps with palette 6. Their other palettes are only similar variants, which also give the frame around the 3D view another color. The forest moon mine uses the palettes of the forest moon (12 and 11), only the map sprite of the Gizzek comes from a map with palette 11.
- The combat value formula of the theme monsters was only tested roughly against the original monsters.
- Random companions can only use the portraits the original game has for their race and gender, so some combinations don't exist (e.g. there are no female gnomes).
- The balancing was not tested beyond a few floors. The hero is alone until floor 12 and the floors are dark from floor 4 on.

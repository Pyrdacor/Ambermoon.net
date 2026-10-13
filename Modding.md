# Modding

Ambermoon.net supports mods. A mod is applied on top of a base game the player already has (the original Ambermoon or Ambermoon Advanced in any language). Mods never contain the original game data.

There are two kinds of mods. Both can be combined in a single mod.

- **Asset mods** change or add data: graphics, music, maps, monsters, items and so on. They don't need any programming.
- **Code mods** contain a .NET assembly which hooks into the game with C# code.

Mods with code are only started after the player has confirmed once that they trust the mod. If the mod assembly changes (for example after an update), the player is asked again.


## Installing mods

Put the mod folder or the packed `.mod` file into the `Mods` folder next to the Ambermoon.net executable. You can change this path with the configuration option `ModPath`.

Each mod is shown in the version selector. It is offered in every language of the base game it needs, so you can choose the language with the flags as usual. The version list can be scrolled if there are more than 5 entries.

Each mod uses its own savegames, which are stored in the folder `Saves/mod_<id>_<language>`.

You can also start a mod directly with the command line option `--mod <id>`. The game then starts in the configured language, and the version selector and the trust question are skipped.


## Creating a mod

The mod packer tool (project `Ambermoon.ModPacker`) helps with the setup:

```
AmbermoonModPacker new MyMod my-mod "My Mod"   # creates the folder MyMod with a mod.json
AmbermoonModPacker validate MyMod              # checks the mod
AmbermoonModPacker pack MyMod                  # checks the mod and creates my-mod.mod
```

While you develop a mod, you can put the folder directly into the `Mods` folder. A packed `.mod` file is a zip archive of the mod folder. If a folder and a `.mod` file have the same id, the folder is used.


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
  "Assembly": "MyMod.dll"
}
```

| Key | Description |
| --- | --- |
| Id | Unique id. Only letters, digits, `-`, `_` and `.` are allowed. It is used for the savegame folder. |
| Name | Shown in the version selector (the first 20 characters). |
| Version | Version as `Major.Minor`. |
| ApiVersion | Version of the mod API the mod was made for (currently 1). |
| Base.Game | `Original`, `Advanced` or `Any`. |
| Base.MinVersion | Optional minimum version of the base game, e.g. `1.20`. |
| Assembly | Optional path of the mod assembly. Leave it out for asset-only mods. |


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
| `assets/music/{song}.mp3` | Replaces a song. See [CustomMusic.md](CustomMusic.md) for the song numbers. |

A patch only contains the properties you want to change, using the property names of the data classes in `Ambermoon.Data.Common`. Enums can be given by name, and flags as comma separated names.

```json
// assets/monsters/2.json
{ "Name": "HUGE SPIDER", "DefeatExperience": 99 }

// assets/monsters/120.json (a new monster)
{ "Template": 2, "Name": "COPY SPIDER" }

// assets/maps/258.json
{ "Texts": [ "MY HOUSE" ], "MusicIndex": 3 }
```

The graphic types are `Player`, `Portrait`, `Item`, `Layout`, `LabBackground`, `Cursor`, `Pics80x80`, `UIElements`, `EventPictures`, `TravelGfx`, `Transports`, `NPC`, `CombatBackground`, `CombatGraphics`, `BattleFieldIcons`, `AutomapGraphics` and `RiddlemouthGraphics`. Colors are mapped to the nearest color of the primary UI palette. You can use another palette with an additional `{index}.json` like `{ "Palette": 6 }`. Pixels with an alpha value below 128 become transparent.


### New maps

A complete map is described as a grid of characters and a legend:

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

- 3D maps use `Wall` and `Object` (indices of the labdata) in the legend. 2D maps use `Back` and `Front` (tile indices of the tileset, given as `Tileset`).
- `Event` in the legend and in characters references an event by its 1-based position in `Events`. Each referenced event is an entry point. A map can have up to 64 of them.
- `Next` links events to a chain (1-based, 0 = end). Branch targets of conditions, doors, chests etc. are 0-based indices into `Events`, and 65535 means none.
- Every event needs a `Type` (see `EventType` in `Ambermoon.Data.Common/Event.cs`). The other properties are those of the matching event class.
- Positions of characters and of teleport targets are 1-based. A map can have up to 32 characters.
- New maps should use the indices 529 to 767.


## Code mods

A code mod is a .NET 8 class library which references `Ambermoon.Mod`. It must contain exactly one public class which derives from `ModBase`. The project `Ambermoon.Mod.AmbermoonDC` is a complete example. After each build it creates a ready-to-use mod folder in `bin/<configuration>/net8.0/mod/`.

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
- `CharacterHelper` equips items, adds items to inventories and raises the level of party members with the same stat changes as the game.
- `EventFactory` creates map events with all their raw data initialized.

Mod assemblies are loaded into their own load context. Further assemblies the mod needs can be put next to the mod assembly.


## Ambermoon DC

Ambermoon DC (`Ambermoon.Mod.AmbermoonDC`) is a dungeon crawler built as a code mod for the original Ambermoon:

- The party starts on floor 1 of an endless dungeon. Each floor is generated from a seed when it is entered and has a hole which leads down.
- Monsters roam the floors. Some wander and chase the party, others only move when they see it. Deeper floors have stronger monsters.
- Every 5th floor a boss guards the way down, every 10th floor a boss with minions. Boss floors have extra treasure, and the boss drops an item which is an upgrade for one of the party members.
- Every 12th floor one of the original companions wanders around and can be asked to join. Level and equipment fit the depth. Party members can be dismissed as usual.
- Secret rooms are hidden behind illusory walls or behind walls which open when touched.
- From floor 4 on the floors are dark, so you need torches or light spells.

For testing, the environment variables `AMBERMOON_DC_SEED` and `AMBERMOON_DC_START_FLOOR` set the seed and the first floor of a new game.


## Tests

`Ambermoon.Mod.Tests` contains headless tests for the mod system and Ambermoon DC. They use the builtin game data (`Ambermoon.net/versions.dat`):

```
dotnet run --project Ambermoon.Mod.Tests             # all tests
dotnet run --project Ambermoon.Mod.Tests -- render 10 # prints floor 10 of Ambermoon DC as text
```

Scenario scripts (`--script`) support the commands `save <slot>` and `load <slot>` to test savegames of mods.

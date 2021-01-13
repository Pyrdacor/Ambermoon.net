# Ambermoon.net

## Introduction

Ambermoon.net is a full C# rewrite of Ambermoon.

I also created another github project called [Ambermoon](https://github.com/Pyrdacor/Ambermoon) for providing resources and research the game data. Feel free to have a look or contribute.

If you want to support this project or contribute scroll down to the bottom. You may also checkout my already working 'SerfCity (Die Siedler) rewrite' at [freeserf.net](https://github.com/Pyrdacor/freeserf.net).


## Download

Version | Windows 64bit | Linux 64bit
--- | --- | ---
0.1.5 beta | [Download](https://github.com/Pyrdacor/Ambermoon.net/releases/download/v0.1.5beta/Ambermoon.net-Windows.zip) | [Download](https://github.com/Pyrdacor/Ambermoon.net/releases/download/v0.1.5beta/Ambermoon.net-Linux.tar.gz)
0.1.4 beta | [Download](https://github.com/Pyrdacor/Ambermoon.net/releases/download/v0.1.4beta/Ambermoon.net-Windows.zip) | [Download](https://github.com/Pyrdacor/Ambermoon.net/releases/download/v0.1.4beta/Ambermoon.net-Linux.tar.gz)

Older releases can be found [here](https://github.com/Pyrdacor/Ambermoon.net/releases). Other platforms will follow.

[![Build status](https://ci.appveyor.com/api/projects/status/cr6temgl1vknho6t?svg=true)](https://ci.appveyor.com/project/Pyrdacor/ambermoon-net)

### How to run the game

1. You need the original game data (either ADF files or extracted files like Party_char.amb etc).
2. Put these files next to the downloaded executable file called Ambermoon.net.exe.
3. If it doesn't work try to start from Windows CMD or Linux bash and check for error printouts.
4. If this isn't working, try to install the latest .NET framework for your OS.
5. If this still doesn't help please file an issue on the [Issue tracker](https://github.com/Pyrdacor/Ambermoon.net/issues). Please provide error messages, your setup (OS, game version, etc) and a description what you tried.

**Note:** The game only runs on Windows and Ubuntu as of now. Feel free to try other linux distributions but I can't guarantee that it will run then.


## Screenshots

![Map rendering](https://github.com/Pyrdacor/Ambermoon.net/raw/master/Screenshots/MapRendering1.png "Map rendering")
![World map](https://github.com/Pyrdacor/Ambermoon.net/raw/master/Screenshots/MapRendering2.png "World map")
![Morag map](https://github.com/Pyrdacor/Ambermoon.net/raw/master/Screenshots/MapRendering3.png "Morag map")
![3D map rendering](https://github.com/Pyrdacor/Ambermoon.net/raw/master/Screenshots/Map3D.png "3D map rendering")
![Battles](https://github.com/Pyrdacor/Ambermoon.net/raw/master/Screenshots/Battle.png "Battles")
![Spells](https://github.com/Pyrdacor/Ambermoon.net/raw/master/Screenshots/Spells.png "Spells")
![Battle loot](https://github.com/Pyrdacor/Ambermoon.net/raw/master/Screenshots/BattleLoot.png "Battle loot")
![Battle positions](https://github.com/Pyrdacor/Ambermoon.net/raw/master/Screenshots/BattlePositions.png "Battle positions")
![Chests](https://github.com/Pyrdacor/Ambermoon.net/raw/master/Screenshots/Chests.png "Chests")
![Inventory](https://github.com/Pyrdacor/Ambermoon.net/raw/master/Screenshots/Inventory.png "Inventory")
![Load game](https://github.com/Pyrdacor/Ambermoon.net/raw/master/Screenshots/LoadWindow.png "Load game")
![Conversation](https://github.com/Pyrdacor/Ambermoon.net/raw/master/Screenshots/Conversation.png "Conversation")
![Monsters](https://github.com/Pyrdacor/Ambermoon.net/raw/master/Screenshots/Monsters3D.png "Monsters")
![Events](https://github.com/Pyrdacor/Ambermoon.net/raw/master/Screenshots/EventBox.png "Events")
![Text box](https://github.com/Pyrdacor/Ambermoon.net/raw/master/Screenshots/DecisionBox.png "Text box")
![Active items](https://github.com/Pyrdacor/Ambermoon.net/raw/master/Screenshots/SpecialItems.png "Active items")


## Controls

Key | Description
--- | ---
Up | Move up (2D) or forward (3D)
Down | Move down (2D) or backward (3D)
Left | Move left (2D) or turn left (3D)
Right | Move right (2D) or turn right (3D)
F11 | Toggle fullscreen mode
ESC | Close ingame windows
Num1-Num9 | Buttons 1-9
1-6 | Set party member 1-6 as active
F1-F6 | Open inventory of party member 1-6


## Change log

- Version [0.1.5 beta](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v0.1.5beta): Added battle position window, battle loot window, spell blocking animation and spell items
- Version [0.1.4 beta](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v0.1.4beta): Added all remaining battle spells, bug fixing
- Version [0.1.3 beta](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v0.1.3beta): Added more spells, bug fixing
- Version [0.1.2 beta](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v0.1.2beta): Added more spells, added damage display on battle field, added critical hits
- Version [0.1.1 beta](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v0.1.1beta): Fixed end battle crash, fixed monster death animation, added firepillar spell
- Version [0.1.0 beta](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v0.1.0beta): Many improvements to battles and UI, some spells do damage or have effects now

The following versions are pre-beta versions:

- Version [1.1.21](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.1.21): Added spell list, many additions and fixed regarding chests, inventory and battles, bugfixing
- Version [1.1.20](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.1.20): Improved battle animations, added first test spell, fix flee logic
- Version [1.1.19](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.1.19): Improvements, additions and fixes for battles
- Version [1.1.18](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.1.18): First battle implementation (no spells yet, beta status)
- Version [1.1.17](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.1.17): Bugfixing
- Version [1.1.16](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.1.16): Added 3D NPCs interaction, turn toward monsters, active spells and items, bugfixing
- Version [1.1.15](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.1.15): Added 3D NPCs/monsters with movement (still WIP), bugfixing
- Version [1.1.14](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.1.14): Added 2D NPCs and rudimentary conversation window, bugfixing
- Version [1.1.13](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.1.13): One-time events now working, UI improvements, bugfixing
- Version [1.1.12](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.1.12): Added many inventory features, bugfixing
- Version [1.1.11](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.1.11): Added player stat window, improved fading
- Version [1.1.10](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.1.10): Travel improvements, equipment window additions, bugfixes
- Version [1.1.9](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.1.9): Added 3D animations, bugfixes
- Version [1.1.8](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.1.8): Added display of OUCH when movement is blocked, bugfixes
- Version [1.1.7](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.1.7): Added swimming, fixed tile blocking (also with transports)
- Version [1.1.6](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.1.6): Added dice100 event, added trigger condition for text popups, numpad arrows in 3D now work as in original game
- Version [1.1.5](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.1.5): Added text inputs (e.g. for riddlemouth), improved transport handling
- Version [1.1.4](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.1.4): Added 3D map interaction, riddlemouths and transports, bugfixing
- Version [1.1.3](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.1.3): Added text, decision and event popups, bugfixing
- Version [1.1.2](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.1.2): Bugfixing
- Version [1.1.1](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.1.1): Game will now use chest locked states and map changes from savegames, bugfixing
- Version [1.1.0](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.1.0): Added option and load game menu, added savegame loading, fixed 3D textures, bugfixing
- Version [1.0.19](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.0.19): Added portrait borders and character bars, added buttons for 2D/3D maps and inventory
- Version [1.0.18](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.0.18): Map names, empty portrait slots, dead character portrait, bugfixes
- Version [1.0.17](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.0.17): Character info, names at portraits, active member logic, fade effects
- Version [1.0.16](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.0.16): Better window handling, disabled scrollbars and item slots, bugfixes
- Version [1.0.15](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.0.15): Added equipment, scrollbars, improved item dragging, many bugfixes
- Version [1.0.14](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.0.14): Added inventory and item drag&drop
- Version [1.0.13](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.0.13): Moving with mouse, map transition fading effect, chest items, change tile events, portraits, initial savegame used
- Version [1.0.12](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.0.12): Added cursors, added first version of chest map events
- Version [1.0.11](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.0.11): Fixed map changes, world map now uses a smaller sprite (not the correct yet)
- Version [1.0.10](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.0.10): Fixed 3D map crash, improved map event handling
- Version [1.0.9](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.0.9): Added floor billboards like holes/lava, fixed billboards
- Version [1.0.8](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.0.8): Lots of 3D improvements.
- Version [1.0.7](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.0.7): Smoother 3D movement (thanks to Metibor), new flag "LegacyMode" in config to use lower 3D fps
- Version [1.0.6](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.0.6): Now uses the updated executable loader for item loading
- Version [1.0.5](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.0.5): All ADF files can now be used (OFS, FFS, INTL, DIRC)
- Version [1.0.4](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.0.4): Config now allows to specify if to use an external data path or the application folder (default)
- Version [1.0.3](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.0.3): Fixed startup exceptions related to wrong data path
- Version [1.0.2](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.0.2): Improved 2D movement and rendering
- Version [1.0.1](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.0.1): Added 3D map events and collision detection
- Version [1.0.0](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.0.0): First release (2D and 3D map movement, 2D map events and collision detection)


## Support development

If you want to support this project you can donate with PayPal.

[![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=76DV5MK5GNEMS&source=url)

You can also flattr.

[![Flattr](http://api.flattr.com/button/flattr-badge-large.png)](https://flattr.com/submit/auto?user_id=Pyrdacor&url=https://github.com/Pyrdacor/Ambermoon.net&title=Ambermoon.net&language=C#&tags=github&category=software)

Thank you very much.


## Contribution

If you want to help use the issue tracker to report bugs or create pull requests.

I appreciate any kind of help.

Special thanks to:
- Metibor
- Thallyrion
- a1exh
- kermitfrog
- Karol-13

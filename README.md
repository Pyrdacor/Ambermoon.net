# Ambermoon.net

## Introduction

Ambermoon.net is a full C# rewrite of Ambermoon and will run on Windows and Linux.

### Download

Version | Windows 64bit | Linux 64bit | Windows 32bit
--- | --- | --- | ---
0.1.8 beta | [Download](https://github.com/Pyrdacor/Ambermoon.net/releases/download/v0.1.8beta/Ambermoon.net-Windows.zip) | [Download](https://github.com/Pyrdacor/Ambermoon.net/releases/download/v0.1.8beta/Ambermoon.net-Linux.tar.gz) | [Download](https://github.com/Pyrdacor/Ambermoon.net/releases/download/v0.1.8beta/Ambermoon.net-Windows32Bit.zip)
0.1.7 beta | [Download](https://github.com/Pyrdacor/Ambermoon.net/releases/download/v0.1.7beta/Ambermoon.net-Windows.zip) | [Download](https://github.com/Pyrdacor/Ambermoon.net/releases/download/v0.1.7beta/Ambermoon.net-Linux.tar.gz) | [Download](https://github.com/Pyrdacor/Ambermoon.net/releases/download/v0.1.7beta/Ambermoon.net-Windows32Bit.zip)
0.1.6 beta | [Download](https://github.com/Pyrdacor/Ambermoon.net/releases/download/v0.1.6beta/Ambermoon.net-Windows.zip) | [Download](https://github.com/Pyrdacor/Ambermoon.net/releases/download/v0.1.6beta/Ambermoon.net-Linux.tar.gz) | [Download](https://github.com/Pyrdacor/Ambermoon.net/releases/download/v0.1.6beta/Ambermoon.net-Windows32Bit.zip)

Older releases can be found [here](https://github.com/Pyrdacor/Ambermoon.net/releases). Other platforms will follow.

[![Build status](https://ci.appveyor.com/api/projects/status/cr6temgl1vknho6t?svg=true)](https://ci.appveyor.com/project/Pyrdacor/ambermoon-net)

<img src="https://github.com/Pyrdacor/Ambermoon.net/raw/master/Screenshots/MapRendering2.png" width="75%" height="75%" />

If you want to support this project or contribute scroll down to the bottom. You may also checkout my already working 'SerfCity (Die Siedler) rewrite' at [freeserf.net](https://github.com/Pyrdacor/freeserf.net).

I also created another github project called [Ambermoon](https://github.com/Pyrdacor/Ambermoon) for providing resources and research the game data. Feel free to have a look or contribute.

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
ESC | Close ingame windows, stop item dragging/specific cursors/etc
Num1-Num9 | Buttons 1-9
1-6 | Set party member 1-6 as active
F1-F6 | Open inventory of party member 1-6

### Right clicks

- On portrait will open the inventory.
- On buttons in map view will toggle between move arrows and actions.
- In 2D map view will show a crosshair cursor which can be used for interactions. Or it aborts an already active action cursor.
- In 3D map view:
  - On the corners (turn arrows) it will perform 90° or 180° rotations.
  - On the center (Zzz) it will trigger interactions with a nearby map object.
- On item it will pickup all the items without asking for the amount.
- When dragging an item it will reset the item back to its source slot.

### Left clicks

- On portrait will select the party member as the active one.
- Drag&drop of items.


## Changelog

You can find the full changelog [here](changelog.md). You can also look at the [releases](https://github.com/Pyrdacor/Ambermoon.net/releases). They have more details in general.


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

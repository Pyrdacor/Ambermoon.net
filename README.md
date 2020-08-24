# Ambermoon.net

## Download

Version | OS
--- | ---
[1.0.12](https://github.com/Pyrdacor/Ambermoon.net/releases/download/v1.0.12/Ambermoon.net-Windows.zip) | Windows 64bit
[1.0.11](https://github.com/Pyrdacor/Ambermoon.net/releases/download/v1.0.11/Ambermoon.net-Windows.zip) | Windows 64bit
[1.0.10](https://github.com/Pyrdacor/Ambermoon.net/releases/download/v1.0.10/Ambermoon.net-Windows.zip) | Windows 64bit

Older releases can be found [here](https://github.com/Pyrdacor/Ambermoon.net/releases). Other platforms will follow.

[![Build status](https://ci.appveyor.com/api/projects/status/cr6temgl1vknho6t?svg=true)](https://ci.appveyor.com/project/Pyrdacor/ambermoon-net)


## Introduction

Ambermoon.net should become a full C# rewrite of Ambermoon. For the first version the original game data will be used (but not provided). The loader will be able to load ADF disk files or the extracted files like Party_char.amb etc.

Later maybe new data (enhanced textures, etc) may be provided or even the possibility to mod several things.

First focus lies on loading and decrypting data so parts of this project can be used by others to read and understand the original game data as well.

I also created another github project called [Ambermoon](https://github.com/Pyrdacor/Ambermoon) for providing resources and research the game data. Feel free to have a look or contribute.

Let's bring this game to life on modern PCs with modern resolutions and graphic APIs. :)

You may also checkout my already working Settlers I rewrite at [freeserf.net](https://github.com/Pyrdacor/freeserf.net).


## Current state

Working things:
- 2D maps
	- movement / auto scrolling / collision detection
	- change map events / chest events (only display the window)
	- walk animations, animated tiles, auto-sit, auto-sleep
- 3D maps
	- movement / static billboards / collision detection
- window mode / fullscreen
- cursors (only used sword, eye, hand and mouth at the moment)
- text rendering including runes (but not used yet)

![Map rendering](https://github.com/Pyrdacor/Ambermoon.net/raw/master/Screenshots/MapRendering1.png "Map rendering")
![World map](https://github.com/Pyrdacor/Ambermoon.net/raw/master/Screenshots/MapRendering2.png "World map")
![3D map rendering 1](https://github.com/Pyrdacor/Ambermoon.net/raw/master/Screenshots/Map3D1.png "3D map rendering 1")
![3D map rendering 2](https://github.com/Pyrdacor/Ambermoon.net/raw/master/Screenshots/Map3D2.png "3D map rendering 2")
![Billboards](https://github.com/Pyrdacor/Ambermoon.net/raw/master/Screenshots/Billboards1.png "Billboards")
![Text rendering](https://github.com/Pyrdacor/Ambermoon.net/raw/master/Screenshots/TextRendering.png "Text rendering")
![Chests](https://github.com/Pyrdacor/Ambermoon.net/raw/master/Screenshots/Chests.png "Chests")


## Controls

Key | Description
--- | ---
Up | Move up (2D) or forward (3D)
Down | Move down (2D) or backward (3D)
Left | Move left (2D) or turn left (3D)
Right | Move right (2D) or turn right (3D)
F11 | Toggle fullscreen mode
ESC | Leave fullscreen mode, leave other ingame windows
Num7 | Eye cursor
Num8 | Hand cursor
Num9 | Mouth cursor


## Change log

- Version 1.0.12: Added cursors, added first version of chest map events
- Version 1.0.11: Fixed map changes, world map now uses a smaller sprite (not the correct yet)
- Version 1.0.10: Fixed 3D map crash, improved map event handling
- Version 1.0.9: Added floor billboards like holes/lava, fixed billboards
- Version 1.0.8: A lot of 3D improvements (see [here](https://github.com/Pyrdacor/Ambermoon.net/releases/tag/v1.0.8)).
- Version 1.0.7: Smoother 3D movement (thanks to Metibor), new flag "LegacyMode" in config to use lower 3D fps
- Version 1.0.6: Now uses the updated executable loader for item loading
- Version 1.0.5: All ADF files can now be used (OFS, FFS, INTL, DIRC)
- Version 1.0.4: Config now allows to specify if to use an external data path or the application folder (default)
- Version 1.0.3: Fixed startup exceptions related to wrong data path
- Version 1.0.2: Improved 2D movement and rendering
- Version 1.0.1: Added 3D map events and collision detection
- Version 1.0.0: First release (2D and 3D map movement, 2D map events and collision detection)


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

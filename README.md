# Ambermoon.net

## Download

Version | OS
--- | ---
[1.0.0](https://github.com/Pyrdacor/Ambermoon.net/releases/download/v1.0.0/Ambermoon.net-Windows.zip) | Windows 64bit

Other platforms will follow.

[![Build status](https://ci.appveyor.com/api/projects/status/cr6temgl1vknho6t?svg=true)](https://ci.appveyor.com/project/Pyrdacor/ambermoon-net)


## Introduction

Ambermoon.net should become a full C# rewrite of Ambermoon. For the first version the original game data will be used (but not provided). The loader will be able to load ADF disk files or the extracted files like Party_char.amb etc.

Later maybe new data (enhanced textures, etc) may be provided or even the possibility to mod several things.

First focus lies on loading and decrypting data so parts of this project can be used by others to read and understand the original game data as well.

I also created another github project called [Ambermoon](https://github.com/Pyrdacor/Ambermoon) for providing resources and research the game data. Feel free to have a look or contribute.

Let's bring this game to life on modern PCs with modern resolutions and graphic APIs. :)

You may also checkout my already working Settlers I rewrite at [freeserf.net](https://github.com/Pyrdacor/freeserf.net).


## Current state

2D map rendering is working. Moreover the player can be moved on 2D maps and map changes are possible. World map scrolling is also working. Collision detection, sitting and sleeping is also working.

3D map rendering also works for the most part. But for now only walls, ceiling and floor. Static objects work too but no monsters, etc.

No collision detection yet for 3D. You can freely walk through 2D and 3D maps. Map events only work in 2D so you won't be able to leave 3D maps yet. :)

Text rendering also works now with text replacements like character names, shadows and different text colors. Rune texts work too.

![Map rendering](https://github.com/Pyrdacor/Ambermoon.net/raw/master/Screenshots/MapRendering1.png "Map rendering")
![World map](https://github.com/Pyrdacor/Ambermoon.net/raw/master/Screenshots/MapRendering2.png "World map")
![3D map rendering 1](https://github.com/Pyrdacor/Ambermoon.net/raw/master/Screenshots/Map3D1.png "3D map rendering 1")
![3D map rendering 2](https://github.com/Pyrdacor/Ambermoon.net/raw/master/Screenshots/Map3D2.png "3D map rendering 2")
![Billboards](https://github.com/Pyrdacor/Ambermoon.net/raw/master/Screenshots/Billboards1.png "Billboards")
![Text rendering](https://github.com/Pyrdacor/Ambermoon.net/raw/master/Screenshots/TextRendering.png "Text rendering")


## Support development

If you want to support this project you can donate with PayPal.

[![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=76DV5MK5GNEMS&source=url)

You can also flattr.

[![Flattr](http://api.flattr.com/button/flattr-badge-large.png)](https://flattr.com/submit/auto?user_id=Pyrdacor&url=https://github.com/Pyrdacor/Ambermoon.net&title=Ambermoon.net&language=C#&tags=github&category=software)

Thank you very much.
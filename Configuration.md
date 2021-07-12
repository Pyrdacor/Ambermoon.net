# Configuration

The configuration is stored inside a file called "ambermoon.cfg". It will be stored either next to the Ambermoon.net executable if possible. Otherwise
it will be stored in the default operating system configuration directory:

- Windows: C:\Users\USERNAME\AppData\Roaming\Ambermoon
- Linux: home/.config/Ambermoon
- Mac: /Users/USERNAME/.config/Ambermoon


## Settings

Setting | Description | Possible values | Default value
--- | --- | --- | ---
Width | Width of the window in screen coordinates | Any positive integer | 5/8 of the monitor's screen width
Height | Height of the window in screen coordinates | Any positive integer | 10 \* Width / 16
FullscreenWidth | Width of the fullscreen mode in screen coordinates | Any positive integer | Will be automatically set to the max resolution
FullscreenHeight | Height of the fullscreen mode in screen coordinates | Any positive integer | Will be automatically set to the max resolution
Music | Enables music playback | true or false | true
Volume | Music volume in percent | Any multiple of 10 from 0 to 100 | 100
FastBattleMode | Enables faster battles | true or false | false

There are also some technical or more advanced settings which should only be changed by advanced users:

Setting | Description | Possible values | Default value
--- | --- | --- | ---
CacheMusic | Enables caching of music (uses ~184MB of disk storage but decreases startup time) | true or false | true
EnableCheats | Enables the cheat console | true or false | false
LegacyMode | If active movement is slower | true or false | false
GameVersionIndex | Remembers the last chosen game data version | 0 to 2 | 0
SaveOption | See below 'External data' | ProgramFolder or DataFolder | ProgramFolder
DataPath | See below 'External data' | Path to a data directory | Empty
UseDataPath | See below 'External data' | true or false | false


## External data

Ambermoon.net provides some built-in game data versions. A german and an english one.

But it also allows you to provide your own game data. For example you can mod game
data or provide a new language, etc.

For that purpose you can store extracted game data files or even ADF disk file images
at a specific location and tell Ambermoon.net to use it.

By default Ambermoon.net will automatically look for such data next to the executable.
But if you prefer another location (for example an emulator directory) you can activate
the option 'UseDataPath' and provide the path via the setting 'DataPath'.

For example if you use WinUAE and have a directory like "C:\WinUAE\Games\Ambermoon\Amberfiles"
you can set DataPath to this directory. Note that a data path should always directly
contain the game data files like NPC_char.amb etc.

When using external data (and only then) the setting 'SaveOption' can be used. It is
ignored for built-in data. By default Ambermoon.net will store savegames in a sub-folder
called "Saves" next to the executable (or inside the config folder if it can't write there).

But for external data you can choose to write the savegames directly to the data path.
This way you can directly play the original savegames and continue them with the emulator etc.
Note that this will overwrite your original savegames so use this with care and backup your
saves beforehand. This feature is not tested that much!

Of course you can't use that special save option if your own game data is provided by
ADF files as it isn't possible to write the savegames into the disk images.
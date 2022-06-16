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
BattleSpeed | Enables faster battles | 0 (= slow with clicks to continue), 10 to 100 (= this value as speed percentage increase, no clicks needed) | 0
AutoDerune | If active and you carry the rune alphabet item with you, all rune texts are shown as real text. Otherwise as runes. | true or false | true
ShowButtonTooltips | If active many buttons have tooltips when you hover them with the mouse | true or false | true
ShowPlayerStatsTooltips | If active many character values have tooltips when you hover them with the mouse | true or false | true
UsePatcher | Enables the patcher | null, true or false | null (the game will then ask if you want to use it)
PatcherTimeout | Timeout for checking new versions | millisecond value | time to wait for an answer from the server when checking for new versions
ShowPyrdacorLogo | If active the Pyrdacor logo is shown at start, otherwise it is skipped | true or false | true
ShowThalionLogo | If active the Thalion logo is shown at start, otherwise it is skipped | true or false | true
ShowSaveLoadMessage | If active a popup is shown after saving and loading a game | true or false | false
ShowFloor | If active the 3D floor texture is shown, otherwise only a color | true or false | true
ShowCeiling | If active the 3D ceiling texture is shown, otherwise only a color | true or false | true
GraphicFilter | Graphic filter to use | None, Smooth, Blur | None
GraphicFilterOverlay | Overlay to use | None, Lines, Grid, Scanlines, CRT | None
Effects | Additional graphic effects to use | None, Grayscale, Sepia | None
ExtendedSavegameSlots | Enables additional 20 savegame slots | true or false | true

Note that the names of the additional savegame slots are stored inside the configuration file as well. The secion is called AdditionalSavegameSlots. The first 10 slots are stored in a binary file called Saves instead. This binary file is compatible with the original Amiga game which only has 10 savegame slots.

### Desktop only

These settings are only available for desktop environments like Windows, Linux or macOS but not for mobile environments like Android or iOS.

Setting | Description | Possible values | Default value
--- | --- | --- | ---
WindowX | X coordinate of the window in screen coordinates | Any positive integer | A value so the window is centered
WindowY | Y coordinate of the window in screen coordinates | Any positive integer | A value so the window is centered
MonitorIndex | Index of the monitor the application is on | 0 to the number of monitors minus 1 | 0 (main monitor)

There are also some technical or more advanced settings which should only be changed by advanced users:

Setting | Description | Possible values | Default value
--- | --- | --- | ---
EnableCheats | Enables the cheat console | true or false | false
ExternalMusic | Enables external music (provided as mp3s in a folder called "music" next to the game, see [Custom Music](CustomMusic.md)) | true or false | false
LegacyMode | If active movement is slower (in the future this might be used for other stuff as well) | true or false | false
GameVersionIndex | Remembers the last chosen game data version (of the version selector) | 0 to 4 (4 is external data, see 'External data' below) | 0 (the first one)
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